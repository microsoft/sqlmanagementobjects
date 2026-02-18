// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// Internal IEnumerable utility class that walks over the
    /// given table and creates objects based on the string in
    /// the first column of each row.
    internal class SfcObjectIterator : IEnumerable, IEnumerator, IDisposable
    {
        ISfcDomain _root = null;
        string[] _fields = null;
        OrderBy[] _orderByFields = null;
        Type _type = null;
        // The reader to use regardless of whether it is from a backing cache of a DataTableReader, or a live one
        IDataReader _ResultsDataReader = null;
        // The data table and its reader when we are caching results
        DataTable _ResultsDataTable = null;
        DataTableReader _ResultsDataTableReader = null;
        Urn _urn = null;
        int _urnColIndex = 0;   // id of the column where the URN is located

        // Are we using a cloned connection or the original one from the domain root?
        bool _closeConnection = false;

        // Are we caching a private DataTable to emulate a live IDataReader or not?
        bool _cacheReader = false;

        // Do we expect only a single active query, or should we support multiple active queries to occur to the regular connection?
        // This may cause the domain root impl of GetQueryConnection to return the same connection, different connection or punt saying "please cache me"
        // Or are we just caching results to avoid all of this? Caching is the default since it always works and is simplest.
        SfcObjectQueryMode _activeQueriesMode = SfcObjectQueryMode.CachedQuery;

        // The connection used to query the Enumerator (whether the original one or a clone)
        ISfcConnection _connection = null;

        // The current object in the iteration.
        SfcInstance _currentInstance = null;

        // TODO:: Shouldn't this ctor be internal since it is only to be created via an ObjectQuery instance?
        /// <summary>
        /// Basic iterator over a query against a Sfc connection
        /// </summary>
        /// <param name="root">The domain root instance indicating the object hierarchy and connection.</param>
        /// <param name="activeQueriesMode">Indicates the cache or connection mode disposition needed to process this iterator successfully.
        /// This may affect how GetConnection on the domain root responds.</param>
        /// <param name="query">The query string to process.</param>
        /// <param name="fields">The field names to retrieve. If null, all default inexpensive fields are retrieved.</param>
        /// <param name="orderByFields"></param>
        public SfcObjectIterator(ISfcDomain root, SfcObjectQueryMode activeQueriesMode, SfcQueryExpression query, string[] fields, OrderBy[] orderByFields)
        {
            _root = root;
            _fields = fields;
            _urn = query.ToUrn();
            _activeQueriesMode = activeQueriesMode;
            _orderByFields = orderByFields;

            // since these queries can only be homogeneous we can get the type from the end
            // of the query. When the type factory gets cleaner we will likely be able to cache
            // the actual type factory here and not just the name
            _type = _root.GetType(_urn.Type);
            Debug.Assert(_type != null);

            // Determine the actual connection to use now. Only do this once in this object instance (not on Reset).
            GetConnection();

            MakeDataReader();
        }

        /// <summary>
        /// Close the iterator and its resources including the data reader used.
        /// Either call this method or use the Dispose() pattern to ensure resources are reclaimed.
        /// </summary>
        public void Close()
        {
            CloseDataReader();
            CloseClonedConnection();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        private bool IsDomainHop(Urn source, Urn destination)
        {
            return (source.XPathExpression[0].Name != destination.XPathExpression[0].Name);
        }

        private object CreateObjectHierarchy(Urn urn)
        {
            object parent = null;

            // Construct the object hiearchy recursively from urn
            if (null != urn.Parent)
            {
                parent = CreateObjectHierarchy(urn.Parent);
            }

            // Get the type and create the object
            Type type = _root.GetType(urn.Type);
            object obj = SfcRegistration.CreateObject(type.FullName);
            Debug.Assert(obj != null);

            // Set the object root to disconnected (design) mode 
            if (obj is IAlienRoot)
            {
                IAlienRoot alienRoot = obj as IAlienRoot;
                alienRoot.DesignModeInitialize();
                alienRoot.ConnectionContext.TrueName = urn.GetNameForType(urn.Type);
                alienRoot.ConnectionContext.ServerInstance = urn.GetNameForType(urn.Type); 
            }

            // Set the parent object reference
            if (null != parent)
            {
                if (obj is IAlienObject)
                {
                    IAlienObject alienObject = obj as IAlienObject;

                    // Set the Parent object 
                    PropertyInfo propInfo = alienObject.GetType().GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public);
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        propInfo.SetValue(alienObject, parent, null);
                    }

                    // Set the Name (key) property
                    propInfo = alienObject.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        propInfo.SetValue(alienObject, urn.GetNameForType(urn.Type), null);
                    }
                }
            }

            return obj;
        }

        Object IEnumerator.Current
        {
            get
            {

                Urn urn = _ResultsDataReader.GetString(_urnColIndex);
                SfcInstance instance = _root as SfcInstance;

                // Check the domain root and the root of the object urn
                // Its a domain hop if they dont match 
                if (IsDomainHop(instance.Urn, urn))
                {
                    // Create the object hiearchy from the urn
                    object obj = CreateObjectHierarchy(urn);

                    // Check if its SMO domain 
                    if (obj is ISqlSmoObjectInitialize)
                    {
                        // Initialize the property bag from the data reader.
                        ISqlSmoObjectInitialize sqlSmoObjectInit = obj as ISqlSmoObjectInitialize;
                        sqlSmoObjectInit.InitializeFromDataReader(_ResultsDataReader);

                        return sqlSmoObjectInit;
                    }
                    else
                    {
                        // TODO: VSTS-252089 Need to handle other domains (if any)
                        throw new SfcMetadataException(SfcStrings.DomainNotFound(urn.XPathExpression[0].ToString()));
                    }
                }


                // Make the current object only the first time Current is called at this position in the enumeration.
                // Repeated calls to Current should just return the same object.
                if (_currentInstance == null)
                {
                    SfcKeyChain kc = new SfcKeyChain(_ResultsDataReader.GetString(_urnColIndex), _root);
                    if( kc.Parent == null )
                    {
                        Debug.Assert( kc.LeafKey is DomainRootKey );
                        _currentInstance = kc.GetObject();
                    }
                    else
                    {
                        SfcInstance parent = kc.Parent.GetObject();
                        Debug.Assert(parent != null);

                        string elementTypeName = kc.LeafKey.InstanceType.Name;
                        ISfcCollection collection = parent.GetChildCollection(elementTypeName);
                        if( !collection.GetExisting(kc.LeafKey, out _currentInstance ) )
                        {
                            SfcObjectFactory factory = collection.GetElementFactory();
                            _currentInstance = factory.Create(parent,new SfcInstance.PopulatorFromDataReader(_ResultsDataReader),SfcObjectState.Existing);
                            collection.Add(_currentInstance);
                        }
                    }
                }

                return _currentInstance;
            }
        }

        bool IEnumerator.MoveNext ()
        {
            // Read to the next object and clear the current object so we make one the first time Current is asked for at this new position.
            _currentInstance = null;
            return _ResultsDataReader.Read();
        }

        void IEnumerator.Reset ()
        {
            CloseDataReader();
            MakeDataReader();
        }

        // Finalizer not wanted or needed. If you don't Dispose you will not be saved in time by a Finalizer anyhow for the purposes
        // of releasing the IDataReader herein.
        void IDisposable.Dispose()
        {
            Close();
        }

        // TODO:: This can be optimized to not recreate the local DataTable if we are in a cache mode.
        /// <summary>
        /// Close the DataReader (and the DataTable if we have one).
        /// </summary>
        private void CloseDataReader()
        {
            // Clear and close depending on cached or live reader mode
            if (_cacheReader)
            {
                if (_ResultsDataTableReader != null)
                {
                    _ResultsDataTableReader.Close();
                    _ResultsDataTableReader = null;
                    // The DataTableReader wraps the IDataReader, so no need to also dispose it
                    _ResultsDataReader = null;
                }
                // Clear the table just to help GC along
                if (_ResultsDataTable != null)
                {
                    _ResultsDataTable.Clear();
                    _ResultsDataTable = null;
                }
            }
            else
            {
                // We have a live IDataReader and it is open so we need to close it
                if (_ResultsDataReader != null && !_ResultsDataReader.IsClosed)
                {
                    _ResultsDataReader.Close();
                    _ResultsDataReader = null;
                }
            }
        }

        /// <summary>
        /// Close the connection if it was cloned.
        /// </summary>
        private void CloseClonedConnection()
        {
            // If the connection was cloned, Disconnect it now
            if (_closeConnection && _connection != null)
            {
                _connection.Disconnect();
                _connection = null;
            }
        }

        /// <summary>
        /// Determine the actual connection to use (original or clone).
        /// This is tricky since the DataReader we open and allow to live until this iterator object dies will conflict with any
        /// other DataReader the caller implicitly or explicitly needs while processing the results.
        /// 
        /// If multiple concurrent queries are supported on the connection (i.e. MARS), or we are not allowed to clone the connection
        ///   Use the one we have
        /// Else
        ///   try {Use a clone of the connection}
        ///   catch {Use the one we have}
        /// 
        /// We leave it to the caller to handle invalid cloning attempts, and to adjust their code to use the iterator to
        /// cache the results into their own temp collection before proceeding to perform more operations that also need a DataReader.
        /// 
        ///Also, if we are running in the SQLCLR context connection, we cannot directly clone it and have to make a "loopback" clone connection.
        /// </summary>
        private void GetConnection()
        {
            // The domain root impl will decide to use
            // 1. the current main connection.
            // 2. another connection.
            // 3. null, signifying we are either disconnected or want to use the current main connection and cache the result set in our own DataTable.
            ISfcConnection origConn = _root.GetConnection();

            if (_root.ConnectionContext.Mode == SfcConnectionContextMode.Offline ||
                _activeQueriesMode == SfcObjectQueryMode.CachedQuery)
            {
                // Cache it all
                _connection = null;
            }
            else
            {
                // If the domain returns null or throws, fall back to caching the data reader with our own data table
                try
                {
                    _connection = _root.GetConnection(_activeQueriesMode);
                }
                catch
                {
                    _connection = null;
                }
            }

            if (_connection == null)
            {
                _cacheReader = true;
                _connection = origConn;
            }

            // Additionally close the connection (after closing the data reader) if it is a secondary connection.
            _closeConnection = (origConn != _connection);

            return;
        }

        private void MakeDataReader()
        {
            _currentInstance = null;

            // currently the fields property is left blank this forces us to do a complete
            // fetch of all properties. This will get replaced with DefaultInitFields type functionality
            // TODO: Add the fields property to return the right number of fields
            Request request = new Request(_urn);
            request.Fields = _fields;
            request.ResultType = ResultType.IDataReader;
            request.OrderByList = _orderByFields;

            // Offline (disconnected) mode will always seems cached
            if (_cacheReader)
            {
                // Fill a private DataTable and use that as our IDataReader, since we need to close the real reader asap.
                _ResultsDataTable = new DataTable();
                _ResultsDataTable.Locale = CultureInfo.InvariantCulture;

                // Guard enumerator if we are disconnected)
                if (_root.ConnectionContext.Mode != SfcConnectionContextMode.Offline)
                {
                    using (_ResultsDataReader = EnumResult.ConvertToDataReader(Enumerator.GetData(_connection.ToEnumeratorObject(), request)))
                    {
                        _ResultsDataTable.Load(_ResultsDataReader);
                    }
                }

                // Use the data table as if it were just a fwd data reader
                _ResultsDataTableReader = _ResultsDataTable.CreateDataReader();
                _ResultsDataReader = (IDataReader)_ResultsDataTableReader;
            }
            else
            {
                // This data reader will remain live throughout the lifetime of this iterator object.
                // It is important for the caller to eventually Close it explicitly or via Dispose.
                _ResultsDataReader = EnumResult.ConvertToDataReader(Enumerator.GetData(_connection.ToEnumeratorObject(), request));
            }

            // we need to identify where the URN column is. 
            // It should be the first one so this should be quick but who knows
            DataTable schemaTable = _ResultsDataReader.GetSchemaTable();
            int colNameIdx = schemaTable.Columns.IndexOf("ColumnName");

            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                string columnName = schemaTable.Rows[i][colNameIdx] as string;

                if (string.Compare(columnName, "Urn", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _urnColIndex = i;
                    break;
                }
            }
        }
    }
}

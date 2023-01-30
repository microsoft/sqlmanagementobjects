// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Server;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// ArrayList of Urn objects
    /// </summary>
    public class UrnCollection : IList<Urn>
    {
        List<Urn> innerColl = null;

        #region ctors
        public UrnCollection()
        {
            innerColl = new List<Urn>();
        }
        #endregion

        #region IList implementation
        public int IndexOf(Urn urn)
        {
            return innerColl.IndexOf(urn);
        }

        public void Insert(int index, Urn urn)
        {
            innerColl.Insert(index, urn);
        }

        public void RemoveAt(int index)
        {
            innerColl.RemoveAt(index);
        }

        public Urn this[int index]
        {
            get
            {
                return innerColl[index];
            }
            set
            {
                innerColl[index] = value;
            }
        }

        public void Add(Urn urn)
        {
            innerColl.Add(urn);
        }

        public void AddRange(IEnumerable<Urn> urnCollection)
        {
            innerColl.AddRange(urnCollection);
        }

        public void Clear()
        {
            innerColl.Clear();
        }
        public bool Contains(Urn urn)
        {
            return innerColl.Contains(urn);
        }

        public void CopyTo(Urn[] array, int arrayIndex)
        {
            innerColl.CopyTo(array, arrayIndex);
        }

        public bool Remove(Urn urn)
        {
            return innerColl.Remove(urn);
        }

        public int Count
        {
            get
            {
                return innerColl.Count;
            }
        }

        bool ICollection<Urn>.IsReadOnly
        {
            get
            {
                return ((ICollection<Urn>)innerColl).IsReadOnly;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)innerColl).GetEnumerator();
        }

        public IEnumerator<Urn> GetEnumerator()
        {
            return innerColl.GetEnumerator();
        }
        #endregion

    }

    //public delegate void ScriptingErrorDelegate(Urn Current, Exception exc);
    public delegate void ScriptingErrorEventHandler(object sender, ScriptingErrorEventArgs e);

    public class ScriptingErrorEventArgs : EventArgs
    {
        internal ScriptingErrorEventArgs(Urn current, Exception innerException)
        {
            this.current = current;
            this.innerException = innerException;
        }

        Urn current;
        public Urn Current
        {
            get
            {
                return current;
            }
        }

        Exception innerException;
        public Exception InnerException
        {
            get
            {
                return innerException;
            }
        }
    }


    /// <summary>
    /// Instance class encapsulating Scripter object
    /// </summary>
    public class Scripter : DependencyWalker
    {
        public Scripter()
            : base()
        {
            Init();
        }

        public Scripter(Server svr)
            : base(svr)
        {
            if (null == svr)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("svr"));
            }

            Init();
        }

        protected internal void Init()
        {
        }

        private ProgressReportEventHandler scriptingProgress;
        public event ProgressReportEventHandler ScriptingProgress
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                scriptingProgress += value;
            }
            remove
            {
                scriptingProgress -= value;
            }
        }

        private ScriptingErrorEventHandler scriptingError;
        public event ScriptingErrorEventHandler ScriptingError
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                scriptingError += value;
            }
            remove
            {
                scriptingError -= value;
            }
        }

        private ScriptingOptions scriptingOptions;
        public ScriptingOptions Options
        {
            get
            {
                if (scriptingOptions == null)
                {
                    scriptingOptions = new ScriptingOptions();
                }
                return scriptingOptions;
            }

            set
            {
                if (value == null)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Options"));
                }

                scriptingOptions = value;
            }
        }

        // The only difference between function GetOptions and property Options, is that the former retrieves and set 
        // TargetServerVersion scripting option. This action was previously part of Init routine, but got moved here
        internal ScriptingOptions GetOptions()
        {
            ScriptingOptions op = Options;
            if (!op.GetScriptingPreferences().TargetVersionAndDatabaseEngineTypeDirty)
            {
                // default target server version should be set equal to the server we are scripting
                if (GetServerObject().ServerVersion == null)
                {
                    // going to the server for the version should populate ServerVersion structure
                    string sVersion = GetServerObject().Information.VersionString;
                    Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, sVersion);
                }

                op.SetTargetServerInfo(GetServerObject(), false);
            }

            return op;
        }

        private bool prefetchObjects = true;
        public bool PrefetchObjects
        {
            get
            {
                return (prefetchObjects && !this.Server.IsDesignMode);
            }
            set
            {
                prefetchObjects = value;
            }
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects.
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection ScriptWithList(SqlSmoObject[] objects)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }


            return EnumerableContainer.IEnumerableToStringCollection(EnumScriptWithList(objects));
        }

        /// <summary>
        /// Returns an IEnumerable<string> object with the script for the passed objects.
        /// </summary>
        public IEnumerable<string> EnumScriptWithList(SqlSmoObject[] objects)
        {
            if (null == objects)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("objects"));
            }

            DependencyCollection depList = new DependencyCollection();
            foreach (SqlSmoObject obj in objects)
            {
                depList.Add(new DependencyCollectionNode(obj.Urn, true, true));
            }
            return ScriptWithList(depList, objects);
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection ScriptWithList(UrnCollection list)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScriptWithList(list));
        }

        public IEnumerable<string> EnumScriptWithList(UrnCollection list)
        {
            if (null == list)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("list"));
            }

            return ScriptWithList(list, null);
        }

        internal IEnumerable<string> ScriptWithList(UrnCollection list, SqlSmoObject[] objects)
        {
            DependencyCollection depList = new DependencyCollection();
            for (int i = 0; i < list.Count; i++)
            {
                depList.Add(new DependencyCollectionNode(list[i], true, true));
            }
            return ScriptWithList(depList, objects);
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection ScriptWithList(Urn[] urns)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScriptWithList(urns));
        }

        public IEnumerable<string> EnumScriptWithList(Urn[] urns)
        {
            if (null == urns)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("urns"));
            }

            return ScriptWithList(urns, null);
        }

        internal IEnumerable<string> ScriptWithList(Urn[] urns, SqlSmoObject[] objects)
        {
            DependencyCollection depList = new DependencyCollection();
            foreach (Urn urn in urns)
            {
                depList.Add(new DependencyCollectionNode(urn, true, true));
            }
            return ScriptWithList(depList, objects);
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection ScriptWithList(DependencyCollection depList)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScriptWithList(depList));

        }

        public IEnumerable<string> EnumScriptWithList(DependencyCollection depList)
        {
            return ScriptWithList(depList, null);
        }

        internal IEnumerable<string> ScriptWithList(DependencyCollection depList, SqlSmoObject[] objects)
        {
            return ScriptWithList(depList, objects, false);
        }

        private IEnumerable<string> ScriptWithList(DependencyCollection depList, SqlSmoObject[] objects,bool discoveryRequired)
        {
            try
            {
                return ScriptWithListWorker(depList, objects,discoveryRequired);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                // we'll try to figure out the object to be scripted 
                // but if we can't use the Server
                SqlSmoObject mainObj = this.Server;

                if (null != objects && objects.Length == 1 && null != objects[0])
                {
                    // if there is only one object in the list we can throw 
                    // an error message that contains the object's name
                    mainObj = objects[0];

                    if (e is PropertyCannotBeRetrievedException)
                    {
                        // if we couldn't retrieve a property
                        // throw custom error message
                        FailedOperationException foe = new FailedOperationException(
                                ExceptionTemplates.FailedOperationExceptionTextScript(
                                                        SqlSmoObject.GetTypeName(mainObj.GetType().Name),
                                                        mainObj.ToString()),
                                e);

                        //add additional properties
                        foe.Operation = ExceptionTemplates.Script;
                        foe.FailedObject = mainObj;

                        throw foe;
                    }
                }
                else if (null != depList && depList.Count == 1 && null != depList[0])
                {
                    // if the dependency list contains only one Urn 
                    // let's try to convert it into an object, 
                    // and ignore the error if conversion fails
                    try
                    {
                        mainObj = this.Server.GetSmoObject(depList[0].Urn);
                    }
                    catch
                    {
                    }
                }

                Diagnostics.TraceHelper.Assert(null != mainObj, "null == mainObj");
                throw new FailedOperationException(ExceptionTemplates.Script, mainObj, e);
            }
        }       

        private IEnumerable<string> ScriptWithListWorker(DependencyCollection depList, SqlSmoObject[] objects,bool discoveryRequired)
        {
            ScriptingOptions options = this.GetOptions();

            CheckConflictingOptions();

            EnumerableContainer queryEnumerable = new EnumerableContainer();

            Server server = (this.Server != null) ? this.Server : objects[0].GetServerObject();
            ScriptMaker scriptMaker = new ScriptMaker(server);
            // The Server's edition in Azure will never be DW, so we want to pass along the DB-specific edition if we know it
            scriptMaker.SourceDatabaseEngineEdition = (objects == null || objects.Length == 0) ? server.DatabaseEngineEdition: objects[0].DatabaseEngineEdition;
            scriptMaker.Preferences = options.GetScriptingPreferences();
            scriptMaker.discoverer = this.GetDiscoverer(server,options,discoveryRequired);

            SmoUrnFilter filter = options.GetSmoUrnFilterForFiltering(server);
            if ( filter!= null)
            {
                scriptMaker.Filter = filter;
            }

            if (this.scriptingError != null)
            {
                scriptMaker.ScriptingError += this.scriptingError;
            }

            if (this.scriptingProgress != null)
            {
                scriptMaker.ObjectScripting += new ObjectScriptingEventHandler(scriptMaker_ObjectScripting);
            }

            scriptMaker.Prefetch = this.PrefetchObjects;

            if ((!string.IsNullOrEmpty(options.FileName)) && (options.ToFileOnly))
            {
                if (SqlContext.IsAvailable)
                {
                    throw new Exception(ExceptionTemplates.SmoSQLCLRUnAvailable);
                }

                using(SingleFileWriter writer = new SingleFileWriter(options.FileName, options.AppendToFile, options.Encoding))
                {
                writer.ScriptBatchTerminator = !options.NoCommandTerminator;
                writer.BatchTerminator = Globals.Go;
                scriptMaker.Script(depList, objects, writer);
                }
            }
            else
            {
                SmoStringWriter writer = new SmoStringWriter();
                scriptMaker.Script(depList,objects,writer);
                queryEnumerable.Add(writer.FinalStringCollection);

                if (!string.IsNullOrEmpty(options.FileName))
                {
                    WriteToFile(options, queryEnumerable);
                }
            }

            return queryEnumerable;
        }

        private void scriptMaker_ObjectScripting(object sender, ObjectScriptingEventArgs e)
        {
            this.scriptingProgress(this, new ProgressReportEventArgs(e.Current,null,1,1,e.CurrentCount,e.Total));
        }

        private ISmoDependencyDiscoverer GetDiscoverer(Server server,ScriptingOptions so,bool discoveryRequired)
        {
            SmoDependencyDiscoverer dependencyDiscoverer = new SmoDependencyDiscoverer(this.Server);
            dependencyDiscoverer.Preferences = so.GetScriptingPreferences();
            dependencyDiscoverer.Preferences.DependentObjects = discoveryRequired;
            dependencyDiscoverer.filteredUrnTypes = so.GetSmoUrnFilterForDiscovery(server).filteredTypes;
            return dependencyDiscoverer;
        }

        private void WriteToFile(ScriptingOptions options, IEnumerable<string> queryEnumerable)
        {
            StreamWriter writer = null;

            if (SqlContext.IsAvailable)
            {
                throw new Exception(ExceptionTemplates.SmoSQLCLRUnAvailable);
            }
            Stream fs = new FileStream(options.FileName, (options.AppendToFile ? FileMode.Append : FileMode.Create), FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(fs, options.Encoding);

            foreach (string sQuery in queryEnumerable)
            {
                writer.WriteLine(sQuery);
                if (!options.NoCommandTerminator)
                {
                    writer.WriteLine(Globals.Go);
                }
            }
            writer.Flush();
            writer.Close();

        }

        private void CheckConflictingOptions()
        {
            ScriptingOptions options = this.GetOptions();

            if (options.DdlBodyOnly && options.DdlHeaderOnly)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.ConflictingScriptingOptions(
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.DdlBodyOnly),
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.DdlHeaderOnly)));
            }

            // Throw error if both ScriptData and ScriptSchema are false
            // Nothing would be scripted if both these are false, thus we are flagging this as error
            //
            if (!options.ScriptData &&
                !options.ScriptSchema)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.InvalidScriptingOutput(
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.ScriptData),
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.ScriptSchema)));
            }

            // Throw an error if ScriptSchema is false and ScriptForAlter is true.
            // This is because Data will not be scripted when ScriptForAlter is true, and if ScriptSchema
            // is also false, then nothing would be scripted at all. Flagging this as an error
            //
            if (!options.ScriptSchema &&
                options.ScriptForAlter)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.ConflictingScriptingOptions(
                    Enum.GetName(typeof(EnumScriptOptions), EnumScriptOptions.ScriptSchema),
                    "ScriptForAlter"));
            }
        }      

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection Script(SqlSmoObject[] objects)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScript(objects));
        }

        public IEnumerable<string> EnumScript(SqlSmoObject[] objects)
        {
            if (null == objects)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("objects"));
            }

            Urn[] urns = new Urn[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                urns[i] = objects[i].Urn;
            }
            return Script(urns, objects);
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection Script(UrnCollection list)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScript(list));
        }

        public IEnumerable<string> EnumScript(UrnCollection list)
        {
            if (null == list)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("list"));
            }

            Urn[] urns = new Urn[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                urns[i] = list[i];
            }
            return Script(urns, null);
        }

        /// <summary>
        /// Returns a StringCollection object with the script for the passed objects. This method
        /// throws an error if ScriptData is true
        /// </summary>
        /// <exception cref="FailedOperationException">If Options.ScriptData is true</exception>
        public StringCollection Script(Urn[] urns)
        {
            if (GetOptions().ScriptData)
            {
                throw new FailedOperationException(ExceptionTemplates.ScriptDataNotSupportedByThisMethod);
            }

            return EnumerableContainer.IEnumerableToStringCollection(EnumScript(urns));
        }

        public IEnumerable<string> EnumScript(Urn[] urns)
        {
            if (null == urns)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("urns"));
            }

            return Script(urns, null);
        }

        internal IEnumerable<string> Script(Urn[] urns, SqlSmoObject[] objects)
        {
            CheckConflictingOptions();

            IEnumerable<string> result = null;
            if (GetOptions().WithDependencies)
            {
                DependencyCollection depList = new DependencyCollection();
                foreach (Urn urn in urns)
                {
                    depList.Add(new DependencyCollectionNode(urn, true, true));
                }

                // script each individual object in the list
                result = ScriptWithList(depList, objects,true);
            }
            else // ignore any dependencies
            {
                result = ScriptWithList(urns, objects);
            }
            return result;
        }

        /// <summary>
        /// list the urns  in the order in which the objects must be created respectively dropped
        /// </summary>
        /// <param name="smoObject"></param>
        /// <param name="dependencyType"></param>
        /// <returns></returns>
        public static UrnCollection EnumDependencies(SqlSmoObject smoObject, DependencyType dependencyType)
        {
            if (null == smoObject)
            {
                throw new ArgumentNullException("smoObject");
            }

            DependencyRequest rd = new DependencyRequest();
            rd.Urns = new Urn[] { smoObject.Urn };
            rd.ParentDependencies = dependencyType == DependencyType.Parents ? true : false;

            DependencyChainCollection deps = smoObject.ExecutionManager.GetDependencies(rd);

            UrnCollection urns = new UrnCollection();

            for (int i = 0; i < deps.Count; i++)
            {
                urns.Add(deps[i].Urn);
            }

            return urns;
        }

        internal StringCollection Script(SqlSmoObject sqlSmoObject)
        {
            StringCollection strcol;
            bool originalWithDependencies;
            SqlServerVersion sqlServerVersion;
            StoreAndChangeOptions(sqlSmoObject, out originalWithDependencies, out sqlServerVersion);

            try
            {                
                strcol = this.Script(new SqlSmoObject[] { sqlSmoObject });
            }
            finally
            {
                RestoreOptions(originalWithDependencies, sqlServerVersion);
            }
            return strcol;
        }

        internal IEnumerable<string> EnumScript(SqlSmoObject sqlSmoObject)
        {
            IEnumerable<string> scriptList;
            bool originalWithDependencies;
            SqlServerVersion sqlServerVersion;
            StoreAndChangeOptions(sqlSmoObject, out originalWithDependencies, out sqlServerVersion);

            try
            {
                scriptList = this.EnumScript(new SqlSmoObject[] { sqlSmoObject });
            }
            finally
            {
                RestoreOptions(originalWithDependencies, sqlServerVersion);
            }
            return scriptList;
        }

        private void RestoreOptions(bool originalWithDependencies, SqlServerVersion sqlServerVersion)
        {
            this.Options.WithDependencies = originalWithDependencies;

            //Reset the values only if the engine type is not standalone
            if (this.Options.TargetDatabaseEngineType != DatabaseEngineType.Standalone)
            {
                //Reset only if different.This will help as verDirtyOrCloudSet will not be set if user has not set it.
                if (this.Options.TargetServerVersion != sqlServerVersion)
                {
                    this.Options.TargetServerVersion = sqlServerVersion;
                }
            }
        }

        private void StoreAndChangeOptions(SqlSmoObject sqlSmoObject, out bool originalWithDependencies, out SqlServerVersion sqlServerVersion)
        {
            if (!this.Options.WithDependencies)
            {
                this.PrefetchObjects = !sqlSmoObject.InitializedForScripting;
            }

            //change dependency discovery to false for design mode
            originalWithDependencies = this.Options.WithDependencies;
            if (sqlSmoObject.State == SqlSmoState.Creating || sqlSmoObject.IsDesignMode)
            {
                this.Options.WithDependencies = false;
            }

            //change version to 105 or 120 (Sterling) for cloud depending on the value supplied
            sqlServerVersion = this.Options.TargetServerVersion;
            if (this.Options.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                if (!sqlSmoObject.IsCloudSupported)
                {
                    throw new UnsupportedEngineTypeException(ExceptionTemplates.UnsupportedEngineTypeException);
                }

                //Azure is hard-coded to stay at v12
                this.Options.TargetServerVersion = SqlServerVersion.Version120;
            }
        }
    }
}


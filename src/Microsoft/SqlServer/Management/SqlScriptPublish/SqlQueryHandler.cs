// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{

    /// <summary>
    /// Helper class for database type and database metadata query
    /// </summary>
    internal class SqlQueryHandler
    {
        private static readonly QueryInfoCollection dicTypes;
        /// <summary>
        /// Static constructor
        /// </summary>
        static SqlQueryHandler()
        {
            dicTypes = new QueryInfoCollection();
            InitializeSupportedObjectTypes();
        }

        private SqlScriptPublishModel model;
        private Dictionary<DatabaseObjectType, List<KeyValuePair<string, string>>> cache;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="model"></param>
        public SqlQueryHandler(SqlScriptPublishModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            this.model = model;
            cache = new Dictionary<DatabaseObjectType, List<KeyValuePair<string, string>>>(dicTypes.Count);
        }


#region Public methods
        /// <summary>
        /// Returns database object types currently existing in the database
        /// If the type does not have any object, we do not want to show the object type.
        /// </summary>
        /// <returns>Database type names whose objects exist</returns>
        public IEnumerable<DatabaseObjectType> GetDatabaseObjectTypes()
        {
            // populate the cache if we haven't yet
            if (cache.Count == 0)
            {
                foreach (QueryInfo info in dicTypes)
                {
                    // doing the enum loads the cache for each object type
                    // and then the Keys will only be those valid database object types
                    EnumChildrenForDatabaseObjectType(info.DatabaseObjectType);
                }
            }

            return cache.Keys;
        }

        /// <summary>
        /// Returns all children's object names and urns for the object type.
        /// </summary>
        /// <param name="objectType">Object type such as tables, views, etc</param>
        /// <returns>Object names and urns for the object type</returns>
        public IEnumerable<KeyValuePair<string, string>> EnumChildrenForDatabaseObjectType(DatabaseObjectType objectType)
        {
            List<KeyValuePair<string, string>> children = null;

            // if this objectType isn't in the cache, go get it, other just return the list
            if (!cache.TryGetValue(objectType, out children))
            {
                children = new List<KeyValuePair<string, string>>();

                QueryInfo info = dicTypes[objectType];
                Debug.Assert(info != null, "Unsupported object type:" + objectType);
                if (info == null)
                {
                    throw new SqlScriptPublishException(SR.InvalidObjectType(objectType.ToString()));
                }

                // Check to see if the object type is supported from the SQL version.
                if (ValidObjectType(info))
                {
                    SfcObjectQuery objQuery = new SfcObjectQuery(this.model.Server);
                    string fullUrn = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                    "Server/Database[@Name='{0}']/{1}",
                                                    Urn.EscapeString(this.model.DatabaseName),
                                                    info.PartialUrn);
                    SfcQueryExpression expr = new SfcQueryExpression(fullUrn);
                    DataTable dataTable = objQuery.ExecuteDataTable(expr, info.Fields, info.OrderByList);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string name = row["Name"].ToString();
                        if (info.SchemaBased)
                        {
                            name = row["Schema"].ToString() + "." + name;
                        }

                        KeyValuePair<string, string> child = new KeyValuePair<string, string>(name, row["Urn"].ToString());
                        children.Add(child);
                    }
                }

                // we only put in children we found so the Keys collection only contains the valid objects
                // if somebody keeps asking for the same db type over and over again we will keep hitting the server
                // but this is an internal class so should not happen.
                if (children.Count > 0)
                {
                    cache.Add(objectType, children);
                }
            }

            return children;
        }

        /// <summary>
        /// Returns an enumeration of DatabaseObjectType that are NOT valid for the passed
        /// in SMO cloud engine edition
        /// </summary>
        public IEnumerable<DatabaseObjectType> InvalidObjectTypesForAzure(DatabaseEngineEdition edition)
        {
            var myEdition = QueryInfo.CloudEngineEdition.SqlDatabase;
            switch (edition)
            {
                case DatabaseEngineEdition.SqlOnDemand:
                    myEdition = QueryInfo.CloudEngineEdition.OnDemand;
                    break;
                case DatabaseEngineEdition.SqlDataWarehouse:
                    myEdition = QueryInfo.CloudEngineEdition.OnDemand;
                    break;
            }

            return dicTypes.Where(d => (d.ValidEngineTypes & QueryInfo.EngineType.Cloud) == 0 ||  (d.Editions & myEdition) == 0).Select(d => d.DatabaseObjectType);
        }

#endregion

        /// <summary>
        /// Check if the passed in QueryInfo for the ObjectType is valid for the current server
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private bool ValidObjectType(QueryInfo info)
        {
            return model.Server.IsSupportedObject(info.SmoType);
        }

        /// <summary>
        /// Build up the list of object types and the engine types that they are supported on.
        /// This list is used to validate the list of objects returned from the EnumObjects call in the model
        /// </summary>
        private static void InitializeSupportedObjectTypes()
        {
            dicTypes.Add(new QueryInfo(DatabaseObjectType.Table, typeof(Table), "Table[@IsSystemObject=false()]"));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.View, typeof(View), "View[@IsSystemObject=false()]"));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.StoredProcedure, typeof(StoredProcedure), "StoredProcedure[@IsSystemObject=false()]"));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.UserDefinedFunction, typeof(UserDefinedFunction), "UserDefinedFunction[@IsSystemObject=false()]"));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.UserDefinedDataType, typeof(UserDefinedDataType), "UserDefinedDataType", true, QueryInfo.SQL80));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.User, typeof(User), "User[@IsSystemObject=false()]", false));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.Default, typeof(Default), "Default", true, QueryInfo.SQL80, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.Rule, typeof(Smo.Rule), "Rule", true, QueryInfo.SQL80, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.DatabaseRole, typeof(DatabaseRole), "Role[@IsFixedRole = false() and @Name != 'public']", false));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.ApplicationRole, typeof(ApplicationRole), "ApplicationRole", false, QueryInfo.SQL80, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.SqlAssembly, typeof(SqlAssembly), "SqlAssembly[@IsSystemObject=false()]", false, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.DdlTrigger, typeof(DatabaseDdlTrigger), "DdlTrigger[@IsSystemObject=false()]", false, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.Synonym, typeof(Synonym), "Synonym", true, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.XmlSchemaCollection, typeof(XmlSchemaCollection), "XmlSchemaCollection", false, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.Schema, typeof(Schema), "Schema[(@ID > 4 and @ID < 16384) or (@ID > 16400)]", false, QueryInfo.SQL90));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.PlanGuide, typeof(PlanGuide), "PlanGuide", false, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.UserDefinedType, typeof(UserDefinedType), "UserDefinedType", true, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.UserDefinedAggregate, typeof(UserDefinedAggregate), "UserDefinedAggregate", true, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.FullTextCatalog, typeof(FullTextCatalog), "FullTextCatalog", false, QueryInfo.SQL90, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.UserDefinedTableType, typeof(UserDefinedTableType), "UserDefinedTableType", true, QueryInfo.SQL100, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase));
            dicTypes.Add(new QueryInfo(DatabaseObjectType.SecurityPolicy, typeof(SecurityPolicy), "SecurityPolicy",true,QueryInfo.SQL130, QueryInfo.EngineType.All, QueryInfo.CloudEngineEdition.SqlDatabase | QueryInfo.CloudEngineEdition.Synapse));
        }

#region QueryInfo class

        /// <summary>
        /// A simple keyed collection
        /// </summary>
        class QueryInfoCollection : KeyedCollection<DatabaseObjectType, QueryInfo>
        {
            public QueryInfoCollection() : base() { }

            protected override DatabaseObjectType GetKeyForItem(QueryInfo info)
            {
                return info.DatabaseObjectType;
            }
        }

        /// <summary>
        /// Internal class used to hold each the Query's for each of the possible Object Types
        /// </summary>
        class QueryInfo
        {
            /// <summary>
            /// A [Flag] represenation of engine types for easy checking
            /// </summary>
            [Flags]
            public enum EngineType
            {
                None = 0,
                Singleton = 1,
                Cloud = 2,
                All = 0xffff
            }

            [Flags]
            public enum CloudEngineEdition
            {
                None = 0,
                SqlDatabase = 1,
                Synapse = 2,
                OnDemand = 4,
                All = 0xffff
            }

            // major version numbers
            public const int SQL70 = 7;
            public const int SQL80 = 8;
            public const int SQL90 = 9;
            public const int SQL100 = 10;
            public const int SQL110 = 11;
            public const int SQL120 = 12;
            public const int SQL130 = 13;
            public const int SQL140 = 14;
            public const int SQL150 = 15;
            public const int SQL160 = 16;

            private string partialUrn;
            private bool schemaBased;
            private int minimumSingletonVersion = SQL80;
            private DatabaseObjectType dbObjectType;
            private EngineType validTypes;
            
            public QueryInfo(DatabaseObjectType type, Type smoType, string urn)
                : this(type, smoType, urn, true, SQL80, EngineType.All, CloudEngineEdition.All)
            {
            }

            public QueryInfo(DatabaseObjectType type, Type smoType, string urn, bool schemaBased)
                : this(type, smoType, urn, schemaBased, SQL80, EngineType.All, CloudEngineEdition.All)
            {
            }

            public QueryInfo(DatabaseObjectType type, Type smoType, string urn, bool schemaBased, int minimumSingletonVersion)
                : this(type, smoType, urn, schemaBased, minimumSingletonVersion, EngineType.All, CloudEngineEdition.All)
            {
            }

            public QueryInfo(DatabaseObjectType type, Type smoType, string urn, bool schemaBased, int minimumSingletonVersion, EngineType types, CloudEngineEdition editions)
            {
                dbObjectType = type;
                partialUrn = urn;
                this.schemaBased = schemaBased;
                this.minimumSingletonVersion = minimumSingletonVersion;
                validTypes = types;
                SmoType = smoType;
                Editions = editions;
            }

            public QueryInfo(DatabaseObjectType type, Type smoType, string urn, bool schemaBased, int minimumSingletonVersion, CloudEngineEdition editions)
            {
                dbObjectType = type;
                partialUrn = urn;
                this.schemaBased = schemaBased;
                this.minimumSingletonVersion = minimumSingletonVersion;
                validTypes = EngineType.All;
                SmoType = smoType;
                Editions = editions;
            }

            /// <summary>
            /// When supported by Azure engine type, which editions it's supported for
            /// </summary>
            public CloudEngineEdition Editions
            {
                get;
                private set;
            }

            /// <summary>
            /// The Type to pass to Server.IsSupportedObject
            /// </summary>
            public Type SmoType
            {
                get;
                private set;
            }

            public string PartialUrn
            {
                get { return this.partialUrn; }
            }

            public bool SchemaBased
            {
                get { return this.schemaBased; }
            }


            public DatabaseObjectType DatabaseObjectType
            {
                get { return this.dbObjectType; }
            }

            public EngineType ValidEngineTypes
            {
                get { return this.validTypes; }
            }

            public OrderBy[] OrderByList
            {
                get
                {
                    if (this.schemaBased)
                    {
                        return new OrderBy[] { new OrderBy("Schema", OrderBy.Direction.Asc), new OrderBy("Name", OrderBy.Direction.Asc) };
                    }
                    else
                    {
                        return new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                    }
                }
            }

            public string[] Fields
            {
                get
                {
                    if (this.schemaBased)
                    {
                        return new string[] { "Urn", "Name", "Schema" };
                    }
                    else
                    {
                        return new string[] { "Urn", "Name" };
                    }
                }
            }

        }
#endregion
    }
}

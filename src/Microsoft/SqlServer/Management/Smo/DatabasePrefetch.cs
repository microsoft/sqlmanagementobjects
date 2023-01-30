// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Object prefetching interface
    /// </summary>
    internal interface IDatabasePrefetch
    {
        /// <summary>
        /// Method to prefetch objects before yielding them
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IEnumerable<Urn> PrefetchObjects(IEnumerable<Urn> input);

        /// <summary>
        /// creating dictionary to store creating state objects
        /// </summary>
        CreatingObjectDictionary creatingDictionary { get; set; }
    }

    /// <summary>
    /// Base class which does the batching of input and lets subclass to use actual method to prefetch
    /// </summary>
    internal abstract class DatabasePrefetchBase : IDatabasePrefetch
    {
        protected Database Database;
        protected ScriptingPreferences scriptingPreferences;

        /// <summary>
        /// urn types which are not going to be discovered in discovery
        /// </summary>
        protected HashSet<UrnTypeKey> filteredTypes;

        /// <summary>
        /// urn types which support prefetching
        /// </summary>
        protected HashSet<string> prefetchableTypes;

        /// <summary>
        /// urn types which support batched prefetching
        /// </summary>
        protected Dictionary<string, List<Urn>> batchedPrefetchDictionary;

        public DatabasePrefetchBase(Database db, ScriptingPreferences scriptingPreferences, HashSet<UrnTypeKey> filteredTypes)
        {
            this.Database = db;
            this.scriptingPreferences = scriptingPreferences;
            this.filteredTypes = filteredTypes;

            this.InitializePrefetchableTypes();
            this.InitializeBatchedPrefetchDictionary();
        }

        private void InitializeObjectCollection(string type)
        {
            if (type.Equals(Table.UrnSuffix) && !this.Database.Tables.initialized)
            {
                this.Database.InitChildLevel(Table.UrnSuffix, this.scriptingPreferences, false);
            }
            else if (type.Equals(View.UrnSuffix) && !this.Database.Views.initialized)
            {
                this.Database.InitChildLevel(View.UrnSuffix, this.scriptingPreferences, false);
            }
        }

        /// <summary>
        /// Method to set batched prefetched types
        /// </summary>
        protected abstract void InitializeBatchedPrefetchDictionary();

        /// <summary>
        /// Method to set prefetchable types
        /// </summary>
        protected abstract void InitializePrefetchableTypes();

        public CreatingObjectDictionary creatingDictionary { get; set; }

        /// <summary>
        /// maximum batch size for prefetching
        /// </summary>
        protected int batchSize;

        /// <summary>
        /// Method to bucketize input and then batched prefetching for types for which batched prefetching need to be done
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual IEnumerable<Urn> PrefetchObjects(IEnumerable<Urn> input)
        {
            //go through all urns and batch urns
            foreach (Urn item in input)
            {
                if ((this.prefetchableTypes.Contains(item.Type)) &&
                    (this.Database.Parent.CompareUrn(this.Database.Urn, item.Parent) == 0) &&
                    (!this.creatingDictionary.ContainsKey(item)))
                {
                    List<Urn> urnList;
                    if (this.batchedPrefetchDictionary.TryGetValue(item.Type, out urnList))
                    {
                        this.InitializeObjectCollection(item.Type);
                        this.AddUrn(item);
                        urnList.Add(item);
                    }
                    else
                    {
                        //Prefetch all objects of the type and remove it
                        this.PrefetchAllObjects(item.Type);
                        this.prefetchableTypes.Remove(item.Type);
                        yield return item;
                    }
                }
                else if (item.Type == "Database")
                {
                    Database.PrefetchScriptingOnlyChildren(scriptingPreferences);
                }
                else
                {
                    //No prefetching needed
                    yield return item;
                }
            }

            // #1236132 - Prefetched urns are returned for discovery. method signature changed.
            foreach (Urn urn in this.PrePrefetchBatches())
            {
                yield return urn;
            }

            //prefetch all types for which no. of batches required is not greater than 1
            foreach (KeyValuePair<string, List<Urn>> item in this.batchedPrefetchDictionary.SkipWhile(kvp => kvp.Value.Count > this.batchSize))
            {
                if (item.Value.Count > 0)
                {
                    HashSet<Urn> urnBatch = new HashSet<Urn>(item.Value);
                    this.PrefetchBatch(item.Key, urnBatch, 1, 1);

                    foreach (Urn urn in item.Value)
                    {
                        yield return urn;
                    }
                    this.PostPrefetchBatch(item.Key, urnBatch, 1, 1);
                    item.Value.Clear();
                }
            }

            //prefetch all remaining types for which no. of batches required is  greater than 1
            foreach (KeyValuePair<string, List<Urn>> item in this.batchedPrefetchDictionary)
            {
                int batchCount = 0;
                int totalBatchCount = totalBatchCount = item.Value.Count / this.batchSize;
                if (item.Value.Count % this.batchSize != 0)
                {
                    totalBatchCount++;
                }

                HashSet<Urn> urnBatch = new HashSet<Urn>();
                foreach (Urn table in item.Value)
                {

                    if (urnBatch.Count >= this.batchSize)
                    {
                        this.PrefetchBatch(item.Key, urnBatch, ++batchCount, totalBatchCount);
                        foreach (Urn batchedtable in urnBatch)
                        {
                            yield return batchedtable;
                        }
                        this.PostPrefetchBatch(item.Key, urnBatch, batchCount, totalBatchCount);
                        urnBatch.Clear();
                    }

                    urnBatch.Add(table);
                }

                //last batch
                if (urnBatch.Count > 0)
                {
                    //partial prefetch
                    this.PrefetchBatch(item.Key, urnBatch, ++batchCount, totalBatchCount);
                    foreach (Urn batchedtable in urnBatch)
                    {
                        yield return batchedtable;
                    }
                    this.PostPrefetchBatch(item.Key, urnBatch, batchCount, totalBatchCount);
                }
            }
        }

        /// <summary>
        /// This method is to be used to do tasks before any batched prefetching occurs
        /// #1236132 - Prefetched urns are returned for discovery. method signature changed.
        /// default to return Empty.
        /// </summary>
        protected virtual IEnumerable<Urn> PrePrefetchBatches()
        {
            return Enumerable.Empty<Urn>();
        }

        /// <summary>
        /// Method to do any post prefetch thing
        /// </summary>
        /// <param name="urnType"></param>
        /// <param name="urnBatch"></param>
        /// <param name="currentBatchCount"></param>
        /// <param name="totalBatchCount"></param>
        protected virtual void PostPrefetchBatch(string urnType, HashSet<Urn> urnBatch, int currentBatchCount, int totalBatchCount)
        {
            return;
        }

        /// <summary>
        /// Method to implement prefetching algo for a batch
        /// </summary>
        /// <param name="urnType"></param>
        /// <param name="urnBatch"></param>
        /// <param name="currentBatchCount"></param>
        /// <param name="totalBatchCount"></param>
        protected virtual void PrefetchBatch(string urnType, HashSet<Urn> urnBatch, int currentBatchCount, int totalBatchCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to store any information before adding in list
        /// </summary>
        /// <param name="item"></param>
        protected virtual void AddUrn(Urn item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to  Database's prefetch method based on type
        /// NOTE: For Transfer to include an object in this list, update two other places.
        /// 1. GswDatabasePrefetch.InitializePrefetchableTypes in this file
        /// 2. Transfer.GetDiscoverableObjects in Transferbase.cs
        /// If objects are not properly pre-fetched by either Transfer or by this class, performance
        /// of the Transfer will be impacted by multiple queries to populate single objects.
        /// </summary>
        /// <param name="urnType"></param>
        protected virtual void PrefetchAllObjects(string urnType)
        {
            switch (urnType)
            {
                case "StoredProcedure":
                    // Prefetches all stored procs
                    Database.PrefetchStoredProcedures(this.scriptingPreferences);
                    break;

                case "User":
                    // Prefetches all users
                    Database.PrefetchUsers(this.scriptingPreferences);
                    break;

                case "DatabaseRole":
                    // Prefetches all DB Roles
                    Database.PrefetchDatabaseRoles(this.scriptingPreferences);
                    break;

                case "Default":
                    // Prefetches all defaults
                    Database.PrefetchDefaults(this.scriptingPreferences);
                    break;

                case "Rule":
                    // Prefetches all rules
                    Database.PrefetchRules(this.scriptingPreferences);
                    break;


                case "UserDefinedFunction":
                    // Prefetches all UDFs
                    Database.PrefetchUserDefinedFunctions(this.scriptingPreferences);
                    break;


                case "ExtendedStoredProcedure":
                    // Prefetches all extended SPs.
                    Database.PrefetchExtendedStoredProcedures(this.scriptingPreferences);
                    break;

                case "UserDefinedType":
                    // Prefetches all UDTs
                    Database.PrefetchUserDefinedTypes(this.scriptingPreferences);
                    break;

                case "UserDefinedTableType":
                    // Prefetches all UDTTs
                    Database.PrefetchUserDefinedTableTypes(this.scriptingPreferences);
                    break;

                case "UserDefinedAggregate":
                    // Prefetches all aggregates
                    Database.PrefetchUserDefinedAggregates(this.scriptingPreferences);
                    break;

                case "UserDefinedDataType":
                    // Prefetches all UDDTs
                    Database.PrefetchUDDT(this.scriptingPreferences);
                    break;

                case "XmlSchemaCollection":
                    // Prefetches all schema collections
                    Database.PrefetchXmlSchemaCollections(this.scriptingPreferences);
                    break;

                case "SqlAssembly":
                    // Prefetches all SQL assemblies
                    Database.PrefetchSqlAssemblies(this.scriptingPreferences);
                    break;

                case "Schema":
                    // Prefetches all schemas
                    Database.PrefetchSchemas(this.scriptingPreferences);
                    break;

                case "PartitionScheme":
                    // Prefetches all partition schemes
                    Database.PrefetchPartitionSchemes(this.scriptingPreferences);
                    break;

                case "PartitionFunction":
                    // Prefetches all partition functions
                    Database.PrefetchPartitionFunctions(this.scriptingPreferences);
                    break;

                case "Table":
                    // Prefetches all partition functions
                    Database.PrefetchTables(this.scriptingPreferences);
                    break;

                case "View":
                    // Prefetches all partition functions
                    Database.PrefetchViews(this.scriptingPreferences);
                    break;

                case nameof(ColumnEncryptionKey):
                    Database.PrefetchColumnEncryptionKey(scriptingPreferences);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Get table children list utilizing the filtering information
        /// </summary>
        /// <returns></returns>
        protected List<string> GetTablePrefetchList()
        {
            List<string> tablePrefetchingList = new List<string>();

            tablePrefetchingList.Add(string.Empty);
            if (this.Database.IsSupportedObject<Column>(this.scriptingPreferences))
            {
                //Column and children
                tablePrefetchingList.Add("/Column");
                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/Column/ExtendedProperty");
                }
                if (this.scriptingPreferences.IncludeScripts.Permissions)
                {
                    tablePrefetchingList.Add("/Column/Permission");
                }

                //Column default and children
                if (this.Database.IsSupportedObject<Default>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/Column/Default");


                    if ((!this.filteredTypes.Contains(new UrnTypeKey("DefaultColumn")))
                        && (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix))
                        && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences)))
                    {
                        tablePrefetchingList.Add("/Column/Default/ExtendedProperty");
                    }
                }
            }

            if (this.Database.IsSupportedObject<Index>(this.scriptingPreferences))
            {
                tablePrefetchingList.Add("/Index");
                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/Index/ExtendedProperty");
                }

                if (this.Database.IsSupportedObject<IndexedColumn>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/Index/IndexedColumn");
                }
            }

            if (this.Database.IsSupportedObject<FullTextIndex>(this.scriptingPreferences))
            {
                tablePrefetchingList.Add("/FullTextIndex");
                if (this.Database.IsSupportedObject<FullTextIndexColumn>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/FullTextIndex/FullTextIndexColumn");
                }
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
            {
                tablePrefetchingList.Add("/ExtendedProperty");
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(Check.UrnSuffix)))
            {
                tablePrefetchingList.Add("/Check");

                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/Check/ExtendedProperty");
                }
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(ForeignKey.UrnSuffix)) && this.Database.IsSupportedObject<ForeignKey>(this.scriptingPreferences))
            {
                tablePrefetchingList.Add("/ForeignKey");
                tablePrefetchingList.Add("/ForeignKey/Column");

                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/ForeignKey/ExtendedProperty");
                }
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(Trigger.UrnSuffix)) && this.Database.IsSupportedObject<Trigger>(this.scriptingPreferences))
            {
                tablePrefetchingList.Add("/Trigger");

                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    tablePrefetchingList.Add("/Trigger/ExtendedProperty");
                }
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(Statistic.UrnSuffix)) && this.Database.IsSupportedObject<Statistic>(this.scriptingPreferences))
            {
                tablePrefetchingList.Add("/Statistic");
                tablePrefetchingList.Add("/Statistic/Column");
            }

            if (this.scriptingPreferences.IncludeScripts.Permissions)
            {
                tablePrefetchingList.Add("/Permission");
            }

            return tablePrefetchingList;
        }

        /// <summary>
        /// Gives view children list utilizing the filtering information
        /// </summary>
        /// <returns></returns>
        protected List<string> GetViewPrefetchList()
        {
            List<string> viewPrefetchingList = new List<string>();

            viewPrefetchingList.Add(string.Empty);
            if (this.Database.IsSupportedObject<Column>(this.scriptingPreferences))
            {
                //Column and children
                viewPrefetchingList.Add("/Column");
                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    viewPrefetchingList.Add("/Column/ExtendedProperty");
                }
                if (this.scriptingPreferences.IncludeScripts.Permissions)
                {
                    viewPrefetchingList.Add("/Column/Permission");
                }

                //Column default and children
                if (this.Database.IsSupportedObject<Default>(this.scriptingPreferences))
                {
                    viewPrefetchingList.Add("/Column/Default");


                    if ((!this.filteredTypes.Contains(new UrnTypeKey("DefaultColumn")))
                        && (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix))
                        && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences)))
                    {
                        viewPrefetchingList.Add("/Column/Default/ExtendedProperty");
                    }
                }
            }

            if (this.Database.IsSupportedObject<Index>(this.scriptingPreferences))
            {
                viewPrefetchingList.Add("/Index");
                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    viewPrefetchingList.Add("/Index/ExtendedProperty");
                }

                if (this.Database.IsSupportedObject<IndexedColumn>(this.scriptingPreferences))
                {
                    viewPrefetchingList.Add("/Index/IndexedColumn");
                }
            }

            if (this.Database.ServerVersion.Major > 8 && this.Database.IsSupportedObject<FullTextIndex>(this.scriptingPreferences))
            {
                viewPrefetchingList.Add("/FullTextIndex");
                viewPrefetchingList.Add("/FullTextIndex/FullTextIndexColumn");
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
            {
                viewPrefetchingList.Add("/ExtendedProperty");
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(Trigger.UrnSuffix)) && this.Database.IsSupportedObject<Trigger>(this.scriptingPreferences))
            {
                viewPrefetchingList.Add("/Trigger");

                if (!this.filteredTypes.Contains(new UrnTypeKey(ExtendedProperty.UrnSuffix)) && this.Database.IsSupportedObject<ExtendedProperty>(this.scriptingPreferences))
                {
                    viewPrefetchingList.Add("/Trigger/ExtendedProperty");
                }
            }

            if (!this.filteredTypes.Contains(new UrnTypeKey(Statistic.UrnSuffix)) && this.Database.IsSupportedObject<Statistic>(this.scriptingPreferences))
            {
                viewPrefetchingList.Add("/Statistic");
                viewPrefetchingList.Add("/Statistic/Column");
            }

            if (this.scriptingPreferences.IncludeScripts.Permissions)
            {
                viewPrefetchingList.Add("/Permission");
            }

            return viewPrefetchingList;
        }
    }

    /// <summary>
    /// Prefetching class employed by GenerateScriptsWizard
    /// </summary>
    internal class GswDatabasePrefetch : DefaultDatabasePrefetch, IDatabasePrefetch
    {
        /// <summary>
        /// Maximum batch size for prefetching
        /// </summary>
        private static int MAX_BATCH_SIZE = 9000;

        private PrefetchBatchEventHandler prefetchBatchEvent;

        /// <summary>
        /// Event to be fired after every batch gets prefetched
        /// </summary>
        public event PrefetchBatchEventHandler PrefetchBatchEvent
        {
            add
            {
                prefetchBatchEvent += value;
            }
            remove
            {
                prefetchBatchEvent -= value;
            }
        }

        public GswDatabasePrefetch(Database db, ScriptingPreferences scriptingPreferences, HashSet<UrnTypeKey> filteredTypes)
            : base(db, scriptingPreferences, filteredTypes)
        {
            this.batchSize = MAX_BATCH_SIZE;
        }

        protected override void InitializeBatchedPrefetchDictionary()
        {
            //initialize batched prefetch types
            this.batchedPrefetchDictionary = new Dictionary<string, List<Urn>>();

            List<string> prefetchableTypes = new List<string>(){
                    "Table",
                    "View"
                };

            foreach (var item in prefetchableTypes)
            {
                this.batchedPrefetchDictionary.Add(item, new List<Urn>());
            }
        }

        protected override void InitializePrefetchableTypes()
        {
            //initialize prefetchable types
            this.prefetchableTypes = new HashSet<string>();

            List<string> prefetchableTypes = new List<string>(){
                    "StoredProcedure",
                    "User",
                    "DatabaseScopedConfiguration",
                    "DatabaseRole",
                    "Default",
                    "Rule",
                    "UserDefinedFunction",
                    "ExtendedStoredProcedure",
                    "UserDefinedType",
                    "UserDefinedTableType",
                    "UserDefinedAggregate",
                    "UserDefinedDataType",
                    "XmlSchemaCollection",
                    "SqlAssembly",
                    "Schema",
                    "PartitionScheme",
                    "PartitionFunction",
                    "Table",
                    "View",
                    nameof(ColumnEncryptionKey),
                };

            foreach (var item in prefetchableTypes)
            {
                this.prefetchableTypes.Add(item);
            }
        }

        /// <summary>
        /// #1236132 - Prefetched urns are returned for discovery. method signature changed.
        /// default to return Empty.
        /// </summary>
        protected override IEnumerable<Urn> PrePrefetchBatches()
        {
            return Enumerable.Empty<Urn>();
        }

        protected override void PostPrefetchBatch(string urnType, HashSet<Urn> urnBatch, int currentBatchCount, int totalBatchCount)
        {
            if (totalBatchCount > 1)
            {
                // Clear all existing Tables or view
                // if more types need to be prefetch following should be replaced by switch
                if (urnType.Equals("Table"))
                {
                    Database.Tables.Clear();
                }
                else
                {
                    Database.Views.Clear();
                }
            }
        }

        protected override void PrefetchBatch(string urnType, HashSet<Urn> urnBatch, int currentBatchCount, int totalBatchCount)
        {
            base.PrefetchBatch(urnType, urnBatch, currentBatchCount, totalBatchCount);

            if (this.prefetchBatchEvent != null)
            {
                this.prefetchBatchEvent(this, new PrefetchBatchEventArgs(urnType, urnBatch.Count, currentBatchCount, totalBatchCount));
            }

        }

        /// <summary>
        /// Calls database' method for prefetching object hierarchy
        /// </summary>
        /// <param name="idFilter"></param>
        /// <param name="initializeCollectionsFilter"></param>
        /// <param name="type"></param>
        /// <param name="prefetchingList"></param>
        protected override void PrefetchUsingIN(string idFilter, string initializeCollectionsFilter, string type, IEnumerable<string> prefetchingList)
        {
            base.PrefetchUsingIN(idFilter, string.Empty, type, prefetchingList);
        }

        protected override List<string> GetChildrenList(string urnType)
        {
            // Clear all existing Tables or view
            // if more types need to be prefetch following should be replaced by switch
            if (urnType.Equals("Table"))
            {
                Database.Tables.Clear();
            }
            else
            {
                Database.Views.Clear();
            }

            return base.GetChildrenList(urnType);
        }
    }

    internal class DefaultDatabasePrefetch : DatabasePrefetchBase, IDatabasePrefetch
    {
        /// <summary>
        /// Maximum batch size for prefetching
        /// </summary>
        private static int MAX_BATCH_SIZE = 9000;

        /// <summary>
        /// Dictionary to stored id which is used while generating filters
        /// </summary>
        private Dictionary<Urn, int> idDictionary;

        public DefaultDatabasePrefetch(Database db, ScriptingPreferences scriptingPreferences, HashSet<UrnTypeKey> filteredTypes)
            : base(db, scriptingPreferences, filteredTypes)
        {
            this.batchSize = MAX_BATCH_SIZE;
            this.idDictionary = new Dictionary<Urn, int>();
        }

        private void InitializeTableSets(HashSet<Urn> userTables, HashSet<Urn> systemTables)
        {
            this.GetIsSystemObjectForCollection<Table>("Table");
            this.GetUserAndSystemObjects<Table>(this.Database.Tables, userTables, systemTables);
        }

        private void InitializeViewSets(HashSet<Urn> userViews, HashSet<Urn> systemViews)
        {
            this.GetIsSystemObjectForCollection<View>("View");
            this.GetUserAndSystemObjects<View>(this.Database.Views, userViews, systemViews);
        }

        private void GetIsSystemObjectForCollection<T>(string urnSuffix)
            where T : SqlSmoObject
        {
            StringCollection origInitFields = this.Database.GetServerObject().GetDefaultInitFields(typeof(T), this.Database.DatabaseEngineEdition);
            this.Database.GetServerObject().SetDefaultInitFields(typeof(T),  this.Database.DatabaseEngineEdition, "IsSystemObject");
            this.Database.InitChildLevel(urnSuffix, this.scriptingPreferences, false);
            this.Database.GetServerObject().SetDefaultInitFields(typeof(T), origInitFields, this.Database.DatabaseEngineEdition);
        }

        private void GetUserAndSystemObjects<T>(SmoCollectionBase collection, ICollection<Urn> userObjects, ICollection<Urn> systemObjects)
           where T : SqlSmoObject
        {
            foreach (T item in collection)
            {
                if (!item.GetPropValueOptional<bool>("IsSystemObject", false))
                {
                    userObjects.Add(item.Urn);
                }
                else
                {
                    systemObjects.Add(item.Urn);
                }
            }
        }

        protected override void InitializeBatchedPrefetchDictionary()
        {
            //initialize batched prefetch types
            this.batchedPrefetchDictionary = new Dictionary<string, List<Urn>>();

            List<string> prefetchableTypes = new List<string>(){
                    "Table",
                    "View"
                };

            foreach (var item in prefetchableTypes)
            {
                this.batchedPrefetchDictionary.Add(item, new List<Urn>());
            }
        }

        protected override void InitializePrefetchableTypes()
        {
            //initialize prefetchable types
            this.prefetchableTypes = new HashSet<string>();

            List<string> prefetchableTypes = new List<string>(){
                    "Table",
                    "View"
                };

            foreach (var item in prefetchableTypes)
            {
                this.prefetchableTypes.Add(item);
            }
        }

        /// <summary>
        /// VSTS #1236132 - Prefetched urns must be returned for discovery. method signature changed.
        /// </summary>
        protected override IEnumerable<Urn> PrePrefetchBatches()
        {
            if (this.batchedPrefetchDictionary["Table"].Count > 1)
            {
                HashSet<Urn> systemTables = new HashSet<Urn>();
                HashSet<Urn> userTables = new HashSet<Urn>();

                this.InitializeTableSets(userTables, systemTables);

                string filter;
                if (this.IsAllObjectPrefetchPossible(this.batchedPrefetchDictionary["Table"], userTables, systemTables, out filter))
                {
                    this.PrefetchUsingIN(filter, string.Empty, "Table", this.GetTablePrefetchList());
                    foreach (Urn urn in batchedPrefetchDictionary["Table"])
                    {
                        yield return urn;
                    }
                    this.batchedPrefetchDictionary["Table"].Clear();
                }
            }

            if (this.batchedPrefetchDictionary["View"].Count > 1)
            {
                HashSet<Urn> systemViews = new HashSet<Urn>();
                HashSet<Urn> userViews = new HashSet<Urn>();
                this.InitializeViewSets(userViews, systemViews);

                string filter;
                if (this.IsAllObjectPrefetchPossible(this.batchedPrefetchDictionary["View"], userViews, systemViews, out filter))
                {
                    this.PrefetchUsingIN(filter, string.Empty, "View", this.GetViewPrefetchList());
                    foreach (Urn urn in batchedPrefetchDictionary["View"])
                    {
                        yield return urn;
                    }
                    this.batchedPrefetchDictionary["View"].Clear();
                }
            }
        }

        private bool IsAllObjectPrefetchPossible(List<Urn> inputList, HashSet<Urn> userObjectSet, HashSet<Urn> systemObjectSet, out string filter)
        {
            bool noSytemObject = true;
            filter = string.Empty;

            //Loop through input list
            foreach (Urn item in inputList)
            {
                userObjectSet.Remove(item);

                if (systemObjectSet.Remove(item))
                {
                    noSytemObject = false;
                }
            }

            if (userObjectSet.Count > 0)
            {
                //are some user object missing from inputlist if yes return false
                return false;
            }
            else if (!this.scriptingPreferences.SystemObjects || noSytemObject)
            {
                //no user object is missing and system objects are not present or will be filtered out so prefetch all user object
                filter = "[@IsSystemObject=false()]";
                return true;
            }
            else if (systemObjectSet.Count > 0)
            {
                //there are some system object in the list
                return false;
            }
            else
            {
                //all user and system objects are in the input list
                return true;
            }
        }

        /// <summary>
        /// Prefetches given set of objects and their children
        /// </summary>
        /// <param name="urnType"></param>
        /// <param name="urnBatch"></param>
        /// <param name="currentBatchCount"></param>
        /// <param name="totalBatchCount"></param>
        protected override void PrefetchBatch(string urnType, HashSet<Urn> urnBatch, int currentBatchCount, int totalBatchCount)
        {
            string idFilter = GetIdFilter(urnBatch);

            List<string> prefetchingList;

            prefetchingList = GetChildrenList(urnType);

            PrefetchUsingIN(idFilter, idFilter, urnType, prefetchingList);
        }

        /// <summary>
        /// Gets the list of children to be prefetched like for table table/column,table/index etc
        /// </summary>
        /// <param name="urnType">type</param>
        /// <returns></returns>
        protected virtual List<string> GetChildrenList(string urnType)
        {
            List<string> prefetchingList;
            // if more types need to be prefetch following should be replaced by switch
            if (urnType.Equals("Table"))
            {
                prefetchingList = this.GetTablePrefetchList();
            }
            else
            {
                prefetchingList = this.GetViewPrefetchList();
            }
            return prefetchingList;
        }

        /// <summary>
        /// Get ID based filter. If there is only one urn in the batch then we make the name and schema or only name urn so that the query is parameterized.
        /// </summary>
        /// <param name="urnBatch"></param>
        /// <returns></returns>
        private string GetIdFilter(HashSet<Urn> urnBatch)
        {
            if (urnBatch.Count == 1)
            {
                Urn item = urnBatch.First();
                SqlSmoObject sqlSmoObject = this.Database.Parent.GetSmoObject(item);
                NamedSmoObject namedSmoObject = sqlSmoObject as NamedSmoObject;
                ScriptSchemaObjectBase schemaSmoObject = sqlSmoObject as ScriptSchemaObjectBase;

                if (namedSmoObject != null)
                {
                    //Converting the Named Smo Object name into format acceptable by XPath query
                    string namedSmoObjectName = SqlSmoObject.SqlString(namedSmoObject.Name) ;
                    if (schemaSmoObject != null)
                    {
                        //Converting the schema Smo Object name into format acceptable by XPath query
                        string schemaSmoObjectName = SqlSmoObject.SqlString(schemaSmoObject.Schema);
                        return string.Format(SmoApplication.DefaultCulture, "[@Name='{0}' and @Schema='{1}']", namedSmoObjectName, schemaSmoObjectName);
                    }

                    return string.Format(SmoApplication.DefaultCulture, "[@Name='{0}']", namedSmoObjectName);
                }
            }

            return string.Format(SmoApplication.DefaultCulture, "[in(@ID, '{0}')]", this.GetFilteringids(urnBatch));
        }

        /// <summary>
        /// Helper to construct filter for batch
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        private string GetFilteringids(HashSet<Urn> objects)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in objects)
            {
                int ID = idDictionary[item];
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0},", ID);
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calls database' method for prefetching object hierarchy
        /// </summary>
        /// <param name="idFilter"></param>
        /// <param name="initializeCollectionsFilter"></param>
        /// <param name="type"></param>
        /// <param name="prefetchingList"></param>
        protected virtual void PrefetchUsingIN(string idFilter, string initializeCollectionsFilter, string type, IEnumerable<string> prefetchingList)
        {
            foreach (string subQuery in prefetchingList)
            {
                string levelFilter = string.Format(SmoApplication.DefaultCulture, "{0}/{1}{2}{3}", this.Database.Urn, type, idFilter, subQuery);
                string levelUrn = string.Format(SmoApplication.DefaultCulture, "{0}/{1}{2}{3}", this.Database.Urn, type, initializeCollectionsFilter, subQuery);

                Database.Parent.InitQueryUrns(new Urn(levelFilter), null, null, null, this.scriptingPreferences, new Urn(levelUrn), Database.DatabaseEngineEdition);
            }
        }

        protected override void AddUrn(Urn item)
        {
            int ID = (int)this.Database.Parent.GetSmoObject(item).Properties.GetValueWithNullReplacement("ID");
            this.idDictionary.Add(item, ID);
        }
    }

    /// <summary>
    /// Delegate to handle batched prefetching progress
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal delegate void PrefetchBatchEventHandler(object sender, PrefetchBatchEventArgs e);

    /// <summary>
    /// Batched prefetching progress event argument class
    /// </summary>
    internal class PrefetchBatchEventArgs : EventArgs
    {
        internal PrefetchBatchEventArgs(string urnType, int batchSize, int currentBatchCount, int totalBatchCount)
        {
            this.UrnType = urnType;
            this.BatchSize = batchSize;
            this.CurrentBatchCount = currentBatchCount;
            this.TotalBatchCount = totalBatchCount;
        }

        /// <summary>
        /// Type of object being prefetched
        /// </summary>
        public string UrnType { get; private set; }

        /// <summary>
        /// Total objects in current batch
        /// </summary>
        public int BatchSize { get; private set; }

        /// <summary>
        /// Batch count of current batch
        /// </summary>
        public int CurrentBatchCount { get; private set; }

        /// <summary>
        /// Total batches of this type to be used while prefetching
        /// </summary>
        public int TotalBatchCount { get; private set; }
    }
}

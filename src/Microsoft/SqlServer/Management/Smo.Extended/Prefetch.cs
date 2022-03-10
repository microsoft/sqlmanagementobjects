// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    #region Prefetch

    /// <summary>
    /// PrefetchEventArgs is used as an argument for
    /// BeforePrefetch and AfterPrefetch events
    /// </summary>
    internal class PrefetchEventArgs : EventArgs
    {
        private string smoType;
        private string filterConditionText;

        /// <summary>
        /// Constructor
        /// </summary>
        internal PrefetchEventArgs(string smoType, string filterConditionText)
        {
            this.smoType = smoType;
            this.filterConditionText = filterConditionText;
        }

        /// <summary>
        /// SMO Type being pre-fetched
        /// </summary>
        internal string Type
        {
            get { return this.smoType; }
        }

        /// <summary>
        /// XPATH filter condition (used as filter in XPATH at the level corresponding to
        /// the current SMO Type)
        /// </summary>
        internal string FilterConditionText
        {
            get { return this.filterConditionText; }
        }
    }

    /// <summary>
    /// This class is responsible for batching prefetching of SMO objects for Database scripting.
    /// 
    /// The class takes a list of URNs produced by Transfer.GetObjects and while enumerating through URNs looks ahead for 
    /// URNs of the same type, groups them together in batches of manageable size and pre-fetches
    /// all needed objects in memory. It's functionality currently overlaps with functionality of Transfer, however
    /// it does prefetching in a more optimal way that ensures that memory consumption never goes
    /// too high.
    /// 
    /// At the moment it isn't used directly from Transfer class. The only user of this
    /// class is Generate Script Wizard.
    /// </summary>
    internal class Prefetch
    {
        private Database database;
        private ScriptingPreferences scriptingPreferences;

        internal delegate void PrefetchEventHandler(object sender, PrefetchEventArgs e);

        /// <summary>
        /// Constructor
        /// </summary>
        internal Prefetch(Database database, ScriptingOptions scriptingOptions) 
        {
            this.database = database;
            this.scriptingPreferences = scriptingOptions.GetScriptingPreferences();
        }

        /// <summary>
        /// Current Database
        /// </summary>
        internal Database Database
        {
            get { return this.database; }
        }

        /// <summary>
        /// Current Server
        /// </summary>
        internal Server Server
        {
            get { return this.database.Parent; }
        }

        /// <summary>
        /// Current Scripting Options
        /// </summary>
        internal ScriptingPreferences ScriptingPreferences
        {
            get { return this.scriptingPreferences; }
        }

        /// <summary>
        /// Enumerates Urns while prefetching corresponsing portions of the database into memory
        /// Most of the types gets prefetched entirely when they first appear in the list
        /// Tables and Views are further sub-divided into batches, typically 5000 objects at a time
        /// This function purges a previous batch before bringing into memory - this is done to cap 
        /// the total memory consumption.
        /// </summary>
        public IEnumerable<Urn> EnumerateObjectUrns(IList<Urn> urns)
        {
            return new UrnIterator(this, urns);
        }

        // Raised before each batch gets processed
        internal event PrefetchEventHandler BeforePrefetch;
        // Raised after each batch gets processed
        internal event PrefetchEventHandler AfterPrefetch;

        private void OnBeforePrefetchObjects(BatchBlock block)
        {
            if (BeforePrefetch != null)
            {
                BeforePrefetch(this, new PrefetchEventArgs(block.TypeName, block.FilterConditionText));
            }
        }

        private void OnAfterPrefetchObjects(BatchBlock block)
        {
            if (AfterPrefetch != null)
            {
                AfterPrefetch(this, new PrefetchEventArgs(block.TypeName, block.FilterConditionText));
            }
        }

        /// <summary>
        /// Factory method for creating prefetch blocks
        /// </summary>
        private BatchBlock CreateBatchBlock(string typeName)
        {
            switch (typeName)
            {
                case "Database":
                    // Batch block not needed
                    return null;

                case "Table":
                    return new LimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Clear all existing Tables
                        Database.Tables.Clear();
                        // Prefetch next batch of Tables
                        string levelUrn = Database.Urn + "/Table";
                        string levelFilter = levelUrn + "[" + batchBlock.FilterConditionText + "]";
                        foreach (string subQuery in Database.EnumerateTableFiltersForPrefetch(String.Empty, ScriptingPreferences))
                        {
                            Server.InitQueryUrns(new Urn(levelFilter + subQuery), null, null, null, ScriptingPreferences, new Urn(levelUrn + subQuery), Database.DatabaseEngineEdition);
                        }

                    });

                case "View":
                    return new LimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Clear all existing Views
                        Database.Views.Clear();
                        // Prefetch next batch of Views
                        string levelUrn = Database.Urn + "/View";
                        string levelFilter = levelUrn + "[" + batchBlock.FilterConditionText + "]";
                        foreach (string subQuery in Database.EnumerateViewFiltersForPrefetch(String.Empty, ScriptingPreferences))
                        {
                            Server.InitQueryUrns(new Urn(levelFilter + subQuery), null, null, null, ScriptingPreferences, new Urn(levelUrn + subQuery), Database.DatabaseEngineEdition);
                        }
                    });

                case "StoredProcedure":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all stored procs
                        Database.PrefetchStoredProcedures(ScriptingPreferences);
                    });

                case "User":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all users
                        Database.PrefetchUsers(ScriptingPreferences);
                    });

                case "DatabaseRole":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all DB Roles
                        Database.PrefetchDatabaseRoles(ScriptingPreferences);
                    });

                case "Default":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all defaults
                        Database.PrefetchDefaults(ScriptingPreferences);
                    });

                case "Rule":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all rules
                        Database.PrefetchRules(ScriptingPreferences);
                    });

                case "UserDefinedFunction":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all UDFs
                        Database.PrefetchUserDefinedFunctions(ScriptingPreferences);
                    });


                case "ExtendedStoredProcedure":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all extended SPs.
                        Database.PrefetchExtendedStoredProcedures(ScriptingPreferences);
                    });

                case "UserDefinedType":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all UDTs
                        Database.PrefetchUserDefinedTypes(ScriptingPreferences);
                    });

                case "UserDefinedTableType":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all UDTTs
                        Database.PrefetchUserDefinedTableTypes(ScriptingPreferences);
                    });

                case "UserDefinedAggregate":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all aggregates
                        Database.PrefetchUserDefinedAggregates(ScriptingPreferences);
                    });

                case "UserDefinedDataType":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all UDDTs
                        Database.PrefetchUDDT(ScriptingPreferences);
                    });

                case "XmlSchemaCollection":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all schema collections
                        Database.PrefetchXmlSchemaCollections(ScriptingPreferences);
                    });

                case "SqlAssembly":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all SQL assemblies
                        Database.PrefetchSqlAssemblies(ScriptingPreferences);
                    });

                case "Schema":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all schemas
                        Database.PrefetchSchemas(ScriptingPreferences);
                    });

                case "PartitionScheme":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all partition schemes
                        Database.PrefetchPartitionSchemes(ScriptingPreferences);
                    });

                case "PartitionFunction":
                    return new UnlimitedBatchBlock(typeName, delegate(BatchBlock batchBlock)
                    {
                        // Prefetches all partition functions
                        Database.PrefetchPartitionFunctions(ScriptingPreferences);
                    });


                default:
                    // Type unsupported:
                    // In this case we create a batch block for this type but do no prefetching
                    // No prefetching may result in a performance hit but
                    // at least that wouldn't be worse than what we already had
                    // in Transfer
                    return new UnlimitedBatchBlock(typeName, null);

            }
        }

        #region class UrnIterator

        class UrnIterator : IEnumerable<Urn>
        {
            private Prefetch prefetch;
            private IList<Urn> urnList;
            private List<BatchBlock> batchBlocks = new List<BatchBlock>();

            internal UrnIterator(Prefetch prefetch, IList<Urn> urns)
            {
                this.prefetch = prefetch;
                this.urnList = urns;
                // Build collection of batch blocks
                // Each of batch blocks triggers a portion of database objects to be prefetched
                BuildBatchBlocks();
            }

            #region IEnumerable implementation

            IEnumerator<Urn> IEnumerable<Urn>.GetEnumerator()
            {
                return new Enumerator(this.prefetch, this.urnList, this.batchBlocks);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this.prefetch, this.urnList, this.batchBlocks);
            }

            class Enumerator : IEnumerator<Urn>
            {
                private Prefetch prefetch;
                private IList<Urn> urns;
                private IList<BatchBlock> batchBlocks;
                private int urnIndex;
                private int batchIndex;

                internal Enumerator(Prefetch prefetch, IList<Urn> urns, IList<BatchBlock> batchBlocks)
                {
                    this.prefetch = prefetch;
                    this.urns = urns;
                    this.batchBlocks = batchBlocks;
                    Reset();
                }

                public void Reset()
                {
                    urnIndex = -1;
                    batchIndex = 0;
                }

                public bool MoveNext()
                {
                    return (++urnIndex < this.urns.Count);
                }

                public Urn Current
                {
                    get
                    {
                        if (urnIndex < 0 || urnIndex >= this.urns.Count)
                        {
                            throw new InvalidOperationException();
                        }

                        // Verify if any batch blocks need to be triggered by the current Urn index
                        while (batchIndex < this.batchBlocks.Count)
                        {
                            BatchBlock batchBlock = this.batchBlocks[batchIndex];
                            if (batchBlock.StartIndex > urnIndex)
                            {
                                break;
                            }

                            // urnIndex has reached the current batch block index
                            // Let's prefetch objects and advance to the next batch Block
                            this.prefetch.OnBeforePrefetchObjects(batchBlock);
                            batchBlock.PrefetchObjects();
                            this.prefetch.OnAfterPrefetchObjects(batchBlock);
                            batchIndex++;
                        }

                        return this.urns[urnIndex];
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return this.Current;
                    }
                }

                void IDisposable.Dispose()
                {
                }
            }

            #endregion

            /// <summary>
            /// This function builds batch blocks.
            /// </summary>
            void BuildBatchBlocks()
            {
                // Loop through all URNs and discover
                for (int index = 0; index < urnList.Count; index++)
                {
                    // Process Urn
                    Urn urn = urnList[index];
                    string type = urn.Type;

                    BatchBlock batchBlock;
                    if (TryGetBatchBlock(type, out batchBlock))
                    {
                        if (!batchBlock.TryAdd(this.prefetch, urn))
                        {
                            // Either the number of names has reached a limit
                            // or schema is incompatible - need a new batch block
                            batchBlock = null;
                        }
                    }
                    
                    if (batchBlock == null)
                    {
                        // Create a new batch block
                        batchBlock = prefetch.CreateBatchBlock(type);
                        if (batchBlock == null)
                        {
                            continue;
                        }

                        // Associate the block with the current position in the Urn list
                        // so that we can trigger the prefetching later at appropriate moment
                        batchBlock.StartIndex = index;
                        // Add name and schema
                        batchBlock.TryAdd(this.prefetch, urn);
                        // Add the block to the collection of batch blocks
                        this.batchBlocks.Add(batchBlock);
                    }
                }
            }

            /// <summary>
            /// Search for the last batch block with matching type name.
            /// Use that block only when the schema matches too.
            /// </summary>
            private bool TryGetBatchBlock(string type, out BatchBlock batchBlock)
            {
                batchBlock = null;

                for (int i = this.batchBlocks.Count - 1; i >= 0; i--)
                {
                    if (this.batchBlocks[i].TypeName == type)
                    {
                        batchBlock = this.batchBlocks[i];
                        return true;
                    }
                }

                return false;
            }

        }

        #endregion // UrnIterator
    }

    #endregion // Prefetch

    #region BatchBlock

    delegate void PrefetchObjectsFunc(BatchBlock batch);

    internal class BatchFactory
    {
    }

    /// <summary>
    /// Each batch block contains enough information to prefetch a batch of SMO objects.
    /// Due to implementation all objects in the same batch must share the same type and schema -
    /// only object name can vary.
    /// </summary>
    internal abstract class BatchBlock
    {
        private string typeName;
        private int startIndex;
        PrefetchObjectsFunc prefetchFunc;

        internal BatchBlock(string typeName, PrefetchObjectsFunc prefetchFunc)
        {
            this.typeName = typeName;
            this.prefetchFunc = prefetchFunc;
        }

        public string TypeName
        {
            get { return this.typeName; }
        }

        public int StartIndex
        {
            get { return this.startIndex; }
            set { this.startIndex = value; }
        }

        public abstract string FilterConditionText
        {
            get;
        }

        public abstract bool TryAdd(Prefetch prefetch, Urn urn);

        public void PrefetchObjects()
        {
            if (this.prefetchFunc != null)
            {
                this.prefetchFunc(this);
            }
        }
    }

    /// <summary>
    /// This class represents a batch block used to load Tables, Views, Stored Procedures, etc
    /// The total number of objects in one batch is limited to a predefined constant - see MaximumObjectsPerBatch below
    /// Also due to implementation all objects must share the same schema.
    /// LimitedBatchBlock instances accumulate names and use them later when querying SMO objects
    /// </summary>
    internal class LimitedBatchBlock : BatchBlock
    {
        const int MaximumObjectsPerBatch = 5000;
        private List<int> ids = new List<int>(MaximumObjectsPerBatch);
        private string filterConditionText = null;
        
        internal LimitedBatchBlock(string typeName, PrefetchObjectsFunc prefetchFunc) : base(typeName, prefetchFunc)
        {
        }

        public override string FilterConditionText
        {
            get
            {
                if (this.filterConditionText == null)
                {
                    // Builds a text for IN function, for example in(@ID, '1,2,3,4,5')

                    StringBuilder builder = new StringBuilder();
                    if (ids.Count > 0)
                    {
                        foreach (int id in ids)
                        {
                            if (builder.Length == 0)
                            {
                                builder.Append("in(@ID, '");
                            }
                            else
                            {
                                builder.Append(",");
                            }
                            builder.Append(id);
                        }

                        builder.Append("')");
                    }

                    this.filterConditionText = builder.ToString();
                }

                return this.filterConditionText;
            }
        }

        public override bool TryAdd(Prefetch prefetch, Urn urn)
        {
            if (this.ids.Count == MaximumObjectsPerBatch)
            {
                // Already has maximum number of IDs accumulated
                return false;
            }

            int id = (int)prefetch.Server.GetSmoObject(urn).Properties["ID"].Value;
            this.ids.Add(id);
            // reset filter condition text - force it to be recalculated
            this.filterConditionText = null;

            return true;
        }
    }

    /// <summary>
    /// This class represents an unlimited batch which is used to load all existing objects of a given type.
    /// It doesn't record individual names of objects and doesn't care about the total number of objects returned by a query
    /// </summary>
    internal class UnlimitedBatchBlock : BatchBlock
    {
        internal UnlimitedBatchBlock(string typeName, PrefetchObjectsFunc prefetchFunc) : base(typeName, prefetchFunc)
        {
        }

        public override string FilterConditionText
        {
            get { return String.Empty; }
        }

        public override bool TryAdd(Prefetch prefetch, Urn urn)
        {
            // Do nothing
            return true; 
        }
    }

    #endregion // BatchBlock

}

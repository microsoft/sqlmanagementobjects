// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

using System.Data;
using System.Linq;

using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Class to order the objects in executable order
    /// </summary>
    internal class SmoDependencyOrderer : ISmoDependencyOrderer
    {
        private Dictionary<UrnTypeKey, List<Urn>> urnTypeDictionary;

        public ScriptingPreferences ScriptingPreferences { get; set; }

        internal ScriptContainerFactory ScriptContainerFactory { get; set; }

        internal CreatingObjectDictionary creatingDictionary;

        #region static strings

        private static string CERTIFICATEKEYLOGIN = "certificatekeylogin";
        private static string CERTIFICATEKEYUSER = "certificatekeyuser";
        private static string MASTERASSEMBLY = "masterassembly";
        private static string MASTERCERTIFICATE = "mastercertificate";
        private static string MASTERASYMMETRICKEY = "masterasymmetrickey";
        private static string USERASSEMBLY = "userassembly";
        private static string USERCERTIFICATE = "usercertificate";
        private static string USERASYMMETRICKEY = "userasymmetrickey";
        private static string CLUSTEREDINDEX = "clusteredindex";
        private static string NONCLUSTEREDINDEX = "nonclusteredindex";
        private static string PRIMARYXMLINDEX = "primaryxmlindex";
        private static string SECONDARYXMLINDEX = "secondaryxmlindex";
        private static string SELECTIVEXMLINDEX = "selectivexmlindex";
        private static string SECONDARYSELECTIVEXMLINDEX = "secondaryselectivexmlindex";
        private static string SPATIALINDEX = "spatialindex";
        private static string COLUMNSTOREINDEX = "columnstoreindex";
        private static string CLUSTEREDCOLUMNSTOREINDEX = "clusteredcolumnstoreindex";
        private static string VECTORINDEX = "vectorindex";
        private static string JSONINDEX = "jsonindex";
        private static string DATA = "data";
        private static string SERVERPERMISSION = "serverpermission";
        private static string DATABASEPERMISSION = "databasepermission";
        private static string SERVERASSOCIATION = "serverassociation";
        private static string DATABASEASSOCIATION = "databaseassociation";
        private static string SERVEROWNERSHIP = "serverownership";
        private static string DATABASEOWNERSHIP = "databaseownership";
        private static string DATABASEREADONLY = "databasereadonly";
        private static string CREATINGUDF = "creatingudf";
        private static string SCALARUDF = "scalarudf";
        private static string TABLEVIEWUDF = "tableviewudf";
        private static string CREATINGSPROC = "creatingsproc";
        private static string NONSCHEMABOUNDSPROC = "nonschemaboundsproc";
        private static string CREATINGVIEW = "creatingview";
        private static string CREATINGTABLE = "creatingtable";
        private static string SERVERROLESUFFIX = "roleserver";
        private static string DATABASEROLESUFFIX = "roledatabase";
        private static string DATABASEDDLTRIGGERSUFFIX = "ddltriggerdatabase";
        private static string DATABASEDDLTRIGGERENABLE = "ddltriggerdatabaseenable";
        private static string DATABASEDDLTRIGGERDISABLE = "ddltriggerdatabasedisable";
        private static string SERVERDDLTRIGGERSUFFIX = "ddltriggerserver";
        private static string SERVERDDLTRIGGERENABLE = "ddltriggerserverenable";
        private static string SERVERDDLTRIGGERDISABLE = "ddltriggerserverdisable";
        #endregion

        /// <summary>
        /// Server for the urns provided
        /// </summary>
        public Server Server { get; set; }

        /// <summary>
        /// Order the list of input urns
        /// </summary>
        /// <param name="urns">input urns</param>
        /// <returns></returns>
        public List<Urn> Order(IEnumerable<Urn> urns)
        {
            Urn outUrn = null;
            int count = this.StoreInDictionary(urns, out outUrn);
            if (count > 1)
            {
                this.ResolveDependencies();
                return this.SortedList();
            }
            else if (count > 0)
            {
                return ResolveSingleUrn(urns, outUrn);
            }

            return new List<Urn>(urns);
        }

        private List<Urn> ResolveSingleUrn(IEnumerable<Urn> urns, Urn outUrn)
        {
            Urn urn = outUrn;

            //Check for table and data handling
            if (urn.Type.Equals(Table.UrnSuffix))
            {
                List<Urn> tableUrns = new List<Urn>();

                if (this.ScriptingPreferences.IncludeScripts.Ddl)
                {
                    tableUrns.Add(urn);
                }

                if (this.ScriptingPreferences.IncludeScripts.Data)
                {
                    if (!this.creatingDictionary.ContainsKey(urn))
                    {
                        tableUrns.Add((this.ConvertUrn(urn, "Data")));
                    }
                }

                return tableUrns;
            }
            else if (this.ScriptingPreferences.IncludeScripts.Ddl)
            {
                //check for login,serverrole and add associations
                if (urn.Type.Equals(Login.UrnSuffix) || (urn.Type.Equals(ServerRole.UrnSuffix) && urn.Parent.Type.Equals(Server.UrnSuffix)))
                {
                    List<Urn> serverSecurityObjects = new List<Urn>();
                    serverSecurityObjects.Add(urn);
                    if (!this.creatingDictionary.ContainsKey(urn) || this.creatingDictionary.SmoObjectFromUrn(urn).State == SqlSmoState.Existing)
                    {
                        serverSecurityObjects.Add((this.ConvertUrn(urn, "Associations")));
                    }
                    return serverSecurityObjects;
                }
                else if (urn.Type.Equals(Database.UrnSuffix))
                {
                    List<Urn> databases = new List<Urn>();
                    databases.Add(urn);
                    databases.Add((this.ConvertUrn(urn, "databasereadonly")));
                    return databases;
                }
                // return for rest
                return new List<Urn>(urns);
            }
            else
            {
                //return empty
                return new List<Urn>();
            }
        }

        /// <summary>
        /// Sorts the type dictionary's keys  and returns the result of sorting
        /// VSTS # 1167567, for each clustered index, if possible, Adjust the sorting order by inserting them immediatly after their parent objects.
        /// </summary>
        /// <returns></returns>
        private List<Urn> SortedList()
        {
            List<UrnTypeKey> urnTypeKeyList = new List<UrnTypeKey>(this.urnTypeDictionary.Keys);
            //sort the list
            urnTypeKeyList.Sort();

            List<Urn> urnList = new List<Urn>();

            foreach (UrnTypeKey urnTypeKey in urnTypeKeyList)
            {
                if (ObjectOrder.clusteredindex != urnTypeKey.CreateOrder)
                {
                    urnList.AddRange(this.urnTypeDictionary[urnTypeKey]);
                }
                else
                {
                    List<Urn> clusteredIndexList = this.urnTypeDictionary[urnTypeKey];
                    foreach (Urn urn in clusteredIndexList)
                    {
                        int i = urnList.IndexOf(urn.Parent); // Find the parent object.
                        if (i >= 0)
                        {
                            urnList.Insert(i + 1, urn); // Insert immediatly after the parent
                        }
                        else
                        {
                            urnList.Add(urn); // Append it in case the clusteredindex is scripted without its parent.
                        }
                    }
                }
            }

            return urnList;
        }

        /// <summary>
        ///  Store the input into a type dictionary
        /// </summary>
        /// <param name="urns"></param>
        /// <param name="outUrn"></param>
        /// <returns></returns>
        private int StoreInDictionary(IEnumerable<Urn> urns, out Urn outUrn)
        {
            outUrn = null;
            int count = 0;
            this.urnTypeDictionary.Clear();

            //If it is data only scripting store only tables
            if (this.ScriptingPreferences.IncludeScripts.Ddl)
            {
                foreach (Urn urn in urns)
                {
                    this.AddToDictionary(urn);
                    count++;
                    outUrn = urn;
                }
            }
            else
            {
                foreach (Urn urn in urns)
                {
                    if (urn.Type.Equals(Table.UrnSuffix))
                    {
                        this.AddToDictionary(urn);
                        count++;
                        outUrn = urn;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Helper to add to type dictionary
        /// </summary>
        /// <param name="urn"></param>
        private void AddToDictionary(Urn urn)
        {
            UrnTypeKey urnTypeKey = new UrnTypeKey(urn);
            if (this.urnTypeDictionary.ContainsKey(urnTypeKey))
            {
                this.urnTypeDictionary[urnTypeKey].Add(urn);
            }
            else
            {
                List<Urn> urnList = new List<Urn>();
                urnList.Add(urn);
                this.urnTypeDictionary.Add(urnTypeKey, urnList);
            }
        }

        /// <summary>
        /// Initialize the class with server
        /// </summary>
        /// <param name="srv"></param>
        public SmoDependencyOrderer(Server srv)
        {
            this.Server = srv;
            this.urnTypeDictionary = new Dictionary<UrnTypeKey, List<Urn>>();
            this.creatingDictionary = null;
        }

        /// <summary>
        /// Resolves the dependency issues of various types
        /// </summary>
        private void ResolveDependencies()
        {
            if (this.ScriptingPreferences.IncludeScripts.Ddl)
            {
                this.ResolveSqlAssemblyDependencies();

                this.ResolveDdlTriggerDependencies();

                this.ResolveSecurityObjectDependencies();

                if (this.ScriptingPreferences.IncludeScripts.Data)
                {
                    this.AddTableData();
                }

                this.ResolveIndexDependencies();

                if (this.ScriptingPreferences.Behavior != ScriptBehavior.Drop)
                {
                    this.EmbedForeignKeysChecksDefaultConstraints();
                }
                else
                {
                    this.AddForeignKeys();
                }

                this.ResolveTableViewUDFSprocDependencies();
            }
            else
            {
                this.ResolveTableOnlyDependencies();
                this.urnTypeDictionary.Remove(new UrnTypeKey(Table.UrnSuffix));
            }
        }

        private void ResolveSqlAssemblyDependencies()
        {
            List<SqlAssembly> sqlAssemblyList = this.GetList<SqlAssembly>(SqlAssembly.UrnSuffix);

            if (sqlAssemblyList != null)
            {
                if (sqlAssemblyList.Count > 1)
                {
                    List<Urn> creatingList = new List<Urn>();
                    List<SqlAssembly> existingList = new List<SqlAssembly>();

                    foreach (var item in sqlAssemblyList)
                    {
                        if (item.State == SqlSmoState.Creating)
                        {
                            creatingList.Add(item.Urn);
                        }
                        else
                        {
                            existingList.Add(item);
                        }
                    }

                    List<Urn> sortedList = new List<Urn>();
                    string query = "select assembly_id,referenced_assembly_id from sys.assembly_references where assembly_id in ({0}) and referenced_assembly_id in ({0})";

                    this.ExecuteQuery(sortedList, existingList.ConvertAll(p => { return p as SqlSmoObject; }), query);
                    sortedList.AddRange(creatingList);
                    this.urnTypeDictionary[new UrnTypeKey(SqlAssembly.UrnSuffix)] = sortedList;
                }
            }
        }

        private void ResolveDdlTriggerDependencies()
        {
            this.ResolveServerDdlTriggerDependencies();
            this.ResolveDatabaseDdlTriggerDependencies();
        }

        private void ResolveServerDdlTriggerDependencies()
        {
            List<Urn> serverDdlTriggers;

            if ((this.urnTypeDictionary.TryGetValue(new UrnTypeKey(SmoDependencyOrderer.SERVERDDLTRIGGERSUFFIX), out serverDdlTriggers))
                && (serverDdlTriggers.Count > 1))
            {
                if ((this.ScriptingPreferences.Behavior & ScriptBehavior.Create) == ScriptBehavior.Create)
                {
                    List<Urn> enableList = serverDdlTriggers.ConvertAll(p => { return this.ConvertUrn(p, SmoDependencyOrderer.SERVERDDLTRIGGERENABLE); });
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SERVERDDLTRIGGERENABLE), enableList);
                }

                if ((this.ScriptingPreferences.Behavior & ScriptBehavior.Drop) == ScriptBehavior.Drop)
                {
                    List<Urn> disableList = serverDdlTriggers.ConvertAll(p => { return this.ConvertUrn(p, SmoDependencyOrderer.SERVERDDLTRIGGERDISABLE); });
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SERVERDDLTRIGGERDISABLE), disableList);
                }

                this.MarkUrnListSpecial(SmoDependencyOrderer.SERVERDDLTRIGGERSUFFIX);
            }
        }

        private void ResolveDatabaseDdlTriggerDependencies()
        {
            List<Urn> databaseDdlTriggers;

            if ((this.urnTypeDictionary.TryGetValue(new UrnTypeKey(SmoDependencyOrderer.DATABASEDDLTRIGGERSUFFIX), out databaseDdlTriggers))
                && (databaseDdlTriggers.Count > 1))
            {
                if ((this.ScriptingPreferences.Behavior & ScriptBehavior.Create) == ScriptBehavior.Create)
                {
                    List<Urn> enableList = databaseDdlTriggers.ConvertAll(p => { return this.ConvertUrn(p, SmoDependencyOrderer.DATABASEDDLTRIGGERENABLE); });
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATABASEDDLTRIGGERENABLE), enableList);
                }

                if ((this.ScriptingPreferences.Behavior & ScriptBehavior.Drop) == ScriptBehavior.Drop)
                {
                    List<Urn> disableList = databaseDdlTriggers.ConvertAll(p => { return this.ConvertUrn(p, SmoDependencyOrderer.DATABASEDDLTRIGGERDISABLE); });
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATABASEDDLTRIGGERDISABLE), disableList);
                }

                this.MarkUrnListSpecial(SmoDependencyOrderer.DATABASEDDLTRIGGERSUFFIX);
            }

        }

        #region Index resolution

        /// <summary>
        /// Combine primary key and unique key to table's script
        /// </summary>
        private void ResolveIndexDependencies()
        {
            if (this.ScriptContainerFactory == null)
            {
                ResolveIndexDependenciesWithoutFactory();
            }
            else
            {
                ResolveIndexDependenciesWithFactory();
            }

        }

        /// <summary>
        /// Resolve index dependencies by accesing SMO objects
        /// </summary>
        private void ResolveIndexDependenciesWithoutFactory()
        {
            List<Index> indexList = this.GetList<Index>(Index.UrnSuffix);
            if (indexList != null)
            {
                List<Urn> tableList;
                this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Table.UrnSuffix), out tableList);

                HashSet<Urn> tableHashSet = new HashSet<Urn>();
                HashSet<Urn> filestreamTableHashSet = new HashSet<Urn>();

                if (tableList != null)
                {
                    tableHashSet = new HashSet<Urn>(tableList);

                    if (this.ScriptingPreferences.IncludeScripts.Data)
                    {
                        filestreamTableHashSet = new HashSet<Urn>(tableList.Where(p =>
                        {
                            return SmoDependencyOrderer.IsFilestreamTable((Table)this.creatingDictionary.SmoObjectFromUrn(p), this.ScriptingPreferences);
                        }));
                    }
                }

                List<Index> clusteredIndex = new List<Index>();
                List<Index> nonClusteredIndex = new List<Index>();
                List<Index> primaryXmlIndex = new List<Index>();
                List<Index> secondaryXmlIndex = new List<Index>();
                List<Index> selectiveXmlIndex = new List<Index>();
                List<Index> secondarySelectiveXmlIndex = new List<Index>();
                List<Index> spatialIndex = new List<Index>();
                List<Index> columnstoreIndex = new List<Index>();
                List<Index> clusteredColumnstoreIndex = new List<Index>();
                List<Index> vectorIndex = new List<Index>();
                List<Index> jsonIndex = new List<Index>();

                foreach (var index in indexList)
                {
                    if (this.ScriptingPreferences.IncludeScripts.Data && (filestreamTableHashSet.Contains(index.Urn.Parent)) && IsKey(index))
                    {
                        this.AddToTable(index);
                        continue;
                    }

                    switch (index.InferredIndexType)
                    {
                        case IndexType.ClusteredIndex:
                            if (IsKey(index) && (tableHashSet.Contains(index.Urn.Parent)))
                            {
                                this.AddToTable(index);
                            }
                            else
                            {
                                clusteredIndex.Add(index);
                            }
                            break;
                        case IndexType.NonClusteredIndex:
                            if (!index.IsMemoryOptimizedIndex)
                            {
                                if (IsKey(index) && !this.ScriptingPreferences.IncludeScripts.Data && tableHashSet.Contains(index.Urn.Parent))
                                {
                                    this.AddToTable(index);
                                }
                                else
                                {
                                    nonClusteredIndex.Add(index);
                                }
                            }
                            break;
                        case IndexType.PrimaryXmlIndex:
                            primaryXmlIndex.Add(index);
                            break;
                        case IndexType.SecondaryXmlIndex:
                            secondaryXmlIndex.Add(index);
                            break;
                        case IndexType.SpatialIndex:
                            spatialIndex.Add(index);
                            break;
                        case IndexType.NonClusteredColumnStoreIndex:
                            columnstoreIndex.Add(index);
                            break;
                        case IndexType.NonClusteredHashIndex:
                            break;
                        case IndexType.SelectiveXmlIndex:
                            selectiveXmlIndex.Add(index);
                            break;
                        case IndexType.SecondarySelectiveXmlIndex:
                            secondarySelectiveXmlIndex.Add(index);
                            break;
                        case IndexType.ClusteredColumnStoreIndex:
                            clusteredColumnstoreIndex.Add(index);
                            break;
                        case IndexType.HeapIndex:
                            break;
                        case IndexType.VectorIndex:
                            vectorIndex.Add(index);
                            break;
                        case IndexType.JsonIndex:
                            jsonIndex.Add(index);
                            break;
                        default:
                            throw new WrongPropertyValueException(index.Properties["IndexType"]);
                    }
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(Index.UrnSuffix));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CLUSTEREDINDEX), clusteredIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.NONCLUSTEREDINDEX), nonClusteredIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.PRIMARYXMLINDEX), primaryXmlIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SECONDARYXMLINDEX), secondaryXmlIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SELECTIVEXMLINDEX), selectiveXmlIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SECONDARYSELECTIVEXMLINDEX), secondarySelectiveXmlIndex.ConvertAll(p => { return p.Urn; }));                
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SPATIALINDEX), spatialIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.COLUMNSTOREINDEX), columnstoreIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CLUSTEREDCOLUMNSTOREINDEX), clusteredColumnstoreIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.VECTORINDEX), vectorIndex.ConvertAll(p => { return p.Urn; }));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.JSONINDEX), jsonIndex.ConvertAll(p => { return p.Urn; }));
            }
        }

        /// <summary>
        /// Resolve index dependencies using IndexScriptContainer
        /// </summary>
        private void ResolveIndexDependenciesWithFactory()
        {
            List<Urn> indexList;

            if (this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Index.UrnSuffix), out indexList))
            {
                List<Urn> clusteredIndex = new List<Urn>();
                List<Urn> nonClusteredIndex = new List<Urn>();
                List<Urn> primaryXmlIndex = new List<Urn>();
                List<Urn> secondaryXmlIndex = new List<Urn>();
                List<Urn> selectiveXmlIndex = new List<Urn>();
                List<Urn> secondarySelectiveXmlIndex = new List<Urn>();
                List<Urn> spatialIndex = new List<Urn>();
                List<Urn> columnstoreIndex = new List<Urn>();
                List<Urn> clusteredColumnstoreIndex = new List<Urn>();
                List<Urn> vectorIndex = new List<Urn>();
                List<Urn> jsonIndex = new List<Urn>();

                foreach (var index in indexList)
                {
                    ScriptContainer container;
                    if (this.ScriptContainerFactory.TryGetValue(index, out container))
                    {
                        IndexScriptContainer indexContainer = (IndexScriptContainer)container;

                        switch (indexContainer.IndexType)
                        {
                            case IndexType.ClusteredIndex:
                                clusteredIndex.Add(index);
                                break;
                            case IndexType.NonClusteredIndex:
                                // Indexes of a memory optimized table should be scripted within the table, not after table script.
                                if (!indexContainer.IsMemoryOptimizedIndex)
                                {
                                    nonClusteredIndex.Add(index);
                                }
                                break;
                            case IndexType.PrimaryXmlIndex:
                                primaryXmlIndex.Add(index);
                                break;
                            case IndexType.SecondaryXmlIndex:
                                secondaryXmlIndex.Add(index);
                                break;
                            case IndexType.SelectiveXmlIndex:
                                selectiveXmlIndex.Add(index);
                                break;
                            case IndexType.SecondarySelectiveXmlIndex:
                                secondarySelectiveXmlIndex.Add(index);
                                break;
                            case IndexType.SpatialIndex:
                                spatialIndex.Add(index);
                                break;
                            case IndexType.NonClusteredColumnStoreIndex:
                                columnstoreIndex.Add(index);
                                break;
                            case IndexType.NonClusteredHashIndex:
                                break;
                            case IndexType.ClusteredColumnStoreIndex:
                                clusteredColumnstoreIndex.Add(index);
                                break;
                            case IndexType.HeapIndex:
                                break;
                            case IndexType.VectorIndex:
                                vectorIndex.Add(index);
                                break;
                            case IndexType.JsonIndex:
                                jsonIndex.Add(index);
                                break;
                            default:
                                Debug.Assert(false, "Invalid IndexType");
                                break;
                        }
                    }
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(Index.UrnSuffix));
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CLUSTEREDINDEX), clusteredIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.NONCLUSTEREDINDEX), nonClusteredIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.PRIMARYXMLINDEX), primaryXmlIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SECONDARYXMLINDEX), secondaryXmlIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SELECTIVEXMLINDEX), selectiveXmlIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SECONDARYSELECTIVEXMLINDEX), secondarySelectiveXmlIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SPATIALINDEX), spatialIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.COLUMNSTOREINDEX), columnstoreIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CLUSTEREDCOLUMNSTOREINDEX), clusteredColumnstoreIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.VECTORINDEX), vectorIndex);
                this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.JSONINDEX), jsonIndex);
            }
        }

        internal static bool IsSecondaryXmlIndex(Index index)
        {
            Nullable<SecondaryXmlIndexType> indexIsSecondaryXmlIndex = index.GetPropValueOptional<SecondaryXmlIndexType>("SecondaryXmlIndexType");
            if (indexIsSecondaryXmlIndex.HasValue && (indexIsSecondaryXmlIndex.Value != SecondaryXmlIndexType.None))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add index to table's propagation list
        /// </summary>
        /// <param name="index"></param>
        private void AddToTable(Index index)
        {
            Table table = this.creatingDictionary.SmoObjectFromUrn(index.Urn.Parent) as Table;
            table.AddToIndexPropagationList(index);
        }

        /// <summary>
        /// Check if index isClustered or not
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static bool IsClustered(Index index)
        {
            bool? indexIsClustered = index.GetPropValueOptional<bool>("IsClustered");
            if (indexIsClustered.HasValue && indexIsClustered.Value)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check if index is primary or unique key
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static bool IsKey(Index index)
        {
            Nullable<IndexKeyType> indexKeyType = index.GetPropValueOptional<IndexKeyType>("IndexKeyType");

            if ((indexKeyType.HasValue) && (indexKeyType.Value != IndexKeyType.None))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if table script will contain filestream column
        /// </summary>
        /// <param name="table"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        internal static bool IsFilestreamTable(Table table, ScriptingPreferences sp)
        {
            return ((sp.Storage.FileStreamColumn)
                && (table.IsSupportedProperty("FileStreamFileGroup", sp))
                && (!string.IsNullOrEmpty(table.GetPropValueOptional<string>("FileStreamFileGroup", string.Empty))
                || !string.IsNullOrEmpty(table.GetPropValueOptional<string>("FileStreamPartitionScheme", string.Empty))));
        }

        #endregion

        /// <summary>
        /// This function collapses foreign key,check,default constraints in table script for design mode
        /// </summary>
        private void EmbedForeignKeysChecksDefaultConstraints()
        {
            if (!this.Server.IsDesignMode)
            {
                return;
            }

            List<Urn> tableList;
            this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Table.UrnSuffix), out tableList);
            if (tableList == null || tableList.Count != 1)
            {
                return;
            }

            Table table = (Table)this.creatingDictionary.SmoObjectFromUrn(tableList.First());

            List<ForeignKey> foreignKeyList = this.GetList<ForeignKey>(ForeignKey.UrnSuffix);
            if (foreignKeyList != null)
            {
                foreach (ForeignKey item in foreignKeyList)
                {
                    Debug.Assert(item.Urn.Parent.Equals(table.Urn), "invalid call");
                    table.AddToEmbeddedForeignKeyChecksList(item);
                }
            }

            List<Check> checkContraintsList = this.GetList<Check>(Check.UrnSuffix);
            if (checkContraintsList != null)
            {
                foreach (Check item in checkContraintsList)
                {
                    Debug.Assert(item.Urn.Parent.Equals(table.Urn), "invalid call");
                    table.AddToEmbeddedForeignKeyChecksList(item);
                }
            }

            List<DefaultConstraint> defaultContraintsList = this.GetList<DefaultConstraint>("DefaultColumn");
            if (defaultContraintsList != null)
            {
                foreach (DefaultConstraint item in defaultContraintsList)
                {
                    Debug.Assert(item.Urn.Parent.Parent.Equals(table.Urn), "invalid call");
                    item.forceEmbedDefaultConstraint = true;
                }
            }

            this.urnTypeDictionary.Remove(new UrnTypeKey(ForeignKey.UrnSuffix));
            this.urnTypeDictionary.Remove(new UrnTypeKey(Check.UrnSuffix));
            this.urnTypeDictionary.Remove(new UrnTypeKey("DefaultColumn"));
        }

        /// <summary>
        /// Add the data entry for given tablelist in type dictionary
        /// </summary>
        /// <param name="tableList"></param>
        private void AddTableData(List<Urn> tableList)
        {
            List<Urn> dataList = tableList.ConvertAll(p => { return this.ConvertUrn(p, "Data"); });
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATA), dataList);
        }

        /// <summary>
        /// Append type to urn
        /// </summary>
        /// <param name="p"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Urn ConvertUrn(Urn p, string type)
        {
            return new Urn(string.Format(SmoApplication.DefaultCulture, "{0}/{1}/Special", p, type));
        }

        /// <summary>
        /// Add data entry for the tables in type dictionary
        /// </summary>
        private void AddTableData()
        {
            List<Urn> tableList;

            if (this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Table.UrnSuffix), out tableList))
            {
                tableList = tableList.FindAll(p => { return !this.creatingDictionary.ContainsKey(p); });
                this.AddTableData(tableList);
            }
        }

        /// <summary>
        /// Add foreign keys entry for the tables in type dictionary
        /// </summary>
        private void AddForeignKeys()
        {
            List<Table> tableList = this.GetList<Table>(Table.UrnSuffix);

            if (tableList != null)
            {
                List<Urn> fkList;
                HashSet<Urn> origFkList;

                this.urnTypeDictionary.TryGetValue(new UrnTypeKey(ForeignKey.UrnSuffix), out fkList);

                if (fkList == null)
                {
                    origFkList = new HashSet<Urn>();
                    fkList = new List<Urn>();
                    this.urnTypeDictionary.Add(new UrnTypeKey(ForeignKey.UrnSuffix), fkList);
                }
                else
                {
                    origFkList = new HashSet<Urn>(fkList);
                }
                this.Server.SetDefaultInitFields(typeof(ForeignKey), nameof(ForeignKey.IsFileTableDefined));
                foreach (Table tb in tableList)
                {
                    foreach (ForeignKey fk in tb.ForeignKeys)
                    {
                        // file table defined FKs are system managed
                        if (!fk.IsSupportedProperty(nameof(fk.IsFileTableDefined)) || !fk.IsFileTableDefined)
                        {
                            if (origFkList.Add(fk.Urn))
                            {
                                fkList.Add(fk.Urn);
                            }
                        }
                    }
                }
            }
        }

        #region Table UDF View resolution

        /// <summary>
        /// Resolve cyclic dependencies in table,view, udf and stored procedures(schema bound)
        /// </summary>
        private void ResolveTableViewUDFSprocDependencies()
        {
            List<Urn> schemaboundList = new List<Urn>();
            
            this.ResolveUDFDependencies(schemaboundList);
            this.ResolveSprocDependencies(schemaboundList);
            this.ResolveViewDependencies(schemaboundList);

            // If there are temporal tables within, add them to dependency checks
            // 'current' table depends on the 'history' table
            //
            if (VersionUtils.IsSql13Azure12OrLater(this.Server.DatabaseEngineType, this.Server.ServerVersion, this.ScriptingPreferences))
            {
                this.ResolveTemporalHistoryTableDependencies(schemaboundList);
            }

            //Pass  schemaboundlist
            if (schemaboundList.Count > 0)
            {
                this.OrderAndStoreSchemaBound(schemaboundList);
            }
        }

        /// <summary>
        /// Resolve UDF dependencies
        /// </summary>
        /// <param name="schemaboundList"></param>
        private void ResolveUDFDependencies(List<Urn> schemaboundList)
        {
            List<UserDefinedFunction> udfList = this.GetList<UserDefinedFunction>(UserDefinedFunction.UrnSuffix);

            if (udfList != null)
            {
                List<Urn> scalarUdfList = new List<Urn>();
                List<Urn> creatingUdfList = new List<Urn>();
                bool scalarSchemaBoundUdfPresent = false;

                foreach (UserDefinedFunction udf in udfList)
                {
                    if (udf.State == SqlSmoState.Creating)
                    {
                        //add to creating list
                        creatingUdfList.Add(udf.Urn);
                    }
                    else if ((!udf.IsSchemaBound) && (udf.FunctionType != UserDefinedFunctionType.Inline))
                    {
                        //add as scalar udf
                        scalarUdfList.Add(udf.Urn);
                    }
                    else
                    {
                        schemaboundList.Add(udf.Urn);

                        if (udf.FunctionType == UserDefinedFunctionType.Scalar)
                        {
                            scalarSchemaBoundUdfPresent = true;
                        }
                    }
                }
                //add scalar udf to dictinary
                if (creatingUdfList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CREATINGUDF), creatingUdfList);
                }

                if (scalarUdfList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SCALARUDF), scalarUdfList);
                }

                if (scalarSchemaBoundUdfPresent)
                {
                    ResolveTableDependencies(schemaboundList);
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(UserDefinedFunction.UrnSuffix));
            }
        }

        /// <summary>
        /// Resolve stored procedure dependencies
        /// </summary>
        /// <param name="schemaboundList"></param>
        private void ResolveSprocDependencies(List<Urn> schemaboundList)
        {
            List<StoredProcedure> spList = this.GetList<StoredProcedure>(StoredProcedure.UrnSuffix);

            if (spList != null)
            {
                List<Urn> creatingSprocList = new List<Urn>();
                List<Urn> nonSchemaBoundspList = new List<Urn>();
                List<Urn> schemaBoundSpList = new List<Urn>();

                bool schemaBoundSprocPresent = false;

                foreach (StoredProcedure sp in spList)
                {
                    if (sp.State == SqlSmoState.Creating)
                    {
                        //add to creating list
                        creatingSprocList.Add(sp.Urn);
                    }
                    else if (sp.IsSchemaBound)
                    {
                        schemaboundList.Add(sp.Urn);
                        schemaBoundSprocPresent = true;
                    }
                    else
                    {
                        nonSchemaBoundspList.Add(sp.Urn);
                    }
                }

                //add stored procedure to dictinary
                if (creatingSprocList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CREATINGSPROC), creatingSprocList);
                }

                //add non schema bound stored procedures to dictionary
                if (nonSchemaBoundspList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.NONSCHEMABOUNDSPROC), nonSchemaBoundspList);
                }

                // resolve table dependencies for schema bound stored procedures ( usually  hekaton sp )
                if (schemaBoundSprocPresent)
                {
                    this.ResolveTableDependencies(schemaboundList);
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(StoredProcedure.UrnSuffix));
            }
        }

        private void ResolveTableDependencies(List<Urn> schemaboundList)
        {
            List<Urn> tableList;

            if (this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Table.UrnSuffix), out tableList))
            {
                List<Urn> existingTableList = new List<Urn>();
                List<Urn> creatingTableList = new List<Urn>();

                foreach (Urn table in tableList)
                {
                    if (this.creatingDictionary.ContainsKey(table))
                    {
                        //add to creating list
                        creatingTableList.Add(table);
                    }
                    else
                    {
                        existingTableList.Add(table);
                    }
                }

                if (creatingTableList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CREATINGTABLE), creatingTableList);
                }

                if (existingTableList.Count > 0)
                {
                    schemaboundList.AddRange(this.ReturnComputedColumnTables(existingTableList));
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(Table.UrnSuffix));
            }
        }

        /// <summary>
        /// Resolve view dependencies
        /// </summary>
        /// <param name="schemaboundList"></param>
        private void ResolveViewDependencies(List<Urn> schemaboundList)
        {
            List<Urn> viewList;
            if (this.urnTypeDictionary.TryGetValue(new UrnTypeKey(View.UrnSuffix), out viewList))
            {
                List<Urn> existingViewList = new List<Urn>();
                List<Urn> creatingViewList = new List<Urn>();

                foreach (Urn view in viewList)
                {
                    if (this.creatingDictionary.ContainsKey(view))
                    {
                        //add to creating list
                        creatingViewList.Add(view);
                    }
                    else
                    {
                        existingViewList.Add(view);
                    }
                }

                if (existingViewList.Count > 0)
                {
                    //add all existing view to schemaboundlist
                    schemaboundList.AddRange(existingViewList);
                }

                if (creatingViewList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CREATINGVIEW), creatingViewList);
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(View.UrnSuffix));

            }
        }

        /// <summary>
        /// Resolve temporal tables' dependencies
        /// </summary>
        /// <param name="schemaboundList"></param>
        private void ResolveTemporalHistoryTableDependencies(List<Urn> schemaboundList)
        {
            List<Urn> tableList;

            if (this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Table.UrnSuffix), out tableList))
            {
                List<Urn> existingTableList = new List<Urn>();
                List<Urn> creatingTableList = new List<Urn>();

                foreach (Urn table in tableList)
                {
                    if (this.creatingDictionary.ContainsKey(table))
                    {
                        //add to creating list
                        creatingTableList.Add(table);
                    }
                    else
                    {
                        existingTableList.Add(table);
                    }
                }

                if (creatingTableList.Count > 0)
                {
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CREATINGTABLE), creatingTableList);
                }

                if (existingTableList.Count > 0)
                {
                    schemaboundList.AddRange(existingTableList);
                }

                this.urnTypeDictionary.Remove(new UrnTypeKey(Table.UrnSuffix));
            }
        }

        private void OrderAndStoreSchemaBound(List<Urn> schemaboundList)
        {
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.TABLEVIEWUDF), new List<Urn>());
            Dictionary<Urn, List<Urn>> ListPerDatabase = new Dictionary<Urn, List<Urn>>();

            foreach (var item in schemaboundList)
            {
                if (ListPerDatabase.ContainsKey(item.Parent))
                {
                    ListPerDatabase[item.Parent].Add(item);
                }
                else
                {
                    List<Urn> objList = new List<Urn>();
                    objList.Add(item);
                    ListPerDatabase.Add(item.Parent, objList);
                }
            }

            string query;

            if (VersionUtils.IsSql13Azure12OrLater(this.Server.DatabaseEngineType, this.Server.ServerVersion, this.ScriptingPreferences))
            {
                // Special handling for temporal tables where we're adding a dependency between 'history' and 'current' tables
                //
                query = "select dep.referencing_id,dep.referenced_id from sys.sql_expression_dependencies as dep join #tempordering as t1 on dep.referenced_id = t1.ID join #tempordering as t2 on dep.referencing_id = t2.ID where dep.referenced_id != dep.referencing_id UNION select [object_id], [history_table_id] from sys.tables where [temporal_type] = 2";
            }
            else if (this.Server.VersionMajor > 9)
            {
                query = "select dep.referencing_id,dep.referenced_id from sys.sql_expression_dependencies as dep join #tempordering as t1 on dep.referenced_id = t1.ID join #tempordering as t2 on dep.referencing_id = t2.ID where dep.referenced_id != dep.referencing_id ";
            }
            else if (this.Server.VersionMajor > 8)
            {
                query = "select dep.object_id,dep.referenced_major_id from sys.sql_dependencies as dep join #tempordering as t1 on dep.referenced_major_id = t1.ID join #tempordering as t2 on dep.object_id = t2.ID where dep.referenced_major_id != dep.object_id";
            }
            else
            {
                query = "select dep.id,dep.depid from dbo.sysdepends as dep join #tempordering as t1 on dep.depid = t1.ID join #tempordering as t2 on dep.id = t2.ID where dep.depid != dep.id";
            }

            foreach (var item in ListPerDatabase)
            {
                this.OrderAndStoreSchemaBoundInSingleDatabase(item.Value, query);
            }
        }

        private void OrderAndStoreSchemaBoundInSingleDatabase(List<Urn> list, string query)
        {
            Debug.Assert(list.Count > 0);
            Debug.Assert(this.urnTypeDictionary.ContainsKey(new UrnTypeKey(SmoDependencyOrderer.TABLEVIEWUDF)));

            List<Urn> objectList = this.urnTypeDictionary[new UrnTypeKey(SmoDependencyOrderer.TABLEVIEWUDF)];

            if (list.Count > 1)
            {
                ExecuteQueryUsingTempTable(objectList, list, query);
            }
            else
            {
                objectList.Add(list[0]);
            }
        }

        private void ExecuteQuery(List<Urn> objectList, List<SqlSmoObject> list, string query)
        {
            Dictionary<int, Urn> idDictionary = new Dictionary<int, Urn>();

            StringBuilder sb = new StringBuilder();

            foreach (var item in list)
            {
                int ID = (int)item.Properties.GetValueWithNullReplacement("ID");
                idDictionary.Add(ID, item.Urn);
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0},", ID);
            }

            sb.Remove(sb.Length - 1, 1);
            string finalQuery = string.Format(SmoApplication.DefaultCulture, query, sb.ToString());

            Database db = (Database)this.creatingDictionary.SmoObjectFromUrn(list[0].Urn.Parent);
            DataSet ds = db.ExecuteWithResults(finalQuery);
            SortDataSet(objectList, idDictionary, ds);
        }

        private void ExecuteQueryUsingTempTable(List<Urn> objectList, List<Urn> list, string query)
        {
            Database db = (Database)this.creatingDictionary.SmoObjectFromUrn(list[0].Parent);
            Dictionary<int, Urn> idDictionary = new Dictionary<int, Urn>();
            StringBuilder sb = new StringBuilder();

            //SqlServer 2005 and below do not allow multiple inserts while SqlServer 2008 allows upto 1000 values in single insert
            int batchLength = (this.Server.VersionMajor > 9) ? 1000 : 1;

            StringCollection strcol = new StringCollection();
            if (db.IsSqlDw)
            {
                strcol.Add("create table #tempordering(ID int)");
                batchLength = 1;
            }
            else
            {
                strcol.Add("create table #tempordering(ID int primary key)");
            }
            int i = 0;

            
            StringBuilder insertstatement = new StringBuilder();

            foreach (var item in list)
            {
                int ID = this.GetIdFromUrn(item);
                idDictionary.Add(ID, item);
                sb.AppendFormat(SmoApplication.DefaultCulture, "({0}),", ID);
                i++;

                if (i % batchLength == 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    insertstatement.AppendFormat(SmoApplication.DefaultCulture, "insert into #tempordering(ID) values {0};", sb.ToString());
                    sb.Length = 0;
                    i = 0;
                }
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
                insertstatement.AppendFormat(SmoApplication.DefaultCulture, "insert into #tempordering(ID) values {0};", sb.ToString());
                sb.Length = 0;
            }

            strcol.Add(insertstatement.ToString());
            strcol.Add(query);
            strcol.Add("drop table #tempordering");

            
            DataSet ds = db.ExecuteWithResults(strcol);
            SortDataSet(objectList, idDictionary, ds);
        }

        private int GetIdFromUrn(Urn urn)
        {
            ScriptContainer container;
            if (this.ScriptContainerFactory != null && this.ScriptContainerFactory.TryGetValue(urn, out container))
            {
                IdBasedObjectScriptContainer objectContainer = container as IdBasedObjectScriptContainer;

                if (objectContainer != null)
                {
                    return objectContainer.ID;
                }
            }

            return (int)this.creatingDictionary.SmoObjectFromUrn(urn).Properties.GetValueWithNullReplacement("ID");
        }

        private void SortDataSet(List<Urn> objectList, Dictionary<int, Urn> idDictionary, DataSet ds)
        {
            List<int> sortedList = this.SortDataSet(ds);

            foreach (var item in sortedList)
            {
                if (idDictionary.ContainsKey(item))
                {
                    objectList.Add(idDictionary[item]);
                    idDictionary.Remove(item);
                }
            }

            foreach (var item in idDictionary)
            {
                objectList.Add(item.Value);
            }
        }

        private List<int> SortDataSet(DataSet ds)
        {
            DataTable tblResult = ds.Tables[0];
            Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
            foreach (DataRow item in tblResult.Rows)
            {
                if (dictionary.ContainsKey((int)item[0]))
                {
                    dictionary[(int)item[0]].Add((int)item[1]);
                }
                else
                {
                    List<int> list = new List<int>();
                    list.Add((int)item[1]);
                    dictionary.Add((int)item[0], list);
                }

            }
            ds.Dispose();
            return this.SortDictionary(dictionary);
        }

        private List<int> SortDictionary(Dictionary<int, List<int>> dictionary)
        {
            List<int> sortedList = new List<int>();
            HashSet<int> visited = new HashSet<int>();
            HashSet<int> current = new HashSet<int>();

            foreach (var item in dictionary.Keys)
            {
                this.DependencyGraphTraversal(item, dictionary, sortedList, visited, current);
            }

            return sortedList;
        }

        private void DependencyGraphTraversal(int num, Dictionary<int, List<int>> dictionary, List<int> sortedList, HashSet<int> visited, HashSet<int> current)
        {
            //Recursively traverse the dependency graph and detect cycles. Throw error if cycle is found.

            if (visited.Contains(num))
            {
                return;
            }

            if (!current.Add(num))
            {
                throw new SmoException(ExceptionTemplates.OrderingCycleDetected);
            }

            if (dictionary.ContainsKey(num))
            {
                foreach (var item in dictionary[num])
                {
                    this.DependencyGraphTraversal(item, dictionary, sortedList, visited, current);
                }
            }
            sortedList.Add(num);
            visited.Add(num);

            current.Remove(num);
        }

        private List<Urn> ReturnComputedColumnTables(List<Urn> existingTableList)
        {
            //add non computed table to dictionary
            return existingTableList;
        }

        private void ResolveTableOnlyDependencies()
        {
            List<Urn> tableList;

            if (this.urnTypeDictionary.TryGetValue(new UrnTypeKey(Table.UrnSuffix), out tableList))
            {
                tableList = tableList.FindAll(p => { return (!this.creatingDictionary.ContainsKey(p)); });
                if (tableList.Count > 1)
                {
                    List<Urn> sortedList = new List<Urn>();
                    string query;

                    if (this.Server.VersionMajor > 8)
                    {
                        query = "select fk.parent_object_id,fk.referenced_object_id from sys.foreign_key_columns as fk join #tempordering as t1 on fk.referenced_object_id = t1.ID join #tempordering as t2 on fk.parent_object_id = t2.ID where fk.referenced_object_id != fk.parent_object_id";
                    }
                    else
                    {
                        query = "select fk.fkeyid,fk.rkeyid from dbo.sysreferences as fk join #tempordering as t1 on fk.rkeyid = t1.ID join #tempordering as t2 on fk.fkeyid = t2.ID where fk.rkeyid != fk.fkeyid";
                    }

                    this.ExecuteQueryUsingTempTable(sortedList, tableList, query);
                    this.AddTableData(sortedList);
                    return;
                }

                this.AddTableData(tableList);
            }
        }


        #endregion

        /// <summary>
        /// Generic method to get list of object for specified UrnSuffix
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="UrnSuffix"></param>
        /// <returns></returns>
        private List<T> GetList<T>(string UrnSuffix) where T : SqlSmoObject
        {
            List<T> typeList = null;

            if (this.urnTypeDictionary.ContainsKey(new UrnTypeKey(UrnSuffix)))
            {
                typeList = this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)].ConvertAll(p => { return this.creatingDictionary.SmoObjectFromUrn(p) as T; });
            }

            return typeList;
        }

        #region Security Object resolution

        /// <summary>
        /// Resolve securtity object cycles if any  and database readonly statements
        /// </summary>
        private void ResolveSecurityObjectDependencies()
        {
            this.ResolveServerSecurityObjectDependencies();

            this.ResolveDatabaseSecurityObjectDependencies();

            if (this.ScriptingPreferences.Behavior != ScriptBehavior.Drop)
            {
                bool specialHandling = this.AddServerAssociations();

                specialHandling = (this.AddDatabaseReadOnly() || specialHandling);

                if (this.ScriptingPreferences.IncludeScripts.Associations)
                {
                    this.AddDatabaseAssociations();
                    specialHandling = true;
                }

                if (this.ScriptingPreferences.IncludeScripts.Owner)
                {
                    this.AddOwner();
                    specialHandling = true;
                }

                if (this.ScriptingPreferences.IncludeScripts.Permissions)
                {
                    this.AddPermissions();
                    specialHandling = true;
                }

                if (specialHandling)
                {
                    this.ChangeUrns();
                }

                if (this.urnTypeDictionary.ContainsKey(new UrnTypeKey("UnresolvedEntity")))
                {
                    this.urnTypeDictionary[new UrnTypeKey("UnresolvedEntity")] = this.urnTypeDictionary[new UrnTypeKey("UnresolvedEntity")].ConvertAll(p => { return this.ConvertUrn(p, "UnresolvedEntity"); });
                }

            }
        }

        /// <summary>
        /// Change the Urns of specific type mostly security object as we do not want permissions etc to be scripted with them
        /// </summary>
        private void ChangeUrns()
        {
            this.MarkUrnListSpecial(Database.UrnSuffix);
            this.MarkUrnListSpecial(Login.UrnSuffix);
            this.MarkUrnListSpecial(SmoDependencyOrderer.MASTERASSEMBLY);
            this.MarkUrnListSpecial(SmoDependencyOrderer.MASTERCERTIFICATE);
            this.MarkUrnListSpecial(SmoDependencyOrderer.MASTERASYMMETRICKEY);
            this.MarkUrnListSpecial(SmoDependencyOrderer.CERTIFICATEKEYLOGIN);
            this.MarkUrnListSpecial(SmoDependencyOrderer.SERVERROLESUFFIX);
            this.MarkUrnListSpecial(ApplicationRole.UrnSuffix);
            this.MarkUrnListSpecial(User.UrnSuffix);
            this.MarkUrnListSpecial(SmoDependencyOrderer.USERASSEMBLY);
            this.MarkUrnListSpecial(SmoDependencyOrderer.USERCERTIFICATE);
            this.MarkUrnListSpecial(SmoDependencyOrderer.USERASYMMETRICKEY);
            this.MarkUrnListSpecial(SmoDependencyOrderer.CERTIFICATEKEYUSER);
            this.MarkUrnListSpecial(SmoDependencyOrderer.DATABASEROLESUFFIX);
        }

        /// <summary>
        /// Helper to change urn
        /// </summary>
        /// <param name="UrnSuffix"></param>
        private void MarkUrnListSpecial(string UrnSuffix)
        {
            if (this.urnTypeDictionary.ContainsKey(new UrnTypeKey(UrnSuffix)))
            {
                this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)] = this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)].ConvertAll(p => { return this.ConvertUrn(p, "Object"); });
            }
        }

        /// <summary>
        /// Add the permissions entry for the security objects in the type dictionary
        /// </summary>
        private void AddPermissions()
        {
            List<Urn> PermissionList = new List<Urn>();
            //server permissions
            this.AddConvertedUrnsToList(PermissionList, Login.UrnSuffix, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.CERTIFICATEKEYLOGIN, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.SERVERROLESUFFIX, "Permission");
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SERVEROWNERSHIP), PermissionList);

            PermissionList = new List<Urn>();
            //database permissions
            this.AddConvertedUrnsToList(PermissionList, Database.UrnSuffix, "Permission");
            this.AddConvertedUrnsToList(PermissionList, User.UrnSuffix, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.CERTIFICATEKEYUSER, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.MASTERASSEMBLY, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.MASTERCERTIFICATE, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.MASTERASYMMETRICKEY, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.USERASSEMBLY, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.USERCERTIFICATE, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.USERASYMMETRICKEY, "Permission");
            this.AddConvertedUrnsToList(PermissionList, SmoDependencyOrderer.DATABASEROLESUFFIX, "Permission");
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATABASEOWNERSHIP), PermissionList);
        }

        /// <summary>
        /// Find UrnSuffix matching urns and convert them and add to the list
        /// </summary>
        /// <param name="List"></param>
        /// <param name="UrnSuffix"></param>
        /// <param name="type"></param>
        private void AddConvertedUrnsToList(List<Urn> List, string UrnSuffix, string type)
        {
            if (this.urnTypeDictionary.ContainsKey(new UrnTypeKey(UrnSuffix)))
            {
                List.AddRange(this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)].ConvertAll(p => { return this.ConvertUrn(p, type); }));
            }
        }

        /// <summary>
        /// Add the owner entry for the security objects in the type dictionary
        /// </summary>
        private void AddOwner()
        {
            List<Urn> OwnershipList = new List<Urn>();
            //server ownership
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.SERVERROLESUFFIX, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, Database.UrnSuffix, "Ownership");
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SERVERPERMISSION), OwnershipList);

            OwnershipList = new List<Urn>();
            //database ownership
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.MASTERASSEMBLY, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.MASTERCERTIFICATE, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.MASTERASYMMETRICKEY, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.USERASSEMBLY, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.USERCERTIFICATE, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.USERASYMMETRICKEY, "Ownership");
            this.AddConvertedUrnsToList(OwnershipList, SmoDependencyOrderer.DATABASEROLESUFFIX, "Ownership");
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATABASEPERMISSION), OwnershipList);
        }

        /// <summary>
        ///  Add the associations entry for the security objects in the type dictionary
        /// </summary>
        /// <returns>true if there are server objects</returns>       
        private bool AddServerAssociations()
        {
            List<Urn> AssociationList = new List<Urn>();
            //server associations
            this.AddConvertedUrnsToList(AssociationList, Login.UrnSuffix, "Associations");
            this.AddConvertedUrnsToList(AssociationList, SmoDependencyOrderer.CERTIFICATEKEYLOGIN, "Associations");
            this.AddConvertedUrnsToList(AssociationList, SmoDependencyOrderer.SERVERROLESUFFIX, "Associations");
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.SERVERASSOCIATION), AssociationList);
            return (AssociationList.Count > 0);
        }

        /// <summary>
        ///  Add the read only entry for the database objects in the type dictionary
        /// </summary>
        /// <returns>true if there are database objects</returns>       
        private bool AddDatabaseReadOnly()
        {
            List<Urn> readonlyList = new List<Urn>();
            //database associations
            this.AddConvertedUrnsToList(readonlyList, Database.UrnSuffix, "databasereadonly");            
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATABASEREADONLY), readonlyList);
            return (readonlyList.Count > 0);
        }


        /// <summary>
        /// Add the associations entry for the database security objects in the type dictionary
        /// </summary>
        private void AddDatabaseAssociations()
        {
            List<Urn> AssociationList = new List<Urn>();
            //database associations
            this.AddConvertedUrnsToList(AssociationList, User.UrnSuffix, "Associations");
            this.AddConvertedUrnsToList(AssociationList, SmoDependencyOrderer.CERTIFICATEKEYUSER, "Associations");
            this.AddConvertedUrnsToList(AssociationList, SmoDependencyOrderer.DATABASEROLESUFFIX, "Associations");
            this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.DATABASEASSOCIATION), AssociationList);
        }

        /// <summary>
        /// Resolve server security object cycles by checking for certificate or key logins
        /// </summary>
        private void ResolveServerSecurityObjectDependencies()
        {
            //Database
            //Normal Login
            //Master Assembly
            //Master Certificate
            //Master Asymmetric key
            //certificate key Login
            //FSR 

            List<Login> loginList = this.GetList<Login>(Login.UrnSuffix);

            if (loginList != null)
            {
                List<Login> certKeyLoginList = new List<Login>();
                List<Login> RegularLogin = new List<Login>();
                foreach (Login login in loginList)
                {
                    if ((login.LoginType == LoginType.AsymmetricKey || login.LoginType == LoginType.Certificate))
                    {
                        certKeyLoginList.Add(login);
                    }
                    else
                    {
                        RegularLogin.Add(login);
                    }
                }

                if (certKeyLoginList.Count > 0)
                {
                    this.urnTypeDictionary[new UrnTypeKey(Login.UrnSuffix)] = RegularLogin.ConvertAll(p => { return p.Urn; });
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CERTIFICATEKEYLOGIN), certKeyLoginList.ConvertAll(p => { return p.Urn; }));
                    this.FindAndAddMasterSecurityObjects(SqlAssembly.UrnSuffix, SmoDependencyOrderer.MASTERASSEMBLY);
                    this.FindAndAddMasterSecurityObjects(Certificate.UrnSuffix, SmoDependencyOrderer.MASTERCERTIFICATE);
                    this.FindAndAddMasterSecurityObjects(AsymmetricKey.UrnSuffix, SmoDependencyOrderer.MASTERASYMMETRICKEY);
                }
            }
        }

        /// <summary>
        /// Find master databases's assembly , certificates and keys , and add them to type dictionary
        /// </summary>
        /// <param name="UrnSuffix"></param>
        /// <param name="urnTypeKey"></param>
        private void FindAndAddMasterSecurityObjects(string UrnSuffix, string urnTypeKey)
        {
            List<Urn> masterUrns = new List<Urn>();
            List<Urn> databaseUrns = new List<Urn>();

            if (this.urnTypeDictionary.ContainsKey(new UrnTypeKey(UrnSuffix)))
            {
                foreach (var item in this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)])
                {
                    if (item.Parent.GetAttribute("Name").Equals("master", StringComparison.Ordinal))
                    {
                        masterUrns.Add(item);
                    }
                    else
                    {
                        databaseUrns.Add(item);
                    }
                }

                if (masterUrns.Count > 0)
                {
                    this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)] = databaseUrns;
                    this.urnTypeDictionary.Add(new UrnTypeKey(urnTypeKey), masterUrns);
                }
            }
        }

        /// <summary>
        /// Resolve database security object cycles by checking for certificate or key users
        /// </summary>
        private void ResolveDatabaseSecurityObjectDependencies()
        {
            //Normal User
            //Assembly
            //Certificate
            //Asymmetric key
            //Symmetric key
            //Certificate Key User
            //Database Role
            List<User> userList = this.GetList<User>(User.UrnSuffix);

            if (userList != null)
            {
                List<User> certKeyUserList = new List<User>();
                List<User> RegularUser = new List<User>();
                foreach (User user in userList)
                {
                    if ((user.UserType == UserType.AsymmetricKey || user.UserType == UserType.Certificate))
                    {
                        certKeyUserList.Add(user);
                    }
                    else
                    {
                        RegularUser.Add(user);
                    }
                }

                if (certKeyUserList.Count > 0)
                {
                    this.urnTypeDictionary[new UrnTypeKey(User.UrnSuffix)] = RegularUser.ConvertAll(p => { return p.Urn; });
                    this.urnTypeDictionary.Add(new UrnTypeKey(SmoDependencyOrderer.CERTIFICATEKEYUSER), certKeyUserList.ConvertAll(p => { return p.Urn; }));
                    this.AddCertificateKeyUserDependencies(SqlAssembly.UrnSuffix, SmoDependencyOrderer.USERASSEMBLY);
                    this.AddCertificateKeyUserDependencies(Certificate.UrnSuffix, SmoDependencyOrderer.USERCERTIFICATE);
                    this.AddCertificateKeyUserDependencies(AsymmetricKey.UrnSuffix, SmoDependencyOrderer.USERASYMMETRICKEY);
                }
            }
        }

        /// <summary>
        /// Switch assembly,certificate and asymmetric keys as dependant types as user might depend on them
        /// </summary>
        /// <param name="UrnSuffix"></param>
        /// <param name="urnKeyType"></param>
        private void AddCertificateKeyUserDependencies(string UrnSuffix, string urnKeyType)
        {
            if (this.urnTypeDictionary.ContainsKey(new UrnTypeKey(UrnSuffix)))
            {
                this.urnTypeDictionary.Add(new UrnTypeKey(urnKeyType), this.urnTypeDictionary[new UrnTypeKey(UrnSuffix)]);
                this.urnTypeDictionary.Remove(new UrnTypeKey(UrnSuffix));
            }
        }
        #endregion
    }
}

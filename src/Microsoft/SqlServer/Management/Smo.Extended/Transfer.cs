// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Instance class encapsulating Transfer object
    /// </summary>
    public class Transfer : DataTransferBase, ITransferMetadataProvider
    {
        public Transfer() : base() 
        {
            this.BatchSize = 0;
            this.BulkCopyTimeout = 0;
        }

        public Transfer(Database database) : base(database) 
        {
            this.BatchSize = 0;
            this.BulkCopyTimeout = 0;
        }
        
        private string tempDtsPackageFilesDir = string.Empty;
        public string TemporaryPackageDirectory
        {
            get { return tempDtsPackageFilesDir; }
            set { tempDtsPackageFilesDir = value; }
        }
        
#region process dependency chain methods
        //Following methods are no longer needed they are here due to back compat reasons

        /// <summary>
        /// Process a dependency chain, into a list of objects. Order is preserved, but
        /// duplicates are removed
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dependencyChain"></param>
        /// <param name="isDataOnly">
        /// specifies if the ordering is needed for data only scripting or not. In case
        /// of data only scripting, foreign key relationships are never broken
        /// All other type of relationships can be broken since other objects are not scripted
        /// during data only
        /// </param>
        /// <param name="isCreateOrder"></param>
        /// <returns></returns>
        protected static Urn[] ProcessDependencyChain(Server server,
            DependencyChainCollection dependencyChain,
            bool isDataOnly,
            bool isCreateOrder)
        {
            //Dictionary to contain bool[] for Dependency Node which maintains state of its links as broken or intact
            Dictionary<Dependency, bool[]> BrokenLinks;
            BrokenLinks = new Dictionary<Dependency, bool[]>();
            // Identify cycles and mark links to be broken
            //
            FindCycles(dependencyChain, server, isDataOnly, isCreateOrder, BrokenLinks);

            // Actually break the cycles
            //
            BreakCycles(BrokenLinks);

            // Create an ordered list of objects based on the result
            List<Urn> objectsInOrder = new List<Urn>(dependencyChain.Count);
            // Create a lookup table used to verify if an object has already been added to
            // objectsInOrder list
            Dictionary<Urn, object> lookupTable = new Dictionary<Urn, object>(dependencyChain.Count);

            foreach (Dependency dependency in dependencyChain)
            {
                AddDependency(objectsInOrder, lookupTable, dependency);
            }

            return objectsInOrder.ToArray();
        }

        /// <summary>
        /// Adds the dependency urn to the list after verifying that all dependent links
        /// have been added
        /// </summary>
        /// <param name="objectsInOrder"></param>
        /// <param name="lookupTable"></param>
        /// <param name="dependency"></param>
        private static void AddDependency(List<Urn> objectsInOrder, Dictionary<Urn, object> lookupTable, Dependency dependency)
        {
            // Remove duplicates as it is expected that object can appear more than once in this list.
            if (dependency == null ||
                lookupTable.ContainsKey(dependency.Urn))
            {
                return;
            }

            // Ensure that all links have been added for the dependency
            // before adding the dependency
            //
            foreach (Dependency childDependency in dependency.Links)
            {
                AddDependency(objectsInOrder, lookupTable, childDependency);
            }

            objectsInOrder.Add(dependency.Urn);
            lookupTable.Add(dependency.Urn, null);
        }

        /// <summary>
        /// This method finds the cycles  amongst objects in the passed DependencyChainCollection
        /// </summary>
        /// <param name="dependencyChain"></param>
        /// <param name="server"></param>
        /// <param name="isDataOnly"></param>
        /// <param name="isCreateOrder"></param>
        /// <param name="BrokenLinks"></param>
        /// <returns></returns>
        private static void FindCycles(DependencyChainCollection dependencyChain, Server server, bool isDataOnly, bool isCreateOrder, Dictionary<Dependency, bool[]> BrokenLinks)
        {
            // Create a list used to store urns that have been visited
            // a missing urn means it has not been visited
            //
            Dictionary<Urn, object> visitedUrns = new Dictionary<Urn, object>();

            // Visit all objects to detect cycles
            // 
            foreach (Dependency dependency in dependencyChain)
            {
                if (visitedUrns.ContainsKey(dependency.Urn))
                {
                    continue;
                }

                // Create a list representing current chain
                List<Dependency> currentChain = new List<Dependency>();
                Visit(dependency, currentChain, visitedUrns, server, isDataOnly, isCreateOrder, BrokenLinks);
            }
        }

        /// <summary>
        /// Actually remove all the links which had been marked as broken earlier
        /// </summary>
        private static void BreakCycles(Dictionary<Dependency, bool[]> BrokenLinks)
        {
            foreach (KeyValuePair<Dependency, bool[]> kvp in BrokenLinks)
            {
                int count = 0;
                for (int i = 0; i < kvp.Value.Length; i++)
                {
                    if (kvp.Value[i])
                    {
                        kvp.Key.Links.RemoveAt(i - count);
                        count++;
                    }
                }
            }
        }

        /// <summary>
        /// Breaks cycles by finding and marking the links that can be broken
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cycle"></param>
        /// <param name="isDataOnly">
        /// specifies if the ordering is needed for data only scripting or not. In case
        /// of data only scripting, foreign key relationships are never broken
        /// All other type of relationships can be broken since other objects are not scripted
        /// during data only
        /// </param>
        /// <param name="isCreateOrder"></param>
        /// <param name="BrokenLinks"></param>
        private static void MarkNodeForBreaking(Dependency[] cycle, Server server, bool isDataOnly, bool isCreateOrder, Dictionary<Dependency, bool[]> BrokenLinks)
        {
            // Identify a link which can be broken
            //
            int nodeForWhichToBreakLink = -1;
            int nextNodeId = -1;

            //DPW HelperSystem.Diagnostics.Trace.WriteLineIf(Logging.ScriptingEngineSwitch.TraceInfo,
            //    "Searching for link to break in cycle ");

            for (int i = 0; i < cycle.Length; i++)
            {
                string urnType = cycle[i].Urn.Type.Trim();

                //DPW HelperSystem.Diagnostics.Trace.WriteLineIf(Logging.ScriptingEngineSwitch.TraceInfo,
                //  "Checking node of type " + urnType);

                // StoredProcedure and Synonyms use deferred name resolution
                // and hence their links can be broken
                // break if we find such a link
                //
                if (urnType.Equals("StoredProcedure", StringComparison.OrdinalIgnoreCase) ||
                    urnType.Equals("Synonym", StringComparison.OrdinalIgnoreCase))
                {
                    nodeForWhichToBreakLink = GetNodeIdForWhichToBreakLink(i, cycle.Length, isCreateOrder);
                    break;
                }

                // if urn is for a scalar userDefinedFunction which is not schema bound
                // then it can be broken as well
                // break if we find such a link
                //
                if (urnType.Equals("UserDefinedFunction", StringComparison.OrdinalIgnoreCase))
                {
                    SqlSmoObject smoObject = server.GetSmoObject(cycle[i].Urn);
                    UserDefinedFunction udf = smoObject as UserDefinedFunction;

                    if (udf != null &&
                       udf.FunctionType == UserDefinedFunctionType.Scalar &&
                       udf.IsSchemaBound == false)
                    {
                        nodeForWhichToBreakLink =
                            GetNodeIdForWhichToBreakLink(i, cycle.Length, isCreateOrder);
                        break;
                    }
                }

                if (urnType.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                    cycle[i].Urn.Parent.Type.Equals("Column", StringComparison.OrdinalIgnoreCase))
                {
                    nodeForWhichToBreakLink =
                        GetNodeIdForWhichToBreakLink(i, cycle.Length, isCreateOrder);
                    break;
                }

                // If this is a data only publish then 
                // we can break links between any objects other than tables
                // If this is not a data only publish then we can break links between tables
                //
                if (isDataOnly)
                {
                    if (!urnType.Equals("Table", StringComparison.OrdinalIgnoreCase))
                    {
                        nodeForWhichToBreakLink =
                            GetNodeIdForWhichToBreakLink(i, cycle.Length, isCreateOrder);
                        break;
                    }
                }
                else
                {
                    nextNodeId = i + 1;
                    if (nextNodeId >= cycle.Length)
                    {
                        nextNodeId = 0;
                    }

                    if (urnType.Equals("Table", StringComparison.OrdinalIgnoreCase) &&
                    cycle[nextNodeId].Urn.Type.Trim().Equals("Table", StringComparison.OrdinalIgnoreCase))
                    {
                        nodeForWhichToBreakLink = i;
                        break;
                    }
                }
            }

            // If we did not find a link to break and this is a data only publish
            // then throw an exception, else choose the first link as the victim
            // 
            if (nodeForWhichToBreakLink < 0)
            {
                if (isDataOnly)
                {
                    throw new FailedOperationException(ExceptionTemplates.CyclicalForeignKeys);
                }
                else
                {
                    nodeForWhichToBreakLink = 0;

                    Sdk.Sfc.TraceHelper.Assert(false, "We could not find a node to break for the cycle");

                    SqlSmoObject.Trace("We could not find a node to break for the cycle");
                }
            }

            // break the link 
            //
            nextNodeId = nodeForWhichToBreakLink + 1;
            if (nextNodeId >= cycle.Length)
            {
                nextNodeId = 0;
            }

            //Mark the link as broken 
            BrokenLinks[cycle[nodeForWhichToBreakLink]][cycle[nodeForWhichToBreakLink].Links.IndexOf(cycle[nextNodeId])] = true;

        }

        private static int GetNodeIdForWhichToBreakLink(int nodeId, int cycleLength, bool isCreateOrder)
        {

            if (isCreateOrder)
            {
                return nodeId;
            }
            else
            {
                if (nodeId == 0)
                {
                    return cycleLength - 1;
                }
                else
                {
                    return nodeId - 1;
                }
            }
        }

        /// <summary>
        /// Using Depth first approach to find all the cycles and then breaking all cycles found 
        /// </summary>
        /// <param name="dependency"></param>
        /// <param name="currentChain"></param>
        /// <param name="visitedUrns"></param>
        /// <param name="server"></param>
        /// <param name="isDataOnly"></param>
        /// <param name="isCreateOrder"></param>
        /// <param name="BrokenLinks"></param>
        private static void Visit(Dependency dependency,
            List<Dependency> currentChain,
            Dictionary<Urn, object> visitedUrns, 
            Server server, 
            bool isDataOnly, 
            bool isCreateOrder, 
            Dictionary<Dependency, 
            bool[]> BrokenLinks)
        {
            if (currentChain.Contains(dependency))
            {
                // CurrentChain contains a cycle. Break the cycle.
                //
                List<Dependency> cycle = new List<Dependency>();
                int startOfCycle = currentChain.IndexOf(dependency);
                for (int i = startOfCycle; i < currentChain.Count; i++)
                {
                    cycle.Add(currentChain[i]);
                }

                //if all the urn in current cycle is visited, then 
                //it has been broken and we don't need to break it again.
                foreach (Dependency dep in cycle)
                {
                    if (!visitedUrns.ContainsKey(dep.Urn))
                    {
                        MarkNodeForBreaking(cycle.ToArray(), server, isDataOnly, isCreateOrder, BrokenLinks);
                        break;
                    }
                }
                return;
            }

            //If we are visiting a node for the first time 
            if ((dependency.Links.Count > 0) && (!BrokenLinks.ContainsKey(dependency)))
            {
                //we need to create bool[] which each element in bool[] correspond to state of each link 
                //whether it is broken or not
                bool[] b1 = new bool[dependency.Links.Count];
                //Initially all links are intact
                //Initialize each link state 
                MarkFalseDependency(dependency, b1, isDataOnly);
                BrokenLinks.Add(dependency, b1);
            }
            currentChain.Add(dependency);

            int j = 0;
            // Visit all child elements of this link which have not been visited
            //
            foreach (Dependency parentDependency in dependency.Links)
            {
                // If parent is the same as the 
                // dependency then skip
                //
                if (parentDependency == null ||
                    parentDependency == dependency)
                {
                    continue;
                }

                //Visit a node only if its link has not been marked as broken
                if (!BrokenLinks[dependency][j])
                {
                    Visit(parentDependency, currentChain, visitedUrns, server, isDataOnly, isCreateOrder, BrokenLinks);
                }
                j++;
            }

            // All children have been visited
            // Remove the Urn from the currentChain and add it to the list
            // of visitedUrns
            currentChain.RemoveAt(currentChain.Count - 1);
            // Note, the actually value doesn't matter. It only matters whether the Urn 
            // presents in the dictionary as key
            visitedUrns[dependency.Urn] = null;
        }

        /// <summary>
        /// Procedure to mark all non necessary links as broken
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="b"></param>
        /// <param name="isDataOnly"></param>
        private static void MarkFalseDependency(Dependency d1, bool[] b, bool isDataOnly)
        {
            int j = 0;
            string urnType = d1.Urn.Type.Trim();
            foreach (Dependency parentDependency in d1.Links)
            {
                if (isDataOnly)
                {
                    //If it is Dataonly script all dependency except table ones are not necessary
                    if (!urnType.Equals("Table", StringComparison.OrdinalIgnoreCase))
                    {
                        b[j] = true;
                    }
                }
                else
                {
                    //If it is not Dataonly script Table to Table dependency is taken care of 
                    //by scripting foreign keys at the end of scripts
                    if (urnType.Equals("Table", StringComparison.OrdinalIgnoreCase) &&
                    parentDependency.Urn.Type.Trim().Equals("Table", StringComparison.OrdinalIgnoreCase))
                    {
                        b[j] = true;
                    }
                }
                j++;
            }
        }

#endregion
        
        /// <summary>
        /// Number of rows in each batch of SqlBulkCopy. At the end of each batch, the rows in the batch are sent to the server.
        /// </summary>
        public int BatchSize
        {
            get;
            set;
        }

        /// <summary>
        /// Number of seconds for the operation to complete before SqlBulkCopy times out. 
        /// </summary>
        public int BulkCopyTimeout 
        {
            get;
            set;
        }
    
        /// <summary>
        /// Performs data transfer
        /// </summary>
        public void TransferData()
        {
            TransferWriter writer = null;

            try
            {
                writer = this.GetScriptLoadedTransferWriter();
                this.UpdateWriter(writer);

                SqlTransaction transaction = null;
                var destinationConnection = GetDestinationServerConnection().SqlConnectionObject;
                try
                {
                    using (var sourceConnection = Database.Parent.ConnectionContext.GetDatabaseConnection(Database.Name, poolConnection: false).SqlConnectionObject)
                    {
                        if (sourceConnection.State != System.Data.ConnectionState.Open)
                        {
                            sourceConnection.Open();
                        }

                        if (destinationConnection.State != System.Data.ConnectionState.Open)
                        {
                            destinationConnection.Open();
                        }

                        this.ExecuteStatements(destinationConnection, writer.PreTransaction, null);

                        try
                        {

                            if (this.UseDestinationTransaction)
                            {
                                transaction = destinationConnection.BeginTransaction();
                                this.DataTransferProgressEvent("BEGIN TRANSACTION");
                            }

                            this.ExecuteStatements(destinationConnection, writer.Prologue, transaction);
                            this.SqlBulkCopyData(sourceConnection, destinationConnection, writer, transaction);
                            this.ExecuteStatements(destinationConnection, writer.Epilogue, transaction);
                        }
                        catch (Exception)
                        {
                            if (this.UseDestinationTransaction && transaction != null)
                            {
                                transaction.Rollback();
                                this.DataTransferProgressEvent("ROLLBACK TRANSACTION");
                            }

                            throw;
                        }

                        if (this.UseDestinationTransaction)
                        {
                            transaction.Commit();
                            this.DataTransferProgressEvent("COMMIT TRANSACTION");
                        }

                        this.ExecuteStatements(destinationConnection, writer.PostTransaction, null);

                        destinationConnection.Close();

                        sourceConnection.Close();
                    }
                }
                catch (Exception Ex)
                {
                    if (this.compensationScript != null && this.compensationScript.Count > 0)
                    {
                        using (var compensationDestinationConnection = (SqlConnection)((ICloneable)destinationConnection).Clone())
                        {
                            compensationDestinationConnection.Open();

                            EnumerableContainer container = new EnumerableContainer
                            {
                                this.compensationScript
                            };
                            this.ExecuteStatements(compensationDestinationConnection, container, null);
                        }
                    }
                    throw new TransferException(ExceptionTemplates.TransferDataException, Ex);
                }
            }
            finally
            {
                if (this.LogTransferDumps && writer != null)
                {
                    // Dump all the writer content
                    string transferDumpFileName = string.Empty;
                    using (StreamWriter swFile = Transfer.GetTempFile<StreamWriter>(this.GetTempDir(), "TransferDump{0}.sql", ref transferDumpFileName, Transfer.GetStreamWriter), swConsole = new StreamWriter(Console.OpenStandardOutput()))
                    {
                        DumpWriterContent("-- NON_TRANSACTABLE", writer.PreTransaction, swFile, swConsole);
                        DumpWriterContent("-- PROLOGUE SQL", writer.Prologue, swFile, swConsole);
                        DumpWriterContent("-- EPILOGUE SQL", writer.Epilogue, swFile, swConsole);
                        DumpWriterContent("-- POST_TRANSACTION SQL", writer.PostTransaction, swFile, swConsole);
                        
                        if (this.compensationScript != null && this.compensationScript.Count > 0)
                        {
                            EnumerableContainer container = new EnumerableContainer();
                            container.Add(this.compensationScript);
                            DumpWriterContent("-- COMPENSATION", container, swFile, swConsole);
                        }                    
                    }
                }
            }
        }

        private void UpdateWriter(TransferWriter writer)
        {
            if (writer.Tables.Count > 0)
            {
                // bulk inserts need to operate with quoted indentifiers
                // so change the connection settings after we finish executing
                // DDL statements
                writer.Prologue.Add("SET QUOTED_IDENTIFIER ON");
                writer.Prologue.Add("SET ANSI_NULLS ON");

                foreach (var item in writer.Tables)
                {
                    var smoObject = this.Database.Parent.GetSmoObject(item);
                    Table table = (Table)smoObject;
                    //Check if the table is a filetable. 
                    if (table.IsSupportedProperty("IsFileTable"))
                    {
                        Boolean isFileTable = table.GetPropValueOptional("IsFileTable", false);
                        if (isFileTable)
                        {
                            string alterScript = "ALTER TABLE {0} {1} FILETABLE_NAMESPACE";
                            string fullTableName = table.FormatFullNameForScripting(this.Scripter.Options.GetScriptingPreferences());

                            writer.Prologue.Add(string.Format(SmoApplication.DefaultCulture, alterScript, fullTableName, Scripts.DISABLE));

                            writer.Epilogue.Add(string.Format(SmoApplication.DefaultCulture, alterScript, fullTableName, Scripts.ENABLE));
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:ReviewSqlQueriesForSecurityVulnerabilities", Justification = "Statements do not come from user input")]
        private void ExecuteStatements(SqlConnection destinationConnection, IEnumerable<string> statements, SqlTransaction transaction)
        {
            foreach (string statement in statements)
            {
                SqlCommand command = new SqlCommand(statement, destinationConnection)
                {
                    // default timeout is 30, but we don't want to use 0 and wait forever unless the user has provided a custom SqlConnection with CommandTimeout set to 0.
                    // 120 should be enough for Azure database creates, which is the long pole.
                    CommandTimeout = destinationConnection.GetCommandTimeout() == 0 ? 0 : Math.Max(120, destinationConnection.GetCommandTimeout())
                };
                if (this.UseDestinationTransaction && transaction != null)
                {
                    command.Transaction = transaction;
                }

                command.ExecuteNonQuery();
                this.DataTransferProgressEvent(statement);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:ReviewSqlQueriesForSecurityVulnerabilities", Justification = "Statements do not come from user input and query is unable to be fully parameterized (columns and table name)")]
        private void SqlBulkCopyData(SqlConnection sourceConnection, SqlConnection destinationConnection, TransferWriter writer, SqlTransaction transaction)
        {
            ScriptingPreferences preferences = this.Scripter.Options.GetScriptingPreferences();
            SqlBulkCopyOptions bulkCopyOptions = SqlBulkCopyOptions.FireTriggers;
            if (preferences.Table.Identities)
            {
                bulkCopyOptions |= SqlBulkCopyOptions.KeepIdentity;
            }

            foreach (var item in writer.Tables)
            {
                var smoObject = this.Database.Parent.GetSmoObject(item);
                Table table = (Table)smoObject;

                using (SqlBulkCopy bulkCopy =
                                new SqlBulkCopy(destinationConnection, bulkCopyOptions, transaction))
                {
                    string tableName = string.Format(SmoApplication.DefaultCulture, "{0}.{1}", SqlSmoObject.MakeSqlBraket(table.Schema), SqlSmoObject.MakeSqlBraket(table.Name));
                    string destinationTableName = table.FormatFullNameForScripting(this.Scripter.Options.GetScriptingPreferences());

                    //Starting table's data transfer event
                    this.DataTransferInformationEvent(ExceptionTemplates.StartingDataTransfer(destinationTableName));

                    string columnNames = this.SetColumnNameAndMapping(table, bulkCopy);
                    
                    // Get data from the source table as a SqlDataReader.
                    // For Hekaton M5, 'READ COMMITTED' is not supported with a memory optimized table. 
                    String cmdText;
                    if (table.IsSupportedProperty("IsMemoryOptimized") && table.IsMemoryOptimized)
                    {
                        cmdText = string.Format(SmoApplication.DefaultCulture, "SELECT {0} FROM {1} WITH (SNAPSHOT)", columnNames, tableName);
                    }
                    else
                    {
                        cmdText = string.Format(SmoApplication.DefaultCulture, "SELECT {0} FROM {1}", columnNames, tableName);                        
                    }

                    SqlCommand commandSourceData = new SqlCommand(cmdText, sourceConnection);

                    using (SqlDataReader reader =
                        commandSourceData.ExecuteReader())
                    {
                        bulkCopy.DestinationTableName = destinationTableName;
                        bulkCopy.BulkCopyTimeout = this.BulkCopyTimeout;
                        bulkCopy.BatchSize = this.BatchSize;
                        
                        // Write from the source to the destination.
                        bulkCopy.WriteToServer(reader);

                        //Completed table's data transfer event
                        this.DataTransferInformationEvent(ExceptionTemplates.CompletedDataTransfer(destinationTableName));
                    }
                }
            }
        }

        private string SetColumnNameAndMapping(Table table, SqlBulkCopy bulkCopy)
        {
            string columnNames = string.Empty;

            //If this table has a column set then we shouldn't add any of the sparse columns
            //as they will be included in the column set anyways (and if we try to add the
            //sparse columns as well SqlBulkCopy will throw an error because it correctly
            //doesn't expect those columns)
            bool hasColumnSet = table.Columns.Cast<Column>().Any(col => col.IsColumnSet);

            foreach (Column col in table.Columns)
            {
                if (col.IsGraphInternalColumn() || col.IsGraphComputedColumn())
                {
                    continue;
                }

                if (!col.Computed && !(hasColumnSet && col.IsSparse))
                {
                    string columnName = string.Empty;
                    if (col.DataType.SqlDataType == SqlDataType.UserDefinedType ||
                        col.DataType.SqlDataType == SqlDataType.HierarchyId ||
                        col.DataType.SqlDataType == SqlDataType.Geography ||
                        col.DataType.SqlDataType == SqlDataType.Geometry)
                    {
                        columnName = "{0} CAST({1} as varbinary(max)) AS {1},";
                    }
                    else
                    {
                        columnName = "{0} {1},";
                    }

                    columnNames = string.Format(SmoApplication.DefaultCulture, columnName, columnNames, SqlSmoObject.MakeSqlBraket(col.Name));
                    SqlBulkCopyColumnMapping mapID = new SqlBulkCopyColumnMapping(col.Name, col.Name);
                    bulkCopy.ColumnMappings.Add(mapID);
                }
            }

            columnNames = columnNames.Remove(columnNames.Length - 1);
            return columnNames;
        }



        /// <summary>
        /// Returns the component that is used to perform transfer
        /// </summary>
        /// <returns></returns>
        public IDataTransferProvider GetTransferProvider()
        {
            throw new NotSupportedException();
        }
        
        public event DataTransferEventHandler DataTransferEvent;

        private void OnDataTransferProgress(DataTransferEventType dataTransferEventType, string message)
        {
            if (this.DataTransferEvent != null)
            {
                this.DataTransferEvent(this, new DataTransferEventArgs(dataTransferEventType, message));
            }
        }

        private void DataTransferProgressEvent(string statement)
        {
            this.OnDataTransferProgress(DataTransferEventType.Progress, ExceptionTemplates.ExecutingScript(statement));
        }

        private void DataTransferInformationEvent(string message)
        {
            this.OnDataTransferProgress(DataTransferEventType.Information, message);
        }

#region ITransferMetadataProvider Members

        /// <summary>
        /// Save the metadata in the paths provided by the variables in the input list
        /// </summary>
        void ITransferMetadataProvider.SaveMetadata()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// returns the options as a collection of named variables
        /// </summary>
        /// <returns></returns>
        SortedList ITransferMetadataProvider.GetOptions()
        {
            throw new NotSupportedException();
        }

#endregion
        private string GetTempDir()
        {
            // We will use the temp folder
            var folder = Path.GetTempPath();

            if (string.IsNullOrEmpty(folder))
            {
                throw new SmoException(ExceptionTemplates.InexistentDir(folder));
            }

            // For administrative purposes we will create a subfolder 
            string tempDir = Path.Combine(folder, @"\Microsoft\SQL Server\Smo");

            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            // double check that the directory creation succeeded
            if (!Directory.Exists(tempDir))
            {
                throw new SmoException(ExceptionTemplates.InexistentDir(tempDir));
            }

            return tempDir;
        }

        private static StreamWriter GetStreamWriter(string fileName)
        {
            return new StreamWriter(new FileStream(fileName,FileMode.OpenOrCreate), Encoding.Unicode);
        }
        
        private static T GetTempFile<T>(string directory, string mask, ref string fileName, Func<string, T> createFile)
            where T : class
        {
            T writer = null;
            int attemptCount = 0;
            fileName = string.Empty;

            do
            {
                fileName = Path.Combine(directory, string.Format(SmoApplication.DefaultCulture, mask, Guid.NewGuid().ToString("N", SmoApplication.DefaultCulture)));
                if (!File.Exists(fileName))
                {
                    try
                    {
                        writer = createFile(fileName);
                    }
                    catch (PathTooLongException)
                    {
                        throw;
                    }
                    catch (IOException e)
                    {
                        //Guids are almost always unique. But ensuring against "file in use" exception just in case there is collision
                        //same Guid got generated
                        if (attemptCount++ < 10)
                        {
                            continue;
                        }
                        else
                        {
                            throw new SmoException(ExceptionTemplates.CantCreateTempFile(directory), e);
                        }
                    }
                }
            } while (writer == null);

            return writer;
        }

        /// <summary>
        /// Dump the contents of the given filename to the given outfile stream (and optionally a second stream if not null)
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="outfile"></param>
        /// <param name="label"></param>
        /// <param name="outfile2"></param>
        private void DumpWriterContent(string label, IEnumerable<string> strings, StreamWriter outfile, StreamWriter outfile2)
        {
            if (label == null)
            {
                return;
            }

            outfile.WriteLine("-- ================================================="); if (outfile2 != null) outfile2.WriteLine("-- =================================================");
            outfile.WriteLine(label); if (outfile2 != null) outfile2.WriteLine(label);
            outfile.WriteLine(""); if (outfile2 != null) outfile2.WriteLine("");

            if (strings == null)
            {
                outfile.WriteLine("-- *** empty ***"); if (outfile2 != null) outfile2.WriteLine("-- *** empty ***");
                return;
            }

            foreach (string s in strings)
            {
                outfile.WriteLine(s);
                outfile.WriteLine(Globals.Go);
                if (outfile2 != null)
                {
                    outfile2.WriteLine(s);
                    outfile2.WriteLine(Globals.Go);
                }
            }

            outfile.WriteLine(""); if (outfile2 != null) outfile2.WriteLine("");
        }
    }

    static class SqlConnectionExtensions
    {
        // System.Data.SqlClient.SqlConnection doesn't have a CommandTimeout property
        static public int GetCommandTimeout(this SqlConnection sqlConnection)
        {
#if MICROSOFTDATA
            return sqlConnection.CommandTimeout;
#else
            return 30;
#endif
        }
    }

}


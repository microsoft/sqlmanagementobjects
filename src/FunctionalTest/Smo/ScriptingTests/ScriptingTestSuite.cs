// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#if false //Commented out temporarily for moving to SSMS_Main as this will take significant rework to be usable in the new branch

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// SMO scripting TestSuite
    /// </summary>
    [TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    public partial class ExternalDataSource_SmoTestSuite : SqlTestBase
    {

#region Methods

        /// <summary>
        /// VSTS# 1145361 - Script Partition Fujnction using DateTimeOffset produces invalid syntax. Backport from
        /// change list 1869567.
        /// This test ensures that a Partion Function with DateTimeOffset parameters produces a script, which in
        /// turn, creates a Partition Function.
        /// </summary>
        [TestMethod,Ignore]
        public void TestScriptingPFDateTimeOffset()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string pfName = "PF" + Guid.NewGuid();
            Microsoft.SqlServer.Management.Smo.PartitionFunction pf = null;
            try
            {
                Microsoft.SqlServer.Management.Smo.Database database = server.Databases["master"];

                // Create a Partition Function using SMO
                pf = new Microsoft.SqlServer.Management.Smo.PartitionFunction(database, pfName);
                pf.RangeType = Microsoft.SqlServer.Management.Smo.RangeType.Right;
                pf.PartitionFunctionParameters.Add(new PartitionFunctionParameter(pf, DataType.DateTimeOffset(7)));
                pf.RangeValues = new Object[] { "20100101", "20100201", "20100301" };
                pf.Create();

                // Script the Partition Function
                ScriptingOptions sp = new ScriptingOptions();
                sp.IncludeHeaders = true;
                pf = database.PartitionFunctions[pfName];
                pf.Refresh();
                StringCollection sc = pf.Script(sp);
                pf.Drop();
                pf = null;

                StringBuilder sb = new StringBuilder();
                foreach (string s in sc)
                {
                    sb.AppendLine(s);
                }

                // Run the script to re-create the Partition Function
                string script = sb.ToString();
                database.ExecuteNonQuery(script);
                database.PartitionFunctions.Refresh();
                pf = database.PartitionFunctions[pfName];
                Assert.IsNotNull(pf, "Partion function was not created by the script: \"" + script + "\"");
            }
            finally
            {
                pf.Drop();
            }
        }

        /// <summary>
        /// Verification of PartitionSchema Script in case we have a Next Used filegroup - Bug# 960570
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyFileGroupsInPartitionSchemaScript()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            Database testDB = null;
            string databaseName = "VerifyPartitionSchemaScript_DB" + Guid.NewGuid();
            string partiotionName = "VerifyPartitionSchemaScript_PS" + Guid.NewGuid();
            string filegroup1 = "VerifyPartitionSchemaScript_FG" + Guid.NewGuid();
            string filegroup2 = "VerifyPartitionSchemaScript_FG" + Guid.NewGuid();
            string filegroup3 = "VerifyPartitionSchemaScript_FG" + Guid.NewGuid();

            try
            {
                // Create database and filegroups
                testDB = new Database(server, databaseName);
                testDB.Create();

                FileGroup fileGroup = new FileGroup(testDB, filegroup1);
                fileGroup.Create();

                fileGroup = new FileGroup(testDB, filegroup2);
                fileGroup.Create();

                fileGroup = new FileGroup(testDB, filegroup3);
                fileGroup.Create();

                testDB.FileGroups.Refresh();

                // Create the partiotion scheme with a next used filegroup
                string query = string.Format(@"USE [{0}]
CREATE PARTITION FUNCTION testFunc (int)
AS RANGE LEFT FOR VALUES (10, 100);
CREATE PARTITION SCHEME [{1}]
AS PARTITION testFunc
TO ([{2}], [{3}], [{4}])", databaseName, partiotionName, testDB.FileGroups[1].Name, testDB.FileGroups[2].Name, testDB.FileGroups[3].Name);
                testDB.ExecuteNonQuery(query);
                testDB.PartitionSchemes.Refresh();

                // Script the partition scheme
                PartitionScheme createdSchema = testDB.PartitionSchemes[partiotionName];
                Assert.IsNotNull(createdSchema, "The partition scheme is not created.");
                StringCollection sc = createdSchema.Script();

                string[] lines = new string[sc.Count];
                sc.CopyTo(lines, 0);
                string result = string.Join(" ", lines);

                // Verify all filegroups exist in the script
                Assert.IsTrue(result.Contains(testDB.FileGroups[1].Name), "Filegroup '{0}' is not scripted", testDB.FileGroups[1].Name);
                Assert.IsTrue(result.Contains(testDB.FileGroups[2].Name), "Filegroup '{0}' is not scripted", testDB.FileGroups[2].Name);
                Assert.IsTrue(result.Contains(testDB.FileGroups[3].Name), "Next Used Filegroup '{0}' is not scripted", testDB.FileGroups[3].Name);
            }
            finally
            {
                server.KillDatabase(databaseName);
            }
        }

        // Validation Scenarios:
        //1. Validated Customer Scenario: Adding default constraint to a not-null column to an non-emtpy table 
        //2. Create a table with columns with nullable and column without default constraint.
        //3. Create a table with columns with nullable and column with default constraint.
        //4. Alter table by adding a new nullable column that has a default constraint.
        //5. Alter table with by adding a new column that is may be null with and without default constraint.
        //6. Negative testing: Add a new column that is not nullable without a default constrain. Failed as expected.
        //7. Calling column.Create and Table.Alter passed.
        //8. Add a column not-null with extended properties to an non-empty table. 

        /// <summary>
        /// Verification of default constraints on existing and new tables - 
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyDefaultConstraint()
        {
            string databaseName = "VerifyDefaultConstrainScript_DB" + Guid.NewGuid();
            string tableName = "TestTable";
            string columnName1 = "TestCol1";
            string columnName2 = "TestCol2";
            string columnName3 = "TestCol3";
            string columnName4 = "TestCol4";
            string columnName5 = "TestCol5";
            TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Creating connection to server {0}", this.ServerName));

            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            Database testDB;

            try
            {
                //Set server's Execution mode to execute the T-SQL to create the database and table
                server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;

                // create a new database
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Creating database {0}", databaseName));
                testDB = new Database(server, databaseName);
                testDB.Create();
                    
                // create a new table
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Creating table {0}", tableName));

                Microsoft.SqlServer.Management.Smo.Table table = new Microsoft.SqlServer.Management.Smo.Table(testDB, tableName);

                // Add nullable column without default constraint
                Microsoft.SqlServer.Management.Smo.Column column1 = new Microsoft.SqlServer.Management.Smo.Column();
                column1.Name = columnName1;
                column1.Parent = table;
                column1.DataType = Microsoft.SqlServer.Management.Smo.DataType.NVarChar(30);
                column1.Nullable = true;
                table.Columns.Add(column1);
                table.Create();
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Adding nullable {0} to empty table. Passed!!", columnName1));

                // add nullable column with default constraint
                Microsoft.SqlServer.Management.Smo.Column column2 = new Microsoft.SqlServer.Management.Smo.Column();
                column2.Name = columnName2;
                column2.Parent = table;
                column2.DataType = Microsoft.SqlServer.Management.Smo.DataType.NVarChar(30);
                column2.Nullable = true;
                column2.AddDefaultConstraint();
                column2.DefaultConstraint.Text = "('defaultvalue2')";
                table.Columns.Add(column2);
                table.Alter();
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Adding nullable {0} with default constraint to empty table. Passed!!", columnName2));

                // add not nullable column with default constraint to empty table
                Microsoft.SqlServer.Management.Smo.Column column3 = new Microsoft.SqlServer.Management.Smo.Column();
                column3.Name = columnName3;
                column3.Parent = table;
                column3.DataType = Microsoft.SqlServer.Management.Smo.DataType.NVarChar(30);
                column3.Nullable = false;
                column3.AddDefaultConstraint();
                column3.DefaultConstraint.Text = "('defaultvalue3')";
                table.Columns.Add(column3);
                table.Alter();
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Adding not nullable {0} with default constraint to empty table. Passed!!", columnName3));

                // insert few rows in the created table
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Adding rows to table {0}", tableName));

                System.Int32 index = 0;

                for (index = 0; index < 5; index++)
                {
                    System.String query = System.String.Format("USE [{0}] INSERT INTO [{1}] ([{2}], [{3}], [{4}]) VALUES ('{5}', '{6}', '{7}');", databaseName, tableName, columnName1, columnName2, columnName3, "value1", "value2", "value3");

                    TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Executing query: {0}", query));

                    testDB.ExecuteNonQuery(query);
                }

                // We to verify verify adding default constraints to a non-empty table. 
                // To ensure this test is done correctly, we refresh the SMO table object to ensure 
                // table properties like row count are updated. 
                table.Refresh();
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Table {0} RowCount:{1}", tableName, table.RowCount));

                // add not nullable column with default constraint to a non-empty table
                Microsoft.SqlServer.Management.Smo.Column column4 = new Microsoft.SqlServer.Management.Smo.Column();
                column4.Name = columnName4;
                column4.Parent = table;
                column4.DataType = Microsoft.SqlServer.Management.Smo.DataType.NVarChar(30);
                column4.Nullable = false;
                column4.AddDefaultConstraint();
                column4.DefaultConstraint.Text = "('defaultvalue4')";
                table.Columns.Add(column4);
                table.Alter();
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Adding not nullable {0} with default constraint to none-empty table. Passed!!", columnName4));

                // add a not nullable column with default constraints and extended properties
                Microsoft.SqlServer.Management.Smo.Column column5 = new Microsoft.SqlServer.Management.Smo.Column();
                column5.Name = columnName5;
                column5.Parent = table;
                column5.DataType = Microsoft.SqlServer.Management.Smo.DataType.NVarChar(30);
                column5.Nullable = false;

                DefaultConstraint defaultConstraint = column5.AddDefaultConstraint("default" + "__dflt_constr5");
                defaultConstraint.Text = "''";  
                defaultConstraint.ExtendedProperties.Add(new ExtendedProperty(defaultConstraint, "Ext prop", "Ext prop value"));
                column5.ExtendedProperties.Add(new ExtendedProperty(column5, "Ext prop", "Ext prop value"));
                column5.Create();
                TraceHelper.TraceInformation(string.Format("Default Constraint Testing - Adding not nullable {0} with default constraint and exteded properties.", columnName5)); 
                   
                // Create a new server object from existing one. To ensure no intermediate cached state is used 
                // we prefetch both the DB and the table. This is not a typical way of doing but it exercise 
                // an execution similar to other test cases.
                testDB.PrefetchObjects();
                testDB.PrefetchObjects(typeof(Table));
                Microsoft.SqlServer.Management.Smo.Server server2 = new Microsoft.SqlServer.Management.Smo.Server(server.ConnectionContext.Copy());

                // verify the set extended propretioes for both the column and default constraint
                Assert.IsTrue(server2.Databases[databaseName].Tables[tableName].Columns[columnName5].ExtendedProperties.Contains("Ext prop"));
                Assert.IsTrue(server2.Databases[databaseName].Tables[tableName].Columns[columnName5].DefaultConstraint.ExtendedProperties.Contains("Ext prop"));

                // Create a new service connection and validate the stored default contraint extended properties. 
                // This is the more typical execution path and added for completeness
                Microsoft.SqlServer.Management.Smo.Server server3 = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);

                //Set server's Execution mode to capture the T-SQL Script
                server3.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;

                // verify the set extended propretioes for both the column and default constraint
                Assert.IsTrue(server3.Databases[databaseName].Tables[tableName].Columns[columnName5].ExtendedProperties.Contains("Ext prop"));
                Assert.IsTrue(server3.Databases[databaseName].Tables[tableName].Columns[columnName5].DefaultConstraint.ExtendedProperties.Contains("Ext prop"));

                TraceHelper.TraceInformation("Default Constraint Testing - Default constraint exteded properties validation passed!!");
            }
            catch (Exception e)
            {
                Assert.Fail("Exception :\n" + e.ToString());
            }
            finally
            {
                // drop the database
                server.KillDatabase(databaseName);
            }
        }

        /// <summary>
        /// Verification of Scripting data of type DateTime and its variations Bug# 959826
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyScriptDataForDateTimeTypes()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            Database testDB = null;
            string databaseName = "VerifyScriptDataForDateTimeTypes_Db" + Guid.NewGuid();
            string tableName = "VerifyScriptDataForDateTimeTypes_Tbl" + Guid.NewGuid();

            try
            {
                // Create database and filegroups
                testDB = new Database(server, databaseName);
                testDB.Create();

                // Creating a test value and expected strings based on BOL
                DateTime testDate1 = new DateTime(2012, 9, 19, 21, 01, 35, 600);
                DateTime testDate2 = new DateTime(2000, 8, 21, 10, 11, 46, 960);
                string dateVal1 = "2012-09-19", dateVal2 = "2000-08-21";
                string timeVal1 = "21:01:35.6000000", timeVal2 = "10:11:46.9600000";
                string datetimeVal1 = "2012-09-19 21:01:35.000", datetimeVal2 = "2000-08-21 10:11:46.000";
                string datetime2Val1 = "2012-09-19 21:01:35.0000000", datetime2Val2 = "2000-08-21 10:11:46.0000000";
                string smalldatetimeVal1 = "2012-09-19 21:02:00", smalldatetimeVal2 = "2000-08-21 10:12:00";
                string datetimeoffset1 = "2012-09-19T21:01:35.0000000+00:00", datetimeoffset2 = "2000-08-21T10:11:46.0000000+00:00";

                // Creating a new table with all data and time types
                Table tbl1 = new Table(testDB, tableName);
                tbl1.Columns.Add(new Column(tbl1, "Column1", DataType.Date));
                tbl1.Columns.Add(new Column(tbl1, "Column2", DataType.Time(7)));
                tbl1.Columns.Add(new Column(tbl1, "Column3", DataType.DateTime));
                tbl1.Columns.Add(new Column(tbl1, "Column4", DataType.DateTime2(7)));
                tbl1.Columns.Add(new Column(tbl1, "Column5", DataType.SmallDateTime));
                tbl1.Columns.Add(new Column(tbl1, "Column6", DataType.DateTimeOffset(7)));
                tbl1.Create();

                // Inserting test values
                string query = string.Format(@"INSERT INTO [{0}].[dbo].[{1}]
VALUES ('{2}','{3}','{4}','{5}','{6}','{7}')",
    databaseName,
    tableName,
    testDate1.Date,
    testDate1.TimeOfDay,
    testDate1,
    testDate1,
    testDate1,
    testDate1);
                query += string.Format(@", ('{0}','{1}','{2}','{3}','{4}','{5}')",
                    testDate2.Date,
                    testDate2.TimeOfDay,
                    testDate2,
                    testDate2,
                    testDate2,
                    testDate2);

                testDB.ExecuteNonQuery(query);

                // Scripting out data
                Scripter scripter = new Scripter(server);
                scripter.Options.ScriptData = true;
                scripter.Options.ScriptSchema = false;
                scripter.Options.NoCommandTerminator = true;
                UrnCollection urns = new UrnCollection();
                urns.Add(tbl1.Urn);
                IEnumerable<string> sc = scripter.EnumScript(urns);
                List<string> lines = new List<string>();
                lines.Add(string.Format(@"USE [{0}]", databaseName));
                lines.Add(string.Format(@"DELETE FROM [dbo].[{1}]", databaseName, tableName));

                // Validating values based
                foreach (string line in sc)
                {
                    if (line.Contains("INSERT"))
                    {
                        Assert.IsTrue(line.Contains(dateVal1) || line.Contains(dateVal2), "Data format is incorrect.");
                        Assert.IsTrue(line.Contains(timeVal1) || line.Contains(timeVal2), "Data format is incorrect.");
                        Assert.IsTrue(line.Contains(datetimeoffset1) || line.Contains(datetimeoffset2), "Data format is incorrect.");
                        Assert.IsTrue(line.Contains(datetime2Val1) || line.Contains(datetime2Val2), "Data format is incorrect.");
                        Assert.IsTrue(line.Contains(datetimeVal1) || line.Contains(datetimeVal2), "Data format is incorrect.");
                        Assert.IsTrue(line.Contains(smalldatetimeVal1) || line.Contains(smalldatetimeVal2), "Data format is incorrect.");
                        lines.Add(line);
                    }
                }

                // Executing script
                lines.Add(string.Format(@"SELECT COUNT(*) FROM [dbo].[{1}]", databaseName, tableName));
                string result = string.Join(";", lines);
                int count = (int)testDB.ExecutionManager.ConnectionContext.ExecuteScalar(result);
                Assert.AreEqual(count, 2, "Incorrect number of rows in the table.");
            }
            finally
            {
                server.KillDatabase(databaseName);
            }
        }

        /// <summary>
        /// Verification of there is no extrea ']' generated in the comment sections. Bug# 990706
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyNoEscapedNameInCommentsJobScript()
        {
            testNoEscapedNameinComments("Normal Job", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("Backup[abc]def2", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("]", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("]]", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("[", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("[[", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("[]", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("[]]]]]", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("[[[[]", "[Uncategorized (Local)]");
            testNoEscapedNameinComments("Job[Name]Te[stD[ef]]]Create", "[Uncategorized (Local)]");
        }

        private void testNoEscapedNameinComments(string jobName, string jobCategory)
        {
            string jobNameInHeader = "Job [" + jobName + "]";
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            Microsoft.SqlServer.Management.Smo.Agent.Job job = new Microsoft.SqlServer.Management.Smo.Agent.Job(server.JobServer, jobName);

            try
            {
                job.Category = jobCategory;
                ScriptingOptions options = new ScriptingOptions();
                options.IncludeHeaders = true;

                job.IsEnabled = true;
                job.StartStepID = 1;
                job.EventLogLevel = Microsoft.SqlServer.Management.Smo.Agent.CompletionAction.Never;
                job.EmailLevel = Microsoft.SqlServer.Management.Smo.Agent.CompletionAction.Never;
                job.NetSendLevel = Microsoft.SqlServer.Management.Smo.Agent.CompletionAction.Never;
                job.PageLevel = Microsoft.SqlServer.Management.Smo.Agent.CompletionAction.Never;
                job.DeleteLevel = Microsoft.SqlServer.Management.Smo.Agent.CompletionAction.Never;
                job.Description = "No description available.";
                job.OwnerLoginName = server.ConnectionContext.TrueLogin;

                StringCollection sc = job.Script(options);

                // Verify the correct name in the header.
                string header = null;
                foreach (string scriptText in sc)
                {
                    string[] lines = scriptText.Split('\n');
                    foreach (string s in lines)
                    {
                        string line = s.Trim();
                        if (line.Length == 0)
                        {
                            continue;
                        }

                        // Header is the first line start with "/*"
                        if (line.StartsWith("/*"))
                        {
                            header = line;
                            break;
                        }
                    }
                }

                Assert.IsTrue(header.Contains(jobNameInHeader), "Job: " + jobName + " not found.");

                // Run the script to create the job, then check the job to see 
                // if it is created by the script correctly.
                StringBuilder sb = new StringBuilder();
                foreach (string s in sc)
                {
                    sb.AppendLine(s);
                }
                server.ExecutionManager.ConnectionContext.ExecuteNonQuery(sb.ToString());

                job = server.JobServer.Jobs[jobName];
                Assert.IsNotNull(job, "Job was not created, check the job script.");
            }
            finally
            {
                job.DropIfExists();
            }
        }

        /// <summary>
        /// 1. Verify a table withTextFileGroup but no text/image column (even though one 
        /// of the columns is UDDT) won'tbe created. 
        /// 2. Verify a table withTextFileGroup and s UDDT based on text produces a script 
        /// with TEXTIMAGE_ON and the generated script creates a table. 
        /// VSTS# 854045
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyTextImageOnForUserDefinedDataType()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string dataDir = Context.TestEnvironment.TargetStandAloneSqlEnvironment.DataDirectory;
            string databaseName = "TestDb" + Guid.NewGuid();
            string tableName = "mytable";
            try
            {
                Database myDatabase = new Database(server, databaseName);
                myDatabase.Create();

                // Add a data file group for text/image
                FileGroup fg1 = new FileGroup(myDatabase, "Blob_Stuff");
                fg1.Create();
                DataFile df1 = new DataFile(fg1, "Data" + Guid.NewGuid());
                df1.FileName = dataDir + @"\Data" + Guid.NewGuid() + ".ndf";
                df1.Size = 3072;
                df1.Growth = 1024;
                df1.GrowthType = FileGrowthType.KB;
                df1.MaxSize = -1.0;

                myDatabase.Alter();

                // define a user data type "int"
                UserDefinedDataType userDefinedDataType1 = new UserDefinedDataType(myDatabase, "userdefined");
                userDefinedDataType1.SystemType = "int";  // UserdefinedDataType "int"
                userDefinedDataType1.Create();
                DataType dataTypeInt = new DataType(userDefinedDataType1);
                Table myTable = new Table(myDatabase, tableName);
                Column column1 = new Column(myTable, "myint", DataType.Int);
                myTable.Columns.Add(column1);
                Column column2 = new Column(myTable, "myblob", dataTypeInt);
                myTable.Columns.Add(column2);
                myTable.TextFileGroup = fg1.Name; // Text image on file group Blob_Stuff
                try
                {
                    // Verify that database engine will throw an exception when creating a table with TEXTIMAGE_ON
                    // but without an text/image column.
                    // Try to create a table with two columns, one int and one user defined "int" on Blob_Stuff.
                    myTable.Create();
                    Assert.Fail("Database engine should throw an exception, TEXTIMAGE_ON is used without text/image columns.");
                }
                catch(Exception e)
                {
                    // Ignore, this is as expected.
                    // Exception "Cannot use TEXTIMAGE_ON when a table has no text, ntext, image, varchar(max),
                    // nvarchar(max), non-FILESTREAM varbinary(max), xml or large CLR type columns."
                    TraceHelper.TraceInformation("Caught an exception as expected: " + e.ToString());
                }

                // change the user defined data type to "text";
                UserDefinedDataType userDefinedDataType2 = new UserDefinedDataType(myDatabase, "b");
                userDefinedDataType2.SystemType = "text";
                userDefinedDataType2.Create();
                column2.DataType = new DataType(userDefinedDataType2);

                // Script the table and verify TEXTIMAGE_ON
                ScriptingOptions options = new ScriptingOptions();
                options.IncludeHeaders = true;
                options.IncludeDatabaseContext = true;
                StringCollection sc = myTable.Script(options);
                bool textImageOn = false;
                foreach (string s in sc)
                {
                    if (s.Contains("TEXTIMAGE_ON [Blob_Stuff]"))
                    {
                        textImageOn = true;
                        break;
                    }
                }
                Assert.IsTrue(textImageOn, "TEXTIMAGE_ON is not set.");

                // Run the script and verify the table is created.
                StringBuilder sb = new StringBuilder();
                foreach (string s in sc)
                {
                    sb.AppendLine(s);
                }
                testDb.ExecuteNonQuery(sb.ToString());
                myDatabase.Tables.Refresh();
                myTable = myDatabase.Tables[tableName];
                Assert.IsNotNull(myTable, "Database table " + "\"" + tableName + "\" was not created by the script: " + sb.ToString());
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// This test is for VSTS# 858448. When scripting a table that has a unique clustered index that is compressed,
        /// the generated script should have only one "DATA_COMPRESSION = PAGE".
        /// Test If there is no data compression on a unique clustered index, the generated script should not include
        ///   "DATA_COMPRESSION = PAGE" and the script should create a table.
        /// </summary>
            
        [TestMethod,Ignore]
        public void VerifyNoDataCompressionScripted()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            string tableName = "mytable";
            Regex dataCompressionPattern = new Regex("DATA_COMPRESSION\\s*=\\s*PAGE");
            try
            {
                Database database = new Database(server, databaseName);
                database.Create();

                // Create a table without data compression
                Table table = createTableForDataCompressionTest(database, tableName, DataCompressionType.None);
                string script = scriptTableForDataCompressionTest(table);

                // Verify data compression is not scripted.
                Assert.IsFalse(dataCompressionPattern.IsMatch(script), "DATA_COMPRESSION is scripted. " + script);

                // Run the script and verify it creates a table
                table.Drop();
                testDb.ExecuteNonQuery(script);
                database.Tables.Refresh();
                table = database.Tables[tableName];
                Assert.IsNotNull(table, "Database table " + "\"" + tableName + "\" was not created by the script: " + script);
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// This test is for VSTS# 995561. 
        /// We should never script SET OFF statements after object creation.
        /// As a fix for this bug, we removed SET ANSI_NULLS OFF and 
        /// SET QUOTED_IDENTIFIER OFF from ddl triggers
        /// </summary>
        [TestMethod(LabRunCategory.Gql)]
        public void VerifyNoSetOFFScriptedForDDLTrigger()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            string triggerName = "TestDDLTrigger" + Guid.NewGuid();
            string query = string.Format(@"CREATE TRIGGER [{0}] ON DATABASE FOR CREATE_TABLE AS PRINT 'A new table!'", triggerName);

            try
            {
                Database database = new Database(server, databaseName);
                database.Create();
                database.ExecuteNonQuery(query);
                database.Triggers.Refresh();
                DatabaseDdlTrigger ddlTrigger = database.Triggers[triggerName];

                StringCollection batches = ddlTrigger.Script();
                foreach (string batch in batches)
                {
                    Assert.IsFalse(batch.Contains("ANSI_NULLS OFF"), "ANSI NULL OFF should not exist after trigger creation.");
                    Assert.IsFalse(batch.Contains("QUOTED_IDENTIFIER OFF"), "QUOTED_IDENTIFIER OFF should not exist after trigger creation.");
                }
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// This test is for VSTS# 858448. When scripting a table that has a unique clustered index that is compressed,
        /// the generated script should have only one "DATA_COMPRESSION = PAGE".
        /// Test If a unique clustered index is compressed, and the data compression is set in script option,
        ///   the generated script should include one "DATA_COMPRESSION = PAGE", and the script should create a table. 
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyDataCompressionScriptedOnlyOnce()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            string tableName = "mytable";
            string dataCompressionPattern = "DATA_COMPRESSION\\s*=\\s*PAGE";
            try
            {
                Database database = new Database(server, databaseName);
                database.Create();

                // Create a table with data compression
                Table table = createTableForDataCompressionTest(database, tableName, DataCompressionType.Page);
                string script = scriptTableForDataCompressionTest(table);

                // Verify data compression is scripted only once
                MatchCollection matches = Regex.Matches(script, dataCompressionPattern);
                Assert.IsTrue(1 == matches.Count, "DATA_COMPRESSION is not scripted. " + script);

                // Run the script and verify it creates a table
                table.Drop();
                testDb.ExecuteNonQuery(script);
                database.Tables.Refresh();
                table = database.Tables[tableName];
                Assert.IsNotNull(table, "Database table " + "\"" + tableName + "\" was not created by the script: " + script);
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        private Table createTableForDataCompressionTest(Database db, string tableName, DataCompressionType compression)
        {
            Table tb = new Table(db, tableName);
            Column col = new Column(tb, "Col1", DataType.Int);
            col.Nullable = false;
            tb.Columns.Add(col);

            Index idx = new Index(tb, "UQ");
            tb.Indexes.Add(idx);
            idx.IndexedColumns.Add(new IndexedColumn(idx, col.Name));
            idx.IndexKeyType = IndexKeyType.DriUniqueKey;
            idx.IsUnique = true;
            idx.IsClustered = true;

            PhysicalPartition pp = new PhysicalPartition(tb, 0, compression);
            tb.PhysicalPartitions.Add(pp);
            tb.Create();

            return tb;
        }

        private string scriptTableForDataCompressionTest(Table tb)
        {
            ScriptingOptions sp = new ScriptingOptions();
            sp.IncludeHeaders = true;
            sp.ScriptDataCompression = true;
            sp.ClusteredIndexes = true;
            sp.DriAllConstraints = true;
            sp.IncludeDatabaseContext = true;

            StringCollection sc = tb.Script(sp);
            StringBuilder sb = new StringBuilder();
            foreach (string s in sc)
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This test is for VSTS# 1167567. Previously the objects are scripted according to the order of
        /// Table -> view -> Clustered Index. This is changed, all clustered indexes will be scripted right after
        /// their parent objects.
        /// This test creates cluster indexed objects table1, table2 and view3. Scipt a single clustered index, verify
        /// it is scripted. Then script multiple table/viewes and clustered indexes. Drop and re-created them using the
        /// generated scripts. Verify that all the objects are created and the objects are scripted in pairs, that is,
        /// index follows their parent object.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyClusteredIndexScriptedImmediatlyAfterParentObject()
        {
            string tableOrViewPattern = "CREATE\\s+(TABLE|VIEW)\\s+(\\[dbo\\]\\.)?\\[[^0-9]+(?<num>\\d)\\]";
            string clusteredIndexPattern = "CREATE\\s+UNIQUE\\s+CLUSTERED\\s+INDEX\\s+\\[Index(?<num>\\d)\\]";
            string indexNumGroupName = "num";
            Regex tableViewRegex = new Regex(tableOrViewPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex clusteredIndexRegex = new Regex(clusteredIndexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
           
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            string table1Name = "Table1";  // pairs the <parent clusteredIndex> by the suffix number. Will look for the number to verify clustered index
                                           // scripted immediatly after their parent.
            string index1Name = "Index1";  // <Table1 Index1>
            string table2Name = "Table2";
            string index2Name = "Index2";  // <Table2 Index2> 
            string view3Name = "View3";
            string index3Name = "Index3";  // <View3 Index3>

            try
            {
                Database database = new Database(server, databaseName);
                database.Create();

                // Create Table1
                Table table1 = new Table(database, table1Name);
                Column col1 = new Column(table1, "ID", DataType.Int);
                col1.Nullable = false;
                col1.Identity = true;
                col1.IdentitySeed = 1;
                col1.IdentityIncrement = 1;
                table1.Columns.Add(col1);

                Column col2 = new Column(table1, "Description", DataType.VarChar(50));
                col2.Nullable = true;
                table1.Columns.Add(col2);

                Column col3 = new Column(table1, "Value", DataType.Money);
                col3.Nullable = false;
                table1.Columns.Add(col3);

                table1.Create();

                // Create clustered index1 on table1
                Index index1 = new Index(table1, index1Name);
                table1.Indexes.Add(index1);
                index1.IndexedColumns.Add(new IndexedColumn(index1, "ID"));
                index1.IsUnique = true;
                index1.IsClustered = true;
                index1.Create();

                // Create Table2
                Table table2 = new Table(database, table2Name);
                col1 = new Column(table2, "Count", DataType.Int);
                col1.Nullable = false;
                table2.Columns.Add(col1);

                col2 = new Column(table2, "Name", DataType.VarChar(50));
                col2.Nullable = true;
                table2.Columns.Add(col2);

                col3 = new Column(table2, "Value", DataType.Money);
                col3.Nullable = false;
                table2.Columns.Add(col3);

                table2.Create();

                // Create clustered index2 on table2
                Index index2 = new Index(table2, index2Name);
                table2.Indexes.Add(index2);
                index2.IndexedColumns.Add(new IndexedColumn(index2, "Name"));
                index2.IsUnique = true;
                index2.IsClustered = true;
                index2.Create();

                // Create view3
                View view3 = new View(database, view3Name);
                view3.TextHeader = "CREATE VIEW [dbo].[" + view3Name + "] with schemabinding AS";
                view3.TextBody = "SELECT Description, SUM(Value) as Sum_Value, COUNT_BIG(*) AS CountBig, Value FROM [dbo].[" + table1Name
                    + "] GROUP BY Description, Value";
                view3.Create();

                // Create a clustered index3 on view3
                Index index3 = new Index(view3, index3Name);
                view3.Indexes.Add(index3);
                index3.IndexedColumns.Add(new IndexedColumn(index3, "Description"));
                index3.IndexedColumns.Add(new IndexedColumn(index3, "Value"));
                index3.IsUnique = true;
                index3.IsClustered = true;
                index3.Create();

                // Generate scripts
                ScriptingOptions options = new ScriptingOptions();
                options.IncludeDatabaseContext = true;
                options.IncludeHeaders = true;
                options.ScriptBatchTerminator = true;
                options.BatchSize = 1;
                options.ScriptSchema = true;
                options.ClusteredIndexes = true;
                Scripter scripter = new Scripter(server);
                scripter.Options = options;

                // Script a index without a parent object. Verify it is scripted.
                SqlSmoObject[] singleIndexArr = { index2 };
                IEnumerable<string> singleIndexEnumerable = scripter.EnumScriptWithList(singleIndexArr);
                IEnumerator<string> singleIndexEnumerator = singleIndexEnumerable.GetEnumerator();
                bool singleIndexScripted = false;
                while (singleIndexEnumerator.MoveNext())
                {
                    string statement = singleIndexEnumerator.Current;
                    TraceHelper.TraceInformation(statement);
                    Match match = clusteredIndexRegex.Match(statement);
                    if (null != match && match.Success)
                    {
                        // It is scripted.
                        singleIndexScripted = true;
                        break;
                    }
                }
                Assert.IsTrue(singleIndexScripted, "Index without a parent object was not scripted.");

                // Script objects, pass in them in random order.
                SqlSmoObject[] objects = {table2, view3, index1, table1, index3, index2 };
                IEnumerable<string> enumerable = scripter.EnumScriptWithList(objects);
                IEnumerator<string> enumerator = enumerable.GetEnumerator();

                // Drop table and views, and run the generated scripts.
                index1.Drop();
                index2.Drop();
                index3.Drop();
                view3.Drop();
                table1.Drop();
                table2.Drop();
                List<string> statements = new List<string>();
                while (enumerator.MoveNext())
                {
                    string statement = enumerator.Current;
                    statements.Add(statement);
                    try
                    {
                        // run the script
                        testDb.ExecuteNonQuery(statement);
                    }
                    catch (Exception e)
                    {
                        Assert.Fail("An exception was thrown while running the script : \"" + statement + "\" " + e);
                    }
                }

                // Verify all the objects have been created.
                database.Tables.Refresh();
                table1 = database.Tables[table1Name];
                Assert.IsNotNull(table1, table1Name + " was not created by the script.");
                index1 = table1.Indexes[index1Name];
                Assert.IsNotNull(index1, index1Name + " was not created by the script.");
                table2 = database.Tables[table2Name];
                Assert.IsNotNull(table1, table2Name + " was not created by the script.");
                index2 = table2.Indexes[index2Name];
                Assert.IsNotNull(index2, index2Name + " was not created by the script.");
                database.Views.Refresh();
                view3 = database.Views[view3Name];
                Assert.IsNotNull(view3, view3Name + " was not created by the script.");
                index3 = view3.Indexes[index3Name];
                Assert.IsNotNull(index3, index3Name + " was not created by the script.");

                // Verify objects scripted in <parent clusteredIndex> pairs. Should find 3 pairs
                bool lookingForTableOrView = true;
                int numberOfPairsToBeFound = 3;
                string expectedIndexNumber = null;
                foreach (string statement in statements)
                {
                    if (lookingForTableOrView)
                    {
                        Match tableViewMatch = tableViewRegex.Match(statement);
                        if (null != tableViewMatch && tableViewMatch.Success)
                        {
                            lookingForTableOrView = false; // Look for index next.
                            expectedIndexNumber = tableViewMatch.Groups[indexNumGroupName].Value; // The index follows must have this number
                        }
                    }
                    else
                    {
                        Match indexMatch = clusteredIndexRegex.Match(statement);
                        if (null != indexMatch && indexMatch.Success)
                        {
                            lookingForTableOrView = true;
                            string indexNumberFound = indexMatch.Groups[indexNumGroupName].Value;
                            Assert.AreEqual(expectedIndexNumber, indexNumberFound);
                            numberOfPairsToBeFound--;
                        }
                    }
                }
                Assert.IsTrue(0 == numberOfPairsToBeFound, "At least one of the clustered indexes was not script with its parent.");
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// VSTS #904569 - keep the "if exists" logic as is in Denali; and only for SP and NSP creation, create 
        /// an empty SP/NSP using dynamic t-sql inside "if not exists" condition, then alter the SP/NSP with the actual body.
        /// Test steps:
        /// 1. Create a StoredProcedure.
        /// 2. Script the store procedure with IncludeIfNotExists to true
        /// 3. Verify the scripts matches the patterns - Empty header and Alter body
        /// 4. Run the script and verify the StoredProcedure is re-created.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyStoredProcedureScriptCreate()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            string headerPattern = "N'CREATE[     ]+PROCEDURE.+AS'";
            string bodyPattern = "ALTER.+PROCEDURE";
            try
            {
                Database database = new Database(server, databaseName);
                database.Create();

                StoredProcedure storedProcedure = new StoredProcedure(database, "storedProcedure");
                storedProcedure.TextMode = true;
                storedProcedure.TextHeader = "CREATE /* Comment   PROCEDURE storedProcedure @p1 int = 0, @p2 int = 0 AS";
                storedProcedure.TextBody = "BEGIN\n SET NOCOUNT ON;\n SELECT @p1, @p2 \n END";
                storedProcedure.Create();

                ScriptingOptions so = new ScriptingOptions();
                so.IncludeIfNotExists = true;
                StringCollection sc = storedProcedure.Script(so);
                storedProcedure.Drop();

                StringBuilder sb = new StringBuilder();
                foreach (string statement in sc)
                {
                    sb.AppendLine(statement);
                    TraceHelper.TraceInformation(statement);
                }
                string scripts = sb.ToString();
                // Verify the store procedure and numbered stored procedure are scripted in the right format, that is CREATE Header followed by ALTER body.
                MatchCollection matches = Regex.Matches(scripts, headerPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Assert.IsTrue(1 == matches.Count, "StoredProcedure header is not scripted correctly.");
                matches = Regex.Matches(scripts, bodyPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Assert.IsTrue(1 == matches.Count, "Stored procedure body is not scriupted correctly.");

                // Verify the script re-create the stored procedure.
                database.ExecuteNonQuery(sc);
                database.StoredProcedures.Refresh();
                Assert.IsNotNull(database.StoredProcedures["storedProcedure"], "Stored procedure was not created by the script.");
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

#region Data Masking Tests

        /// <summary>
        /// A small data class that contains configuration info for a table column in Data Masking tests.
        /// </summary>
        public class DataMaskingTableColumn
        {
            /// <summary>
            /// Name of the column to be created
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// SQL data type of the column to be created
            /// </summary>
            public DataType DataType { get; set; }
            /// <summary>
            /// Whether the column to be created has a data masking function applied to it or not.
            /// </summary>
            public bool IsMasked { get; set; }
            /// <summary>
            /// The data masking function to apply to the column. Ignored if IsMasked is false.
            /// </summary>
            public string MaskingFunction { get; set; }
            /// <summary>
            /// Whether the column to be created is nullable or not.
            /// </summary>
            public bool Nullable { get; set; }
            /// <summary>
            /// The collation to apply to the column, or none if null.
            /// </summary>
            public string Collation { get; set; }
            /// <summary>
            /// Whether the column to be created is an identity column or not.
            /// </summary>
            public bool Identity { get; set; }
            /// <summary>
            /// Whether the column to be created is sparse or not.
            /// </summary>
            public bool IsSparse { get; set; }
            /// <summary>
            /// Whether the column to be created is a ROWGUIDCOLUMN or not.
            /// </summary>
            public bool RowGuidCol { get; set; }
        }

        /// <summary>
        /// Check if SMO can successfully retrieve the metadata of an existing table with data masking functions applied to the columns.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyRetrieveDataMaskingTable()
        {
            SupportedSqlServer testServers = SmoTestHelpers.SQL2017_AND_AFTER_ONPREM | SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD;

            this.ExecuteWithDbDrop(
                testServers,
                database =>
                {
                    const string maskFunctionDefault = "default()";
                    const string maskFunctionEmail = "email()";
                    const string maskFunctionPartial = "partial(3, \"XXXX\", 4)";

                    DataMaskingTableColumn[] tableColumns =
                    {
                        new DataMaskingTableColumn() { Name = "c1",  DataType = DataType.Int,          IsMasked = true,  MaskingFunction = maskFunctionDefault, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c2",  DataType = DataType.Int,          IsMasked = true,  MaskingFunction = maskFunctionDefault, Nullable = false },
                        new DataMaskingTableColumn() { Name = "c3",  DataType = DataType.NVarChar(32), IsMasked = true,  MaskingFunction = maskFunctionEmail,   Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c4",  DataType = DataType.NVarChar(32), IsMasked = true,  MaskingFunction = maskFunctionPartial, Nullable = true  },
                    };

                    TraceHelper.TraceInformation("Creating Data Masking table");

                    Table tableToCreate = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());

                    foreach (DataMaskingTableColumn column in tableColumns)
                    {
                        Column c = new Column(tableToCreate, column.Name, column.DataType)
                        {
                            IsMasked = column.IsMasked,
                            MaskingFunction = column.MaskingFunction,
                            Nullable = column.Nullable,
                        };
                        tableToCreate.Columns.Add(c);
                    }

                    tableToCreate.Create();

                    string copiedTableName = DuplicateDataMaskingTable(tableToCreate);
                    StringCollection ddlTableCopy = database.Tables[copiedTableName].Script(new ScriptingOptions() { DriAll = true });
                    string otherTableName = copiedTableName + "2nd";
                    foreach (string s in ddlTableCopy)
                    {
                        string query = s.Replace(copiedTableName, otherTableName);
                        TraceHelper.TraceInformation(String.Format("Executing query: {0}", query));
                        database.ExecuteNonQuery(query);
                    }

                    database.Tables.Refresh();
                    tableToCreate.Drop();
                    database.Tables[copiedTableName].Drop();
                    database.Tables[otherTableName].Drop();
                    database.Tables.Refresh();

                    Assert.IsNull(database.Tables[tableToCreate.Name], String.Format("Failed to drop table {0}", tableToCreate.Name));
                    Assert.IsNull(database.Tables[copiedTableName], String.Format("Failed to drop table {0}", copiedTableName));
                    Assert.IsNull(database.Tables[otherTableName], String.Format("Failed to drop table {0}", otherTableName));
                }
            );
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully created and if
        /// metadata is properly retrieved by SMO (for 2017AndAfterOnPrem)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyCreateDataMaskingTable2017AndAfterOnPrem()
        {
            VerifyCreateDataMaskingTable(SmoTestHelpers.SQL2017_AND_AFTER_ONPREM);
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully created and if
        /// metadata is properly retrieved by SMO (for AzureSterlingV12)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyCreateDataMaskingTableAzureSterlingV12()
        {
            VerifyCreateDataMaskingTable(SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD);
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully created and if
        /// metadata is properly retrieved by SMO
        /// </summary>
        private void VerifyCreateDataMaskingTable(SupportedSqlServer testServers)
        {
            this.ExecuteWithDbDrop(
                testServers,
                database =>
                {
                    const string maskFunctionDefault = "default()";
                    const string maskFunctionEmail = "email()";
                    const string maskFunctionPartial = "partial(3, \"XXXX\", 4)";

                    DataMaskingTableColumn[] columns1 =
                    {
                        new DataMaskingTableColumn() { Name = "c1",  DataType = DataType.Int,          IsMasked = true,  MaskingFunction = maskFunctionDefault, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c2",  DataType = DataType.Int,          IsMasked = true,  MaskingFunction = maskFunctionDefault, Nullable = false },
                        new DataMaskingTableColumn() { Name = "c3",  DataType = DataType.NVarChar(32), IsMasked = true,  MaskingFunction = maskFunctionEmail,   Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c4",  DataType = DataType.NVarChar(32), IsMasked = true,  MaskingFunction = maskFunctionPartial, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c5",  DataType = DataType.NVarChar(32), IsMasked = true,  MaskingFunction = maskFunctionPartial, Nullable = true,  Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c6",  DataType = DataType.NVarChar(32), IsMasked = false, MaskingFunction = String.Empty,        Nullable = true,  Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c7",  DataType = DataType.Int,          IsMasked = true,  MaskingFunction = maskFunctionDefault, Nullable = false, Identity = true },
                        new DataMaskingTableColumn() { Name = "c8",  DataType = DataType.Int,          IsMasked = true,  MaskingFunction = maskFunctionDefault, Nullable = true,  IsSparse = true },
                        new DataMaskingTableColumn() { Name = "c9",  DataType = DataType.Int,          IsMasked = false, MaskingFunction = String.Empty,        Nullable = true,  IsSparse = true },
                        new DataMaskingTableColumn() { Name = "c10", DataType = DataType.UniqueIdentifier, IsMasked = true,  MaskingFunction = maskFunctionDefault, RowGuidCol = true },
                    };

                    DataMaskingTableColumn[] columns2 =
                    {
                        new DataMaskingTableColumn() { Name = "d1",  DataType = DataType.Int,              IsMasked = false, MaskingFunction = String.Empty, Nullable = false, Identity = true },
                        new DataMaskingTableColumn() { Name = "d2",  DataType = DataType.UniqueIdentifier, IsMasked = false, MaskingFunction = String.Empty, RowGuidCol = true },
                    };

                    List<Table> tables = new List<Table>();
                    List<DataMaskingTableColumn[]> tablesDefinition = new List<DataMaskingTableColumn[]>() { columns1, columns2 };

                    TraceHelper.TraceInformation("Creating Data Masking table");

                    foreach (DataMaskingTableColumn[] columns in tablesDefinition)
                    {
                        Table table = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());
                        tables.Add(table);

                        foreach (DataMaskingTableColumn column in columns)
                        {
                            Column c = new Column(table, column.Name, column.DataType)
                            {
                                IsMasked = column.IsMasked,
                                MaskingFunction = column.MaskingFunction,
                                Nullable = column.Nullable,
                                Identity = column.Identity,
                                IsSparse = column.IsSparse,
                                RowGuidCol = column.RowGuidCol,
                            };
                            if (!String.IsNullOrEmpty(column.Collation))
                            {
                                c.Collation = column.Collation;
                            }
                            table.Columns.Add(c);
                        }

                        table.Create();
                    }

                    foreach (Table table in tables)
                    {
                        table.Refresh();
                    }

                    // Validate metadata stuff is propagated to SMO correctly
                    //
                    for (int i = 0; i < tables.Count; i++)
                    {
                        ValidateDataMaskingTable(testServers, tables[i], tablesDefinition[i]);
                    }

                    // script the table once again, re-create it and validate
                    // (round-trip test)
                    //
                    for (int i = 0; i < tables.Count; i++)
                    {
                        string copiedTableName = DuplicateDataMaskingTable(tables[i]);
                        Table copiedTable = database.Tables[copiedTableName];
                        ValidateDataMaskingTable(testServers, copiedTable, tablesDefinition[i]);
                        copiedTable.Drop();
                    }

                    foreach (Table table in tables)
                    {
                        table.Drop();
                    }
                    database.Tables.Refresh();

                    foreach (Table table in tables)
                    {
                        Assert.IsNull(database.Tables[table.Name], String.Format("Failed to drop table {0}", table.Name));
                    }
                }
            );
        }

        /// <summary>
        /// Check that both Data Masking properties should be set for the table to be created.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyInvalidDataMaskingPropertiesUsage()
        {
            this.ExecuteWithDbDrop(
                SmoTestHelpers.SQL2017_AND_AFTER_ONPREM | SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD,
                database =>
                {
                    Table table1 = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());
                    Column c1 = new Column(table1, "c1", DataType.Int) { IsMasked = true };
                    table1.Columns.Add(c1);
                    AttemptCreatingDataMaskingTable(table1, false, "Cannot create a table with data masking without setting the MaskingFunction property.");

                    Table table2 = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());
                    Column c2 = new Column(table2, "c2", DataType.Int);
                    table2.Columns.Add(c2);
                    AttemptCreatingDataMaskingTable(table2, true, "Creating a simple table should succeed.");
                    c2.IsMasked = true;
                    AttemptAlterDataMaskingTable(table2, false, "Cannot alter a table adding data masking without setting the MaskingFunction property.");
                }
            );
        }

        /// <summary>
        /// Check that tables with invalid masking functions on its columns cannot be created.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyInvalidDataMaskingFunctions()
        {
            this.ExecuteWithDbDrop(
                SmoTestHelpers.SQL2017_AND_AFTER_ONPREM | SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD,
                database =>
                {
                    string[] invalidIntMaskFunctions = new string[] { "default", "creditcard()", "default(1, 2)", "partial(1, \"XX\", 3)", "email()" };
                    string[] invalidStringMaskFunctions = new string[] { "email(1)", "partial(1, 3)", "random(1, 5)" };

                    foreach (string maskFunction in invalidIntMaskFunctions)
                    {
                        Table table = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());
                        Column c = new Column(table, "c1", DataType.Int) { IsMasked = true, MaskingFunction = maskFunction };
                        table.Columns.Add(c);

                        AttemptCreatingDataMaskingTable(table, false, String.Format("Cannot create column with invalid masking function: {0}", maskFunction));
                    }

                    foreach (string maskFunction in invalidStringMaskFunctions)
                    {
                        Table table = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());
                        Column c = new Column(table, "c1", DataType.NVarChar(32)) { IsMasked = true, MaskingFunction = maskFunction };
                        table.Columns.Add(c);

                        AttemptCreatingDataMaskingTable(table, false, String.Format("Cannot create column with invalid masking function: {0}", maskFunction));
                    }
                }
            );
        }

        /// <summary>
        /// Check that tables with temporal columns used with data masking cannot be created.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyInvalidDataMaskingOnTemporalColumns()
        {
            this.ExecuteWithDbDrop(
                SmoTestHelpers.SQL2017_AND_AFTER_ONPREM,
                database =>
                {
                    Table table = new Table(database, "CurrentTable");

                    Column c1 = new Column(table, "c1", DataType.Int);
                    Column c2 = new Column(table, "SysStart", DataType.DateTime2(5)) { IsMasked = true, MaskingFunction = "default()" };
                    Column c3 = new Column(table, "SysEnd", DataType.DateTime2(5)) { IsMasked = true, MaskingFunction = "default()" };

                    table.Columns.Add(c1);
                    table.Columns.Add(c2);
                    table.Columns.Add(c3);

                    Index index = new Index(table, "pk_current");
                    index.IndexKeyType = IndexKeyType.DriPrimaryKey;

                    index.IndexedColumns.Add(new IndexedColumn(index, "c1"));
                    table.Indexes.Add(index);

                    c2.Nullable = false;
                    c3.Nullable = false;

                    // mark both columns as hidden
                    //
                    c2.IsHidden = true;
                    c3.IsHidden = true;

                    c2.GeneratedAlwaysType = GeneratedAlwaysType.AsRowStart;
                    c3.GeneratedAlwaysType = GeneratedAlwaysType.AsRowEnd;
                    table.AddPeriodForSystemTime(c2.Name, c3.Name, true);

                    AttemptCreatingDataMaskingTable(table, false, "Cannot add a data masking function on temporal columns");
                }
            );
        }

        /// <summary>
        /// Check that tables with computed columns used with data masking cannot be created.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyInvalidDataMaskingOnComputedColumns()
        {
            this.ExecuteWithDbDrop(
                SmoTestHelpers.SQL2017_AND_AFTER_ONPREM | SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD,
                database =>
                {
                    Table table1 = new Table(database, "ComputedColumnsSuccess");
                    Column c1 = new Column(table1, "c1", DataType.Int) { IsMasked = true, MaskingFunction = "default()" };
                    Column c2 = new Column(table1, "c2", DataType.Int) { IsMasked = true, MaskingFunction = "default()" };
                    Column c3 = new Column(table1, "c3", DataType.Int) { Computed = true, ComputedText = "c1 + c2" };
                    table1.Columns.Add(c1);
                    table1.Columns.Add(c2);
                    table1.Columns.Add(c3);
                    AttemptCreatingDataMaskingTable(table1, true, "Using Computed columns based on other columns with masking function should succeed");

                    Table table2 = new Table(database, "ComputedColumnsFail");
                    Column d1 = new Column(table2, "d1", DataType.Int) { IsMasked = true, MaskingFunction = "default()" };
                    Column d2 = new Column(table2, "d2", DataType.Int) { IsMasked = true, MaskingFunction = "default()" };
                    Column d3 = new Column(table2, "d3", DataType.Int) { IsMasked = true, MaskingFunction = "default()", Computed = true, ComputedText = "c1 + c2" };
                    table2.Columns.Add(d1);
                    table2.Columns.Add(d2);
                    table2.Columns.Add(d3);
                    AttemptCreatingDataMaskingTable(table2, false, "Cannot create a table with masking function on computed columns.");
                }
            );
        }

                /// <summary>
        /// Check if a table without data masking functions applied to the columns can be successfully altered to add
        /// data masking functions (for 2017AndAfterOnPrem)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyAlterColumnAddDataMasking2017AndAfterOnPrem()
        {
            VerifyAlterColumnAddDataMasking(SmoTestHelpers.SQL2017_AND_AFTER_ONPREM);
        }

        /// <summary>
        /// Check if a table without data masking functions applied to the columns can be successfully altered to add
        /// data masking functions (for AzureSterlingV12)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyAlterColumnAddDataMaskingAzureSterlingV12()
        {
            VerifyAlterColumnAddDataMasking(SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD);
        }

        /// <summary>
        /// Check if a table without data masking functions applied to the columns can be successfully altered to add
        /// data masking functions
        /// </summary>
        private void VerifyAlterColumnAddDataMasking(SupportedSqlServer testServers)
        {
            this.ExecuteWithDbDrop(
                testServers,
                database =>
                {
                    const string maskFunctionDefault = "default()";
                    const string maskFunctionEmail = "email()";

                    DataMaskingTableColumn[] columns =
                    {
                        new DataMaskingTableColumn() { Name = "c1",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c2",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = false },
                        new DataMaskingTableColumn() { Name = "c3",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c4",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = false },
                        new DataMaskingTableColumn() { Name = "c5",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = true,  Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c6",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = false, Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c7",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = false, Identity = true },
                        new DataMaskingTableColumn() { Name = "c8",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = true,  IsSparse = true },
                    };

                    TraceHelper.TraceInformation("Creating table without data masking");

                    Table table = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());

                    foreach (DataMaskingTableColumn column in columns)
                    {
                        Column c = new Column(table, column.Name, column.DataType)
                        {
                            Nullable = column.Nullable,
                            Identity = column.Identity,
                            IsSparse = column.IsSparse,
                            RowGuidCol = column.RowGuidCol,
                        };
                        if (!String.IsNullOrEmpty(column.Collation))
                        {
                            c.Collation = column.Collation;
                        }
                        table.Columns.Add(c);
                    }

                    table.Create();

                    TraceHelper.TraceInformation("Alter table columns to add data masking");

                    foreach (Column column in table.Columns)
                    {
                        column.IsMasked = true;
                        if (column.DataType.ToString() == DataType.Int.ToString())
                        {
                            column.MaskingFunction = maskFunctionDefault;
                        }
                        else
                        {
                            column.MaskingFunction = maskFunctionEmail;
                        }
                    }
                    table.Alter();

                    table.Refresh();

                    // Validate metadata stuff is propagated to SMO correctly
                    //
                    ValidateDataMaskingTable(testServers, table, columns);

                    table.Drop();
                    database.Tables.Refresh();
                    Assert.IsNull(database.Tables[table.Name], String.Format("Failed to drop table {0}", table.Name));
                }
            );
        }

                /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully altered to drop
        /// data masking functions (for 2017AndAfterOnPrem)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyAlterColumnDropDataMasking2017AndAfterOnPrem()
        {
            VerifyAlterColumnDropDataMasking(SmoTestHelpers.SQL2017_AND_AFTER_ONPREM);
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully altered to drop
        /// data masking functions (for AzureSterlingV12)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyAlterColumnDropDataMaskingAzureSterlingV12()
        {
            VerifyAlterColumnDropDataMasking(SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD);
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully altered to drop
        /// data masking functions
        /// </summary>
        private void VerifyAlterColumnDropDataMasking(SupportedSqlServer testServers)
        {
            this.ExecuteWithDbDrop(
                testServers,
                database =>
                {
                    const string maskFunctionDefault = "default()";
                    const string maskFunctionEmail = "email()";

                    DataMaskingTableColumn[] columns =
                    {
                        new DataMaskingTableColumn() { Name = "c1",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c2",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = false },
                        new DataMaskingTableColumn() { Name = "c3",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c4",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = false },
                        new DataMaskingTableColumn() { Name = "c5",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = true,  Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c6",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail, Nullable = false, Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c7",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = false, Identity = true },
                        new DataMaskingTableColumn() { Name = "c8",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = true,  IsSparse = true },
                    };

                    TraceHelper.TraceInformation("Creating table without data masking");

                    Table table = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());

                    foreach (DataMaskingTableColumn column in columns)
                    {
                        Column c = new Column(table, column.Name, column.DataType)
                        {
                            IsMasked = column.IsMasked,
                            MaskingFunction = column.MaskingFunction,
                            Nullable = column.Nullable,
                            Identity = column.Identity,
                            IsSparse = column.IsSparse,
                            RowGuidCol = column.RowGuidCol,
                        };
                        if (!String.IsNullOrEmpty(column.Collation))
                        {
                            c.Collation = column.Collation;
                        }
                        table.Columns.Add(c);
                    }

                    table.Create();

                    TraceHelper.TraceInformation("Alter table columns to drop data masking");

                    foreach (Column column in table.Columns)
                    {
                        column.IsMasked = false;
                    }
                    table.Alter();

                    table.Refresh();

                    // Remove the masking values from the DataMaskingTableColumn array to prepare it for validation
                    foreach (DataMaskingTableColumn column in columns)
                    {
                        column.IsMasked = false;
                        column.MaskingFunction = String.Empty;
                    }

                    // Validate metadata stuff is propagated to SMO correctly
                    //
                    ValidateDataMaskingTable(testServers, table, columns);

                    table.Drop();
                    database.Tables.Refresh();
                    Assert.IsNull(database.Tables[table.Name], String.Format("Failed to drop table {0}", table.Name));
                }
            );
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully altered to change
        /// data masking functions (for 2017AndAfterOnPrem)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyAlterColumnChangeDataMasking2017AndAfterOnPrem()
        {
            VerifyAlterColumnChangeDataMasking(SmoTestHelpers.SQL2017_AND_AFTER_ONPREM);
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully altered to change
        /// data masking functions (for AzureSterlingV12)
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyAlterColumnChangeDataMaskingAzureSterlingV12()
        {
            VerifyAlterColumnChangeDataMasking(SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD);
        }

        /// <summary>
        /// Check if a table with data masking functions applied to the columns can be successfully altered to change
        /// data masking functions
        /// </summary>
        private void VerifyAlterColumnChangeDataMasking(SupportedSqlServer testServers)
        {
            this.ExecuteWithDbDrop(
                testServers,
                database =>
                {
                    const string maskFunctionDefault = "default()";
                    const string maskFunctionEmail = "email()";
                    const string maskFunctionPartial = "partial(3, \"XXXX\", 4)";

                    DataMaskingTableColumn[] columns =
                    {
                        new DataMaskingTableColumn() { Name = "c1",  DataType = DataType.Int,          IsMasked = true, MaskingFunction = maskFunctionDefault, Nullable = false, Identity = true },
                        new DataMaskingTableColumn() { Name = "c2",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail,   Nullable = true  },
                        new DataMaskingTableColumn() { Name = "c3",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail,   Nullable = false },
                        new DataMaskingTableColumn() { Name = "c4",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail,   Nullable = true,  Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c5",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail,   Nullable = false, Collation = "Traditional_Spanish_ci_ai" },
                        new DataMaskingTableColumn() { Name = "c6",  DataType = DataType.NVarChar(32), IsMasked = true, MaskingFunction = maskFunctionEmail,   Nullable = true,  IsSparse = true },
                    };

                    TraceHelper.TraceInformation("Creating table without data masking");

                    Table table = new Table(database, "CurrentTable_" + CommonRandom.NextUInt32().ToString());

                    foreach (DataMaskingTableColumn column in columns)
                    {
                        Column c = new Column(table, column.Name, column.DataType)
                        {
                            IsMasked = column.IsMasked,
                            MaskingFunction = column.MaskingFunction,
                            Nullable = column.Nullable,
                            Identity = column.Identity,
                            IsSparse = column.IsSparse,
                            RowGuidCol = column.RowGuidCol,
                        };
                        if (!String.IsNullOrEmpty(column.Collation))
                        {
                            c.Collation = column.Collation;
                        }
                        table.Columns.Add(c);
                    }

                    table.Create();

                    TraceHelper.TraceInformation("Alter table columns to drop data masking");

                    foreach (Column column in table.Columns)
                    {
                        if (column.DataType.ToString() != DataType.Int.ToString())
                        {
                            column.MaskingFunction = maskFunctionPartial;
                        }
                    }
                    table.Alter();

                    table.Refresh();

                    // Chage the masking function in the DataMaskingTableColumn array to prepare it for validation
                    foreach (DataMaskingTableColumn column in columns)
                    {
                        if (column.DataType.ToString() != DataType.Int.ToString())
                        {
                            column.MaskingFunction = maskFunctionPartial;
                        }
                    }

                    // Validate metadata stuff is propagated to SMO correctly
                    //
                    ValidateDataMaskingTable(testServers, table, columns);

                    table.Drop();
                    database.Tables.Refresh();
                    Assert.IsNull(database.Tables[table.Name], String.Format("Failed to drop table {0}", table.Name));
                }
            );
        }

        private void ValidateDataMaskingTable(SupportedSqlServer testServers, Table table, DataMaskingTableColumn[] columns)
        {
            TraceHelper.TraceInformation(String.Format("Validating that metadata is propagated to SMO correctly for table {0}", table.Name));

            Assert.AreEqual(columns.Length, table.Columns.Count, String.Format("Expected {0} columns in the table.", columns.Length));

            for (int i = 0; i < columns.Length; i++)
            {
                DataMaskingTableColumn column = columns[i];

                Assert.AreEqual(column.Name, table.Columns[i].Name, "The column name {0} is not as expected", column.Name);
                Assert.AreEqual(column.DataType.Name, table.Columns[i].DataType.Name, "The column DataType is not as expected for column {0}", column.Name);
                Assert.AreEqual(column.IsMasked, table.Columns[i].IsMasked, "The column IsMasked property is not as expected for column {0}", column.Name);
                if (column.IsMasked)
                {
                    Assert.AreEqual(column.MaskingFunction, table.Columns[i].MaskingFunction, "The column MaskingFunction property is not as expected for column {0}", column.Name);
                }
                Assert.AreEqual(column.Nullable, table.Columns[i].Nullable, "The column Nullable property is not as expected for column {0}", column.Name);
                if (!String.IsNullOrEmpty(column.Collation))
                {
                    Assert.AreEqual(column.Collation.ToLowerInvariant(), table.Columns[i].Collation.ToLowerInvariant(), "The column Collation property is not as expected for column {0}", column.Name);
                }
                Assert.AreEqual(column.Identity, table.Columns[i].Identity, "The column Identity property is not as expected for column {0}", column.Name);
                Assert.AreEqual(column.IsSparse, table.Columns[i].IsSparse, "The column IsSparse property is not as expected for column {0}", column.Name);
                if ((testServers & SmoTestHelpers.AzureSterlingV12_AND_AFTER_CLOUD) == 0)
                {
                    // ROWGUIDCOL is not supported on Azure V12
                    Assert.AreEqual(column.RowGuidCol, table.Columns[i].RowGuidCol, "The column RowGuidCol property is not as expected for column {0}", column.Name);
                }
            }
        }

        /// <summary>
        /// Create a copy of the data masking table
        /// </summary>
        /// <param name="table">Data masking table.</param>
        /// <returns>The name of the newly created table.</returns>
        private string DuplicateDataMaskingTable(Table table)
        {
            // script table and try to recreate it using different name
            string newTableName = table.Name + "_New";

            TraceHelper.TraceInformation("Scripting data masking table");
            StringCollection ddlTable = table.Script(new ScriptingOptions() { DriAll = true });

            TraceHelper.TraceInformation(String.Format("Attempting to re-create data masking table under new name: {0}", newTableName));

            foreach (string s in ddlTable)
            {
                string query = s.Replace(table.Name, newTableName);
                TraceHelper.TraceInformation(String.Format("Executing query: {0}", query));
                table.Parent.ExecuteNonQuery(query);
            }

            TraceHelper.TraceInformation("Refreshing the list of tables");
            table.Parent.Tables.Refresh();

            return newTableName;
        }

        /// <summary>
        /// Creates a table and validates if the operation's outcome was as expected
        /// </summary>
        private void AttemptCreatingDataMaskingTable(Table table, bool shouldSucceed, string errorMessage)
        {
            try
            {
                table.Create();
                if (!shouldSucceed)
                {
                    Context.LogError(errorMessage);
                    Assert.Fail(String.Format("Creating the Data Masking table {0} should not have succeeded", table.Name));
                }
            }
            catch (SmoException e)
            {
                if (shouldSucceed)
                {
                    Context.LogError(errorMessage);
                    Assert.Fail(String.Format("Creating the Data Masking table {0} has thrown an exception.\nException: {1}", table.Name, e.Message));
                }
            }
        }

        /// <summary>
        /// Alters a table and validates if the operation's outcome was as expected
        /// </summary>
        private void AttemptAlterDataMaskingTable(Table table, bool shouldSucceed, string errorMessage)
        {
            try
            {
                table.Alter();
                if (!shouldSucceed)
                {
                    Context.LogError(errorMessage);
                    Assert.Fail(String.Format("Creating the Data Masking table {0} should not have succeeded", table.Name));
                }
            }
            catch (SmoException e)
            {
                if (shouldSucceed)
                {
                    Context.LogError(errorMessage);
                    Assert.Fail(String.Format("Creating the Data Masking table {0} has thrown an exception.\nException: {1}", table.Name, e.Message));
                }
            }
        }
#endregion

        /// <summary>
        /// Tests scripting, creating, altering, and dropping of CMKs via SMO.
        /// Test steps:
        /// 1. Create a new database.
        /// 2. Create a column master key with a certificate path and validate the creation with catalog views.
        /// 3. Script the column master key
        /// 4. Drop the column master key, and verify that it has been dropped.
        /// 5. Verify that the script contains the expected information.
        /// 6. Run the script and verify the column master key is re-created.
        /// 7. Finally drop the column master key and the database
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyColumnMasterKeyCreateDrop()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            const string cmkName = "cmk1";
            const string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
            const string cmkPath = "Current User/Personal/f2260f28d909d21c642a3d8e0b45a830e79a1420";
            const string cmkDefintionCountQuery = @"SELECT COUNT(*) FROM sys.column_master_keys";
            const string enableTCEDDLQuery = @"dbcc traceon(4051, -1)";

            try
            {
                // Step 1 - Create database
                //
                Database database = new Database(server, databaseName);
                database.Create();
                Connection.ChangeDatabase(databaseName);

                // Temporary - Enable the trace flag to execute TCE DDL
                //
                testDb.ExecuteNonQuery(enableTCEDDLQuery);

                // Step 2 - Create CMK
                //
                ColumnMasterKey cmk = new ColumnMasterKey(database, cmkName, cmkStoreProviderName, cmkPath);
                cmk.Create();
                Assert.AreEqual(1, (int)Connection.ExecuteScalar(cmkDefintionCountQuery), "Column master key was not created.");

                // Step 3 - Script the CMK creation
                //
                StringCollection sc = cmk.Script();
                StringBuilder sb = new StringBuilder();
                foreach (string statement in sc)
                {
                    sb.AppendLine(statement);
                    TraceHelper.TraceInformation(statement);
                }

                string scripts = sb.ToString();

                // Step 4 - Drop CMK
                //
                cmk.Drop();
                database.ColumnMasterKeys.Refresh();
                Assert.AreEqual(0, database.ColumnMasterKeys.Count);

                //Step 5. Validate that the script contains the expected information.
                //
                Assert.IsTrue(scripts.Contains(String.Format(@"CREATE COLUMN MASTER KEY [{0}]", cmkName)));
                Assert.IsTrue(scripts.Contains(String.Format(@"KEY_STORE_PROVIDER_NAME = N'{0}'", cmkStoreProviderName)));
                Assert.IsTrue(scripts.Contains(String.Format(@"KEY_PATH = N'{0}'", cmkPath)));

                // Step 6. Verify the script recreates the column master key.
                //
                testDb.ExecuteNonQuery(scripts);
                database.ColumnMasterKeys.Refresh();

                cmk = database.ColumnMasterKeys[cmkName];
                Assert.IsNotNull(cmk, "Column master key was not recreated by the script.");
                Assert.AreEqual(cmkName, cmk.Name, "Recreated column master key name does not match the original Name.");
                Assert.AreEqual(cmkStoreProviderName, cmk.KeyStoreProviderName, "Recreated column master key does not have the same value for KeyStoreProviderName.");
                Assert.AreEqual(cmkPath, cmk.KeyPath, "Recreated column master key does not have the same value for KeyPath.");

                // Step 7. Finally test that ColumnMasterKey.Drop() behaves as expected.
                //
                cmk.Drop();
                database.ColumnMasterKeys.Refresh();
                Assert.AreEqual(0, database.ColumnMasterKeys.Count, "There should be no column master keys present in the database.");
                Assert.AreEqual(0, (int)Connection.ExecuteScalar(cmkDefintionCountQuery), "There should be no column master keys present in the database.");
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// Tests scripting, creating, altering, and dropping of CEKs via SMO.
        /// Test steps:
        /// 1. Create test objects.
        /// 2. Try to create a CEK without any CEK values
        /// 3. Create CEK with two cek Values
        /// 4. Drop the column encryption key, and verify that the count is 0.
        /// 5. Verify that the script contains the expected information.
        /// 6. Run the script and verify the column encryption key is re-created.
        /// 7. Perform a multi-operation ColumnEncryptionKey.Alter including: Adding, dropping, and adding a cek value and altering the CEK.
        /// 8. Finally drop the column encryption key and the database
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyColumnEncryptionKeyCreateAlterDrop()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            const string cmk1Name = "cmk1";
            const string cmk2Name = "cmk2";
            const string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
            const string cmkPath = "Current User/Personal/f2260f28d909d21c642a3d8e0b45a830e79a1420";
            const string cmkDefintionCountQuery = @"SELECT COUNT(*) FROM sys.column_master_keys";
            const string cekName = "cek1";
            const string encryptionAlgorithm = "rsa_oaep";
            byte[] cekEncryptedValue1 = GenerateRandomBytes(32);
            byte[] cekEncryptedValue2 = GenerateRandomBytes(32);
            const string cekCountQuery = @"SELECT COUNT(*) FROM sys.column_encryption_keys";
            const string cekValueCountQuery = @"SELECT COUNT(*) FROM sys.column_encryption_key_values";
            const string enableTCEDDLQuery = @"dbcc traceon(4051, -1)";
            
            try
            {
                // Step 1. Create test objects.
                //
                Database database = new Database(server, databaseName);
                database.Create();
                Connection.ChangeDatabase(databaseName);

                // Temporary - Enable the trace flag to execute TCE DDL
                //
                testDb.ExecuteNonQuery(enableTCEDDLQuery);

                ColumnMasterKey cmk1 = new ColumnMasterKey(database, cmk1Name, cmkStoreProviderName, cmkPath);
                cmk1.Create();
                ColumnMasterKey cmk2 = new ColumnMasterKey(database, cmk2Name, cmkStoreProviderName, cmkPath);
                cmk2.Create();
                database.ColumnMasterKeys.Refresh();
                Assert.AreEqual(2, (int)Connection.ExecuteScalar(cmkDefintionCountQuery), "Column master key were not created.");

                // Step 2 - Try to create a CEK without any CEK values
                //
                ColumnEncryptionKey cek = new ColumnEncryptionKey(database, cekName);

                try
                {
                    cek.Create();
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e.ToString().Contains("Must specify at least one Column Encryption Key value before creation"));
                }

                // Step 3 - Create CEK with two cek Values
                //
                cmk1 = database.ColumnMasterKeys[cmk1Name];
                cmk2 = database.ColumnMasterKeys[cmk2Name];
                ColumnEncryptionKeyValue cekValue1 = new ColumnEncryptionKeyValue(cek, cmk1, encryptionAlgorithm, cekEncryptedValue1);
                ColumnEncryptionKeyValue cekValue2 = new ColumnEncryptionKeyValue(cek, cmk2, encryptionAlgorithm, cekEncryptedValue2);
                cek.ColumnEncryptionKeyValues.Add(cekValue1);
                cek.ColumnEncryptionKeyValues.Add(cekValue2);
                cek.Create();

                database.ColumnEncryptionKeys.Refresh();
                cek = database.ColumnEncryptionKeys[cekName];
                Assert.AreEqual(1, (int)Connection.ExecuteScalar(cekCountQuery), "Column encryption key was not created.");
                Assert.AreEqual(2, cek.ColumnEncryptionKeyValues.Count, "Column encryption key does not have two encrypted values.");

                // Validate the CEK values
                cekValue1 = cek.ColumnEncryptionKeyValues.GetItemByColumnMasterKeyID(cmk1.ID);
                Assert.AreEqual(encryptionAlgorithm, cekValue1.EncryptionAlgorithm, "Created column encryption key value does not have the correct encryption algorithm.");
                Assert.AreEqual(GetHexString(cekEncryptedValue1), GetHexString(cekValue1.EncryptedValue), "Created column encryption key value does not have the correct encrypted value.");

                cekValue2 = cek.ColumnEncryptionKeyValues.GetItemByColumnMasterKeyID(cmk2.ID);
                Assert.AreEqual(encryptionAlgorithm, cekValue2.EncryptionAlgorithm, "Created column encryption key value does not have the correct encryption algorithm.");
                Assert.AreEqual(GetHexString(cekEncryptedValue1), GetHexString(cekValue2.EncryptedValue), "Created column encryption key value does not have the correct encrypted value.");

                // Script the column encryption key with IncludeIfNotExists to true
                //
                ScriptingOptions so = new ScriptingOptions();
                so.IncludeIfNotExists = true;
                so.IncludeDatabaseContext = true;
                so.ExtendedProperties = true;
                StringCollection sc = cek.Script(so);

                // Step 4. Drop the column encryption key, and verify that the count is 0.
                //
                cek.Drop();
                database.ColumnEncryptionKeys.Refresh();
                Assert.AreEqual(0, database.ColumnEncryptionKeys.Count);

                //Step 5. Validate the script contains the expected information.
                //
                StringBuilder sb = new StringBuilder();
                foreach (string statement in sc)
                {
                    sb.AppendLine(statement);
                    TraceHelper.TraceInformation(statement);
                }

                string scripts = sb.ToString();
                Assert.IsTrue(scripts.Contains(String.Format("CREATE COLUMN ENCRYPTION KEY [{0}]", cekName)));
                Assert.IsTrue(scripts.Contains(String.Format("COLUMN_MASTER_KEY = [{0}]", cmk1Name)));
                Assert.IsTrue(scripts.Contains(String.Format("ALGORITHM = '{0}'", encryptionAlgorithm)));
                Assert.IsTrue(scripts.Contains(String.Format("ENCRYPTED_VALUE = {0}", GetHexString(cekEncryptedValue1))));

                // Step 6. Verify the script recreates the column encryption key.
                //
                testDb.ExecuteNonQuery(scripts);
                database.ColumnEncryptionKeys.Refresh();
                cek = database.ColumnEncryptionKeys[cekName];
                Assert.IsNotNull(cek, "Column Encryption Key was not recreated by the script.");
                Assert.AreEqual(cekName, cek.Name, "Recreated column encryption key name does not match the original column encryption key name.");
                Assert.AreEqual(2, cek.ColumnEncryptionKeyValues.Count, "Recreated column encryption key does not have two encrypted values.");

                // Validate the CEK values
                cekValue1 = cek.ColumnEncryptionKeyValues.GetItemByColumnMasterKeyID(cmk1.ID);
                Assert.AreEqual(encryptionAlgorithm, cekValue1.EncryptionAlgorithm, "Recreated column encryption key value does not have the correct encryption algorithm.");
                Assert.AreEqual(GetHexString(cekEncryptedValue1), GetHexString(cekValue1.EncryptedValue), "Recreated column encryption key value does not have the correct encrypted value.");

                cekValue2 = cek.ColumnEncryptionKeyValues.GetItemByColumnMasterKeyID(cmk2.ID);
                Assert.AreEqual(encryptionAlgorithm, cekValue2.EncryptionAlgorithm, "Recreated column encryption key value does not have the correct encryption algorithm.");
                Assert.AreEqual(GetHexString(cekEncryptedValue1), GetHexString(cekValue2.EncryptedValue), "Recreated column encryption key value does not have the correct encrypted value.");

                // Step 7 - Perform a multi-operation ColumnEncryptionKey.Alter including: Adding, dropping, and adding a cek value and altering the CEK.
                //
                // Delete a cek value using ColumnEncryptionKeyValue.Drop()
                cekValue2 = cek.ColumnEncryptionKeyValues.GetItemByColumnMasterKeyID(cmk2.ID);
                cekValue2.Drop();
                database.ColumnEncryptionKeys.Refresh();
                cek = database.ColumnEncryptionKeys[cekName];
                Assert.AreEqual(1, cek.ColumnEncryptionKeyValues.Count, "ColumnEncryptionKeyValue.Drop() did not delete an encrypted value.");

                // Add a CEK using ColumnEncryptionKeyValue.Create()
                cekValue2 = new ColumnEncryptionKeyValue(cek, cmk2, encryptionAlgorithm, cekEncryptedValue2);
                cekValue2.Create();
                cek.ColumnEncryptionKeyValues.Refresh();
                Assert.AreEqual(2, cek.ColumnEncryptionKeyValues.Count, "ColumnEncryptionKeyValue.Create() did not add an encrypted value.");

                // Delete a cek value using ColumnEncryptionKey.Alter()
                cekValue1 = cek.ColumnEncryptionKeyValues.GetItemByColumnMasterKeyID(cmk1.ID);
                cekValue1.MarkForDrop(dropOnAlter:true);
                cek.Alter();
                database.ColumnEncryptionKeys.Refresh();
                cek = database.ColumnEncryptionKeys[cekName];
                Assert.AreEqual(1, cek.ColumnEncryptionKeyValues.Count, "ColumnEncryptionKey.Alter() did not delete an encrypted value.");
                Assert.AreEqual(SqlSmoState.Dropped, cekValue1.State, "ColumnEncryptionKeyValye.State was not updated via ColumnEncryptionKey.Alter().");

                // Add it back using ColumnEncryptionKey.Alter
                cekValue1 = new ColumnEncryptionKeyValue(cek, cmk1, encryptionAlgorithm, cekEncryptedValue1);
                cek.ColumnEncryptionKeyValues.Add(cekValue1);
                cek.Alter();
                Assert.AreEqual(2, cek.ColumnEncryptionKeyValues.Count, "ColumnEncryptionKey.Alter() did not create an encrypted value.");
                Assert.AreEqual(SqlSmoState.Existing, cekValue1.State, "ColumnEncryptionKeyValye.State was not updated to Existing via ColumnEncryptionKey.Alter().");

                // Step 8 Finally test that ColumnEncryptionKeyValue.Drop() and ColumnEncryptionKey.Drop() behave as expected.
                //
                cekValue1.Drop();
                cek.ColumnEncryptionKeyValues.Refresh();
                Assert.AreEqual(1, (int)Connection.ExecuteScalar(cekValueCountQuery), "There should be 1 cek value present in the database after the explicit drop.");
                Assert.AreEqual(1, cek.ColumnEncryptionKeyValues.Count, "ColumnEncryptionKeyValue.Drop() did not delete an encrypted value.");

                cek.Drop();
                database.ColumnEncryptionKeys.Refresh();
                Assert.AreEqual(0, (int)Connection.ExecuteScalar(cekCountQuery), "There should be 1 cek value present in the database after the explicit drop.");
                Assert.AreEqual(0, database.ColumnEncryptionKeys.Count, "ColumnEncryptionKey.Drop() did not delete an encrypted key.");
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// Tests scripting, creating, dropping of TCE enabled columns via SMO.
        /// Test steps:
        /// 1. Create test objects.
        /// 2. Create a table with a TCE enabled column
        /// 2a. Try creating a column with incomplete properties
        /// 3. Validate the added columns
        /// 4. Drop the table and recreate the table with the script and validate again.
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyCreateColumnWithEncryptionKey()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            const string cmk1Name = "cmk1";
            const string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
            const string cmkPath = "Current User/Personal/f2260f28d909d21c642a3d8e0b45a830e79a1420";
            const string cekName = "cek1";
            const string rsaAlgorithm = "rsa_oaep";
            byte[] cekEncryptedValue1 = GenerateRandomBytes(32);
            const string enableTCEDDLQuery = @"dbcc traceon(4051, -1)";
            const string tableName1 = "cekTable1";
            const string plainTextColumnName = "plainTextColumn";
            const string encryptedColumnName = "encryptedColumn";
            const string aesAlgorithm = "AEAD_AES_256_CBC_HMAC_SHA_256";
            DataType dt = new DataType(SqlDataType.Int);

            try
            {
                // Step 1. Create test objects.
                //
                Database database = new Database(server, databaseName);
                database.Create();
                Connection.ChangeDatabase(databaseName);

                // Temporary - Enable the trace flag to execute TCE DDL
                testDb.ExecuteNonQuery(enableTCEDDLQuery);

                // Create CMK
                ColumnMasterKey cmk1 = new ColumnMasterKey(database, cmk1Name, cmkStoreProviderName, cmkPath);
                cmk1.Create();
                database.ColumnMasterKeys.Refresh();

                // Create CEK
                ColumnEncryptionKey cek = new ColumnEncryptionKey(database, cekName);
                ColumnEncryptionKeyValue cekValue1 = new ColumnEncryptionKeyValue(cek, cmk1, rsaAlgorithm, cekEncryptedValue1);
                cek.ColumnEncryptionKeyValues.Add(cekValue1);
                cek.Create();

                // Step 2 - Create a table with a TCE enabled column
                //
                Table table1 = new Table(database, tableName1);
                Column plainTextColumn = new Column(table1, plainTextColumnName, dt);
                table1.Columns.Add(plainTextColumn);

                Column encryptedColumn = new Column(table1, encryptedColumnName, dt);
                encryptedColumn.ColumnEncryptionKeyName = cekName;
                encryptedColumn.EncryptionType = ColumnEncryptionType.Deterministic;
                encryptedColumn.EncryptionAlgorithm = aesAlgorithm;
                table1.Columns.Add(encryptedColumn);
                table1.Create();
                database.Tables.Refresh();

                // Step 2a - Try creating a column with incomplete properties
                //
                Column encryptedColumn1 = new Column(table1, "invalidColumn", dt);
                encryptedColumn1.ColumnEncryptionKeyName = cekName;
                table1.Columns.Add(encryptedColumn1);

                try
                {
                    table1.Alter();
                }
                catch(Exception e)
                {
                    Assert.IsTrue(e.InnerException.ToString().Contains("All Column.ColumnEncryptionKeyName, Column.EncryptionAlgorithm and Column.EncryptionType must be set to enable Encryption on the column."));
                }

                // Remove the invalid column from the collections.
                table1.Columns.Remove(encryptedColumn1);

                // Step 3 - Validate the added columns
                //
                database.Tables.Refresh();
                table1 = database.Tables[tableName1];

                Assert.IsNotNull(table1, "Table.Create() did not create a table with proper table name.");
                Assert.AreEqual(2, table1.Columns.Count, "Table.Create() did not create two columns.");

                // Validate the plaintext column
                plainTextColumn = table1.Columns[plainTextColumnName];
                Assert.IsFalse(plainTextColumn.IsEncrypted, "Plaintext column's IsEncrypted value is true, but should be false.");

                // Validate the added encrypted column
                encryptedColumn = table1.Columns[encryptedColumnName];
                Assert.AreEqual(cek.Name, encryptedColumn.ColumnEncryptionKeyName, "Created encrypted column does not have the proper column encryption key name.");
                Assert.AreEqual(ColumnEncryptionType.Deterministic, encryptedColumn.EncryptionType, "Created encrypted column does not have the proper column encryption type.");
                Assert.AreEqual(aesAlgorithm, encryptedColumn.EncryptionAlgorithm, "Created encrypted column does not have the proper column encryption algorithm.");
                Assert.IsTrue(encryptedColumn.IsEncrypted, "Encrypted column's IsEncrypted value is false, but should be true.");

                // Validate the CEK knows which columns it's encrypting
                Assert.AreEqual(cek.GetColumnsEncrypted().Count, 1, "Incorrect number of columns the CEK thinks it encrypts");
                Assert.AreEqual(cek.GetColumnsEncrypted()[0].Name, encryptedColumn.Name, "CEK's encrypted column's name doesn't match the actual encrypted column");
                Assert.AreEqual((cek.GetColumnsEncrypted()[0].Parent as Table).Name, table1.Name, "CEK's encrypted column's containing table's name doesn't match the correct table");

                // Validate the CMK knows which CEK values it's encrypting
                Assert.AreEqual(cmk1.GetColumnEncryptionKeyValuesEncrypted().Count, 1, "Incorrect number of CEK values the CMK thinks it encrypts");
                Assert.AreEqual(cmk1.GetColumnEncryptionKeyValuesEncrypted()[0].ColumnEncryptionKeyName, cek.Name, "CMK's encrypted CEK value's name doesn't match the actual CEK");

                // Get the script to create the table
                StringCollection scTable = table1.Script();
                StringBuilder sbTable = new StringBuilder();
                foreach (string statement in scTable)
                {
                    sbTable.AppendLine(statement);
                    TraceHelper.TraceInformation(statement);
                }

                string createTableScript = sbTable.ToString();
                Assert.IsTrue(createTableScript.Contains(String.Format("ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = [{0}], ENCRYPTION_TYPE = {1}, ALGORITHM = '{2}')", cekName, ColumnEncryptionType.Deterministic, aesAlgorithm)));

                // Step 4 - Drop the table and recreate the table with the script and validate again.
                //
                table1.Drop();
                testDb.ExecuteNonQuery(createTableScript);
                database.Tables.Refresh();

                // Validate the recreated table
                table1 = database.Tables[tableName1];
                Assert.IsNotNull(table1, "Recreated table did not create a table with proper table name.");
                Assert.AreEqual(2, table1.Columns.Count, "Recreated table did not create with two columns.");

                // Validate the plaintext column
                plainTextColumn = table1.Columns[plainTextColumnName];

                // Validate the added encrypted column
                encryptedColumn = table1.Columns[encryptedColumnName];
                Assert.AreEqual(cek.Name, encryptedColumn.ColumnEncryptionKeyName, "Created encrypted column does not have the proper column encryption key name.");
                Assert.AreEqual(ColumnEncryptionType.Deterministic, encryptedColumn.EncryptionType, "Created encrypted column does not have the proper column encryption type.");
                Assert.AreEqual(aesAlgorithm, encryptedColumn.EncryptionAlgorithm, "Created encrypted column does not have the proper column encryption algorithm.");

                // Validate the drop column functionality.
                encryptedColumn.Drop();
                table1.Columns.Refresh();
                Assert.AreEqual(1, table1.Columns.Count, "Column.Drop() did not drop the encrypted columns");

                table1.Drop();
                database.Tables.Refresh();
                Assert.AreEqual(0, database.Tables.Count, "Table.Drop() did not drop the table containing encrypted columns");
            }
            finally
            {
                Connection.ChangeDatabase("master");
                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// Generates cryptographicall random bytes
        /// </summary>
        /// <param name="length">No of cryptographically random bytes to be generated</param>
        /// <returns>A byte array containing cryptographically generated random bytes</returns>
        internal static byte[] GenerateRandomBytes(int length)
        {
            // Generate random bytes cryptographically.
            byte[] randomBytes = new byte[length];
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(randomBytes);

            return randomBytes;
        }

        /// <summary>
        /// Gets hex representation of byte array.
        /// <param name="input">input byte array</param>
        /// </summary>
        internal static string GetHexString(byte[] input)
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"0x");

            foreach (byte b in input)
            {
                str.AppendFormat(b.ToString(@"X2"));
            }

            return str.ToString();
        }

        /// <summary>
        /// Test if indexes with filter predicate can be successfully created and scripted.
        /// Cover both positive tests and negative tests. 
        /// Positive: nonclustered coloumnstore|rowstore indexes, 
        /// Negative: clustered columnstore|rowstore indexes
        /// </summary>
        [TestMethod,Ignore]
        public void VerifyIndexCreateWithFilter()
        {
            // create test database
            //
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(this.ServerName);
            string databaseName = "TestDb" + Guid.NewGuid();
            Database database = new Database(server, databaseName);
            database.Create();

            try
            {
                VerifyIndexCreateWithFilterHelper(database, "t1", IndexType.NonClusteredIndex, false);
                VerifyIndexCreateWithFilterHelper(database, "t2", IndexType.NonClusteredColumnStoreIndex, false);
                VerifyIndexCreateWithFilterHelper(database, "t3", IndexType.ClusteredIndex, true);
                VerifyIndexCreateWithFilterHelper(database, "t4", IndexType.ClusteredColumnStoreIndex, true);
            }
            finally
            {
                // drop test database
                //
                SmoTestHelpers.DropDatabase(server, databaseName);
            }

        }

        /// <summary>
        /// The helper function for VerifyIndexCreateWithFilter. It does the following:
        /// 1. create a table
        /// 2. create an index
        /// 3. script index and verify if filter is scripted
        /// </summary>
        /// <param name="db">the database</param>
        /// <param name="tableName">the table name</param>
        /// <param name="indexType">the type of index</param>
        /// <param name="exceptionExpected">if an exception is expected in this test</param>
        private void VerifyIndexCreateWithFilterHelper(Database db, string tableName, IndexType indexType, bool exceptionExpected)
        {
            const string columnName = "col1";
            const string filter = "col1<100";
            const string indexName = "idx";

            // create table
            //
            Table t = new Table(db, tableName);
            t.Columns.Add(new Column(t, columnName, DataType.Int));
            t.Create();

            try
            {
                Index i = CreateIndexWithFilter(t, indexType, indexName, filter);
                ScriptAndVerifyIndexWithPredicate(i);
                if (exceptionExpected)
                {
                    Assert.Fail("Exception is expected because filter predicate is not supported for clustered index");				    
                }
            }
            catch (Exception)
            {
                if (exceptionExpected)
                {
                    TraceHelper.TraceInformation("Exception is expected because filter predicate is not supported for clustered index");				    
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Create index with filter predicate
        /// </summary>
        /// <param name="t">the table</param>
        /// <param name="indexType">the type of index to create</param>
        /// <param name="indexName">the index name</param>
        /// <param name="filter">the filter predicate</param>
        /// <returns>the created index</returns>
        private Index CreateIndexWithFilter(Table t, IndexType indexType, string indexName, string filter)
        {
            Index i = new Index(t, indexName);
            i.FilterDefinition = filter;
            i.IndexType = indexType;
            
            if (indexType == IndexType.ClusteredColumnStoreIndex || indexType == IndexType.ClusteredIndex)
            {
                i.IsClustered = true;
            }
            else
            {
                i.IsClustered = false;
            }

            if (indexType != IndexType.ClusteredColumnStoreIndex)
            {
                i.IndexedColumns.Add(new IndexedColumn(i, t.Columns[0].Name));			    
            }
            i.Create();

            return i;
        }

        /// <summary>
        /// Script the index and verify if the filter predicated is scripted
        /// </summary>
        /// <param name="i">the index</param>
        private void ScriptAndVerifyIndexWithPredicate(Index i)
        {
            ScriptingOptions sp = new ScriptingOptions();
            sp.IncludeHeaders = true;

            StringCollection sc = i.Script(sp);
            StringBuilder sb = new StringBuilder();
            foreach (string s in sc)
            {
                sb.AppendLine(s);
            }

            string script = sb.ToString();
            Assert.IsTrue(script.Contains("WHERE"));
        }

        /// <summary>
        /// Checks if the specified exception is the expected exception
        /// based on the exception message.
        /// </summary>
        /// <param name="e">Exception to check.</param>
        /// <param name="errorMessage">Expected exception message.</param>
        /// <returns>True, if the expected exception.  False otherwise.</returns>
        private bool IsExpectedException(Exception e, string errorMessage)
        {
            bool expectedException = false;

            Context.LogError("The operation thrown an exception.");

            while (e != null)
            {
                Context.LogError("\tError message:\n\r{0}", e.Message);
                Context.LogError("\tException stack trace:\n\r{0}", e.StackTrace);

                // validate expected error message
                if (e.Message.Equals(errorMessage, StringComparison.OrdinalIgnoreCase))
                {
                    expectedException = true;
                    break;
                }
                
                e = e.InnerException;           
            }

            // if the expected error message was not found, the excepted was unexpected,
            // rethrow and terminate execution
            if (!expectedException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This test verifies that SMO can correctly add/drop columnstore index from Hekaton table, script it back out
        /// and recreate it from the script.
        /// </summary>
        [TestMethod,Ignore]
        public void AlterHkCsTableAddDropColumnstoreIndex()
        {
            this.ExecuteWithDbDrop(
                SmoTestHelpers.SQL2017_AND_AFTER_ONPREM,
                database => 
                {
                    string tableName = this.TestContext.TestName;

                    FileGroup memoryOptimizedFg = new FileGroup(database,
                                String.Format("{0}_hkfg", database.Name),
                                FileGroupType.MemoryOptimizedDataFileGroup);

                    memoryOptimizedFg.Create();

                    DataFile dataFile = new DataFile(memoryOptimizedFg, String.Format("{0}_hkfg", database.Name))
                    {
                        FileName =
                            Path.Combine(Path.GetDirectoryName(database.FileGroups[1].Files[0].FileName),
                                String.Format("{0}_hkfg", database.Name))
                    };

                    dataFile.Create();
                    database.FileGroups.Refresh();

                    // Disable background migration
                    //
                    database.ExecuteNonQuery("dbcc traceon(9975, -1)");

                    // Enable alter to add/drop columnstore for Hekaton tables.
                    //
                    database.ExecuteNonQuery("dbcc traceon(9973, -1)");
                    
                    Table table = database.CreateTable(tableName);

                    // Create the Hekaton table
                    //
                    Table t = new Table(database, this.TestContext.TestName, "dbo")
                    {
                        IsMemoryOptimized = true,
                        Durability = DurabilityType.SchemaAndData
                    };

                    Column c1 = new Column(t, "c1", DataType.BigInt) { Nullable = false };
                    Column c2 = new Column(t, "c2", DataType.BigInt) { Nullable = false };

                    t.Columns.Add(c1);
                    t.Columns.Add(c2);

                    Index pk = new Index(t, "idx")
                    {
                        IsClustered = false,
                        IndexKeyType = IndexKeyType.DriPrimaryKey,
                        IndexType = IndexType.NonClusteredHashIndex,
                        BucketCount = 100,
                    };

                    pk.IndexedColumns.Add(new IndexedColumn(pk, "c1"));

                    t.Indexes.Add(pk);

                    // Create the table on the server.
                    //
                    t.Create();

                    // Verify the table is memory optimized.
                    //
                    Assert.IsTrue(t.IsMemoryOptimized, "The table must be memory optimized.");

                    // Verify that the table has a primary key index.
                    //
                    bool foundPrimaryKeyIndex = false;

                    foreach (Index index in t.Indexes)
                    {
                        if (index.IndexKeyType == IndexKeyType.DriPrimaryKey)
                        {
                            foundPrimaryKeyIndex = true;
                        }
                    }

                    Assert.IsTrue(foundPrimaryKeyIndex, "The Hekaton table must have a primary key index.");

                    // Add columnstore index.
                    //
                    Index cci = new Index(t, "cci")
                    {
                        IndexType = IndexType.ClusteredColumnStoreIndex
                    };

                    t.Indexes.Add(cci);

                    // Alter table on the server to add the columnstore index.
                    //
                    t.Alter();

                    Table serverRefreshedTable = database.Tables[tableName];

                    serverRefreshedTable.Refresh();

                    StringCollection stringCollection = serverRefreshedTable.Script();

                    Assert.IsTrue(stringCollection[2].Contains("INDEX [cci] CLUSTERED COLUMNSTORE"), "The clustered columnstore syntax must be present.");

                    // Drop the columnstore index.
                    //
                    t.Indexes["cci"].Drop();

                    // Alter table on the server to drop the columnstore index.
                    //
                    t.Alter();

                    // Drop the table.
                    //
                    t.Drop();

                    // Create the table again from the script.
                    //
                    database.ExecuteNonQuery(stringCollection[2]);

                    database.Tables.Refresh();

                    Assert.IsTrue(database.Tables.Contains(tableName), "The hkcs table must be re-created from the script.");
                }
            ); 
        }

        /// <summary>
        /// This test verifies that SMO can correctly create a HKCS table, script it back out
        /// and recreate it from the script.
        /// </summary>
        [TestMethod,Ignore]
        public void CreateHkCsTable()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(
                this.ServerName);

            string databaseName = String.Format("hkcs_{0}", Guid.NewGuid());

            try
            {
                // Create a hekaton database
                //
                Database db = CreateHekatonDatabase(server, databaseName);
                string hkcsTableName = "hkcs_table";
                
                // Disable background migration
                //
                server.SetTraceFlag(9975, true);

                // Create the HKCS table
                //
                Table t = new Table(db, hkcsTableName, "dbo") 
                { 
                    IsMemoryOptimized = true, 
                    Durability = DurabilityType.SchemaAndData
                };
                
                Column c1 = new Column(t, "c1", DataType.BigInt) { Nullable = false };
                Column c2 = new Column(t, "c2", DataType.BigInt) { Nullable = false };
                
                t.Columns.Add(c1);
                t.Columns.Add(c2);

                Index pk = new Index(t, "idx") 
                {
                    IsClustered = false,
                    IndexKeyType = IndexKeyType.DriPrimaryKey,
                    IndexType = IndexType.NonClusteredHashIndex,
                    BucketCount = 100,
                };
                
                pk.IndexedColumns.Add(new IndexedColumn(pk, "c1"));

                t.Indexes.Add(pk);

                Index cci = new Index(t, "cci")
                {
                    IndexType = IndexType.ClusteredColumnStoreIndex
                };

                t.Indexes.Add(cci);

                // Create the table on the server.
                //
                t.Create();

                // Verify the table is memory optimized.
                //
                Assert.IsTrue(t.IsMemoryOptimized, "The table must be memory optimized.");

                // Verify that the table has a clustered index and a primary key index.
                //
                bool foundClusteredIndex = false;
                bool foundPrimaryKeyIndex = false;

                foreach(Index index in t.Indexes)
                {
                    if(index.IsClustered)
                    {
                        foundClusteredIndex = true;
                    }

                    if(index.IndexKeyType == IndexKeyType.DriPrimaryKey)
                    {
                        foundPrimaryKeyIndex = true;
                    }
                }
                
                Assert.IsTrue(foundClusteredIndex, "The HKCS table must have a clustered index.");
                Assert.IsTrue(foundPrimaryKeyIndex, "The HKCS table must have a primary key index.");

                Table serverRefreshedTable = db.Tables[hkcsTableName];

                serverRefreshedTable.Refresh();

                StringCollection stringCollection = serverRefreshedTable.Script();

                Assert.IsTrue(stringCollection[2].Contains("INDEX [cci] CLUSTERED COLUMNSTORE"), "The clustered columnstore syntax must be present.");

                // Drop the table.
                //
                t.Drop();

                // Create the table again from the script.
                //
                db.ExecuteNonQuery(stringCollection[2]);

                db.Tables.Refresh();

                Assert.IsTrue(db.Tables.Contains(hkcsTableName), "The hkcs table must be re-created from the script.");
            }
            finally
            {
                server.SetTraceFlag(9975, false);

                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// This test verifies that a table with a clustered column store index can be created
        /// through SMO and that the script SMO generates for the table can be used to create the
        /// table.
        /// </summary>
        [TestMethod,Ignore]
        public void CreateTableWithClusteredColumnstoreIndex()
        {
            Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(
                this.ServerName);
            string databaseName = String.Format("cci_db{0}", Guid.NewGuid());
            string tableName = "cci_table";
            string cciName = "cci";

            if (server.Databases.Contains(databaseName))
            {
                server.Databases[databaseName].Drop();
            }

            Database db = new Database(server, databaseName);

            try
            {
                db.Create();

                Table t = new Table(db, tableName, "dbo");

                Column c1 = new Column(t, "c1", DataType.BigInt) { Nullable = false };
                Column c2 = new Column(t, "c2", DataType.BigInt) { Nullable = false };

                t.Columns.Add(c1);
                t.Columns.Add(c2);

                Index cci = new Index(t, cciName)
                {
                    IndexType = IndexType.ClusteredColumnStoreIndex
                };

                t.Indexes.Add(cci);

                // Create the table on the server.
                //
                t.Create();

                Assert.IsTrue(t.HasClusteredColumnStoreIndex, "The table must have a clustered columnstore index.");

                // Script out the CCI
                //
                ScriptingOptions options = new ScriptingOptions(ScriptOption.ClusteredIndexes);

                StringCollection stringCollection = t.Script(options);

                Assert.IsTrue(stringCollection[0].Contains("COLUMNSTORE INDEX [cci]"), "The clustered columnstore syntax must be present.");

                // Drop the CCI index and recreate it using the generated script.
                //
                t.Indexes["cci"].Drop();
                
                db.ExecuteNonQuery(stringCollection[0]);

                t.Indexes.Refresh();

                Assert.IsTrue(t.Indexes.Contains(cciName), "The index must be recreated with the string collection script.");

                t.Drop();
            }
            finally
            {
                Connection.ChangeDatabase("master");

                SmoTestHelpers.DropDatabase(server, databaseName);
            }
        }

        /// <summary>
        /// Creates a hekaton database using SMO objects.
        /// </summary>
        /// <param name="server">The SMO server object.</param>
        /// <param name="name">The database name.</param>
        /// <returns>The SMO Hekaton database.</returns>
        private Database CreateHekatonDatabase(Microsoft.SqlServer.Management.Smo.Server server, string name)
        {
            Database db = new Database(server, name);

            if (server.Databases.Contains(name))
            {
                server.Databases[name].Drop();
            }

            db.Create();

            FileGroup memoryOptimizedFg = new FileGroup(db, String.Format("{0}_hkfg", name), FileGroupType.MemoryOptimizedDataFileGroup);
            memoryOptimizedFg.Create();

            DataFile dataFile = new DataFile(memoryOptimizedFg, String.Format("{0}_hkfg", name))
            {
                FileName = Path.Combine(TestEnvironment.SqlProcessEnvironments.First().DataDirectory, String.Format("{0}_hkfg", name))
            };

            dataFile.Create();

            db.FileGroups.Refresh();

            return db;
        }

#endregion
    }
}
#endif

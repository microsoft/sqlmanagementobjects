﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Notebook;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
    public class ScriptMakerTests : SmoTestBase
    {
        
        /// <summary>
        /// Bugfix 11293363 - only the "Drop" statement should have the "if exists" check
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void ScriptMaker_avoids_dynamic_SQL_for_DropAndCreate_with_ExistenceCheck()
        {
            ExecuteFromDbPool(TestContext.FullyQualifiedTestClassName,(db) =>
            {
                var view = db.CreateView("DropAndCreate", "dbo", "select 1 as Col1");
                var scriptingPreferences = new ScriptingPreferences(db)
                { 
                    Behavior = ScriptBehavior.DropAndCreate,
                    IncludeScripts =
                    {
                        ExistenceCheck = true
                    }
                };
                var viewName = SmoObjectHelpers.SqlBracketQuoteString(view.Name);
                SmoTestBase.ValidateUrnScripting(db, new[] {view.Urn},
                    new[]
                    {
                        "DROP VIEW IF EXISTS [dbo]." + viewName, 
                        "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE VIEW [dbo].{0}  AS{1}select 1 as Col1", viewName, Environment.NewLine)
                    }, scriptingPreferences);
                // Now make sure Create still works via dynamic sql
                viewName = SmoObjectHelpers.SqlEscapeSingleQuote(viewName);
                scriptingPreferences.Behavior = ScriptBehavior.Create;
                SmoTestBase.ValidateUrnScripting(db, new[] { view.Urn },
                    new[]
                    {
                        "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].{0}')){1}EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].{0}  AS{1}select 1 as Col1'", viewName, Environment.NewLine)
                    }, scriptingPreferences);
            });
        }

        /// <summary>
        /// Bugfix 12324697 - [V] Add new menu item(s) for "CREATE OR ALTER" when scripting objects
        /// Verifying the script generated by ScriptMaker for stored procedure using CreateOrAlter behavior
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptMaker_Verify_CreateOrAlter_StoredProcedure()
        {
            ExecuteFromDbPool(TestContext.FullyQualifiedTestClassName, (db) =>
            {
                var sp = db.CreateSPDefinition("sp", "dbo","BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND");
                sp.Create();
                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;
                var spName = SmoObjectHelpers.SqlBracketQuoteString(sp.Name);
                SmoTestBase.ValidateUrnScripting(db, new[] { sp.Urn },
                    new[]
                    {
                        "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE OR ALTER PROCEDURE [dbo].{0}  AS{1}BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND", spName, Environment.NewLine)
                    }, scriptingPreferences);
            });
        }

        /// <summary>
        /// Bugfix 12324697 - [V] Add new menu item(s) for "CREATE OR ALTER" when scripting objects
        /// Verifying the script generated by ScriptMaker for view using CreateOrAlter behavior for standalone databases
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ScriptMaker_Verify_CreateOrAlter_View_Standalone()
        {
            ExecuteFromDbPool(TestContext.FullyQualifiedTestClassName, (db) =>
            {
                var view = db.CreateViewDefinition("view", "dbo", "SELECT * FROM sys.tables");

                view.Create();
                var viewName = SmoObjectHelpers.SqlBracketQuoteString(view.Name);

                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;
                
                SmoTestBase.ValidateUrnScripting(db, new[] { view.Urn },
                    new[]
                    {
                         "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE OR ALTER VIEW [dbo].{0}  AS{1}SELECT * FROM sys.tables", viewName, Environment.NewLine)
                    }, scriptingPreferences);
            });
        }
        /// <summary>
        /// Bugfix 12324697 - [V] Add new menu item(s) for "CREATE OR ALTER" when scripting objects
        /// Verifying the script generated by ScriptMaker for view using CreateOrAlter behavior for azure databases
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void ScriptMaker_Verify_CreateOrAlter_View_Azure()
        {
            ExecuteFromDbPool((db) =>
            {
                var view = db.CreateViewDefinition("view", "dbo", "SELECT * FROM sys.tables");

                view.Create();
                var viewName = SmoObjectHelpers.SqlBracketQuoteString(view.Name);

                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;

                SmoTestBase.ValidateUrnScripting(db, new[] { view.Urn },
                    new[]
                    {
                        string.Format("CREATE OR ALTER VIEW [dbo].{0}  AS{1}SELECT * FROM sys.tables", viewName, Environment.NewLine)
                    }, scriptingPreferences);
            });
        }
        /// <summary>
        /// Bugfix 12324697 - [V] Add new menu item(s) for "CREATE OR ALTER" when scripting objects
        /// Verifying the script generated by ScriptMaker for user defined function using CreateOrAlter behavior
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptMaker_Verify_CreateOrAlter_UDF()
        {
            ExecuteFromDbPool((db) =>
            {
                var udf = DatabaseObjectHelpers.CreateUdfDefinition(db,
                                                                            "udf",
                                                                            schema: "dbo",
                                                                            textBody: "BEGIN\nRETURN 0;\nEND");
                udf.TextHeader = string.Format("CREATE FUNCTION {0}.{1}() RETURNS INT",
                                                SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                                                SmoObjectHelpers.SqlBracketQuoteString(udf.Name));
                udf.Create();
                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;
               
                var udfName = SmoObjectHelpers.SqlBracketQuoteString(udf.Name);
                SmoTestBase.ValidateUrnScripting(db, new[] { udf.Urn },
                    new[]
                    {
                        "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE OR ALTER FUNCTION [dbo].{0}() RETURNS INT{1}BEGIN\nRETURN 0;\nEND", udfName, Environment.NewLine)
                    }, scriptingPreferences);
            });
        }

        /// <summary>
        /// Bugfix 12324697 - [V] Add new menu item(s) for "CREATE OR ALTER" when scripting objects
        /// Verifying the script generated by ScriptMaker for trigger using CreateOrAlter behavior
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptMaker_Verify_CreateOrAlter_Trigger()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = db.CreateTable("trgTbl");
                var trigger = TableObjectHelpers.CreateTrigger(
                                            table,
                                            "trigger",
                                            textBody: "SELECT 'Create trigger testing.'");
                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;
                var triggerName = SmoObjectHelpers.SqlBracketQuoteString(trigger.Name);
                var tableName = SmoObjectHelpers.SqlBracketQuoteString(table.Name);
                SmoTestBase.ValidateUrnScripting(db, new[] { trigger.Urn },
                    new[]
                    {
                        "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE OR ALTER TRIGGER [dbo].{0} ON [dbo].{1} FOR INSERT AS{2}SELECT 'Create trigger testing.'", triggerName,tableName, Environment.NewLine),
                        string.Format("ALTER TABLE [dbo].{0} ENABLE TRIGGER {1}", tableName,triggerName)
                    }, scriptingPreferences);
            });
        }

        /// <summary>
        /// Bug Fix 12838857 - [V] CREATE OR ALTER is not supported for type ExtendedProperty
        /// Verifying the script generated by ScriptMaker for Extended property with parent object as trigger using CreateOrAlter behavior
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void ScriptMaker_Verify_CreateOrAlter_ExtendedProperty()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = new Table(db, "trgTbl" + Guid.NewGuid());
                table.Columns.Add(new Column(table, "col_1", new DataType(SqlDataType.Int)));
                table.Create();

                var trigger = new Trigger(table, "trgA")
                {
                    TextBody = "SET NOCOUNT ON",
                };
                trigger.TextHeader = $"CREATE TRIGGER {trigger.Name} ON {table.Schema}.{SmoObjectHelpers.SqlBracketQuoteString(table.Name)} AFTER UPDATE AS";
                trigger.Create();

                var extendedProperty = new ExtendedProperty(trigger, "Test", "Test");
                extendedProperty.Create();

                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;
                var triggerName = SmoObjectHelpers.SqlBracketQuoteString(trigger.Name);
                var tableName = SmoObjectHelpers.SqlBracketQuoteString(table.Name);

                SmoTestBase.ValidateUrnScripting(db, new[] { trigger.Urn, extendedProperty.Urn },
                    new[]
                    {
                        "SET ANSI_NULLS ON",
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE OR ALTER TRIGGER [dbo].{0} ON [dbo].{1} AFTER UPDATE AS\r\nSET NOCOUNT ON", triggerName,tableName),
                        string.Format("ALTER TABLE [dbo].{0} ENABLE TRIGGER {1}", tableName,triggerName),
                        $"IF NOT EXISTS (SELECT * FROM sys.fn_listextendedproperty(N'Test' , N'SCHEMA',N'dbo', N'TABLE',N'{table.Name}', N'TRIGGER',N'trgA'))\r\n\tEXEC sys.sp_addextendedproperty @name=N'Test', @value=N'Test' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{table.Name}', @level2type=N'TRIGGER',@level2name=N'trgA'\r\nELSE\r\nBEGIN\r\n\tEXEC sys.sp_updateextendedproperty @name=N'Test', @value=N'Test' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{table.Name}', @level2type=N'TRIGGER',@level2name=N'trgA'\r\nEND"
                    }, scriptingPreferences);
            });
        }

        /// <summary>
        /// Verifying the script generated by ScriptMaker for stored procedure using "Create" behavior
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptMaker_Verify_Create_StoredProcedure()
        {
            ExecuteFromDbPool((db) =>
            {
                var sp = db.CreateSPDefinition("sp", "dbo", "BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND");
                sp.Create();
                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.Create
                };
                var spName = SmoObjectHelpers.SqlBracketQuoteString(sp.Name);
                SmoTestBase.ValidateUrnScripting(db, new[] { sp.Urn },
                    new[]
                    {
                        "SET ANSI_NULLS ON", 
                        "SET QUOTED_IDENTIFIER ON",
                        string.Format("CREATE PROCEDURE [dbo].{0}  AS{1}BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND", spName, Environment.NewLine)
                    }, scriptingPreferences);
            });
        }

        /// <summary>
        /// Tests CreateOrAlter is not supported for Table, so we script it as Create
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void ScriptMaker_Verify_CreateOrAlter_scripts_Table_as_Create()
        {
            ExecuteFromDbPool(TestContext.FullyQualifiedTestClassName, (db) =>
            {
                var table = DatabaseObjectHelpers.CreateTable(db, "table");
                var scriptingPreferences = new ScriptingPreferences(db)
                {
                    Behavior = ScriptBehavior.CreateOrAlter
                };
                var tableName = SmoObjectHelpers.SqlBracketQuoteString(table.Name);
                var server = db.GetServerObject();
                scriptingPreferences.OldOptions.EnforceScriptingPreferences = true;
                var m = new ScriptMaker(server)
                {
                    Preferences = scriptingPreferences
                };
                var dependencyDiscoverer = new SmoDependencyDiscoverer(server)
                {
                    Preferences = scriptingPreferences
                };

                m.discoverer = dependencyDiscoverer;
                scriptingPreferences.IncludeScripts.ExistenceCheck = true;
                ValidateUrnScripting(db, new Urn[] {table.Urn}, new string[]
                {
                    "SET ANSI_NULLS ON",
                    "SET QUOTED_IDENTIFIER ON",
                    $"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].{tableName.SqlEscapeSingleQuote()}') AND type in (N'U')){Environment.NewLine}BEGIN{Environment.NewLine}CREATE TABLE [dbo].{tableName}({Environment.NewLine}\t[col_1] [int] NULL{Environment.NewLine}) ON [PRIMARY]{Environment.NewLine}END"
                }, scriptingPreferences );
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15, MaxMajor = 15, HostPlatform =HostPlatformNames.Windows)]
        [VisualStudio.TestTools.UnitTesting.Ignore]
        public void ScriptMaker_script_to_notebook()
        {
            ExecuteTest(() =>
            {
                var filePath = Path.ChangeExtension(Path.GetTempFileName(), "ipynb");
                using (var writer = new NotebookFileWriter(filePath))
                {
                    var db = ServerContext.Databases["Keep_WideWorldImporters"];
                    var scriptingPreferences = new ScriptingPreferences(db);
                    var dependencyDiscoverer = new SmoDependencyDiscoverer(ServerContext)
                    {
                        Preferences = scriptingPreferences
                    };
                    var scriptMaker = new ScriptMaker(ServerContext)
                    {
                        Preferences = scriptingPreferences,
                        Discoverer = dependencyDiscoverer
                    };
                    var urns = db.Tables.Cast<Table>().Select(t => t.Urn).ToArray();
                    Assert.DoesNotThrow(() => scriptMaker.Script(urns, writer), "Scripting to NotebookFileWriter");
                }
                Process.Start(filePath);
            });
            

        }
    }
}

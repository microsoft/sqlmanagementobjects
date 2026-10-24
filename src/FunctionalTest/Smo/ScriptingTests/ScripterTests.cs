// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    [TestClass]
    public class ScripterTests : SmoTestBase
    {
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 13, DatabaseEngineType = Management.Common.DatabaseEngineType.Standalone)]
        [SupportedServerVersionRange(DatabaseEngineType = Management.Common.DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void Scripter_Script_supports_CreateOrAlter_for_sql2016_and_beyond()
        {
            ExecuteFromDbPool(db =>
            {
                var urnErrors = new List<Urn>();
                var table = db.CreateTable("tbl1");
                var sp = db.CreateSPDefinition("sp1", "dbo", "select 1");
                sp.Create();
                var trigger = db.CreateDatabaseDdlTrigger("trig", "AFTER ALTER_INDEX", "WAITFOR DELAY '00:10:00'");
                var udf = db.CreateUdfDefinition("udf", "dbo", textBody: "BEGIN\nRETURN 0;\nEND");
                udf.TextHeader = string.Format("CREATE OR ALTER FUNCTION {0}.{1}() RETURNS INT",
                                                  SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                                                  SmoObjectHelpers.SqlBracketQuoteString(udf.Name));
                udf.Create();
                var view = db.CreateView("view", "dbo", "select 'view' as col1");
                var ep = new ExtendedProperty(table, "tableEp") { Value = "EP" };
                ep.Create();
                var scripter = new Scripter(db.Parent);
                scripter.Options.ScriptForCreateOrAlter = true;
                scripter.Options.EnforceScriptingOptions = true;
                scripter.Options.ContinueScriptingOnError = true;
                scripter.Options.IncludeIfNotExists = true;
                scripter.ScriptingError += (sender, e) =>  urnErrors.Add(e.Current) ;
                var scripts = scripter.EnumScript(new SqlSmoObject[] { table, sp, trigger, udf, view, ep }).ToArray();
                Trace.TraceInformation(string.Join(Environment.NewLine, scripts));
                Assert.Multiple(() =>
                {
                    Assert.That(urnErrors, Is.EquivalentTo(new Urn[] { }), "IncludeIfNotExists:CreateOrAlter should not have errors for lists that include types which don't support CreateOrAlter");
                    var expected = new[] { "SET ANSI_NULLS ON", 
                                           "SET QUOTED_IDENTIFIER ON",
                                           $"CREATE OR ALTER   FUNCTION {udf.FullQualifiedName}() RETURNS INT{Environment.NewLine}BEGIN\nRETURN 0;\nEND",
                                           "SET ANSI_NULLS ON",
                                           "SET QUOTED_IDENTIFIER ON",
                                           $"CREATE OR ALTER VIEW {view.FullQualifiedName}  AS{Environment.NewLine}select 'view' as col1",
                                           "SET ANSI_NULLS ON",
                                           "SET QUOTED_IDENTIFIER ON",
                                           $"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID({SqlSmoObject.MakeSqlString(table.FullQualifiedName)}) AND type in (N'U')){Environment.NewLine}BEGIN{Environment.NewLine}CREATE TABLE {table.FullQualifiedName}({Environment.NewLine}\t[col_1] [int] NULL{Environment.NewLine}) ON [PRIMARY]{Environment.NewLine}END",
                                           "SET ANSI_NULLS ON",
                                           "SET QUOTED_IDENTIFIER ON",
                                           $"CREATE OR ALTER PROCEDURE {sp.FullQualifiedName}  AS{Environment.NewLine}select 1",
                                           $"CREATE OR ALTER TRIGGER {trigger.FullQualifiedName} ON DATABASE AFTER ALTER_INDEX AS{Environment.NewLine}WAITFOR DELAY '00:10:00'",
                                           $"ENABLE TRIGGER {trigger.FullQualifiedName} ON DATABASE",
                        $"IF NOT EXISTS (SELECT * FROM sys.fn_listextendedproperty(N'tableEp' , N'SCHEMA',N'dbo', N'TABLE',{SqlSmoObject.MakeSqlString(table.Name)}, NULL,NULL)){Environment.NewLine}\tEXEC sys.sp_addextendedproperty @name=N'tableEp', @value=N'EP' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name={SqlSmoObject.MakeSqlString(table.Name)}{Environment.NewLine}ELSE{Environment.NewLine}BEGIN{Environment.NewLine}\tEXEC sys.sp_updateextendedproperty @name=N'tableEp', @value=N'EP' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name={SqlSmoObject.MakeSqlString(table.Name)}{Environment.NewLine}END{Environment.NewLine}"
                    }; 
                    Assert.That(scripts, Is.EqualTo(expected), "Generated scripts with IncludeIfNotExists");
                });
                scripter.Options.IncludeIfNotExists = false;
                scripts = scripter.EnumScript(new SqlSmoObject[] { table, sp }).ToArray();
                Trace.TraceInformation(string.Join(Environment.NewLine, scripts));
                Assert.Multiple(() =>
                {
                    Assert.That(urnErrors, Is.EquivalentTo(new Urn[] { }), "ScriptForCreateDrop:CreateOrAlter should not have errors for lists that include types which don't support CreateOrAlter");
                    var expected = new[] { "SET ANSI_NULLS ON",
                                           "SET QUOTED_IDENTIFIER ON",
                                           $"CREATE TABLE {table.FullQualifiedName}({Environment.NewLine}\t[col_1] [int] NULL{Environment.NewLine}) ON [PRIMARY]{Environment.NewLine}",
                                           "SET ANSI_NULLS ON",
                                           "SET QUOTED_IDENTIFIER ON",
                                           $"CREATE OR ALTER PROCEDURE {sp.FullQualifiedName}  AS{Environment.NewLine}select 1"};
                    Assert.That(scripts, Is.EqualTo(expected), "Generated scripts with ScriptForCreateDrop");
                });
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(MaxMajor = 12, DatabaseEngineType = Management.Common.DatabaseEngineType.Standalone)]
        public void Script_Script_ForCreateOrAlter_fails_pre_sql2016()
        {
            ExecuteFromDbPool(db =>
            {
                var urnErrors = new List<Urn>();
                var table = db.CreateTable("tbl1");
                var sp = db.CreateSPDefinition("sp1", "dbo", "select 1");
                sp.Create();
                var scripter = new Scripter(db.Parent);
                scripter.Options.ScriptForCreateOrAlter = true;
                scripter.Options.EnforceScriptingOptions = true;
                scripter.Options.ContinueScriptingOnError = true;
                scripter.ScriptingError += (sender, e) => urnErrors.Add(e.Current);
                var scripts = scripter.EnumScript(new SqlSmoObject[] { table, sp }).ToArray();
                Trace.TraceInformation(string.Join(Environment.NewLine, scripts));
                Assert.That(urnErrors, Is.EquivalentTo(new Urn[] { sp.Urn }), "CreateOrAlter should have errors when scripting pre-sql2016");
            });
        } 
    }
}

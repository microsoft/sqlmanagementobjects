// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Statistic properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Statistic_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Statistic stat = (_SMO.Statistic)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.Statistics.Refresh();
            Assert.IsNull(table.Statistics[stat.Name],
                            "Current statistic not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a statistic with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Statistic_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                    _SMO.Statistic stat = new _SMO.Statistic(table, GenerateSmoObjectName("stat"));
                    _SMO.StatisticColumn statcol = new _SMO.StatisticColumn(stat, table.Columns[0].Name);

                    stat.StatisticColumns.Add(statcol);

                    string scriptDropIfExistsTemplate = "if  exists";

                    VerifySmoObjectDropIfExists(stat, table, scriptDropIfExistsTemplate);
                });
        }

            #endregion // Scripting Tests

        #region Statistics Properties

        /// <summary>
        /// Test verifies SHOW_STATISTICS query executed on current database through SMO
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 13)]
        public void Verify_StatisticsDetails_executed_on_current_database()
        {
            this.ExecuteWithDbDrop(
               database =>
               {
                   _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                   _SMO.Statistic statistic = new _SMO.Statistic(table, GenerateSmoObjectName("statistics_" + this.TestContext.TestName));
                   _SMO.StatisticColumn statcol = new _SMO.StatisticColumn(statistic, table.Columns[0].Name);

                   statistic.StatisticColumns.Add(statcol);
                   statistic.Create();

                   DataSet ds = statistic.EnumStatistics();
                   Assert.That(ds, Is.Not.Null, "No Statistics information available");
                   Assert.That(ds.Tables.Count, Is.GreaterThan(0), "Current Statistics has no tables available");
                   Assert.That(ds.Tables[0].Rows[0][0], Does.Match(statistic.Name));

                   //Removed the hard-coded Use statement from statistic.EnumStatistics() to support for Azure.
                   //Verifying if DBCC SHOW_STATISTICS query is executed on the current context DB.
                   Assert.That(statistic.GetContextDB().Name, Does.Match(database.Name));
               });
        }

        /// <summary>
        /// This test creates a filtered statistics object on the server and scripts it back out
        /// to verify that the filter clause is not scripted. To get the filtered statistics the
        /// test must create a filtered index.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 10)]
        public void Script_Filtered_Statistics()
        {
            this.ExecuteWithDbDrop(
               database =>
               {
                   TraceHelper.TraceInformation("Starting Filtered Statistics Test");

                   string indexName = "idx";
                   _SMO.Table table = database.CreateTable(TestContext.TestName);
                   _SMO.Index index = new _SMO.Index(table, indexName);

                   index.IndexedColumns.Add(new _SMO.IndexedColumn(index, table.Columns[0].Name));
                   index.IndexKeyType = _SMO.IndexKeyType.None;
                   index.FilterDefinition = $"{table.Columns[0].Name} IS NOT NULL";

                   index.Create();

                   _SMO.ScriptingOptions sp = new _SMO.ScriptingOptions()
                   {
                       OptimizerData = true,
                       Statistics = true
                   };

                   table.Refresh();
                   Assert.That(table.Statistics.Count, Is.GreaterThan(0), "The statistics count should be at least one.");

                   bool foundIdxUpdateStatistics = false;

                   foreach (_SMO.Statistic stat in table.Statistics)
                   {
                       stat.Refresh();
                       StringCollection sc = stat.Script(sp);

                       // The predicate clause for the statistics object should not be added to the scripted DDL.
                       //
                       foreach (string line in sc)
                       {
                           TraceHelper.TraceInformation(line);
                           Assert.That(line, Does.Not.Contain("WHERE"), "There should be no filter clauses.");
                           Assert.That(line, Does.Not.Contain($"{table.Columns[0].Name} IS NOT NULL"), "There should be no filter definitions.");

                           if (line.Contains("UPDATE STATISTICS") && line.Contains(indexName))
                           {
                               foundIdxUpdateStatistics = true;
                           }
                       }
                   }

                   // If we don't find the UPDATE STATISTICS for this table, something is wrong.
                   //
                   Assert.That(foundIdxUpdateStatistics, Is.True, "We must find the UPDATE STATISTICS for idx!");
               });
        }

        #endregion //Statistics Properties
    }



}


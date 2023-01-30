//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert=NUnit.Framework.Assert;
using NUnit.Framework;
using Sfc=Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Linq;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// These are basic tests for how SqlObject generates SQL queries given a request and a config XML
    /// We are only covering a tiny portion of the permutations.
    /// </summary>
    [TestClass]
    public class SqlObjectTests : UnitTestBase
    {

        const string simplexml = @"<EnumObject type='testObj' impl_type='SqlObject' min_major='7'>
<settings><version min_major='7'><property_link table='tableName as t'>(t.Id &gt;1)</property_link></version></settings><properties><property name='ID' type='int'>t.Id</property></properties></EnumObject>";

        //Set of URN object names to test
        readonly string[] urnTestNames = {"testid", //Normal case
                                     "test'id", "test''db", //Single Quotes (reserved character, will be escaped in XPath expression)
                                     "test]db", "test]]db",  //Closing Brackets (not reserved character, added for completeness since it is reserved in other scenarios such as T-SQL)
                                     "test\"db", "test\"\"db", //Double Quotes (not reserved character, added for completeness since it is reserved in other scenarios such as T-SQL)
                                     "test'[\"''[[\"\""}; //All

#if NET462
        [TestInitialize]
        public void Initialize()
        {
            QueryIsolationTests.ResetRegistry();
        }

        [TestCleanup]
        public void Cleanup()
        {
            QueryIsolationTests.ResetRegistry();
        }
#endif

        [TestMethod]
        [TestCategory("Unit")]
        public void When_version_not_provided_LoadInitData_throws_InternalEnumeratorException()
        {
            const string xml = @"<EnumObject type='testObj' impl_type='SqlObject'>
<settings><version min_major='7'><property_link table='tableName'>t.Id &gt;1</property_link></version></settings><properties/></EnumObject>";
            var sqlObject = new SqlObject();
            Assert.Catch<Microsoft.SqlServer.Management.Sdk.Sfc.InternalEnumeratorException>(() => sqlObject.LoadInitData(xml.GetStream(), new ServerVersion(10, 0), DatabaseEngineType.Standalone,
                DatabaseEngineEdition.Standard),
                "LoadInitData should throw with no version");
        }

        /// <summary>
        /// This is pretty much the minimum code needed to generate a sql query using SqlObject and StatementBuilder.
        /// You have to create a request for the type with at least one field in the query
        /// To get the real query, you have to pass the StatementBuilder on to a SqlEnumResult object
        /// If the request urn needs multiple levels, you need to construct the appropriate object hierarchy
        /// as well so the parent tree is available for the script generation. This test doesn't 
        /// cover the parent case.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_prefix_and_postfix_are_empty_only_select_is_in_SqlStatement()
        {
            var sqlObject = BuildTestSqlObject("testObj");
            sqlObject.Request = new Sfc.Request(new Sfc.Urn("testObj"), new string[]{"ID"});
            sqlObject.PrepareGetData(null);
            var sqlEnumResult = new SqlEnumResult(sqlObject.StatementBuilder, Sfc.ResultType.Reserved1,
                DatabaseEngineType.Standalone);
            var sql = sqlEnumResult.BuildSql();
            Assert.That(sql.Count, Is.EqualTo(1), "Sql should be 1 statement");
            Assert.That(sql[0], Is.EqualTo("SELECT\nt.Id AS [ID]\nFROM\ntableName as t\nWHERE\n((t.Id >1))"),
                "First line should be a select statement");

        }

        /// <summary>
        /// This tests that the SQL Statement generated for a SqlObject is correct when the XPath contains nodes
        /// that are filtered (@AttributeName='value'). This will test a number of different filter values, including
        /// ones that contain reserved characters (such as ')
        /// </summary>
        /// <remarks>This is a regression test for the fix for TFS#9731281</remarks>
        [TestMethod]
        [TestCategory("Unit")]
        public void SqlObject_SqlStatementWithFilters_EscapesReservedCharactersCorrectly()
        {
            foreach (string name in urnTestNames)
            {
                //Build up the test object - we just need to add a filter on any property (it doesn't need to "make sense" from an actual query standpoint)
                var sqlObj = BuildTestSqlObject(string.Format("testObj[@ID='{0}']", Sfc.Urn.EscapeString(name)));
                sqlObj.Request = new Sfc.Request(new Sfc.Urn("testObj"), new string[] { "ID" });
                sqlObj.PrepareGetData(null);
                var sqlEnumResult = new SqlEnumResult(sqlObj.StatementBuilder, Sfc.ResultType.Reserved1, DatabaseEngineType.Standalone);

                //Execute and get the resulting SQL statement, verify here that the name is the same as the original
                var sql = sqlEnumResult.BuildSql();
                Assert.That(sql[0], Is.EqualTo(string.Format("SELECT\nt.Id AS [ID]\nFROM\ntableName as t\nWHERE\n((t.Id >1))and(t.Id=<msparam>{0}</msparam>)", name)));
            }
                        
        }

        /// <summary>
        /// Tests that calling the GetFixedStringProperty method returns the name correctly for various names, including ones with 
        /// reserved characters. 
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void SqlObject_CallingGetFixedStringPropertyOnSqlStatementWithFilters_EscapesReservedCharactersCorrectly()
        {
            foreach (string name in urnTestNames)
            {
                var sqlObj = BuildTestSqlObject(string.Format("testObj[@ID='{0}']", Sfc.Urn.EscapeString(name)));
                
                //Test getting a property both escaped and unescaped
                Assert.That(sqlObj.GetFixedStringProperty("ID", removeEscape:true), Is.EqualTo(name), "Got wrong value when calling GetFixedStringProperty when removing URN escapes");
                Assert.That(sqlObj.GetFixedStringProperty("ID", removeEscape: false), Is.EqualTo(Sfc.Urn.EscapeString(name)), "Got wrong value when calling GetFixedStringProperty without removing URN escapes");
            }
        }

#if NET462
        [TestMethod]
        [TestCategory("Unit")]
        public void When_isolation_registry_valid_BuildSql_returns_script_with_isolation_set()
        {
            QueryIsolationTests.SetRegistry(QueryIsolationTests.ReadUncommitted, QueryIsolationTests.ReadCommitted);
            var sqlObject = BuildTestSqlObject("testObj");

            sqlObject.Request = new Sfc.Request(new Sfc.Urn("testObj"), new string[] {"ID"});
            sqlObject.PrepareGetData(null);
            var sqlEnumResult = new SqlEnumResult(sqlObject.StatementBuilder, Sfc.ResultType.Reserved1,
                DatabaseEngineType.Standalone);
            var sql = sqlEnumResult.BuildSql().Cast<string>();
            // the isolation postfix is appended to the last statement because the last statement in the collection is the one executed as a data reader

            Assert.That(sql, Is.EqualTo(new string[] {
                string.Format(QueryIsolationTests.IsolationFormat, QueryIsolationTests.ReadUncommitted),
                "SELECT\nt.Id AS [ID]\nFROM\ntableName as t\nWHERE\n((t.Id >1))\n"+ string.Format(QueryIsolationTests.IsolationFormat, QueryIsolationTests.ReadCommitted) }), 
             "Second statement should be select and set isolation to read uncommitted");
        }
#endif
        /// <summary>
        /// Make sure all the automated and manually generated propertymetadataprovider implementations are in sync after vbump
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void SqlPropertMetadataProvider_implementations_have_correct_number_of_versions()
        {
            // VBUMP
            const int standaloneVersionCount = 11; //7.0, 8.0, 9.0, 10.0, 10.5, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0
            const int cloudVersionCount = 3; //10.0, 11.0, 12.0
            var metadataType = typeof(SqlPropertyMetadataProvider);
            var providers = metadataType.Assembly.GetTypes().Where(t => t != metadataType && metadataType.IsAssignableFrom(t)).ToArray();
            Assert.Multiple(() =>
            {
                foreach (var provider in providers) 
                {
                    var methodVersionArray = provider.GetMethod(nameof(SqlPropertyMetadataProvider.GetVersionArray), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var standaloneVersions = (int[])methodVersionArray.Invoke(null, new object[] { DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise });
                    var cloudVersions = (int[])methodVersionArray.Invoke(null, new object[] { DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase });
                    Assert.That(standaloneVersions, Has.Length.EqualTo(standaloneVersionCount), $"standalone version count for type {provider}");
                    Assert.That(cloudVersions, Has.Length.EqualTo(cloudVersionCount), $"cloud version count for type {provider}");

                }
            });

        }
        #region Helpers

        /// <summary>
        /// Constructs and initializes a simple test object with the given XPath expression
        /// </summary>
        /// <param name="xpathExpression"></param>
        /// <returns></returns>
        private TestSqlObj BuildTestSqlObject(string xpathExpression)
        {
            //Build up the test object - we just need to add a filter on any property (it doesn't need to "make sense" from an actual query standpoint)
            var sqlObj = new TestSqlObj();
            sqlObj.Initialize(null, new Sfc.XPathExpression(xpathExpression)[0]);
            sqlObj.LoadInitData(simplexml.GetStream(),
                    new ServerVersion(10, 0),
                    DatabaseEngineType.Standalone,
                    DatabaseEngineEdition.Standard);
            return sqlObj;
        }
        #endregion Helpers
    }

    static class StringExtensions
    {
        public static Stream GetStream(this string str)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            return new MemoryStream(bytes);
        }
    }

    /// <summary>
    /// Simple wrapper over SqlObject that lets us test protected methods
    /// </summary>
    class TestSqlObj : SqlObject
    {
        public new string GetFixedStringProperty(string name, bool removeEscape)
        {
            return base.GetFixedStringProperty(name, removeEscape);
        }
    }

}

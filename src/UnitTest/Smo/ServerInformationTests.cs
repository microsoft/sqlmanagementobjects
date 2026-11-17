// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;
using SmoServer = Microsoft.SqlServer.Management.Smo.Server;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class ServerInformationTests : UnitTestBase
    {
        [DataTestMethod]
        [TestCategory("Unit")]
        [DataRow(16, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow(17, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow(18, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow(15, DatabaseEngineType.Standalone, DatabaseEngineEdition.SqlManagedInstance)]
        [DataRow(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase)]
        [DataRow(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse)]
        [DataRow(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlOnDemand)]
        [DataRow(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabaseInFabric)]
        public void EditionID_is_supported_and_read_only(int majorVersion, DatabaseEngineType engineType, DatabaseEngineEdition edition)
        {
            var server = ServerTests.GetDesignModeServer(majorVersion, engineType, edition);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<SmoServer>(nameof(SmoServer.EditionID)), Is.True,
                    $"Server.EditionID support on {majorVersion}, {engineType}, {edition}");
                Assert.That(server.IsSupportedProperty<Information>(nameof(Information.EditionID)), Is.True,
                    $"Information.EditionID support on {majorVersion}, {engineType}, {edition}");
                foreach (var smoObject in new SqlSmoObject[] { server, server.Information })
                {
                    var property = smoObject.Properties.GetPropertyObject(nameof(SmoServer.EditionID), doNotLoadPropertyValues: true);
                    Assert.That(property.Type, Is.EqualTo(typeof(long)), $"{smoObject.GetType().Name}.EditionID type");
                    Assert.That(property.Writable, Is.False, $"{smoObject.GetType().Name}.EditionID must be read-only");
                }
            });
        }

        [DataTestMethod]
        [TestCategory("Unit")]
        [DataRow(0L)]
        [DataRow(-978196525L)]
        [DataRow(1804890536L)]
        [DataRow(4294967296L)]
        public void EditionID_preserves_cached_64_bit_values(long editionID)
        {
            var server = ServerTests.GetDesignModeServer(17);
            foreach (var smoObject in new SqlSmoObject[] { server, server.Information })
            {
                var property = smoObject.Properties.GetPropertyObject(nameof(SmoServer.EditionID), doNotLoadPropertyValues: true);
                property.SetValue(editionID);
                property.SetRetrieved(true);
            }

            Assert.Multiple(() =>
            {
                Assert.That(server.EditionID, Is.EqualTo(editionID), "Server.EditionID cached value");
                Assert.That(server.Information.EditionID, Is.EqualTo(editionID), "Information.EditionID cached value");
            });
        }

        [DataTestMethod]
        [TestCategory("Unit")]
        [DataRow("Server.xml", 16, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow("Information.xml", 16, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow("Server.xml", 17, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow("Information.xml", 17, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise)]
        [DataRow("Server.xml", 15, DatabaseEngineType.Standalone, DatabaseEngineEdition.SqlManagedInstance)]
        [DataRow("Information.xml", 15, DatabaseEngineType.Standalone, DatabaseEngineEdition.SqlManagedInstance)]
        [DataRow("Server.xml", 12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase)]
        [DataRow("Information.xml", 12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase)]
        [DataRow("Server.xml", 12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse)]
        [DataRow("Information.xml", 12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse)]
        public void EditionID_query_uses_bigint_and_handles_null(string resourceName, int majorVersion,
            DatabaseEngineType engineType, DatabaseEngineEdition edition)
        {
            var sqlObject = new SqlObject();
            sqlObject.Initialize(null, new Sfc.XPathExpression("Server")[0]);
            sqlObject.LoadInitData(resourceName, new ServerVersion(majorVersion, 0), engineType, edition);

            sqlObject.Request = new Sfc.Request(new Sfc.Urn("Server"), new[] { nameof(SmoServer.EditionID) });
            sqlObject.PrepareGetData(null);
            var result = new SqlEnumResult(sqlObject.StatementBuilder, Sfc.ResultType.Reserved1, engineType);
            var sql = result.BuildSql();
            Assert.That(sql.Count, Is.EqualTo(1), $"{resourceName} should query EditionID in a single statement");
            Assert.That(sql[0], Does.Contain("CAST(ISNULL(SERVERPROPERTY(N'EditionID'), 0) AS bigint) AS [EditionID]"),
                $"{resourceName} must return a non-null 64-bit edition identifier");
        }
    }
}

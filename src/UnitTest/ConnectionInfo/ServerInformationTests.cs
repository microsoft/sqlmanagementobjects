// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using Assert=NUnit.Framework.Assert;


namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    /// <summary>
    /// Tests to verify that ServerInformation class uses efficient means of retrieving data.
    /// </summary>
    [TestClass]
    public class ServerInformationTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void GetServerInformation_executes_one_query_and_accepts_three_result_tables()
        {
            const string versionString = "50.01.1234";

            var connectMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();
            var dataAdapterMock = new Mock<IDbDataAdapter>();
            var expectedScript = @"DECLARE @edition sysname;
SET @edition = cast(SERVERPROPERTY(N'EDITION') as sysname);
SELECT case when @edition = N'SQL Azure' then 2 else 1 end as 'DatabaseEngineType',
SERVERPROPERTY('EngineEdition') AS DatabaseEngineEdition,
SERVERPROPERTY('ProductVersion') AS ProductVersion,
@@MICROSOFTVERSION AS MicrosoftVersion;
select host_platform from sys.dm_os_host_info
if @edition = N'SQL Azure' 
  select 'TCP' as ConnectionProtocol
else
  exec ('select CONVERT(nvarchar(40),CONNECTIONPROPERTY(''net_transport'')) as ConnectionProtocol')
";
            // The Moq verification doesn't print a nice diff of the expected/actual,
            // so let's use the nUnit assert for the string comparison
            commandMock.SetupSet(c => c.CommandText = It.IsAny<string>()).
                Callback<string>((s) => Assert.That(s, Is.EqualTo(expectedScript), "Unexpected script"));
            connectMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);
            dataAdapterMock.SetupSet(d => d.SelectCommand = commandMock.Object);
            dataAdapterMock.Setup(d => d.Fill(It.IsAny<DataSet>())).Callback(
                // Note use of a dataset that would never occur in real life
                (DataSet ds) =>
                {
                    FillTestDataSet(ds, versionString, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase, 0x320104d2, HostPlatformNames.Linux, "TCP");
                });

            var si = ServerInformation.GetServerInformation(connectMock.Object, dataAdapterMock.Object, versionString);
            Assert.That(si.ProductVersion, Is.EqualTo(new Version(versionString)), "Unexpected ProductVersion");
            Assert.That(si.HostPlatform, Is.EqualTo(HostPlatformNames.Linux), "Unexpected HostPlatform");
            Assert.That(si.DatabaseEngineEdition, Is.EqualTo(DatabaseEngineEdition.SqlDatabase), "Unexpected DatabaseEngineEdition");
            Assert.That(si.DatabaseEngineType, Is.EqualTo(DatabaseEngineType.SqlAzureDatabase), "Unexpected DatabaseEngineType");
            Assert.That(si.ServerVersion.Major, Is.EqualTo(50), "Unexpected ServerVersion");
            Assert.That(si.ConnectionProtocol, Is.EqualTo(NetworkProtocol.TcpIp), "Unexpected ConnectionProtocol");
            connectMock.VerifyAll();
            commandMock.VerifyAll();
            dataAdapterMock.VerifyAll();

            // Let's make sure it only calls Fill once
            dataAdapterMock.Invocations.Clear();
            dataAdapterMock.Verify(d => d.Fill(It.IsAny<DataSet>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_ServerVersion_is_less_than_14_HostPlatform_is_hard_coded_Windows()
        {
            var connectMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();
            var dataAdapterMock = new Mock<IDbDataAdapter>();
            var expectedScript =
                @"DECLARE @edition sysname;
SET @edition = cast(SERVERPROPERTY(N'EDITION') as sysname);
SELECT case when @edition = N'SQL Azure' then 2 else 1 end as 'DatabaseEngineType',
SERVERPROPERTY('EngineEdition') AS DatabaseEngineEdition,
SERVERPROPERTY('ProductVersion') AS ProductVersion,
@@MICROSOFTVERSION AS MicrosoftVersion;
select N'Windows' as host_platform
if @edition = N'SQL Azure' 
  select 'TCP' as ConnectionProtocol
else
  exec ('select CONVERT(nvarchar(40),CONNECTIONPROPERTY(''net_transport'')) as ConnectionProtocol')
";

            // The Moq verification doesn't print a nice diff of the expected/actual,
            // so let's use the nUnit assert for the string comparison
            commandMock.SetupSet(c => c.CommandText = It.IsAny<string>()).
                Callback<string>((s) => Assert.That(s, Is.EqualTo(expectedScript), "Unexpected script"));
            connectMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);
            dataAdapterMock.SetupSet(d => d.SelectCommand = commandMock.Object);
            dataAdapterMock.Setup(d => d.Fill(It.IsAny<DataSet>())).Callback(
                (DataSet ds) =>
                {
                    FillTestDataSet(ds, "12.0.2000.8", DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase, 0x0A0104d2, HostPlatformNames.Windows, null);
                });

            var si = ServerInformation.GetServerInformation(connectMock.Object, dataAdapterMock.Object, "10.01.1234");
            Assert.That(si.HostPlatform, Is.EqualTo(HostPlatformNames.Windows), "Unexpected HostPlatform");
            commandMock.VerifyAll();
            connectMock.VerifyAll();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_Edition_is_1000_from_Azure_server_it_is_converted_to_SqlDatabase()
        {
            // Test for Dynamics CRM scenario (edition 1000)
            const string versionString = "16.00.1000.6";

            var connectMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();
            var dataAdapterMock = new Mock<IDbDataAdapter>();
            
            connectMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);
            dataAdapterMock.SetupSet(d => d.SelectCommand = commandMock.Object);
            dataAdapterMock.Setup(d => d.Fill(It.IsAny<DataSet>())).Callback(
                (DataSet ds) =>
                {
                    // Simulate Dynamics CRM server returning edition 1000 with Azure engine type
                    FillTestDataSet(ds, versionString, DatabaseEngineType.SqlAzureDatabase, (DatabaseEngineEdition)1000, 0x10000FA0, HostPlatformNames.Windows, "TCP");
                });

            var si = ServerInformation.GetServerInformation(connectMock.Object, dataAdapterMock.Object, versionString);
            
            // Verify that edition 1000 from Azure is converted to SqlDatabase
            Assert.That(si.DatabaseEngineType, Is.EqualTo(DatabaseEngineType.SqlAzureDatabase), "Engine type should remain SqlAzureDatabase");
            Assert.That(si.DatabaseEngineEdition, Is.EqualTo(DatabaseEngineEdition.SqlDatabase), "Edition 1000 from Azure should be converted to SqlDatabase");
            Assert.That(si.OriginalDatabaseEngineEdition, Is.EqualTo((DatabaseEngineEdition)1000), "Original edition should be preserved as 1000");
            Assert.That(si.ProductVersion, Is.EqualTo(new Version(versionString)), "Unexpected ProductVersion");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_Edition_is_1000_from_Standalone_server_it_remains_unchanged()
        {
            // Test for edition 1000 from a standalone server (not Azure)
            const string versionString = "16.00.1000.6";

            var connectMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();
            var dataAdapterMock = new Mock<IDbDataAdapter>();
            
            connectMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);
            dataAdapterMock.SetupSet(d => d.SelectCommand = commandMock.Object);
            dataAdapterMock.Setup(d => d.Fill(It.IsAny<DataSet>())).Callback(
                (DataSet ds) =>
                {
                    // Simulate standalone server returning edition 1000
                    FillTestDataSet(ds, versionString, DatabaseEngineType.Standalone, (DatabaseEngineEdition)1000, 0x10000FA0, HostPlatformNames.Windows, "TCP");
                });

            var si = ServerInformation.GetServerInformation(connectMock.Object, dataAdapterMock.Object, versionString);
            
            // Verify that edition 1000 from standalone server is NOT converted
            Assert.That(si.DatabaseEngineType, Is.EqualTo(DatabaseEngineType.Standalone), "Engine type should be Standalone");
            Assert.That(si.DatabaseEngineEdition, Is.EqualTo((DatabaseEngineEdition)1000), "Edition 1000 from standalone should remain as 1000");
            Assert.That(si.OriginalDatabaseEngineEdition, Is.EqualTo((DatabaseEngineEdition)1000), "Original edition should be 1000");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_Edition_is_SqlDatabase_original_edition_matches_converted_edition()
        {
            // Test that genuine Azure SQL Database preserves original edition correctly
            const string versionString = "12.00.6024.0";

            var connectMock = new Mock<IDbConnection>();
            var commandMock = new Mock<IDbCommand>();
            var dataAdapterMock = new Mock<IDbDataAdapter>();
            
            connectMock.Setup(c => c.CreateCommand()).Returns(commandMock.Object);
            dataAdapterMock.SetupSet(d => d.SelectCommand = commandMock.Object);
            dataAdapterMock.Setup(d => d.Fill(It.IsAny<DataSet>())).Callback(
                (DataSet ds) =>
                {
                    // Simulate genuine Azure SQL Database (edition 5)
                    FillTestDataSet(ds, versionString, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase, 0x0C001788, HostPlatformNames.Windows, "TCP");
                });

            var si = ServerInformation.GetServerInformation(connectMock.Object, dataAdapterMock.Object, versionString);
            
            // Verify that SqlDatabase edition stays the same and original matches
            Assert.That(si.DatabaseEngineType, Is.EqualTo(DatabaseEngineType.SqlAzureDatabase), "Engine type should be SqlAzureDatabase");
            Assert.That(si.DatabaseEngineEdition, Is.EqualTo(DatabaseEngineEdition.SqlDatabase), "Edition should be SqlDatabase");
            Assert.That(si.OriginalDatabaseEngineEdition, Is.EqualTo(DatabaseEngineEdition.SqlDatabase), "Original edition should also be SqlDatabase (no conversion)");
        }

        private void FillTestDataSet(DataSet ds, string productVersion, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition,
            int microsoftVersion, string hostPlatform, string protocol)
        {
            Trace.TraceInformation("Creating test DataSet");
            ds.Tables.Add("Table");
            ds.Tables["Table"].Columns.Add(new DataColumn("ProductVersion", typeof(string)));
            ds.Tables["Table"].Columns.Add(new DataColumn("DatabaseEngineType", typeof(int)));
            ds.Tables["Table"].Columns.Add(new DataColumn("DatabaseEngineEdition", typeof(int)));
            ds.Tables["Table"].Columns.Add(new DataColumn("MicrosoftVersion", typeof(int)));
            ds.Tables["Table"].Rows.Add(productVersion, (int)databaseEngineType, (int)databaseEngineEdition, microsoftVersion);
            ds.Tables.Add("Table2");
            ds.Tables["Table2"].Columns.Add(new DataColumn("host_platform", typeof(string)));
            ds.Tables["Table2"].Rows.Add(hostPlatform);
            ds.Tables.Add("Table3");
            ds.Tables["Table3"].Columns.Add(new DataColumn("ConnectionProtocol", typeof (string)));
            ds.Tables["Table3"].Rows.Add(protocol);
            ds.AcceptChanges();
            Trace.TraceInformation("Tables: {0}", string.Join(",", ds.Tables.OfType<DataTable>().Select(dt => dt.TableName).ToArray()));
            Trace.TraceInformation("Rows : {0}",
                string.Join(",",
                    ds.Tables.OfType<DataTable>()
                        .SelectMany(dt => dt.Rows.OfType<DataRow>())
                        .Select(r => r[0].ToString())
                        .ToArray()));
        }
    }
}

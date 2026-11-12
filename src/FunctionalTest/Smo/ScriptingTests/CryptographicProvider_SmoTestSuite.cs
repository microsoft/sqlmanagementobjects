// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing CryptographicProvider properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class CryptographicProvider_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            _SMO.CryptographicProvider cp = (_SMO.CryptographicProvider)obj;
            _SMO.Server server = cp.Parent;

            // Check if cryptographic provider is already created in baseline tests
            // and if it isn't create it.
            //
            if (server.CryptographicProviders.Count != 0)
            {
                cp = server.CryptographicProviders[0];
            }
            else
            {
                cp.DllPath = "C:\\Program Files\\SQL Server Connector for Microsoft Azure Key Vault\\Microsoft.AzureKeyVaultService.EKM.dll";

                server.ConnectionContext.ExecuteNonQuery(@"sp_configure 'show advanced options', 1;
                                                                     GO
                                                                     RECONFIGURE;
                                                                     GO
                                                                     sp_configure 'EKM provider enabled', 1;
                                                                     GO
                                                                     RECONFIGURE;
                                                                     GO");
                cp.Create();
            }
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.CryptographicProvider cp = (_SMO.CryptographicProvider)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.CryptographicProviders.Refresh();
            Assert.IsNull(server.CryptographicProviders[cp.Name],
                          "Current cryptographic provider not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a cryptographic provider with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void SmoDropIfExists_CryptographicProvider_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.CryptographicProvider cp = new _SMO.CryptographicProvider(server,
                        GenerateUniqueSmoObjectName("cp"));

                    try
                    {
                        VerifySmoObjectDropIfExists(cp, server);
                    }
                    catch (Exception)
                    {
                        if (server.CryptographicProviders[cp.Name] != null)
                        {
                            cp.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Tests for the AsymmetricKey object
    /// </summary>
    [TestClass]
    public class AsymmetricKeyTests : SqlTestBase
    {
        /// <summary>
        /// Test AsymmetricKey.Drop(bool removeProviderKey) generates correct DROP statement
        /// </summary>
        [TestCategory("Functional")]
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        [SupportedServerVersionRange(MinMajor = 10)]
        public void AsymmetricKey_Drop_GeneratesCorrectScript(bool removeProviderKey)
        {
            ExecuteFromDbPool((db) =>
            {
                var asymmetricKey = new AsymmetricKey(db, $"ak_{TestContext.TestName}_{Guid.NewGuid():N}");
                asymmetricKey.Create(AsymmetricKeyEncryptionAlgorithm.Rsa2048, Guid.NewGuid().ToString());

                try
                {
                    var capturedSql = db.ExecutionManager.RecordQueryText(() => asymmetricKey.Drop(removeProviderKey)).ToSingleString();
                    var expectedDropStatement = string.Format("DROP ASYMMETRIC KEY [{0}]", asymmetricKey.Name);
                    
                    // Verify the DROP statement is present in the captured SQL
                    Assert.That(capturedSql, Contains.Substring(expectedDropStatement),
                        $"Drop script should contain '{expectedDropStatement}'");
                    
                    // Verify presence or absence of REMOVE PROVIDER KEY clause
                    if (removeProviderKey)
                    {
                        Assert.That(capturedSql, Contains.Substring("REMOVE PROVIDER KEY"),
                            "Drop script with removeProviderKey=true should include REMOVE PROVIDER KEY clause");
                    }
                    else
                    {
                        Assert.That(capturedSql, Does.Not.Contain("REMOVE PROVIDER KEY"),
                            "Drop script with removeProviderKey=false should not include REMOVE PROVIDER KEY clause");
                    }
                }
                finally
                {
                    if (asymmetricKey.State != SqlSmoState.Dropped)
                    {
                        asymmetricKey.Drop();
                    }
                }
            });
        }
    }
}

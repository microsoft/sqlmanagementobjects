// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    [TestClass]
    public class StoredProcedureTests : SqlTestBase
    {
        /// <summary>
        /// Tests that when retrieving a StoredProcedure from the database the
        /// DataType property of its parameters is correctly populated for all supported SQL data types.
        /// This is mostly meant as a guard for when new types are added, since we can't guarantee that
        /// a stored procedure with the new type will be added to the baseline tests.
        /// </summary>
        [TestMethod]
        public void StoredProcedure_CreatesDataTypePropertyCorrectlyForAllDatatypes()
        {
            ExecuteFromDbPool((db) =>
            {
                var isFabricDw = db.DatabaseEngineEdition == Management.Common.DatabaseEngineEdition.SqlOnDemand && db.IsFabricDatabase;
                Assert.Multiple(() =>
                {
                    foreach (SqlDataType dataType in Enum.GetValues(typeof(SqlDataType)))
                    {
                        var sqlServerVersion = ScriptingOptions.ConvertToSqlServerVersion(db.ServerVersion);
                        if (DataType.IsSystemDataType(dataType, sqlServerVersion, ServerContext.DatabaseEngineType, ServerContext.DatabaseEngineEdition, db.IsFabricDatabase))
                        {
                            // JSON is supported on 2022, but isn't a native data type until 2025 so we ignore it for earlier versions
                            if (dataType == SqlDataType.Json && (ServerContext.VersionMajor < 17 || isFabricDw))
                            {
                                continue;
                            }
                            if (isFabricDw && (dataType == SqlDataType.NText || dataType == SqlDataType.Text || dataType == SqlDataType.Vector))
                            {
                                // NText and Text with default UTF8 collation is not supported in Fabric DW
                                continue;
                            }
                            var typeObj = new DataType(dataType);
                            StoredProcedure sp = new StoredProcedure(db, "spTest", "dbo")
                            {
                                TextMode = false,
                                TextBody = "RETURN 0"
                            };
                            sp.Parameters.Add(new StoredProcedureParameter(sp, "@param1", typeObj));
                            try
                            {
                                // Using DoesNotThrow so the foreach loop continues on a failure
                                Assert.DoesNotThrow(sp.Create);
                                db.StoredProcedures.Refresh();
                                var refreshedStoredProcedure = db.StoredProcedures["spTest", "dbo"];
                                Assert.That(refreshedStoredProcedure, Is.Not.Null, "Stored procedure was not created correctly");
                                if (refreshedStoredProcedure != null)
                                {
                                    Assert.That(refreshedStoredProcedure.Parameters[0].DataType.SqlDataType, Is.EqualTo(typeObj.SqlDataType), "StoredProcedure.Parameter.DataType was not the expected type");
                                }
                            }
                            finally
                            {
                                // DropIfExists supported in SQL 2016 and later versions
                                if (ServerContext.VersionMajor < 13)
                                {
                                    sp.Drop();
                                }
                                else
                                {
                                    sp.DropIfExists();
                                }
                            }
                        }
                    }
                   
                });
            });
        }
    }
}

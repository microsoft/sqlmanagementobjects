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
    public class UserDefinedFunctionTests : SqlTestBase
    {
        /// <summary>
        /// Tests that when retrieving a UserDefinedFunction from the database the
        /// DataType property of its parameters is correctly populated for all supported SQL data types.
        /// This is mostly meant as a guard for when new types are added, since we can't guarantee that
        /// a stored procedure with the new type will be added to the baseline tests.
        /// </summary>
        [TestMethod]
        public void UserDefinedFunction_CreatesDataTypePropertyCorrectlyForAllDatatypes()
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
                            switch(dataType)
                            {
                                case SqlDataType.Timestamp:
                                    // Timestamp isn't supported as either parameter or return values so just skip it
                                    continue;
                                case SqlDataType.Json:
                                    // JSON is supported on 2022, but isn't a native data type until 2025 so we ignore it for earlier versions
                                    if (ServerContext.VersionMajor < 17)
                                    {
                                        continue;
                                    }
                                    break;

                            }

                            var paramTypeObj = new DataType(dataType);
                            DataType retTypeObj;
                            // Some types aren't allowed as return values so use int as the return value for the purposes of this test
                            switch (dataType)
                            {
                                case SqlDataType.Image:
                                case SqlDataType.NText:
                                case SqlDataType.Text:
                                    retTypeObj = DataType.Int;
                                    if (ServerContext.ConnectionContext.IsFabricServer)
                                    {
                                        // legacy LOB types aren't supported in Fabric with the default UTF8 collation
                                        continue;
                                    }
                                    break;
                                default:
                                    retTypeObj = paramTypeObj;
                                    break;
                            }
                            var udf = new UserDefinedFunction(db, "udfTest", "dbo")
                            {
                                TextMode = false,
                                FunctionType = UserDefinedFunctionType.Scalar,
                                DataType = retTypeObj
                            };
                            string sqlName;
                            switch (dataType)
                            {
                                case SqlDataType.Xml:
                                    // XML is special cased to be empty for other uses in GetSqlName so we just hardcode it here
                                    sqlName = "xml";
                                    break;
                                case SqlDataType.Vector:
                                    // Vector requires a dimension, so just hardcode it here
                                    sqlName = "vector(1998)";
                                    break;
                                default:
                                    sqlName = retTypeObj.GetSqlName(retTypeObj.SqlDataType);
                                    break;
                            }
                            
                            // The returned value must match the type, but we don't care about the actual value so just fake it
                            udf.TextBody = $"BEGIN RETURN CONVERT({sqlName}, NULL) END";
                            udf.Parameters.Add(new UserDefinedFunctionParameter(udf, "@param1", paramTypeObj));
                            try
                            {
                                // Using an Assert.DoesNotThrow here so that if creation fails we still try the other types
                                Assert.DoesNotThrow(udf.Create);
                                db.UserDefinedFunctions.Refresh();
                                var refreshedUserDefinedFunction = db.UserDefinedFunctions["udfTest", "dbo"];
                                Assert.That(refreshedUserDefinedFunction, Is.Not.Null, "UserDefinedFunction was not created correctly");
                                Assert.That(refreshedUserDefinedFunction.Parameters[0].DataType.SqlDataType, Is.EqualTo(paramTypeObj.SqlDataType), "Parameter DataType was not the expected type");
                                Assert.That(refreshedUserDefinedFunction.DataType.SqlDataType, Is.EqualTo(retTypeObj.SqlDataType), "Return DataType was not the expected type");
                            }
                            finally
                            {
                                // DropIfExists supported in SQL 2016 and later versions
                                if (ServerContext.VersionMajor < 13)
                                {
                                    udf.Drop();
                                }
                                else
                                {
                                    udf.DropIfExists();
                                }
                            }
                        }
                    }

                });
            });
        }
    }
}

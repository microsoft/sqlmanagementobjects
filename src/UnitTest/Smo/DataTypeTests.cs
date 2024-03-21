// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the DataType object
    /// </summary>
    [TestClass]
    public class DataTypeTests : UnitTestBase
    {
        /// <summary>
        /// The mapping of all types added post-2000 and what their minimum version is
        /// </summary>
        private static readonly Dictionary<SqlDataType, SqlServerVersion> post2000Types = new Dictionary
            <SqlDataType, SqlServerVersion>()
        {
            { SqlDataType.NVarCharMax, SqlServerVersion.Version90},
            { SqlDataType.VarCharMax, SqlServerVersion.Version90},
            { SqlDataType.VarBinaryMax, SqlServerVersion.Version90},
            { SqlDataType.Xml, SqlServerVersion.Version90},
            { SqlDataType.Date, SqlServerVersion.Version100},
            { SqlDataType.DateTime2, SqlServerVersion.Version100},
            { SqlDataType.DateTimeOffset, SqlServerVersion.Version100},
            { SqlDataType.Geography, SqlServerVersion.Version100},
            { SqlDataType.Geometry, SqlServerVersion.Version100},
            { SqlDataType.HierarchyId, SqlServerVersion.Version100},
            { SqlDataType.Time, SqlServerVersion.Version100},
            { SqlDataType.UserDefinedTableType, SqlServerVersion.Version100},
            { SqlDataType.Json, SqlServerVersion.Version160},
        };

        /// <summary>
        /// Verifies that for every actual type in SqlDataType a call to DataType.IsDataTypeSupportedOnTargetVersion
        /// returns true for all versions that should support that type (i.e. all versions >= the SQL version it was
        /// added in).
        /// This is to prevent errors where a new data type is added to SqlDataType but that method is not updated
        /// correctly.
        /// Note: This unit test only covers the types on SQL Standalone.
        ///    For Azure, new types added should have their dedicated unit tests that include edition type.
        ///    See JsonDataType_SupportedOnAllApplicableVersions as an example.
        ///
        /// </summary>
        [TestCategory("Unit")]
        [TestMethod]
        public void AllSqlDataTypeValues_SupportedOnSqlStandaloneVersions()
        {
            foreach (SqlDataType sqlDataType in Enum.GetValues(typeof (SqlDataType)).Cast<SqlDataType>().Except(new []{ SqlDataType.None }))
            {
                foreach (SqlServerVersion version in Enum.GetValues(typeof (SqlServerVersion)).Cast<SqlServerVersion>())
                {
                    //If this was a type added post-2000 then skip any versions lower than
                    //the minimum supported version
                    if (post2000Types.ContainsKey(sqlDataType) && post2000Types[sqlDataType] > version)
                    {
                        continue;
                    }

                    // Database edition type is not used when engine type is Standalone.
                    //
                    Assert.That(DataType.IsDataTypeSupportedOnTargetVersion(sqlDataType, version, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise),
                        Is.True,
                        "Data Type {0} is marked as not supported on target version {1} - has DataType.IsDataTypeSupportedOnTargetVersion been updated for this type?",
                        sqlDataType,
                        version);
                }
            }
        }

        #region New Data type unit test

        /// <summary>
        /// Verifies that data types are correctly supported in various SQL DB edition (see GetSupportedDatabaseEngineEditions)
        /// Note: It only tests the data types added since SQL DB was first shipped.
        ///     For old data types before SQL DB, they are so old that Standalone unit test should cover them for SQL DB because
        ///     SQL DB is using a SQL version that supports all old data types.
        /// </summary>
        [TestCategory("Unit")]
        [TestMethod]
        [DataTestMethod]
        [DataRow(SqlDataType.Json, DatabaseEngineEdition.SqlDatabase, true)]
        [DataRow(SqlDataType.Json, DatabaseEngineEdition.SqlDataWarehouse, false)]
        public void SqlDataType_SupportedOnAzure(SqlDataType dataType, DatabaseEngineEdition engineEdition, bool supported)
        {
            // Supportablity of a data type in SQL DB doesn't care about SQL Server version
            //
            Assert.That(DataType.IsDataTypeSupportedOnTargetVersion(dataType, SqlServerVersion.Version120, DatabaseEngineType.SqlAzureDatabase, engineEdition), Is.EqualTo(supported),
                "Supportability of data Type {0} in database edition {1} is incorrect - has DataType.IsDataTypeSupportedOnTargetVersion been updated for this type?",
                SqlDataType.Json,
                engineEdition);
        }

        #endregion
    }
}

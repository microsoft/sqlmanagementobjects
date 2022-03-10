// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
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
        };

        /// <summary>
        /// Verifies that for every actual type in SqlDataType a call to DataType.IsDataTypeSupportedOnTargetVersion
        /// returns true for all versions that should support that type (i.e. all versions >= the SQL version it was
        /// added in).
        /// This is to prevent errors where a new data type is added to SqlDataType but that method is not updated
        /// correctly.
        /// </summary>
        [TestCategory("Unit")]
        [TestMethod]
        public void AllSqlDataTypeValues_SupportedOnAllApplicableVersions()
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

                    Assert.That(DataType.IsDataTypeSupportedOnTargetVersion(sqlDataType, version),
                        Is.True,
                        "Data Type {0} is marked as not supported on target version {1} - has DataType.IsDataTypeSupportedOnTargetVersion been updated for this type?",
                        sqlDataType,
                        version);
                }
            }
        }

    }


}

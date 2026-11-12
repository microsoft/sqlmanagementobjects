// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class DependencyScriptTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow(12, false, "SQL12Dependency.sql")]
        [DataRow(13, false, "SQL12Dependency.sql")]
        [DataRow(17, false, "SQL12Dependency.sql")]
        [DataRow(10, true, "CloudDependency.sql")]
        [TestCategory("Unit")]
        public void Dependency_script_selection_uses_supported_resource(
            int serverMajorVersion,
            bool isDbCloud,
            string expectedResourceFileName)
        {
            string resourceFileName = SqlEnumDependencies.GetDependencyScriptResourceFileName(
                new ServerVersion(serverMajorVersion, 0),
                isDbCloud);

            Assert.That(
                resourceFileName,
                Is.EqualTo(expectedResourceFileName),
                $"Unexpected dependency script for server major version {serverMajorVersion}, cloud: {isDbCloud}.");
        }

        [DataTestMethod]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        [DataRow(11)]
        [TestCategory("Unit")]
        public void Dependency_script_selection_rejects_unsupported_standalone_versions(int serverMajorVersion)
        {
            Assert.Throws<InvalidVersionEnumeratorException>(
                () => SqlEnumDependencies.GetDependencyScriptResourceFileName(
                    new ServerVersion(serverMajorVersion, 0),
                    isDbCloud: false),
                $"Dependency discovery should reject standalone server major version {serverMajorVersion}.");
        }
    }
}

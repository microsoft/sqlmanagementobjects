// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Reflection;

namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    [TestClass]
    public class VersionTests
    {
        [TestMethod]
        public void Applocal_minor_version_is_100()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
#if MICROSOFTDATA
            Assert.That(version.Minor, Is.EqualTo(100), "Applocal minor version");
#else
            Assert.That(version.Minor, Is.EqualTo(0), "GAC minor version");
#endif
        }
    }
}

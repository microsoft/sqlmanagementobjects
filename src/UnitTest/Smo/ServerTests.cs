//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the Server object
    /// </summary>
    [TestClass]
    public class ServerTests : UnitTestBase
    {
        /// <summary>
        /// Validate we get LCID for all known collations from a hard-coded list, since the list rarely changes.
        /// If there are additional collations not in the list, we get it from the server at runtime.
        /// GetStringComparer will call GetLCIDCollation to get the LCID from the hard-coded collation list
        [TestCategory("Unit")]
        [TestMethod]
        public void GetLcidFromCollationListTest()
        {
            var server = new Microsoft.SqlServer.Management.Smo.Server();
            var comparer = server.GetStringComparer("Japanese_BIN2");
            Assert.That(comparer, Is.Not.Null, "Cannot get valid LCID or comparer");
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Server_name_constructor_sets_State_to_Existing()
        {
            var server = new Management.Smo.Server("someName");
            Assert.That(server.State, Is.EqualTo(SqlSmoState.Existing), "server.State after construction");
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the User object
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class UserTests : UnitTestBase
    {
        /// <summary>
        /// Verifies that scripting a User with LoginType.WindowsGroup using ScriptAlter
        /// produces the expected valid script output.
        /// </summary>
        [TestMethod]
        [DataRow("dbo", "[dbo]")]
        [DataRow("", "NULL")]
        public void User_ScriptAlter_WindowsGroup_ScriptsCorrectly(string defaultSchema, string expectedDefaultSchema)
        {
            // Create a design-mode server (SQL Server 2012+ to support WindowsGroup with DefaultSchema)
            var server = ServerTests.GetDesignModeServer(11);

            var database = new Database(server, "TestDatabase");
            var user = new User(database, "TestWindowsGroupUser");

            // Set LoginType to WindowsGroup using the IAlienObject interface
            // since LoginType is a read-only property
            ((IAlienObject)user).SetPropertyValue(
                nameof(User.LoginType),
                typeof(LoginType),
                LoginType.WindowsGroup);

            // Set a Login and DefaultSchema
            user.Login = @"DOMAIN\TestGroup";
            user.DefaultSchema = defaultSchema;

            // Call ScriptAlter
            var script = new StringCollection();
            var sp = database.GetScriptingPreferencesForCreate();
            user.ScriptAlter(script, sp);
            var scriptText = string.Join("\n", script.Cast<string>());

            Assert.That(scriptText, Is.EqualTo($"ALTER USER [TestWindowsGroupUser] WITH DEFAULT_SCHEMA={expectedDefaultSchema}, LOGIN=[DOMAIN\\TestGroup]"));
        }
    }
}

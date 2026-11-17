// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SqlScriptPublishTests
{
    [TestClass]
    
    public class SqlScriptPublishModelTests
    {
        private const string dbUrn = "Server/Database[@Name='databaseName']";
        [TestMethod]
        [TestCategory("Unit")]
        public void ScriptEventArgs_constructor_and_properties()
        {
            var args = new ScriptEventArgs(new Urn(dbUrn),
                new InvalidOperationException(), completed: true);
            Assert.Multiple(() =>
            {
                Assert.That(args.Completed, Is.True, nameof(args.Completed));
                Assert.That(args.Urn.Value, Is.EqualTo(dbUrn), nameof(args.Urn));
                Assert.That(args.ContinueScripting, Is.False, nameof(args.ContinueScripting));
                Assert.That(args.Error, Is.TypeOf<InvalidOperationException>(), nameof(args.Error));
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ScriptItemsArgs_constructor_and_properties()
        {
            var args = new ScriptItemsArgs(new [] {new Urn(dbUrn) });
            Assert.Multiple(() =>
            {
                Assert.That(args.Urns, Is.EquivalentTo(new[] { new Urn(dbUrn) }), nameof(args.Urns));
            });
        }
    }
}

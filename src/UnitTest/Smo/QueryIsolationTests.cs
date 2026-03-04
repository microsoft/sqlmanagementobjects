//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
#if !NETCOREAPP
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class QueryIsolationTests : UnitTestBase
    {
        internal const string IsolationFormat = @"SET TRANSACTION ISOLATION LEVEL {0};";
        internal const string ReadCommitted = "read committed";
        internal const string ReadUncommitted = "read uncommitted";
        private const string InvalidLevel = "read sqltoolsguy";


        public override void MyTestInitialize()
        {
            base.MyTestInitialize();
            ResetRegistry();
        }

        public override void MyTestCleanup()
        {
            base.MyTestCleanup();
            ResetRegistry();
        }

        internal static void ResetRegistry()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKey(string.Format(QueryIsolation.RegPathFormat, RegKeyName));
            }
            catch (Exception)
            {
                
            }
            // this clears the cache that's set by the static constructor
            QueryIsolation.cachedPrefix = null;
        }

        /// <summary>
        /// We create a registry key with the current process name and set the Prefix/Postfix values 
        /// with the requested isolation levels
        /// </summary>
        /// <param name="initialIsolation">The isolation level to run the query</param>
        /// <param name="restoredIsolation">The isolation level to restore after running the query</param>
        internal static void SetRegistry(string initialIsolation, string restoredIsolation)
        {
            using (var isolationKey = Registry.CurrentUser.CreateSubKey(string.Format(QueryIsolation.RegPathFormat, RegKeyName)))
            {
                isolationKey.SetValue("Prefix", initialIsolation);
                isolationKey.SetValue("Postfix", restoredIsolation);
            }
        }

        private static string RegKeyName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);            
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_process_has_valid_regvalue_GetQueryPrefix_returns_valid_string()
        {
            SetRegistry(ReadCommitted, ReadUncommitted);
            var prefixSql = QueryIsolation.GetQueryPrefix();
            Assert.That(prefixSql, Is.EqualTo(string.Format(IsolationFormat, ReadCommitted)));
            var postfixSql = QueryIsolation.GetQueryPostfix();
            Assert.That(postfixSql, Is.EqualTo(string.Format(IsolationFormat, ReadUncommitted)));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_process_has_no_regvalue_GetQueryPrefix_returns_empty_string()
        {
            var prefixSql = QueryIsolation.GetQueryPrefix();
            Assert.That(prefixSql, Is.Empty);
        }

        /// <summary>
        /// Both prefix and postfix must be valid for either to be used
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_process_has_valid_prefix_and_invalid_postfix_GetQueryPostfix_and_GetQueryPrefix_are_empty()
        {
            SetRegistry(ReadCommitted, InvalidLevel);
            var postfixSql = QueryIsolation.GetQueryPostfix();
            var prefixSql = QueryIsolation.GetQueryPrefix();
            Assert.That(postfixSql, Is.Empty);
            Assert.That(prefixSql, Is.Empty);            
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_process_has_invalid_prefix_and_valid_postfix_GetQueryPostfix_and_GetQueryPrefix_are_empty()
        {
            SetRegistry(InvalidLevel, ReadCommitted);
            var postfixSql = QueryIsolation.GetQueryPostfix();
            var prefixSql = QueryIsolation.GetQueryPrefix();
            Assert.That(postfixSql, Is.Empty);
            Assert.That(prefixSql, Is.Empty);
        }
    }
}
#endif
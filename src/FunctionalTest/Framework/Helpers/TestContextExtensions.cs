// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Extension methods for TestContext
    /// </summary>
    public static class TestContextExtensions
    {
        /// <summary>
        /// Tests the given name against the set of names in the SqlTestTargetServersFilter parameter.
        /// The parameter is a semi-colon delimited list of allowed server names for the test run.
        /// This filter is intersected with the environment variable of the same name. 
        /// </summary>
        /// <param name="testContext"></param>
        /// <param name="serverName"></param>
        /// <returns>true if the filter is empty or if the given name is included in the list</returns>
        public static bool SqlTestTargetServersFilter(this TestContext testContext, string serverName)
        {
            var strings = testContext.Properties["SqlTestTargetServersFilter"]?.ToString()
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            return strings?.Contains(serverName, StringComparer.OrdinalIgnoreCase) ?? true;
        }
    }
}

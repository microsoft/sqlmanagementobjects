// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// The different categories of tests. A test can have multiple categories,
    /// so a separate category should be made for each scenario or grouping of
    /// tests that exists
    /// </summary>
    public enum SqlTestCategory
    {
        /// <summary>
        /// For tests that should be run as part of a release signoff
        /// </summary>
        Signoff,
        /// <summary>
        /// For tests that validate regressions (possible future or previously occured) from happening
        /// </summary>
        NoRegression,
        /// <summary>
        /// Baseline verification tests for SMO
        /// </summary>
        SmoBaseline,
        /// <summary>
        /// Tests that are currently in staging - they will not be ran as part of "official" test runs
        ///  until they've been validated as being stable
        /// </summary>
        Staging,
        /// <summary>
        /// For tests that cover legacy functionality to skip during most runs
        /// </summary>
        Legacy
    }

    /// <summary>
    /// Helper attribute to mark test methods with different test categories which can be used to selectively
    /// group and run tests. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class SqlTestCategoryAttribute : TestCategoryBaseAttribute
    {
        private IList<SqlTestCategory> _categories;

        /// <summary>
        /// Constructs a new SqlTestCategoryAttribute with the given categories
        /// </summary>
        /// <param name="categories"></param>
        public SqlTestCategoryAttribute(params SqlTestCategory[] categories)
        {
            _categories = categories;
        }

        /// <summary>
        /// Returns the list of categories associated with this attribute
        /// </summary>
        public override IList<string> TestCategories
        {
            get 
            {
                return _categories.Select(c => c.ToString()).ToList();
            }
        }
    }
}

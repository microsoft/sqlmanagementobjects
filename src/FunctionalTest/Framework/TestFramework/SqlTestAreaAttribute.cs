// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// The different areas of tests. This is the logical grouping of functionality
    /// provided, usually wrapped into a single feature or component. 
    /// </summary>
    public enum SqlTestArea
    {
        AlwaysOn,
        AutoParameterization,
        ConnectionDialog,
        ExtendedEvents,
        GraphDb,
        Hekaton,
        ObjectExplorer,
        OptionsDialog,
        PBM,
        Polybase,
        QueryStore,
        RegisteredServers,
        Showplan,
        SMO
    }

    /// <summary>
    /// Helper attribute to mark test methods with different test areas which can be used to selectively
    /// group and run tests. While a test can have multiple areas assigned to it that should be a rare
    /// scenario. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SqlTestAreaAttribute : TestCategoryBaseAttribute
    {
        private IList<SqlTestArea> _areas;

        public SqlTestAreaAttribute(params SqlTestArea[] areas)
        {
            _areas = areas;
        }

        public override IList<string> TestCategories
        {
            get
            {
                return _areas.Select(c => c.ToString()).ToList();
            }
        }
    }
}

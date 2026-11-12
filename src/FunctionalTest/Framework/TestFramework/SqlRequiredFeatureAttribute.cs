// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{

    /// <summary>
    /// The attribute to describe the required features that must be enabled on the target servers in order to run the test
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SqlRequiredFeatureAttribute : Attribute
    {
        private SqlFeature[] _requiredFeatures;


        /// <summary>
        /// Constructor
        /// </summary>
        public SqlRequiredFeatureAttribute(params SqlFeature[] requiredFeatures)
        {
            _requiredFeatures = requiredFeatures;
        }

        public SqlFeature[] RequiredFeatures
        {
            get { return _requiredFeatures; }
        }
    }
}

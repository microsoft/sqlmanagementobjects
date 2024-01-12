// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Attribute to assign to a TestMethod or TestClass to identify a SqlFeature whose presence in the enabled_features
    /// tag should block the test
    /// </summary>
    public class UnsupportedFeatureAttribute : SqlUnsupportedDimensionAttribute
    {
        private readonly SqlFeature feature;

        /// <summary>
        /// Constructs a new UnsupportedFeatureAttribute for the given blocking feature 
        /// </summary>
        /// <param name="feature"></param>
        public UnsupportedFeatureAttribute(SqlFeature feature) 
        {
            this.feature = feature;
        }

        /// <summary>
        /// Returns true if the enabled_features attribute of serverDescriptor does not include the blocking feature
        /// </summary>
        /// <param name="server"></param>
        /// <param name="serverDescriptor"></param>
        /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public override bool IsSupported(Management.Smo.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            return !serverDescriptor.EnabledFeatures.Any(f => f == feature);
        }

        /// <summary>
        /// Throws NotImplementedException
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool IsSupported(Management.Smo.Server server)
        {
            throw new NotImplementedException();
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public class TestDescriptor
    {

        /// <summary>
        /// Expected DatabaseEngineType
        /// </summary>
        public DatabaseEngineType DatabaseEngineType { get; set; }

        /// <summary>
        /// Enabled features on the server
        /// </summary>
        public IEnumerable<SqlFeature> EnabledFeatures { get; set; }

        /// <summary>
        /// The features that the server is reserved for.
        /// </summary>
        public IEnumerable<SqlFeature> ReservedFor { get; set; } = Enumerable.Empty<SqlFeature>();

        /// <summary>
        /// Expected HostPlatform
        /// </summary>
        public string HostPlatform { get; set; }

        /// <summary>
        /// Name used to identify the server in configuration
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Expected DatabaseEngineEdition. Will be Unknown if not provided in the XML
        /// </summary>
        public DatabaseEngineEdition DatabaseEngineEdition { get; set; }

        /// <summary>
        /// Major version number, eg 13 for SQL2016. 0 if not specified
        /// </summary>
        public int MajorVersion { get; set; }
 
        public string Description { get; set; } 
    }
}

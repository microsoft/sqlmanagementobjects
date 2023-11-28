// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Management.Collector;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Tests for the data collector enumerator
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = HostPlatformNames.Windows)]
    public class CollectorTests : SqlTestBase
    {
        [TestMethod]
        public void CollectorConfigStore_enumerates_system_collection_sets()
        {
            ExecuteTest(() =>
           {
               var configStore = new CollectorConfigStore(new SqlStoreConnection(ServerContext.ConnectionContext.SqlConnectionObject));
               var collectionSets = configStore.CollectionSets.AsEnumerable().Where(c => c.IsSystem);
               Assert.That(collectionSets.Select(c => c.Name), Is.EquivalentTo(new[] { "Disk Usage", "Query Statistics", "Server Activity", "Utility Information" }), "Unexpected system collection sets");
           });
        }
    }
}

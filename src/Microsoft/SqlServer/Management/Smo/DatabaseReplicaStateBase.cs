// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This class represents the runtime state of a database that's participating in an availability group.
    /// This database may be located on any of the replicas that compose the availability group.
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    public partial class DatabaseReplicaState : SqlSmoObject
    {
        internal DatabaseReplicaState(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Gets the name of the Availability Replica Server
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string AvailabilityReplicaServerName
        {
            get
            {
                return (string)this.Properties.GetValueWithNullReplacement("AvailabilityReplicaServerName");
            }
        }

        /// <summary>
        /// Gets the name of the Availability Database
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string AvailabilityDatabaseName
        {
            get
            {
                return (string)this.Properties.GetValueWithNullReplacement("AvailabilityDatabaseName");
            }
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "DatabaseReplicaState";
            }
        }
    }
}

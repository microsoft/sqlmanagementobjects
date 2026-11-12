// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    public class BasicAvailabilityGroupValidator : Validator
    {
        /// <summary>
        /// Basic AGs only allow the DefaultBackupPriority.
        /// </summary>
        private const int DefaultBackupPriority = 50;

        /// <summary>
        /// availabilityGroupData object contains information for the Availability Group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">Availability Group Data</param>
        public BasicAvailabilityGroupValidator(AvailabilityGroupData availabilityGroupData)
            : base(Resource.ValidatingBasicAvailabilityGroupOptions)
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// Validates the compatibility of the configuration options for BASIC Availability Groups.
        /// </summary>
        /// <param name="policy"></param>
        protected override void Validate(IExecutionPolicy policy)
        {
            if (this.availabilityGroupData.IsBasic)
            {
                if (this.availabilityGroupData.AvailabilityGroupReplicas.Count > 2)
                {
                    throw new BasicAvailabilityGroupIncompatibleException(Resource.BasicTooManyReplicasReason);
                }

                if (this.availabilityGroupData.ExistingAvailabilityDatabases.Count +
                    this.availabilityGroupData.NewAvailabilityDatabases.Count > 1)
                {
                    throw new BasicAvailabilityGroupIncompatibleException(Resource.BasicTooManyDatabasesReason);
                }

                foreach (AvailabilityGroupReplica replica in this.availabilityGroupData.AvailabilityGroupReplicas)
                {
                    if (replica.BackupPriority != DefaultBackupPriority)
                    {
                        throw new BasicAvailabilityGroupIncompatibleException(Resource.BasicBackupPriorityReason);
                    }

                    if (replica.ReadableSecondaryRole != Smo.AvailabilityReplicaConnectionModeInSecondaryRole.AllowNoConnections)
                    {
                        throw new BasicAvailabilityGroupIncompatibleException(Resource.BasicSecondaryRoleReason);
                    }
                }
            }
        }
    }
}

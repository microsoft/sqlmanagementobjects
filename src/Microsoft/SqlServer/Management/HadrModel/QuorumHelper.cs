// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This is a helper class that contains static methods for 
    /// validating the quroum configuration of an availability group.
    /// </summary>
    internal class QuorumHelper
    {
        /// <summary>
        /// Validates the quorum vote configuration of the given availability group.
        /// Nodes participating in an AG should only have a quroum vote if they 
        /// can host the primary replica or if the can host a automatic secondary
        /// partnered with the primary. Note the use of 'can' is due to the potential
        /// presence of FCIs.
        /// </summary>
        /// <param name="group">The availability group to validate.</param>
        /// <returns>True if the quorum vote configuration is valid, false otherwise.</returns>
        public static bool ValidateQuorumVoteConfiguration(AvailabilityGroup group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("AvailabilityGroup");
            }

            string primaryName = group.PrimaryReplicaServerName;
            AvailabilityReplica primary = null;

            
            if (!string.IsNullOrEmpty(primaryName))
            {
                primary = group.AvailabilityReplicas[primaryName];
            }

            if (primary == null)
            {
                throw new ArgumentNullException("primaryReplica");
            }

            bool primaryIsAutomatic = primary.FailoverMode == AvailabilityReplicaFailoverMode.Automatic;

            DataTable table = group.EnumReplicaClusterNodes();

            // Ensure the primary replica has a quorum vote
            // (Note that a replica can have more than one potential owner if it's hosted on an FCI).
            if (!AllNodesHaveQuorumVote(primary.Name, table))
            {
                throw new QuorumHelperException(primary.Name);
            }

            // Ensure that automatic partners with the primary have a quorum vote. 
            // Update for 935100: No longer verify that nodes outside of automatic set don't have quorum vote.
            foreach (AvailabilityReplica ar in group.AvailabilityReplicas)
            {
                // Filter out the primary replica, since we've already processed it.
                if (string.Compare(ar.Name, primary.Name, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    if (primaryIsAutomatic && ar.FailoverMode == AvailabilityReplicaFailoverMode.Automatic)
                    {
                        // Ensure that all nodes that can host this replica have a quorum vote.
                        // (Note that a replica can have more than one potential owner if it's hosted on an FCI).
                        if (!AllNodesHaveQuorumVote(ar.Name, table))
                        {
                            throw new QuorumHelperException(ar.Name);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determine if all nodes that can host a given replica have a vote. Note that a replica
        /// can have more than one potential owner if its hosted on an FCI.
        /// </summary>
        /// <param name="replica">Name of the replica.</param>
        /// <param name="table">DataTable with vote data.</param>
        /// <returns>True if all nodes have a vote, false otherwise.</returns>
        private static bool AllNodesHaveQuorumVote(string replica, DataTable table)
        {
            return GetReplicaNodes(replica, table)
                .Select(row => row["NumberOfQuorumVotes"])
                .All(value => (value != null) && (value is int) && (int)value > 0);
        }

        /// <summary>
        /// Determine if any nodes that can host a given replica have a vote. Note that a replica
        /// can have more than one potential owner if its hosted on an FCI.
        /// </summary>
        /// <param name="replica">Name of the replica.</param>
        /// <param name="table">DataTable with vote data.</param>
        /// <returns>True if any nodes have a vote, false otherwise.</returns>
        private static bool AnyNodesHaveQuorumVote(string replica, DataTable table)
        {
            return GetReplicaNodes(replica, table)
                .Select(row => row["NumberOfQuorumVotes"])
                .Any(value => (value != null) && (value is int) && (int)value > 0);
        }

        /// <summary>
        /// Retrieves all nodes that can host a replica. Note that a replica
        /// can have more than one potential owner if its hosted on an FCI.
        /// </summary>
        /// <param name="targetReplicaName">Name of the replica.</param>
        /// <param name="table">DataTable with vote data.</param>
        /// <returns>All nodes that can host the given replica.</returns>
        private static IEnumerable<DataRow> GetReplicaNodes(string targetReplicaName, DataTable table)
        {
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    string name = row["ReplicaName"] as string;
                    if (string.Compare(targetReplicaName, name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        yield return row;
                    }
                }
            }
        }
    }
}

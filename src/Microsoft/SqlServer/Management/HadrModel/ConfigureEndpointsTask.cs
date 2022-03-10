// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to configure endpoints on a replica
    /// </summary>
    public class ConfigureEndpointsTask : HadrTask, IScriptableTask
    {
        /// <summary>
        /// The replica data
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// The login names to grant connect rights to
        /// </summary>
        private readonly IEnumerable<string> loginNames;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replica">The replica data</param>
        /// <param name="loginNames">The logins to grant connect rights to</param>
        public ConfigureEndpointsTask(AvailabilityGroupReplica replica, IEnumerable<string> loginNames)
            : base(Resource.ConfigureEndpointsText)
        {

            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            if (loginNames == null)
            {
                throw new ArgumentNullException("loginNames");
            }

            this.replica = replica;
            this.loginNames = loginNames;

            this.ScriptingConnections = new List<ServerConnection>
                {
                    this.replica.AvailabilityGroupReplicaData.Connection
                };
        }

        /// <summary>
        /// Connections to use for scripting
        /// </summary>
        public List<ServerConnection> ScriptingConnections
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates the endpoint and grants access to the logins
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            // This task will only be tried once.
            policy.Expired = true;

            this.replica.ConfigureEndpoint();
            this.replica.AddGrantServiceAccount(this.loginNames);
        }

        /// <summary>
        /// Rollback is not supported for this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task that configures AlwaysOn_health Xevent session to autostart
    /// </summary>
    public class StartAlwaysOnXeventSessionTask : HadrTask
    {
        /// <summary>
        /// Server side XEvent session name
        /// </summary>
        public const string AlwaysOnHealthSessionName = "AlwaysOn_health";

        /// <summary>
        /// The XEvent store
        /// </summary>
        private XEStore store;

        /// <summary>
        /// The connection to the XEvent store
        /// </summary>
        private SqlStoreConnection storeConnection;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replica">The replica data</param>
        public StartAlwaysOnXeventSessionTask(AvailabilityGroupReplica replica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.StartAlwaysOnXeventSessionText, AlwaysOnHealthSessionName, replica.AvailabilityGroupReplicaData.ReplicaName))
        {
            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            this.Replica = replica;
            this.storeConnection = new SqlStoreConnection(replica.AvailabilityGroupReplicaData.Connection.SqlConnectionObject);
            this.store = new XEStore(this.storeConnection);
        }

        /// <summary>
        /// Gets the replica data
        /// </summary>
        public AvailabilityGroupReplica Replica { get; private set; }

        /// <summary>
        /// Configures AlwaysOn_health XEvent session to autostart
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            this.UpdateStatus(new TaskEventArgs(this.Name,
                string.Format(Resource.EnablingXeventOnReplica,
                                AlwaysOnHealthSessionName,
                                this.Replica.AvailabilityGroupReplicaData.ReplicaName),
                TaskEventStatus.Running));

            // This task will only be tried once.
            policy.Expired = true;

            Session alwaysOnHealthSession;

            if (this.store.Sessions.TryGetValue(new Session.Key(AlwaysOnHealthSessionName), out alwaysOnHealthSession))
            {
                alwaysOnHealthSession.AutoStart = true;
                alwaysOnHealthSession.Alter();

                if (!alwaysOnHealthSession.IsRunning)
                {
                    alwaysOnHealthSession.Start();
                }
            }
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

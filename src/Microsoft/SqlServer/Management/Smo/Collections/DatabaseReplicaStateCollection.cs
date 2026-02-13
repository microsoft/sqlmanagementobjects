// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Collection of DatabaseReplicaState objects associated with an AvailabilityGroup
    ///</summary>
    public sealed class DatabaseReplicaStateCollection : DatabaseReplicaStateCollectionBase
    {

        internal DatabaseReplicaStateCollection(SqlSmoObject parentInstance) : base(parentInstance)
        {
        }


        public AvailabilityGroup Parent => ParentInstance as AvailabilityGroup;


        public DatabaseReplicaState this[string replicaName, string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }
                else if (replicaName == null)
                {
                    throw new ArgumentNullException("replica name cannot be null");
                }

                return GetObjectByKey(new DatabaseReplicaStateObjectKey(replicaName, name)) as DatabaseReplicaState;
            }
        }



        public void Remove(string replicaName, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name cannot be null");
            }
            else if (replicaName == null)
            {
                throw new ArgumentNullException("replica name cannot be null");
            }

            Remove(new DatabaseReplicaStateObjectKey(replicaName, name));
        }

        public void Remove(DatabaseReplicaState DatabaseReplicaState)
        {
            if (null == DatabaseReplicaState)
                throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("DatabaseReplicaState"));

            RemoveObj(DatabaseReplicaState, new DatabaseReplicaStateObjectKey(DatabaseReplicaState.AvailabilityReplicaServerName, DatabaseReplicaState.AvailabilityDatabaseName));
        }


        public void Add(DatabaseReplicaState DatabaseReplicaState)
        {
            if (null == DatabaseReplicaState)
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("DatabaseReplicaState"));

            AddImpl(DatabaseReplicaState);
        }
    }
}

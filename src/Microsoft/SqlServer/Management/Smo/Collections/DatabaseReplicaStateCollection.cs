// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed collection of MAPPED_TYPE objects
    /// Supports indexing objects by their AvailabilityReplicaServerName and AvailabilityDatabaseName properties
    ///</summary>
    public sealed class DatabaseReplicaStateCollection : DatabaseReplicaStateCollectionBase
    {

        internal DatabaseReplicaStateCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }


        public AvailabilityGroup Parent
        {
            get
            {
                return this.ParentInstance as AvailabilityGroup;
            }
        }


        public DatabaseReplicaState this[Int32 index]
        {
            get
            { 
                return GetObjectByIndex(index) as DatabaseReplicaState;
            }
        }

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

        public void CopyTo(DatabaseReplicaState[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }


        public DatabaseReplicaState ItemById(int id)
        {
            return (DatabaseReplicaState)GetItemById(id);
        }


        protected override Type GetCollectionElementType()
        {
            return typeof(DatabaseReplicaState);
        }

        internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return new DatabaseReplicaState(this, key, state);
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

                this.Remove(new DatabaseReplicaStateObjectKey(replicaName, name));
            }

            public void Remove(DatabaseReplicaState DatabaseReplicaState)
            {
                if( null == DatabaseReplicaState )
                    throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("DatabaseReplicaState"));
            
                RemoveObj(DatabaseReplicaState, new DatabaseReplicaStateObjectKey(DatabaseReplicaState.AvailabilityReplicaServerName, DatabaseReplicaState.AvailabilityDatabaseName));
            }


            public void Add(DatabaseReplicaState DatabaseReplicaState) 
            {
                if( null == DatabaseReplicaState )
                    throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("DatabaseReplicaState"));
            
                AddImpl(DatabaseReplicaState);
            }
    }
}

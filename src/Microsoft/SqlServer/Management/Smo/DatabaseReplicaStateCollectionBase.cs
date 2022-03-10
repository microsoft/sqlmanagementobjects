// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// this is the class that contains common features of all schema collection classes
    ///</summary>
    public class DatabaseReplicaStateCollectionBase : SortedListCollectionBase
    {
        internal DatabaseReplicaStateCollectionBase(SqlSmoObject parent)
            : base(parent)
        {
        }

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new DatabaseReplicaStateObjectComparer(this.StringComparer));
        }

        protected override Type GetCollectionElementType()
        {
            return typeof(DatabaseReplicaState);
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            string databaseName = urn.GetAttribute("AvailabilityDatabaseName");
            string replicaName = urn.GetAttribute("AvailabilityReplicaServerName");

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("AvailabilityDatabaseName", urn.Type));
            }

            if (string.IsNullOrEmpty(replicaName))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("AvailabilityReplicaServerName", urn.Type));
            }

            return new DatabaseReplicaStateObjectKey(replicaName, databaseName);
        }
    }

    internal class DatabaseReplicaStateObjectComparer : ObjectComparerBase
    {
        public DatabaseReplicaStateObjectComparer(IComparer stringComparer) 
            : base(stringComparer)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            DatabaseReplicaStateObjectKey dbr1 = obj1 as DatabaseReplicaStateObjectKey;
            DatabaseReplicaStateObjectKey dbr2 = obj2 as DatabaseReplicaStateObjectKey;

            Diagnostics.TraceHelper.Assert((null != dbr1 || null != dbr2), "Can't compare null objects for DatabaseReplicaState");

            //We order first by Avaialbility Replica name, then by Database name
            int replicaNameComparison = this.stringComparer.Compare(dbr1.ReplicaName, dbr2.ReplicaName);
            if (replicaNameComparison == 0)
            {
                return this.stringComparer.Compare(dbr1.DatabaseName, dbr2.DatabaseName);
            }
            else
            {
                return replicaNameComparison;
            }
        }
    }

    internal class DatabaseReplicaStateObjectKey : ObjectKeyBase
    {
        internal static StringCollection fields;

        public DatabaseReplicaStateObjectKey(string replicaName, string databaseName)
        {
            this.ReplicaName = replicaName;
            this.DatabaseName = databaseName;
        }

        static DatabaseReplicaStateObjectKey()
        {
            fields = new StringCollection();
            fields.Add("AvailabilityReplicaServerName");
            fields.Add("AvailabilityDatabaseName");
        }

        public string ReplicaName
        {
            get;
            set;
        }

        public string DatabaseName
        {
            get;
            set;
        }

        public override string UrnFilter
        {
            get
            {
                return string.Format(SmoApplication.DefaultCulture, "@AvailabilityReplicaServerName='{0}' and @AvailabilityDatabaseName='{1}'", SqlSmoObject.SqlString(this.ReplicaName), SqlSmoObject.SqlString(this.DatabaseName));
            }
        }

        public override StringCollection GetFieldNames()
        {
            return DatabaseReplicaStateObjectKey.fields;
        }

        internal override void Validate(Type objectType)
        {
            if (string.IsNullOrEmpty(this.ReplicaName) || string.IsNullOrEmpty(this.DatabaseName))
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
            }
        }

        public override bool IsNull
        {
            get
            {
                return null == this.DatabaseName || null == this.ReplicaName;
            }
        }

        public override string GetExceptionName()
        {
            return string.Format(SmoApplication.DefaultCulture, "Database {1} in Availability Replica {0}", this.ReplicaName, this.DatabaseName);
        }

        public override ObjectKeyBase Clone()
        {
            return new DatabaseReplicaStateObjectKey(this.ReplicaName, this.DatabaseName);
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new DatabaseReplicaStateObjectComparer(stringComparer);
        }

        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}/{1}", this.ReplicaName, this.DatabaseName);
        }
    }
}

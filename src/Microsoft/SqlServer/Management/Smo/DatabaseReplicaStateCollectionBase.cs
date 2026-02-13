// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Collection of DatabaseReplicaState objects associated with an AvailabilityGroup
    ///</summary>
    public class DatabaseReplicaStateCollectionBase : SortedListCollectionBase<DatabaseReplicaState, AvailabilityGroup>
    {
        internal DatabaseReplicaStateCollectionBase(SqlSmoObject parent)
            : base((AvailabilityGroup)parent)
        {
        }

        protected override string UrnSuffix => DatabaseReplicaState.UrnSuffix;

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList<DatabaseReplicaState>(new DatabaseReplicaStateObjectComparer(StringComparer));
        }


        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var databaseName = urn.GetAttribute("AvailabilityDatabaseName");
            var replicaName = urn.GetAttribute("AvailabilityReplicaServerName");

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

        internal override DatabaseReplicaState GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            throw new NotImplementedException();
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
            var dbr1 = obj1 as DatabaseReplicaStateObjectKey;
            var dbr2 = obj2 as DatabaseReplicaStateObjectKey;

            Debug.Assert(null != dbr1 || null != dbr2, "Can't compare null objects for DatabaseReplicaState");

            //We order first by Avaialbility Replica name, then by Database name
            var replicaNameComparison = stringComparer.Compare(dbr1.ReplicaName, dbr2.ReplicaName);
            if (replicaNameComparison == 0)
            {
                return stringComparer.Compare(dbr1.DatabaseName, dbr2.DatabaseName);
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
            ReplicaName = replicaName;
            DatabaseName = databaseName;
        }

        static DatabaseReplicaStateObjectKey()
        {
            fields = new StringCollection
            {
                "AvailabilityReplicaServerName",
                "AvailabilityDatabaseName"
            };
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
                return string.Format(SmoApplication.DefaultCulture, "@AvailabilityReplicaServerName='{0}' and @AvailabilityDatabaseName='{1}'", SqlSmoObject.SqlString(ReplicaName), SqlSmoObject.SqlString(DatabaseName));
            }
        }

        public override StringCollection GetFieldNames()
        {
            return DatabaseReplicaStateObjectKey.fields;
        }

        internal override void Validate(Type objectType)
        {
            if (string.IsNullOrEmpty(ReplicaName) || string.IsNullOrEmpty(DatabaseName))
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
            }
        }

        public override bool IsNull
        {
            get
            {
                return null == DatabaseName || null == ReplicaName;
            }
        }

        public override string GetExceptionName()
        {
            return string.Format(SmoApplication.DefaultCulture, "Database {1} in Availability Replica {0}", ReplicaName, DatabaseName);
        }

        public override ObjectKeyBase Clone()
        {
            return new DatabaseReplicaStateObjectKey(ReplicaName, DatabaseName);
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new DatabaseReplicaStateObjectComparer(stringComparer);
        }

        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}/{1}", ReplicaName, DatabaseName);
        }
    }
}

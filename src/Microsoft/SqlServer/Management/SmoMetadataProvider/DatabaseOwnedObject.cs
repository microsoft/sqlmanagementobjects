// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class DatabaseOwnedObject<S> : DatabaseObject<S, Database>, IDatabaseOwnedObject
        where S : Smo.NamedSmoObject
    {
        protected DatabaseOwnedObject(S smoMetadataObject, Database parent)
            : base(smoMetadataObject, parent)
        {
        }

        public Database Database
        {
            get { return this.m_parent; }
        }

        public sealed override T Accept<T>(IDatabaseObjectVisitor<T> visitor)
        {
            return this.Accept((IDatabaseOwnedObjectVisitor<T>)visitor);
        }

        public sealed override void AssertNameMatches(string name, string detailedMessage)
        {
            Debug.Assert(name != null, "SmoMetadataProvider Assert", "name != null");

            // retrieve and use database collation info
            CollationInfo collationInfo = this.m_parent.CollationInfo;
            Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");

            Debug.Assert(collationInfo.EqualityComparer.Equals(this.Name, name), "SmoMetadataProvider Assert", detailedMessage);
        }


        #region IDatabaseOwnedObject Members
        IDatabase IDatabaseOwnedObject.Database
        {
            get { return this.m_parent; }
        }

        abstract public T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor);
        #endregion
    }
}

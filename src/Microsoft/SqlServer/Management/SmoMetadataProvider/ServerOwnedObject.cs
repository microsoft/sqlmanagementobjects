// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class ServerOwnedObject<S> : DatabaseObject<S, Server>, IServerOwnedObject
        where S : Smo.NamedSmoObject
    {
        protected ServerOwnedObject(S smoMetadataObject, Server parent)
            : base(smoMetadataObject, parent)
        {
        }

        public Server Server
        {
            get { return this.m_parent; }
        }

        public sealed override T Accept<T>(IDatabaseObjectVisitor<T> visitor)
        {
            return this.Accept((IServerOwnedObjectVisitor<T>)visitor);
        }

        public sealed override void AssertNameMatches(string name, string detailedMessage)
        {
            Debug.Assert(name != null, "SmoMetadataProvider Assert", "name != null");

            // retrieve and use server collation info
            CollationInfo collationInfo = this.m_parent.CollationInfo;
            Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");

            Debug.Assert(collationInfo.EqualityComparer.Equals(this.Name, name), "SmoMetadataProvider Assert", detailedMessage);
        }

        #region IServerOwnedObject Members
        IServer IServerOwnedObject.Server
        {
            get { return this.m_parent; }
        }

        abstract public T Accept<T>(IServerOwnedObjectVisitor<T> visitor);
        #endregion
    }
}

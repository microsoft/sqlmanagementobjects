// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class SchemaOwnedObject<S> : DatabaseObject<S, Schema>, ISchemaOwnedObject
        where S : Smo.ScriptSchemaObjectBase
    {
        protected SchemaOwnedObject(S smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
            parent.AssertNameMatches(smoMetadataObject.Schema,"Schema object name does not match SMO schema name!");
        }

        public override void AssertNameMatches(string name, string detailedMessage)
        {
            Debug.Assert(name != null, "SmoMetadataProvider Assert", "name != null");

            // retrieve and use database collation info
            CollationInfo collationInfo = this.m_parent.Database.CollationInfo;
            Debug.Assert(collationInfo != null, "SmoMetadataProvider Assert", "collationInfo != null");

            Debug.Assert(collationInfo.EqualityComparer.Equals(this.Name, name), "SmoMetadataProvider Assert", detailedMessage);
        }

        public sealed override T Accept<T>(IDatabaseObjectVisitor<T> visitor)
        {
            return this.Accept((ISchemaOwnedObjectVisitor<T>)visitor);
        }

        public CollationInfo CollationInfo
        {
            get { return this.m_parent.Database.CollationInfo; }
        }

        #region ISchemaObject Members
        public ISchema Schema
        {
            get { return this.m_parent; }
        }

        abstract public T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor);
        #endregion
    }
}

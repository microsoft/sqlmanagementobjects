// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    internal abstract class DatabaseObject<S, P> : DatabaseObjectBase, IDatabaseObject, ISmoDatabaseObject
        where S : Smo.NamedSmoObject
        where P : class, IDatabaseObject
    {
        protected readonly S m_smoMetadataObject;
        protected readonly P m_parent;

        protected DatabaseObject(S smoMetadataObject, P parent)
        {
            Debug.Assert(smoMetadataObject != null, "SmoMetadataProvider Assert", "smoMetadataObject != null");
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");

            this.m_smoMetadataObject = smoMetadataObject;
            this.m_parent = parent;
        }

        public P Parent
        {
            get { return this.m_parent; }
        }

        public S SmoObject
        {
            get { return this.m_smoMetadataObject; }
        }

        abstract public int Id { get; }

        #region IDatabaseObject Members
        IDatabaseObject IDatabaseObject.Parent
        {
            get { return this.m_parent; }
        }

        abstract public bool IsSystemObject { get; }

        public bool IsVolatile
        {
            get { return false; }
        }

        abstract public T Accept<T>(IDatabaseObjectVisitor<T> visitor);
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoMetadataObject.Name; }
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            return this.Accept((IDatabaseObjectVisitor<T>)visitor);
        }
        #endregion

        #region ISmoDatabaseObject Members

        Smo.SqlSmoObject ISmoDatabaseObject.SmoObject
        {
            get { return this.m_smoMetadataObject; }
        }

        #endregion

        [Conditional("DEBUG")]
        public abstract void AssertNameMatches(string name, string detailedMessage);
    }
}

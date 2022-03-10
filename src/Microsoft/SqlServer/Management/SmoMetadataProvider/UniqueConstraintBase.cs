// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class UniqueConstraintBase : IUniqueConstraintBase
    {
        private readonly IDatabaseTable m_parent;
        private readonly IRelationalIndex m_index;

        protected UniqueConstraintBase(IDatabaseTable parent, IRelationalIndex index)
        {
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
            Debug.Assert(index != null, "SmoMetadataProvider Assert", "index != null");

            this.m_parent = parent;
            this.m_index = index;
        }

        #region IConstraint Members

        public ITabular Parent
        {
            get { return this.m_parent; }
        }

        public bool IsSystemNamed
        {
            get { return this.m_index.IsSystemNamed; }
        }

        abstract public ConstraintType Type { get; }
        #endregion

        #region IUniqueConstraintBase Members
        public IRelationalIndex AssociatedIndex
        {
            get { return this.m_index; }
        }
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_index.Name; }
        }

        public abstract T Accept<T>(IMetadataObjectVisitor<T> visitor);
        #endregion
    }
}

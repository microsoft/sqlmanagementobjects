// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class CheckConstraint : ICheckConstraint, ISmoDatabaseObject
    {
        private readonly IDatabaseTable m_parent;
        private readonly Smo.Check m_smoCheckConstraint;

        public CheckConstraint(IDatabaseTable parent, Smo.Check smoCheckConstraint)
        {
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
            Debug.Assert(smoCheckConstraint != null, "SmoMetadataProvider Assert", "smoCheckConstraint != null");

            this.m_parent = parent;
            this.m_smoCheckConstraint = smoCheckConstraint;
        }

        #region IConstraint Members

        public ITabular Parent
        {
            get { return this.m_parent; }
        }

        public bool IsSystemNamed
        {
            get { return this.m_smoCheckConstraint.IsSystemNamed; }
        }

        public ConstraintType Type
        {
            get { return ConstraintType.Check; }
        }

        #endregion

        #region ICheckConstraint Members
        public bool IsEnabled
        {
            get { return this.m_smoCheckConstraint.IsEnabled; }
        }

        public bool IsChecked
        {
            get { return this.m_smoCheckConstraint.IsChecked; }
        }

        public bool NotForReplication
        {
            get { return this.m_smoCheckConstraint.NotForReplication; }
        }

        public string Text
        {
            get { return this.m_smoCheckConstraint.Text; }
        }
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoCheckConstraint.Name; }
        }

        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
        #endregion

        #region ISmoDatabaseObject Members

        public Microsoft.SqlServer.Management.Smo.SqlSmoObject SmoObject
        {
            get { return this.m_smoCheckConstraint; }
        }

        #endregion
    }
}

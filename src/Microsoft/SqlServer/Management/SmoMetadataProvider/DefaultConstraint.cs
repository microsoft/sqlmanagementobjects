// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class DefaultConstraint : IDefaultConstraint, ISmoDatabaseObject
    {
        private readonly IColumn parent;
        private readonly Smo.DefaultConstraint smoDefaultConstraint;

        public DefaultConstraint(IColumn parent, Smo.DefaultConstraint smoDefaultConstraint)
        {
            Debug.Assert(parent != null, "SmoMetadataProvider Assert", "parent != null");
            Debug.Assert(smoDefaultConstraint != null, "SmoMetadataProvider Assert", "smoDefaultConstraint != null");

            this.parent = parent;
            this.smoDefaultConstraint = smoDefaultConstraint;
        }

        #region IDefaultConstraint Members

        public IColumn Parent
        {
            get { return this.parent; }
        }

        public bool IsSystemNamed
        {
            get 
            {
                bool? isSystemNamed;

                Utils.TryGetPropertyValue<bool>(this.smoDefaultConstraint, "IsSystemNamed", out isSystemNamed);

                return isSystemNamed.GetValueOrDefault();
            }
        }

        public string Text
        {
            get { return this.smoDefaultConstraint.Text; }
        }
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.smoDefaultConstraint.Name; }
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
            get { return this.smoDefaultConstraint; }
        }

        #endregion
    }
}

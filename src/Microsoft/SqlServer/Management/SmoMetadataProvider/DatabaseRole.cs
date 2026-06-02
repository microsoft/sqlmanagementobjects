// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class DatabaseRole : DatabasePrincipal<Smo.DatabaseRole>, IDatabaseRole
    {
        private bool? isSystemObject;

        public DatabaseRole(Smo.DatabaseRole smoMetadataObject, Database parent)
            : base(smoMetadataObject, parent)
        {
        }

        public override T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get 
            {
                if (!this.isSystemObject.HasValue)
                {
                    this.isSystemObject = this.IsFixedRole || this.SmoObject == this.Parent.SmoObject.Roles["public"];
                }

                Debug.Assert(this.isSystemObject.HasValue, "SmoMetadataProvider Assert", "(this.isSystemObject.HasValue == true");
                return this.isSystemObject.Value;
            }
        }

        protected override IEnumerable<string> GetMemberOfRoleNames()
        {
            return this.m_smoMetadataObject.EnumRoles().Cast<string>();
        }

        #region IDatabaseRole Members
        public bool IsFixedRole
        {
            get { return this.m_smoMetadataObject.IsFixedRole; }
        }

        public IDatabasePrincipal Owner
        {
            get 
            {
                String ownerName;
                Utils.TryGetPropertyObject<String>(this.m_smoMetadataObject, "Owner", out ownerName);

                return (ownerName != null) ?
                    Utils.GetDatabasePrincipal(this.Parent, ownerName):
                    null;
            }
        }
        #endregion
    }
}

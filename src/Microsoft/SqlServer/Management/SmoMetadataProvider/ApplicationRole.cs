// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class ApplicationRole : DatabasePrincipal<Smo.ApplicationRole>, IApplicationRole
    {
        public ApplicationRole(Smo.ApplicationRole smoMetadataObject, Database parent)
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
            get { return false; }
        }

        protected override IEnumerable<string> GetMemberOfRoleNames()
        {
            return Enumerable.Empty<string>();
        }

        #region IApplicationRole Members
        public ISchema DefaultSchema
        {
            get { return this.Parent.Schemas[this.m_smoMetadataObject.DefaultSchema]; }
        }
        #endregion
    }
}

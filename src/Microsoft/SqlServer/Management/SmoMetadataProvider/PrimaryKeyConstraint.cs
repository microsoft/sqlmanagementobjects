// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class PrimaryKeyConstraint : UniqueConstraintBase, IPrimaryKeyConstraint
    {
        public PrimaryKeyConstraint(IDatabaseTable parent, IRelationalIndex index)
            : base(parent, index)
        {
        }

        public override ConstraintType Type
        {
            get { return ConstraintType.PrimaryKey; }
        }

        public override T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
    }
}

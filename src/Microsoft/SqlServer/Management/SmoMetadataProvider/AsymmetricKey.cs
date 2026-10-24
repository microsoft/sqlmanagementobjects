// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class AsymmetricKey : DatabaseOwnedObject<Smo.AsymmetricKey>, IAsymmetricKey
    {
        public AsymmetricKey(Smo.AsymmetricKey smoMetadataObject, Database parent)
            : base(smoMetadataObject, parent)
        {
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return false; }
        }

        public override T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
    }
}

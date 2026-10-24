// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class Credential : ServerOwnedObject<Smo.Credential>, ICredential
    {
        public Credential(Smo.Credential smoMetadataObject, Server parent)
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

        public override T Accept<T>(IServerOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class XmlIndex : Index, IXmlIndex
    {
        public XmlIndex(IDatabaseTable parent, Smo.Index smoIndex)
            : base(parent, smoIndex)
        {
            Debug.Assert(smoIndex.IsXmlIndex, "SmoMetadataProvider Assert", "Expected XML SMO index!");
        }

        public override T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
    }
}

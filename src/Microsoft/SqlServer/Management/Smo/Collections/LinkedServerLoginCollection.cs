// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    public sealed partial class LinkedServerLoginCollection : SimpleObjectCollectionBase<LinkedServerLogin, LinkedServer>
    {
         internal override bool CanHaveEmptyName(Urn urn) => true;
    }
}

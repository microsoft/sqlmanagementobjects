// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// A collection of SystemMessage instances associated with a Server
    /// </summary>
    public sealed class SystemMessageCollection : MessageCollectionBase<SystemMessage>
    {

        internal SystemMessageCollection(SqlSmoObject parentInstance) : base(parentInstance)
        {
        }

        protected override string UrnSuffix => SystemMessage.UrnSuffix;

        internal override SystemMessage GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new SystemMessage(this, key, state);
    }
}

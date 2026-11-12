// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// A collection of UserDefinedMessage instances associated with a Server
    /// </summary>
    public sealed class UserDefinedMessageCollection : MessageCollectionBase<UserDefinedMessage>
    {

        internal UserDefinedMessageCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }

        protected override string UrnSuffix  => UserDefinedMessage.UrnSuffix;

        internal override UserDefinedMessage GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new UserDefinedMessage(this, key, state);

    }
}


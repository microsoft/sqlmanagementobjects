// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlServer.Management.XEventDbScoped
{
    /// <summary>
    /// Sql provider for Session at database scope
    /// </summary>
    internal class DatabaseSessionProvider : SessionProviderBase
    {
        public DatabaseSessionProvider(Session session) : base(session, "DATABASE")
        {
        }
    }
}
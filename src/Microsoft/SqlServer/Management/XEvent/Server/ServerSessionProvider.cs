// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Sql provider for Session at server scope.
    /// </summary>
    internal class ServerSessionProvider : SessionProviderBase
    {

        /// <summary>
        /// Constructs a ServerSessionProvider
        /// </summary>
        /// <param name="session"></param>
        public ServerSessionProvider(Session session) : base(session, 
            "EVENT SESSION " + SfcTsqlProcFormatter.MakeSqlBracket(session.Name) + " ON SERVER ", 
            sessionName => String.Format(CultureInfo.InvariantCulture, "CREATE EVENT SESSION {0} ON SERVER ", SfcTsqlProcFormatter.MakeSqlBracket(sessionName)))
        {
        }

    }
}
// Copyright (c) Microsoft.
// Licensed under the MIT license.

#if NETSTANDARD2_0 || NETCOREAPP
using System;

namespace Microsoft.SqlServer.Server
{


    ///<summary>
    ///Implementation of SqlContext.IsAvailable for use with the dotnetcore framework since SqlContext.IsAvailable isn't available on .NetCore
    ///</summary>
    public class SqlContext
    {
        public static bool IsAvailable = false;

        public static SqlPipe Pipe { get; }
    }

    public class SqlPipe
    {
         public void Send(string message)
         { }
    }
}
#endif
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Marks a test as being a "Disconnected" test, which means it will be run
    /// without making a server connection first. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DisconnectedTestAttribute : Attribute
    {
        public DisconnectedTestAttribute()
        {

        }
    }
}

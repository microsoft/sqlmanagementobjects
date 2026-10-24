// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when database fails to join the availability group
    /// </summary>
    public class JoinDatabaseToAvailabilityGroupException : HadrTaskBaseException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inner">Inner exception</param>
        public JoinDatabaseToAvailabilityGroupException(Exception inner) 
            : base(Resource.JoinAvailabilityGroupError, inner)
        {
        }
    }
}

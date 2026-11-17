// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown from QuorumHelper
    /// </summary>
    public class QuorumHelperException : HadrTaskBaseException
    {
        /// <summary>
        /// Exception with Replica Name and inner Exception
        /// </summary>
        public QuorumHelperException(string ReplicaName,Exception inner)
            : base(Resource.FormatQuorumHelperException(ReplicaName), inner)
        {
        }

        /// <summary>
        /// Exception with Replica Name
        /// </summary>
        public QuorumHelperException(string ReplicaName)
            : base(Resource.FormatQuorumHelperException(ReplicaName))
        {
        }
    }
}

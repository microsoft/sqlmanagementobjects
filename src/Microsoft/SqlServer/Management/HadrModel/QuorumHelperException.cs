// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

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
            : base(string.Format(CultureInfo.InvariantCulture, Resource.QuorumHelperException, ReplicaName), inner)
        {
        }

        /// <summary>
        /// Exception with Replica Name
        /// </summary>
        public QuorumHelperException(string ReplicaName)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.QuorumHelperException, ReplicaName))
        {
        }
    }
}

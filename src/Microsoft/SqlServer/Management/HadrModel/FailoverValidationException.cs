// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    public class FailoverValidationException : HadrTaskBaseException
    {
        /// <summary>
        /// Standard Exception With Replica Name and Replica Role
        /// FailoverTask Exception When the TargetReplica Is A primary Replica
        /// </summary>
        /// <param name="replicaName"></param>
        /// <param name="replicaRole"></param>
        public FailoverValidationException(string replicaName,string replicaRole):
            base(string.Format(CultureInfo.InvariantCulture,
                Resource.FailoverValidationException,
                replicaName, replicaRole))
        {

        }

        /// <summary>
        /// Exception With Replica Name and Replica Role and Inner Exception
        /// FailoverTask Exception When the TargetReplica Is A primary Replica
        /// </summary>
        /// <param name="replicaName"></param>
        /// <param name="replicaRole"></param>
        /// <param name="inner"></param>
        public FailoverValidationException(string replicaName, string replicaRole,Exception inner) :
            base(string.Format(CultureInfo.InvariantCulture,
                  Resource.FailoverValidationException,
                  replicaName, replicaRole),inner)
        {

        }
    }
}

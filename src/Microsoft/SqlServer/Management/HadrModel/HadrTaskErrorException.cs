// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// The Base Task Error Exception For HADRTask Model
    /// </summary>
    public class HadrTaskErrorException : Exception
    {
        /// <summary>
        /// Standard Exception with Message
        /// </summary>
        /// <param name="message"></param>
        public HadrTaskErrorException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Exception with Message and Inner Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public HadrTaskErrorException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// The Base Validation Exception For HADRTask Model
    /// </summary>
    public class HadrValidationBaseException : Exception
    {
        /// <summary>
        /// Standard Exception with Message
        /// </summary>
        /// <param name="message"></param>
        public HadrValidationBaseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Exception with Message and Inner Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public HadrValidationBaseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
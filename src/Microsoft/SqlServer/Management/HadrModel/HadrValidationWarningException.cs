// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// The Validation Warning Exception For HADRTask Model
    /// </summary>
    public class HadrValidationWarningException : Exception
    {
        /// <summary>
        /// Standard Exception with Message
        /// </summary>
        /// <param name="message"></param>
        public HadrValidationWarningException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Exception with Message and Inner Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public HadrValidationWarningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
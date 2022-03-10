// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This event argument for validator progress status update
    /// </summary>
    public sealed class ValidatorEventArgs : EventArgs
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="validatorName">The called validator's Name</param>
        /// <param name="validatorDetails">Validation Details</param>
        /// <param name="validatorStatus">Validation Status</param>
        public ValidatorEventArgs(string validatorName, string validatorDetails, string validatorStatus)
        {
            this.Name = validatorName;
            this.Status = validatorStatus;
            this.Details = validatorDetails;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="validatorName">The called validator's Name</param>
        /// <param name="validatorDetails">Validation Details</param>
        public ValidatorEventArgs(string validatorName, string validatorDetails)
        {
            this.Name = validatorName;
            this.Details = validatorDetails;
        }


        /// <summary>
        /// Current Validator Name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Current validation status
        /// </summary>
        public string Status
        {
            get;
            set;
        }

        /// <summary>
        /// Validation details
        /// </summary>
        public string Details
        {
            get;
            set;
        }
    }
}

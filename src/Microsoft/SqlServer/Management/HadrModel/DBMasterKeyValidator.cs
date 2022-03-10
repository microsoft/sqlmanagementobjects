// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.HadrData;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Class for validating the password of database master key
    /// </summary>
    public class DatabaseMasterKeyValidator : Validator
    {
        private bool validateExistingDbMode = false;
        public readonly AvailabilityGroupData availabilityGroupData;

        public DatabaseMasterKeyValidator(AvailabilityGroupData availabilityGroupData)
            : base(Resource.ValidatingDatabaseMasterKey)
        {
            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// The availability group data
        /// </summary>
        public AvailabilityGroupData AvailabilityGroupData
        {
            get
            {
                return this.availabilityGroupData;
            }
        }

        /// <summary>
        /// The validate mode with default value "false"
        /// If true, validate the existing database in AG instead of the new added database
        /// </summary>
        public bool ValidateExistingDbMode
        {
            get 
            { 
                return this.validateExistingDbMode; 
            }
            set 
            { 
                this.validateExistingDbMode = value; 
            }
        }

        /// <summary>
        /// The validate method
        /// </summary>
        protected override void Validate(IExecutionPolicy policy)
        {
            List<PrimaryDatabaseData> checkDatabases = this.validateExistingDbMode ? availabilityGroupData.ExistingAvailabilityDatabases : availabilityGroupData.NewAvailabilityDatabases;
            foreach (PrimaryDatabaseData database in checkDatabases)
            {
                if (availabilityGroupData.PrimaryServer.Databases[database.Name].MasterKey == null)
                    continue;

                if (!FailoverUtilities.TryDecrypt(availabilityGroupData.PrimaryServer.Databases[database.Name], database.DBMKPassword))
                {
                    throw new InvalidOperationException(Resource.DBMKPasswordIncorrect);
                }
            }
        }
    }
}

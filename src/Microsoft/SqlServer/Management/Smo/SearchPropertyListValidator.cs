// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal class SearchPropertyListValidator
    {
        private SearchPropertyListExtender searchPropertyListExtender;
        private SearchPropertyCollection existingSearchProperties = null;
        private SmoSet<string> deletedSearchPropertyNames = new SmoSet<string>();
        private SmoSet<int> updatedRows = new SmoSet<int>();
        private SmoSet<int> newRows = new SmoSet<int>();

        public SearchPropertyListValidator(SearchPropertyListExtender searchPropertyListExtender)
        {
            this.searchPropertyListExtender = searchPropertyListExtender;
        }

        public void Initialize(SearchPropertyCollection existingSearchProperties)
        {
            this.existingSearchProperties = existingSearchProperties;
            foreach (DataRow row in this.searchPropertyListExtender.SearchProperties.Rows)
            {
                string searchPropertyName = row["Name"] as string;                
                this.AddMatchingRowForName(searchPropertyName, (int)row["RowId"]);
                string guidIntId = row["PropertySetGuid"] as string + row["IntID"].ToString();
                this.AddMatchingRowForGuidIntId(guidIntId, (int)row["RowId"]);
            }
        }
        //Validation Errors for Each Row of the Search Properties Table
        private Dictionary<int, ValidationError> validationErrors = new Dictionary<int, ValidationError>();

        //mapping of Search Property Names to matching Rows
        private Dictionary<string, List<int>> matchingRowsForNames = new Dictionary<string, List<int>>();

        //mapping of Guid+IntId to matching Rows
        private Dictionary<string, List<int>> matchingRowsForGuidIntId = new Dictionary<string, List<int>>();

        /// <summary>
        ///  add the rowId to mapped rows of searchPropertyName 
        /// </summary>
        /// <param name="searchPropertyName"></param>
        /// <param name="rowId"></param>
        /// <returns></returns>
        private bool AddMatchingRowForName(string searchPropertyName, int rowId)
        {
            bool isUnique = true;
            if (!this.matchingRowsForNames.ContainsKey(searchPropertyName))
            {
                this.matchingRowsForNames.Add(searchPropertyName, new List<int>());
            }

            List<int> matchingRows = this.matchingRowsForNames[searchPropertyName];
            matchingRows.Add(rowId);
            if (matchingRows.Count > 1)
            {
                isUnique = false;
            }

            return isUnique;
        }

        /// <summary>
        ///  remove the rowId from mapped rows of searchPropertyName 
        /// </summary>
        /// <param name="searchPropertyName"></param>
        /// <param name="rowId"></param>
        private void RemoveMatchingRowForName(string searchPropertyName, int rowId)
        {
                List<int> matchingRows = this.matchingRowsForNames[searchPropertyName];
                matchingRows.Remove(rowId);

                if (matchingRows.Count == 1)
                {
                    int uniqueRowId = matchingRows[0];
                    //Remove the uniqueness error for this rowId with unique name
                    this.AddOrRemoveValidationError(uniqueRowId, ValidationError.NameError, true, ref nameErrors);
                    this.searchPropertyListExtender.GetRow(uniqueRowId).RowError = this.GetValidationErrorsMessage(uniqueRowId);
                }
                else if (matchingRows.Count == 0)
                {
                    this.matchingRowsForNames.Remove(searchPropertyName);
                    if (this.existingSearchProperties.Contains(searchPropertyName))
                    {
                        this.deletedSearchPropertyNames.Add(searchPropertyName);
                        this.updatedRows.Remove(rowId);
                    }
                    else
                    {
                        this.newRows.Remove(rowId);
                    }
                }
       }

        /// <summary>
        ///  add the rowId to mapped rows of Guid+IntId 
        /// </summary>
        /// <param name="searchPropertyName"></param>
        /// <param name="rowId"></param>
        /// <returns></returns>
        private bool AddMatchingRowForGuidIntId(string guidIntId, int rowId)
        {
            bool isUnique = true;
            if (!this.matchingRowsForGuidIntId.ContainsKey(guidIntId))
            {
                this.matchingRowsForGuidIntId.Add(guidIntId, new List<int>());
            }

            List<int> matchingRows = this.matchingRowsForGuidIntId[guidIntId];
            matchingRows.Add(rowId);
            if (matchingRows.Count > 1)
            {
                isUnique = false;
            }

            return isUnique;
        }

        /// <summary>
        ///  remove the rowId from mapped rows of Guid+IntId 
        /// </summary>
        /// <param name="searchPropertyName"></param>
        /// <param name="rowId"></param>
        private void RemoveMatchingRowForGuidIntId(string guidIntId, int rowId)
        {
            List<int> matchingRows = this.matchingRowsForGuidIntId[guidIntId];
            matchingRows.Remove(rowId);

            if (matchingRows.Count == 1)
            {
                int uniqueRowId = matchingRows[0];
                //Remove the uniqueness error for this rowId with unique guid+intid
                this.AddOrRemoveValidationError(uniqueRowId, ValidationError.GuidIntIdError, true, ref guidIntIdErrors);
                this.searchPropertyListExtender.GetRow(uniqueRowId).RowError = this.GetValidationErrorsMessage(uniqueRowId);
            }
            else if (matchingRows.Count == 0)
            {
                this.matchingRowsForNames.Remove(guidIntId);
                if (this.existingSearchProperties.Contains(guidIntId))
                {
                    this.deletedSearchPropertyNames.Add(guidIntId);
                    this.updatedRows.Remove(rowId);
                }
                else
                {
                    this.newRows.Remove(rowId);
                }
            }
        }

        /// <summary>
        ///   Enumeration of Errors for each of the fields of Search Properties Table
        /// </summary>
        [Flags]
        private enum ValidationError
        {
            // Default value
            None = 0,
            // Name Error
            NameError = 1,
            // Guid Error
            GuidError = 2,
            //Int ID Error
            IntIdError = 4,
            //Description Error
            DescriptionError = 8,
            //Guid+IntId Not Unique Error
            GuidIntIdError = 16
        } 

        private int nameErrors = 0;
        private int guidErrors = 0;
        private int intIdErrors = 0;
        private int descriptionErrors = 0;
        private int guidIntIdErrors = 0;


        public string GetValidationErrorsMessage(int rowId)
        {
            StringBuilder validationErrorsMessage = new StringBuilder();

            if (this.validationErrors.ContainsKey(rowId))
            {
                ValidationError error = this.validationErrors[rowId];

                if ((this.validationErrors[rowId] & ValidationError.NameError) == ValidationError.NameError)
                {
                    validationErrorsMessage.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyNameNotValid(SearchPropertyListConstants.MaxSearchPropertyNameLength));
                    validationErrorsMessage.AppendLine();
                }

                if ((this.validationErrors[rowId] & ValidationError.GuidError) == ValidationError.GuidError)
                {
                    validationErrorsMessage.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertySetGuidNotValid);
                    validationErrorsMessage.AppendLine();
                }

                if ((this.validationErrors[rowId] & ValidationError.IntIdError) == ValidationError.IntIdError)
                {
                    validationErrorsMessage.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyIntIDNotValid);
                    validationErrorsMessage.AppendLine();
                }

                if ((this.validationErrors[rowId] & ValidationError.DescriptionError) == ValidationError.DescriptionError)
                {
                    validationErrorsMessage.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyDescriptionNotValid(SearchPropertyListConstants.MaxSearchPropertyDescriptionLength));
                    validationErrorsMessage.AppendLine();
                }

                if ((this.validationErrors[rowId] & ValidationError.GuidIntIdError) == ValidationError.GuidIntIdError)
                {
                    validationErrorsMessage.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyGuidIntIdNotValid);
                    validationErrorsMessage.AppendLine();
                }
            }

            return validationErrorsMessage.ToString().Trim();
        }


        public void SearchPropertiesColumnChangingValidationHandler(object sender, DataColumnChangeEventArgs e)
        {
            DataRow row = e.Row;
            this.InitializeRow(row);

            int rowId = (int)row["RowId"];
            string columnName = e.Column.ColumnName;
            string proposedValue = String.Empty;
            if (e.ProposedValue != null)
            {
                proposedValue = e.ProposedValue.ToString();
            }

            if ((this.IsEquals(columnName, "PropertySetGuid") || this.IsEquals(columnName, "IntID")) && this.IsValidGuidAndIntIds(rowId))
            {
                string guidIntId = row["PropertySetGuid"] as string + this.GetNormalizedIntId(row["IntID"].ToString());
                this.RemoveMatchingRowForGuidIntId(guidIntId, rowId);
            }

            switch (columnName)
            {
                case "Name":
                    string searchPropertyName = row["Name"] as string;
                    if (this.IsValidNameLength(searchPropertyName))
                    {
                        //remove the row from mapped rows of current search property name 
                        this.RemoveMatchingRowForName(searchPropertyName, rowId);
                    }
                    this.ValidateName(rowId, proposedValue);
                    break;
                case "PropertySetGuid":
                    this.ValidateGuid(rowId, proposedValue);
                    proposedValue += this.GetNormalizedIntId(row["IntID"].ToString());
                    this.ValidateGuidIntId(rowId, proposedValue);
                    break;

                case "IntID":
                    this.ValidateIntID(rowId, proposedValue);
                    proposedValue = row["PropertySetGuid"] as string + this.GetNormalizedIntId(proposedValue);
                    this.ValidateGuidIntId(rowId, proposedValue);
                    break;
                case "Description":
                    this.ValidateDescription(rowId, proposedValue);
                    break;
            }



        }

 
        public void SearchPropertiesColumnChangedValidationHandler(object sender, DataColumnChangeEventArgs e)
        {
            DataRow row = e.Row;
            int rowId = (int)row["RowId"];

            string validationErrors = this.GetValidationErrorsMessage(rowId);

            if (string.IsNullOrEmpty(validationErrors))
            {
                row.ClearErrors();
                string searchPropertyName = row["Name"] as string;

                if (this.existingSearchProperties.Contains(searchPropertyName))
                {
                    if (this.deletedSearchPropertyNames.Contains(searchPropertyName))
                    {
                        this.deletedSearchPropertyNames.Remove(searchPropertyName);
                    }

                    SearchProperty existingSearchProperty = this.existingSearchProperties[searchPropertyName];
                    //If Dirty
                    if (!this.IsEquals(row["PropertySetGuid"] as string, existingSearchProperty.PropertySetGuid.ToString())
                         || !this.IsEquals(row["IntID"].ToString(), existingSearchProperty.IntID.ToString(SmoApplication.DefaultCulture))
                         || !this.IsEquals(row["Description"] as string,  existingSearchProperty.Description))
                    {
                        this.updatedRows.Add(rowId);
                    }
                    else
                    {
                        this.updatedRows.Remove(rowId);
                    }
                }
                else
                {
                    this.newRows.Add(rowId);
                }
            }
            else
            {
                row.RowError = validationErrors;
            }

            SetGridValidationState();
        }

        private int changingRowId = -1;
        private void InitializeRow(DataRow row)
        {
            int rowId = (int)row["RowId"];
            if (rowId != this.changingRowId && row.RowState == DataRowState.Detached)
            {
                this.changingRowId = rowId;

                if (string.IsNullOrEmpty(row["Name"] as string))
                {
                    this.AddOrRemoveValidationError(rowId, ValidationError.NameError, false, ref nameErrors); 
                }

                if (string.IsNullOrEmpty(row["PropertySetGuid"] as string))
                {
                    this.AddOrRemoveValidationError(rowId, ValidationError.GuidError, false, ref guidErrors);
                    this.AddOrRemoveValidationError(rowId, ValidationError.GuidIntIdError, false, ref guidIntIdErrors);
                }

                if (string.IsNullOrEmpty(row["IntID"].ToString()))
                {
                    this.AddOrRemoveValidationError(rowId, ValidationError.IntIdError, false, ref intIdErrors);
                    this.AddOrRemoveValidationError(rowId, ValidationError.GuidIntIdError, false, ref guidIntIdErrors);
                }
            }
        }

        public void SearchPropertiesRowDeleteValidationHandler(object sender,
DataRowChangeEventArgs e)
        {            
            DataRow row = e.Row;
            int rowId = (int) row["RowId"];
            string searchPropertyName = row["Name"] as string;
            if (this.IsValidNameLength(searchPropertyName))
            {
                //remove the row from mapped rows of current search property name 
                this.RemoveMatchingRowForName(searchPropertyName, rowId);
            }

            if (this.IsValidGuidAndIntIds(rowId))
            {
                string guidIntId = row["PropertySetGuid"] as string + this.GetNormalizedIntId(row["IntID"].ToString());
                //remove the row from mapped rows of current guidIntId
                this.RemoveMatchingRowForGuidIntId(guidIntId, rowId);
            }

            this.ResetValiationErrors(rowId);
            SetGridValidationState();
        }



        /// <summary>
        ///   for now, setting to only one of the errors (because of a New Dialog Framework Issue)
        /// </summary>
        private void SetGridValidationState()
        {
            StringBuilder validationErrors = new StringBuilder();

            if (nameErrors != 0)
            {
                validationErrors.AppendFormat(SmoApplication.DefaultCulture,ExceptionTemplates.SearchPropertyNameNotValid(SearchPropertyListConstants.MaxSearchPropertyNameLength) );
            }

            else if (guidErrors != 0)
            {
                validationErrors.AppendFormat(SmoApplication.DefaultCulture,ExceptionTemplates.SearchPropertySetGuidNotValid);
            }

            else if (intIdErrors != 0)
            {
                validationErrors.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyIntIDNotValid);
            }

            else if (descriptionErrors != 0)
            {
                validationErrors.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyDescriptionNotValid(SearchPropertyListConstants.MaxSearchPropertyDescriptionLength));
            }

            else if (guidIntIdErrors != 0)
            {
                validationErrors.AppendFormat(SmoApplication.DefaultCulture, ExceptionTemplates.SearchPropertyGuidIntIdNotValid);
            }

            string validationErrorsMessage = validationErrors.ToString().Trim();
            if (string.IsNullOrEmpty(validationErrorsMessage))
            {
                this.searchPropertyListExtender.GridValidationState = new ValidationState();
            }
            else
            {
                this.searchPropertyListExtender.GridValidationState = new ValidationState(validationErrorsMessage, "SearchProperties");
            }
        }

        private bool AddValidationError(int rowID, ValidationError error)
        {
            bool isAdded = false;
            if (!this.validationErrors.ContainsKey(rowID))
            {
                this.validationErrors.Add(rowID, error);
                isAdded = true;
            }
            else
            {
                //if the error not already set
                if ((this.validationErrors[rowID] & error) != error)
                {
                    this.validationErrors[rowID] |= error;
                    isAdded = true;
                }
            }
            return isAdded;
        }

        private bool RemoveValidationError(int rowID, ValidationError error)
        {
            bool isRemoved = false;
            if (this.validationErrors.ContainsKey(rowID))
            {
                //if the error is set
                if ((this.validationErrors[rowID] & error) == error)
                {
                    this.validationErrors[rowID] &= ~error;
                    isRemoved = true;
                }
            }
            return isRemoved;
        }

        private void AddOrRemoveValidationError(int rowID, ValidationError error, bool isValid, ref int errorCount)
        {
            if (!isValid)
            {
                if (this.AddValidationError(rowID, error))
                {
                    ++errorCount;
                }
            }
            else
            {
                if (this.RemoveValidationError(rowID, error))
                {
                    --errorCount;
                }
            }
        }

        private void ValidateName(int rowId, string proposedValue)
        {
            bool isValid = true;
            //Validate NOT NULL & Max Supported Length
            if (!this.IsValidNameLength(proposedValue))
            {
                isValid = false;               
            }
            else
            {
                //validate uniqueness
                isValid = this.AddMatchingRowForName(proposedValue, rowId);
            }

            this.AddOrRemoveValidationError(rowId, ValidationError.NameError, isValid, ref nameErrors);           
        }

        //Normalize intId (remove prefixed 0's etc.)
        private string GetNormalizedIntId(string intId)
        {                        
            try
            {
                int t = Convert.ToInt32(intId, SmoApplication.DefaultCulture);
                //assign the normalized value
                intId = t.ToString(SmoApplication.DefaultCulture);
            }
            catch (FormatException)
            {
                //Ignore
            }
            catch (OverflowException)
            {
                //Ignore
            }

            return intId;
        }

        private void ValidateGuidIntId(int rowId, string proposedValue)
        {
            bool isValid = true;
            
            //Validate for valid individual guid, intID columns
            if (!this.IsValidGuidAndIntIds(rowId))
            {
                isValid = false;              
            }
            else
            {
                //validate uniqueness
                isValid = this.AddMatchingRowForGuidIntId(proposedValue, rowId);
            }

            this.AddOrRemoveValidationError(rowId, ValidationError.GuidIntIdError, isValid, ref guidIntIdErrors);
        }

        private void ValidateGuid(int rowId, string proposedValue)
        {
            bool isValid = true;

            //Validate NOT NULL
            if (string.IsNullOrEmpty(proposedValue))
            {
                isValid = false;
            }
            else
            {
                //Validate Format
                try
                {
                    new Guid(proposedValue);
                }
                catch (FormatException)
                {
                    isValid = false;
                }
            }

            this.AddOrRemoveValidationError(rowId, ValidationError.GuidError, isValid, ref guidErrors);
        }

        private void ValidateIntID(int rowId, string proposedValue)
        {
            bool isValid = true;
            int intID = 0;
            //Validate NOT NULL
            if (string.IsNullOrEmpty(proposedValue))
            {
                isValid = false;
            }
            else
            {
                //Validate Format
                try
                {
                    intID = Convert.ToInt32(proposedValue, SmoApplication.DefaultCulture);
                }
                catch (FormatException)
                {
                    isValid = false;
                }
                catch (OverflowException)
                {
                    isValid = false;
                }
            }

            //Validate Range
            if (intID < 0)
            {
                isValid = false;
            }

            this.AddOrRemoveValidationError(rowId, ValidationError.IntIdError, isValid, ref intIdErrors);
        }

        private void ValidateDescription(int rowId, string proposedValue)
        {
            bool isValid = true;
            //validate length
            if (!string.IsNullOrEmpty(proposedValue) && proposedValue.Length > SearchPropertyListConstants.MaxSearchPropertyDescriptionLength)
            {
                isValid = false;
            }

            this.AddOrRemoveValidationError(rowId, ValidationError.DescriptionError, isValid, ref descriptionErrors);
        }

        private bool IsValidGuidAndIntIds(int rowId)
        {
            bool isValidGuid = true;
            bool isValidIntId = true;
            if (this.validationErrors.ContainsKey(rowId))
            {
                isValidGuid = (this.validationErrors[rowId] & ValidationError.GuidError) != ValidationError.GuidError;
                isValidIntId = (this.validationErrors[rowId] & ValidationError.IntIdError) != ValidationError.IntIdError;
            }
            return isValidGuid & isValidIntId;
        }

        //Validate NOT NULL & Max Supported Length
        private bool IsValidNameLength(string searchPropertyName)
        {
            return !string.IsNullOrEmpty(searchPropertyName) && searchPropertyName.Length <= SearchPropertyListConstants.MaxSearchPropertyNameLength;
        }

        private void ResetValiationErrors(int rowId)
        {
            this.AddOrRemoveValidationError(rowId, ValidationError.NameError, true, ref nameErrors);
            this.AddOrRemoveValidationError(rowId, ValidationError.GuidError, true, ref guidErrors);
            this.AddOrRemoveValidationError(rowId, ValidationError.IntIdError, true, ref intIdErrors);
            this.AddOrRemoveValidationError(rowId, ValidationError.DescriptionError, true, ref descriptionErrors);
            this.AddOrRemoveValidationError(rowId, ValidationError.GuidIntIdError, true, ref guidIntIdErrors);
        }

        private bool IsEquals(string s1, string s2)
        {
            return NetCoreHelpers.StringCompare(s1, s2, false, SmoApplication.DefaultCulture) == 0;
        }

        public SmoSet<string> DeletedSearchPropertyNames
        {
            get
            {
                return this.deletedSearchPropertyNames;
            }
        }

        public SmoSet<int> UpdatedRows
        {
            get
            {
                return this.updatedRows;
            }
        }

        public SmoSet<int> NewRows
        {
            get
            {
                return this.newRows;
            }
        }
    }
}

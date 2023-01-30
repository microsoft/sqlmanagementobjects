// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Provides detailed validation information.
    /// </summary>
    public class ValidationResult
    {
        #region Private Data Members
        private string text;
        private string bindingKey;
        private Exception errorDetails;
        private bool isWarning;
        #endregion
        #region Internal Constructor(s)
        internal ValidationResult(string text, string bindingKey,
            Exception errorDetails, bool isWarning)
        {
            this.isWarning = isWarning;
            this.text = text;
            this.bindingKey = bindingKey;
            this.errorDetails = errorDetails;
        }
        #endregion
        #region Public Properties
        /// <summary>
        /// Gets the text result of the validation result item
        /// </summary>
        public string Text
        {
            get
            {
                return text;
            }
        }

        /// <summary>
        /// The property name which caused the error.
        /// </summary>
        public string BindingKey
        {
            get
            {
                return bindingKey;
            }
        }

        /// <summary>
        /// The detailed exception for the error
        /// </summary>
        public Exception ErrorDetails
        {
            get
            {
                return errorDetails;
            }
        }

        /// <summary>
        /// True if warning, otherwise false
        /// </summary>
        public bool IsWarning
        {
            get
            {
                return isWarning;
            }
        }
        #endregion

        /// <summary>
        /// Returns the Text property
        /// This is needed for SQL Server Setup, so that narrator can read the validation result.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
    }

    /// <summary>
    /// Encapsulates various common state operations
    /// </summary>
    public class ValidationMethod
    {
        #region Enum Members
        public static readonly string Create = "Create";
        public static readonly string Alter = "Alter";
        public static readonly string Rename = "Rename";
        #endregion
    }

    /// <summary>
    /// Provides overall state information.
    /// </summary>
    public class ValidationState
    {
        #region Private Data Members
        private List<ValidationResult> results = new List<ValidationResult>();
        #endregion
        #region Public Constructors
        /// <summary>
        /// Constructs an object of type ValidationState
        /// </summary>
        public ValidationState(){}

        /// <summary>
        /// Constructs an object of type ValidationState
        /// </summary>
        /// <param name="message">The error/warning message</param>
        /// <param name="bindingKey">The property name which caused the problem</param>
        /// <param name="isWarning">True if warning, otherwise false</param>
        public ValidationState(string message, string bindingKey, bool isWarning)
        {
            if (isWarning)
            {
                AddWarning(message, bindingKey);
            }
            else
            {
                AddError(message, bindingKey);
            }
        }

        /// <summary>
        /// Constructs an object of type ValidationState, the error message is set to the exception message
        /// </summary>
        /// <param name="error">The source exception for the error/warning</param>
        /// <param name="bindingKey">The property name which caused the problem</param>
        /// <param name="isWarning">True if warning, otherwise false</param>
        public ValidationState(Exception error, string bindingKey, bool isWarning)
        {
            if (isWarning)
            {
                AddWarning(error, bindingKey);
            }
            else
            {
                AddError(error, bindingKey);
            }
        }

        /// <summary>
        /// Constructs an object of type ValidationState
        /// </summary>
        /// <param name="message">The error/warning message</param>
        /// <param name="error">The source exception for the error/warning</param>
        /// <param name="bindingKey">The property name which caused the problem</param>
        /// <param name="isWarning">True if warning, otherwise false</param>
        public ValidationState(string message, Exception error, string bindingKey, bool isWarning)
        {
            if (isWarning)
            {
                AddWarning(message, error, bindingKey);
            }
            else
            {
                AddError(message, error, bindingKey);
            }
        }

        /// <summary>
        /// Constructs an object of type ValidationState, the fault type is set to error by default.
        /// </summary>
        /// <param name="message">The error/warning message</param>
        /// <param name="bindingKey">The property name which caused the problem</param>
        public ValidationState(string message, string bindingKey)
        {
            AddError(message, bindingKey);
        }

        /// <summary>
        /// Constructs an object of type ValidationState, the error message is set to the exception message, the fault type is set to error by default.
        /// </summary>
        /// <param name="error">The source exception for the error/warning</param>
        /// <param name="bindingKey">The property name which caused the problem</param>
        public ValidationState(Exception error, string bindingKey)
        {
            AddError(error, bindingKey);
        }

        /// <summary>
        /// Constructs an object of type ValidationState, the fault type is set to error by default.
        /// </summary>
        /// <param name="message">The error/warning message</param>
        /// <param name="error">The source exception for the error/warning</param>
        /// <param name="bindingKey">The property name which caused the problem</param>
        public ValidationState(string message, Exception error, string bindingKey)
        {
            AddError(message, error, bindingKey);
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Adds a new error to the errors list
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="bindingKey">The property name which caused the error</param>
        public void AddError(string message, string bindingKey)
        {
            AddError(message, null, bindingKey);
        }

        /// <summary>
        /// Adds a new error to the errors list, the error message is set to the exception message.
        /// </summary>
        /// <param name="error">The source exception for the error</param>
        /// <param name="bindingKey">The property name which caused the error</param>
        public void AddError(Exception error, string bindingKey)
        {
            AddError(null, error, bindingKey);
        }

        /// <summary>
        /// Adds a new error to the errors list
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="error">The source exception for the error</param>
        /// <param name="bindingKey">The property name which caused the error</param>
        public void AddError(string message, Exception error, string bindingKey)
        {
            string userMessage = message;
            //If the user didn't provide a customer error message, set the message to the exception message
            if (userMessage == null)
            {
                if (error != null)
                {
                    userMessage = error.Message;
                }
            }
            results.Add(new ValidationResult(userMessage,bindingKey, error, false));
        }


        /// <summary>
        /// Adds a new warning to the warnings list
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="bindingKey">The property name which caused the warning</param>
        public void AddWarning(string message, string bindingKey)
        {
            AddWarning(message, null, bindingKey);
        }

        /// <summary>
        /// Adds a new warning to the warnings list, the warning message is set to the exception message.
        /// </summary>
        /// <param name="error">The source exception for the warning</param>
        /// <param name="bindingKey">The property name which caused the warning</param>
        public void AddWarning(Exception error, string bindingKey)
        {
            AddWarning(null, error, bindingKey);
        }

        /// <summary>
        /// Adds a new warning to the warnings list
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="error">The source exception for the warning</param>
        /// <param name="bindingKey">The property name which caused the warning</param>
        public void AddWarning(string message, Exception error, string bindingKey)
        {
            string userMessage = message;
            //If the user didn't provide a customer warning message, set the message to the exception message
            if (userMessage == null)
            {
                if (error != null)
                {
                    userMessage = error.Message;
                }
            }
            results.Add(new ValidationResult(userMessage, bindingKey, error, true));
        }
        #endregion
        #region Public Properties
        /// <summary>
        /// Get the list of the results associated with this validation state
        /// </summary>
        public IList<ValidationResult> Results
        {
            get
            {
                return results;
            }
        }

        /// <summary>
        /// True if this validation state contains one error or more, otherwise false
        /// </summary>
        public bool HasErrors
        {
            get
            {
                foreach (ValidationResult result in results)
                {
                    if (!result.IsWarning)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// True if this validation state contains one warning or more, otherwise false
        /// </summary>
        public bool HasWarnings
        {
            get
            {
                foreach (ValidationResult result in results)
                {
                    if (result.IsWarning)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        #endregion
    }

    /// <summary>
    /// Interface that allows object state to be validated.
    /// </summary>
    public interface ISfcValidate
    {
        #region Public Interface Methods
        ValidationState Validate(string methodName, params object[] arguments);
        #endregion
    }
}
// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// An implementation of IScriptStringBuilderParameter to support parameters that are basic key-value pairs
    /// 
    /// e.g. The KEY_STORE_PROVIDER_NAME and KEY_PATH values in the below TSQL
    /// 
    /// CREATE COLUMN MASTER KEY key_name
    ///     WITH 
    ///     (
    ///         KEY_STORE_PROVIDER_NAME = 'key store provider name',  
    ///         KEY_PATH = 'key path
    ///         [, ENCLAVE_COMPUTATIONS (SIGNATURE = signature)]
    ///     )
    /// </summary>
    internal class ScriptStringBuilderParameter : IScriptStringBuilderParameter
    {
        /// <summary>
        /// The parameter key
        /// </summary>
        private readonly string key;

        /// <summary>
        /// The parameter value
        /// </summary>
        private readonly string value;

        /// <summary>
        /// Determines if the value is surrounded with quotes when this parameter is converted to script
        /// </summary>
        private readonly ParameterValueFormat format;

        public ScriptStringBuilderParameter(string key, string value, ParameterValueFormat format = ParameterValueFormat.CharString)
        {
            this.key = key;
            this.value = value;
            this.format = format;
        }

        public string GetKey()
        {
            return key;
        }

        public string ToScript()
        {
            string formattedValue = value;

            switch (format)
            {
                case ParameterValueFormat.CharString:
                    formattedValue = "'" + value + "'";
                    break;
                case ParameterValueFormat.NVarCharString:
                    formattedValue = "N'" + value + "'";
                    break;
                case ParameterValueFormat.NotString:
                default:
                    break;
            }

            return String.Format("{0} = {1}", key, formattedValue);
        }
    }
}
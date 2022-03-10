// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// An implementation of IScriptStringBuilderParameter to support parameters that are complex objects.
    /// 
    /// e.g. syntax similar to ENCLAVE_COMPUTATIONS in the below TSQL
    /// 
    /// CREATE COLUMN MASTER KEY key_name
    ///     WITH 
    ///     (
    ///         KEY_STORE_PROVIDER_NAME = 'key store provider name',  
    ///         KEY_PATH = 'key path
    ///         [, ENCLAVE_COMPUTATIONS (SIGNATURE = signature)]
    ///     )
    /// </summary>
    internal class ScriptStringBuilderObjectParameter : IScriptStringBuilderParameter
    {
        /// <summary>
        /// The parameter key 
        /// </summary>
        private string key { get; set; }

        /// <summary>
        /// This parameter's value as an object represented by a list of parameters
        /// </summary>
        private IList<IScriptStringBuilderParameter> parameters { get; set; }

        public ScriptStringBuilderObjectParameter(string key, IList<IScriptStringBuilderParameter> parameters)
        {
            this.key = key;
            this.parameters = parameters;
        }

        public string GetKey()
        {
            return key;
        }

        public string ToScript()
        {
            return string.Format("{0} {1}", this.key,
                this.parameters.Any() ? string.Format("({0})", string.Join(", ", parameters.Select(p => p.ToScript())))
                : string.Empty);
        }
    }
}

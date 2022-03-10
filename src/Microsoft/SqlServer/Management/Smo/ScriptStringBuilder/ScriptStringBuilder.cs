// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Simple class for constructing TSQL statements with variable numbers of arguments in the argument list
    /// We could override SetParameter with type-specific overrides for value if we want to provide
    /// formatting services as well
    /// </summary>
    internal class ScriptStringBuilder
    {
        private readonly string statement;
        private readonly IList<IScriptStringBuilderParameter> parameters = new List<IScriptStringBuilderParameter>();
        private readonly ScriptingPreferences scriptingPreferences;

        /// <summary>
        /// Constructs a new TSQL statement builder starting with the given statement
        /// </summary>
        /// <param name="baseStatement"></param>
        /// <param name="scriptingPreferences"></param>
        public ScriptStringBuilder(string baseStatement, ScriptingPreferences scriptingPreferences = null)
        {
            this.statement = baseStatement;
            this.scriptingPreferences = scriptingPreferences;
        }

        /// <summary>
        /// Adds a new parameter to the statement or replace the existing value with a new one        
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public ScriptStringBuilder SetParameter(string name, string value, ParameterValueFormat format = ParameterValueFormat.CharString)
        {
            return SetParameter(new ScriptStringBuilderParameter(name, value, format));
        }

        /// <summary>
        /// Adds a new parameter object to the statement or replace the existing value with a new one
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="parameters">list of parameters of this object</param>
        /// <returns></returns>
        public ScriptStringBuilder SetParameter(string name, IList<IScriptStringBuilderParameter> parameters)
        {
            return SetParameter(new ScriptStringBuilderObjectParameter(name, parameters));
        }

        /// <summary>
        /// Adds a new parameter to the statement or replace the existing value with a new one  
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public ScriptStringBuilder SetParameter(IScriptStringBuilderParameter param)
        {
            var found = false;
            for (var i = 0; i < parameters.Count && !found; ++i)
            {
                if (parameters[i].GetKey().Equals(param.GetKey()))
                {
                    parameters[i] = param;
                    found = true;
                }
            }

            if (!found)
            {
                parameters.Add(param);
            }

            return this;
        }

        /// <summary>
        /// Default ToString() method, scripts the statement and appends ";" right after
        /// </summary>
        public override string ToString()
        {
            return this.ToString(scriptSemiColon: true);
        }

        /// <summary>
        /// ToString() method which scripts this object with optional appending of  ";"
        /// after the statement itself
        /// </summary>
        /// <param name="scriptSemiColon">whether the semicolon should be scripted after the statement body</param>
        /// <param name="pretty">whether to format the parameter list with tabs and new lines</param>
        public string ToString(bool scriptSemiColon, bool pretty = false)
        {
            string parametersString = String.Empty;

            string newLine, tab, delimiter;

            // if pretty-print formatting is supported and enabled: each parameter on its own line and indented
            if (pretty && scriptingPreferences != null)
            {
                newLine = scriptingPreferences.NewLine;
                tab = Globals.tab;
                delimiter = Globals.comma;
            }
            else // otherwise: all parameters on the same line
            {
                newLine = String.Empty;
                tab = String.Empty;
                delimiter = Globals.commaspace;
            }

            if (parameters.Any())
            {
                parametersString = "(" + newLine;
                parametersString += tab + String.Join(delimiter + newLine + tab, parameters.Select(p => p.ToScript()).ToArray());
                parametersString += newLine + ")";
            }

            return String.Format("{0}{1}{2}{3}", this.statement, pretty ? String.Empty : " ", parametersString, scriptSemiColon ? ";" : String.Empty);
        }
    }
}
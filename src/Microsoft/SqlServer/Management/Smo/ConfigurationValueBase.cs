// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Mail
{
    public partial class ConfigurationValue : NamedSmoObject, Cmn.IAlterable, IScriptable
    {
        internal ConfigurationValue(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ConfigurationValue";
            }
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            ScriptConfigurationValue(queries, sp);
        }

        /// <summary>
        /// Alter object properties
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptConfigurationValue(queries, sp);
        }

        private void ScriptConfigurationValue(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            Initialize(true);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sqlStmt.Append(ExceptionTemplates.IncludeHeader(UrnSuffix, SqlBraket(this.Name), DateTime.Now.ToString(GetDbCulture())));
                sqlStmt.Append(sp.NewLine);
            }

            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_configure_sp @parameter_name=N'{0}', @parameter_value=N'{1}', @description=N'{2}'",
                SqlString(this.Name), SqlString((string)Properties.Get("Value").Value), SqlString((string)Properties.Get("Description").Value));

            queries.Add(sqlStmt.ToString());
        }

        /// <summary>
        /// Generate object creation script using default scripting options
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }
    }
}




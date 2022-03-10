﻿// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Provider for Target.
    /// </summary>
    internal class ServerTargetProvider : ITargetProvider
    {
        private Target target = null;

        public ServerTargetProvider(Target parent)
        {
            this.target = parent;
        }

        /// <summary>
        /// Script Create for the Target.
        /// </summary>
        /// <returns>Target Create script.</returns>
        public string GetCreateScript()
        {
            StringBuilder sb = new StringBuilder(128);

            sb.Append(string.Format(CultureInfo.InvariantCulture, "ADD TARGET {0}", this.target.ScriptName));

            if (this.target.HasCustomizableField())
            {
                // add the customizabel columns.
                sb.Append("(");
                sb.Append("SET ");

                // only those non-null field counts.
                foreach (TargetField field in this.target.TargetFields)
                {
                    if (field.Value != null)
                    {
                        BaseXEStore store = this.target.Parent.Parent;
                        TargetColumnInfo columnInfo = store.ObjectInfoSet.Get<TargetInfo>(this.target.Name).TargetColumnInfoSet[field.Name];
                        sb.Append(
                            string.Format(
                            CultureInfo.InvariantCulture, 
                            "{0}={1},", 
                            field.Name,
                            store.FormatFieldValue(field.Value.ToString(), columnInfo.TypePackageID, columnInfo.TypeName)));
                    }
                }

                // remove the last comma for set statement
                sb.Remove(sb.Length - 1, 1);

                // close for the whole set and action statement.
                sb.Append(")");
            }

            return sb.ToString();          
        }

        /// <summary>
        /// Scripts Drop for this Target.
        /// </summary>
        /// <returns>Target Drop script.</returns>
        public string GetDropScript()
        {
            return string.Format(CultureInfo.InvariantCulture, "DROP TARGET {0}", this.target.ScriptName);          
        }

        /// <summary>
        /// Gets the target data.
        /// </summary>
        /// <returns>Target data xml string.</returns>
        public string GetTargetData()
        {
            Session session = this.target.Parent;
            if (session == null)
            {
                throw new XEventException(ExceptionTemplates.ParentNull);
            }

            string targetInfoName = this.target.Name.Substring(this.target.Name.LastIndexOf('.') + 1);
            string sql = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT target_data FROM sys.dm_xe_session_targets AS t JOIN sys.dm_xe_sessions AS s ON t.event_session_address=s.address WHERE s.name='{0}' AND t.target_name='{1}'",
                SfcTsqlProcFormatter.SqlString(session.Name),
                SfcTsqlProcFormatter.SqlString(targetInfoName));
            object data = this.ServerConnection.ExecuteScalar(sql);

            if (data == null)
            {
                // result set is empty
                throw new XEventException(ExceptionTemplates.CannotReadTargetData);
            }

            // as string will return null for DBNull                       
            return data as string; 
        }        

        /// <summary>
        /// Gets the underlying ServerConnection
        /// </summary>
        private ServerConnection ServerConnection
        {
            get
            {          
                BaseXEStore store = this.target.Parent.Parent;
                return (ServerConnection)store.SfcConnection;
            }
        }
    }
}
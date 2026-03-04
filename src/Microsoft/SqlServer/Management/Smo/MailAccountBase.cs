// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Mail
{
    public partial class MailAccount : ScriptNameObjectBase, Cmn.IAlterable, Cmn.ICreatable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {

        internal MailAccount(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "MailAccount";
            }
        }

        /// <summary>
        /// Create object
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }


        /// <summary>
        /// Used to check whether this account is currently being used for sending emails which are
        /// in the mail queue or not.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool IsBusyAccount
        {
            get
            {
                string sql = String.Format(CultureInfo.InvariantCulture,
                                 "USE msdb\r\nSELECT COUNT(*) FROM dbo.sysmail_unsentitems,sysmail_profileaccount,sysmail_account WHERE sysmail_unsentitems.profile_id = sysmail_profileaccount.profile_id AND sysmail_profileaccount.account_id = sysmail_account.account_id AND sysmail_account.account_id = {0}",
                                 ID);
                return ((int)this.ExecutionManager.ExecuteScalar(sql)) != 0;
            }
        }

        /// <summary>
        /// Used to check whether this account is currently being used by any profiles or not, and
        /// in case of being used, it returns a list of profiles which are using it
        /// Never return NULL. Return empty (zero element) array if no profiles exist
        /// </summary>
        public string[] GetAccountProfileNames()
        {
            string sql = String.Format(CultureInfo.InvariantCulture,
                @"SELECT p.[name]
                 FROM [msdb].[dbo].[sysmail_profile] as p
                 JOIN	[msdb].[dbo].[sysmail_profileaccount] as pa
                 ON p.profile_id = pa.profile_id
                 WHERE pa.account_id = {0}",
                ID);
            DataSet s = this.ExecutionManager.ExecuteWithResults(sql);
            string[] results = new string[s.Tables[0].Rows.Count];
            for (int i = 0; i < s.Tables[0].Rows.Count; i++)
            {
                results[i] = s.Tables[0].Rows[i][0] as string;
            }

            return results;
        }


        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion);

            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sqlStmt.Append(ExceptionTemplates.IncludeHeader(UrnSuffix, SqlBraket(this.Name),
                    DateTime.Now.ToString(GetDbCulture())));
                sqlStmt.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sqlStmt.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_MAIL_ACCOUNT,
                    "NOT",
                    FormatFullNameForScripting(sp, false));
                sqlStmt.Append(sp.NewLine);
            }

            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_add_account_sp @account_name=N'{0}'", SqlString(GetName(sp)));

            int count = 1; // append comma
            GetStringParameter(sqlStmt, sp, "EmailAddress", "@email_address=N'{0}'", ref count, true);
            GetStringParameter(sqlStmt, sp, "DisplayName", "@display_name=N'{0}'", ref count);
            GetStringParameter(sqlStmt, sp, "ReplyToAddress", "@replyto_address=N'{0}'", ref count);
            GetStringParameter(sqlStmt, sp, "Description", "@description=N'{0}'", ref count);
            queries.Add(sqlStmt.ToString());
        }

        /// <summary>
        /// Alter object
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            // all properties of the mail server are currently required in order to script
            // the account so defer implementation to a child object
            MailServer mailServer = this.MailServers[0];
            mailServer.ScriptMailServer(queries, sp);
        }

        /// <summary>
        /// Drop object
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersion);

            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sqlStmt.Append(ExceptionTemplates.IncludeHeader(UrnSuffix, SqlBraket(this.Name),
                    DateTime.Now.ToString(GetDbCulture())));
                sqlStmt.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sqlStmt.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_MAIL_ACCOUNT,
                    "",
                    FormatFullNameForScripting(sp, false));
                sqlStmt.Append(sp.NewLine);
            }

            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_delete_account_sp @account_name=N'{0}'", SqlString(GetName(sp)));
            queries.Add(sqlStmt.ToString());
        }

        /// <summary>
        /// Rename object
        /// </summary>
        /// <param name="newName"></param>
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            // all properties of the mail server are currently required in order to script
            // the account so defer implementation to a child object
            MailServer mailServer = this.MailServers[0];

            mailServer.ScriptMailServer(queries, new ScriptingPreferences(), newName, null);
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

        /// <summary>
        /// Collection of mail server objects
        /// </summary>
        MailServerCollection mailServers;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(MailServer))]
        public MailServerCollection MailServers
        {
            get
            {
                if (mailServers == null)
                {
                    mailServers = new MailServerCollection(this);
                }
                return mailServers;
            }
        }
    }
}



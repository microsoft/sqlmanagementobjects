// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo.Mail
{
    public partial class MailServer : ScriptNameObjectBase, Cmn.IRenamable, Cmn.IAlterable, IScriptable
    {

        internal MailServer(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "MailServer";
            }
        }

        private SqlSecureString password = String.Empty;
        internal SqlSecureString Password
        {
            get { return password;}
        }

        /// <summary>
        /// The value of "noCredentialChange" is passed to the parameter @no_credential_change
        /// of the sp "dbo.sysmail_update_account_sp"
        /// 
        ///   noCredentialChange=false vs @no_credential_change=0
        ///   noCredentialChange=true  vs @no_credential_change=1
        /// 
        /// </summary>
        private bool noCredentialChange = false;
        public bool NoCredentialChange
        {
            get { return noCredentialChange; }
            set { noCredentialChange = value; }
        }

        /// <summary>
        /// Sets server authentication information
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public void SetAccount(string userName, string password)
        {
            SetAccount(userName, password != null ? new SqlSecureString(password) : null);
        }

        /// <summary>
        /// Sets server authentication information
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public void SetAccount(string userName, System.Security.SecureString password)
        {
            if (userName == null)
            {
                throw new FailedOperationException(ExceptionTemplates.SetAccount,
                                                   this,
                                                   new ArgumentNullException("userName"));
            }

            if (userName.Length == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.SetAccount,
                                                   this,
                                                   new ArgumentException(ExceptionTemplates.EmptyInputParam("userName", "string")));
            }

            Properties.Get("UserName").Value = userName;
            this.password = password;
            SetAccountPasswordInternal(true);
        }

        /// <summary>
        /// Sets server authentication password
        /// </summary>
        /// <param name="password"></param>
        public void SetPassword(string password)
        {
            SetPassword(password != null ? new SqlSecureString(password) : null);
        }

        /// <summary>
        /// Sets server authentication password
        /// </summary>
        /// <param name="password"></param>
        public void SetPassword(System.Security.SecureString password)
        {
            this.password = password;
            SetAccountPasswordInternal(false);
        }

        private void SetAccountPasswordInternal(bool setAccount)
        {
            try
            {
                StringCollection sc = new StringCollection();
                ScriptMailServer(sc, new ScriptingPreferences());
                this.ExecutionManager.ExecuteNonQuery(sc);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(setAccount ? ExceptionTemplates.SetMailServerAccount : ExceptionTemplates.SetMailServerPassword, this, e);
            }
        }

        internal void ScriptMailServer(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptMailServer(queries, sp, null, null);
        }

        internal void ScriptMailServer(StringCollection queries, ScriptingPreferences sp, string newAccountName, string newServerName)
        {
            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            Initialize(true);

            MailAccount parent = ParentColl.ParentInstance as MailAccount;
            parent.Initialize(true);

            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, 
                                 "EXEC msdb.dbo.sysmail_update_account_sp @account_name=N'{0}', @description=N'{1}', @email_address=N'{2}', @display_name=N'{3}', @replyto_address=N'{4}', @mailserver_name=N'{5}', @mailserver_type=N'{6}', @port={7}, @username=N'{8}', @password=N'{9}', @use_default_credentials={10}, @enable_ssl={11}", 
                                 (null == newAccountName) ? SqlString(parent.GetName(sp)) : SqlString(newAccountName), 
                                 SqlString(parent.Description), 
                                 SqlString(parent.EmailAddress), 
                                 SqlString(parent.DisplayName), 
                                 SqlString(parent.ReplyToAddress), 
                                 (null == newServerName) ? SqlString(GetName(sp)) : SqlString(newServerName), 
                                 SqlString((string)Properties.Get("ServerType").Value), 
                                 (int)Properties.Get("Port").Value, 
                                 SqlString((string)Properties.Get("UserName").Value), 
                                 SqlString((string)this.Password),
								 (bool)Properties.Get("UseDefaultCredentials").Value? "1":"0",
								 (bool)Properties.Get("EnableSsl").Value? "1":"0");

            // The sysmail_update_account_sp procedure was updated in various hotfixes to
            // include a new parameter, @no_credential_change. Depending on the version of SQL
            // Server that we are connected to, we conditionally include this parameter in
            // this script. The common case for this class is that it is called from the
            // DatabaseMailWizard program which is going to turn around and execute this
            // script directly on this same sql server, so we only want to include this
            // parameter if the sql server supports it.
            //
            // The less common case is that this script is being generated for offline use, which
            // can happen because this is just a SMO object and anybody could be trying to script
            // it. In this scenario we can't know what version of SQL Server the script is going
            // to be run on, so we simply generate the correct script based on the version of the
            // connected sql server.
            //
            // The sysmail_update_account_sp procedure defaults this value to false, so we only
            // bother including if we're specifying true. This allows for trivial back
            // compatibility with older sql servers that don't have this parameter.
            if (this.NoCredentialChange)
            {
                bool bIncludeCredentialChangeFlag = true;

                // This is the build of the first SQL 2005 hotfix that contains the fix.
                Version sql2005Sp3Cu5 = new Version(9,0,4230);

                Version connectedServerVersion = (Version)this.ServerVersion;

                if ((this.ServerVersion.Major == 9) &&
                    (connectedServerVersion < sql2005Sp3Cu5))
                {
                    bIncludeCredentialChangeFlag = false;
                }
                else if (this.ServerVersion.Major == 10)
                {
                    // This is the last SQL 2008 RTM CU build before the fix in CU 7.
                    Version sql2008RtmCu6 = new Version(10,0,1815);

                    // This is the SQL 2008 SP1, which does not have the fix.
                    Version sql2008Sp1 = new Version(10,0,2531);

                    // This is the last SQL 2008 SP1 CU build before the fix in CU 4.
                    Version sql2008Sp1Cu3 = new Version(10,0,2732);

                    if (connectedServerVersion <= sql2008RtmCu6)
                    {
                        // This is a Katmai RTM build that is RTM CU6 or earlier, and it does not
                        // have the new parameter.
                        bIncludeCredentialChangeFlag = false;
                    }
                    else if (connectedServerVersion < sql2008Sp1)
                    {
                        // The server is between just after RTM CU 6 and SP1, so it has the
                        // fix. No change. DO NOT REMOVE THIS ELSE BLOCK because it accounts for
                        // the range of builds which have the fix in the Katmai RTM CU branch
                        // after the fix but before Katmai SP1.
                        bIncludeCredentialChangeFlag = true;
                    }
                    else if (connectedServerVersion <= sql2008Sp1Cu3)
                    {
                        // The server is between Katmai SP1 and Katmai SP1 CU3, and it does not
                        // have the new parameter.
                        bIncludeCredentialChangeFlag = false;
                    }
                }

                if (bIncludeCredentialChangeFlag)
                {
                    sqlStmt.Append(", @no_credential_change=1");
                }
            }
            
            if (null != newAccountName) // need to reference account by id
            {
                sqlStmt.AppendFormat(", @account_id={0}", parent.ID);
            }
            queries.Add(sqlStmt.ToString());
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
                queries.Add(sqlStmt.ToString());
            }
            ScriptMailServer(queries, sp);
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
            ScriptMailServer(queries, sp);
        }

        /// <summary>
        /// Rename the object
        /// </summary>
        /// <param name="newName"></param>
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        internal override void ScriptRename(StringCollection queries, ScriptingPreferences sp, string newName)
        {
            ScriptMailServer(queries, new ScriptingPreferences(), null, newName);
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



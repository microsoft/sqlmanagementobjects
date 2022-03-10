// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Mail
{
    public partial class MailProfile : ScriptNameObjectBase, Cmn.IAlterable, Cmn.ICreatable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable
    {
        private bool forceDeleteForActiveProfiles = true;

        internal MailProfile(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "MailProfile";
            }
        }

        /// <summary>
        /// Create object
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

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
                    Scripts.INCLUDE_EXISTS_MAIL_PROFILE,
                    "NOT",
                    FormatFullNameForScripting(sp, false));
                sqlStmt.Append(sp.NewLine);
                sqlStmt.Append("BEGIN");
                sqlStmt.Append(sp.NewLine);
            }

            string profileName = GetName(sp);
            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_add_profile_sp @profile_name=N'{0}'", SqlString(profileName));

            int count = 1; // append comma
            GetStringParameter(sqlStmt, sp, "Description", "@description=N'{0}'", ref count);
            queries.Add(sqlStmt.ToString());

            if (!sp.ScriptForCreateDrop && sp.Mail.Accounts)
            {
                DataTable dt = EnumAccounts();
                foreach (DataRow dr in dt.Rows)
                {
                    sqlStmt = ScriptAddAccount(
                        profileName,
                        Convert.ToString(dr["AccountName"], CultureInfo.InvariantCulture),
                        Convert.ToInt32(dr["SequenceNumber"], CultureInfo.InvariantCulture));
                    queries.Add(sqlStmt.ToString());
                }
            }

            if (!sp.ScriptForCreateDrop && sp.Mail.Principals)
            {
                DataTable dt = EnumPrincipals();
                foreach (DataRow dr in dt.Rows)
                {
                    sqlStmt = ScriptAddPrincipal(
                        profileName,
                        Convert.ToString(dr["PrincipalName"], CultureInfo.InvariantCulture),
                        Convert.ToBoolean(dr["IsDefault"], CultureInfo.InvariantCulture));
                    queries.Add(sqlStmt.ToString());
                }
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                queries.Add(sp.NewLine + "END" + sp.NewLine);
            }
        }


        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool ForceDeleteForActiveProfiles
        {
            set
            {
                forceDeleteForActiveProfiles = value;
            }

            get
            {
                return forceDeleteForActiveProfiles;
            }
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
            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_update_profile_sp @profile_name=N'{0}'", SqlString(this.Name));

            int count = 1; // append comma
            GetStringParameter(sqlStmt, sp, "Description", "@description=N'{0}'", ref count);
            if (1 < count) // description has been modified
            {
                queries.Add(sqlStmt.ToString());
            }
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

        /// <summary>
        /// Used to check whether this profile is currently being used for sending emails which are
        /// in the mail queue or not.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool IsBusyProfile
        {
            get
            {
                string sql = String.Format(CultureInfo.InvariantCulture,
                                 "use msdb\r\nSELECT COUNT(*) from sysmail_unsentitems WHERE sysmail_unsentitems.profile_id = {0}",
                                 ID);
                return ((int)this.ExecutionManager.ExecuteScalar(sql)) != 0;
            }
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sqlStmt.Append(ExceptionTemplates.IncludeHeader(UrnSuffix, SqlBraket(GetName(sp)),
                    DateTime.Now.ToString(GetDbCulture())));
                sqlStmt.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sqlStmt.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_MAIL_PROFILE,
                    "",
                    FormatFullNameForScripting(sp, false));
                sqlStmt.Append(sp.NewLine);
            }

            sqlStmt.AppendFormat(SmoApplication.DefaultCulture,
                "EXEC msdb.dbo.sysmail_delete_profile_sp @profile_name=N'{0}', @force_delete={1}",
                SqlString(this.Name), forceDeleteForActiveProfiles);
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
            StringBuilder sqlStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sqlStmt.AppendFormat(SmoApplication.DefaultCulture,
                                    "EXEC msdb.dbo.sysmail_update_profile_sp @profile_id={0}, @profile_name=N'{1}'",
                                    this.ProfileIDInternal,
                                    SqlString(newName));
            queries.Add(sqlStmt.ToString());
        }

        internal int ProfileIDInternal
        {
            get { return (int)Properties["ID"].Value; }
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
        /// Adds profile-account association
        /// </summary>
        public void AddAccount(string accountName, int sequenceNumber)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringBuilder sqlStmt = ScriptAddAccount(this.Name, accountName, sequenceNumber);
                StringCollection query = new StringCollection();
                query.Add(sqlStmt.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.AddMailAccountToProfile, this, e);
            }
        }

        private StringBuilder ScriptAddAccount(string profileName, string accountName, int sequenceNumber)
        {
            StringBuilder sqlStmt = new StringBuilder();
            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_add_profileaccount_sp @profile_name=N'{0}', @account_name=N'{1}', @sequence_number={2}", SqlString(profileName), SqlString(accountName), sequenceNumber);
            return sqlStmt;
        }

        /// <summary>
        /// Removes profile-account association
        /// </summary>
        public void RemoveAccount(string accountName)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringBuilder sqlStmt = new StringBuilder();
                sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_delete_profileaccount_sp @profile_name=N'{0}', @account_name=N'{1}'",
                    SqlString(this.Name), SqlString(accountName));

                StringCollection query = new StringCollection();
                query.Add(sqlStmt.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.RemoveMailAccountFromProfile, this, e);
            }
        }

        /// <summary>
        /// Retrieves accounts associated with this profile
        /// </summary>
        public DataTable EnumAccounts()
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                Request req = new Request(this.Urn + "/MailProfileAccount");
                return this.ExecutionManager.GetEnumeratorData(req);

            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumMailAccountsForProfile, this, e);
            }
        }

        /// <summary>
        /// Adds profile-principal association, assuming this profile will be the default
        /// </summary>
        public void AddPrincipal(string principalName)
        {
            AddPrincipal(principalName, true);
        }

        /// <summary>
        /// Adds profile-principal association
        /// </summary>
        public void AddPrincipal(string principalName, bool isDefaultProfile)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringCollection query = new StringCollection();
                StringBuilder sqlStmt = ScriptAddPrincipal(this.Name, principalName, isDefaultProfile);
                query.Add(sqlStmt.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.AddPrincipalToMailProfile, this, e);
            }
        }

        private StringBuilder ScriptAddPrincipal(string profileName, string principalName, bool isDefaultProfile)
        {
            StringBuilder sqlStmt = new StringBuilder();
            sqlStmt.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_add_principalprofile_sp @principal_name=N'{0}', @profile_name=N'{1}', @is_default={2}", SqlString(principalName), SqlString(profileName), isDefaultProfile ? 1 : 0);
            return sqlStmt;
        }

        /// <summary>
        /// Removes profile-principal association
        /// </summary>
        public void RemovePrincipal(string principalName)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();

                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sysmail_delete_principalprofile_sp @principal_name=N'{0}', @profile_name=N'{1}'", SqlString(principalName), SqlString(this.Name));
                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.RemovePrincipalFromMailProfile, this, e);
            }

        }

        /// <summary>
        /// Retrieves principal associated with this profile
        /// </summary>
        public DataTable EnumPrincipals()
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/MailProfilePrincipal"));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumPrincipalsForMailProfile, this, e);
            }
        }
    }
}



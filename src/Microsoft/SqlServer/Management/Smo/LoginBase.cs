// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Specifies options that can be specified when creating a login
    /// </summary>
    [Flags]
    public enum LoginCreateOptions
    {
        /// <summary>No options.</summary>
        None = 0,
        /// <summary>Indicates if the password has already been hashed, which allows passwords to be reapplied to a login.</summary>
        IsHashed = 1,
        /// <summary>Indicates whether the user has to change the password on the next Login.</summary>
        MustChange = 2,
    }


    ///<summary>
    /// This class represents a SqlServer login
    ///</summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Login : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IScriptable, ILoginOptions
    {
        internal Login(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            InitVariables();
        }

        private SqlSecureString password;
        private bool passwordIsHashed;
        private bool mustChangePassword;
        private StringCollection credentialCollection = new StringCollection();
        private string oldCredential = string.Empty;

        // Service Principal Object ID obtained from Microsoft Entra ID
        private Guid objectId;

        void InitVariables()
        {
            this.password = null;
            this.passwordIsHashed = false;
            this.mustChangePassword = false;
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// Gets or sets the objectId of this login object. 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Guid ObjectId
        {
            get
            {
                return this.objectId;
            }
            set
            {
                if (this.State != SqlSmoState.Creating)
                {
                    throw new SmoException(ExceptionTemplates.ObjectIdCannotBeSet);
                }
                this.objectId = value;
            }
        }

        /// <summary>
        /// Gets the Collection of Credential Names mapped to the login.
        /// This method would always execute the select queries on the server even when ExecutionManager.ConnectionContext.SqlExecutionModes is set only to CaptureSql.
        /// </summary>
        public StringCollection EnumCredentials()
        {
            ThrowIfBelowVersion100();

            StringCollection credentialNames = new StringCollection();

            if (this.IsDesignMode)
            {
                foreach (string credential in credentialCollection)
                {
                    credentialNames.Add(credential);
                }
                return credentialNames;
            }

            string query = string.Format(SmoApplication.DefaultCulture, "select name from sys.server_principal_credentials as p join sys.credentials as c on c.credential_id = p.credential_id where p.principal_id = {0}", this.ID.ToString());

            // we don't want to capture the enum credentials query so we change the sqlExecution mode and make this execute on server.
            // Also, Enumerator cannot be used here because the "Login/Credential" URN Request joins table sys.credentials and sys.cryptographic_providers whereas
            // here its needed to join sys.server_principal_credentials and sys.credentials.
            Microsoft.SqlServer.Management.Common.SqlExecutionModes originalEvaluationMode = this.ExecutionManager.ConnectionContext.SqlExecutionModes;
            this.ExecutionManager.ConnectionContext.SqlExecutionModes = Microsoft.SqlServer.Management.Common.SqlExecutionModes.ExecuteSql;
            try
            {
                DataTable dt = this.ExecutionManager.ExecuteWithResults(query).Tables[0];
                foreach (DataRow dr in dt.Rows)
                {
                    credentialNames.Add((string)dr["name"]);
                }
            }
            finally
            {
                this.ExecutionManager.ConnectionContext.SqlExecutionModes = originalEvaluationMode;
            }
            return credentialNames;
        }


        /// <summary>
        /// Changes login password, effective immediate on the server.
        /// </summary>
        /// <param name="newPassword"></param>
        public void ChangePassword(string newPassword)
        {
            SqlSecureString secureNewPassword = null;
            if (null != newPassword)
            {
                secureNewPassword = new SqlSecureString(newPassword);
            }
            ChangePassword(secureNewPassword);
        }

        /// <summary>
        /// Changes login password using SecureString, effective immediate on the server.
        /// </summary>
        /// <param name="newPassword"></param>
        public void ChangePassword(System.Security.SecureString newPassword)
        {

            try
            {
                CheckObjectState();
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                ExecuteLoginPasswordOptions(newPassword,
                                            null,
                                            false,
                                            false);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        /// <summary>
        /// Changes login password, effective immediate on the server.
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        public void ChangePassword(string oldPassword, string newPassword)
        {
            SqlSecureString secureOldPassword = null;
            SqlSecureString secureNewPassword = null;
            if (null != oldPassword)
            {
                secureOldPassword = new SqlSecureString(oldPassword);
            }
            if (null != newPassword)
            {
                secureNewPassword = new SqlSecureString(newPassword);
            }

            ChangePassword(secureOldPassword, secureNewPassword);
        }


        /// <summary>
        /// Changes login password using SecureString, effective immediate on the server.
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        public void ChangePassword(System.Security.SecureString oldPassword, System.Security.SecureString newPassword)
        {
            try
            {
                CheckObjectState();
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                if (null == oldPassword)
                {
                    throw new ArgumentNullException("oldPassword");
                }

                ExecuteLoginPasswordOptions(newPassword,
                                            oldPassword,
                                            false,
                                            false);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }

        }

        ///<summary>
        ///Changes the password. The unlock parameter indictates if the account needs to be unlocked.
        ///The mustChange parameter indicates whether the user has to change the password on the next Login.</summary>
        /// <param name="newPassword"></param>
        /// <param name="unlock"></param>
        /// <param name="mustChange"></param>
        public void ChangePassword(string newPassword, bool unlock, bool mustChange)
        {
            SqlSecureString secureNewPassword = null;
            if (null != newPassword)
            {
                secureNewPassword = new SqlSecureString(newPassword);
            }

            ChangePassword(secureNewPassword, unlock, mustChange);
        }


        ///<summary>
        ///Changes the password using SecureString. The unlock parameter indictates if the account needs to be unlocked.
        ///The mustChange parameter indicates whether the user has to change the password on the next Login.</summary>
        /// <param name="newPassword"></param>
        /// <param name="unlock"></param>
        /// <param name="mustChange"></param>
        public void ChangePassword(System.Security.SecureString newPassword, bool unlock, bool mustChange)
        {
            try
            {
                ThrowIfBelowVersion90();
                CheckObjectState();
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                ExecuteLoginPasswordOptions(newPassword,
                                            null,
                                            unlock,
                                            mustChange);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        ///<summary>
        ///returns the name of the type in the urn expression</summary>
        public static string UrnSuffix
        {
            get
            {
                return "Login";
            }
        }

        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
            this.SetProperties();
        }

        private void SetProperties()
        {
            if (this.ExecutionManager.Recording || !this.IsDesignMode)
            {
                return;
            }

            this.SetWindowsLoginType();
            this.SetHasAccess();
            this.SetCredential();
        }

        private void SetHasAccess()
        {
            bool hasAccess = true;

            Property denyWindowsLogin = this.Properties.Get("DenyWindowsLogin");

            if (null != denyWindowsLogin.Value && (bool)denyWindowsLogin.Value)
            {
                hasAccess = false;
            }

            //lookup the property ordinal from name
            int hasAccessSet = this.Properties.LookupID("HasAccess", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(hasAccessSet, hasAccess);
            //mark the property as retrived
            this.Properties.SetRetrieved(hasAccessSet, true);
        }

        private void SetMustChangePassword()
        {
            if (this.ExecutionManager.Recording || !this.IsDesignMode || !this.IsVersion90AndAbove())
            {
                return;
            }

            //lookup the property ordinal from name
            int mustChangePasswordSet = this.Properties.LookupID("MustChangePassword", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(mustChangePasswordSet, this.mustChangePassword);
            //mark the property as retrived
            this.Properties.SetRetrieved(mustChangePasswordSet, true);
        }

        private void SetCredential()
        {
            //check greater than 9
            if (!this.IsVersion90AndAbove())
            {
                return;
            }

            string credentialName = this.Properties.Get("Credential").Value as string;

            if(string.IsNullOrEmpty(credentialName))
            {
                if(!string.IsNullOrEmpty(this.oldCredential) && this.credentialCollection.Contains(this.oldCredential))
                {
                    this.credentialCollection.Remove(this.oldCredential);
                }
            }
            else if (!this.credentialCollection.Contains(credentialName))
            {
                this.credentialCollection.Add(credentialName);
            }
            this.oldCredential = credentialName;
        }

        private void SetWindowsLoginType()
        {
            WindowsLoginAccessType windowsLoginType = WindowsLoginAccessType.Undefined;

            Property loginType = this.Properties.Get("LoginType");
            if (loginType.Value != null && ((LoginType)loginType.Value == LoginType.WindowsUser || (LoginType)loginType.Value == LoginType.WindowsGroup))
            {
                Property denyWindowsLogin = this.Properties.Get("DenyWindowsLogin");

                if (null != denyWindowsLogin.Value && (bool)denyWindowsLogin.Value)
                {
                    windowsLoginType = WindowsLoginAccessType.Deny;
                }
                else
                {
                    windowsLoginType = WindowsLoginAccessType.Grant;
                }
            }
            else
            {
                windowsLoginType = WindowsLoginAccessType.NonNTLogin;
            }

            //lookup the property ordinal from name
            int windowsLoginAccessTypeSet = this.Properties.LookupID("WindowsLoginAccessType", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(windowsLoginAccessTypeSet, windowsLoginType);
            //mark the property as retrived
            this.Properties.SetRetrieved(windowsLoginAccessTypeSet, true);
        }

        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        /// <param name="password">Password.</param>
        public void Create(string password)
        {
            if (password == null)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, new ArgumentNullException("password"));
            }

            Create(new SqlSecureString(password));
        }

        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        /// <param name="password">Password.</param>
        public void Create(System.Security.SecureString password)
        {
            if (password == null)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, new ArgumentNullException("password"));
            }

            this.password = password;
            Create();
        }

        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        /// <param name="password">Password.</param>
        /// <param name="options">Combination of additional options for password such as IsHashed or MushChange.</param>
        public void Create(string password, LoginCreateOptions options)
        {
            if (password == null)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, new ArgumentNullException("password"));
            }

            Create(new SqlSecureString(password), options);
        }

        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        /// <param name="password">Password.</param>
        /// <param name="options">Combination of additional options for password such as IsHashed or MushChange.</param>
        public void Create(System.Security.SecureString password, LoginCreateOptions options)
        {
            if (password == null)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, new ArgumentNullException("password"));
            }

            this.passwordIsHashed = (options & LoginCreateOptions.IsHashed) != 0;
            this.mustChangePassword = (options & LoginCreateOptions.MustChange) != 0;

            Create(password);
        }

        ///<summary>
        /// Generate the script to check the server to see if the given login can be created.
        /// This is intended to be executed in a transcation and rolld back, to check actual syntax as well as semantic context.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal void ScriptCreateCheck(StringCollection query, ScriptingPreferences sp)
        {
            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                // Skip the ALTER DISABLE generation part since it can cause its own throws sometimes
                ScriptLogin(query, sp, true, true);
            }
            else
            {
                // Use same script generation as normal CREATEs for Shiloh-
                ScriptCreateLess9(query, sp);
            }
        }

        ///<summary>
        ///generates the scripts for creating the login</summary>
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType)
            {
                ScriptCreateForCloud(query, sp);
                return;
            }

            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                ScriptCreateGreaterEqual9(query, sp);
            }
            else
            {
                ScriptCreateLess9(query, sp);
            }
        }

        void AddComma(StringBuilder sb, ref bool bStuffAdded)
        {
            if (bStuffAdded)
            {
                sb.Append(Globals.commaspace);
            }
            else
            {
                bStuffAdded = true;
            }
        }

        void AppendSid(Object oSid, StringBuilder sb)
        {
            sb.Append("0x");
            byte[] sid = (byte[])oSid;
            foreach (byte b in sid)
            {
                sb.Append(b.ToString("X2", SmoApplication.DefaultCulture));
            }
        }

        string BoolToOnOff(Object oBool)
        {
            return true == (bool)oBool ? "ON" : "OFF";
        }

        void ExecuteLoginPasswordOptions(SqlSecureString password,
                                         SqlSecureString oldPassword,
                                         bool bUnlock,
                                         bool bMustChange)
        {
            if (this.LoginType != LoginType.SqlLogin)
            {
                throw new SmoException(ExceptionTemplates.CannotChangePassword);
            }

            StringCollection sc = new StringCollection();

            if (this.ServerVersion.Major < 9)
            {
                // for standard logins, we can try to change their password
                sc.Add(string.Format(SmoApplication.DefaultCulture,
                                      "EXEC master.dbo.sp_password @old={0}, @new={1}, @loginame={2}",
                                      oldPassword == null ? "NULL" : MakeSqlString((string)oldPassword),
                                      MakeSqlString((string)password),
                                      FormatFullNameForScripting(new ScriptingPreferences())));
            }
            else
            {
                sc.Add("USE [master]");
                StringBuilder sb = new StringBuilder();
                sb.Append("ALTER LOGIN ");
                sb.Append(FormatFullNameForScripting(new ScriptingPreferences()));
                sb.Append(" WITH ");

                AddPasswordOptions(null, sb, password, oldPassword, false, bMustChange, bUnlock);
                sc.Add(sb.ToString());
            }

            this.ExecutionManager.ExecuteNonQuery(sc);
        }

        void AddPasswordOptions(ScriptingPreferences sp,
                                StringBuilder sb,
                                SqlSecureString password,
                                SqlSecureString oldPassword,
                                bool bIsHashed,
                                bool bMustChange,
                                bool bUnlock)
        {
            if (null != password)
            {
                sb.Append("PASSWORD=");

                if (bIsHashed)
                {
                    this.ValidatePasswordHash((string) password);
                    sb.Append((string)password);
                }
                else
                {
                    sb.Append(MakeSqlString((string)password));
                }

                if (null != oldPassword)
                {
                    sb.Append(" OLD_PASSWORD=");
                    sb.Append(MakeSqlString((string)oldPassword));
                }
                if (true == bUnlock)
                {
                    sb.Append(" UNLOCK");
                }
                if (true == bIsHashed)
                {
                    sb.Append(" HASHED");
                }
                if (true == bMustChange)
                {
                    sb.Append(" MUST_CHANGE");
                }
            }
            else if (null != sp && !sp.ScriptForCreateDrop && !sp.ScriptForAlter)
            {
                // we are in scripting mode at this point and we don't have
                // the password. We will generate a random password of reasonable
                // complexity, because we want the script to execute without
                // failure. It is considered to be the responsability of the
                // system administrator to further manage the login, ie change
                // its password to something else
                sb.Append("PASSWORD=");
                sb.Append(MakeSqlString(SecurityUtils.GenerateRandomPassword()));
            }
            else
            {
                throw new PropertyNotSetException("password");
            }
        }


        void ValidatePasswordHash(string passwordHash)
        {
            //verify that there are no spaces
            // to avoid sql injections
            foreach (char ch in passwordHash)
            {
                if (ch == ' ')
                {
                    throw new SmoException(ExceptionTemplates.InvalidPasswordHash);
                }
            }
        }

        bool HasMainDdlDirtyProps()
        {
            foreach (Property prop in this.Properties)
            {
                if (prop.Dirty && prop.Name != "DenyWindowsLogin")
                {
                    return true;
                }
            }

            return false;
        }

        //---------------------------------------------------------------------------
        // CREATE LOGIN login_name { WITH < option_list1 > | FROM < sources > }
        //
        // < sources >::=
        //    WINDOWS [ WITH windows_options [,...] ]
        //    | CERTIFICATE certname
        //    | ASYMMETRIC KEY asym_key_name
        //    | EXTERNAL PROVIDER [ WITH external_options [,...] ]
        //
        // < option_list1 >::=
        //    PASSWORD = ' password ' [ HASHED ] [ MUST_CHANGE ]
        //    [ , option_list2 [ ,... ] ]
        //
        // < option_list2 >::=
        //     SID = sid
        //    | DEFAULT_DATABASE = database
        //    | DEFAULT_LANGUAGE = language
        //    | CHECK_EXPIRATION = { ON | OFF}
        //    | CHECK_POLICY = { ON | OFF}
        //    [ CREDENTIAL = credential_name ]
        //
        // < windows_options >::=
        //    DEFAULT_DATABASE = database
        //    | DEFAULT_LANGUAGE = language
        //
        // < external_options >::=
        //     SID = sid, TYPE = type
        //    DEFAULT_DATABASE = database
        //    | DEFAULT_LANGUAGE = language
        //
        // ALTER LOGIN login_name
        //    {
        //    < status_option >
        //    | WITH set_option [ ,... ]
        //    }
        //
        // < status_option >::=
        //        ENABLE | DISABLE
        //
        // < set_option >::=
        //    PASSWORD = ' password '
        //    [
        //        OLD_PASSWORD = ' oldpassword '
        //        | secadmin_pwd_option [ secadmin_pwd_option ]
        //    ]
        //    | DEFAULT_DATABASE = database
        //    | DEFAULT_LANGUAGE = language
        //    | NAME = login_name
        //    | CHECK_POLICY = { ON | OFF }
        //    | CHECK_EXPIRATION = { ON | OFF }
        //    | CREDENTIAL = credential_name
        //    | NO CREDENTIAL
        //
        // < secadmin_pwd_opt >::=
        //        MUST_CHANGE | UNLOCK
        //
        //-----------------------------------------------------------------------------

        void ScriptLogin(StringCollection sc, ScriptingPreferences sp, bool bForCreate, bool bForServerCreateCheck)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringBuilder disableCmd = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringCollection rolesCmd = new StringCollection();

            // This function call must be placed out of if condition as it is the correct place to print
            // Also, this avoids the second call to script comment after the if condition
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (bForCreate || HasMainDdlDirtyProps())
            {
                if (bForCreate && sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                    Scripts.INCLUDE_EXISTS_LOGIN90,
                                    "NOT",
                                    FormatFullNameForScripting(sp, false));
                    sb.Append(sp.NewLine);
                }

                sb.Append(bForCreate ? "CREATE LOGIN " : "ALTER LOGIN ");
                string scriptName = FormatFullNameForScripting(sp);
                sb.Append(scriptName);

                StringBuilder sbOption = new StringBuilder();
                bool bStuffAdded = false;

                if (this.LoginType == LoginType.SqlLogin)
                {
                    // Don't script ALTER DISABLE if we are scripting for a transacted server-side check of the validity of the login creation.
                    if (bForCreate && !bForServerCreateCheck)
                    {
                        if (!sp.ScriptForCreateDrop)
                        {
                            sb.Insert(0, Globals.newline);
                            sb.Insert(0, "/* For security reasons the login is created disabled and with a random password. */");
                        }

                        AddPasswordOptions(sp, sbOption, this.password, null,
                                           this.passwordIsHashed, this.mustChangePassword, false);
                        if (sbOption.Length > 0)
                        {
                            bStuffAdded = true;
                        }

                        if (!sp.ScriptForCreateDrop)
                        {
                            //Here control will only reach if targetenginetype is not cloud.
                            if ((Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType) && (sp.IncludeScripts.Associations))
                            {
                                ScriptAssociations(rolesCmd,sp);
                            }

                            disableCmd.AppendFormat(SmoApplication.DefaultCulture,
                                                    "ALTER LOGIN {0} DISABLE", scriptName);
                        }
                    }

                    Object oSid = GetPropValueOptional("Sid");

                    if (null != oSid && (sp.ScriptForCreateDrop || sp.Security.Sid))
                    {
                        //Control will only reach here when targetengine type is not cloud.
                        if (Cmn.DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
                        {
                            throw new InvalidSmoOperationException(ExceptionTemplates.CloudSidNotApplicableOnStandalone);
                        }
                        AddComma(sbOption, ref bStuffAdded);
                        sbOption.Append("SID=");
                        AppendSid(oSid, sbOption);
                    }

                    String s = (string)GetPropValueOptional("DefaultDatabase");

                    if (null != s && s.Length > 0)
                    {
                        AddComma(sbOption, ref bStuffAdded);
                        sbOption.Append("DEFAULT_DATABASE=");
                        sbOption.Append(MakeSqlBraket(s));
                    }

                    s = (string)GetPropValueOptional("Language");
                    if (null != s && s.Length > 0)
                    {
                        AddComma(sbOption, ref bStuffAdded);
                        sbOption.Append("DEFAULT_LANGUAGE=");
                        sbOption.Append(MakeSqlBraket(s));
                    }

                    if (this.ServerVersion.Major >= 9)
                    {
                        Object o = GetPropValueOptional("PasswordExpirationEnabled");

                        if (null != o)
                        {
                            AddComma(sbOption, ref bStuffAdded);
                            sbOption.Append("CHECK_EXPIRATION=");
                            sbOption.Append(BoolToOnOff(o));
                        }

                        o = GetPropValueOptional("PasswordPolicyEnforced");
                        if (null != o)
                        {
                            AddComma(sbOption, ref bStuffAdded);
                            sbOption.Append("CHECK_POLICY=");
                            sbOption.Append(BoolToOnOff(o));
                        }

                        //Here control will only reach if targetenginetype is not cloud.
                        if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                        {
                            Property credential = this.Properties.Get("Credential");
                            if (null != credential.Value && (credential.Dirty || bForCreate))
                            {
                                string credentialStr = (string)credential.Value;

                                if (credentialStr.Length == 0)
                                {
                                    if (!bForCreate)
                                    {
                                        AddComma(sbOption, ref bStuffAdded);
                                        // if we are altering the login and the credential is
                                        // an empty string then we remove the mapping of the login to
                                        // the existing credential
                                        sbOption.Append("NO CREDENTIAL");
                                    }
                                }
                                else
                                {
                                    AddComma(sbOption, ref bStuffAdded);
                                    sbOption.Append("CREDENTIAL = ");
                                    sbOption.Append(MakeSqlBraket(credentialStr));
                                }
                            }
                        }
                    }
                } //end standard login
                else if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType) //Only SqlLogins can exist on Cloud for now.
                {
                    if (bForCreate &&
                    (this.LoginType == LoginType.Certificate || this.LoginType == LoginType.AsymmetricKey))
                    {
                        // Certificate and Asymmetric Key logins can't alter their properties except Name
                        // In the case when this is a Certificate Login or an Asymmetric Key login and
                        // bForCreate is false we skip this clause and go to the next one
                        // and let SQL server error out
                        string propertyName = this.LoginType == LoginType.Certificate
                                              ? "Certificate" : "AsymmetricKey";
                        string optionName = this.LoginType == LoginType.Certificate
                                            ? "CERTIFICATE" : "ASYMMETRIC KEY";

                        string propertyValue = (string)this.GetPropValue(propertyName);
                        if (propertyValue == null || propertyValue.Length == 0)
                        {
                            throw new PropertyNotSetException(propertyName);
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                        " FROM {0} {1}",
                                        optionName,
                                        MakeSqlBraket(propertyValue));
                    }
                    else
                    {
                        if (bForCreate)
                        {
                            if (this.LoginType == LoginType.ExternalUser || this.LoginType == LoginType.ExternalGroup)
                            {
                                sb.Append(" FROM EXTERNAL PROVIDER");
                            }
                            else
                            {
                                sb.Append(" FROM WINDOWS");
                            }
                        }

                        int lastLength = sbOption.Length;
                        Object o = GetPropValueOptional("DefaultDatabase");

                        if (null != o)
                        {
                            bStuffAdded = true;
                            sbOption.Append("DEFAULT_DATABASE=");
                            sbOption.Append(MakeSqlBraket((string)o));
                        }

                        o = GetPropValueOptional("Language");
                        if (null != o && ((string)o).Length > 0)
                        {
                            AddComma(sbOption, ref bStuffAdded);
                            sbOption.Append("DEFAULT_LANGUAGE=");
                            sbOption.Append(MakeSqlBraket((string)o));
                        }
                    }
                }

                if (sbOption.Length > 0)
                {
                    sb.Append(" WITH ");
                    sb.Append(sbOption.ToString());
                }
            }
            // There is no need of extra call to write comment as there is already exists a function call in the beginning of this function.
            // The call for scripting comment in the if condition is moved out. Hence there is no need to call scripting comment here.
            if (sb.Length > 0)
            {
                sc.Add(sb.ToString());

                foreach (string s in rolesCmd)
                {
                    sc.Add(s);
                }

                if (disableCmd.Length > 0)
                {
                    sc.Add(disableCmd.ToString());
                }
            }

            //Here control will only reach if targetenginetype is not cloud.
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                Property denyWindowsLogin = this.Properties.Get("DenyWindowsLogin");

                //for create we always script DenyWindowsLogin if available
                if (bForCreate && null != denyWindowsLogin.Value && true == (bool)denyWindowsLogin.Value)
                {
                    sc.Add("DENY CONNECT SQL TO " + FormatFullNameForScripting(sp));
                }

                //for alter we script DenyWindowsLogin only if it is dirty
                if (!bForCreate && true == denyWindowsLogin.Dirty)
                {
                    if (true == (bool)denyWindowsLogin.Value)
                    {
                        sc.Add("DENY CONNECT SQL TO " + FormatFullNameForScripting(sp));
                    }
                    else
                    {
                        sc.Add("GRANT CONNECT SQL TO " + FormatFullNameForScripting(sp));
                    }
                }
            }
        }

        internal override void ScriptAssociations(StringCollection rolesCmd, ScriptingPreferences sp)
        {
            if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                //
                // add server roles membership for those
                // that this login is a member of
                //
                StringCollection roles = this.ListMembers();
                foreach (string role in roles)
                {
                    rolesCmd.Add(GetAddToRoleDdl(role, sp));
                }
            }
        }

        private void ScriptCreateForCloud(StringCollection query, ScriptingPreferences sp)
        {
            if (this.LoginType != LoginType.SqlLogin && this.LoginType != LoginType.ExternalUser && this.LoginType != LoginType.ExternalGroup)
            {
                throw new UnsupportedVersionException(ExceptionTemplates.InvalidPropertyValueForVersion(this.GetType().Name, "LoginType", this.LoginType.ToString(), LocalizableResources.EngineCloud));
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringBuilder disableCmd = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // This function call must be placed out of if condition as it is the correct place to print
            // Also, this avoids the second call to script comment after the if condition
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            sb.Append("CREATE LOGIN ");
            string scriptName = FormatFullNameForScripting(sp);
            sb.Append(scriptName);

            StringBuilder sbOption = new StringBuilder();

            if (this.LoginType == LoginType.SqlLogin)
            {
                if (!sp.ScriptForCreateDrop)
                {
                    sb.Insert(0, Globals.newline);
                    sb.Insert(0, "/* For security reasons the login is created disabled and with a random password. */");
                }

                if (null != password)
                {
                    sbOption.Append("PASSWORD=");
                    sbOption.Append(MakeSqlString((string)this.password));
                }
                else if (null != sp && !sp.ScriptForCreateDrop)
                {
                    // we are in scripting mode at this point and we don't have
                    // the password. We will generate a random password of reasonable
                    // complexity, because we want the script to execute without
                    // failure. It is considered to be the responsability of the
                    // system administrator to further manage the login, ie change
                    // its password to something else
                    sbOption.Append("PASSWORD=");
                    sbOption.Append(MakeSqlString(SecurityUtils.GenerateRandomPassword()));
                }
                else
                {
                    throw new PropertyNotSetException("password");
                }

                if (!sp.ScriptForCreateDrop)
                {
                    disableCmd.AppendFormat(SmoApplication.DefaultCulture,
                                                "ALTER LOGIN {0} DISABLE", scriptName);
                }
            }
            else
            {
                sb.Append(" FROM EXTERNAL PROVIDER");
                
                if (this.State == SqlSmoState.Creating && ObjectId != Guid.Empty)
                {
                    sbOption.Append($"OBJECT_ID = {MakeSqlString(ObjectId.ToString())}");
                }
            }

            if (sbOption.Length > 0)
            {
                sb.Append(" WITH ");
                sb.Append(sbOption.ToString());
            }

            // There is no need of extra call to write comment as there is already exists a function call in the beginning of this function.
            // The call for scripting comment in the if condition is moved out. Hence there is no need to call scripting comment here.
            if (sb.Length > 0)
            {
                query.Add(sb.ToString());

                if (disableCmd.Length > 0)
                {
                    query.Add(disableCmd.ToString());
                }
            }
        }

        private void ScriptCreateGreaterEqual9(StringCollection query, ScriptingPreferences sp)
        {
            ScriptLogin(query, sp, true, false);
        }

        private void ScriptCreateLess9(StringCollection query, ScriptingPreferences sp)
        {
            bool bSuppressDirtyCheck = sp.SuppressDirtyCheck;
            StringBuilder statement = new StringBuilder();

            ScriptIncludeHeaders(statement, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_LOGIN80, "NOT", FormatFullNameForScripting(sp, false));
                statement.Append(sp.NewLine);
                statement.Append("BEGIN");
                statement.Append(sp.NewLine);
            }

            if (this.LoginType == LoginType.SqlLogin)
            {
                statement.Append("EXEC master.dbo.sp_addlogin ");
                statement.AppendFormat(SmoApplication.DefaultCulture, "@loginame = {0}", FormatFullNameForScripting(sp, false));

                int chgcount = 0;
                if (null != this.password)
                {
                    statement.Append(Globals.commaspace);
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                                        "@passwd = {0}",
                                        MakeSqlString((string)this.password));
                    chgcount++;
                }
                else if (!sp.ScriptForCreateDrop)
                {
                    // we are in scripting mode at this point and we don't have
                    // the password. We will generate a random password of reasonable
                    // complexity, because we want the script to execute without
                    // failure. It is considered to be the responsability of the
                    // system administrator to further manage the login, ie change
                    // its password to something else
                    // the password gets generated on the client side
                    StringBuilder pwdGenScript = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    SecurityUtils.ScriptPlaceholderPwd(pwdGenScript);
                    statement.Insert(0, pwdGenScript.ToString());
                    statement.Append(", @passwd = @placeholderPwd");
                    chgcount++;
                }
                else
                {
                    throw new PropertyNotSetException("password");
                }

                Property p = Properties.Get("DefaultDatabase");
                if (p.Value != null && (bSuppressDirtyCheck || p.Dirty))
                {
                    statement.Append(Globals.commaspace);
                    statement.AppendFormat(SmoApplication.DefaultCulture, "@defdb = N'{0}'", SqlString((string)p.Value));
                    chgcount++;
                }

                Object oSid = GetPropValueOptional("Sid");

                    if (null != oSid && (sp.ScriptForCreateDrop || sp.Security.Sid))
                    {
                        //Control will only reach here when targetengine type is not cloud.
                        if (Cmn.DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
                        {
                            throw new InvalidSmoOperationException(ExceptionTemplates.CloudSidNotApplicableOnStandalone);
                        }
                        statement.Append(", @sid=");
                        AppendSid(oSid, statement);
                    }

                // language is treated differently because it could be the default
                // which is here the empty string
                p = Properties.Get("Language");
                if (p.Value != null && (bSuppressDirtyCheck || p.Dirty) &&
                    0 < ((string)p.Value).Length)
                {
                    statement.Append(Globals.commaspace);
                    statement.AppendFormat(SmoApplication.DefaultCulture, "@deflanguage = N'{0}'", SqlString((string)p.Value));
                    chgcount++;
                }

            }
            else if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType) //Control will reach here only if TargetEngineType is not Cloud.
            {
                if (this.LoginType == LoginType.WindowsUser || this.LoginType == LoginType.WindowsGroup)
                {
                    // determine if we create this login or deny it
                    bool bDeny = false;
                    Property deny = Properties.Get("DenyWindowsLogin");
                    if (deny.Value != null && (bSuppressDirtyCheck || deny.Dirty))
                    {
                        bDeny = (bool)deny.Value;
                    }

                    if (bDeny)
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_denylogin @loginame = {0}",
                                               FormatFullNameForScripting(sp, false));
                        statement.Append(Globals.newline);
                    }
                    else
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_grantlogin @loginame = {0}",
                                            FormatFullNameForScripting(sp, false));
                        statement.Append(Globals.newline);


                        // also add the properties, if they have changed
                        Property p = Properties.Get("DefaultDatabase");
                        if (p.Value != null && (bSuppressDirtyCheck || p.Dirty))
                        {
                            statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_defaultdb @loginame = {0}, @defdb = N'{1}'",
                                                   FormatFullNameForScripting(sp, false), SqlString((string)p.Value));
                            statement.Append(Globals.newline);
                        }

                        GetLanguageDDL(statement, bSuppressDirtyCheck);
                    }
                }
                else
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.UnknownEnumeration(this.LoginType.GetType().Name));
                }
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.Append(Globals.newline);
                statement.Append("END");
            }

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                statement.Insert(0, Globals.newline);
                statement.Insert(0, ExceptionTemplates.IncludeHeader(
                                                  UrnSuffix, FormatFullNameForScripting(sp),
                                                  DateTime.Now.ToString(GetDbCulture())));
            }

            query.Add(statement.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
            this.SetProperties();
        }

        // generates the scripts for the alter action
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                ScriptAlterGreaterEqual9(query, sp);
            }
            else
            {
                ScriptAlterLess9(query, sp);
            }
        }

        void ScriptAlterGreaterEqual9(StringCollection query, ScriptingPreferences sp)
        {
            ScriptLogin(query, sp, false, false);
        }

        void ScriptAlterLess9(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder statement = new StringBuilder();

            Property p = Properties.Get("DefaultDatabase");
            if (p.Value != null && p.Dirty)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_defaultdb @loginame= N'{0}', @defdb= N'{1}'",
                                       SqlString(this.Name), SqlString(p.Value.ToString()));
                query.Add(statement.ToString());
                statement.Length = 0;
            }

            LoginType type = (LoginType)Properties.Get("LoginType").Value;
            if (type == LoginType.WindowsUser || type == LoginType.WindowsGroup)
            {
                if (null != this.password)
                {
                    throw new SmoException(ExceptionTemplates.PasswdModiOnlyForStandardLogin);
                }
                // grant or deny acces to the login or group, if this property has been modified

                Property deny = Properties.Get("DenyWindowsLogin");
                if (deny.Value != null && deny.Dirty)
                {
                    bool bDeny = (bool)deny.Value;

                    if (bDeny)
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_denylogin @loginame=N'{0}'",
                                               SqlString(this.Name));
                        query.Add(statement.ToString());
                    }
                    else
                    {
                        statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_grantlogin @loginame=N'{0}'",
                                               SqlString(this.Name));

                        query.Add(statement.ToString());
                    }
                }
            }
            else //standard login
            {
                if (true == this.Properties.Get("DenyWindowsLogin").Dirty)
                {
                    throw new SmoException(ExceptionTemplates.DenyLoginModiNotForStandardLogin);
                }
            }

            statement.Length = 0;

            GetLanguageDDL(statement, false);
            if (statement.Length > 0)
            {
                query.Add(statement.ToString());
            }
        }

        protected override void PostCreate()
        {
            this.SetMustChangePassword();
            InitVariables();
        }

        internal void GetLanguageDDL(StringBuilder statement, bool bSuppressDirtyCheck)
        {
            // language is treated differently because it could be the default
            // which is here the empty stirng
            Property p = Properties.Get("Language");
            if (p.Value != null && (bSuppressDirtyCheck || p.Dirty))
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_defaultlanguage @loginame = N'{0}'", SqlString(this.Name));
                if (0 < ((string)p.Value).Length)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, ", @language = N'{0}'", SqlString((string)p.Value));
                }
            }
        }


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

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                if (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType &&
                    sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        Scripts.INCLUDE_EXISTS_LOGIN90,
                        "",
                        FormatFullNameForScripting(sp, false));
                    sb.Append(sp.NewLine);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP LOGIN {0}", FormatFullNameForScripting(sp));
            }
            else
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        Scripts.INCLUDE_EXISTS_LOGIN80,
                        "",
                        FormatFullNameForScripting(sp, false));
                    sb.Append(sp.NewLine);
                }

                // we have to decide if it's a standard or a NT login
                LoginType type = (LoginType)Properties["LoginType"].Value;
                if (type == LoginType.WindowsUser || type == LoginType.WindowsGroup)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_revokelogin @loginame = {0}",
                                                 FormatFullNameForScripting(sp, false));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_droplogin @loginame = {0}",
                                                 FormatFullNameForScripting(sp, false));
                }
            }

            dropQuery.Add(sb.ToString());
        }

        ///<summary>
        /// List the roles in which this login is a member
        ///</summary>
        public StringCollection ListMembers()
        {
            try
            {
                CheckObjectState();

                StringCollection membercol = new StringCollection();

                if (!IsDesignMode)
                {
                    // build the request for the enumerator
                    Request req = new Request(string.Format(SmoApplication.DefaultCulture, "Server[@Name='{0}']/Role", Urn.EscapeString(GetServerName())));
                    req.Fields = new String[] { "Name" };

                    // process data
                    foreach (DataRow dr in this.ExecutionManager.GetEnumeratorData(req).Rows)
                    {
                        req = new Request(string.Format(SmoApplication.DefaultCulture, "Server[@Name='{0}']/Role[@Name='{1}']/Member[@Name='{2}']", Urn.EscapeString(GetServerName()),
                                                        Urn.EscapeString((String)dr["Name"]),
                                                        Urn.EscapeString(this.Name)));
                        req.Fields = new String[] { "Name" };
                        if (0 != this.ExecutionManager.GetEnumeratorData(req).Rows.Count)
                        {
                            membercol.Add((String)dr["Name"]);
                        }
                    }
                }
                else
                {
                    membercol = this.EnumRolesForLogin(this.Name);
                }

                return membercol;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumMembers, this, e);
            }
        }


        ///<summary>
        /// Enumerates the database mappings for this login
        ///</summary>
        public DatabaseMapping[] EnumDatabaseMappings()
        {
            try
            {
                CheckObjectState();

                DatabaseMapping[] mapping = new DatabaseMapping[0];
                if (!this.IsDesignMode)
                {
                    Urn uMappings = this.Urn.ToString() + "/DatabaseMapping";
                    Request req = new Request(uMappings);
                    DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

                    if (dt.Rows.Count > 0)
                    {
                        mapping = new DatabaseMapping[dt.Rows.Count];
                        for (int iRow = 0; iRow < dt.Rows.Count; iRow++)
                        {
                            DataRow dr = dt.Rows[iRow];
                            mapping[iRow] = new DatabaseMapping(Convert.ToString(dr["LoginName"], SmoApplication.DefaultCulture),
                                                                Convert.ToString(dr["DBName"], SmoApplication.DefaultCulture),
                                                                Convert.ToString(dr["UserName"], SmoApplication.DefaultCulture));
                        }
                    }
                }
                else
                {
                    // TODO: Try to retrieve the DatabaseMappings information from existing tree, by traversing
                    // the nodes of the in-memory tree

                }
                return mapping;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumDatabaseMappings, this, e);
            }
        }

        ///<summary>
        /// Returns true if the login is a member of the role given as argument
        ///</summary>
        public bool IsMember(string role)
        {
            try
            {
                if (null == role)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("role"));
                }

                CheckObjectState();

                if (this.IsDesignMode)
                {
                    return this.EnumLoginsForRole(role).Contains(this.Name);
                }

                StringBuilder filter = new StringBuilder();
                filter.Append("Server[@Name='");
                filter.Append(Urn.EscapeString(ParentColl.ParentInstance.InternalName));
                filter.Append("']/Role[@Name='");
                filter.Append(Urn.EscapeString(role));
                filter.Append("']/Member[@Name='");
                filter.Append(Urn.EscapeString(this.Name));
                filter.Append("']");
                Request req = new Request(filter.ToString());

                return (this.ExecutionManager.GetEnumeratorData(req).Rows.Count > 0);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.IsMember, this, e);
            }
        }

        ///<summary>
        /// Returns the user for this login in the specified database
        ///</summary>
        /// <returns>The name of the user mapped to this login in the specified database, or an empty string
        /// if no such user exists.</returns>
        public string GetDatabaseUser(string databaseName)
        {
            try
            {
                if (null == databaseName)
                {
                    throw new ArgumentNullException("databaseName");
                }

                CheckObjectState();

                var user = this.Parent.Databases[databaseName].Users.Cast<User>().FirstOrDefault(u => u.Login == this.Name);
                return user == null ? string.Empty : user.Name;

            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetDatabaseUser, this, e);
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        ///<summary>
        ///Script object with specific scripting optiions</summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        ///<summary>
        ///Add the login to the specified role.</summary>
        public void AddToRole(string role)
        {
            try
            {
                CheckObjectState();

                if (this.IsDesignMode)
                {
                    this.AddLoginToRole(role, this.Name);
                }
                else
                {
                    this.ExecutionManager.ExecuteNonQuery(GetAddToRoleDdl(role, new ScriptingPreferences(this)));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddToRole, this, e);
            }
        }

        private string GetAddToRoleDdl(string role, ScriptingPreferences sp)
        {
            if (null == role)
            {
                throw new ArgumentNullException("role");
            }


            if (VersionUtils.IsTargetServerVersionSQl11OrLater(sp.TargetServerVersion))
            {
                return string.Format(SmoApplication.DefaultCulture,
                        "ALTER SERVER ROLE {0} ADD MEMBER {1}", MakeSqlBraket(role), MakeSqlBraket(this.Name));
            }
            else
            {
                string prefix;
                if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                {
                    prefix = "sys";
                }
                else
                {
                    prefix = "master.dbo";
                }
                return string.Format(SmoApplication.DefaultCulture,
                                     "EXEC {0}.sp_addsrvrolemember @loginame = {1}, @rolename = {2}",
                                     prefix,
                                     MakeSqlString(this.Name),
                                     MakeSqlString(role));
            }
        }


        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            if (this.ServerVersion.Major < 9)
            {
                throw new InvalidVersionSmoOperationException(this.ServerVersion);
            }

            // the user is responsible to put the database in single user mode on 7.0 server
            AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER LOGIN {0} WITH NAME={1}",
                                          FormatFullNameForScripting(new ScriptingPreferences()), MakeSqlBraket(newName)));
        }

        ///<summary>
        /// Retrieves ProxyAccounts associated with login
        ///</summary>
        public DataTable EnumAgentProxyAccounts()
        {

            StringCollection query = new StringCollection();
            StringBuilder statement = new StringBuilder();
            statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_enum_login_for_proxy @name={0}", MakeSqlBraket(this.Name));
            query.Add(statement.ToString());

            return this.ExecutionManager.ExecuteWithResults(query).Tables[0];
        }

        /// <summary>
        /// Disable Login if version >= 9. Otherwise, throw an exception.
        /// This is not part of Login.Alter because LOGIN [] ENABLE|DISABLE has to be issued by itself
        /// </summary>
        public void Disable()
        {
            try
            {
                //action only valid on Yukon
                ThrowIfBelowVersion90();
                //action not valid if the object is Creating or in the Pending state
                CheckObjectState();

                if (!this.IsDesignMode)
                {
                    //
                    // disable the login
                    //
                    String stmt = string.Format(SmoApplication.DefaultCulture, "ALTER LOGIN {0} DISABLE",
                                                this.FormatFullNameForScripting(new ScriptingPreferences()));
                    // execute the script
                    this.ExecutionManager.ExecuteNonQuery(stmt);
                }

                //
                // Update the property value in the local storage ( property bug )
                //

                //lookup the property ordinal from name
                int indexIsDisabled = this.Properties.LookupID("IsDisabled", PropertyAccessPurpose.Write);
                //set the new value
                this.Properties.SetValue(indexIsDisabled, true);
                //mark the property as retrived, that means that it is
                //in sync with value on the server
                this.Properties.SetRetrieved(indexIsDisabled, true);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.LoginDisable, this, e);
            }
        }

        /// <summary>
        /// Enable Login if version >= 9. Otherwise, throw an exception.
        /// This is not part of Login.Alter because LOGIN [] ENABLE|DISABLE has to be issued by itself
        /// </summary>
        public void Enable()
        {
            try
            {
                //action only valid on Yukon
                ThrowIfBelowVersion90();
                //action not valid if the object is Creating or in the Pending state
                CheckObjectState();

                if (!this.IsDesignMode)
                {
                    //
                    // Enable the login
                    //
                    String stmt = string.Format(SmoApplication.DefaultCulture, "ALTER LOGIN {0} ENABLE",
                                                this.FormatFullNameForScripting(new ScriptingPreferences()));
                    // execute the script
                    this.ExecutionManager.ExecuteNonQuery(stmt);
                }

                //
                // Update the property value in the local storage ( property bug )
                //

                //lookup the property ordinal from name
                int indexIsDisabled = this.Properties.LookupID("IsDisabled", PropertyAccessPurpose.Write);
                //set the new value
                this.Properties.SetValue(indexIsDisabled, false);
                //mark the property as retrived, that means that it is
                //in sync with value on the server
                this.Properties.SetRetrieved(indexIsDisabled, true);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.LoginEnable, this, e);
            }
        }

        /// <summary>
        /// Refresh the Login
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
        }

        /// <summary>
        /// Adds a credential to the login
        /// </summary>
        /// <param name="credentialName"></param>
        public void AddCredential(string credentialName)
        {
            try
            {
                ThrowIfBelowVersion100();
                if (this.IsDesignMode)
                {
                    if (this.State != SqlSmoState.Existing)
                    {
                        throw new InvalidSmoOperationException("AddCredential", this.State);
                    }

                    if (!this.credentialCollection.Contains(credentialName))
                    {
                        this.credentialCollection.Add(credentialName);
                    }
                    else
                    {
                        throw new SmoException(ExceptionTemplates.CannotAddObject("Credential", credentialName));
                    }
                }
                else
                {
                    this.ExecutionManager.ExecuteNonQuery(ScriptAddDropCredential(true, credentialName));
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.AddCredential, this, e);
            }
        }

        /// <summary>
        /// Drops a credential
        /// </summary>
        /// <param name="credentialName"></param>
        public void DropCredential(string credentialName)
        {
            try
            {
                ThrowIfBelowVersion100();

                if (this.IsDesignMode)
                {
                    if (this.State != SqlSmoState.Existing)
                    {
                        throw new InvalidSmoOperationException("DropCredential", this.State);
                    }

                    if (this.credentialCollection.Contains(credentialName))
                    {
                        this.credentialCollection.Remove(credentialName);
                    }
                    else
                    {
                        throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist("Credential", credentialName));
                    }
                }
                else
                {
                    this.ExecutionManager.ExecuteNonQuery(ScriptAddDropCredential(false, credentialName));
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.DropCredential, this, e);
            }
        }

        /// <summary>
        /// Generates the script to add/drop credentials
        /// </summary>
        /// <param name="add"></param>
        /// <param name="credentialName"></param>
        /// <returns></returns>
        private string ScriptAddDropCredential(bool add, string credentialName)
        {
            string query = string.Format(SmoApplication.DefaultCulture, "ALTER LOGIN {0} {1} CREDENTIAL {2}", FullQualifiedName, add ? "ADD" : "DROP", MakeSqlBraket(credentialName));
            return query;
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                                 Microsoft.SqlServer.Management.Common.ServerVersion version,
                                                 Cmn.DatabaseEngineType databaseEngineType,
                                                 Cmn.DatabaseEngineEdition databaseEngineEdition,
                                                 bool defaultTextMode)
        {
            string[] fields = {
                         "LoginType",
                    "DefaultDatabase",
                    "Sid",
                    "Language",
                    "DenyWindowsLogin"};
            List<string> list = GetSupportedScriptFields(typeof(Login.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // on 8.0 and below we do not have permissions on Logins
            if (sp.TargetServerVersion <= SqlServerVersion.Version80 ||
                this.ServerVersion.Major <= 8 ||
                IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType)) //Server level permissions are not supported in cloud.
            {
                return;
            }

            base.AddScriptPermission(query, sp);
        }
    }

    ///<summary>
    /// this is a structure to hold database mappings
    ///</summary>
    public sealed class DatabaseMapping
    {
        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="dbName"></param>
        /// <param name="userName"></param>
        public DatabaseMapping(string loginName, string dbName, string userName)
        {
            this.loginName = loginName;
            this.dbName = dbName;
            this.userName = userName;
        }

        private string loginName;
        /// <summary>
        /// LoginName
        /// </summary>
        /// <value></value>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string LoginName { get { return loginName;}}

        private string dbName;
        /// <summary>
        /// DBName
        /// </summary>
        /// <value></value>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string DBName { get { return dbName;}}

        private string userName;
        /// <summary>
        /// UserName
        /// </summary>
        /// <value></value>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string UserName { get { return userName; } }

        /// <summary>
        /// Override - returns "Login={0};Database={1};User={2}"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture,
                                 "Login={0};Database={1};User={2}",
                                 SqlSmoObject.MakeSqlBraket(LoginName),
                                 SqlSmoObject.MakeSqlBraket(DBName),
                                 SqlSmoObject.MakeSqlBraket(UserName));
        }
    }
}



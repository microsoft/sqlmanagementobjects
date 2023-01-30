// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Security;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class User : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, Cmn.IRenamable,
        IExtendedProperties, IScriptable, IUserOptions
    {
        internal User(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            this.InitVariables();
        }

    private SqlSecureString password;

        private void InitVariables()
        {
            this.password = null;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "User";
            }
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
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

        private DefaultLanguage defaultLanguageObj;

        /// <summary>
        /// Gets or sets the default language of this user object.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public DefaultLanguage DefaultLanguage
        {
            get
            {
                this.ThrowIfCloudProp("DefaultLanguage");
                this.ThrowIfBelowVersion110Prop("DefaultLanguage");

                if (this.defaultLanguageObj == null)
                {
                    this.defaultLanguageObj = new DefaultLanguage(this, "DefaultLanguage");
                }

                return this.defaultLanguageObj;
            }
            //This property is not like other SfcProperties. In order to deserialize this property
            //we need to have a setter for this. Hence implementing an internal setter.
            internal set //Design Mode
            {
                this.ThrowIfCloudProp("DefaultLanguage");
                this.ThrowIfBelowVersion110Prop("DefaultLanguage");

                if (value.IsProperlyInitialized())
                {
                    this.defaultLanguageObj = value;
                }
                else
                {
                    this.defaultLanguageObj = value.Copy(this, "DefaultLanguage");
                }
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
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
            if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType
                && 8 == this.ServerVersion.Major)
            {
                ScriptDropFrom80ToCloud(dropQuery, sp);
                return;
            }

            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                                                UrnSuffix, FormatFullNameForScripting(sp),
                                                DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90
                && this.ServerVersion.Major >= 9)
            {
                if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_USER90, "", SqlString(this.Name));
                    sb.Append(sp.NewLine);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP USER {0}{1}",
                    (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty,
                    FormatFullNameForScripting(sp));
            }
            else
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_USER80, "", SqlString(this.Name));
                    sb.Append(sp.NewLine);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "EXEC dbo.sp_revokedbaccess {0}",
                    FormatFullNameForScripting(sp, false));
            }

            dropQuery.Add(sb.ToString());
        }

        private void ScriptDropFrom80ToCloud(StringCollection dropQuery, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                                                UrnSuffix, FormatFullNameForScripting(sp),
                                                DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            //Such that we don't try to drop "guest" schema
            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "if {0} <> 'guest'",
                            MakeSqlString(this.Name));

            sb.AppendLine();
            sb.Append(Scripts.BEGIN);

            //Drop schema only if such schema exists having owner with same name
            sb.AppendFormat(SmoApplication.DefaultCulture,
                            Scripts.IF_SCHEMA_EXISTS_WITH_GIVEN_OWNER,
                            SqlString(this.Name), //schema name
                            SqlString(this.Name));//owner name

            sb.Append(Scripts.BEGIN);
            sb.AppendLine();

            StringBuilder dropSchemaStmt = new StringBuilder();
            dropSchemaStmt.AppendFormat(SmoApplication.DefaultCulture,
                            "DROP SCHEMA {0}",
                            MakeSqlBraket(this.Name));

            sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC sys.sp_executesql N'{0}'", SqlString(dropSchemaStmt.ToString()));

            sb.AppendLine();
            sb.Append(Scripts.END);

            sb.AppendLine();
            sb.Append(Scripts.END);

            dropQuery.Add(sb.ToString());

            sb = new StringBuilder();
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_USER90, "", SqlString(this.Name));
                sb.Append(sp.NewLine);
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP USER {0}", FormatFullNameForScripting(sp));

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Creates a database user on the instance of SQL Server.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }


        /// <summary>
        /// Creates a database user with a password on the instance of SQL Server.
        /// </summary>
        /// <param name="password">Password.</param>
        public void Create(string password)
        {
            try
            {
                ThrowIfBelowVersion110();

                if (password == null)
                {
                    throw new ArgumentNullException("password");
                }

                Create(new SqlSecureString(password));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        /// <summary>
        /// Creates a database user with a password on the instance of SQL Server.
        /// </summary>
        /// <param name="password">Password.</param>
        public void Create(SecureString password)
        {
            try
            {
                ThrowIfBelowVersion110();

                if (password == null)
                {
                    throw new ArgumentNullException("password");
                }

                this.password = password;
                Create();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            finally
            {
                this.InitVariables();
            }
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            ValidateVersionAndEngineTypeForScripting(sp);
            ValidateBeforeScriptCreate();

            if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType
                && 8 == this.ServerVersion.Major)
            {
                ScriptCreateFrom80ToCloud(createQuery, sp);
                return;
            }

            bool bSuppressDirtyCheck = sp.SuppressDirtyCheck;
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                                                UrnSuffix, FormatFullNameForScripting(sp),
                                                DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90
                && this.ServerVersion.Major >= 9)
            {
                StringBuilder sbOption = new StringBuilder();
                bool optionAdded = false;

                // on some early SQL releases on Linux Login returns \ for a user without login
                var loginProperty = GetPropertyOptional("Login");
                UserType type = GetPropValueOptional("UserType", UserType.SqlUser);
                if (type == UserType.SqlUser && loginProperty != null && (string)loginProperty.Value == @"\")
                {
                    type = UserType.NoLogin;
                }
                //Include If not exisits is not supported for cloud if User Type is SQL LOGIN type
                if (sp.IncludeScripts.ExistenceCheck && !(sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase && type == UserType.SqlUser))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_USER90, "NOT", SqlString(this.Name));
                    sb.Append(sp.NewLine);
                }

                sb.Append("CREATE USER ");
                sb.Append(FormatFullNameForScripting(sp));

                // Bug 446932: make sure a user mapped to a login that is a certificate or key still
                // considers itself a "Login" for scripting purposes here, since the "FOR CERTIFICATE" or "FOR ASYMMETRIC KEY"
                // clauses would be invalid and cause a throw
                var loginName = (string)this.GetPropValueOptional(nameof(Login), String.Empty);
                if (type == UserType.Certificate || type == UserType.AsymmetricKey)
                {

                    if (loginName != null && loginName.Length > 0)
                    {
                        // The script should just generate "FOR LOGIN" syntax regasrdless of whether the login is sql/nt/cert/key-based
                        // ARTIFICIALLY CHANGING THE TYPE FOR THIS PURPOSE!! The expectation is to have this only propagate thru the
                        // switch and subsequent scripting based on the switch, up to the "else".
                        type = UserType.SqlUser;
                    }
                }

                // External user scripting should be blocked below Sql 160 standalone database version
                if (type == UserType.External && sp.TargetServerVersion < SqlServerVersion.Version160 
                    && sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.Standalone
                    && sp.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlManagedInstance)
                {
                    throw new UnsupportedVersionException(
                        ExceptionTemplates.InvalidPropertyValueForVersion(
                            this.GetType().Name, "UserType", this.UserType.ToString(), GetSqlServerName(sp)));
                }

                string propertyName = null;
                string optionName = null;
                bool isOptional = false;

                switch (type)
                {
                    case UserType.SqlUser:
                        propertyName = "Login";
                        optionName = "LOGIN";
                        isOptional = true;
                        break;

                    case UserType.Certificate:
                        propertyName = "Certificate";
                        optionName = "CERTIFICATE";
                        break;

                    case UserType.AsymmetricKey:
                        propertyName = "AsymmetricKey";
                        optionName = "ASYMMETRIC KEY";
                        break;

                    case UserType.NoLogin:
                        propertyName = null;
                        optionName = " WITHOUT LOGIN";
                        break;

                    case UserType.External:
                        if (!string.IsNullOrEmpty(loginName))
                        {
                            propertyName = "Login";
                            optionName = "LOGIN";
                        }
                        else
                        {
                            propertyName = null;
                            optionName = " EXTERNAL PROVIDER";
                        }
                        break;

                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("UserType"));
                }

                string s = null;


                if (type == UserType.NoLogin)
                {
                    // certain system users have user type NoLogin
                    // sys, guest, INFORMATION_SCHEMA, etc
                    // we don't want to add the WITHOUT LOGIN
                    // clause when asked to script them.
                    if (this.State == SqlSmoState.Creating || !this.IsSystemObject)
                    {
                        sb.Append(optionName);
                    }
                }
                else if (type == UserType.External && string.IsNullOrEmpty(loginName))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                    " FROM {0} ",
                    optionName);
                } else
                {

                    s = (string)this.GetPropValueOptional(propertyName);
                    if (s != null && s.Length > 0)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            " FOR {0} {1}",
                            optionName,
                            MakeSqlBraket(s));
                    }
                    else if (!isOptional)
                    {
                        throw new PropertyNotSetException(s);
                    }

                    AuthenticationType authenticationType = AuthenticationType.Instance;
                    if (this.IsSupportedProperty("AuthenticationType"))
                    {
                        authenticationType = this.GetPropValueOptional<AuthenticationType>("AuthenticationType", AuthenticationType.Instance);
                    }

                    if (string.IsNullOrEmpty(s) //Not mapped to Login/Asymmetric Key/Certificate
                        && type == UserType.SqlUser //And of type SqlUser
                        && ((this.State == SqlSmoState.Creating && this.password != null) //In Creating State, if password is not null
                            || (this.State != SqlSmoState.Creating && authenticationType == AuthenticationType.Database))
                        )
                    {
                        AddPasswordOptions(sp,
                                        sbOption,
                                        this.password,
                                        null,
                                        ref optionAdded);
                    }
                }

                this.AddDefaultLanguageOptionToScript(sbOption, sp, ref optionAdded);

                s = (string)GetPropValueOptional("DefaultSchema");

                LoginType lType = GetPropValueOptional("LoginType", LoginType.SqlLogin);

                if (null != s && s.Length > 0 && (lType != LoginType.WindowsGroup || ServerVersion.Major >= 11))
                {
                    AddComma(sbOption, ref optionAdded);
                    sbOption.Append("DEFAULT_SCHEMA=");
                    sbOption.Append(MakeSqlBraket(s));
                }

                if (sbOption.Length > 0)
                {
                    sb.Append(" WITH ");
                    sb.Append(sbOption.ToString());
                }
            }
            else
            {
                UserType type = GetPropValueOptional("UserType", UserType.SqlUser);
                // Certificate and AsymmetricKey properties are not supported in this case
                if (type == UserType.Certificate || type == UserType.AsymmetricKey || type == UserType.External)
                {
                    throw new UnsupportedVersionException(
                        ExceptionTemplates.InvalidPropertyValueForVersion(
                            this.GetType().Name, "UserType", this.UserType.ToString(), GetSqlServerName(sp)));
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_USER80, "NOT", SqlString(this.Name));
                    sb.Append(sp.NewLine);
                }

                if (0 == StringComparer.Compare(this.Name, "guest"))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_grantdbaccess @loginame = {0}", FormatFullNameForScripting(sp, false));
                }
                else // non-guest user
                {
                    // create the user only if the login property is set
                    // because we need it in t-sql
                    Property login = this.Properties["Login"];
                    if ((null == login.Value) || (login.Value.ToString() == string.Empty) || (!bSuppressDirtyCheck && !login.Dirty))
                    {
                        // throw new SmoException("You must set the Login property before creating an user");
                        throw new SmoException(ExceptionTemplates.UsersWithoutLoginsDownLevel(GetSqlServerName(sp)));
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_grantdbaccess @loginame = {0}, @name_in_db = {1}",
                            MakeSqlString((string)login.Value),
                            FormatFullNameForScripting(sp, false));
                    }
                }
            }
            createQuery.Add(sb.ToString());

            if (!sp.ScriptForCreateDrop && sp.IncludeScripts.Associations)
            {
                ScriptAssociations(createQuery, sp);
            }

        }

        private void AddDefaultLanguageOptionToScript(
            StringBuilder sbOption,
            ScriptingPreferences sp,
            ref bool optionAdded)
        {
            //Script default language only if the parent database is Contained database.
            Database parentDatabase = this.ParentColl.ParentInstance as Database;

            if (parentDatabase.IsSupportedProperty("ContainmentType")
                && this.IsSupportedProperty("DefaultLanguageLcid", sp))
            {
                ContainmentType parentContainmentType = parentDatabase.GetPropValueOptional<ContainmentType>("ContainmentType", ContainmentType.None);

                if (parentContainmentType != ContainmentType.None)
                {
                    bool nameRetrived = false; //Either of Lcid or name should be used for default language scripting, not both.
                    string languageValue = string.Empty;

                    if (this.GetPropertyOptional("DefaultLanguageName").Dirty) //Name shall only be scripted if user has changed it otherwise scripting lcid should get the priority.
                    {
                        Property langProp = this.GetPropertyOptional("DefaultLanguageName");
                        if (langProp.Dirty || !sp.ForDirectExecution)
                        {
                            //not using ToString() method as we are not checking whether the value
                            //is already null or not.
                            string langPropValue = langProp.Value as string;
                            if (!string.IsNullOrEmpty(langPropValue))
                            {
                                languageValue = langPropValue;
                            }
                            else if (langProp.Dirty && sp.ScriptForAlter) //"NONE" is needed only while scripting for alter.
                            {
                                languageValue = "NONE";
                            }

                            nameRetrived = true;
                        }
                    }

                    if (!nameRetrived)
                    {
                        //Try scripting default language lcid only if default language name is not dirty.
                        Property langProp = this.GetPropertyOptional("DefaultLanguageLcid");
                        if (!langProp.IsNull
                            && (langProp.Dirty || !sp.ForDirectExecution))
                        {
                            if (((int)langProp.Value) >= 0) //-1 is the value returned from Enumerator when Engine returns NULL
                            {
                                languageValue = langProp.Value.ToString();
                            }
                            else if (langProp.Dirty && sp.ScriptForAlter) //"NONE" is needed only while scripting for alter.
                            {
                                languageValue = "NONE";
                            }
                        }
                    }

                    if (languageValue.Length > 0) //Only if either Lcid or Name is available or changed.
                    {
                        AddComma(sbOption, ref optionAdded);
                        sbOption.Append("DEFAULT_LANGUAGE=");
                        if (nameRetrived)
                        {
                            sbOption.Append(MakeSqlBraket(languageValue));
                        }
                        else //LCID can't be in paranthesis
                        {
                            sbOption.Append(languageValue);
                        }
                    }
                }
                else
                {
                    this.ValidateDefaultLanguageNotDirty();
                }
            }
        }

        private void ValidateVersionAndEngineTypeForScripting(ScriptingPreferences sp)
        {
            AuthenticationType authenticationType = AuthenticationType.Instance;
            if (this.IsSupportedProperty("AuthenticationType"))
            {
                authenticationType = this.GetPropValueOptional<AuthenticationType>("AuthenticationType", AuthenticationType.Instance);
            }

            //Sql user with password cannot be created on 
            //version earlier than Denali on Standalone engine type.
            if ((this.State == SqlSmoState.Creating
                    && this.password != null)
                ||(this.State != SqlSmoState.Creating
                    && authenticationType == AuthenticationType.Database)
                )
            {
                ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            }
        }

        private void ValidateBeforeScriptCreate()
        {
            UserType type = (UserType)GetPropValueOptional("UserType", UserType.SqlUser); //SqlUser is the default.
            string mappedLoginName = (string)GetPropValueOptional("Login");

            if (this.password != null
                && (type == UserType.AsymmetricKey
                    || type == UserType.Certificate
                    || type == UserType.NoLogin
                    || (type == UserType.SqlUser && !string.IsNullOrEmpty(mappedLoginName))  //User is mapped to a Login.
                    ))
            {
                throw new SmoException(ExceptionTemplates.PasswordOnlyForDatabaseAuthenticatedNonWindowsUser);
            }

            if (this.IsSupportedProperty("DefaultLanguageLcid"))
            {
                if (type == UserType.AsymmetricKey
                    || type == UserType.Certificate)
                {
                    this.ValidateDefaultLanguageNotDirty();
                }
                else
                {
                    this.DefaultLanguage.VerifyBothLcidAndNameNotDirty(true);
                }
            }
        }

        /// <summary>
        /// Allows the administrator to change any user password.
        /// </summary>
        /// <param name="newPassword">The new user password.</param>
        public void ChangePassword(string newPassword)
        {
            try
            {
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                ChangePassword(new SqlSecureString(newPassword));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        /// <summary>
        /// Allows the administrator to change any user password.
        /// </summary>
        /// <param name="newPassword">The new user password.</param>
        public void ChangePassword(SecureString newPassword)
        {
            try
            {
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                ExecuteUserPasswordOptions(newPassword,
                                            null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        /// <summary>
        /// Allows the user to change password by providing the old user password.
        /// </summary>
        /// <param name="oldPassword">The old user password.</param>
        /// <param name="newPassword">The new user password.</param>
        public void ChangePassword(string oldPassword, string newPassword)
        {
            try
            {
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                if (null == oldPassword)
                {
                    throw new ArgumentNullException("oldPassword");
                }

                ChangePassword(new SqlSecureString(oldPassword),
                                new SqlSecureString(newPassword));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        /// <summary>
        /// Allows the user to change password by providing the old user password.
        /// </summary>
        /// <param name="oldPassword">The old user password.</param>
        /// <param name="newPassword">The new user password.</param>
        public void ChangePassword(SecureString oldPassword, SecureString newPassword)
        {
            try
            {
                if (null == newPassword)
                {
                    throw new ArgumentNullException("newPassword");
                }

                if (null == oldPassword)
                {
                    throw new ArgumentNullException("oldPassword");
                }

                ExecuteUserPasswordOptions(newPassword, oldPassword);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        private void ExecuteUserPasswordOptions(SqlSecureString password,
                                         SqlSecureString oldPassword)
        {
            CheckObjectState();
            ThrowIfBelowVersion110();
            ThrowIfCloud();

            if (!(this.UserType == UserType.SqlUser
                && this.GetPropValueOptional<AuthenticationType>("AuthenticationType", AuthenticationType.Instance)
                    == AuthenticationType.Database)
                )
            {
                throw new SmoException(ExceptionTemplates.CannotChangePasswordForUser);
            }

            StringCollection sc = new StringCollection();

            AddDatabaseContext(sc);
            StringBuilder sb = new StringBuilder();
            sb.Append("ALTER USER ");
            ScriptingPreferences sp = new ScriptingPreferences(this);
            sp.ForDirectExecution = true;
            sb.Append(FormatFullNameForScripting(sp));

            StringBuilder sbOption = new StringBuilder();
            bool optionAdded = false;

            AddPasswordOptions(sp, sbOption, password, oldPassword, ref optionAdded);
            if (sbOption.Length > 0)
            {
                sb.Append(" WITH ");
                sb.Append(sbOption.ToString());
            }

            sc.Add(sb.ToString());
            this.ExecutionManager.ExecuteNonQuery(sc);
        }

        private bool userContainmentInProgress = false;

        /// <summary>
        /// When conversion of a SQL Login mapped user to a contained user is
        /// going on, this method takes care of not sending Rename query when called
        /// from this.RenameImpl method.
        /// </summary>
        /// <param name="newName"></param>
        protected override void ExecuteRenameQuery(string newName)
        {
            if (!userContainmentInProgress)
            {
                base.ExecuteRenameQuery(newName);
            }
        }

        /// <summary>
        /// Converts a login mapped user to a contained user with password using the sys.sp_copy_password_to_user stored procedure.
        /// </summary>
        /// <param name="copyLoginName">Copies the earlier mapped login name to the contained user.</param>
        /// <param name="disableLogin">Disables the earlier mapped login.</param>
        public void MakeContained(bool copyLoginName, bool disableLogin)
        {
            try
            {
                CheckObjectState();
                ThrowIfBelowVersion110();
                ThrowIfCloud();

                this.userContainmentInProgress = true;

                if (!(this.UserType == UserType.SqlUser
                    && this.GetPropValueOptional<AuthenticationType>("AuthenticationType", AuthenticationType.Instance)
                        == AuthenticationType.Instance)
                    )
                {
                    throw new SmoException(ExceptionTemplates.CannotCopyPasswordToUser);
                }

                string mappedLoginName = this.Login;

                this.ExecutionManager.ExecuteNonQuery(this.GetMakeContainedScript(copyLoginName, disableLogin));

                if (disableLogin
                    && !this.ExecutionManager.Recording)
                {
                    SimpleObjectKey key = new SimpleObjectKey(mappedLoginName);
                    Login mappedLogin = (this.Parent.Parent as Server).Logins.NoFaultLookup(key) as Login; //Verifies if login object is in Cache or not.

                    if (mappedLogin != null)
                    {
                        Property isLoginDisabled = mappedLogin.GetPropertyOptional("IsDisabled");
                        isLoginDisabled.SetValue(true);
                        isLoginDisabled.SetRetrieved(true);
                    }
                }

                if (copyLoginName)
                {
                    this.RenameImpl(mappedLoginName); //Takes care of the all the follow up SMO operations required after user's name change
                }

                this.Refresh(); //For refreshing properties which might have changed because of the Conversion from Uncontained to Contained.
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.MakeContained, this, e);
            }
            finally
            {
                this.userContainmentInProgress = false;
            }
        }

        private StringCollection GetMakeContainedScript(bool copyLoginName, bool disableLogin)
        {
            StringCollection sc = new StringCollection();
            AddDatabaseContext(sc);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture,
                "EXEC sys.sp_migrate_user_to_contained @username = {0}, @rename = N'{1}', @disablelogin = N'{2}'",
                MakeSqlString(this.Name),
                copyLoginName ? "copy_login_name" : "keep_name",
                disableLogin ? "disable_login" : "do_not_disable_login");

            sc.Add(sb.ToString());
            return sc;
        }

        private void ScriptCreateFrom80ToCloud(StringCollection createQuery, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                createQuery.Add(ExceptionTemplates.IncludeHeader(
                                                UrnSuffix, FormatFullNameForScripting(sp),
                                                DateTime.Now.ToString(GetDbCulture())));
            }

            //Include If not exist is not supported for cloud
            //Include if not exist should be added here, once cloud engine starts supporting it.
            sb.Append("CREATE USER ");
            sb.Append(FormatFullNameForScripting(sp));

            string s = (string)this.GetPropValue("Login");
            if (!string.IsNullOrEmpty(s))
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    " FOR LOGIN {0}",
                    MakeSqlBraket(s));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                            " WITH DEFAULT_SCHEMA={0}",
                            MakeSqlBraket(this.Name)); //Schema name is same as User name.

            createQuery.Add(sb.ToString());

            sb = new StringBuilder();
            sb.AppendFormat(SmoApplication.DefaultCulture,
                            Scripts.IF_SCHEMA_NOT_EXISTS_WITH_GIVEN_OWNER,
                            SqlString(this.Name), //schema name
                            SqlString(this.Name)); //owner name

            sb.Append(Scripts.BEGIN);
            sb.AppendLine();

            StringBuilder createSchemaStmt = new StringBuilder();
            createSchemaStmt.AppendFormat(SmoApplication.DefaultCulture,
                        "CREATE SCHEMA {0} AUTHORIZATION {0}",
                        MakeSqlBraket(this.Name));

            sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC sys.sp_executesql N'{0}'", SqlString(createSchemaStmt.ToString()));


            sb.AppendLine();
            sb.Append(Scripts.END);

            createQuery.Add(sb.ToString());

            if (!sp.ScriptForCreateDrop && sp.IncludeScripts.Associations)
            {
                ScriptAssociations(createQuery, sp);
            }
        }

        internal override void ScriptAssociations(StringCollection createQuery, ScriptingPreferences sp)
        {
            // Add database role membership for each role this user is a member of
            StringCollection roles = EnumRoles();
            foreach (string role in roles)
            {
                createQuery.Add(ScriptAddToRole(role, sp));
            }
        }


        void AddPasswordOptions(ScriptingPreferences sp,
                                StringBuilder sb,
                                SqlSecureString password,
                                SqlSecureString oldPassword,
                                ref bool optionAdded)
        {

            if (!VersionUtils.IsSql11OrLater(sp.TargetServerVersionInternal, this.ServerVersion))
            {
                return;
            }

            if (null != password)
            {
                AddComma(sb, ref optionAdded);
                sb.Append("PASSWORD=");
                sb.Append(MakeSqlString((string)password));

                if (null != oldPassword)
                {
                    sb.Append(" OLD_PASSWORD=");
                    sb.Append(MakeSqlString((string)oldPassword));
                }
            }
            else if (null != sp && !sp.ForDirectExecution)
            {
                // we are in scripting mode at this point and we don't have
                // the password. We will generate a random password of reasonable
                // complexity, because we want the script to execute without
                // failure. It is considered to be the responsability of the
                // system administrator to further manage the user, i.e. change
                // its password to something else
                AddComma(sb, ref optionAdded);
                sb.Append("PASSWORD=");
                sb.Append(MakeSqlString(SecurityUtils.GenerateRandomPassword()));
            }
            else
            {
                throw new ArgumentNullException("password");
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

        protected override void PostCreate()
        {
            this.CheckObjectState();
            this.InitVariables();
            this.isDefaultLanguageModified = this.IsDefaultLanguageModified();

            base.PostCreate();
        }

        protected override void PostAlter()
        {
            this.CheckObjectState();
            this.isDefaultLanguageModified = this.IsDefaultLanguageModified();

            base.PostAlter();
        }

        private bool IsDefaultLanguageModified()
        {
            if (this.IsSupportedProperty("DefaultLanguageLcid"))
            {
                StringCollection sc = new StringCollection();
                sc.Add("DefaultLanguageName");
                sc.Add("DefaultLanguageLcid");

                return this.Properties.ArePropertiesDirty(sc);
            }
            else
            {
                return false;
            }
        }

        private bool isDefaultLanguageModified = false;
        protected override void CleanObject()
        {
            base.CleanObject();

            if (this.isDefaultLanguageModified)
            {
                Property defaultLanguageName = this.Properties.Get("DefaultLanguageName");
                Property defaultLanguageLcid = this.Properties.Get("DefaultLanguageLcid");

                //After Alter() or Create() we can't guarantee that these values are
                //correct or not. Hence we need to retrieve these from server again.
                //But by using SetRetrieved(false) method, we are delaying these properties
                //retrieval to when the user uses these properties next.
                defaultLanguageName.SetRetrieved(false);
                defaultLanguageLcid.SetRetrieved(false);

                //If PropertyBagState is Lazy, that means it still has un-retrieved properties.
                this.propertyBagState = PropertyBagState.Lazy;
            }
            //resetting to original
            this.isDefaultLanguageModified = false;
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        private void ValidateAlterInputs()
        {
            if (this.State == SqlSmoState.Creating)
            {
                return;
            }

            UserType uType = (UserType)GetPropValueOptional("UserType", UserType.SqlUser); //SqlUser is the default.
            AuthenticationType aType = AuthenticationType.Instance; //Login mapped user is the default authentication type.

            if (this.IsSupportedProperty("AuthenticationType"))
            {
                aType = (AuthenticationType)GetPropValueOptional("AuthenticationType", AuthenticationType.Instance); //Login mapped SqlUser is default.
            }

            if (this.IsSupportedProperty("DefaultLanguageLcid")
                && !(uType == UserType.SqlUser
                        && (aType == AuthenticationType.Database
                            || aType == AuthenticationType.Windows)
                    )
                )
            {
                this.ValidateDefaultLanguageNotDirty();
            }
            else
            {
                if (this.IsSupportedProperty("DefaultLanguageLcid"))
                {
                    this.DefaultLanguage.VerifyBothLcidAndNameNotDirty(true);
                }
            }
        }

        private void ValidateDefaultLanguageNotDirty()
        {
            Property defaultLanguageLcid = this.GetPropertyOptional("DefaultLanguageLcid");
            Property defaultLanguageName = this.GetPropertyOptional("DefaultLanguageName");

            //All values are valid in user's default langauge.
            if (defaultLanguageLcid.Dirty
                || defaultLanguageName.Dirty)
            {
                throw new SmoException(ExceptionTemplates.DefaultLanguageOnlyForDatabaseAuthenticatedUser);
            }
        }

    internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            ValidateVersionAndEngineTypeForScripting(sp);

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90
                && this.Parent.Parent.ConnectionContext.ServerVersion.Major >= 9)
            {
                ValidateAlterInputs();

                StringBuilder sbOption = new StringBuilder();
                bool optionAdded = false;

                Property p = this.GetPropertyOptional("DefaultSchema");
                if (!p.IsNull && p.Dirty
                    && p.Value.ToString().Length > 0)
                {
                    AddComma(sbOption, ref optionAdded);
                    sbOption.Append("DEFAULT_SCHEMA=");
                    sbOption.Append(MakeSqlBraket(p.Value.ToString()));
                }
                else
                {
                    LoginType lType = GetPropValueOptional("LoginType", LoginType.SqlLogin);
                    // Windows group are supported for NULL setting of default schema for version > 11
                    if (lType == LoginType.WindowsGroup && this.ServerVersion.Major >= 11)
                    {
                        query.Add(string.Format(SmoApplication.DefaultCulture,
                                    "ALTER USER {0} WITH DEFAULT_SCHEMA=NULL",
                                    FormatFullNameForScripting(sp)));
                    }
                }

                this.AddDefaultLanguageOptionToScript(sbOption, sp, ref optionAdded);

                StringBuilder sb = new StringBuilder();

                if (sbOption.Length > 0)
                {
                    ScriptIncludeHeaders(sb, sp, UrnSuffix);

                    sb.Append("ALTER USER ");
                    sb.Append(FormatFullNameForScripting(sp));

                    sb.Append(" WITH ");
                    sb.Append(sbOption.ToString());

                    query.Add(sb.ToString());
                }
            }
        }


        public bool IsMember(string role)
        {
            CheckObjectState();
            StringBuilder filter = new StringBuilder(ParentColl.ParentInstance.Urn);
            filter.AppendFormat(SmoApplication.DefaultCulture, "/Role[@Name='{0}']/Member[@Name='{1}']", Urn.EscapeString(role), Urn.EscapeString(this.Name));

            Request req = new Request(filter.ToString());
            DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
            return (dt.Rows.Count > 0);
        }

        public StringCollection EnumRoles()
        {
            CheckObjectState();
            StringCollection roles = new StringCollection();

            StringBuilder filter = new StringBuilder(ParentColl.ParentInstance.Urn);
            filter.AppendFormat(SmoApplication.DefaultCulture, "/Role/Member[@Name='{0}']", Urn.EscapeString(this.Name));

            Request req = new Request(filter.ToString(), new String[] { });
            req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest(new String[] { "Name" }) };
            DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
            if (0 == dt.Rows.Count)
            {
                return roles;
            }

            foreach (DataRow dr in dt.Rows)
            {
                roles.Add(Convert.ToString(dr[0], SmoApplication.DefaultCulture));
            }

            return roles;
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (this.DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
            }
            return null;
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // on 8.0 and below we do not have permissions on Users
            if (sp.TargetServerVersionInternal <= SqlServerVersionInternal.Version80 ||
                this.ServerVersion.Major <= 8)
            {
                return;
            }

            base.AddScriptPermission(query, sp);
        }

               /// <summary>
        /// Add to Role script
        /// </summary>
        /// <param name="role"></param>
        /// <returns>The DDL string to add the user to the given role.</returns>
        private string ScriptAddToRole(System.String role, ScriptingPreferences sp)
        {
            if (VersionUtils.IsTargetServerVersionSQl11OrLater(sp.TargetServerVersionInternal)
                && sp.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                return string.Format(SmoApplication.DefaultCulture,
                        "ALTER ROLE {0} ADD MEMBER {1}", MakeSqlBraket(role), MakeSqlBraket(this.Name));
            }
            else
            {

            string prefix;
            //Prefix should be based on TargetServerVersion instead of this Server Version
            if (sp != null)
            {
                if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                {
                    prefix = "sys";
                }
                else
                {
                    prefix = "dbo";
                }
            }
            else
            {
                if (this.ServerVersion.Major >= 9)
                {
                    prefix = "sys";
                }
                else
                {
                    prefix = "dbo";
                }
            }
            string username;
            if (sp != null)
            {
                // Will already be in N'xyz' format
                username = this.FormatFullNameForScripting(sp, false);
            }
            else
            {
                username = MakeSqlString(this.Name);
            }

            return string.Format(SmoApplication.DefaultCulture,
                "{0}.sp_addrolemember @rolename = {1}, @membername = {2}",
                prefix, MakeSqlString(role), username);
        }
        }


        /// <summary>
        /// Add to Role.
        /// </summary>
        /// <param name="role"></param>
        public void AddToRole(System.String role)
        {
            CheckObjectState();

            if (null == role)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("role"));
            }


        StringCollection sc = new StringCollection();
        sc.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
        sc.Add(ScriptAddToRole(role, new ScriptingPreferences(this)));
        this.ExecutionManager.ExecuteNonQuery(sc);
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
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(Parent.Name)));
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER USER {0} WITH NAME={1}",
                FormatFullNameForScripting(new ScriptingPreferences()), MakeSqlBraket(newName)));
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
            string[] fields =
            {
                nameof(AsymmetricKey),
                nameof(AuthenticationType),
                nameof(Certificate),
                "DefaultLanguageLcid",
                "DefaultLanguageName",
                nameof(DefaultSchema),
                nameof(ID),
                nameof(IsSystemObject),
                nameof(Login),
                nameof(LoginType),
                nameof(UserType),
            };

            List<string> list = GetSupportedScriptFields(typeof(User.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

    }
}



// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.StateChangeEvent ("CREATE_APPLICATION_ROLE", "APPLICATION ROLE")]
    [Facets.StateChangeEvent ("ALTER_APPLICATION_ROLE", "APPLICATION ROLE")]
    [Facets.EvaluationMode (Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ApplicationRole : ScriptNameObjectBase, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IExtendedProperties, IScriptable
    {
        internal ApplicationRole(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
        base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ApplicationRole";
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if ( null == m_ExtendedProperties )
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        private SqlSecureString password;

        /// <summary>
        /// Scripts permissions for this object.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // Permissions on ApplicationRoles were introduced in SQL Server 2005 (version 9).
            // Since minimum supported version is now SQL Server 2008 (version 10), this is always supported.
            base.AddScriptPermission(query, sp);
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
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            // need to see if it is an app role, defaults to false
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                                        "ApplicationRole", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                                    sp.TargetServerVersion < SqlServerVersion.Version90 ? Scripts.INCLUDE_EXISTS_APPROLE80 : Scripts.INCLUDE_EXISTS_APPROLE90,
                                    "", FormatFullNameForScripting(sp, false)));
                sb.Append(sp.NewLine);
            }

            //if 7.0, 8.0
            if (SqlServerVersion.Version90 > sp.TargetServerVersion)
            {
                sb.Append("EXEC dbo.sp_dropapprole @rolename = " + FormatFullNameForScripting(sp, false));
            }
            else //9.0
            {
                sb.Append("DROP APPLICATION ROLE  " + FormatFullNameForScripting(sp, true));
            }

            dropQuery.Add(sb.ToString());
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
            base.CreateImpl();
        }

        private void ScriptDdl(StringBuilder sb, ScriptingPreferences sp)
        {
            // if we're creating the role and the user has not supplied a password
            // throw an exception
            if ( sp.ScriptForCreateDrop && this.password == null )
            {
                throw new PropertyNotSetException("password");
            }

            // add code that will generate the random password
            if (!sp.ScriptForCreateDrop)
            {
                SecurityUtils.ScriptPlaceholderPwd(sb);
            }

            //if 7.0, 8.0
            if ( SqlServerVersion.Version90 > sp.TargetServerVersion  )
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                    Scripts.INCLUDE_EXISTS_APPROLE80, "NOT", FormatFullNameForScripting(sp, false));
                    sb.Append(sp.NewLine);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_addapprole @rolename = {0}",
                                FormatFullNameForScripting(sp, false));

                if (!sp.ScriptForCreateDrop)
                {
                    sb.Append(", @password = @placeholderPwd");
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                    ", @password = {0}",
                                    MakeSqlString((string)password));
                }
            }
            else //9.0
            {
                if (!sp.ForDirectExecution)
                {
                    sb.Append("declare @statement nvarchar(4000)");
                    sb.Append(Globals.newline);
                }

                StringBuilder roleStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    roleStmt.AppendFormat(SmoApplication.DefaultCulture,
                                          Scripts.INCLUDE_EXISTS_APPROLE90, "NOT", FormatFullNameForScripting(sp, false));
                    roleStmt.Append(sp.NewLine);
                }

                roleStmt.AppendFormat(SmoApplication.DefaultCulture,
                                      "CREATE APPLICATION ROLE {0}", FormatFullNameForScripting(sp, true));

                StringBuilder sbOptions = new StringBuilder();
                string schema = (string)GetPropValueOptional("DefaultSchema", string.Empty);
                if ( schema.Length > 0 )
                {
                    sbOptions.AppendFormat(SmoApplication.DefaultCulture, "DEFAULT_SCHEMA = {0}, ", MakeSqlBraket(schema));
                }

                if (sp.ForDirectExecution)
                {
                    sbOptions.AppendFormat(SmoApplication.DefaultCulture, "PASSWORD = {0}", MakeSqlString((string)password));
                }

                if ( sbOptions.Length > 0 )
                {
                    roleStmt.Append(" WITH ");
                    roleStmt.Append(sbOptions.ToString());
                }

                if (sp.ForDirectExecution)
                {
                    sb.Append(roleStmt.ToString());
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                    "select @statement = N'{0}' + N'PASSWORD = N' + QUOTENAME(@placeholderPwd,'''')",
                                    SqlString(roleStmt.ToString()));
                    sb.Append(Globals.newline);
                    sb.Append("EXEC dbo.sp_executesql @statement");
                    sb.Append(Globals.newline);
                }
            }
        }

        protected override void PostCreate()
        {
            this.password = null;
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder statement = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                statement.Append(ExceptionTemplates.IncludeHeader(
                                               "ApplicationRole", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                statement.Append(sp.NewLine);
            }

            ScriptDdl(statement, sp);

            createQuery.Add(statement.ToString());
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

        public void Alter()
        {
            base.AlterImpl();
        }

        // generates the scripts for the alter action
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder roleStmt = new StringBuilder();

            Debug.Assert(this.password == null, "Cannot change password using Alter().");

            //if 7.0, 8.0
            if (SqlServerVersion.Version90 > sp.TargetServerVersion)
            {
                throw new InvalidVersionSmoOperationException(this.ServerVersion);
            }

            roleStmt.AppendFormat(SmoApplication.DefaultCulture,
                                  "ALTER APPLICATION ROLE {0}", FormatFullNameForScripting(sp, true));

            string schema = (string)GetPropValueOptional("DefaultSchema", string.Empty);
            if (schema.Length > 0)
            {
                roleStmt.AppendFormat(SmoApplication.DefaultCulture, " WITH DEFAULT_SCHEMA = {0}", MakeSqlBraket(schema));
            }

            query.Add(roleStmt.ToString());
        }

        /// <summary>
        /// Renames the ApplicationRole.
        /// </summary>
        /// <param name="newname">new name</param>
        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            // the user is responsible to put the database in single user mode on 7.0 server
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER APPLICATION ROLE {0} WITH NAME={1}",
                                          FormatFullNameForScripting(new ScriptingPreferences()), MakeSqlBraket(newName)));
        }

        /// <summary>
        /// Changes the password for the ApplicationRole.
        /// </summary>
        /// <param name="password">Password.</param>
        public void ChangePassword(System.String password)
        {
            if (password == null)
            {
                throw new FailedOperationException(ExceptionTemplates.ChangePassword,
                                                   this,
                                                   new ArgumentNullException("password"));
            }

            ChangePassword(new SqlSecureString(password));
        }

        /// <summary>
        /// Changes the password for the ApplicationRole.
        /// </summary>
        /// <param name="password">Password.</param>
        public void ChangePassword(System.Security.SecureString password)
        {
            if (null == password)
            {
                throw new FailedOperationException(ExceptionTemplates.ChangePassword,
                                                   this,
                                                   new ArgumentNullException("password"));
            }

            // ChangePassword was introduced in SQL Server 2005 (version 9).
            // Since minimum supported version is now SQL Server 2008 (version 10), this is always supported.

            try
            {
                StringCollection sc = new StringCollection();
                sc.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                sc.Add(string.Format(SmoApplication.DefaultCulture,
                                     "ALTER APPLICATION ROLE {0} WITH PASSWORD={1}",
                                     FormatFullNameForScripting(new ScriptingPreferences()),
                                     MakeSqlString((string)((SqlSecureString)password))));

                this.ExecutionManager.ExecuteNonQuery(sc);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.ChangePassword, this, e);
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major <= 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix)};
        }
    }

}


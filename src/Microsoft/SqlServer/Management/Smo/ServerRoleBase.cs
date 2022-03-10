// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.StateChangeEvent("CREATE_SERVER_ROLE", "SERVER ROLE")]
    [Facets.StateChangeEvent("ALTER_SERVER_ROLE", "SERVER ROLE")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_SERVER", "SERVER ROLE")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [SfcElementType("Role")]
    public partial class ServerRole : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable, Cmn.IRenamable, IScriptable
    {
        internal ServerRole(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Role";
            }
        }

        /// <summary>
        /// Alters the object on the server.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Scripts the object on the server.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Scripts the object on the server on the basis of Scripting options.
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        /// <param name="newname"></param>
        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Drops the object on the server.
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
        /// Generates the Alter Script for Flexible Server Role.
        /// </summary>
        /// <param name="alterQuery"></param>
        /// <param name="so"></param>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            ThrowIfSourceOrDestBelowVersion110(sp.TargetServerVersionInternal, ExceptionTemplates.CreateAlterNotSupported);

            Property prop = this.Properties.Get("Owner");

            if (prop.Dirty && string.IsNullOrEmpty(prop.Value as string))
            {
                throw new FailedOperationException(ExceptionTemplates.ServerRoleOwnerNameEmpty);
            }

            ScriptChangeOwner(alterQuery, sp);
        }
                
        /// <summary>
        /// Generates the Rename Script for Server Role.
        /// </summary>
        /// <param name="renameQuery"></param>
        /// <param name="so"></param>
        /// <param name="newName"></param>
        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            ThrowIfSourceOrDestBelowVersion110(sp.TargetServerVersionInternal, ExceptionTemplates.CreateAlterNotSupported);

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER SERVER ROLE {0} WITH NAME={1}",
                                          FormatFullNameForScripting(sp), MakeSqlBraket(newName)));
        }        

        /// <summary>
        /// Generates the Drop Script for Server Role.
        /// </summary>
        /// <param name="dropQuery"></param>
        /// <param name="so"></param>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            ThrowIfSourceOrDestBelowVersion110(sp.TargetServerVersionInternal, ExceptionTemplates.CreateAlterNotSupported);
            
            //Dropping role members
            StringBuilder strBuilder = new StringBuilder(string.Format(SmoApplication.DefaultCulture, Scripts.DECLARE_ROLE_MEMEBER, EscapeString(this.Name, '\'')));

            // Check if exists check should be included
            if (sp.IncludeScripts.ExistenceCheck)
            {
                strBuilder.Append(Scripts.INCLUDE_EXISTS_SERVER_ROLE_MEMBERS);
            }

            //Check if not a Fixed role or 'public'
            strBuilder.Append(Scripts.IS_SERVER_ROLE_FIXED_OR_PUBLIC);


            // Add the actual drop script
            strBuilder.Append(Scripts.DROP_SERVER_ROLE_MEMBERS);
            
            if (sp.IncludeScripts.Header)
            {
                strBuilder.Append(ExceptionTemplates.IncludeHeader(
                    "ServerRole", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                strBuilder.Append(sp.NewLine);
            }
            // Check if exists check should be included
            if (sp.IncludeScripts.ExistenceCheck)
            {
                strBuilder.Append(string.Format(SmoApplication.DefaultCulture,
                                                        Scripts.INCLUDE_EXISTS_SERVER_ROLE,
                                                        "", FormatFullNameForScripting(sp, false)));
            }
            strBuilder.AppendLine();
            strBuilder.Append("DROP SERVER ROLE " + FormatFullNameForScripting(sp, true));
            
            dropQuery.Add(strBuilder.ToString());
        }

        //
        //Create Server Role DDL:
        //  use [master]
        //  CREATE SERVER ROLE <Role_Name> [AUTHORIZATION <server_principal>]
        //
        //
        /// <summary>
        /// Generates the create script for Server Role.
        /// </summary>
        /// <param name="createQuery"></param>
        /// <param name="so"></param>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            ThrowIfSourceOrDestBelowVersion110(sp.TargetServerVersionInternal, ExceptionTemplates.CreateAlterNotSupported);

            StringBuilder statement = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                statement.Append(ExceptionTemplates.IncludeHeader(
                    "ServerRole", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                statement.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_SERVER_ROLE,
                    "NOT", FormatFullNameForScripting(sp, false));
                statement.Append(sp.NewLine);
            }

            statement.Append("CREATE SERVER ROLE " + FormatFullNameForScripting(sp, true));

            Property ownerProp = Properties.Get("Owner");

            if (ownerProp.Dirty 
                && (ownerProp.Value as string) == string.Empty) //Owner property can't be set to null in any case by the user.
                                                                //Logic of not check-ing this to null is utilized internally in ServerRoleExtender
                                                                //in OwnerForUI property.
            {
                throw new FailedOperationException(ExceptionTemplates.ServerRoleOwnerNameEmpty);
            }

            if (ownerProp.Value != null 
                && (ownerProp.Dirty || sp.IncludeScripts.Owner))
            {
                statement.Append(" AUTHORIZATION " + MakeSqlBraket(ownerProp.Value.ToString()));
            }

            createQuery.Add(statement.ToString());            
        }

        internal override void ScriptAssociations(StringCollection rolesCmd, ScriptingPreferences sp)
        {
            ThrowIfSourceOrDestBelowVersion110(sp.TargetServerVersionInternal);

            // add server roles membership for those 
            // that this server role is a member of

            StringCollection roles = this.EnumServerRoleMemberships();
            foreach (string role in roles)
            {
                rolesCmd.Add(ScriptAddMembershipToRole(role));
            }
        }

        /// <summary>
        /// Script to add this role's membership to the given server role.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        private string ScriptAddMembershipToRole(string role)
        {
            ThrowIfBelowVersion110();

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(SmoApplication.DefaultCulture,
                    "ALTER SERVER ROLE {0} ADD MEMBER {1}", MakeSqlBraket(role), MakeSqlBraket(this.Name));

            return sb.ToString();
        }

        /// <summary>
        /// Script to drop this role's membership from the given server role.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        private string ScriptDropMembershipFromRole(string role)
        {
            ThrowIfBelowVersion110();

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(SmoApplication.DefaultCulture,
                    "ALTER SERVER ROLE {0} DROP MEMBER {1}", MakeSqlBraket(role), MakeSqlBraket(this.Name));
            
            return sb.ToString();
        }

        /// <summary>
        /// Enumerates Server Roles which has this server role as their member
        /// </summary>
        /// <returns></returns>
        public StringCollection EnumContainingRoleNames()
        {
            StringCollection roles = new StringCollection();

            try
            {
                ThrowIfBelowVersion110();
                CheckObjectState();
            
                //IComparer cmp = this.Parent.GetStringComparer(this.Parent.Collation);

                //Although we should use the upper comment way to find out if role is public role or not.
                //But presently, this.Parent.Parent creates an LPU problem, hence using public's ID instead.

                if (this.IsFixedRole 
                    || /*(0 == cmp.Compare(this.Name, "public"))*/this.ID == 2) //public role's ID is 2
                {
                    return roles; //Fixed roles and public role can't be member of other roles.
                }

                StringBuilder filter = new StringBuilder(this.Urn.Parent);
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
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumContainingRoles, this, e);
            }

            return roles;
        }


        /// <summary>
        /// Adds server principal with name "memberName" to member collection to this server role.
        /// </summary>
        /// <param name="memberName"></param>
        public void AddMember(string memberName)
        {
            try
            {
                CheckObjectState();
                if (null == memberName)
                {
                    throw new ArgumentNullException("memberName");
                }

                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();

                if (VersionUtils.IsSql11OrLater(ServerVersion))
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                        "ALTER SERVER ROLE {0} ADD MEMBER {1}", MakeSqlBraket(this.Name), MakeSqlBraket(memberName));
                }
                else
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC master..sp_addsrvrolemember @loginame = N'{0}', @rolename = N'{1}'",
                                SqlString(memberName), SqlString(this.InternalName));
                }

                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddMember, this, e);
            }
        }

        /// <summary>
        /// Drops server principal with name "memberName" from member collection of this server role.
        /// </summary>
        /// <param name="memberName"></param>
        public void DropMember(string memberName)
        {
            try
            {
                CheckObjectState();
                if (null == memberName)
                {
                    throw new ArgumentNullException("memberName");
                }

                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();

                if (VersionUtils.IsSql11OrLater(ServerVersion))
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                        "ALTER SERVER ROLE {0} DROP MEMBER {1}", MakeSqlBraket(this.Name), MakeSqlBraket(memberName));
                }
                else
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC master..sp_dropsrvrolemember @loginame = N'{0}', @rolename = N'{1}'",
                            SqlString(memberName), SqlString(this.InternalName));
                }

                query.Add(statement.ToString());
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropMember, this, e);
            }
        }

        /// <summary>
        /// This method will be removed in a future version of SMO. 
        /// This method returns a StringColleciton object that enumerates names of 
        /// the members of a server role. 
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public StringCollection EnumServerRoleMembers()
        {
            return EnumMemberNames();
        }

        /// <summary>
        /// This method returns a StringColleciton object that enumerates names of 
        /// the members of a server role. 
        /// </summary>
        /// <returns></returns>
        public StringCollection EnumMemberNames()
        {
            CheckObjectState();

            try
            {
                StringCollection members = new StringCollection();

                Request req = new Request(this.Urn + "/Member");
                foreach (DataRow dr in this.ExecutionManager.GetEnumeratorData(req).Rows)
                {
                    members.Add(Convert.ToString(dr["Name"], SmoApplication.DefaultCulture));
                }

                return members;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumMembers, this, e);
            }

        }

        /// <summary>
        /// This method returns a StringColleciton object that enumerates names of 
        /// the memberships of the server role. 
        /// </summary>
        /// <returns></returns>
        internal StringCollection EnumServerRoleMemberships()
        {
            CheckObjectState();

            try
            {
                StringCollection membershipcol = new StringCollection();

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
                        membershipcol.Add((String)dr["Name"]);
                    }
                }

                return membershipcol;
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumMembers, this, e);
            }

        }

        /// <summary>
        /// Adds membership of this server role to other server role named "roleName".
        /// </summary>
        /// <param name="roleName"></param>
        public void AddMembershipToRole(string roleName)
        {
            ThrowIfBelowVersion110();
            CheckObjectState();

            try
            {
                StringCollection query = new StringCollection();
                query.Add(ScriptAddMembershipToRole(roleName));
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddMember, this, e);
            }
        }

        /// <summary>
        /// Drops membership of this server role from other server role named "roleName".
        /// </summary>
        /// <param name="roleName"></param>
        public void DropMembershipFromRole(string roleName)
        {
            ThrowIfBelowVersion110();
            CheckObjectState();

            try
            {
                StringCollection query = new StringCollection();
                query.Add(ScriptDropMembershipFromRole(roleName));
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropMember, this, e);
            }
        }

        /// <summary>
        /// This method will be removed in a future version of SMO.
        /// This method works correctly for only Fixed Server Roles, not for Flexible Server Roles.
        /// This method returns a DataTable object that enumerates the statement 
        /// execution permissions of this role.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes")]
        [Obsolete]
        public DataTable EnumServerRolePermissions()
        {
            try
            {
                CheckObjectState();

                if (!this.IsFixedRole) //public is also not a fixed role. sys.server_principals catalog returns 0 in is_fixed_role column for "public" role.
                {
                    return new DataTable();
                }

                string permQuery = string.Format(SmoApplication.DefaultCulture, "EXEC master..sp_srvrolepermission @srvrolename = N'{0}'",
                                    SqlString(this.Name));
                DataSet ds = ExecutionManager.ExecuteWithResults(permQuery);
                if (null == ds || null == ds.Tables || ds.Tables.Count == 0)
                {
                    // return empty data table in case the query has no results
                    return new DataTable();
                }
                else
                {
                    return ds.Tables[0];
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPermissions, this, e);
            }

        }

        ///<summary>
        /// Retrieves ProxyAccounts associated with server role
        ///</summary>
        public DataTable EnumAgentProxyAccounts()
        {
            try
            {
                StringCollection query = new StringCollection();
                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_enum_login_for_proxy @name={0}", MakeSqlBraket(this.Name));
                query.Add(statement.ToString());

                return this.ExecutionManager.ExecuteWithResults(query).Tables[0];
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumAgentProxyAccounts, this, e);
            }
        }
    }
}



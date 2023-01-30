// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.StateChangeEvent ("CREATE_ROLE", "ROLE")]
    [Facets.StateChangeEvent ("ALTER_ROLE", "ROLE")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "ROLE")] // For Owner
    [Facets.EvaluationMode (Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [SfcElementType("Role")]
    public partial class DatabaseRole : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IAlterable, Cmn.IRenamable, IExtendedProperties, IScriptable
	{
        internal DatabaseRole(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
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

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
		public ExtendedPropertyCollection ExtendedProperties
		{
			get 
			{
				ThrowIfBelowVersion80();
				CheckObjectState();
				if( null == m_ExtendedProperties )
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
            StringBuilder strBuilder = new StringBuilder(string.Format(SmoApplication.DefaultCulture, Scripts.DECLARE_ROLE_MEMEBER, EscapeString(this.Name, '\'')));

            // Check if exists check should be included
            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                strBuilder.Append((sp.TargetServerVersion >= SqlServerVersion.Version90) ? Scripts.INCLUDE_EXISTS_ROLE_MEMBERS90 : Scripts.INCLUDE_EXISTS_ROLE_MEMBERS80);
            }

            //Check if not a Fixed role or 'public'
            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                strBuilder.Append(Scripts.IS_DBROLE_FIXED_OR_PUBLIC_90);
            }

            bool isSqlDw = this.Parent.GetPropValueOptional("IsSqlDw", false);

            if (isSqlDw)
            {
                strBuilder.AppendFormat(Scripts.DROP_DATABASEROLE_MEMBERS_DW, Guid.NewGuid().ToString("N"));
            }
            else
            {
                // Add the actual drop script
                strBuilder.Append(
                    VersionUtils.IsTargetServerVersionSQl11OrLater(sp.TargetServerVersionInternal)
                    ? Scripts.DROP_DATABASEROLE_MEMBERS_110
                    : (sp.TargetServerVersion >= SqlServerVersion.Version90)
                        ? Scripts.DROP_DATABASEROLE_MEMBERS_90
                        : Scripts.DROP_DATABASEROLE_MEMBERS_80
                    );
            }

            if (sp.IncludeScripts.Header)
            {
                strBuilder.Append(ExceptionTemplates.IncludeHeader(
                    "DatabaseRole", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                strBuilder.Append(sp.NewLine);
            }


            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {

                strBuilder.Append(string.Format(SmoApplication.DefaultCulture,
                    sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90 ? Scripts.INCLUDE_EXISTS_DBROLE80 : Scripts.INCLUDE_EXISTS_DBROLE90,
                                                        "", FormatFullNameForScripting(sp, false)));
                strBuilder.Append(sp.NewLine);
            }


            //if 7.0, 8.0
            if (SqlServerVersionInternal.Version90 > sp.TargetServerVersionInternal)
            {
                strBuilder.Append("EXEC dbo.sp_droprole @rolename = " + FormatFullNameForScripting(sp, false));
            }
            else // > 9.0
            {
                strBuilder.Append("DROP ROLE " +
                    ((sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty) +
                    FormatFullNameForScripting(sp, true));
            }

            dropQuery.Add(strBuilder.ToString());
        }


        public void Create()
        {
            base.CreateImpl();
        }


        private void CreateDdl(StringBuilder sb, ScriptingPreferences sp, string owner)
        {
            //if 7.0, 8.0
            if (SqlServerVersionInternal.Version90 > sp.TargetServerVersionInternal)
            {
                sb.Append("EXEC dbo.sp_addrole @rolename = " + FormatFullNameForScripting(sp, false));
                if (sp.IncludeScripts.Owner)
                {
                    if (null != owner && owner.Length > 0)
                    {
                        sb.Append(", @ownername = " + MakeSqlString(owner));
                    } 
                }
            }
            else //9.0
            {
                sb.Append("CREATE ROLE " + FormatFullNameForScripting(sp, true));
                if (sp.IncludeScripts.Owner)
                {
                    if (null != owner && owner.Length > 0)
                    {
                        sb.Append(" AUTHORIZATION " + MakeSqlBraket(owner));
                    } 
                }
            }
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            if(0 == String.Compare(this.Name, "public", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            StringBuilder statement = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                statement.Append(ExceptionTemplates.IncludeHeader(
                    "DatabaseRole", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                statement.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture,
                    sp.TargetServerVersionInternal < SqlServerVersionInternal.Version90 ? Scripts.INCLUDE_EXISTS_DBROLE80 : Scripts.INCLUDE_EXISTS_DBROLE90,
                    "NOT", FormatFullNameForScripting(sp, false));
                statement.Append(sp.NewLine);
            }

            CreateDdl(statement, sp, (string)Properties.Get("Owner").Value);

            createQuery.Add(statement.ToString());

            if (!sp.ScriptForCreateDrop && sp.IncludeScripts.Associations && !this.IsDesignMode)
            {
                ScriptAssociations(createQuery, sp);
            }

		}

        internal override void ScriptAssociations(StringCollection createQuery, ScriptingPreferences sp)
        {
            // Add database role membership for each role this database role is a member of
            StringCollection roles = EnumRoles();
            foreach (string role in roles)
            {
                createQuery.Add(ScriptAddToRole(role, sp));
            }
        }


        public void AddMember(string name)
        {
            try
            {
                CheckObjectState();

                if (null == name)
                {
                    throw new ArgumentNullException("name");
                }

                if (CompareAccToDbCollation(this.Name, "public"))
                {
                    return;
                }

                StringCollection query = new StringCollection();
                query.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));

                if (VersionUtils.IsSql11OrLater(ServerVersion)
                    && DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture,
                        "ALTER ROLE {0} ADD MEMBER {1}", MakeSqlBraket(this.InternalName), MakeSqlBraket(name)));
                }
                else
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SP_ADDDBROLEMEMBER,
                        SqlString(this.InternalName), SqlString(name)));
                }
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddMember, this, e);
            }



        }

        public void DropMember(string name)
        {
            try
            {
                CheckObjectState();
                if (null == name)
                {
                    throw new ArgumentNullException("name");
                }

                if (CompareAccToDbCollation(this.Name, "public"))
                {
                    return;
                }

                StringCollection query = new StringCollection();
                query.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));

                if (VersionUtils.IsSql11OrLater(ServerVersion))
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture,
                        "ALTER ROLE {0} DROP MEMBER {1}", MakeSqlBraket(this.InternalName), MakeSqlBraket(name)));
                }
                else
                {
                    query.Add(string.Format(SmoApplication.DefaultCulture, Scripts.SP_DROPDBROLEMEMBER,
                        SqlString(this.InternalName), SqlString(name)));
                }
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropMember, this, e);
            }
        }

		
		public StringCollection EnumMembers()
		{
			try
			{
				CheckObjectState();
				
				StringCollection memberList = new StringCollection();
				Request req = new Request(this.Urn + "/Member");
				foreach(DataRow dr in this.ExecutionManager.GetEnumeratorData(req).Rows)
				{
					memberList.Add(Convert.ToString(dr["Name"], SmoApplication.DefaultCulture));
				}

				return memberList;
			}
			catch(Exception e)
			{
				SqlSmoObject.FilterException(e);
                                throw new FailedOperationException(ExceptionTemplates.EnumMembers, this, e);
                        }
                 }

        /// <summary>
        /// Script string to add this role to the given database role.
        /// </summary>
        /// <param name="role">The database role to add this current database role to.</param>
        /// <returns>The DDL string to add this database role to the given database role.</returns>
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
                string myrolename;
                if (sp != null)
                {
                    // Will already be in N'xyz' format
                    myrolename = this.FormatFullNameForScripting(sp, false);
                }
                else
                {
                    myrolename = MakeSqlString(this.Name);
                }

                string prefix;

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

                return string.Format(SmoApplication.DefaultCulture,
                    "EXEC {0}.sp_addrolemember @rolename = {1}, @membername = {2}",
                    prefix, MakeSqlString(role), myrolename);
            }
        }


        /// <summary>
        /// Add this role to the given database role.
        /// This is currently private since it was not already in the public interface but should be someday.
        /// </summary>
        /// <param name="role">The database role to add this current database role to.</param>
        private void AddToRole(System.String role)
        {
            CheckObjectState();

            if (null == role)
            {
                return;
            }

            StringCollection sc = new StringCollection();
            sc.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            sc.Add(ScriptAddToRole(role, null));
            this.ExecutionManager.ExecuteNonQuery(sc);
        }

        private bool CompareAccToDbCollation(object obj1, object obj2)
        {
            System.Collections.IComparer cmp = this.Parent.GetComparerFromCollation(this.Parent.Collation);
            if (0 == cmp.Compare(obj1, obj2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return the list of database roles the given database role is a member of.
        /// </summary>
        /// <returns></returns>
        public StringCollection EnumRoles()
        {
            CheckObjectState();
            StringCollection roles = new StringCollection();

            // NOTE: The Enumerator clauses in DatabaseRole.xml do not work as of CTP1 of Yukon SP2. SO we perform the 
            // equivalent direct T-SQL for Yukon and Shiloh- herein. This shuld be moved to a urn query that looks like this:
            //   /Server[@Name='x']/Database[@Name='y']/Role/DatabaseRoleMember[@Name='z']
#if USE_ENUMERATOR_BROKEN
            StringBuilder filter = new StringBuilder(ParentColl.ParentInstance.Urn);
            filter.AppendFormat(SmoApplication.DefaultCulture, "/Role/Member[@Name='{0}']", Urn.EscapeString(this.Name));

            Request req = new Request(filter.ToString(), new String[] { });
            req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest(new String[] { "Name" }) };
            DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
#else
            StringCollection query = new StringCollection();
	    StringBuilder statement = new StringBuilder();

            //Context for the query should be the current database.This was a bug in singleton.Also UseDB will 
            //Work for cloud as the context is same database.
            AddDatabaseContext(query);

            if (this.ServerVersion.Major >= 9)
            {
                statement.AppendLine("SELECT p1.name FROM sys.database_role_members as r ");
                statement.AppendLine("JOIN sys.database_principals as p1 on p1.principal_id = r.role_principal_id ");
                statement.AppendLine("JOIN sys.database_principals as p2 on p2.principal_id = r.member_principal_id ");
                statement.AppendFormat(SmoApplication.DefaultCulture, "WHERE p2.name = {0}", MakeSqlString(this.Name));
            }
            else
            {
                statement.AppendLine("SELECT g.name FROM sysusers u, sysusers g, sysmembers m ");
                statement.AppendFormat(SmoApplication.DefaultCulture, 
                    "WHERE u.name = {0} AND u.uid = m.memberuid AND g.uid = m.groupuid AND u.issqlrole = 1 ", 
                    MakeSqlString(this.Name));
            }
            query.Add(statement.ToString());
            DataTable dt = this.ExecutionManager.ExecuteWithResults(query).Tables[0];
#endif

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

		public StringCollection Script()
		{
			return ScriptImpl();
		}
		
		// Script object with specific scripting optiions
		public StringCollection Script(ScriptingOptions scriptingOptions)
		{
			return ScriptImpl(scriptingOptions);
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

            this.AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER ROLE {0} WITH NAME={1}",
                FormatFullNameForScripting(new ScriptingPreferences()), MakeSqlBraket(newName)));
        }


        ///<summary>
        /// Retrieves ProxyAccounts associated with msdb role
        ///</summary>
        public DataTable EnumAgentProxyAccounts()	
        {
            StringCollection queries = new StringCollection();
            StringBuilder statement = new StringBuilder();
            statement.AppendFormat(SmoApplication.DefaultCulture, "EXEC msdb.dbo.sp_enum_login_for_proxy @name={0}", MakeSqlBraket(this.Name));
            queries.Add(statement.ToString());

            return this.ExecutionManager.ExecuteWithResults( queries ).Tables[0];
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

        [SfcProperty(SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        [SfcReference(typeof(User), "Server[@Name = '{0}']/Database[@Name = '{1}']/User[@Name = '{2}']", "Parent.Parent.ConnectionContext.TrueName", "Parent.Name", "Owner")]
        [SfcReference(typeof(DatabaseRole), "Server[@Name = '{0}']/Database[@Name = '{1}']/Role[@Name = '{2}']", "Parent.Parent.ConnectionContext.TrueName", "Parent.Name", "Owner")]
        [SfcReference(typeof(ApplicationRole), "Server[@Name = '{0}']/Database[@Name = '{1}']/ApplicationRole[@Name = '{2}']", "Parent.Parent.ConnectionContext.TrueName", "Parent.Name", "Owner")]
        [CLSCompliant(false)]
		public System.String Owner
		{
			get
			{
				return (System.String)this.Properties.GetValueWithNullReplacement("Owner");
			}

			set
			{
				ThrowIfBelowVersion90();
				Properties.SetValueWithConsistencyCheck("Owner", value);
			}
		}

		public void Alter()
		{
			base.AlterImpl();
		}


        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            ScriptChangeOwner(alterQuery, sp);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (this.DatabaseEngineType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major <= 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
            }
            return null;
            
        }

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // on 8.0 and below we do not have permissions on database roles
            if (sp.TargetServerVersionInternal <= SqlServerVersionInternal.Version80 ||
                this.ServerVersion.Major <= 8)
            {
                return;
            }

            base.AddScriptPermission(query, sp);
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
        internal static string[] GetScriptFields(Type parentType, Cmn.ServerVersion version, Cmn.DatabaseEngineType databaseEngineType, Cmn.DatabaseEngineEdition databaseEngineEdition, bool defaultTextMode)
        {
            return new string[] {   
                                        "IsFixedRole",
                                        "ID",
                                        "Owner"};
        }
    }

}



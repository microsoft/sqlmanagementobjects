// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliant(false)]
    public class ServerRoleExtender : SmoObjectExtender<ServerRole>, ISfcValidate
    {
        private DataTable serverRoleMembershipsTableData;    
        private StringCollection containingRoleNames;  //never use this directly anywhere in code, use Property ContainingRoleNames
        private ServerRoleCollection serverRoleCol;    //never use this directly anywhere in code, use Property SvrRoleCollection
        private Dictionary<string, bool> svrRoleNameHasMembershipHash; //never use this directly anywhere in code, use Property ServerRoleNameHasMembershipHash

        private DataTable roleMembersTableData;
        private Dictionary<string, bool> memberNameIsMemberHash; //never use this directly anywhere in code, use Property MemberNameIsMemberHash
        private StringCollection memberNames; //never use this directly anywhere in code, use Property MemberRoleNames
        
        private event EventHandler generalPageOnRunNow;    
        private object generalPageDataContainer;

        public ServerRoleExtender() : base() 
        {
            PopulateRequiredVariables();
        }
        public ServerRoleExtender(ServerRole serverRole) : base(serverRole) 
        {
            PopulateRequiredVariables();
        }

        [ExtendedPropertyAttribute()]
        public SqlSmoState State { get { return this.Parent.State; } }

        [ExtendedPropertyAttribute()]
        public SqlSmoObject CurrentObject { get { return this.Parent; } }

        [ExtendedPropertyAttribute()]
        public bool IsFixedRoleOrPublic
        {
            get
            {
                if (this.Parent.State == SqlSmoState.Creating)
                {
                    return false; //A Fixed Role can't be in Creating state.
                }

                //IComparer cmp = this.Parent.Parent.GetStringComparer(this.Parent.Parent.Collation);

                //return !VersionUtils.IsSql11OrLater(this.Parent.ServerVersion)
                //    || this.Parent.IsFixedRole
                //    || (0 == cmp.Compare(this.Parent.Name, "public"));

                //Although we should use the upper comment way to find out if role is public role or not.
                //But presently, this.Parent.Parent creates an LPU problem, hence using public's ID instead.

                return this.Parent.ServerVersion.Major <= 8 //In Shiloh, there was no public role.
                    || !VersionUtils.IsSql11OrLater(this.Parent.ServerVersion)
                    || this.Parent.IsFixedRole
                    || this.Parent.ID == 2; //public role's id.
            }
        }

        /// <summary>
        /// This property is used in Server Role relational engine task UI. If someone sets Owner property
        /// to string.Empty, that means Owner is dirty and is not null. At that point SMO also scripts
        /// the Authorization-part of create T-SQL. Hence, we set Owner property to 'null' in that case, just 
        /// before Create() in order to skip scripting Authorization-part of Create T-SQL.
        /// </summary>
        [ExtendedPropertyAttribute()]
        public string OwnerForUI
        {
            get
            {
                if (VersionUtils.IsSql11OrLater(this.Parent.ServerVersion))
                {
                    return this.Parent.Properties.GetValueWithNullReplacement("Owner", false, true) as string;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (VersionUtils.IsSql11OrLater(this.Parent.ServerVersion))
                {
                    this.Parent.Properties.SetValueWithConsistencyCheck("Owner", value, true);
                }
            }
        }

        [ExtendedPropertyAttribute()]
        public ServerConnection ConnectionContext { get { return this.Parent.Parent.ConnectionContext; } }

        [ExtendedPropertyAttribute()]
        public DataTable ServerRoleMembershipsTableData //UI controls binds to the datatable object directly. Setter is not useful in this case.
        {
            get
            {
                return this.serverRoleMembershipsTableData;
            }
        }

        /// <summary>
        /// Property to access the General Page's OnRunNow method.
        /// </summary>
        [ExtendedPropertyAttribute()]
        public object GeneralPageOnRunNow
        {        
            get{ return this.generalPageOnRunNow; }
            set { this.generalPageOnRunNow = (EventHandler)value; }
        }

        [ExtendedPropertyAttribute()]
        public object GeneralPageDataContainer
        {
            get { return this.generalPageDataContainer; }
            set { this.generalPageDataContainer = value; }
        }

        private void PopulateRequiredVariables()
        {
            //For Membership page
            this.serverRoleMembershipsTableData = this.ServerRoleMembershipsTableSchema;

            foreach (ServerRole svrRole in this.SvrRoleCollection)
            {
                if (svrRole.Name != this.Parent.Name // A server role can't be a member of itself.
                    && string.Compare("public",svrRole.Name,StringComparison.Ordinal) != 0 //Public role's membership can't be changed.
                    && string.Compare("sysadmin",svrRole.Name,StringComparison.Ordinal) != 0) //A server role can't be added to a sysadmin role.
                {
                    bool isFixedRole = VersionUtils.IsSql11OrLater(this.Parent.ServerVersion)
                        ? svrRole.IsFixedRole : true;

                    DataRow dr = this.serverRoleMembershipsTableData.NewRow();
                    dr["Name"] = svrRole.Name;
                    dr["HasMembership"] = this.ContainingRoleNames.Contains(svrRole.Name);
                    dr["IsFixedRole"] = isFixedRole;

                    this.serverRoleMembershipsTableData.Rows.Add(dr);
                }
            }  

            
            //For Members page
            this.roleMembersTableData = this.RoleMembersTableSchema;

            foreach (string principalName in this.MemberNames)
            {
                DataRow dr = this.roleMembersTableData.NewRow();
                dr["Name"] = principalName;

                if (this.Parent.Parent.Logins[principalName] == null)
                {
                    dr["IsLogin"] = false;
                }
                else
                {
                    dr["IsLogin"] = true;
                }

                this.roleMembersTableData.Rows.Add(dr);
            }
        }


        private DataTable ServerRoleMembershipsTableSchema
        {
            get
            {
                DataTable serverRoleMembershipsTableSchema = new DataTable("ServerRoleMembershipsTableData");
                serverRoleMembershipsTableSchema.Columns.Add(new DataColumn("HasMembership", typeof(bool)));
                serverRoleMembershipsTableSchema.Columns.Add(new DataColumn("Name", typeof(string)));
                serverRoleMembershipsTableSchema.Columns.Add(new DataColumn("IsFixedRole", typeof(bool)));
                serverRoleMembershipsTableSchema.Locale = CultureInfo.InvariantCulture;
                
                serverRoleMembershipsTableSchema.Columns["HasMembership"].AllowDBNull = false;
                serverRoleMembershipsTableSchema.Columns["HasMembership"].DefaultValue = false;
                serverRoleMembershipsTableSchema.Columns["IsFixedRole"].AllowDBNull = false;
                serverRoleMembershipsTableSchema.Columns["IsFixedRole"].DefaultValue = false; 
                
                return serverRoleMembershipsTableSchema;
            }
        }

        public void RefreshServerRoleNameHasMembershipHash()
        {
            StringCollection rolesCovered = new StringCollection();
            this.ServerRoleNameHasMembershipHash.Clear();

            foreach (DataRow dr in this.serverRoleMembershipsTableData.Rows)
            {
                string roleName = dr["Name"].ToString();
                bool hasMembership = (bool)dr["HasMembership"];
                rolesCovered.Add(roleName);

                if (this.IsMembershipChanged(roleName, hasMembership))
                {
                    this.ServerRoleNameHasMembershipHash.Add(roleName, hasMembership);
                }
            }            
        }

        private bool IsMembershipChanged(string roleName, bool hasMembership)
        {
            if ((this.ContainingRoleNames.Contains(roleName) && hasMembership)
                || (!this.ContainingRoleNames.Contains(roleName) && !hasMembership))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// For every server role name key, value = true means a new membership is added
        /// and value = false means an existing membership is dropped.
        /// </summary>
        public Dictionary<string, bool> ServerRoleNameHasMembershipHash
        {                                                
            get
            {
                if (this.svrRoleNameHasMembershipHash == null)
                {
                    this.svrRoleNameHasMembershipHash = new Dictionary<string, bool>();
                }
                return this.svrRoleNameHasMembershipHash;
            }
        }

        private ServerRoleCollection SvrRoleCollection
        {
            get
            {
                if (this.serverRoleCol == null)
                {
                    this.serverRoleCol = this.Parent.Parent.Roles;
                }
                return this.serverRoleCol;
            }
        }

        [ExtendedPropertyAttribute()]
        public StringCollection ContainingRoleNames
        {
            get
            {
                if (this.containingRoleNames == null)
                {
                    if (VersionUtils.IsSql11OrLater(this.Parent.ServerVersion) //Fixed Server Roles can't be contained by other roles and User-Defined roles require >= version 11
                        && (this.Parent.State == SqlSmoState.Existing))
                    {
                        this.containingRoleNames = this.Parent.EnumContainingRoleNames();
                    }
                    else
                    {
                        this.containingRoleNames = new StringCollection();
                    }
                }
                
                return this.containingRoleNames;
            }
        }

        [ExtendedPropertyAttribute()]
        public DataTable RoleMembersTableData
        {
            get
            {
                return this.roleMembersTableData;
            }
        }

        public void RefreshRoleMembersHash()
        {
            StringCollection principalsCovered = new StringCollection();

            this.MemberNameIsMemberHash.Clear();

            foreach (DataRow dr in this.roleMembersTableData.Rows)
            {
                string principalName = dr["Name"].ToString();
                principalsCovered.Add(principalName);

                if (!this.MemberNames.Contains(principalName)) //Originally not a member but now in the list
                {
                    this.MemberNameIsMemberHash.Add(principalName, true);
                }
            }

            foreach (string principalName in this.MemberNames) //Originally a member but now not in the list
            {
                if (!principalsCovered.Contains(principalName))
                {
                    this.MemberNameIsMemberHash.Add(principalName, false);
                }
            }
        }

        private StringCollection MemberNames
        {
            get
            {
                if (this.memberNames == null)
                {
                    this.memberNames = this.Parent.EnumMemberNames();
                }
                return this.memberNames;
            }
        }

        private DataTable RoleMembersTableSchema
        {
            get
            {
                DataTable roleMembersTableSchema = new DataTable("RoleMembersTableData");
                roleMembersTableSchema.Columns.Add(new DataColumn("Name", typeof(string)));
                roleMembersTableSchema.Columns.Add(new DataColumn("IsLogin", typeof(bool)));
                roleMembersTableSchema.Locale = CultureInfo.InvariantCulture;

                roleMembersTableSchema.Columns["IsLogin"].AllowDBNull = false;
                roleMembersTableSchema.Columns["IsLogin"].DefaultValue = false;

                return roleMembersTableSchema;
            }
        }

        /// <summary>
        /// For every member name key, value = true means a new member is added
        /// and value = false means an existing member is dropped.
        /// </summary>
        public Dictionary<string, bool> MemberNameIsMemberHash
        {
            get
            {
                if (this.memberNameIsMemberHash == null)
                {
                    this.memberNameIsMemberHash = new Dictionary<string, bool>();
                }
                return this.memberNameIsMemberHash;
            }
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            if (methodName.Equals("Create")
                && string.IsNullOrEmpty(this.Parent.Name))
            {
                return new ValidationState(ExceptionTemplates.EnterServerRoleName, "Name");
            }

            if (VersionUtils.IsSql11OrLater(this.Parent.ServerVersion))
            {
                Property ownerProp = this.Parent.Properties.Get("Owner");

                if (methodName.Equals("Alter")
                    && ownerProp.Dirty
                    && string.IsNullOrEmpty(ownerProp.Value as string))
                {
                    return new ValidationState(ExceptionTemplates.ServerRoleOwnerNameEmpty, "Owner");
                }
            }

            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }    
}

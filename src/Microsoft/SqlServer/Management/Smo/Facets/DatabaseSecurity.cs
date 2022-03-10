// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;


namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [Sfc.DisplayNameKey("IDatabaseSecurityFacet_Name")]
    [Sfc.DisplayDescriptionKey("IDatabaseSecurityFacet_Desc")]
    public interface IDatabaseSecurityFacet : Sfc.IDmfFacet
    {
        #region Interface properties
        [Sfc.DisplayNameKey("Database_TrustworthyName")]
        [Sfc.DisplayDescriptionKey("Database_TrustworthyDesc")]
        bool Trustworthy
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("IDatabaseSecurityFacet_IsOwnerSysadminName")]
        [Sfc.DisplayDescriptionKey("IDatabaseSecurityFacet_IsOwnerSysadminDesc")]
        bool IsOwnerSysadmin
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Database security adds properties from the children and some computed properties.  It inherits from the DatabaseAdapter.
    /// </summary>
    public class DatabaseSecurityAdapter : DatabaseAdapter, IDatabaseSecurityFacet
    {
        #region Constructors
        public DatabaseSecurityAdapter(Microsoft.SqlServer.Management.Smo.Database obj) 
            : base (obj)
        {
        }
        #endregion

        #region Computed Properties

        /// <summary>
        /// Returns true if the database owner is granted the sysadmin role.
        /// </summary>
        public bool IsOwnerSysadmin
        {
            get
            {
                Diagnostics.TraceHelper.Assert(this.Database.Parent != null, "Database Security Adapter database Parent is null");

                // Owner can be lost for attached database
                //
                if (String.IsNullOrEmpty (this.Database.Owner))
                {
                    return false;
                }

                bool isMember = false;
                Login login = this.Database.Parent.Logins[this.Database.Owner];

                if (null != login && login.LoginType == LoginType.SqlLogin)
                {
                    // recognized as a SQL login
                    isMember = login.IsMember("sysadmin");
                }
                else
                {
                    TraceHelper.Implies(login != null, login.LoginType == LoginType.WindowsUser, "The database owner is nether a SqlLogin nor a WindowsUser, which cannot happen according to BOL.");

                    // a windows login
                    string arguments = String.Format(CultureInfo.InvariantCulture, "'{0}', 'all'", this.Database.Owner);
                    string filter = String.Format(CultureInfo.InvariantCulture, "LOWER([privilege]) = 'admin'");
                    DataTable dt = this.Database.Parent.EnumAccountInfo(arguments,filter);
                    isMember = dt.Rows.Count > 0;
                }

                return isMember;
            }
        }
        #endregion

        #region Refresh
        public override void Refresh()
        {
            this.Database.SymmetricKeys.Refresh();
            this.Database.AsymmetricKeys.Refresh();

            Diagnostics.TraceHelper.Assert(this.Database.Parent != null, "Database Security Adapter database Parent is null");
            this.Database.Parent.Logins.Refresh();
        }
        #endregion

    }
}


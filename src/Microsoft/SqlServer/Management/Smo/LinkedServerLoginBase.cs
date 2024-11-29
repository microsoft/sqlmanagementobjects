// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Login")]
    public partial class LinkedServerLogin : NamedSmoObject, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IAlterable
    {
        internal LinkedServerLogin(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Login";
            }
        }

        private SqlSecureString remoteUserPassword = null;

        /// <summary>
        /// Name of linked server login
        /// </summary>
        [SfcKey(0)]
        [SfcReference(typeof(Login), "Server[@Name = '{0}']/Login[@Name = '{1}']", "Parent.Parent.ConnectionContext.TrueName", "Name")]
        [CLSCompliant(false)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
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

        public void Create()
        {
            base.CreateImpl();
        }

        // generates the scripts for creating the login
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder addlogquery = new StringBuilder();

            ScriptIncludeHeaders(addlogquery, sp, UrnSuffix);

            ScriptIncludeIfNotExists(addlogquery, sp, "NOT");

            addlogquery.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_addlinkedsrvlogin @rmtsrvname = N'{0}'",
                                        SqlString(this.ParentColl.ParentInstance.InternalName));

            addlogquery.Append(", @locallogin = ");

            // empty string means the argument goes as null
            if (this.Name.Length == 0)
            {
                addlogquery.Append("NULL ");
            }
            else
            {
                addlogquery.AppendFormat(SmoApplication.DefaultCulture, "N'{0}'", SqlString(this.Name));
            }

            Property prop = Properties.Get("Impersonate");
            if ((null != prop.Value) && (prop.Dirty || sp.ScriptForCreateDrop))
            {
                addlogquery.AppendFormat(SmoApplication.DefaultCulture, ", @useself = N'{0}'", SqlString(prop.Value.ToString()));
            }

            prop = Properties.Get("RemoteUser");
            if ((null != prop.Value) && (prop.Dirty || sp.ScriptForCreateDrop))
            {
                addlogquery.AppendFormat(SmoApplication.DefaultCulture, ", @rmtuser = N'{0}'", SqlString(prop.Value.ToString()));
                if (null != remoteUserPassword)
                {
                    addlogquery.AppendFormat(SmoApplication.DefaultCulture, ", @rmtpassword = N'{0}'", SqlString((string)remoteUserPassword));
                }
            }

            query.Add(addlogquery.ToString());
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
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            ScriptIncludeIfNotExists(sb, sp, string.Empty);

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_droplinkedsrvlogin @rmtsrvname = N'{0}'",
                                        SqlString(this.ParentColl.ParentInstance.InternalName));

            sb.Append(", @locallogin = ");

            // empty string means the argument goes as null
            if (this.Name.Length == 0)
            {
                sb.Append("NULL ");
            }
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "N'{0}'", SqlString(this.Name));
            }

            dropQuery.Add(sb.ToString());
        }

        private void ScriptIncludeIfNotExists(StringBuilder sb, ScriptingPreferences sp, string predicate)
        {
            if (sp.IncludeScripts.ExistenceCheck)
            {
                if (sp.TargetServerVersion >= SqlServerVersion.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                Scripts.INCLUDE_EXISTS_LINKED_SERVER_LOGIN90,
                                predicate,
                                FormatFullNameForScripting(sp, false));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                                Scripts.INCLUDE_EXISTS_LINKED_SERVER_LOGIN80,
                                predicate,
                                FormatFullNameForScripting(sp, false));
                }
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        // alter is the same as create, because the same stored proc is invoked
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            ScriptCreate(alterQuery, sp);
        }

        public void SetRemotePassword(string password)
        {
            remoteUserPassword = password != null ? new SqlSecureString(password) : null;
            Properties.Get("Impersonate").SetDirty(true);
            Properties.Get("RemoteUser").SetDirty(true);
        }

        public void SetRemotePassword(System.Security.SecureString password)
        {
            remoteUserPassword = password;
            Properties.Get("Impersonate").SetDirty(true);
            Properties.Get("RemoteUser").SetDirty(true);
        }
    }
}



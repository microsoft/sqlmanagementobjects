// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ServerProxyAccount : SqlSmoObject, Cmn.IAlterable
    {
        internal ServerProxyAccount()
        { }

        internal ServerProxyAccount(Server parent, ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
            singletonParent = parent;
            SetServerObject( parent.GetServerObject());
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Server;
            }
        }

        private SqlSecureString m_password;

        /// <summary>
        /// Sets the  Password that is used by the proxy
        /// </summary>
        public void SetPassword(System.String password)
        {
            if (null == password)
            {
                throw new FailedOperationException(ExceptionTemplates.SetPassword,
                                                   this,
                                                   new ArgumentNullException("password"));
            }

            SetPassword(new SqlSecureString(password));
        }

        /// <summary>
        /// Sets the  Password that is used by the proxy
        /// </summary>
        public void SetPassword(System.Security.SecureString password)
        {
            if (null == password)
            {
                throw new FailedOperationException(ExceptionTemplates.SetPassword,
                                                   this,
                                                   new ArgumentNullException("password"));
            }

            m_password = password;
        }


        /// <summary>
        /// Sets the  Account and Password that is used by the proxy
        /// </summary>
        public void SetAccount(System.String windowsAccount, System.String password)
        {
            if (null == windowsAccount)
            {
                throw new FailedOperationException(ExceptionTemplates.SetHostLoginAccount,
                                                   this,
                                                   new ArgumentNullException("windowsAccount"));
            }

            if (null == password)
            {
                throw new FailedOperationException(ExceptionTemplates.SetHostLoginAccount,
                                                   this,
                                                   new ArgumentNullException("password"));
            }

            SetAccount(windowsAccount, new SqlSecureString(password));
        }

        /// <summary>
        /// Sets the  Account and Password that is used by the proxy
        /// </summary>
        public void SetAccount(System.String windowsAccount, System.Security.SecureString password)
        {
            if (null == windowsAccount)
            {
                throw new FailedOperationException(ExceptionTemplates.SetHostLoginAccount,
                                                   this,
                                                   new ArgumentNullException("windowsAccount"));
            }

            if (null == password)
            {
                throw new FailedOperationException(ExceptionTemplates.SetHostLoginAccount,
                                                   this,
                                                   new ArgumentNullException("password"));
            }

            Properties.Get("WindowsAccount").Value = windowsAccount;
            SetPassword(password);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ServerProxyAccount";
            }
        }

        internal static void ParseAccountName(string accountName, StringBuilder domainName, StringBuilder userName)
        {
            string[] res = accountName.Split(new char[] { '\\' });
            if (res.Length == 2)
            {
                domainName.Append(res[0]);
                userName.Append(res[1]);
            }
            else if (res.Length == 1)
            {
                userName.Append(res[0]);
            }
            else
            {
                throw new SmoException(ExceptionTemplates.InvalidAcctName);
            }
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            ThrowIfAboveVersion80();

            StringBuilder alterQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            StringBuilder domainName = new StringBuilder();
            StringBuilder userName = new StringBuilder();

            bool bIsEnabled = (bool)GetPropValue("IsEnabled");

            if (bIsEnabled)
            {

                ParseAccountName(Properties["WindowsAccount"].Value as System.String, domainName, userName);

                // Eventually (after B-2) Yukon implementation of the function will change to call a different proc
                alterQuery.AppendFormat(SmoApplication.DefaultCulture,
                                        "EXEC master.dbo.xp_sqlagent_proxy_account N'SET', N'{0}', N'{1}', N'{2}'",
                                        SqlString(domainName.ToString()),
                                        SqlString(userName.ToString()),
                                        SqlString((string)m_password));

                alterQuery.Append(Globals.newline);
                alterQuery.Append("EXEC msdb.dbo.sp_set_sqlagent_properties @sysadmin_only=0");
            }
            else
            {
                //disable proxy account
                //Calling this SP will automatically clear the account from LSA.
                alterQuery.Append("EXEC msdb.dbo.sp_set_sqlagent_properties @sysadmin_only=1");
            }

            query.Add(alterQuery.ToString());
        }

        /// <summary>
        /// Alters object
        /// </summary>
        /// <returns></returns>
        public void Alter()
        {
            if (ServerVersion.Major <= 8)
            {
                base.AlterImpl();
            }
            else
            {
                const string specialCredentialName = "##xp_cmdshell_proxy_account##";

                Credential c = Parent.Credentials[specialCredentialName];

                bool bIsEnabled = (bool)GetPropValue("IsEnabled");

                if (bIsEnabled)
                {
                    string windowsAccout = (string)Properties["WindowsAccount"].Value;

                    if (c != null)
                    {
                        c.Alter(windowsAccout, m_password);
                    }
                    else
                    {
                        Credential c1 = new Credential(Parent, specialCredentialName);
                        c1.Create(windowsAccout, m_password);

                    }

                }
                else
                {
                    if (c != null)
                    {
                        c.Drop();
                    }
                }
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }
    }
}


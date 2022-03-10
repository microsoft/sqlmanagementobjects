// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class Login : ServerOwnedObject<Smo.Login>, ILogin
    {
        private readonly LoginType m_loginType;
        private ICredential m_credential;
        private IDatabase m_defaultDatabase;
        private bool m_credentialSet;
        private bool m_defaultDatabaseSet;

        private Login(Smo.Login smoMetadataObject, Server parent,LoginType loginType)
            : base(smoMetadataObject, parent)
        {
            this.m_loginType = loginType;
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return this.m_smoMetadataObject.IsSystemObject; }
        }

        public override T Accept<T>(IServerOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        public static Login CreateLogin(Smo.Login smoMetadataObject, Server parent)
        {
            Debug.Assert(smoMetadataObject != null, "SmoMetadataProvider Assert", "smoMetadataObject != null");

            switch (smoMetadataObject.LoginType)
            {
                case Microsoft.SqlServer.Management.Smo.LoginType.AsymmetricKey:
                    return new AsymmetricKeyLogin(smoMetadataObject, parent);
                case Microsoft.SqlServer.Management.Smo.LoginType.Certificate:
                    return new CertificateLogin(smoMetadataObject, parent);
                case Microsoft.SqlServer.Management.Smo.LoginType.SqlLogin:
                    return new SqlLogin(smoMetadataObject, parent);
                case Microsoft.SqlServer.Management.Smo.LoginType.WindowsGroup:
                    return new WindowsLogin(smoMetadataObject, parent);
                case Microsoft.SqlServer.Management.Smo.LoginType.WindowsUser:
                    return new WindowsLogin(smoMetadataObject, parent);
                case Microsoft.SqlServer.Management.Smo.LoginType.ExternalGroup:
                    return new ExternalLogin(smoMetadataObject, parent);
                case Microsoft.SqlServer.Management.Smo.LoginType.ExternalUser:
                    return new ExternalLogin(smoMetadataObject, parent);
                default:
                    Debug.Fail("SmoMetadataProvider Assert", "unexpected login type: " + smoMetadataObject.LoginType);
                    return null;
            }
        }

        #region ILogin Interface
        
        public LoginType LoginType
        {
            get { return this.m_loginType; }
        }

        public abstract IAsymmetricKey AsymmetricKey {get;}

        public abstract ICertificate Certificate { get; }

        public abstract IPassword Password { get; }
        
        public ICredential Credential
        {
            get 
            {
                if (!this.m_credentialSet)
                {
                    string credentialName = null;
                    Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "Credential", out credentialName);

                    if (!String.IsNullOrEmpty(credentialName))
                    {
                        this.m_credential = this.Server.Credentials[credentialName];
                        Debug.Assert(this.m_credential != null, "SmoMetadataProvider Assert", "this.m_credential != null");
                    }
                    this.m_credentialSet = true;
                }
                return this.m_credential;
            }
        }

        public IDatabase DefaultDatabase
        {
            get
            {
                if (!this.m_defaultDatabaseSet)
                {
                    string databaseName = this.m_smoMetadataObject.DefaultDatabase;
                    
                    if (!String.IsNullOrEmpty(databaseName))
                    {
                        IServer server = this.m_parent;
                        this.m_defaultDatabase = Utils.GetDatabase(server, databaseName);

                        Debug.Assert(this.m_defaultDatabase != null, "SmoMetadataProvider Assert", "this.m_defaultDatabase != null");
                    }

                    this.m_defaultDatabaseSet = true;
                }

                return this.m_defaultDatabase;
            }
        }

        public string Language
        {
            get 
            {
                return Utils.GetPropertyObject<string>(this.m_smoMetadataObject, "Language");
            }
        }

        public byte[] Sid
        {
            get
            {
                //Do not access this property if loginType is not Sql, to avoid getting exception.
                if (this.m_loginType == LoginType.Sql)
                {
                    return Utils.GetPropertyObject<byte[]>(this.m_smoMetadataObject, "Sid");
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion


        #region Private classes

        private sealed class LoginPassword : IPassword
        {
            private readonly Smo.Login m_smoMetadataObject;

            public LoginPassword(Smo.Login smoMetadataObject)
            {
                Debug.Assert(smoMetadataObject != null, "SmoMetadataProvider Assert", "smoMetadataObject != null");
                this.m_smoMetadataObject = smoMetadataObject;
            }

            #region IPassword Members

            public string Value
            {
                //Unable to retrieve it from SMO, return default.
                get { return null; }
            }

            public bool IsHashed
            {
                //Unable to retrieve it from SMO, return default.
                get { return false; }
            }

            public bool MustChange
            {
                get 
                {
                    bool? mustChange;
                    Utils.TryGetPropertyValue<bool>(this.m_smoMetadataObject, "MustChangePassword", out mustChange);

                    return mustChange.GetValueOrDefault();
                }
            }

            public bool CheckPolicy
            {
                get 
                {
                    bool? checkPolicy;
                    Utils.TryGetPropertyValue<bool>(this.m_smoMetadataObject, "PasswordPolicyEnforced", out checkPolicy);

                    return checkPolicy.GetValueOrDefault();
                }
            }

            public bool CheckExpiration
            {
                get
                {
                    bool? checkExpiration;
                    Utils.TryGetPropertyValue<bool>(this.m_smoMetadataObject, "PasswordExpirationEnabled", out checkExpiration);

                    return checkExpiration.GetValueOrDefault();
                }
            }

            #endregion
        }

        private sealed class AsymmetricKeyLogin : Login
        {
            private IAsymmetricKey m_asymmetricKey;
            
            public AsymmetricKeyLogin(Smo.Login smoMetadataObject, Server parent)
            : base(smoMetadataObject, parent, LoginType.AsymmetricKey)
            {
            }

            public override IAsymmetricKey AsymmetricKey
            {
                get 
                {
                    if (this.m_asymmetricKey == null)
                    {
                        string asymKeyName = this.m_smoMetadataObject.AsymmetricKey;
                        Debug.Assert(!String.IsNullOrEmpty(asymKeyName), "SmoMetadataProvider Assert", "asymKeyName != null");
                        this.m_asymmetricKey = this.m_parent.MasterDatabase.AsymmetricKeys[asymKeyName];
                        Debug.Assert(this.m_asymmetricKey != null, "SmoMetadataProvider Assert", "this.m_asymmetricKey != null");
                    }
                    return this.m_asymmetricKey ;
                }
            }

            public override ICertificate Certificate
            {
                get { return null; }
            }

            public override IPassword Password
            {
                get { return null; }
            }
        }

        private sealed class CertificateLogin : Login
        {
            private ICertificate m_certificate;

            public CertificateLogin(Smo.Login smoMetadataObject, Server parent)
            : base(smoMetadataObject, parent, LoginType.Certificate)
            {
            }

            public override IAsymmetricKey AsymmetricKey
            {
                get { return null; }
            }

            public override ICertificate Certificate
            {
                get 
                {
                    if (this.m_certificate == null)
                    {
                        string certificateName = this.m_smoMetadataObject.Certificate;
                        Debug.Assert(!String.IsNullOrEmpty(certificateName), "SmoMetadataProvider Assert", "certificateName != null");
                        this.m_certificate = this.m_parent.MasterDatabase.Certificates[certificateName];
                        Debug.Assert(this.m_certificate != null, "SmoMetadataProvider Assert", "this.m_certificate != null");
                    }
                    return this.m_certificate;
                }
            }

            public override IPassword Password
            {
                get { return null; }
            }
        }

        private sealed class SqlLogin : Login
        {
            private IPassword m_password;

            public SqlLogin(Smo.Login smoMetadataObject, Server parent)
                : base(smoMetadataObject, parent, LoginType.Sql)
            {
            }

            public override IAsymmetricKey AsymmetricKey
            {
                get { return null; }
            }

            public override ICertificate Certificate
            {
                get { return null; }
            }

            public override IPassword Password
            {
                get 
                {
                    if (this.m_password == null)
                    {
                        this.m_password = new LoginPassword(this.m_smoMetadataObject);
                    }
                    return this.m_password; 
                }
            }
        }

        private sealed class WindowsLogin : Login
        {
            public WindowsLogin(Smo.Login smoMetadataObject, Server parent)
                : base(smoMetadataObject, parent, LoginType.Windows)
            {
            }

            public override IAsymmetricKey AsymmetricKey
            {
                get { return null; }
            }

            public override ICertificate Certificate
            {
                get { return null; }
            }

            public override IPassword Password
            {
                get { return null; }
            }
        }

        private sealed class ExternalLogin : Login
        {
            public ExternalLogin(Smo.Login smoMetadataObject, Server parent)
                : base(smoMetadataObject, parent, LoginType.External)
            {
            }

            public override IAsymmetricKey AsymmetricKey
            {
                get { return null; }
            }

            public override ICertificate Certificate
            {
                get { return null; }
            }

            public override IPassword Password
            {
                get { return null; }
            }
        }

        #endregion
    }
}

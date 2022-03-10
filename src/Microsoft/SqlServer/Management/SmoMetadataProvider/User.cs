// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using UserType = Microsoft.SqlServer.Management.SqlParser.Metadata.UserType;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class User : DatabasePrincipal<Smo.User>, IUser
    {
        private readonly UserType m_userType;

        private ISchema m_defaultSchema;
        private bool m_defaultSchemaIsSet;

        public User(Smo.User smoMetadataObject, Database parent, UserType userType)
            : base(smoMetadataObject, parent)
        {
            m_userType = userType;
        }

        public override int Id
        {
            get { return m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return m_smoMetadataObject.IsSystemObject; }
        }

        public override T Accept<T>(IDatabaseOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        public static User CreateUser(Smo.User smoMetadataObject, Database parent)
        {
            Debug.Assert(smoMetadataObject != null, "SmoMetadataProvider Assert", "smoMetadataObject != null");

            switch (smoMetadataObject.UserType)
            {
                case Smo.UserType.AsymmetricKey:
                    return new AsymmetricKeyUser(smoMetadataObject, parent);
                case Smo.UserType.Certificate:
                    return new CertificateUser(smoMetadataObject, parent);
                case Smo.UserType.External:
                    return new NoLoginUser(smoMetadataObject, parent, UserType.External);
                case Smo.UserType.NoLogin:
                    return new NoLoginUser(smoMetadataObject, parent);
                case Smo.UserType.SqlLogin:

                    AuthenticationType? authenticationType;
                    Utils.TryGetPropertyValue(smoMetadataObject, "AuthenticationType", out authenticationType);

                    if (authenticationType.HasValue &&
                        authenticationType.Value == AuthenticationType.Database)
                    {
                        return new PasswordUser(smoMetadataObject, parent);
                    }
                    return new SqlLoginUser(smoMetadataObject, parent);

                default:
                    Debug.Fail("SmoMetadataProvider Assert", "unexpected user type: " + smoMetadataObject.UserType);
                    return null;
            }
        }

        protected override IEnumerable<string> GetMemberOfRoleNames()
        {
            return m_smoMetadataObject.EnumRoles().Cast<string>();
        }

        #region IUser Interface
        public UserType UserType 
        {
            get { return m_userType; }
        }

        public abstract IAsymmetricKey AsymmetricKey { get; }

        public abstract ICertificate Certificate { get; }

        public abstract ILogin Login { get; }

        public abstract string Password { get; }

        public ISchema DefaultSchema
        {
            get
            {
                if (!m_defaultSchemaIsSet)
                {
                    Debug.Assert(m_defaultSchema == null, "SmoMetadataProvider Assert", "this.m_defaultSchema == null");

                    string defaultSchemaName;
                    Utils.TryGetPropertyObject(m_smoMetadataObject, "DefaultSchema", out defaultSchemaName);

                    if (defaultSchemaName != null)
                    {
                        m_defaultSchema = Database.Schemas[defaultSchemaName];
                    }
                    m_defaultSchemaIsSet = true;
                }

                return m_defaultSchema;
            }
        }
        #endregion

        #region Private classes

        private sealed class AsymmetricKeyUser : User
        {
            private IAsymmetricKey m_asymmetricKey;

            public AsymmetricKeyUser(Smo.User smoMetadataObject, Database parent)
                : base(smoMetadataObject, parent, UserType.AsymmetricKey)
            {
            }

            public override IAsymmetricKey AsymmetricKey
            {
                get
                {
                    if (m_asymmetricKey == null)
                    {
                        string asymKeyName = m_smoMetadataObject.AsymmetricKey;
                        Debug.Assert(!String.IsNullOrEmpty(asymKeyName), "SmoMetadataProvider Assert", "asymKeyName != null");
                        m_asymmetricKey = Database.AsymmetricKeys[asymKeyName];
                        Debug.Assert(m_asymmetricKey != null, "SmoMetadataProvider Assert", "this.m_asymmetricKey != null");
                    }
                    return m_asymmetricKey;
                }
            }

            public override ICertificate Certificate
            {
                get { return null; }
            }

            public override ILogin Login
            {
                get { return null; }
            }

            public override string Password
            {
                get { return null; }
            }
        }

        private sealed class CertificateUser : User
        {
            private ICertificate m_certificate;

            public CertificateUser(Smo.User smoMetadataObject, Database parent)
                : base(smoMetadataObject, parent, UserType.Certificate)
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
                    if (m_certificate == null)
                    {
                        string certificateName = m_smoMetadataObject.Certificate;
                        Debug.Assert(!String.IsNullOrEmpty(certificateName), "SmoMetadataProvider Assert", "certificateName != null");
                        m_certificate = Database.Certificates[certificateName];
                        Debug.Assert(m_certificate != null, "SmoMetadataProvider Assert", "this.m_certificate != null");
                    }
                    return m_certificate;
                }
            }

            public override ILogin Login
            {
                get { return null; }
            }

            public override string Password
            {
                get { return null; }
            }
        }

        private sealed class NoLoginUser : User
        {

            public NoLoginUser(Smo.User smoMetadataObject, Database parent, UserType userType = UserType.NoLogin)
                : base(smoMetadataObject, parent, userType)
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

            public override ILogin Login
            {
                get { return null; }
            }

            public override string Password
            {
                get { return null; }
            }
        }

        private sealed class SqlLoginUser : User
        {
            private ILogin m_login;
            private bool m_loginIsSet;

            public SqlLoginUser(Smo.User smoMetadataObject, Database parent)
                : base(smoMetadataObject, parent, UserType.SqlLogin)
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

            public override ILogin Login
            {
                get
                {
                    if (!m_loginIsSet)
                    {
                        Debug.Assert(m_login == null, "SmoMetadataProvider Assert", "this.m_login == null");
                        string loginName = m_smoMetadataObject.Login;
                        Debug.Assert(!String.IsNullOrEmpty(loginName), "SmoMetadataProvider Assert", "loginName != null");

                        m_login = Database.Server.Logins[loginName];
                        m_loginIsSet = true;
                    }
                    return m_login;
                }
            }

            public override string Password
            {
                get { return null; }
            }
        }

        private sealed class PasswordUser : User
        {
            public PasswordUser(Smo.User smoMetadataObject, Database parent)
                : base(smoMetadataObject, parent, UserType.Password)
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

            public override ILogin Login
            {
                get { return null; }
            }

            public override string Password
            {
                get { return null; }
            }
        }

        #endregion
    }
}

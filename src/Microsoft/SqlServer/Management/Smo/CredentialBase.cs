// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Internal;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Credential : NamedSmoObject, ICreatable, IAlterable, IDroppable, IDropIfExists
    {
        /// <summary>
        /// Constructs Credential object.
        /// </summary>
        /// <param name="parentColl">Parent collection.</param>
        /// <param name="key">Object key.</param>
        /// <param name="state">Object state.</param>
        internal Credential(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Once created the ProviderName of the credential cannot be changed
        /// </summary>
        /// <returns></returns>
        internal override string[] GetNonAlterableProperties()
        {
            return new string[] { "ProviderName", "MappedClassType" };
        }

        /// <summary>
        /// Creates the object. Implements ICreatable.Create().
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        public void Create(string identity)
        {
            Create(identity, (string)null);
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <param name="secret">Secret string, usually a password.</param>
        public void Create(string identity, string secret)
        {
            Create(identity, secret != null ? new SqlSecureString(secret) : null);
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <param name="secret">Secret string, usually a password.</param>
        public void Create(string identity, System.Security.SecureString secret)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            if (identity.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplates.EmptyInputParam("identity", "string"));
            }

            this.Identity = identity;
            this.secret = secret;

            Create();
        }

        ///<summary>
        /// Generates the scripts for creating the object.
        /// </summary>
        /// <param name="query">Collection of query lines of text.</param>
        /// <param name="so">Scription options.</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            if (sp.IncludeScripts.Header)
            {
                // need to generate commentary headers
                queries.Add(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, FullQualifiedName, DateTime.Now.ToString(GetDbCulture())));
            }

            queries.Add(CreateAlterScript(true, sp));
        }

        /// <summary>
        /// Alters the object. Implements IAlterable.Alter().
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Alters the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        public void Alter(string identity)
        {
            Alter(identity, (string)null);
        }

        /// <summary>
        /// Alters the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <param name="secret">Secret string, usually a password.</param>
        public void Alter(string identity, string secret)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            if (identity.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplates.EmptyInputParam("identity", "string"));
            }

            this.Identity = identity;
            this.secret = secret != null ? new SqlSecureString(secret) : null;

            Alter();
        }

        /// <summary>
        /// Alters the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <param name="secret">Secret string, usually a password.</param>
        public void Alter(string identity, System.Security.SecureString secret)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            if (identity.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplates.EmptyInputParam("identity", "string"));
            }

            this.Identity = identity;
            this.secret = secret;

            Alter();
        }

        /// <summary>
        /// Generates the script that alters the object
        /// </summary>
        /// <param name="query">Collection of query lines of text.</param>
        /// <param name="so">Scription options.</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            if (IsObjectDirty())
            {
                queries.Add(CreateAlterScript(false, sp));
            }
        }

        /// <summary>
        /// Drops the object. Implements IDroppable.Drop().
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
        /// Generates script that drops the object.
        /// </summary>
        /// <param name="query">Collection of query lines of text.</param>
        /// <param name="so">Scription options.</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(Scripts.INCLUDE_EXISTS_CREDENTIAL, "", this.ID);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP CREDENTIAL {0}", FullQualifiedName);

            queries.Add(sb.ToString());
        }

        /// <summary>
        /// Returns a list of login object names that reference the credential.
        /// </summary>
        /// <returns>Collection of login names.</returns>
        public StringCollection EnumLogins()
        {
            CheckObjectState();

            try
            {
                StringCollection logins = new StringCollection();

                if (this.IsDesignMode)
                {
                    foreach (Login login in this.Parent.Logins)
                    {
                        if (login.EnumCredentials().Contains(this.Name))
                        {
                            logins.Add(login.Name);
                        }
                    }
                }
                else
                {
                    DataTable dataTable;
                    if (ServerVersion.Major < 10)
                    {
                        string loginUrn = String.Format(SmoApplication.DefaultCulture,
                            this.Parent.Urn.Value + "/Login[@Credential = '{0}']", Urn.EscapeString(this.Name));
                        Request req = new Request(loginUrn, new string[] { "Name" });
                        dataTable = this.ExecutionManager.GetEnumeratorData(req);
                    }
                    else
                    {
                        string query = string.Format(SmoApplication.DefaultCulture, "select name from sys.server_principal_credentials as c join sys.server_principals as p on p.principal_id = c.principal_id where c.credential_id = {0}", this.ID.ToString());
                        dataTable = this.ExecutionManager.ExecuteWithResults(query).Tables[0];
                    }

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string name = row[0].ToString();
                        if (!logins.Contains(name))
                        {
                            logins.Add(name);
                        }
                    }
                }

                return logins;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumLogins, this, e);
            }
        }


        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "Credential";
            }
        }

        /// <summary>
        /// Generates script for creating or altering the object.
        /// </summary>
        /// <param name="create">True to generate CREATE script; false to generate ALTER script</param>
        /// <param name="so">Scripting options.</param>
        /// <returns>Script string.</returns>
        private string CreateAlterScript(bool create, ScriptingPreferences sp)
        {
            string identity = (string)this.GetPropValue("Identity");
            if (identity == null || identity.Length == 0)
            {
                throw new PropertyNotSetException("Identity");
            }

            StringBuilder builder = new StringBuilder();

            if (create)
            {
                ScriptIncludeHeaders(builder, sp, UrnSuffix);
            }

            if (create && sp.IncludeScripts.ExistenceCheck)
            {
                builder.AppendFormat(Scripts.INCLUDE_EXISTS_CREDENTIAL, "NOT", this.ID);
                builder.Append(sp.NewLine);
            }

            builder.AppendFormat(SmoApplication.DefaultCulture,
                "{0} CREDENTIAL {1} WITH IDENTITY = N'{2}'",
                create ? "CREATE" : "ALTER",
                FullQualifiedName,
                SqlString(this.Identity));

            if (this.secret != null)
            {
                builder.AppendFormat(SmoApplication.DefaultCulture,
                    ", SECRET = N'{0}'",
                    SqlString((string)this.secret));
                // null out secret after the use
                this.secret = null;
            }

            if (create && ServerVersion.Major >= 10)
            {
                Property mappedClassType = Properties.Get("MappedClassType");
                if (mappedClassType.Dirty && (MappedClassType)mappedClassType.Value == MappedClassType.CryptographicProvider)
                {
                    string providerName = (string)this.GetPropValue("ProviderName");
                    if (string.IsNullOrEmpty(providerName))
                    {
                        throw new PropertyNotSetException("ProviderName");
                    }
                    builder.AppendLine();
                    builder.AppendFormat(SmoApplication.DefaultCulture,
                        "FOR CRYPTOGRAPHIC PROVIDER {0}",
                        MakeSqlBraket(providerName));
                }
            }

            return builder.ToString();
        }

        #region Private members

        // REVIEW: Consider using SecureString when there will be a way
        // to pass secret to the server as an encrypted string.
        // For now it doesn't make much sense to use SecurityString for
        // storing the secret because it is passed to SQL as clear text
        private SqlSecureString secret;

        #endregion
    }
}


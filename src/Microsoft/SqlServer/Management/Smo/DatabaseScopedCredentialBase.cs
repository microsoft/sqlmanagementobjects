// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Internal;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{

    public partial class DatabaseScopedCredential : NamedSmoObject, ICreatable, IAlterable, IDroppable, IDropIfExists, IScriptable
    {
        /// <summary>
        /// Constructs Credential object.
        /// </summary>
        /// <param name="parentColl">Parent collection.</param>
        /// <param name="key">Object key.</param>
        /// <param name="state">Object state.</param>
        internal DatabaseScopedCredential(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
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

            if (Util.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException("identity");
            }

            Create(identity, !Util.IsNullOrWhiteSpace(secret) ? new SqlSecureString(secret) : null);
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <param name="secret">Secret string, usually a password.</param>
        public void Create(string identity, System.Security.SecureString secret)
        {

            if (Util.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException("identity");
            }

            this.Identity = identity;
            this.secret = secret;

            Create();
        }

        // Script object. Implements IScriptable.Script().
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting options. Implements IScriptable.Script(ScriptingOptions).
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        ///<summary>
        /// Generates the scripts for creating the object.
        /// </summary>
        /// <param name="queries">Collection of query lines of text.</param>
        /// <param name="sp">Scripting options.</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            queries.Add(CreateAlterScript(queries, true, sp));
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
            if (Util.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException("identity");
            }

            this.Identity = identity;
            this.secret = !Util.IsNullOrWhiteSpace(secret) ? new SqlSecureString(secret) : null;

            Alter();
        }

        /// <summary>
        /// Alters the object.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <param name="secret">Secret string, usually a password.</param>
        public void Alter(string identity, System.Security.SecureString secret)
        {
            if (Util.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentNullException("identity");
            }

            this.Identity = identity;
            this.secret = secret;

            Alter();
        }

        /// <summary>
        /// Generates the script that alters the object
        /// </summary>
        /// <param name="queries">Collection of query lines of text.</param>
        /// <param name="sp">Scripting options.</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (IsObjectDirty())
            {
                queries.Add(CreateAlterScript(queries, false, sp));
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
        /// <param name="queries">Collection of query lines of text.</param>
        /// <param name="sp">Scripting options.</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.TargetDatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(Parent.Name));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(Scripts.INCLUDE_EXISTS_DATABASESCOPEDCREDENTIAL, "", this.ID);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP DATABASE SCOPED CREDENTIAL {0}", FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }


        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "DatabaseScopedCredential";
            }
        }

        /// <summary>
        /// Generates script for creating or altering the object.
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="create">True to generate CREATE script; false to generate ALTER script</param>
        /// <param name="sp">Scripting options.</param>
        /// <returns>Script string.</returns>
        private string CreateAlterScript(StringCollection queries, bool create, ScriptingPreferences sp)
        {
            string identity = (string)this.GetPropValue("Identity");
            if (Util.IsNullOrWhiteSpace(identity))
            {
                throw new PropertyNotSetException("Identity");
            }

            StringBuilder builder = new StringBuilder();

            if (create)
            {
                ScriptIncludeHeaders(builder, sp, UrnSuffix);
            }

            if (sp.IncludeScripts.DatabaseContext)
            {
                AddDatabaseContext(queries, sp);
            }

            if (create && sp.IncludeScripts.ExistenceCheck)
            {
                builder.AppendFormat(Scripts.INCLUDE_EXISTS_DATABASESCOPEDCREDENTIAL, "NOT", this.ID);
                builder.Append(sp.NewLine);
            }

            builder.AppendFormat(SmoApplication.DefaultCulture,
                "{0} DATABASE SCOPED CREDENTIAL {1} WITH IDENTITY = N'{2}'",
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

            return builder.ToString();
        }

        #region Private members

        // REVIEW: Consider using SecureString when there will be a way
        // to pass secret to the server as an encrypted string.
        // For now it doesn't make much sense to use SecurityString for
        // storing the secret because it is passed to SQL as clear text
        private SqlSecureString secret;

        #endregion

        internal static string[] GetScriptFields(Type parentType,
            Common.ServerVersion version,
            Common.DatabaseEngineType databaseEngineType,
            Common.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            var fields = new[]
            {
                "ID",
                "Name",
                "Identity"
            };

            var list = GetSupportedScriptFields(typeof(DatabaseScopedConfiguration.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Server;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class SqlAssembly : ScriptNameObjectBase, Cmn.IAlterable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable
    {
        private string primaryAssemblyServerPath;
        // copy of overloaded Create() parameter, referenced in ScriptCreate().

        private string[] assemblyLocalPaths;
        // copy of overloaded Create() parameter, referenced in ScriptCreate();

        private AssemblyAlterOptions assemblyAlterMethod;
        // copy of overloaded Alter() parameter, referenced in ScriptAlter().

        private SqlAssemblyFileCollection sqlAssemblyFiles;
        // state variable for SqlAssemblyFiles property.

        private void init()
        {
            this.primaryAssemblyServerPath = null;
            this.assemblyLocalPaths = null;

            this.assemblyAlterMethod = AssemblyAlterOptions.None;

            this.sqlAssemblyFiles = null;
        }

        #region Constructors

        internal SqlAssembly(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            if (SqlContext.IsAvailable)
            {
                throw new Exception(ExceptionTemplates.SmoSQLCLRUnAvailable);
            }
            init();
        }

        #endregion

        #region Properties

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "SqlAssembly";
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == this.m_ExtendedProperties)
                {
                    this.m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return this.m_ExtendedProperties;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(SqlAssemblyFile))]
        public SqlAssemblyFileCollection SqlAssemblyFiles
        {
            get
            {
                CheckObjectState();
                if (null == this.sqlAssemblyFiles)
                {
                    this.sqlAssemblyFiles = new SqlAssemblyFileCollection(this);
                }
                return this.sqlAssemblyFiles;
            }
        }

        #endregion

        #region Create Methods

        // Create assembly by serializing one or more assemblies with CREATE ASSEMBLY FROM <assembly_bits>
        public void Create(string[] assemblyLocalPaths)
        {
            try
            {
                if (null == assemblyLocalPaths)
                {
                    throw new FailedOperationException(ExceptionTemplates.Create, this,
                                                    new ArgumentNullException("assemblyLocalPaths"),
                                                    ExceptionTemplates.InnerException);
                }

                if (0 == assemblyLocalPaths.Length)
                {
                    throw new FailedOperationException(ExceptionTemplates.Create, this, null,
                                            ExceptionTemplates.EmptyInputParam("assemblyLocalPaths", "Collection"));
                }

                this.assemblyLocalPaths = assemblyLocalPaths;
            base.CreateImpl();
            }
        finally
            {
                if (!this.IsDesignMode)
                {
                    this.assemblyLocalPaths = null;
                }
            }
        }

        // Create assembly using a single primary assembly filepath with CREATE ASSEMBLY FROM <client_assembly_specifier>
        public void Create(string primaryAssemblyServerPath)
        {
            try
            {
                if (null == primaryAssemblyServerPath)
                {
                    throw new FailedOperationException(ExceptionTemplates.Create, this,
                                                    new ArgumentNullException("primaryAssemblyServerPath"),
                                                    ExceptionTemplates.InnerException);
                }

                if (string.Empty == primaryAssemblyServerPath)
                {
                    throw new FailedOperationException(ExceptionTemplates.Create, this, null,
                                                    ExceptionTemplates.EmptyInputParam("primaryAssemblyServerPath", "string"));
                }

                this.primaryAssemblyServerPath = primaryAssemblyServerPath;
            base.CreateImpl();
            }
        finally
            {
                if (!this.IsDesignMode)
                {
                    this.primaryAssemblyServerPath = null;
                }
            }
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            Property property;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringCollection scAddFile = new StringCollection();

            string fullName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                if (sp.TargetServerVersion >= SqlServerVersion.Version100)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_ASSEMBLY100, "NOT", SqlString(GetName(sp)));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_ASSEMBLY, "NOT", SqlString(GetName(sp)));
                }
                sb.Append(sp.NewLine);
            }

            // CREATE ASSEMBLY <assembly_name>
            sb.AppendFormat("CREATE ASSEMBLY {0}", fullName);
            sb.Append(sp.NewLine);

            // AUTHORIZATION <owner_name>
            if (sp.IncludeScripts.Owner && (null != (property = this.Properties.Get("Owner")).Value))
            {
                sb.AppendFormat("AUTHORIZATION [{0}]", SqlBraket(property.Value.ToString()));
                sb.Append(sp.NewLine);
            }

            Debug.Assert((null != this.primaryAssemblyServerPath) || (null != this.assemblyLocalPaths) || !sp.ScriptForCreateDrop);

            // FROM <client_assembly_specifier>
            if (null != this.primaryAssemblyServerPath)
            {
                sb.AppendFormat("FROM N'{0}'", SqlString(this.primaryAssemblyServerPath));
                sb.Append(sp.NewLine);
            }

            // FROM <assembly_bits>
            else if (null != this.assemblyLocalPaths)
            {
                sb.Append("FROM 0x");
                int acount = 0;
                foreach (string path in this.assemblyLocalPaths)
                {
                    if (0 != acount++)
                    {
                        sb.Append(", 0x");
                    }

                    AppendAssemblyFile(sb, path);
                }
                sb.Append(sp.NewLine);
            }

            // FROM (SqlAssemblyFiles collection)
            else
            {
                Debug.Assert(!sp.ScriptForCreateDrop && (this.SqlAssemblyFiles.Count > 0));
                sb.Append("FROM ");
                foreach (SqlAssemblyFile saf in this.SqlAssemblyFiles)
                {
                    if (saf.ID == 1) //add main dll to 'create assembly'
                    {
                        sb.Append(saf.GetFileText());
                    }
                    else //add remainings to 'alter assembly add file'
                    {
                        StringBuilder sbAddFile = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                        sbAddFile.AppendFormat("ALTER ASSEMBLY {0}", fullName);
                        sbAddFile.Append(sp.NewLine);
                        sbAddFile.Append("ADD FILE FROM ");
                        sbAddFile.Append(saf.GetFileText());
                        sbAddFile.Append(sp.NewLine);
                        sbAddFile.AppendFormat("AS N'{0}'", SqlString(saf.Name));
                        sbAddFile.Append(sp.NewLine);
                        scAddFile.Add(sbAddFile.ToString());
                    }
                }
                sb.Append(sp.NewLine);
            }

            // WITH
            int ocount = 0;

            // PERMISSION_SET
            if (null != (property = this.Properties.Get("AssemblySecurityLevel")).Value)
            {
                if (0 == ocount++)
                {
                    sb.Append("WITH");
                }

                sb.Append(" PERMISSION_SET = ");

                switch ((AssemblySecurityLevel)property.Value)
                {
                    case AssemblySecurityLevel.Unrestricted:
                        sb.Append("UNSAFE");
                        break;

                    case AssemblySecurityLevel.External:
                        sb.Append("EXTERNAL_ACCESS");
                        break;

                    case AssemblySecurityLevel.Safe:
                        sb.Append("SAFE");
                        break;

                    default:
                        sb.Append("SAFE");
                        break;
                }
            }

            if (ocount > 0)
            {
                sb.Append(sp.NewLine);
            }

            queries.Add(sb.ToString());
            foreach (string s in scAddFile)
            {
                queries.Add(s);
            }
        }
        #endregion

        #region Drop Methods
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
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string fullName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion < SqlServerVersion.Version130)
            {

                if (sp.TargetServerVersion >= SqlServerVersion.Version100)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_ASSEMBLY100, "", SqlString(GetName(sp)));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_ASSEMBLY, "", SqlString(GetName(sp)));
                }
                sb.Append(sp.NewLine);
            }

            // DROP ASSEMBLY
            sb.AppendFormat("DROP ASSEMBLY {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                fullName);
            sb.Append(sp.NewLine);

            dropQuery.Add(sb.ToString());
        }
        #endregion

        #region Alter Methods

        public void Alter()
        {
            this.assemblyAlterMethod = AssemblyAlterOptions.None;
            base.AlterImpl();
        }

        public void Alter(AssemblyAlterOptions assemblyAlterMethod)
        {
        try	{
            this.assemblyAlterMethod = assemblyAlterMethod;
            base.AlterImpl();
            }
        finally
            {
                if (!this.IsDesignMode)
                {
                    this.assemblyAlterMethod = AssemblyAlterOptions.NoChecks;
                }
            }
        }

        public void Alter(AssemblyAlterOptions assemblyAlterMethod, string primaryAssemblyServerPath)
        {
            try
            {
                if (null == primaryAssemblyServerPath)
                {
                    throw new FailedOperationException(ExceptionTemplates.Alter, this,
                                                    new ArgumentNullException("primaryAssemblyServerPath"),
                                                    ExceptionTemplates.InnerException);
                }

                if (string.Empty == primaryAssemblyServerPath)
                {
                    throw new FailedOperationException(ExceptionTemplates.Alter, this, null,
                                                    ExceptionTemplates.EmptyInputParam("primaryAssemblyServerPath", "string"));
                }

                this.assemblyAlterMethod = assemblyAlterMethod;
                this.primaryAssemblyServerPath = primaryAssemblyServerPath;
                base.AlterImpl();
            }
            finally
        {
                if (!this.IsDesignMode)
                {
                    this.assemblyAlterMethod = AssemblyAlterOptions.NoChecks;
                    this.primaryAssemblyServerPath = null;
                }
        }
        }

        public void Alter(AssemblyAlterOptions assemblyAlterMethod, string[] assemblyLocalPaths)
        {
            try
            {
                if (null == assemblyLocalPaths)
                {
                    throw new FailedOperationException(ExceptionTemplates.Alter, this,
                                                    new ArgumentNullException("assemblyLocalPaths"),
                                                    ExceptionTemplates.InnerException);
                }

                if (0 == assemblyLocalPaths.Length)
                {
                    throw new FailedOperationException(ExceptionTemplates.Alter, this, null,
                                            ExceptionTemplates.EmptyInputParam("assemblyLocalPaths", "Collection"));
                }

                this.assemblyLocalPaths = assemblyLocalPaths;
                base.AlterImpl();
            }
            finally
        {
                if (!this.IsDesignMode)
                {
                    this.assemblyAlterMethod = AssemblyAlterOptions.NoChecks;
                    this.assemblyLocalPaths = null;
                }
        }
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (!IsObjectDirty())
            {
                return;
            }
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            Property property;
            // ALTER ASSEMBLY <assembly_name>
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sb.AppendFormat("ALTER ASSEMBLY {0}", FullQualifiedName);
            sb.Append(sp.NewLine);

            int statementLength = sb.Length;

            // FROM <client_assembly_specifier>
            if (null != this.primaryAssemblyServerPath)
            {
                sb.AppendFormat("FROM N'{0}'", SqlString(this.primaryAssemblyServerPath));
                sb.Append(sp.NewLine);
            }

            // FROM <assembly_bits>
            else if (null != this.assemblyLocalPaths)
            {
                sb.Append("FROM 0x");
                int acount = 0;
                foreach (string path in this.assemblyLocalPaths)
                {
                    if (0 != acount++)
                    {
                        sb.Append(", 0x");
                    }

                    AppendAssemblyFile(sb, path);
                }
                sb.Append(sp.NewLine);
            }

            //[ WITH alter_option [,] ]
            bool needsComma = false;
            //	| PERMISSION_SET = { SAFE | EXTERNAL_ACCESS | UNSAFE }
            if ((null != (property = this.Properties.Get("AssemblySecurityLevel")).Value) && property.Dirty)
            {
                AppendWithCommaText(sb, "PERMISSION_SET = ", ref needsComma);

                switch ((AssemblySecurityLevel)property.Value)
                {
                    case AssemblySecurityLevel.Unrestricted:
                        sb.Append("UNSAFE");
                        break;
                    case AssemblySecurityLevel.External:
                        sb.Append("EXTERNAL_ACCESS");
                        break;
                    case AssemblySecurityLevel.Safe:
                        sb.Append("SAFE");
                        break;
                    default:
                        throw new WrongPropertyValueException(this.Properties.Get("AssemblySecurityLevel"));
                }
            }

            //	| VISIBILITY = { ON | OFF }
            if ((null != (property = this.Properties.Get("IsVisible")).Value) && property.Dirty)
            {
                AppendWithCommaText(sb, "VISIBILITY = ", ref needsComma);
                sb.Append((bool)property.Value ? "ON" : "OFF");
            }

            //	| { UNCHECKED DATA }
            switch (this.assemblyAlterMethod)
            {
                case AssemblyAlterOptions.None: break;
                case AssemblyAlterOptions.NoChecks: AppendWithCommaText(sb, "UNCHECKED DATA", ref needsComma); break;
                default:
                    throw new WrongPropertyValueException(this.Properties.Get("AssemblySecurityLevel"));
            }

            if (needsComma)
            {
                sb.Append(sp.NewLine);
            }

            if (sb.Length > statementLength)
            {
                alterQuery.Add(sb.ToString());
            }

            // change owner - this is the syntax we'll use
            // ALTER AUTHORIZATION ON [<entity_type>::]<entity_name>
            // TO {SCHEMA OWNER |  principal_name }
            if ((null != (property = this.Properties.Get("Owner")).Value) && property.Dirty)
            {
                alterQuery.Add(string.Format(SmoApplication.DefaultCulture,
                            "ALTER AUTHORIZATION ON ASSEMBLY::{0} TO {1}",
                            FullQualifiedName, MakeSqlBraket(property.Value.ToString())));
            }

        }
        #endregion

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        protected override bool IsObjectDirty()
        {
            return (base.IsObjectDirty() || (null != primaryAssemblyServerPath || null != assemblyLocalPaths));
        }

        private void AppendAssemblyFile(StringBuilder sb, string assemblyLocalPath)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(assemblyLocalPath, FileMode.Open, FileAccess.Read);
                byte[] file = new byte[fs.Length];
                fs.Read(file, 0, (int)fs.Length);

                foreach (byte b in file)
                {
                    sb.Append(b.ToString("X2", SmoApplication.DefaultCulture));
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Close();
                }
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != sqlAssemblyFiles)
            {
                sqlAssemblyFiles.MarkAllDropped();
            }
        }

        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public System.Version Version
        {
            get
            {
                return new System.Version(
                    (int)this.Properties.GetValueWithNullReplacement("VersionMajor"),
                    (int)this.Properties.GetValueWithNullReplacement("VersionMinor"),
                    (int)this.Properties.GetValueWithNullReplacement("VersionBuild"),
                    (int)this.Properties.GetValueWithNullReplacement("VersionRevision"));
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">DatabaseEngineType of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[] {
                                    "Owner",
                                    "AssemblySecurityLevel",
                                    "IsSystemObject"
                                };
        }
    }
}


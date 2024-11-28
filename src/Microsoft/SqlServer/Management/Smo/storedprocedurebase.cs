// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;
// History
//
// Fix for bug 412057: SP' startup order wasn't scripted

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.StateChangeEvent("CREATE_PROCEDURE", "PROCEDURE")]
    [Facets.StateChangeEvent("ALTER_PROCEDURE", "PROCEDURE")]
    [Facets.StateChangeEvent("RENAME", "PROCEDURE")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "PROCEDURE")] // For Owner
    [Facets.StateChangeEvent("ALTER_SCHEMA", "PROCEDURE")] // For Schema
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    public partial class StoredProcedure : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.ICreateOrAlterable, Cmn.IAlterable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable,
        IExtendedProperties, IScriptable, ITextObject
    {
        internal StoredProcedure(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_Params = null;
        }

        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
        }

        private NumberedStoredProcedureCollection numberedStoredProcedureCollection = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(NumberedStoredProcedure))]
        public NumberedStoredProcedureCollection NumberedStoredProcedures
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(NumberedStoredProcedure));
                if (null == numberedStoredProcedureCollection)
                {
                    numberedStoredProcedureCollection = new NumberedStoredProcedureCollection(this);
                }
                return numberedStoredProcedureCollection;
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        private StoredProcedureParameterCollection m_Params;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(StoredProcedureParameter), SfcObjectFlags.Design | SfcObjectFlags.NaturalOrder)]
        public StoredProcedureParameterCollection Parameters
        {
            get
            {
                CheckObjectState();
                if( null == m_Params )
                {
                    m_Params = new StoredProcedureParameterCollection(this);
                    SetCollectionTextMode(this.TextMode, m_Params);
                }
                return m_Params;
            }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "StoredProcedure";
            }
        }

        [SfcKey(1)]
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

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        [CLSCompliant(false)]
        [SfcReference(typeof(Schema), typeof(SchemaCustomResolver), "Resolve")]
        public override System.String Schema
        {
            get
            {
                return base.Schema;
            }
            set
            {
                base.Schema = value;
            }
        }

        /// <summary>
        /// Touch all the numbered stored procedures too
        /// </summary>
        protected override void TouchImpl()
        {            
            if (this.IsSupportedObject<NumberedStoredProcedure>())
            {
                foreach (NumberedStoredProcedure nsp in this.NumberedStoredProcedures)
                {
                    nsp.Touch();
                }
            }
        }

        bool ShouldScriptBodyAtAlter()
        {
            if( GetIsTextDirty() )
            {
                return true;
            }
            StringCollection sc = new StringCollection();
            sc.Add("AnsiNullsStatus");
            sc.Add("QuotedIdentifierStatus");
            sc.Add("Recompile");
            sc.Add("IsEncrypted");
            sc.Add("ForReplication");
            sc.Add("ImplementationType");

            if( this.ServerVersion.Major > 8 )
            {
                sc.Add("ExecutionContext");
                sc.Add("AssemblyName");
                sc.Add("ClassName");
                sc.Add("MethodName");
            }
            if( this.Properties.ArePropertiesDirty(sc) )
            {
                return true;
            }

            if( IsCollectionDirty(Parameters) )
            {
                return true;
            }

            return false;
        }

        void ScriptSP(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType, bool skipSetOptions = false)
        {
            bool bForCreate = IsCreate(scriptHeaderType);

            if (bForCreate || ShouldScriptBodyAtAlter())
            {
                ScriptInternal(queries, sp, scriptHeaderType, skipSetOptions);
            }

            //sp_procoption not supported for Cloud DB
            if(!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                ScriptExternal(queries, sp, bForCreate);
            }

            // In Hekaton M5, ALTER AUTHORIZATION is not supported for a natively compiled stored procedure.
            if (!IsSupportedProperty("IsNativelyCompiled", sp) || !this.GetPropValueOptional("IsNativelyCompiled", false))
            {
                if (sp.IncludeScripts.Owner)
                {
                    //script change owner if dirty
                    ScriptOwner(queries, sp);
                }
            }
        }

        void ScriptSPHeaderInternal(StringBuilder sb, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            switch (scriptHeaderType)
            {
                case ScriptHeaderType.ScriptHeaderForCreate:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} PROCEDURE {1}", Scripts.CREATE, FormatFullNameForScripting(sp));
                    break;
                case ScriptHeaderType.ScriptHeaderForAlter:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} PROCEDURE {1}", Scripts.ALTER, FormatFullNameForScripting(sp));
                    break;
                case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                    ThrowIfCreateOrAlterUnsupported(sp.TargetServerVersion,
                        ExceptionTemplates.CreateOrAlterDownlevel(
                            "Procedure",
                            GetSqlServerName(sp)));

                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0} PROCEDURE {1}", Scripts.CREATE_OR_ALTER, FormatFullNameForScripting(sp));
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.UnknownEnumeration(scriptHeaderType.ToString()));
            }

            sb.Append(sp.NewLine);

            bool bFirst = true;
            StringCollection param_strings = new StringCollection();
            foreach (StoredProcedureParameter spp in Parameters)
            {
                if (spp.State == SqlSmoState.ToBeDropped)
                {
                    continue;
                }
                if (String.Empty == spp.Name)
                {
                    continue;
                }

                if (!bFirst)
                {
                    sb.Append(",");
                    sb.Append(sp.NewLine);
                }
                bFirst = false;
                spp.ScriptDdlInternal(param_strings, sp);
                sb.AppendFormat(SmoApplication.DefaultCulture, "\t");
                sb.Append(param_strings[0]);
                param_strings.Clear();
            }
            if (!bFirst)
            {
                sb.Append(sp.NewLine);
            }

            bool bNeedsComma = false;

            // script Hekaton properties
            if (IsSupportedProperty("IsNativelyCompiled", sp))
            {
                AppendWithOption(sb, "IsNativelyCompiled", Scripts.NATIVELY_COMPILED, ref bNeedsComma);
            }

            if (IsSupportedProperty("IsSchemaBound", sp))
            {
                AppendWithOption(sb, "IsSchemaBound", Scripts.SP_SCHEMABINDING, ref bNeedsComma);
            }

            if (IsTransactSql(sp))
            {
                AppendWithOption(sb, "Recompile", "RECOMPILE", ref bNeedsComma);
                if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
                {
                    AppendWithOption(sb, "IsEncrypted", "ENCRYPTION", ref bNeedsComma);
                }
            }

            if (ServerVersion.Major >= 9 && sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                AddScriptExecuteAs(sb, sp, this.Properties, ref bNeedsComma);
            }

            if (bNeedsComma) //if options were added then go to next line
            {
                sb.Append(sp.NewLine);
            }

            if (IsSupportedProperty("ForReplication", sp))
            {
                if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType) &&  Object.Equals(Properties.Get("ForReplication").Value, true))
                {
                    sb.Append("FOR REPLICATION");
                    sb.Append(sp.NewLine);
                }
            }

            sb.Append("AS");
        }

        void ScriptSPBodyInternal(StringBuilder sb, ScriptingPreferences sp)
        {
            if (IsTransactSql(sp))
            {
                sb.Append(GetTextBody(true));
            }
            else
            {
                // this check ensures we can't script a CLR Stored Proc that targets Cloud Engine
                ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.ClrStoredProcedureDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));

                // it insures we can't script a CLR SP that targets a 8.0 server
                ThrowIfBelowVersion90(sp.TargetServerVersion,
                    ExceptionTemplates.ClrStoredProcedureDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));

                string tempString;
                sb.Append("EXTERNAL NAME ");

                tempString = (string)this.GetPropValue("AssemblyName");
                if (string.Empty == tempString)
                {
                    throw new PropertyNotSetException("AssemblyName");
                }

                sb.AppendFormat("[{0}]", SqlBraket(tempString));

                tempString = (string)this.GetPropValue("ClassName");
                if (string.Empty == tempString)
                {
                    throw new PropertyNotSetException("ClassName");
                }

                sb.AppendFormat(".[{0}]", SqlBraket(tempString));

                tempString = (string)this.GetPropValue("MethodName");
                if (string.Empty == tempString)
                {
                    throw new PropertyNotSetException(tempString);
                }

                sb.AppendFormat(".[{0}]", SqlBraket(tempString));
            }
        }

        bool IsTransactSql(ScriptingPreferences sp)
        {
            bool bTransactSql = true;
            object obj = this.GetPropValueOptional("ImplementationType");
            if (this.DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase && obj != null && (ImplementationType)obj == ImplementationType.SqlClr)
            {
                // CLR procedures are not supported on versions prior to 9.0
                // so throw an error saying we can't create it there
                if (ServerVersion.Major < 9)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.ClrNotSupported("ImplementationType", ServerVersion.ToString()));
                }

                // this check ensures we can't script a CLR Stored Proc that targets Cloud Engine
                ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.ClrStoredProcedureDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));

                // it insures we can't script a CLR SP that targets a 8.0 server
                ThrowIfBelowVersion90(sp.TargetServerVersion,
                    ExceptionTemplates.ClrStoredProcedureDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));

                bTransactSql = false;

                if (this.Properties.Get("Text").Dirty && false == this.TextMode)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoPropertyChangeForDotNet("TextBody"));
                }
            }
            return bTransactSql;
        }

        private void ScriptAlterSkipSetOptions(StringCollection alterQuery, ScriptingPreferences sp)
        {
            InitializeKeepDirtyValues();
            ScriptSP(alterQuery, sp, ScriptHeaderType.ScriptHeaderForAlter, skipSetOptions: true); // script ALTER and skip SET options
        }

        void ScriptInternal(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType, bool skipSetOptions = false)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string sFullScriptingName = SqlString(FormatFullNameForScripting(sp));
            String sOwner = (String)this.Schema;
            ScriptInformativeHeaders(sp, sb);

            object ansi, qi;
            ScriptAnsiQI(this, sp, queries, sb, out ansi, out qi, skipSetOptions);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    string sExists;
                    sExists = (sp.ScriptForAlter) ? string.Empty : "NOT";
                    if (sp.TargetServerVersion <= SqlServerVersion.Version80)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            Scripts.INCLUDE_EXISTS_PROCEDURE80, sExists,
                            sFullScriptingName);
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            Scripts.INCLUDE_EXISTS_PROCEDURE90, sExists,
                            sFullScriptingName);
                    }

                    sb.Append(Globals.newline);
                    sb.Append("BEGIN");
                    sb.Append(Globals.newline);
                }
            }

            StringBuilder spBody = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (false == this.TextMode || (true == sp.OldOptions.EnforceScriptingPreferences && true == sp.DataType.UserDefinedDataTypesToBaseType))
            {
                if (!sp.OldOptions.DdlBodyOnly)
                {
                    ScriptSPHeaderInternal(spBody, sp, scriptHeaderType);
                    spBody.Append(sp.NewLine);
                }

                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    ScriptSPBodyInternal(spBody, sp);
                }
            }
            else
            {
                if (this.State == SqlSmoState.Existing && IsSupportedProperty("ForReplication", sp))
                {
                    object objForRepl = GetPropValueOptional("ForReplication");
                    if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType
                        && null != objForRepl && true == (bool)objForRepl)
                    {
                        throw new WrongPropertyValueException(string.Format(CultureInfo.CurrentCulture,
                            ExceptionTemplates.ReplicationOptionNotSupportedForCloud, "FOR REPLICATION"));
                    }

                }
                // we switch on the forceCheckName to true for stored procs. This is because engine doesn't store
                // the definition for stored procs properly if sp_rename is used to rename the stored proc.
                // Our ssms uses sp_rename for stored procs which should not be used see vsts:204338.
                // But even if the user renamed the procs manually using the script the server stored definition
                // becomes un-trustable. We force the options which would force replace the server's definition script
                // name with the SMO name if required.-anchals
                spBody.Append(GetTextForScript(sp, new String[] { "procedure", "proc" }, forceCheckNameAndManipulateIfRequired: true, scriptHeaderType: scriptHeaderType));
            }

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly && sp.IncludeScripts.ExistenceCheck)
            {
                // VSTS #904569 - keep the 'if exists' logic as is in Denali; and only for SP creation, create
                // an empty SP using dynamic t-sql inside 'if not exists' condition, then alter the SP with the actual body.
                if (IsCreate(scriptHeaderType))
                {
                    // VSTS #904569, Script Empty Header
                    string spEmptyCreateStatement = "CREATE PROCEDURE " + sFullScriptingName + " AS";
                    // sFullScriptingName is already skipped, we can use it here directly
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_executesql @statement = N'{0}' ", spEmptyCreateStatement);
                    sb.Append(Globals.newline);
                    sb.Append("END");
                    queries.Add(sb.ToString());
                    sb.Length = 0;

                    StringCollection alterQuery = new StringCollection();
                    this.Touch(); // Touch the the object for ScriptAlter
                    ScriptingPreferences spTemp = (ScriptingPreferences)sp.Clone();
                    spTemp.IncludeScripts.ExistenceCheck = false;
                    spTemp.IncludeScripts.Header = false;
                    spTemp.OldOptions.EnforceScriptingPreferences = true;
                    // Script ALTER without SET options on the top
                    ScriptAlterSkipSetOptions(alterQuery, spTemp);

                    queries.AddCollection(alterQuery);
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_executesql @statement = {0} ", MakeSqlString(spBody.ToString()));
                    sb.Append(Globals.newline);
                    sb.Append("END");
                }
            }
            else
            {
                sb.Append(spBody.ToString());
            }

            if (sb.Length > 0)
            {
                queries.Add(sb.ToString());
                sb.Length = 0;
            }
        }

        private void ScriptExternal(StringCollection queries, ScriptingPreferences sp, bool bForCreate)
        {
            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                Property startupProp = Properties.Get("Startup");

                if ((!bForCreate && startupProp != null && startupProp.Dirty) || // alter
                    (bForCreate && startupProp != null && startupProp.Value != null && (bool)startupProp.Value == true)) // create
                {

                    StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                    String sOwner = (String)this.Schema;

                    // retrieve full scripting name
                    string sFullScriptingName = FormatFullNameForScripting(sp);

                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC sp_procoption N'{0}', 'startup', '{1}'",
                        SqlString(sFullScriptingName),
                        (bool)startupProp.Value ? 1 : 0);

                    sb.Append(sp.NewLine);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }
            }
        }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            // NOTE: we have to check if it's not a CLR sproc, because for some reason,
            // SMO thinks CLR sprocs are encrypted
            if (this.State != SqlSmoState.Creating && this.IsEncrypted && this.ImplementationType == ImplementationType.TransactSql)
            {
                ThrowIfBelowVersion90(sp.TargetServerVersion,
                    ExceptionTemplates.EncryptedStoredProcedureDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));
            }

            ScriptSP(queries, sp, ScriptHeaderType.ScriptHeaderForCreate);
        }

        /// <summary>
        /// Create OR ALTER the object. First perform an internal check for existence. If object exists, take CREATE path; Otherwise take ALTER path.
        /// return without exception.
        /// </summary>
        public void CreateOrAlter()
        {
            base.CreateOrAlterImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreateOrAlter(StringCollection queries, ScriptingPreferences sp)
        {
            InitializeKeepDirtyValues();
            ScriptSP(queries, sp, ScriptHeaderType.ScriptHeaderForCreateOrAlter);
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


        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            CheckObjectState();
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            // format full table name for scripting
            string sFullScriptingName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, sFullScriptingName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion < SqlServerVersion.Version130)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, sp.TargetServerVersion < SqlServerVersion.Version90 ?
                    Scripts.INCLUDE_EXISTS_PROCEDURE80 : Scripts.INCLUDE_EXISTS_PROCEDURE90,
                    "", SqlString(sFullScriptingName));
                sb.Append(Globals.newline);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP PROCEDURE {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                sFullScriptingName);

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (IsObjectDirty())
            {
                InitializeKeepDirtyValues();
                ScriptSP(alterQuery, sp, ScriptHeaderType.ScriptHeaderForAlter);
                ScriptNumberedStoredProcedures(alterQuery, sp);
            }
        }

        public void Rename(string newname)
        {
            Table.CheckTableName(newname);
            base.RenameImpl(newname);
        }

        public void ReCompileReferences()
        {
            ReCompile(this.Name, this.Schema);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            // the user is responsible to put the database in single user mode on 7.0 server
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_rename @objname = N'{0}', @newname = N'{1}', @objtype = N'OBJECT'",
                SqlString(this.FullQualifiedName),
                SqlString(newName)));
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || IsCollectionDirty(Parameters);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (this.DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[] { new PropagateInfo(Parameters, false) };
            }

            return new PropagateInfo[] {
            new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
            new PropagateInfo(Parameters, false),
            new PropagateInfo(NumberedStoredProcedures, false,false)
            };
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

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_ExtendedProperties)
            {
                m_ExtendedProperties.MarkAllDropped();
            }

            if (null != numberedStoredProcedureCollection)
            {
                numberedStoredProcedureCollection.MarkAllDropped();
            }
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
        internal static string[] GetScriptFields(Type parentType,
            Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
                if (defaultTextMode)
                {
                    string[] fields =
                    {
                        "AnsiNullsStatus",
                        "ImplementationType",
                        "IsEncrypted",
                        "IsSystemObject",
                        "ID",
                        "QuotedIdentifierStatus",
                        "Startup",
                        "Text",
                    };
                    List<string> list = GetSupportedScriptFields(typeof(StoredProcedure.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                    return list.ToArray();
                }
                else
                {
                    string[] fields =
                    {
                        "AnsiNullsStatus",
                        "AssemblyName",
                        "ClassName",
                        "ExecutionContext",
                        "ExecutionContextPrincipal",
                        "ForReplication",
                        "ID",
                        "ImplementationType",
                        "IsEncrypted",
                        "IsNativelyCompiled",
                        "IsSchemaBound",
                        "IsSchemaOwned",
                        "IsSystemObject",
                        "MethodName",
                        "QuotedIdentifierStatus",
                        "Owner",
                        "Recompile",
                        "Startup",
                        "Text",
                    };
                    List<string> list = GetSupportedScriptFields(typeof(StoredProcedure.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                    return list.ToArray();

                }


        }

        #region TextModeImpl

        public string ScriptHeader(bool forAlter)
        {
            CheckObjectState();
            return GetTextHeader(forAlter);
        }

        public string ScriptHeader(ScriptHeaderType scriptHeaderType)
        {
            CheckObjectState();
            return GetTextHeader(scriptHeaderType);
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Design)]
        public string TextBody
        {
            get { CheckObjectState(); return GetTextBody(); }
            set { CheckObjectState(); SetTextBody(value); }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public string TextHeader
        {
            get { CheckObjectState(); return GetTextHeader(false); }
            set { CheckObjectState(); SetTextHeader(value); }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.Design| SfcPropertyFlags.SqlAzureDatabase)]
        public bool TextMode
        {
            get { CheckObjectState(); return GetTextMode(); }
            set
            {
                CheckObjectState();
                if (value && ImplementationType.SqlClr == this.GetPropValueOptional("ImplementationType", ImplementationType.TransactSql))
                {
                    throw new PropertyWriteException("TextMode", this.GetType().Name, this.Name,
                        ExceptionTemplates.ReasonNotIntextMode);
                }
                SetTextMode(value, new SmoCollectionBase[] { Parameters });
            }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            switch (this.ServerVersion.Major)
            {
                case 7:
                    switch (prop.Name)
                    {
                        case "Recompile":
                            Validate_set_TextObjectDDLProperty(prop, value);
                            break;
                        case "ForReplication": goto case "Recompile";
                        case "IsEncrypted": goto case "Recompile";

                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                case 8: goto case 7;
                case 9:
                    switch (prop.Name)
                    {
                        case "Recompile":
                            Validate_set_TextObjectDDLProperty(prop, value);
                            break;
                        case "ForReplication": goto case "Recompile";
                        case "IsEncrypted": goto case "Recompile";
                        case "ExecutionContext": goto case "Recompile";
                        case "ExecutionContextPrincipal": goto case "Recompile";
                        case "AssemblyName": goto case "Recompile";
                        case "ClassName": goto case "Recompile";
                        case "MethodName": goto case "Recompile";

                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                default: goto case 9;
            }
        }

        // after object creation we do not support text mode = true
        protected override void PostCreate()
        {
            if (true == this.TextMode && false == CheckTextModeSupport())
            {
                //simulate an user reset
                this.TextMode = false;
            }
        }
        #endregion

        internal override void ScriptCreateInternal(StringCollection query, ScriptingPreferences sp, bool skipPropagateScript)
        {
            ScriptCreate(query, sp);
            ScriptNumberedStoredProcedures(query, sp);

            // script permissions if needed
            if (sp.IncludeScripts.Permissions)
            {
                AddScriptPermission(query, sp);
            }

            if (!skipPropagateScript)
            {
                //propagate create to children collections
                PropagateScript(query, sp, PropagateAction.Create);
            }
        }

        private void ScriptNumberedStoredProcedures(StringCollection queries, ScriptingPreferences sp)
        {
            if (this.IsSupportedObject<NumberedStoredProcedure>(sp))
            {
                foreach (NumberedStoredProcedure nsp in this.NumberedStoredProcedures)
                {
                    if (sp.ScriptForAlter)
                    {
                        nsp.ScriptAlterInternal(queries, sp);
                    }
                    else
                    {
                        nsp.ScriptCreateInternal(queries, sp);
                    }
                }
            }
        }
    }
}

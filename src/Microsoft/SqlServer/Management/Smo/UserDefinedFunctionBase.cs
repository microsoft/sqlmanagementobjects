// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.StateChangeEvent("CREATE_FUNCTION", "FUNCTION")]
    [Facets.StateChangeEvent("ALTER_FUNCTION", "FUNCTION")]
    [Facets.StateChangeEvent("RENAME", "FUNCTION")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "FUNCTION")] // For Owner
    [Facets.StateChangeEvent("ALTER_SCHEMA", "FUNCTION")] // For Schema
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    public partial class UserDefinedFunction : ScriptSchemaObjectBase,
        Cmn.ICreatable, Cmn.IAlterable, Cmn.ICreateOrAlterable, Cmn.IRenamable, Cmn.IDroppable, Cmn.IDropIfExists,
        IExtendedProperties, IScriptable, ITextObject
    {
        internal UserDefinedFunction(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
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

        private UserDefinedFunctionParameterCollection m_UserDefinedFunctionParams;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedFunctionParameter), SfcObjectFlags.Design | SfcObjectFlags.NaturalOrder)]
        public UserDefinedFunctionParameterCollection Parameters
        {
            get
            {
                CheckObjectState();
                if( null == m_UserDefinedFunctionParams )
                {
                    m_UserDefinedFunctionParams = new UserDefinedFunctionParameterCollection(this);
                    SetCollectionTextMode(this.TextMode, m_UserDefinedFunctionParams);
                }
                return m_UserDefinedFunctionParams;
            }
        }

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // add the object-level permissions
            AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Object, sp);

            // iterate through all the columns and add the column-level permissions
            foreach (Column c in this.Columns)
            {
                c.AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Column, sp);
            }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "UserDefinedFunction";
            }
        }

        private void AddParam(StringBuilder sb, ScriptingPreferences sp, UserDefinedFunctionParameter spp)
        {
            StringCollection param_strings = new StringCollection();

            spp.UseOutput = false;
            spp.ScriptDdlInternal(param_strings, sp);

            sb.Append(param_strings[0]);
            param_strings.Clear();
        }

        private void ScriptUDF(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            // Azure SQL DW database only support Scaler-Valued UDF, throw if object not this type while
            // scripting against SQL DW
            UserDefinedFunctionType functionType = this.GetPropValueOptional<UserDefinedFunctionType>("FunctionType",
                UserDefinedFunctionType.Unknown);
            if (functionType != UserDefinedFunctionType.Scalar && functionType != UserDefinedFunctionType.Inline && sp.TargetEngineIsAzureSqlDw())
            {
                throw new UnsupportedEngineEditionException(
                    ExceptionTemplates.PropertyValueNotSupportedForSqlDw(typeof(UserDefinedFunctionType).Name, functionType.ToString()))
                    .SetHelpContext("PropertyValueNotSupportedForSqlDw");
            }
            Property property;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool fAnsiNullsExists = false;
            bool fQuotedIdentifierExists = false;

            // retrieve full scripting name
            string sFullScriptingName = FormatFullNameForScripting(sp);

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, sFullScriptingName, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                fAnsiNullsExists = (null != Properties.Get("AnsiNullsStatus").Value);
                fQuotedIdentifierExists = (null != Properties.Get("QuotedIdentifierStatus").Value);

                if (fAnsiNullsExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_ANSI_NULLS,
                        (bool)Properties["AnsiNullsStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }

                if (fQuotedIdentifierExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_QUOTED_IDENTIFIER,
                        (bool)Properties["QuotedIdentifierStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }
            }

            bool bTransactSql = true;
            if (null != (property = this.Properties.Get("ImplementationType")).Value)
            {
                if (ImplementationType.SqlClr == (ImplementationType)property.Value)
                {
                    // CLR procedures are not supported on versions prior to 9.0 
                    if (ServerVersion.Major < 9)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.ClrNotSupported("ImplementationType", ServerVersion.ToString()));
                    }

                    //CLR UDF is not supported if target server is Azure below v12.0
                    if (sp.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                    {
                    // it insures we can't script a CLR UDF that targets a 8.0 server
                   ThrowIfBelowVersion90(sp.TargetServerVersion,
                        ExceptionTemplates.ClrUserDefinedFunctionDownlevel(
                            FormatFullNameForScripting(sp, true),
                            GetSqlServerName(sp)));
                    }

                    bTransactSql = false;
                }
            }

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    string sExists;
                    sExists = (sp.ScriptForAlter) ? string.Empty : "NOT";
                    if (sp.TargetServerVersion >= SqlServerVersion.Version90 && this.ServerVersion.Major >= 9)
                    {
                        sb.AppendFormat(Scripts.INCLUDE_EXISTS_FUNCTION90, sExists, SqlString(sFullScriptingName));
                    }
                    else
                    {
                        sb.AppendFormat(Scripts.INCLUDE_EXISTS_FUNCTION80, sExists, SqlString(sFullScriptingName));
                    }
                    sb.Append(Globals.newline);
                    sb.Append("BEGIN");
                    sb.Append(Globals.newline);
                }
            }

            StringBuilder udfBody = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (!TextMode || (sp.OldOptions.EnforceScriptingPreferences && sp.DataType.UserDefinedDataTypesToBaseType))
            {
                if (!sp.OldOptions.DdlBodyOnly)
                {
                    switch (scriptHeaderType)
                    {
                        case ScriptHeaderType.ScriptHeaderForCreate:
                            udfBody.AppendFormat(SmoApplication.DefaultCulture, "{0} FUNCTION {1}", Scripts.CREATE, sFullScriptingName);
                            break;
                        case ScriptHeaderType.ScriptHeaderForAlter:
                            udfBody.AppendFormat(SmoApplication.DefaultCulture, "{0} FUNCTION {1}", Scripts.ALTER, sFullScriptingName);
                            break;
                        case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                            ThrowIfCreateOrAlterUnsupported(sp.TargetServerVersion,
                                ExceptionTemplates.CreateOrAlterDownlevel(
                                    "Function",
                                    GetSqlServerName(sp)));

                            udfBody.AppendFormat(SmoApplication.DefaultCulture, "{0} FUNCTION {1}", Scripts.CREATE_OR_ALTER, sFullScriptingName);
                            break;
                        default:
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(scriptHeaderType.ToString()));
                    }
                    udfBody.Append(Globals.LParen);

                    bool bFirst = true;
                    foreach (UserDefinedFunctionParameter spp in Parameters)
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
                            udfBody.Append(", ");
                        }

                        bFirst = false;

                        AddParam(udfBody, sp, spp);
                    }
                    udfBody.Append(Globals.RParen);
                    udfBody.Append(sp.NewLine);

                    UserDefinedFunctionType type = (UserDefinedFunctionType)this.GetPropValue("FunctionType");
                    ScriptReturnType(sp, udfBody, type);

                    bool bNeedsComma = false;

                    if (bTransactSql)
                    {
                        AppendWithOption(udfBody, "IsSchemaBound", Scripts.SP_SCHEMABINDING, ref bNeedsComma);

                        if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
                        {
                            if (ServerVersion.Major >= 13 &&
                                sp.TargetServerVersion >= SqlServerVersion.Version130)
                            {
                                // script Hekaton properties
                                if (IsSupportedProperty("IsNativelyCompiled", sp))
                                {
                                    AppendWithOption(udfBody, "IsNativelyCompiled", Scripts.NATIVELY_COMPILED,
                                        ref bNeedsComma);
                                }
                            }
                            AppendWithOption(udfBody, "IsEncrypted", "ENCRYPTION", ref bNeedsComma);
                        }

                        if (IsSupportedProperty("InlineType", sp) && functionType == UserDefinedFunctionType.Scalar)
                        {
                            AppendWithOption(udfBody, "InlineType", Scripts.INLINE_TYPE,
                                ref bNeedsComma);
                        }
                    }

                    if (ServerVersion.Major >= 9 && sp.TargetServerVersion >= SqlServerVersion.Version90)
                    {
                        // we can't specify execution context for inline table-valued functions
                        if (type != UserDefinedFunctionType.Inline)
                        {
                            AddScriptExecuteAs(udfBody, sp, this.Properties, ref bNeedsComma);
                        }

                        AppendWithOption(udfBody, "ReturnsNullOnNullInput", "RETURNS NULL ON NULL INPUT", ref bNeedsComma);
                    }

                    if (bNeedsComma) //if options were added then go to next line
                    {
                        udfBody.Append(sp.NewLine);
                    }

                    if (!bTransactSql && m_OrderColumns != null && m_OrderColumns.Count > 0)
                    {
                        udfBody.Append("ORDER ");
                        udfBody.Append(Globals.LParen);
                        bool firstCol = true;
                        foreach (OrderColumn orderCol in OrderColumns)
                        {
                            if (!firstCol)
                            {
                                udfBody.Append(Globals.comma);
                                udfBody.Append(Globals.space);
                            }
                            else
                            {
                                firstCol = false;
                            }
                            Column basecol = m_Columns[orderCol.Name];
                            if (basecol == null)
                            {
                                throw new SmoException(ExceptionTemplates.OrderHintRefsNonexCol(Name, "[" + SqlStringBraket(orderCol.Name) + "]"));
                            }

                            udfBody.Append(MakeSqlBraket(orderCol.GetName(sp)));
                            udfBody.Append(Globals.space);
                            if (orderCol.Descending)
                            {
                                udfBody.Append("DESC");
                            }
                            else
                            {
                                udfBody.Append("ASC");
                            }
                        }
                        udfBody.Append(Globals.RParen);
                        udfBody.Append(sp.NewLine);
                    }

                    udfBody.Append("AS ");
                }

                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    if (!sp.OldOptions.DdlBodyOnly)
                    {
                        udfBody.Append(sp.NewLine);
                    }

                    string tempString;

                    // btw, bTransactSql will be true here, for Cloud Engine
                    if (bTransactSql)
                    {
                        udfBody.Append(GetTextBody(true));
                    }
                    else
                    {

                        udfBody.Append("EXTERNAL NAME ");

                        tempString = (string)this.GetPropValue("AssemblyName");
                        if (string.Empty == tempString)
                        {
                            throw new PropertyNotSetException("AssemblyName");
                        }

                        udfBody.AppendFormat("[{0}]", SqlBraket(tempString));

                        tempString = (string)this.GetPropValue("ClassName");
                        if (string.Empty == tempString)
                        {
                            throw new PropertyNotSetException("ClassName");
                        }

                        udfBody.AppendFormat(".[{0}]", SqlBraket(tempString));

                        tempString = (string)this.GetPropValue("MethodName");
                        if (string.Empty == tempString)
                        {
                            throw new PropertyNotSetException(tempString);
                        }

                        udfBody.AppendFormat(".[{0}]", SqlBraket(tempString));
                    }
                }
            }
            else
            {
                // we switch on the forceCheckName to true for stored procs. This is because engine doesn't store
                // the definition for stored procs properly if sp_rename is used to rename the stored proc.
                // Our ssms uses sp_rename for stored procs which should not be used see vsts:204338.
                // But even if the user renamed the procs manually using the script the server stored definition
                // becomes un-trustable. We force the options which would force replace the server's definition script
                // name with the SMO name if required.-anchals
                udfBody.Append(GetTextForScript(sp, new String[] { "function" }, forceCheckNameAndManipulateIfRequired: true, scriptHeaderType: scriptHeaderType));
            }

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly && sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "execute dbo.sp_executesql @statement = {0} ", MakeSqlString(udfBody.ToString()));
                sb.Append(Globals.newline);
                sb.Append("END");
                sb.Append(Globals.newline);
            }
            else
            {
                sb.Append(udfBody.ToString());
            }

            // write it out

            queries.Add(sb.ToString());

            sb.Length = 0;

        }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            // UDFs are not scriptable on 7.0
            ThrowIfBelowVersion80(sp.TargetServerVersion);
            ThrowIfCompatibilityLevelBelow80();

            if (this.State != SqlSmoState.Creating && this.IsEncrypted && this.ImplementationType == ImplementationType.TransactSql)
            {
                ThrowIfBelowVersion90(sp.TargetServerVersion,
                    ExceptionTemplates.EncryptedUserDefinedFunctionsDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));
            }

            if (sp.OldOptions.PrimaryObject == true)
            {
                InitializeKeepDirtyValues();
                ScriptUDF(queries, sp, ScriptHeaderType.ScriptHeaderForCreate);
                if (sp.IncludeScripts.Owner)
                {

                    //script change owner if dirty
                    ScriptOwner(queries, sp);
                }
            }
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
            // UDFs are not scriptable on 7.0
            ThrowIfBelowVersion80(sp.TargetServerVersion);
            ThrowIfCompatibilityLevelBelow80();

            InitializeKeepDirtyValues();
            ScriptUDF(queries, sp, ScriptHeaderType.ScriptHeaderForCreateOrAlter);
            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptOwner(queries, sp);
            }
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
                    Scripts.INCLUDE_EXISTS_FUNCTION80 : Scripts.INCLUDE_EXISTS_FUNCTION90,
                    "", SqlString(sFullScriptingName));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP FUNCTION {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                sFullScriptingName);

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
        }

        bool ShouldScriptBodyAtAlter()
        {
            if (GetIsTextDirty())
            {
                return true;
            }

            foreach (Property prop in this.Properties)
            {
                // If any property other than owner is dirty then body will be scripted

                if (string.Compare("Owner",prop.Name,StringComparison.OrdinalIgnoreCase) != 0
                    && prop.Writable
                    && prop.Dirty)
                {
                    return true;
                }
            }

            return false;
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (IsObjectDirty())
            {
                InitializeKeepDirtyValues();
                if (ShouldScriptBodyAtAlter())
                {
                    ScriptUDF(alterQuery, sp, ScriptHeaderType.ScriptHeaderForAlter);
                }
                if (sp.IncludeScripts.Owner)
                {
                    //script change owner if dirty
                    ScriptOwner(alterQuery, sp);
                }
            }
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || IsCollectionDirty(Parameters);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[]
            {
                new PropagateInfo(Parameters, false),
                        new PropagateInfo(action != PropagateAction.Create ? null: Indexes, false),
                new PropagateInfo((ServerVersion.Major < 8 || this.DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase) ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix )
            };
        }

        /// <summary>
        /// Renames the object
        /// </summary>
        /// <param name="newname">New UDF name</param>
        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC {0}.dbo.sp_rename @objname = N'{1}', @newname = N'{2}', @objtype = N'OBJECT'",
                                MakeSqlBraket(Parent.Name),
                                SqlString(this.FullQualifiedName),
                                SqlString(newName)));
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

        void ScriptReturnType(ScriptingPreferences sp, StringBuilder sb, UserDefinedFunctionType type)
        {
            sb.Append("RETURNS ");
            if (UserDefinedFunctionType.Scalar == type)
            {
                UserDefinedDataType.AppendScriptTypeDefinition(sb, sp, this, this.DataType.SqlDataType);
            }
            else if (UserDefinedFunctionType.Inline == type)
            {
                sb.Append("TABLE");
            }
            else if (UserDefinedFunctionType.Table == type)
            {
                sb.Append(GetPropValue("TableVariableName"));
                sb.Append(" TABLE ");
                bool bSysNamed = sp.Table.SystemNamesForConstraints;
                //we always script the UDF TVF def with primary keys, checks
                //supports only nameless constraints
                sp.Table.SystemNamesForConstraints = false;
                try
                {
                    Table.ScriptTableInternal(sp, sb, Columns, Indexes);

                    foreach (Check check in Checks)
                    {
                        sb.Append(Globals.comma);
                        sb.Append(sp.NewLine);
                        sb.Append(check.ScriptDdlBodyWithoutName(sp));
                    }
                }
                finally
                {
                    sp.Table.SystemNamesForConstraints = bSysNamed;
                }
                sb.Append(sp.NewLine);
                sb.Append(Globals.RParen);
            }
            sb.Append(" ");
        }

        private IndexCollection m_Indexes;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Index), SfcObjectFlags.Design)]
        public IndexCollection Indexes
        {
            get
            {
                CheckObjectState();
                if (null == m_Indexes)
                {
                    m_Indexes = new IndexCollection(this);
                }
                return m_Indexes;
            }
        }

        private ColumnCollection m_Columns = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Column), SfcObjectFlags.Design | SfcObjectFlags.NaturalOrder)]
        public ColumnCollection Columns
        {
            get
            {
                CheckObjectState();
                if (null == m_Columns)
                {
                    m_Columns = new ColumnCollection(this);
                    SetCollectionTextMode(this.TextMode, m_Columns);
                }
                return m_Columns;
            }
        }

        // ADD Order Column Collection here
        private OrderColumnCollection m_OrderColumns = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(OrderColumn))]
        public OrderColumnCollection OrderColumns
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(OrderColumn));
                if (null == m_OrderColumns)
                {
                    m_OrderColumns = new OrderColumnCollection(this);
                    SetCollectionTextMode(this.TextMode, m_OrderColumns);
                }
                return m_OrderColumns;
            }
        }

        private CheckCollection m_Checks = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Check), SfcObjectFlags.Design)]
        public CheckCollection Checks
        {
            get
            {
                CheckObjectState();
                if (null == m_Checks)
                {
                    m_Checks = new CheckCollection(this);
                    SetCollectionTextMode(this.TextMode, m_Checks);
                }
                return m_Checks;
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

        private DataType dataType = null;

        [CLSCompliant(false)]
        [SfcReference(typeof(UserDefinedType), typeof(UserDefinedTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedDataType), typeof(UserDefinedDataTypeResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public DataType DataType
        {
            get
            {
                CheckObjectState();
                //if the object is Multi-statement Table-valued Function it has no return value
                //so we return null. But only if the object is existing.
                //if the object is creating it means the udf is not commited and the user can play with the property
                if( this.State == SqlSmoState.Existing &&
                    UserDefinedFunctionType.Scalar != (UserDefinedFunctionType)this.GetPropValue("FunctionType") )
                {
                    return null;
                }

                return GetDataType(ref dataType);
            }
            set
            {
                //we always let him set this values in case the user will also change the FunctionType
                SetDataType(ref dataType, value);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            this.dataType = null;
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
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            if (defaultTextMode)
                {
                       string[] fields = {
                            nameof(AnsiNullsStatus),
                            nameof(FunctionType),
                            nameof(ID),
                            nameof(ImplementationType),
                            nameof(InlineType),
                            nameof(IsEncrypted),
                            nameof(IsNativelyCompiled),
                            nameof(IsSchemaBound),
                            nameof(IsSystemObject),
                            nameof(QuotedIdentifierStatus),
                            nameof(ReturnsNullOnNullInput)};
                        List<string> list = GetSupportedScriptFields(typeof(UserDefinedFunction.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                        list.Add("Text");
                        return list.ToArray();
                }
                else
                {
                    string[] fields = {
                        nameof(AnsiNullsStatus),
                        nameof(AssemblyName),
                        nameof(ClassName),
                        "DataTypeSchema",
                        nameof(ExecutionContext),
                        nameof(ExecutionContextPrincipal),
                        nameof(FunctionType),
                        nameof(ID),
                        nameof(ImplementationType),
                        nameof(InlineType),
                        nameof(IsEncrypted),
                        nameof(IsNativelyCompiled),
                        nameof(IsSchemaBound),
                        nameof(IsSchemaOwned),
                        nameof(IsSystemObject),
                        "Length",
                        nameof(MethodName),
                        "NumericPrecision",
                        "NumericScale",
                        nameof(Owner),
                        nameof(QuotedIdentifierStatus),
                        nameof(ReturnsNullOnNullInput),
                        "SystemType",
                        nameof(TableVariableName),
                        "XmlDocumentConstraint",
                        "XmlSchemaNamespace",
                        "XmlSchemaNamespaceSchema"};
                        List<string> list = GetSupportedScriptFields(typeof(UserDefinedFunction.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                        list.Add("Text");
                        list.Add("DataType");
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

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
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

        [SfcProperty(SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public bool TextMode
        {
            get { CheckObjectState(); return GetTextMode(); }
            set { CheckObjectState(); SetTextMode(value, new SmoCollectionBase [] { Parameters, Columns, Checks }); }
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
                    // we should not have UDFs on 7.0
                    Diagnostics.TraceHelper.Assert(false, "ServerVersion.Major == 7");
                    break;
                case 8:
                    switch (prop.Name)
                    {
                        case "FunctionType":
                            Validate_set_TextObjectDDLProperty(prop, value);
                            break;
                        case "IsSchemaBound": goto case "FunctionType";
                        case "IsEncrypted": goto case "FunctionType";
                        case "TableVariableName": goto case "FunctionType";

                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                case 9:
                    switch (prop.Name)
                    {
                        case "FunctionType":
                            Validate_set_TextObjectDDLProperty(prop, value);
                            break;
                        case "IsSchemaBound": goto case "FunctionType";
                        case "IsEncrypted": goto case "FunctionType";
                        case "ExecutionContext": goto case "FunctionType";
                        case "TableVariableName": goto case "FunctionType";
                        case "ExecutionContextPrincipal": goto case "FunctionType";
                        case "AssemblyName": goto case "FunctionType";
                        case "ClassName": goto case "FunctionType";
                        case "MethodName": goto case "FunctionType";

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
    }
}



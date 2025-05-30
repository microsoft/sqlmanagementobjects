// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class UserDefinedDataType : ScriptSchemaObjectBase,
        Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable,
        IExtendedProperties, IScriptable
    {
        internal UserDefinedDataType(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }


        #region Private Constants
        private const string AddUddt = "EXEC dbo.sp_addtype @typename={0}, @phystype='{1}'";
        private const string AddUddtNullOption = ", @nulltype='{0}'";
        private const string AddUddtOwnerOption80 = ", @owner=N'{0}'";

        private const string IfUddtNotExists =
@"IF NOT EXISTS (SELECT * FROM dbo.systypes WHERE name = N'{0}')
BEGIN
{1}
END
";
        #endregion

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "UserDefinedDataType";
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

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                #region Version 90
                if (this.State == SqlSmoState.Existing)
                {
                    InitializeKeepDirtyValues();
                }

                string sFullScriptingName = FormatFullNameForScripting(sp);
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, sFullScriptingName, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    AddExistsCheck(sb, "NOT", sp);
                }

                ScriptDdlGreaterEqual9(sb, sp);

                queries.Add(sb.ToString());
                AddBindings(queries, sp);

                //Owner will be scripted this way only if version is greater than 9
                if (sp.IncludeScripts.Owner)
                {

                    //script change owner if dirty
                    ScriptOwner(queries, sp);
                }
                #endregion
            }
            else
            {
                #region Version 80

                string typeName = this.Name;
                string physType = string.Empty;
                string nullable = "NULL";

                // need to get underlying type, and get attributes of it, if possible
                //
                physType = (string)GetTypeDefinitionScript(sp, this, "SystemType", false);

                // now add it
                sb.Append(string.Format(SmoApplication.DefaultCulture, AddUddt, FormatFullNameForScripting(sp, false), physType));

                // check to see if there's a valid null type
                if (this.State != SqlSmoState.Creating &&
                    !this.Nullable)
                {
                    nullable = "NOT NULL";
                }
                else
                {
                    nullable = "NULL";
                }

                // add null option
                sb.Append(string.Format(SmoApplication.DefaultCulture, AddUddtNullOption, nullable));

                // if our target is 2000, and we have a value for owner, let's use it
                if (!string.IsNullOrEmpty(this.Schema) && sp.IncludeScripts.Owner)
                {
                    sb.Append(string.Format(SmoApplication.DefaultCulture, AddUddtOwnerOption80, this.Schema));
                }

                // check for If Not Exists
                if (sp.IncludeScripts.ExistenceCheck)
                {
                    queries.Add(string.Format(
                                SmoApplication.DefaultCulture,
                                IfUddtNotExists,
                                typeName,
                                sb.ToString()));
                }
                else
                {
                    queries.Add(sb.ToString());
                }
                #endregion
            }

        }
        /// <summary>
        /// UDDT doesn't support sp_changeobjectowner - so this functionality is
        /// unsupported for Shiloh (2005) since ALTER AUTHORIZATION isn't available
        /// until Yukon (2008)
        /// </summary>
        /// <param name="sb">Builder to add the statements to</param>
        /// <param name="newOwner">The name of the new owner</param>
        /// <param name="sp">The scripting preferences</param>
        /// <returns></returns>
        internal override void ScriptOwnerForShiloh(StringBuilder sb, ScriptingPreferences sp, string newOwner)
        {
            if (this.ServerVersion.Major > 8)
            {
                throw new UnsupportedFeatureException(ExceptionTemplates.PropertyCannotBeSetForVersion("Owner", "User Defined Data Type", GetSqlServerName(sp) ));
            }
        }

        private void ScriptDdlGreaterEqual9(StringBuilder sb, ScriptingPreferences sp)
        {
            sb.AppendFormat("CREATE TYPE {0} FROM ", FormatFullNameForScripting(sp));
            sb.Append(GetTypeDefinitionScript(sp, this, "SystemType", true));

            object oNulls = Properties.Get("Nullable").Value;
            if (null != oNulls)
            {
                if (true == (bool)oNulls)
                {
                    //server will throw if null is not accepted
                    sb.Append(" NULL");
                }
                else
                {
                    sb.Append(" NOT NULL");
                }
            }
        }

        private void ScriptDdlLess9(StringBuilder sb, ScriptingPreferences sp)
        {
            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "EXEC dbo.sp_addtype {0}, ",
                            FormatFullNameForScripting(sp, false));
            sb.AppendFormat("N'{0}'", SqlString(GetTypeDefinitionScript(sp, this, "SystemType", false)));

            object oNulls = GetPropValueOptional("Nullable");
            if (null != oNulls)
            {
                if (true == (bool)oNulls)
                {
                    //server will throw if null is not accepted
                    sb.Append(",N'null'");
                }
                else
                {
                    sb.Append(",N'not null'");
                }
            }
            else
            {
                sb.Append(",null ");
            }

            if (0 != StringComparer.Compare("dbo", this.Schema))
            {
                throw new WrongPropertyValueException(ExceptionTemplates.TypeSchemaMustBeDbo("Schema", this.Schema));
            }
            sb.Append(sp.NewLine);
        }

        internal void AddBindings(StringCollection queries, ScriptingPreferences sp)
        {
        if (IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                return;
            }

            if (sp.OldOptions.Bindings || sp.ScriptForCreateDrop)
            {
                object oDefault = Properties.Get("Default").Value;
                if (null != oDefault && String.Empty != (String)oDefault)
                {
                    object oDefaultSchema = Properties.Get("DefaultSchema").Value;
                    if (sp.IncludeScripts.SchemaQualify)
                    {
                        queries.Add(GetBindDefaultScript(sp, (string)oDefaultSchema, (string)oDefault, true));
                    }
                    else
                    {
                        queries.Add(GetBindDefaultScript(sp, null, (string)oDefault, true));
                    }
                }

                object oRule = Properties.Get("Rule").Value;
                if (null != oRule && String.Empty != (String)oRule)
                {
                    object oRuleSchema = Properties.Get("RuleSchema").Value;
                    if (sp.IncludeScripts.SchemaQualify)
                    {
                        queries.Add(GetBindRuleScript(sp, (string)oRuleSchema, (string)oRule, true));
                    }
                    else
                    {
                        queries.Add(GetBindRuleScript(sp, null, (string)oRule, true));
                    }
                }
            }
        }


        /// <summary>
        /// This function generates a script containing the type definition for
        /// the object that is being passed in. For example it will output the
        /// name plus data type for a column.
        /// </summary>
        /// <param name="so"></param>
        /// <param name="oObj"></param>
        /// <param name="sTypeNameProperty"></param>
        /// <param name="bSquareBraketsForNative"></param>
        /// <returns></returns>
        static private string GetTypeDefinitionScript(ScriptingPreferences sp, SqlSmoObject oObj, string sTypeNameProperty, bool bSquareBraketsForNative)
        {
            StringBuilder sb = new StringBuilder();

            PropertyCollection p = oObj.Properties;

            if (oObj.State == SqlSmoState.Creating && null == oObj.Properties.Get(sTypeNameProperty).Value)
            {
                throw new PropertyNotSetException(sTypeNameProperty);
            }
            String sType = (String)p[sTypeNameProperty].Value;


            if (sp.DataType.TimestampToBinary && 0 == oObj.StringComparer.Compare("timestamp", sType))
            {
                if (bSquareBraketsForNative)
                {
                    sb.Append("[binary](8)");
                }
                else
                {
                    sb.Append("binary(8)");
                }
            }
            else
            {
                //no need for SqlBraket as it is a system type. it will only slow us down
                //we put brakets just look like uddts
                if (bSquareBraketsForNative)
                {
                    sb.AppendFormat("[{0}]", sType);
                }
                else
                {
                    sb.AppendFormat("{0}", sType);
                }

                SqlDataType sqlDataType = DataType.SqlToEnum(sType);

                // raise error if data type is not supported on target version.
                if (!DataType.IsDataTypeSupportedOnTargetVersion(sqlDataType, sp.TargetServerVersion, sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition))
                {
                    throw new SmoException(ExceptionTemplates.UnsupportedDataTypeOnTarget(
                        sqlDataType.ToString(),
                        sp.TargetServerVersion.ToString(),
                        sp.TargetDatabaseEngineType.ToString(),
                        sp.TargetDatabaseEngineEdition.ToString()));
                }

                // raise error if data type is not supported on target engine type.
                if (!DataType.IsDataTypeSupportedOnCloud(sqlDataType))
                {
                    ThrowIfCloud(sp.TargetDatabaseEngineType,
                        string.Format(CultureInfo.CurrentCulture,
                            ExceptionTemplates.PropertyNotSupportedOnCloud(sqlDataType.ToString())));
                }

                if (0 == string.Compare(sType, "xml", StringComparison.OrdinalIgnoreCase))
                {
                    // throw an exception if the target server is less than 9.0
                    SqlSmoObject.ThrowIfBelowVersion90(sp.TargetServerVersion);

                    if (true == sp.DataType.XmlNamespaces && sp.TargetServerVersion >= SqlServerVersion.Version90 &&
                        oObj.ServerVersion.Major >= 9)
                    {
                        // set the xml collection name if supplied
                        string xmlNamespaceName = oObj.Properties.Get("XmlSchemaNamespace").Value as string;
                        if (null != xmlNamespaceName && xmlNamespaceName.Length > 0)
                        {
                            sb.Append("(");

                            XmlDocumentConstraint docCstr = (XmlDocumentConstraint)oObj.GetPropValueOptional("XmlDocumentConstraint", XmlDocumentConstraint.Default);
                            switch (docCstr)
                            {
                                case XmlDocumentConstraint.Content:
                                    sb.Append("CONTENT "); break;
                                case XmlDocumentConstraint.Document:
                                    sb.Append("DOCUMENT "); break;
                                case XmlDocumentConstraint.Default:
                                    break;
                                default:
                                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("XmlDocumentConstraint"));
                            }

                            string xmlSchemaName = oObj.Properties.Get("XmlSchemaNamespaceSchema").Value as string;
                            if (null != xmlSchemaName && xmlSchemaName.Length > 0 && sp.IncludeScripts.SchemaQualify)
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}.",
                                                    MakeSqlBraket(xmlSchemaName));
                            }

                            sb.AppendFormat(SmoApplication.DefaultCulture, "{0})",
                                                    MakeSqlBraket(xmlNamespaceName));
                        }

                    }
                }
                else if (true == UserDefinedDataType.TypeAllowsLength(sType, oObj.StringComparer))
                {
                    object oLength = p.Get("Length").Value;
                    if (null == oLength)
                    {
                        throw new PropertyNotSetException("Length");
                    }
                    var length = (Int32)oLength;
                    if (sType == "vector")
                    {
                        // Temporary workaround to convert the length of the column to the dimensions for vector types
                        // until sys.columns is updated to include the dimensions of the vector type.
                        // https://msdata.visualstudio.com/SQLToolsAndLibraries/_workitems/edit/3906463
                        // dimensions = (length - 8) / 4
                        // https://learn.microsoft.com/sql/t-sql/data-types/vector-data-type
                        sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", (length - 8) / 4);
                    }
                    else if (length != 0)
                    {
                        if ((sType == "varchar" || sType == "nvarchar" || sType == "varbinary") && length < 0)
                        {
                            sb.Append("(max)");
                        }
                        else
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", oLength);
                        }
                    }
                }
                else if (true == UserDefinedDataType.TypeAllowsPrecisionScale(sType, oObj.StringComparer))
                {
                    object oPrecision = p.Get("NumericPrecision").Value;
                    if (null != oPrecision)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "({0}", oPrecision);
                        object oScale = p.Get("NumericScale").Value;
                        if (null != oScale)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, ", {0}", oScale);
                        }
                        sb.Append(")");
                    }
                }
                else if (true == UserDefinedDataType.TypeAllowsScale(sType, oObj.StringComparer))
                {
                    object oScale = p.Get("NumericScale").Value;
                    if (null != oScale)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", oScale);
                    }
                }
                // scripting the precision of float data type only when the column is getting created.
                // After getting created the precison of float changes to 24 if it was specified between 1 and 24
                // and to 53 if specified between 25 and 53.
                else if (DataType.IsTypeFloatStateCreating(sType, oObj))
                {
                    object oPrecision = p.Get("NumericPrecision").Value;
                    if (null != oPrecision)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", oPrecision);
                    }
                }
            }
            return sb.ToString();
        }


        /// <summary>
        /// Appends script containing the t-sql type definition for the object
        /// passed in as parameter
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="so"></param>
        /// <param name="oObj"></param>
        static internal void AppendScriptTypeDefinition(StringBuilder sb, ScriptingPreferences sp,
                        SqlSmoObject oObj, SqlDataType sqlDataType)
        {
            // we will not script UDT for downlevel servers
            if (sqlDataType == SqlDataType.UserDefinedType)
            {
                ThrowIfBelowVersion90(sp.TargetServerVersion);
            }

            if (!DataType.IsDataTypeSupportedOnCloud(sqlDataType))
            {
                ThrowIfCloud(sp.TargetDatabaseEngineType,
                    string.Format(CultureInfo.CurrentCulture,
                        ExceptionTemplates.PropertyNotSupportedOnCloud(sqlDataType.ToString())));
            }

            if (oObj.State == SqlSmoState.Creating && null == oObj.Properties.Get("DataType").Value)
            {
                throw new PropertyNotSetException("DataType");
            }

            string sType = (string)oObj.Properties["DataType"].Value;

            if (null == sType || sType.Length == 0)
            {
                throw new PropertyNotSetException("DataType");
            }

            if (IsSystemType(oObj,sp))
            {
                sb.Append(GetTypeDefinitionScript(sp, oObj, "DataType", true));
                return;
            }
            String sTypeSchema = (String)oObj.Properties.Get("DataTypeSchema").Value;

            string sSystemType = oObj.GetPropValueOptional("SystemType") as string;

            // check if we need to convert the type to its base type
            if (sp.DataType.UserDefinedDataTypesToBaseType)
            {
                // if the object has not been created yet we can't get the
                // base type from its property bag and we need to try to retrieve
                // the base type from the UserDefinedDataTypes collection
                if (null == sSystemType)
                {
                    Database thisDb = oObj.GetServerObject().Databases[oObj.GetDBName()];
                    UserDefinedDataType uddt;
                    if (null == sTypeSchema || sTypeSchema.Length == 0)
                    {
                        uddt = thisDb.UserDefinedDataTypes[sType];
                    }
                    else
                    {
                        uddt = thisDb.UserDefinedDataTypes[sType, sTypeSchema];
                    }

                    if (null != uddt)
                    {
                        // if we managed to find the UserDefinedDataType we set the base type
                        // in the property bag accordingly
                        oObj.Properties.Get("SystemType").SetValue(uddt.GetPropValueOptional("SystemType") as string);
                        oObj.Properties.Get("SystemType").SetRetrieved(true);
                        sSystemType = (string)oObj.Properties.Get("SystemType").Value;
                    }
                }
                // if we have the base type we can script the object properly
                // we will not error out otherwise becase we can still generate a script
                // however we won't be able to convert to the base type
                else if (null != sSystemType && sSystemType.Length > 0) //check against udt
                {
                    sb.Append(GetTypeDefinitionScript(sp, oObj, "SystemType", true));
                    return;
                }
            }

            // script only the name of the type if we don't have the schema
            // or we're on 8.0
            if (SqlServerVersion.Version80 == sp.TargetServerVersion ||
                oObj.ServerVersion.Major < 9 ||
                null == sTypeSchema)
            {
                sb.AppendFormat("[{0}]", SqlBraket(sType));
                return;
            }
            sb.AppendFormat("[{0}].[{1}]", SqlBraket(sTypeSchema), SqlBraket(sType));
        }

        /// <summary>
        /// Returns true if the object's type is a SqlServer system data type
        /// Will return false for UserDefinedDataType and UserDefinedType
        /// </summary>
        /// <param name="oObj"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        static internal bool IsSystemType(SqlSmoObject oObj, ScriptingPreferences sp)
        {
            String type = oObj.Properties.Get("DataType").Value as string;
            return DataType.IsSystemDataType(DataType.SqlToEnum(type), sp.TargetServerVersion, sp.TargetDatabaseEngineType, sp.TargetDatabaseEngineEdition);
        }

        /// <summary>
        /// Drops the object.
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


        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
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
                AddExistsCheck(sb, "", sp);
            }

            if (SqlServerVersion.Version90 > sp.TargetServerVersion)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_droptype @typename={0}",
                    FormatFullNameForScripting(sp, false));
            }
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP TYPE {0}{1}",
                    (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                    FormatFullNameForScripting(sp));
            }

            queries.Add(sb.ToString());
        }

        private void AddExistsCheck(StringBuilder sb, string prefix, ScriptingPreferences sp)
        {
            if (sp.TargetServerVersion < SqlServerVersion.Version90 || !sp.IncludeScripts.SchemaQualify)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDDT80,
                                prefix, FormatFullNameForScripting(sp, false));
            }
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDDT90,
                                prefix,
                                SqlString(ScriptName.Length > 0 ? ScriptName : Name),
                                SqlString(ScriptSchema.Length > 0 ? ScriptSchema : Schema));
            }
            sb.Append(sp.NewLine);

        }

        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
        }

        public void BindRule(string ruleSchema, string ruleName)
        {
            BindRule(ruleSchema, ruleName, false);
        }

        public void BindRule(string ruleSchema, string ruleName, bool bindColumns)
        {
            CheckObjectState(true);
            BindRuleImpl(ruleSchema, ruleName, bindColumns);
        }

        public void UnbindRule()
        {
            UnbindRule(false);
        }

        public void UnbindRule(bool bindColumns)
        {
            CheckObjectState(true);
            UnbindRuleImpl(bindColumns);
        }

        public void BindDefault(string defaultSchema, string defaultName)
        {
            BindDefault(defaultSchema, defaultName, false);
        }

        public void BindDefault(string defaultSchema, string defaultName, bool bindColumns)
        {
            CheckObjectState(true);
            BindDefaultImpl(defaultSchema, defaultName, bindColumns);
        }

        public void UnbindDefault()
        {
            UnbindDefault(false);
        }

        public void UnbindDefault(bool bindColumns)
        {
            CheckObjectState(true);
            UnbindDefaultImpl(bindColumns);
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptOwner(alterQuery, sp);
            }
        }

        /// <summary>
        /// performs actions needed after the object is created
        /// </summary>
        protected override void PostCreate()
        {
            if (this.IsDesignMode)
            {
                string type = (String)this.Properties["SystemType"].Value;

                if (0 == this.StringComparer.Compare(type, "nvarchar") || 0 == this.StringComparer.Compare(type, "varchar") || 0 == this.StringComparer.Compare(type, "varbinary"))
                {
                    int maxLength = this.Length;

                    if (this.Length < 0)
                    {
                        maxLength = -1;
                        this.Length = -1;
                    }
                    else
                    {
                        if (0 == this.StringComparer.Compare(type, "nvarchar"))
                        {
                            maxLength = 2 * maxLength;
                        }
                    }
                    int maxLengthSet = this.Properties.LookupID("MaxLength", PropertyAccessPurpose.Write);
                    this.Properties.SetValue(maxLengthSet, (System.Int16)maxLength);
                    this.Properties.SetRetrieved(maxLengthSet, true);
                }
            }
        }


        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
        if (Cmn.DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
            {
                return null;
            }

            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        public SqlSmoObject[] EnumBoundColumns()
        {
            try
            {
                CheckObjectState();
                // make the request Urn
                DataTable tbl = this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/BoundColumn"));
                // allocate result
                SqlSmoObject[] results = new SqlSmoObject[tbl.Rows.Count];
                Urn thisurn = this.Urn;
                int idx = 0;
                // get all objects
                foreach (DataRow dr in tbl.Rows)
                {
                    string objecturn = string.Format(SmoApplication.DefaultCulture, "{0}/Table[@Name='{1}' and @Schema='{2}']/Column[@Name='{3}']",
                                                thisurn.Parent, Urn.EscapeString((string)dr["ObjectName"]),
                                                Urn.EscapeString((string)dr["ObjectSchema"]), Urn.EscapeString((string)dr["Name"]));
                    results[idx++] = GetServerObject().GetSmoObject(objecturn);
                }

                return results;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumBoundColumns, this, e);
            }
        }

        static internal bool TypeAllowsLength(string type, StringComparer comparer)
        {
            if (0 == comparer.Compare(type, "nvarchar") ||
                0 == comparer.Compare(type, "varchar") ||
                0 == comparer.Compare(type, "binary") ||
                0 == comparer.Compare(type, "varbinary") ||
                0 == comparer.Compare(type, "nchar") ||
                0 == comparer.Compare(type, "char") ||
                0 == comparer.Compare(type, "vector"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns true if the type can have a collation that needs to be
        /// scripted.
        /// </summary>
        /// <param name="type">The name of the SqlServer type</param>
        /// <param name="comparer">comparer used to do comparisons</param>
        /// <returns></returns>
        static internal bool TypeAllowsCollation(string type, StringComparer comparer)
        {
            if (0 == comparer.Compare(type, "nvarchar") ||
                0 == comparer.Compare(type, "varchar") ||
                0 == comparer.Compare(type, "nchar") ||
                0 == comparer.Compare(type, "text") ||
                0 == comparer.Compare(type, "ntext") ||
                0 == comparer.Compare(type, "char") ||
                0 == comparer.Compare(type, "sysname"))
            {
                return true;
            }

            return false;
        }

        static internal bool TypeAllowsPrecisionScale(string type, StringComparer comparer)
        {
            if (0 == comparer.Compare(type, "numeric") ||
                0 == comparer.Compare(type, "decimal"))
            {
                return true;
            }
            return false;
        }

        static internal bool TypeAllowsScale(string type, StringComparer comparer)
        {
            if (0 == comparer.Compare(type, "time") ||
                0 == comparer.Compare(type, "datetimeoffset") ||
                0 == comparer.Compare(type, "datetime2"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Renames the object
        /// </summary>
        /// <param name="newname">New UDDT name</param>
        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC {0}.dbo.sp_rename @objname = N'{1}', @newname = N'{2}', @objtype = N'USERDATATYPE'",
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

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // on 8.0 we do not have permissions on uddts
            if (sp.TargetServerVersion == SqlServerVersion.Version80 ||
                this.ServerVersion.Major == 8)
            {
                return;
            }

            base.AddScriptPermission(query, sp);
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
            string[] fields = {
                                                "Default",
                                                "DefaultSchema",
                                                "Rule",
                                                "RuleSchema",
                                                "Nullable",
                                                "Length",
                                                "MaxLength",
                                                "NumericPrecision",
                                                "NumericScale",
                                                "SystemType",
                                                "ID",
                                                "Owner",
                                                "IsSchemaOwned"};
            List<string> list = GetSupportedScriptFields(typeof(UserDefinedDataType.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}



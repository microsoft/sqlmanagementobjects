// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    public partial class View : TableViewBase, Cmn.ICreatable, Cmn.ICreateOrAlterable, Cmn.IAlterable,
        Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, IExtendedProperties, ITextObject, IViewOptions
    {
        internal View(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "View";
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Index),  SfcObjectFlags.Deploy)]
        public override IndexCollection Indexes
        {
            get { return base.Indexes; }
        }

        private ResumableIndexCollection m_ResumableIndexes;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Index), SfcObjectFlags.Deploy)]
        public ResumableIndexCollection ResumableIndexes
        {
            get
            {
                CheckObjectState();
                if (null == m_ResumableIndexes)
                {
                    m_ResumableIndexes = new ResumableIndexCollection(this);
                }
                return m_ResumableIndexes;
            }
        }
            
        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
        }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            if (this.State != SqlSmoState.Creating &&
                this.IsEncrypted)
            {
                ThrowIfBelowVersion90(sp.TargetServerVersionInternal,
                    ExceptionTemplates.EncryptedViewsFunctionsDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetSqlServerName(sp)));

                ThrowIfCloud(sp.TargetDatabaseEngineType,
                    ExceptionTemplates.EncryptedViewsFunctionsDownlevel(
                        FormatFullNameForScripting(sp, true),
                        GetDatabaseEngineName(sp)));
            }

            if (sp.OldOptions.PrimaryObject)
            {
                // retrieve the DDL 
                GetDDL(queries, sp, ScriptHeaderType.ScriptHeaderForCreate);

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
            GetDDL(queries, sp, ScriptHeaderType.ScriptHeaderForCreateOrAlter);
            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptOwner(queries, sp);
            }
        }

        public void Alter()
        {
            base.AlterImpl();
        SetSchemaOwned();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            if (IsObjectDirty())
            {
                if (ShouldScriptBodyAtAlter())
                {
                    GetDDL(queries, sp, ScriptHeaderType.ScriptHeaderForAlter);
                }
                if (sp.IncludeScripts.Owner)
                {
                    //script change owner if dirty
                    ScriptOwner(queries, sp);
                }
            }
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || IsCollectionDirty(Columns);
        }

        private string GetIfNotExistString(bool forCreate, ScriptingPreferences sp)
        {
            // perform check for existing object
            string checkString;
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
            {
                checkString = Scripts.INCLUDE_EXISTS_VIEW90;
            }
            else
            {
                checkString = Scripts.INCLUDE_EXISTS_VIEW80;
            }

            return String.Format(SmoApplication.DefaultCulture, checkString, forCreate ? "NOT" : "", SqlString(FormatFullNameForScripting(sp)));
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

            bool supportsIfExists = (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                ? !sp.TargetEngineIsAzureSqlDw() : 
                  sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130;

            if (sp.IncludeScripts.ExistenceCheck && !supportsIfExists)
            {
                sb.AppendLine(GetIfNotExistString( /* forCreate = */ false, sp));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP VIEW {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && supportsIfExists) ? "IF EXISTS " : string.Empty,
                sFullScriptingName);

            queries.Add(sb.ToString());
        }

        public void Rename(string newName)
        {
            RenameImpl(newName);
        }


        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_rename N'{0}', N'{1}', 'OBJECT'",
                                    SqlString(this.FullQualifiedName), SqlString(newName)));
        }

        private void GetDDL(StringCollection queries, ScriptingPreferences sp, ScriptHeaderType scriptHeaderType)
        {
            bool bCreate = IsCreate(scriptHeaderType);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // retrieve full scripting name
            string sFullScriptingName = FormatFullNameForScripting(sp);

            bool fAnsiNullsExists = false;
            bool fQuotedIdentifierExists = false;
            bool fServerAnsiNulls = false;
            bool fServerQuotedIdentifier = false;

            if (!sp.OldOptions.DdlHeaderOnly && !sp.OldOptions.DdlBodyOnly)
            {
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                            UrnSuffix, sFullScriptingName, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                fAnsiNullsExists = ( null != Properties.Get("AnsiNullsStatus").Value);
                fQuotedIdentifierExists = ( null != Properties.Get("QuotedIdentifierStatus").Value );
                
                if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    // save server settings first
                    Server svr = (Server)ParentColl.ParentInstance.ParentColl.ParentInstance;
                    fServerAnsiNulls = (bool)svr.UserOptions.AnsiNulls;
                    fServerQuotedIdentifier = (bool)svr.UserOptions.QuotedIdentifier;
                }

                if (fAnsiNullsExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_ANSI_NULLS,(bool)Properties["AnsiNullsStatus"].Value?Globals.On:Globals.Off);
                    queries.Add( sb.ToString() );
                    sb.Length = 0;
                }

                if (fQuotedIdentifierExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_QUOTED_IDENTIFIER, (bool)Properties["QuotedIdentifierStatus"].Value ? Globals.On : Globals.Off);
                    queries.Add(sb.ToString());
                    sb.Length = 0;
                }
            }

            if (false == this.TextMode || (true == sp.OldOptions.EnforceScriptingPreferences && true == sp.OldOptions.NoViewColumns))
            {
                StringBuilder sbForSpExec = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                if (!sp.OldOptions.DdlBodyOnly)
                {
                    if (bCreate && sp.IncludeScripts.ExistenceCheck)
                    {
                        sb.AppendLine(GetIfNotExistString( /* forCreate = */ true, sp));
                        sb.AppendLine("EXECUTE dbo.sp_executesql N'");
                    }

                    switch (scriptHeaderType)
                    {
                        case ScriptHeaderType.ScriptHeaderForCreate:
                            sbForSpExec.AppendFormat(SmoApplication.DefaultCulture, "{0} VIEW {1} ", Scripts.CREATE, sFullScriptingName); 
                            break;
                        case ScriptHeaderType.ScriptHeaderForAlter:
                            sbForSpExec.AppendFormat(SmoApplication.DefaultCulture, "{0} VIEW {1} ", Scripts.ALTER, sFullScriptingName); 
                            break;
                        case ScriptHeaderType.ScriptHeaderForCreateOrAlter:
                            ThrowIfCreateOrAlterUnsupported(sp.TargetServerVersionInternal,
                                ExceptionTemplates.CreateOrAlterDownlevel(
                                    "View",
                                    GetSqlServerName(sp)));

                            sbForSpExec.AppendFormat(SmoApplication.DefaultCulture, "{0} VIEW {1} ", Scripts.CREATE_OR_ALTER, sFullScriptingName);
                            break;
                        default:
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(scriptHeaderType.ToString()));
                    }

                    // generate the list of columns - if they have been specified
                    if (Columns.Count > 0 && !sp.OldOptions.NoViewColumns
                        && ((bCreate && (null == GetPropValueOptional("HasColumnSpecification") || true == (bool)GetPropValueOptional("HasColumnSpecification"))
                        || (!bCreate && true == (bool)GetPropValueOptional("HasColumnSpecification")))))
                    {
                        StringBuilder colList = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                        int colCount = 0;
                        foreach (Column column in Columns)
                        {
                            // prepare to script column only if in direct execution mode or not explicitly directed to ignore it
                            if (sp.ScriptForCreateDrop || !column.IgnoreForScripting)
                            {
                                if (colCount++ > 0)
                                {
                                    colList.Append(Globals.comma);
                                    colList.Append(sp.NewLine);
                                }

                                colList.Append(Globals.tab);
                                colList.Append(column.FormatFullNameForScripting(sp));
                            }
                        }
                        if (colList.Length > 0)
                        {
                            sbForSpExec.AppendFormat("({0})", colList.ToString());
                            sbForSpExec.AppendLine();
                        }
                    }

                    bool bNeedsComma = false;
                    if (this.ServerVersion.Major > 7 && sp.TargetServerVersionInternal > SqlServerVersionInternal.Version70)
                    {

                        AppendWithOption(sbForSpExec, "IsSchemaBound", "SCHEMABINDING", ref bNeedsComma);
                    }

                    if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
                    {
                        AppendWithOption(sbForSpExec, "IsEncrypted", "ENCRYPTION", ref bNeedsComma);
                    }

                    if (ServerVersion.Major >= 9 && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
                    {
                        AppendWithOption(sbForSpExec, "ReturnsViewMetadata", "VIEW_METADATA", ref bNeedsComma);
                    }


                    if (bNeedsComma)
                    {
                        sbForSpExec.AppendLine();
                    }

                    sbForSpExec.AppendLine(" AS ");
                }

                if (!sp.OldOptions.DdlHeaderOnly)
                {
                    sbForSpExec.AppendLine(GetTextBody(true));
                }

                if (bCreate && sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendLine(SqlString(sbForSpExec.ToString()));
                    sb.Append("'");
                }
                else
                {
                    sb.Append(sbForSpExec.ToString());
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
                string body = GetTextForScript(sp, new String[] { "view" }, forceCheckNameAndManipulateIfRequired: true, scriptHeaderType: scriptHeaderType);

                if (bCreate && sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendLine(GetIfNotExistString( /* forCreate = */ true, sp));
                    sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC dbo.sp_executesql @statement = {0} ", MakeSqlString(body));
                }
                else
                {
                    sb.Append(body);
                }
            }

            // add the ddl to create the object
            queries.Add(sb.ToString());

            sb.Length = 0;

        }

        bool ShouldScriptBodyAtAlter()
        {
            if (GetIsTextDirty())
            {
                return true;
            }
            StringCollection sc = new StringCollection();
            sc.Add("AnsiNullsStatus");
            sc.Add("QuotedIdentifierStatus");
            sc.Add("IsSchemaBound");
            sc.Add("IsEncrypted");
            if (this.ServerVersion.Major > 8)
            {
                sc.Add("ReturnsViewMetadata");
            }
            if (this.Properties.ArePropertiesDirty(sc))
            {
                return true;
            }
            if (IsCollectionDirty(Columns))
            {
                return true;
            }

            return false;
        }

        public DataTable EnumColumns()
        {
            try
            {
                CheckObjectState();
                Request req = new Request(this.Urn.Value + "/Column");
                req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumColumns, this, e);
            }
        }

        // I include 'refreshView' so that people will be able to correlate it with sp_refreshview
        public void Refresh(bool refreshViewMetadata)
        {
            base.Refresh();
            try
            {
                if (refreshViewMetadata)
                {
                    Database db = (Database)ParentColl.ParentInstance;
                    db.ExecuteNonQuery(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_refreshview @viewname=N'{0}'", SqlString(FullQualifiedName)));
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Refresh, this, e);
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return GetPropagateInfoImpl(false);
        }

        private PropagateInfo[] GetPropagateInfoImpl(bool forDiscovery)
        {
            ArrayList propInfo = new ArrayList();

            propInfo.Add(new PropagateInfo(Columns, false, true));
            propInfo.Add(new PropagateInfo(Statistics, true, Statistic.UrnSuffix));

            if (forDiscovery)
            {
                // During discovery the Indexes collection is expanded so let's optimize it
                if (State == SqlSmoState.Existing)
                {
                    InitChildLevel(nameof(Index), new ScriptingPreferences(this), true);
                }
                (new IndexPropagateInfo(Indexes)).PropagateInfo(propInfo);
            }
            else
            {
                propInfo.Add(new PropagateInfo(Indexes, true, Index.UrnSuffix));
            }

            propInfo.Add(new PropagateInfo(Triggers, true, Trigger.UrnSuffix));

            if (this.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
            {
                propInfo.Add(new PropagateInfo(ExtendedProperties, true, ExtendedProperty.UrnSuffix));
            }

            if (this.DatabaseEngineType == DatabaseEngineType.Standalone && ServerVersion.Major >= 9)
            {
                propInfo.Add(new PropagateInfo(FullTextIndex, true, FullTextIndex.UrnSuffix));
            }

            PropagateInfo[] retArr = new PropagateInfo[propInfo.Count];
            propInfo.CopyTo(retArr, 0);

            return retArr;
        }

        internal override PropagateInfo[] GetPropagateInfoForDiscovery(PropagateAction action)
        {
            return GetPropagateInfoImpl(true);
        }

        /// <summary>
        /// Overrides the permission scripting.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // on 7.0 we do not have permissions on views
            if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version70 ||
                this.ServerVersion.Major == 7)
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
            Cmn.ServerVersion version, 
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition, 
            bool defaultTextMode)
        {
           if (!defaultTextMode)
                {
                    string[] fields = {
                                            nameof(AnsiNullsStatus),
                                            nameof(DwMaterializedViewDistribution),
                                            nameof(HasColumnSpecification),
                                            nameof(ID),
                                            nameof(IsSchemaBound),
                                            nameof(IsEncrypted),
                                            nameof(IsSystemObject),
                                            nameof(IsSchemaOwned),
                                            nameof(Owner),
                                            nameof(QuotedIdentifierStatus),
                                            nameof(ReturnsViewMetadata)};
                    List<string> list = GetSupportedScriptFields(typeof(View.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                    list.Add("Text");
                    return list.ToArray();
                }
                else
                {
                    string[] fields = {   
                                            nameof(AnsiNullsStatus),
                                            nameof(IsSystemObject),
                                            nameof(ID),
                                            nameof(QuotedIdentifierStatus)};
                    List<string> list = GetSupportedScriptFields(typeof(View.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
                    list.Add("Text");
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
            return GetTextHeader(scriptHeaderType);;
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
            set { CheckObjectState(); SetTextMode(value, new SmoCollectionBase [] { Columns }); }
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
                        case "IsEncrypted":
                            Validate_set_TextObjectDDLProperty(prop, value);
                            break;
                        case "HasColumnSpecification": goto case "IsEncrypted";

                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                case 8:
                    switch (prop.Name)
                    {
                        case "IsSchemaBound":
                            Validate_set_TextObjectDDLProperty(prop, value);
                            break;
                        case "IsEncrypted": goto case "IsSchemaBound";
                        case "HasColumnSpecification": goto case "IsSchemaBound";

                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                case 9: goto case 8;
                default: goto case 9;
            }
        }



        #endregion
    }
}



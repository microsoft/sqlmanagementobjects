// Copyright (c) Microsoft.
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
    public partial class ForeignKey : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists,
                                    Cmn.IMarkForDrop, Cmn.IAlterable, Cmn.IRenamable, IExtendedProperties, IScriptable
    {
        internal ForeignKey(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ForeignKey";
            }
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                if (IsDesignMode && GetIsSystemNamed() && this.State == SqlSmoState.Creating)
                {
                    return null;
                }
                return base.Name;
            }
            set
            {
                base.Name = value;
                if (ParentColl != null)
                {
                    SetIsSystemNamed(false);
                }
            }
        }

        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public System.Boolean IsSystemNamed
        {
            get
            {
                if (ParentColl != null && IsDesignMode && this.State != SqlSmoState.Existing)
                {
                    throw new PropertyNotSetException("IsSystemNamed");
                }
                return (System.Boolean)this.Properties.GetValueWithNullReplacement("IsSystemNamed");
            }
        }

        internal override void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && null != this.ParentColl && (!key.IsNull || IsDesignMode))
            {
                SetState(SqlSmoState.Creating);
                if (key.IsNull)
                {
                    AutoGenerateName();
                }
                else
                {
                    SetIsSystemNamed(false);
                }
            }
        }

        private ForeignKeyColumnCollection m_Columns;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(ForeignKeyColumn), SfcObjectFlags.Design | SfcObjectFlags.NaturalOrder)]
        public ForeignKeyColumnCollection Columns
        {
            get
            {
                CheckObjectState();
                if (null == m_Columns)
                {
                    m_Columns = new ForeignKeyColumnCollection(this);
                }
                return m_Columns;
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_Columns)
            {
                m_Columns.MarkAllDropped();
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ConstraintScriptCreate(ScriptDdlBody(sp), createQuery, sp);
        }

        internal String ScriptDdlBody(ScriptingPreferences sp)
        {
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            AddConstraintName(sb, sp);
            sb.Append("FOREIGN KEY");
            sb.Append(Globals.LParen);

            // go thru referencing columns
            if (Columns.Count == 0)
            {
                throw new SmoException(ExceptionTemplates.NoObjectWithoutColumns("ForeignKey"));
            }

            bool fFirstColumn = true;
            foreach (ForeignKeyColumn colFrom in Columns)
            {
                if (fFirstColumn)
                {
                    fFirstColumn = false;
                }
                else
                {
                    sb.Append(Globals.commaspace);
                }


                // we check to see if the specified column exists,
                // first we need to know which type is the parent, table or view
                // for views, we leave it unchecked for the moment, we'll have to do it later
                Column colBase = null;
                if (typeof(Table).Equals(ParentColl.ParentInstance.GetType()))
                {
                    colBase = ((Table)ParentColl.ParentInstance).Columns[colFrom.Name];
                    if (null == colBase)
                    {
                        // the column does not exist, so we need to abort this scripting
                        throw new SmoException(ExceptionTemplates.ObjectRefsNonexCol(UrnSuffix, Name, "[" + SqlStringBraket(ParentColl.ParentInstance.InternalName) + "].[" + SqlStringBraket(colFrom.Name) + "]"));
                    }

                    // if this column is going to be ignored for scripting, we cannot script this index
                    // since it references the above mentioned column
                    if (colBase.IgnoreForScripting)
                    {
                        return "";
                    }
                }

                // use proper name for scripting
                if (null != colBase && colBase.ScriptName.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(colBase.ScriptName));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(colFrom.Name));
                }

            }

            sb.Append(Globals.RParen);
            sb.Append(sp.NewLine);

            string sFullTableName = "";
            string sReferencedTable = "";
            string sReferencedTableSchema = "";
            if (!sp.ForDirectExecution && this.ScriptReferencedTable.Length > 0)
            {
                sReferencedTable = this.ScriptReferencedTable;
            }
            else
            {
                sReferencedTable = (string)GetPropValue("ReferencedTable");
            }
            if (!sp.ForDirectExecution && (sp.IncludeScripts.SchemaQualifyForeignKeysReferences || sp.IncludeScripts.SchemaQualify) &&
                this.ScriptReferencedTableSchema.Length > 0)
            {
                sReferencedTableSchema = this.ScriptReferencedTableSchema;
            }
            else if ((!sp.ForDirectExecution && (sp.IncludeScripts.SchemaQualifyForeignKeysReferences || sp.IncludeScripts.SchemaQualify) ||
                    sp.ForDirectExecution))
            {
                string sSchemaProp = (string)GetPropValueOptional("ReferencedTableSchema");
                if (null != sSchemaProp)
                {
                    sReferencedTableSchema = sSchemaProp;
                }
            }

            //if in create script or script option to qualify is true, then qualify
            if (sReferencedTableSchema.Length > 0)
            {
                //it can be set only on scripting so we will have a valid ReferencedTableSchema
                sFullTableName = String.Format(SmoApplication.DefaultCulture, "[{0}].", SqlBraket(sReferencedTableSchema));
            }
            sFullTableName += String.Format(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(sReferencedTable));
            sb.AppendFormat(SmoApplication.DefaultCulture, "REFERENCES {0} (", sFullTableName);

            // go thru referenced columns
            fFirstColumn = true;
            foreach (ForeignKeyColumn colTo in Columns)
            {
                if (fFirstColumn)
                {
                    fFirstColumn = false;
                }
                else
                {
                    sb.Append(Globals.commaspace);
                }

                sb.AppendFormat("[{0}]", SqlBraket(colTo.ReferencedColumn));
            }

            sb.Append(Globals.RParen);

            //add update referential action
            AddReferentioalAction(sp, sb, "UpdateAction", "ON UPDATE");
            //add delete referential action
            AddReferentioalAction(sp, sb, "DeleteAction", "ON DELETE");

            if (IsSupportedProperty("NotForReplication", sp))
            {
                Property pNotRepl = Properties.Get("NotForReplication");
                if (null != pNotRepl.Value)
                {
                    if ((bool)pNotRepl.Value && !IsCloudAtSrcOrDest(this.DatabaseEngineType,sp.TargetDatabaseEngineType))
                    {
                        sb.Append(sp.NewLine);
                        sb.Append("NOT FOR REPLICATION ");
                    }
                }
            }


            return sb.ToString();
        }

        internal override string GetScriptIncludeExists(ScriptingPreferences sp, string tableName, bool forCreate)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("N'");
            if (sp.IncludeScripts.SchemaQualify)
            {
                string schema = this.Parent.GetSchema(sp);
                if (schema.Length > 0)
                {
                    sb.Append(MakeSqlBraket(SqlString(schema)));
                    sb.Append(Globals.Dot);
                }
            }
            sb.Append(SqlString(FormatFullNameForScripting(sp)));
            sb.Append("'");

            if (sp.TargetServerVersionInternal > SqlServerVersionInternal.Version80)
            {
                return string.Format(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FOREIGN_KEY90, forCreate ? "NOT" : "", sb.ToString(), MakeSqlString(tableName));
            }
            else
            {
                return string.Format(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_FOREIGN_KEY80, forCreate ? "NOT" : "", sb.ToString());
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

        /// see fk syntax:
        /// | [ FOREIGN KEY ]
        ///		REFERENCES [ schema_name . ] referenced_table_name [ ( ref_column ) ]
        ///		[ ON DELETE { NO ACTION | CASCADE | SET NULL | SET DEFAULT } ]
        ///		[ ON UPDATE { NO ACTION | CASCADE | SET NULL | SET DEFAULT } ]
        void AddReferentioalAction(ScriptingPreferences sp, StringBuilder sb, string propertyName, string action)
        {
            ForeignKeyAction fkAction = (ForeignKeyAction)this.GetPropValueOptional(propertyName, ForeignKeyAction.NoAction);
            switch (fkAction)
            {
                case ForeignKeyAction.NoAction:
                    break;
                case ForeignKeyAction.Cascade:
                    sb.Append(sp.NewLine);
                    sb.Append(action);
                    sb.Append(" CASCADE");
                    break;
                case ForeignKeyAction.SetNull:
                    if (SqlServerVersionInternal.Version90 > sp.TargetServerVersionInternal) //only supported on Yukon
                    {
                        throw new WrongPropertyValueException(this.Properties.Get(propertyName));
                    }
                    sb.Append(sp.NewLine);
                    sb.Append(action); sb.Append(" SET NULL");
                    break;
                case ForeignKeyAction.SetDefault:
                    if (SqlServerVersionInternal.Version90 > sp.TargetServerVersionInternal) //only supported on Yukon
                    {
                        throw new WrongPropertyValueException(this.Properties.Get(propertyName));
                    }
                    sb.Append(sp.NewLine);
                    sb.Append(action); sb.Append(" SET DEFAULT");
                    break;
                default:
                    throw new WrongPropertyValueException(this.Properties.Get(propertyName));
            }
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            TableViewBase table = (TableViewBase)ParentColl.ParentInstance;
            string sTableName = table.FormatFullNameForScripting(sp);
            string sForeignKeyName = FormatFullNameForScripting(sp);
            StringBuilder stmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            bool isTargetServerVersionSQl13OrLater = VersionUtils.IsTargetServerVersionSQl13OrLater(sp.TargetServerVersionInternal);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                // Existence of parent table is not checked when ALTER TABLE DROP CONSTRAINT IF EXISTS syntax is used.
                // Check is added here to keep behavior same as in previous versions.
                //
                if (isTargetServerVersionSQl13OrLater)
                {
                    stmt.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "", SqlString(sTableName));
                }
                else
                {
                    stmt.Append(GetScriptIncludeExists(sp, sTableName, false));
                }
                stmt.Append(sp.NewLine);
            }

            stmt.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP CONSTRAINT {1}{2}",
                sTableName,
                (sp.IncludeScripts.ExistenceCheck && isTargetServerVersionSQl13OrLater) ? "IF EXISTS " : string.Empty,
                sForeignKeyName);
            queries.Add(stmt.ToString());
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            ConstraintScriptAlter(alterQuery, sp);
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
        /// Rename the index
        /// </summary>
        /// <param name="newName"></param>
        public void Rename(string newName)
        {
            base.RenameImpl(newName);
        }

        /// <summary>
        /// Generates script for the Rename operation.
        /// </summary>
        /// <param name="renameQuery"></param>
        /// <param name="so"></param>
        /// <param name="newName"></param>
        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));

            //ForeignKey.FullQualifiedName does not override NamedSmoObject.FullQualifiedName to include the schema name too
            //so we are computing the qualified name here
            string schemaName = ((SchemaObjectKey)this.ParentColl.ParentInstance.key).Schema;
            string qualifiedName = string.Format(SmoApplication.DefaultCulture, "{0}.{1}",
                MakeSqlBraket(schemaName),
                this.FullQualifiedName);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_rename N'{0}', N'{1}', N'OBJECT'",
                SqlString(qualifiedName),
                SqlString(newName)));
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

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (this.DatabaseEngineType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
            }
            return null;
        }

        private string m_sReferencedTable = String.Empty;

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public string ScriptReferencedTable
        {
            get
            {
                CheckObjectState();
                return m_sReferencedTable;
            }
            set
            {
                CheckObjectState();
                if( null == value )
                {
                    throw new SmoException( ExceptionTemplates.InnerException, new ArgumentNullException("ScriptReferencedTable"));
                }

                m_sReferencedTable = value;
            }
        }

        private string m_sReferencedTableSchema = String.Empty;

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public string ScriptReferencedTableSchema
        {
            get
            {
                CheckObjectState();
                return m_sReferencedTableSchema;
            }
            set
            {
                CheckObjectState();
                if( null == value )
                {
                    throw new SmoException( ExceptionTemplates.InnerException, new ArgumentNullException("ScriptReferencedTableSchema"));
                }

                m_sReferencedTableSchema = value;
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
           string[] fields = {
                                        "IsSystemNamed",
                                        "DeleteAction",
                                        "UpdateAction",
                                        "ReferencedTable",
                                        "ReferencedTableSchema",
                                        "IsChecked",
                                        "IsEnabled",
                                        "NotForReplication",
                                        "IsFileTableDefined"
                             };
           List<string> list = GetSupportedScriptFields(typeof(ForeignKey.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
           return list.ToArray();
        }
    }
}



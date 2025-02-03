// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

// History
//
// Fixed bug 403691: Cannot create column constraint in table type definition of CREATE FUNCTION for multistatement table-valued function
// We were scripting constraints as 'CONSTRAINT [Check on UDF] CHECK ([id] > 10000)'. The right syntax is
// 'CHECK  ([id] > 10000)' -- i.e. same but without the name of the constraint.


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class Check : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, Cmn.IMarkForDrop, Cmn.IAlterable, IExtendedProperties, IScriptable
    {
        internal Check(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Check";
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

        public StringCollection Script()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null);
            }

            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null);
            }

            return ScriptImpl(scriptingOptions);
        }

        public void Create()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.UDTTChecksCannotBeModified);
            }

            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }

            ConstraintScriptCreate(ScriptDdlBodyWithName(sp), queries, sp);
        }

        public void Alter()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.UDTTChecksCannotBeModified);
            }

            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }

            ConstraintScriptAlter(queries, sp);
        }

        private String ScriptDdlBodyWithName(ScriptingPreferences sp)
        {
            return ScriptDdlBodyWorker(sp, /*withConstraintName = */ true);
        }

        internal String ScriptDdlBodyWithoutName(ScriptingPreferences sp)
        {
            return ScriptDdlBodyWorker(sp, /*withConstraintName = */ false);
        }

        internal String ScriptDdlBody(ScriptingPreferences sp)
        {
            if (string.IsNullOrEmpty(Name))
            {
                return ScriptDdlBodyWithoutName(sp);
            }
            return ScriptDdlBodyWithName(sp);
        }

        private String ScriptDdlBodyWorker(ScriptingPreferences sp, bool withConstraintName)
        {
            CheckObjectState();
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool excludeReplication = false;
            if (IsSupportedProperty("NotForReplication",sp))
            {
                Property propExcludeReplication = Properties.Get("NotForReplication");
                if (propExcludeReplication.Value != null && !IsCloudAtSrcOrDest(this.DatabaseEngineType,sp.TargetDatabaseEngineType))
                {
                    excludeReplication = (bool)propExcludeReplication.Value;
                }
            }

            if (withConstraintName)
            {
                AddConstraintName(sb, sp);
            }

            String sText = (string)this.GetPropValue("Text");
            // we should try here to check for matching parentheses in TextBody
            sb.AppendFormat(SmoApplication.DefaultCulture, "CHECK {0} ({1})",
                            excludeReplication ? "NOT FOR REPLICATION" : "",
                            sText);

            return sb.ToString();
        }

        internal override string GetScriptIncludeExists(ScriptingPreferences sp, string tableName, bool forCreate)
        {
            string fullCheckName = FormatFullNameForScripting(sp);
            if (this.Parent != null && this.Parent is Table)
            {
                fullCheckName = MakeSqlBraket(((Table)this.Parent).Schema) + "." + fullCheckName;
            }
            fullCheckName = MakeSqlString(fullCheckName);

            if (sp.TargetServerVersion > SqlServerVersion.Version80)
            {
                return string.Format(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_CHECK90, forCreate ? "NOT" : "", fullCheckName, MakeSqlString(tableName));
            }
            else
            {
                return string.Format(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_CHECK80, forCreate ? "NOT" : "", fullCheckName);
            }
        }

        /// <summary>
        /// Renames the object
        /// </summary>
        /// <param name="newname">New check constraint name</param>
        public void Rename(string newname)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.UDTTChecksCannotBeModified);
            }

            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC {0}.dbo.sp_rename @objname = N'{1}.{2}', @newname = N'{3}', @objtype = N'OBJECT'",
                                MakeSqlBraket(GetDBName()),
                                SqlString(MakeSqlBraket(((ScriptSchemaObjectBase)this.ParentColl.ParentInstance).Schema)),
                                SqlString(this.FullQualifiedName),
                                SqlString(newName)));
        }

        public void Drop()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.UDTTChecksCannotBeModified);
            }

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
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            var inlineIfExists = sp.TargetDatabaseEngineEdition == Cmn.DatabaseEngineEdition.SqlDatabase || VersionUtils.IsTargetServerVersionSQl13OrLater(sp.TargetServerVersion);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                TableViewBase table = (TableViewBase)ParentColl.ParentInstance;
                string sTableName = table.FormatFullNameForScripting(sp);

                // Existence of parent table is not checked when ALTER TABLE DROP CONSTRAINT IF EXISTS syntax is used.
                // Check is added here to keep behavior same as in previous versions.
                //
                if (inlineIfExists)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "", SqlString(sTableName));
                }
                else
                {
                    sb.Append(GetScriptIncludeExists(sp, sTableName, false));
                }
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "ALTER TABLE {0} DROP CONSTRAINT {1}{2}",
                            ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FormatFullNameForScripting(sp),
                            (sp.IncludeScripts.ExistenceCheck && inlineIfExists) ? "IF EXISTS " : string.Empty,
                            FormatFullNameForScripting(sp));

            queries.Add(sb.ToString());
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.UDTTChecksCannotBeModified);
            }

            base.MarkForDropImpl(dropOnAlter);
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
            if (this.DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[] { new PropagateInfo((ServerVersion.Major < 8  || this.Parent is UserDefinedTableType) ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
            }
            return null;
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
           string [] fields = {
                                        "NotForReplication",
                                        "IsChecked",
                                        "IsEnabled",
                                        "IsSystemNamed",
                                        "IsFileTableDefined"
                              };
            List<string> list = GetSupportedScriptFields(typeof(Check.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            list.Add("Text");
            return list.ToArray();

        }
    }
}




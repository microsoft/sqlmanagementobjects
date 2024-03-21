// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // This type corresponds to the CREATE_TYPE event, but UDTs already listen on this event and DMF cannot resolve the URN correctly
    // The issue with eventing and types is being tracked with VSTS 216079
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)] // Extended properties are only allowed as Alterable on this type
    public partial class UserDefinedTableType : TableViewTableTypeBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IRenamable, Cmn.IDroppable, Cmn.IDropIfExists,
        IScriptable, IExtendedProperties
    {
        internal UserDefinedTableType(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }

        public static string UrnSuffix
        {
            get
            {
                return "UserDefinedTableType";
            }
        }

        /// <summary>
        /// Schema of UserDefinedTableType
        /// </summary>
        [SfcKey(0)]
        [SfcReference(typeof(Schema), typeof(SchemaCustomResolver), "Resolve")]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.Design | SfcPropertyFlags.SqlAzureDatabase)]
        [CLSCompliant(false)]
        public override string Schema
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
        /// Name of UserDefinedTableType
        /// </summary>
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

        private CheckCollection m_checks = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Check), SfcObjectFlags.Design)]
        public CheckCollection Checks
        {
            get
            {
                CheckObjectState();
                if (null == m_checks)
                {
                    m_checks = new CheckCollection(this);
                }
                return m_checks;
            }
        }

        #region ICreatable Members

        public void Create()
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            bool bWithScript = action != PropagateAction.Create;
            ArrayList propInfo = new ArrayList();
            propInfo.Add(new PropagateInfo(Columns, bWithScript));
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType ||
                this.ServerVersion.Major >= 12) //Azure v12 (Sterling) and above support Extended Properties
            {
                propInfo.Add(new PropagateInfo(ExtendedProperties, true, ExtendedProperty.UrnSuffix));
            }

            if (action == PropagateAction.Create)
            {
                propInfo.Add(new PropagateInfo(Indexes, bWithScript));
                propInfo.Add(new PropagateInfo(Checks, bWithScript));
            }

            PropagateInfo[] retArr = new PropagateInfo[propInfo.Count];
            propInfo.CopyTo(retArr, 0);
            return retArr;
        }


        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            //create intermediate string collection
            StringCollection sc = new StringCollection();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            String fullTableTypeName = FormatFullNameForScripting(sp);

            StringCollection col_Strings = new StringCollection();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(UrnSuffix, fullTableTypeName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDDT90, "NOT", SqlString(ScriptName.Length > 0 ? ScriptName : Name), SqlString(ScriptName.Length > 0 ? ScriptName : Schema));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE TYPE {0} AS TABLE", fullTableTypeName);

            // script the columns
            if (this.Columns.Count < 1)
            {
                throw new SmoException(ExceptionTemplates.ObjectWithNoChildren("UserDefinedTableType", "Column"));
            }
            sb.Append(Globals.LParen);
            sb.Append(sp.NewLine);

            bool firstCol = true;
            col_Strings.Clear();
            foreach (Column column in this.Columns)
            {
                column.ScriptDdlInternal(col_Strings, sp);
                if (firstCol)
                {
                    firstCol = false;
                }
                else
                {
                    sb.Append(Globals.comma);
                    sb.Append(sp.NewLine);
                }
                sb.Append(Globals.tab);
                sb.Append(col_Strings[0]);
                col_Strings.Clear();
            }

            // script the  indexes
            foreach (Index index in this.Indexes)
            {
                Nullable<IndexKeyType> indexKeyType = index.GetPropValueOptional<IndexKeyType>("IndexKeyType");
                Nullable<IndexType> indexType = index.GetPropValueOptional<IndexType>("IndexType");

                // Primary keys, unique keys and memory optimized indexes can be created on UDTT.
                // Inline indexes (clustered, nonclustered) can be created on UDTT.
                if (((indexKeyType.HasValue) && (indexKeyType.Value != IndexKeyType.None))
                    || ((indexType.HasValue) && (indexType.Value == IndexType.ClusteredIndex || indexType.Value == IndexType.NonClusteredIndex))
                    || index.IsMemoryOptimizedIndex)
                {
                    index.ScriptDdl(col_Strings, sp, false, true); //The index is embedded
                    sb.Append(Globals.comma);
                    sb.Append(sp.NewLine);
                    sb.Append(Globals.tab);
                    sb.Append(col_Strings[0]);
                    col_Strings.Clear();
                }
                else
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.NotIndexOnUDTT);
                }
            }

            // script the checks
            foreach (Check check in this.Checks)
            {
                sb.Append(Globals.comma);
                sb.Append(sp.NewLine);
                sb.Append(Globals.tab);
                sb.Append("CHECK ");
                string stext = (string)check.GetPropValue("Text");
                sb.Append(Globals.LParen);
                sb.Append(stext);
                sb.Append(Globals.RParen);
            }

            sb.Append(sp.NewLine);
            sb.Append(Globals.RParen);

            // script memory optimized table type
            if (IsSupportedProperty("IsMemoryOptimized", sp))
            {
                if (this.GetPropValueOptional("IsMemoryOptimized", false))
                {
                    sb.Append(sp.NewLine);
                    sb.AppendFormat(Scripts.WITH_MEMORY_OPTIMIZED);
                }
            }

            sc.Add(sb.ToString());
            foreach (string s in sc)
            {
                query.Add(s);
            }

            if (sp.IncludeScripts.Owner &&
                (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType ||
                sp.TargetServerVersion == SqlServerVersion.Version120)) //Azure v12 (Sterling) and above support
            {
                if (sp.IncludeScripts.Owner)
                {
                    //script change owner if dirty
                    ScriptOwner(query, sp);
                }
            }
        }

        protected override void PostCreate()
        {
            this.Indexes.Refresh();
            this.Checks.Refresh();
        }

        #endregion

        #region IAlterable Members

        public void Alter()
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            base.AlterImpl();
            SetSchemaOwned();
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            // nothing to be done here, we only alter the extended properties.
            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptOwner(query, sp);
            }
        }

        #endregion

        #region IRenamable Members

        public void Rename(string newname)
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_rename @objname=N'{0}', @newname=N'{1}', @objtype=N'USERDATATYPE'", SqlString(this.FullQualifiedName), SqlString(newName)));
        }

        #endregion

        #region IDroppable Members

        public void Drop()
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full table type name for scripting
            string fullTableTypeName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, fullTableTypeName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDDT90, "", SqlString(ScriptName.Length > 0 ? ScriptName : Name), SqlString(ScriptName.Length > 0 ? ScriptName : Schema));
                sb.Append(sp.NewLine);
            }

            /*
             * This also removes all indexes, constraints, and
             * permission specifications for the table type
             */
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP TYPE {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty,
                fullTableTypeName);

            dropQuery.Add(sb.ToString());

        }
        #endregion

        #region IScriptable Members

        public new StringCollection Script()
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            return ScriptImpl();
        }

        public new StringCollection Script(ScriptingOptions so)
        {
            this.ThrowIfNotSupported(typeof(UserDefinedTableType));
            return ScriptImpl(so);
        }

        #endregion

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
            string[] fields = new string[] {
                                        "ID",
                                        "Owner",
                                        "IsMemoryOptimized",
                                        "IsSchemaOwned"};
            List<string> list = GetSupportedScriptFields(typeof(UserDefinedTableType.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }


    }

}

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
    [SfcElementType("Default")]
    public partial class DefaultConstraint : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IRenamable, Cmn.IAlterable, IExtendedProperties, IScriptable
    {


        private string IfExistsDefaultConstraint(string notOrEmpty, string name, string script, string newLine) =>
$@"IF {notOrEmpty} EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{name}') AND type = 'D'){newLine}BEGIN{newLine}{script}{newLine}END{newLine}";

        internal DefaultConstraint() : base() { }
		internal DefaultConstraint(Column parentColumn, ObjectKeyBase key, SqlSmoState state) : 
			base(key, state)
		{
			// even though we called with the parent collection of the column, we will 
			// place the DefaultConstraint under the right collection
            singletonParent = parentColumn;
			
			// WATCH OUT! we are setting the m_server value here, because DefaultConstraint does
			// not live in a collection, but directly under the Column
			SetServerObject( parentColumn.GetServerObject());
		}

        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectFlags.Design)]
		public Column Parent
		{
			get
			{
                return singletonParent as Column;
			}
            internal set
            {
                SetServerObject(((Column)value).GetServerObject());
                SetParentImpl(value);
                ((Column)value).DefaultConstraint = this;
           
                
            }
		}

        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                if (IsDesignMode && GetIsSystemNamed() && singletonParent.State == SqlSmoState.Creating)
                {
                    return null;
                }
                return base.Name;
            }
            set
            {
                base.Name = value;
                if (singletonParent != null)
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
                if (singletonParent != null && IsDesignMode && singletonParent.State != SqlSmoState.Existing)
                {
                    throw new PropertyNotSetException("IsSystemNamed");
                }
                return (System.Boolean)this.Properties.GetValueWithNullReplacement("IsSystemNamed");
            }
        }

        internal override void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && null != this.singletonParent && (!key.IsNull || IsDesignMode))
            {
                SetState(SqlSmoState.Existing);
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

        internal override void ValidateName(string name)
        {
            if (null == name)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Name"));
            }

            if (IsDesignMode)
            {

                return;//Serializer works in little different way for singleton. It set the parent object existing first
                       //then try to set the name
            }

            if (
                (singletonParent.State != SqlSmoState.Pending) &&
                (singletonParent.State != SqlSmoState.Creating)
                )
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationOnlyInPendingState);
            }
        }

		public StringCollection Script()
		{
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            return ScriptImpl(scriptingOptions);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Default";
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture,
                            "/{0}[{1}]", UrnSuffix, key.UrnFilter);            
        }

        public void Create()
        {
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            string tablename = ((ScriptSchemaObjectBase)(this.Parent.ParentColl.ParentInstance)).FormatFullNameForScripting(sp);
            string script = string.Format(SmoApplication.DefaultCulture,
                                            "ALTER TABLE {0} ADD {1} FOR [{2}]",
                                            tablename,
                                            ScriptDdl(sp),
                                            SqlBraket(this.Parent.Name));
            queries.Add(AddIfExistsCheck(script, sp, "NOT"));
        }

        public void Drop()
        {
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            base.DropImpl();

            //We don't need to check Recording mode criteria. Once object is in dropped state
            //then we have to remove it.
            if (this.State == SqlSmoState.Dropped)
            {
                Parent.RemoveDefaultConstraint();
            }
		}

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            base.DropImpl(true);

            //We don't need to check Recording mode criteria. Once object is in dropped state
            //then we have to remove it.
            if (this.State == SqlSmoState.Dropped)
            {
                Parent.RemoveDefaultConstraint();
            }
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            CheckObjectState();
            string tablename = ((ScriptSchemaObjectBase)(this.Parent.ParentColl.ParentInstance)).FormatFullNameForScripting(sp);
            
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            // Existence of parent table is not checked when ALTER TABLE DROP CONSTRAINT IF EXISTS syntax is used.
            // Check is added here to keep behavior same as in previous versions.
            //
            bool isTargetServerVersionSQl13OrLater = VersionUtils.IsTargetServerVersionSQl13OrLater(sp.TargetServerVersionInternal);
            if (sp.IncludeScripts.ExistenceCheck && isTargetServerVersionSQl13OrLater)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "", SqlString(tablename));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP CONSTRAINT {1}[{2}]",
                tablename,
                (sp.IncludeScripts.ExistenceCheck && isTargetServerVersionSQl13OrLater) ? "IF EXISTS " : string.Empty,
                SqlBraket(this.Name));

            string script = sb.ToString();
            dropQuery.Add(isTargetServerVersionSQl13OrLater ? script : AddIfExistsCheck(script, sp, ""));
        }

        private string AddIfExistsCheck(string script, ScriptingPreferences sp, string qualifier)
        {
            if (!sp.IncludeScripts.ExistenceCheck)
            {
                return script;
            }

            return IfExistsDefaultConstraint(qualifier, Util.EscapeString(FormatFullNameForScripting(sp), '\''), script, sp.NewLine);
        }

        public void Alter()
        {
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            base.AlterImpl();
        }

        internal string ScriptDdl(ScriptingPreferences sp)
        {
            if(!String.IsNullOrEmpty(Name))
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                if (Parent.ParentColl.ParentInstance is Table)
                {
                    if (ScriptConstraintWithName(sp))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " CONSTRAINT {0} ", MakeSqlBraket(this.Name));
                    }
                }

                sb.Append(" DEFAULT ");
                sb.Append(GetTextProperty("Text"));
                return sb.ToString();
            }
            else
            {
                return " DEFAULT " + GetTextProperty("Text");
            }
        }

        /// <summary>
        /// Renames the object
        /// </summary>
        /// <param name="newname">New default constraint name</param>
        public void Rename(string newname)
        {
            if (Parent.ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC {0}.dbo.sp_rename @objname = N'{1}', @newname = N'{2}', @objtype = N'OBJECT'",
                                MakeSqlBraket(GetDBName()),
                                SqlString(this.FormatFullNameForScripting(sp)),
                                SqlString(newName)));
        }

        override protected string GetServerName()
        {
            return Parent.ParentColl.ParentInstance.ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName;
        }

        internal protected override string GetDBName()
        {
            return this.Parent.ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName;
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
                    if (null == m_comparer)
                    {
                        m_comparer = Parent.StringComparer;
                    }
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        // Embed default constraint flag
        // This flag is used during script generation. It should not be used 
        // to control default constraint embedding under different condition.
        internal bool forceEmbedDefaultConstraint;

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            this.forceEmbedDefaultConstraint = false;

            if (this.DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
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
            Cmn.ServerVersion version, 
            Cmn.DatabaseEngineType databaseEngineType, 
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
           string[] fields = {   
                                        "IsSystemNamed",
                                        "IsFileTableDefined"
                             };
            List<string> list = GetSupportedScriptFields(typeof(DefaultConstraint.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            list.Add("Text");
            return list.ToArray();
        }

        internal override string FormatFullNameForScripting(ScriptingPreferences sp)
        {
            CheckObjectState();
            // format full object name for scripting
            string sFullNameForScripting = String.Empty;
            if (sp.IncludeScripts.SchemaQualify) // pre-qualify object name with an owner name
            {
                string schema = ((ScriptSchemaObjectBase)(this.Parent.ParentColl.ParentInstance)).Schema;
                if (schema.Length > 0)
                {
                    sFullNameForScripting = MakeSqlBraket(schema);
                    sFullNameForScripting += Globals.Dot;
                }
            }
            sFullNameForScripting += base.FormatFullNameForScripting(sp);

            return sFullNameForScripting;
        }
    }
}


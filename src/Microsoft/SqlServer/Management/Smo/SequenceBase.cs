// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server Sequence object
    ///</summary>
    [Facets.StateChangeEvent("CREATE_SEQUENCE", "SEQUENCE")]
    [Facets.StateChangeEvent("ALTER_SEQUENCE", "SEQUENCE")]
    [Facets.StateChangeEvent("RENAME", "SEQUENCE")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "SEQUENCE")] // For Owner
    [Facets.StateChangeEvent("ALTER_SCHEMA", "SEQUENCE")] // For Schema
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Sequence : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IRenamable, IExtendedProperties, IScriptable, Cmn.IAlterable
    {
        internal Sequence(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "Sequence";
            }
        }

        ///<summary>
        /// ExtendedProperties
        ///</summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                this.ThrowIfNotSupported(typeof(Sequence));
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        /// <summary>
        /// Schema of the Sequence
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
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
        /// Name of Sequence
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
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

        private DataType dataType = null;
        /// <summary>
        /// Datatype of the Sequence
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public DataType DataType
        {
            get
            {
                return GetDataType(ref dataType);
            }
            set
            {
                if (value != null && value.SqlDataType == SqlDataType.UserDefinedTableType)
                {
                    throw new FailedOperationException(ExceptionTemplates.SetDataType, this, null);
                }

                SetDataType(ref dataType, value);
            }
        }

        /// <summary>
        /// Drop the Sequence
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

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            // need to see if it is an app role, defaults to false
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "Sequence", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }
            if (sp.IncludeScripts.DatabaseContext)
            {
                AddDatabaseContext(dropQuery);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                   Scripts.INCLUDE_EXISTS_SEQUENCE, String.Empty, FormatFullNameForScripting(sp, false), MakeSqlString(GetSchema(sp))));
                sb.Append(sp.NewLine);
            }

            sb.Append("DROP SEQUENCE " +
                ((sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty) +
                FormatFullNameForScripting(sp));

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Create the Sequence
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        //This validate the user given input. So we can Prevent SQLInjection.
        private bool IsValidDoubleValue(string propertyName, string propertyValue)
        {
            Double result;

            if (Double.TryParse(propertyValue, out result))
            {
                return true;
            }
            else
            {
                throw new WrongPropertyValueException(ExceptionTemplates.InvalidSequenceValue(propertyName));
            }
        }

        //CREATE SEQUENCE [ schema_name . ]  sequence_name
        //[ <sequence_property_assignment> [ ,...n ] ]
        //[ ; ]
        //<sequence_property_assignment>::=
        //{
        //AS { <built_in_integer_type> | <user-defined_integer_type> } ]
        //| START WITH <constant>
        //| INCREMENT BY <constant>
        //| { MINVALUE <constant> | NO MINVALUE }
        //| { MAXVALUE <constant> | NO MAXVALUE }
        //| { CYCLE | NO CYCLE }
        //| { CACHE [<constant> ] | NO CACHE }
        //}



        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "Sequence", this.FormatFullNameForScripting(sp),
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }
            if (sp.IncludeScripts.DatabaseContext)
            {
                AddDatabaseContext(createQuery);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SEQUENCE, "NOT", FormatFullNameForScripting(sp, false), MakeSqlString(GetSchema(sp)));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE SEQUENCE {0} ", FormatFullNameForScripting(sp));
            if ((this.DataType != null) && (!String.IsNullOrEmpty(dataType.Name)))
            {
                sb.Append(Globals.newline);
                sb.Append(" AS ");
                UserDefinedDataType.AppendScriptTypeDefinition(sb, sp, this, DataType.SqlDataType);
            }


            string startValue =  Convert.ToString(this.GetPropValueOptional("StartValue"), SmoApplication.DefaultCulture);
            if (!String.IsNullOrEmpty(startValue) && IsValidDoubleValue("StartValue",startValue))
            {
                sb.Append(Globals.newline);
                sb.Append(" START WITH ");
                sb.Append(startValue);
            }

            string incrementValue = Convert.ToString(this.GetPropValueOptional("IncrementValue"), SmoApplication.DefaultCulture);
            if (!String.IsNullOrEmpty(incrementValue) && IsValidDoubleValue("IncrementValue",incrementValue))
            {
                sb.Append(Globals.newline);
                sb.Append(" INCREMENT BY ");
                sb.Append(incrementValue);
            }



            string minValue = Convert.ToString(this.GetPropValueOptional("MinValue"), SmoApplication.DefaultCulture);
            if (!String.IsNullOrEmpty(minValue) && IsValidDoubleValue("MinValue",minValue) )
            {
                sb.Append(Globals.newline);
                sb.Append(" MINVALUE ");
                sb.Append(minValue);
            }

            string maxValue = Convert.ToString(this.GetPropValueOptional("MaxValue"), SmoApplication.DefaultCulture);
            if (!String.IsNullOrEmpty(maxValue) && IsValidDoubleValue("MaxValue",maxValue))
            {
                sb.Append(Globals.newline);
                sb.Append(" MAXVALUE ");
                sb.Append(maxValue);
            }

            Object isCycleEnabled = this.GetPropValueOptional("IsCycleEnabled");
            if ((isCycleEnabled != null) && ((bool)isCycleEnabled))
            {
                sb.Append(Globals.newline);
                sb.Append(" CYCLE ");
            }

            Object sequenceCacheType = this.GetPropValueOptional("SequenceCacheType");

            if (sequenceCacheType != null)
            {
                SequenceCacheType cacheType = (SequenceCacheType)sequenceCacheType;
                if (!Enum.IsDefined(typeof(SequenceCacheType), cacheType))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("SequenceCacheType"));
                }
                switch ((SequenceCacheType)sequenceCacheType)
                {
                    case SequenceCacheType.DefaultCache:
                        sb.Append(Globals.newline);
                        sb.Append(" CACHE ");
                        break;
                    case SequenceCacheType.NoCache:
                        sb.Append(Globals.newline);
                        sb.Append(" NO CACHE ");
                        break;
                    case SequenceCacheType.CacheWithSize:
                        int cacheSize = (int)this.GetPropValueOptional("CacheSize",0);
                        sb.Append(Globals.newline);
                        sb.AppendFormat(SmoApplication.DefaultCulture, " CACHE  {0} ", cacheSize.ToString(SmoApplication.DefaultCulture));
                        break;
                }
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }
            createQuery.Add(sb.ToString());
            if (sp.IncludeScripts.Owner)
            {
                ScriptOwner(createQuery, sp);
            }
        }

        /// <summary>
        /// Script the Sequence
        /// </summary>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Alter the Sequence
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
        }
        //        ALTER SEQUENCE [ schema_name . ]  sequence_name
        //[ <sequence_property_update> [ ,...n ] ]
        //[ ; ]

        //<sequence_property_update>::=
        //{
        //{ RESTART [ WITH <constant> ] }
        //| INCREMENT BY <constant>
        //| { MINVALUE <constant> | NO MINVALUE }
        //| { MAXVALUE <constant> | NO MAXVALUE }
        //| { CYCLE | NO CYCLE }
        //| { CACHE [<constant> ] | NO CACHE }
        //}


        internal override  void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {

            if (!IsObjectDirty() || this.State == SqlSmoState.Creating)
            {
                return;
            }
            this.ThrowIfNotSupported(this.GetType(), sp);

            Property property, cacheSizeProperty;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sb.AppendFormat("ALTER SEQUENCE {0}", FullQualifiedName);
            int statementLength = sb.Length;
            if ((null != (property = this.Properties.Get("StartValue")).Value) && property.Dirty)
            {
                sb.Append(sp.NewLine);
                if (String.IsNullOrEmpty(property.Value.ToString()))
                {
                    sb.Append(" RESTART ");
                }
                else if (IsValidDoubleValue("StartValue", property.Value.ToString()))
                {
                    sb.AppendFormat(" RESTART  WITH {0} ", property.Value.ToString());
                }
            }
            if ((null != (property = this.Properties.Get("IncrementValue")).Value) && property.Dirty)
            {
                if (!String.IsNullOrEmpty(property.Value.ToString()) && (IsValidDoubleValue("IncrementValue", property.Value.ToString())))
                {
                    sb.Append(sp.NewLine);
                    sb.AppendFormat(" INCREMENT BY {0} ", property.Value.ToString());
                }
            }

            if ((null != (property = this.Properties.Get("MinValue")).Value) && property.Dirty)
            {
                sb.Append(sp.NewLine);
                if (!String.IsNullOrEmpty(property.Value.ToString()) && (IsValidDoubleValue("MinValue", property.Value.ToString())))
                {
                    sb.AppendFormat(" MINVALUE {0} ", property.Value.ToString());
                }
                else
                {
                    sb.Append(" NO MINVALUE ");
                }
            }

            if ((null != (property = this.Properties.Get("MaxValue")).Value) && property.Dirty)
            {
                sb.Append(sp.NewLine);
                if (!String.IsNullOrEmpty(property.Value.ToString()) && (IsValidDoubleValue("MaxValue", property.Value.ToString())))
                {
                    sb.AppendFormat(" MAXVALUE {0} ", property.Value.ToString());
                }
                else
                {
                    sb.Append(" NO MAXVALUE ");
                }
            }

            if ((null != (property = this.Properties.Get("IsCycleEnabled")).Value) && property.Dirty)
            {
                sb.Append(sp.NewLine);
                if ((bool)property.Value)
                {
                    sb.Append(" CYCLE ");
                }
                else
                {
                    sb.Append(" NO CYCLE ");
                }
            }

            if ( ((null != (property = this.Properties.Get("SequenceCacheType")).Value) && property.Dirty) ||
                ((null != (cacheSizeProperty = this.Properties.Get("CacheSize")).Value) && cacheSizeProperty.Dirty)
                )
            {
                SequenceCacheType sequenceCacheType = (SequenceCacheType)property.Value;
                if (!Enum.IsDefined(typeof(SequenceCacheType), sequenceCacheType))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("SequenceCacheType"));
                }
                switch (sequenceCacheType)
                {
                    case SequenceCacheType.DefaultCache:
                        sb.Append(Globals.newline);
                        sb.Append(" CACHE ");
                        break;
                    case SequenceCacheType.NoCache:
                        sb.Append(Globals.newline);
                        sb.Append(" NO CACHE ");
                        break;
                    case SequenceCacheType.CacheWithSize:
                        int cacheSize = (int)this.Properties.Get("CacheSize").Value;
                        sb.Append(Globals.newline);
                        sb.AppendFormat(SmoApplication.DefaultCulture, " CACHE  {0} ", cacheSize.ToString(SmoApplication.DefaultCulture));
                        break;
                }
            }

            if (sb.Length > statementLength)
            {
                alterQuery.Add(sb.ToString());
            }

            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptChangeOwner(alterQuery, sp);
            }
        }

        /// <summary>
        /// Rename
        /// </summary>
        /// <param name="newname"></param>
        public void Rename(string newname)
        {
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));
            //@objtype = N'OBJECT:  An item of a type tracked in sys.objects like sequence.
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_rename @objname = N'{0}', @newname = N'{1}', @objtype = N'OBJECT'",
                SqlString(this.FullQualifiedName),
                SqlString(newName)));
        }

        /// <summary>
        /// Refresh
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.dataType = null;
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] {
                new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix ) };
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object. This is used by transfer.
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
            string[] fields = {
                                                "NumericPrecision",
                                                "NumericScale",
                                                "SystemType",
                                                "DataTypeSchema",
                                                "StartValue",
                                                "IncrementValue",
                                                "MinValue",
                                                "MaxValue",
                                                "IsCycleEnabled",
                                                "SequenceCacheType",
                                                "CacheSize",
                                                "ID",
                                                "Owner",
                                                "IsSchemaOwned"};
            List<string> list = GetSupportedScriptFields(typeof(Sequence.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            list.Add("DataType");
            return list.ToArray();
        }
    }
}


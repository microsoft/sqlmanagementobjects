// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    [SfcElementType("DdlTrigger")]
    public partial class DatabaseDdlTrigger : DdlTriggerBase, IExtendedProperties
    {
        internal DatabaseDdlTrigger(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public DatabaseDdlTrigger() :
            base()
        {
        }

        public DatabaseDdlTrigger(Database database, string name) :
            base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = database;
        }

        public DatabaseDdlTrigger(Database parent, string name,
            DatabaseDdlTriggerEventSet events, string textBody)
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = parent;

            Properties.Get("DdlTriggerEvents").Value = events;
            TextBody = textBody;
            Properties.Get("ImplementationType").Value = ImplementationType.TransactSql;
        }

        public DatabaseDdlTrigger(Database parent, System.String name,
            DatabaseDdlTriggerEventSet events, string assemblyName,
            string className, string method)
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = parent;

            Properties.Get("DdlTriggerEvents").Value = events;
            Properties.Get("ImplementationType").Value = ImplementationType.SqlClr;
            Properties.Get("AssemblyName").Value = assemblyName;
            Properties.Get("ClassName").Value = className;
            Properties.Get("MethodName").Value = method;
        }

        [SfcKey(0)]
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

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Database Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as Database;
            }
            set
            {
                SetParentImpl(value);
            }
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "DdlTrigger";
            }
        }

        public Microsoft.SqlServer.Management.Smo.DatabaseDdlTriggerEventSet DdlTriggerEvents
        {
            get
            {
                // DdlTriggerEvents is a special property. We would like to have it
                // created when users want to set certain flags.
                if (State == SqlSmoState.Creating)
                {
                    Property prop = Properties.Get("DdlTriggerEvents");
                    if (null == prop.Value)
                    {
                        prop.Value = new DatabaseDdlTriggerEventSet();
                    }
                }

                return (Microsoft.SqlServer.Management.Smo.DatabaseDdlTriggerEventSet)this.Properties.GetValueWithNullReplacement("DdlTriggerEvents");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("DdlTriggerEvents", value);
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

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            string sFullScriptingName = FormatFullNameForScripting(sp);
            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture,GetIfNotExistStatement(sp, ""));
            }
            sb.AppendFormat(SmoApplication.DefaultCulture,
                                        "DROP TRIGGER {0}{1} ON DATABASE",
                                        (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty,
                                        sFullScriptingName);
            queries.Add(sb.ToString());
        }

        /// <summary>
        /// adds the events to the for clause of the ddl
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="so"></param>
        internal override void AddDdlTriggerEvents(StringBuilder sb, ScriptingPreferences sp)
        {
            // we will always have a non null value, because we're setting it in the ctor
            DatabaseDdlTriggerEventSet events = (DatabaseDdlTriggerEventSet)GetPropValueOptional("DdlTriggerEvents", new DatabaseDdlTriggerEventSet());

            int evtCount = 0;
            for (int i = 0; i < events.NumberOfElements; i++)
            {
                if (events.GetBitAt(i))
                {
                    if (evtCount++ > 0)
                    {
                        sb.Append(Globals.commaspace);
                    }

                    sb.Append(StringFromDatabaseDdlTriggerEvent(i));
                }
            }

            if (0 == evtCount)
            {
                throw new PropertyNotSetException("DdlTriggerEvents");
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

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "IsEncrypted")
            {
                Validate_set_TextObjectDDLProperty(prop, value);
            }
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || IsEventSetDirty();
        }

        protected override void CleanObject()
        {
            base.CleanObject();
            DatabaseDdlTriggerEventSet tempSet = (DatabaseDdlTriggerEventSet)(Properties.Get("DdlTriggerEvents").Value);
            if (null != tempSet)
            {
                tempSet.Dirty = false;
            }
        }

        protected override bool IsEventSetDirty()
        {
            bool isDirty = false;
            DatabaseDdlTriggerEventSet tempSet = (DatabaseDdlTriggerEventSet)(Properties.Get("DdlTriggerEvents").Value);
            if (null != tempSet)
            {
                isDirty = tempSet.Dirty;
            }

            return isDirty;
        }

        internal override string GetIfNotExistStatement(ScriptingPreferences sp, string prefix)
        {
            return string.Format(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE_DDL_TRIGGER,
                prefix, FormatFullNameForScripting(sp, false));
        }

        #region TextModeImpl

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string TextBody
        {
            get { return base.TextBody; }
            set { base.TextBody = value; }
        }

        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string TextHeader
        {
            get { return base.TextHeader; }
            set { base.TextHeader = value; }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override bool TextMode
        {
            get { return base.TextMode; }
            set { base.TextMode = value; }
        }

        #endregion

        internal static string[] GetScriptFields(Type parentType,
            Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            return new string[] 
            {
               "IsSystemObject"
            };
        }

        internal static string[] GetScriptFields2(Type parentType, Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType, Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode, ScriptingPreferences sp)
        {
            return new string[] 
            {
                "Text",
            };
        }
    }
}


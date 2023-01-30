// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet(PhysicalFacetOptions.ReadOnly)]
    [SfcElementType("DdlTrigger")]
    public partial class ServerDdlTrigger : DdlTriggerBase
    {
        internal ServerDdlTrigger(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public ServerDdlTrigger() :
            base()
        {
        }

        public ServerDdlTrigger(Server server, string name)
            : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = server;
        }

        public ServerDdlTrigger(Server parent, string name,
            ServerDdlTriggerEventSet events, string textBody)
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = parent;

            Properties.Get("DdlTriggerEvents").Value = events;
            TextBody = textBody;
            Properties.Get("ImplementationType").Value = ImplementationType.TransactSql;
        }

        public ServerDdlTrigger(Server parent, System.String name,
            ServerDdlTriggerEventSet events, string assemblyName,
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

        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Server Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as Server;
            }
            set { SetParentImpl(value); }
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "DdlTrigger";
            }
        }

        public Microsoft.SqlServer.Management.Smo.ServerDdlTriggerEventSet DdlTriggerEvents
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
                        prop.Value = new ServerDdlTriggerEventSet();
                    }
                }

                return (Microsoft.SqlServer.Management.Smo.ServerDdlTriggerEventSet)this.Properties.GetValueWithNullReplacement("DdlTriggerEvents");
            }

            set
            {
                Properties.SetValueWithConsistencyCheck("DdlTriggerEvents", value);
            }
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string sFullScriptingName = FormatFullNameForScripting(sp);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.AppendLine(GetIfNotExistStatement(sp, ""));
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "DROP TRIGGER {0}{1} ON ALL SERVER",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty,
                sFullScriptingName);
            queries.Add(sb.ToString());
        }


        /// <summary>
        /// adds the events to the for clause of the ddl
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="sp"></param>
        internal override void AddDdlTriggerEvents(StringBuilder sb, ScriptingPreferences sp)
        {
            // we will always have a non null value, because we're setting it in the ctor
            ServerDdlTriggerEventSet events = (ServerDdlTriggerEventSet)GetPropValueOptional("DdlTriggerEvents", new ServerDdlTriggerEventSet());

            int evtCount = 0;
            for (int i = 0; i < events.NumberOfElements; i++)
            {
                if (events.GetBitAt(i))
                {
                    if (evtCount++ > 0)
                    {
                        sb.Append(Globals.commaspace);
                    }

                    sb.Append(StringFromServerDdlTriggerEvent(i));
                }
            }

            if (0 == evtCount)
            {
                throw new PropertyNotSetException("DdlTriggerEvents");
            }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="prop"></param>
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
            ServerDdlTriggerEventSet tempSet = (ServerDdlTriggerEventSet)(Properties.Get("DdlTriggerEvents").Value);
            if (null != tempSet)
            {
                tempSet.Dirty = false;
            }
        }

        protected override bool IsEventSetDirty()
        {
            bool isDirty = false;
            ServerDdlTriggerEventSet tempSet = (ServerDdlTriggerEventSet)(Properties.Get("DdlTriggerEvents").Value);
            if (null != tempSet)
            {
                isDirty = tempSet.Dirty;
            }

            return isDirty;
        }


        internal override string GetIfNotExistStatement(ScriptingPreferences sp, string prefix)
        {
            return string.Format(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SERVER_DDL_TRIGGER,
                prefix, FormatFullNameForScripting(sp, false));
        }
    }
}


// Copyright (c) Microsoft Corporation.
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
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Synonym : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable, Cmn.IAlterable
    {
        internal Synonym(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Synonym";
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
                    "Synonym", this.FormatFullNameForScripting(sp), DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion < SqlServerVersion.Version130)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                   Scripts.INCLUDE_EXISTS_SYNONYM, "", FormatFullNameForScripting(sp, false), MakeSqlString(GetSchema(sp))));
                sb.Append(sp.NewLine);
            }

            sb.Append("DROP SYNONYM " +
                ((sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty) +
                FormatFullNameForScripting(sp));

            dropQuery.Add(sb.ToString());
            }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "Synonym", this.FormatFullNameForScripting(sp),
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SYNONYM, "NOT", FormatFullNameForScripting(sp, false), MakeSqlString(GetSchema(sp)));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE SYNONYM {0} FOR ", FormatFullNameForScripting(sp));

            string baseServer = string.Empty;
            if (IsSupportedProperty("BaseServer", sp)
        && !IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                baseServer = (string)this.GetPropValueOptional("BaseServer");
                if (!string.IsNullOrEmpty(baseServer))
                {
                    sb.Append(MakeSqlBraket(baseServer));
                    sb.Append(Globals.Dot);
                }
            }

            string baseDatabase = (string)this.GetPropValueOptional("BaseDatabase");
            if (!string.IsNullOrEmpty(baseDatabase))
            {
                sb.Append(MakeSqlBraket(baseDatabase));
                sb.Append(Globals.Dot);
            }
            else if (!string.IsNullOrEmpty(baseServer))
            {
                sb.Append(Globals.Dot);
            }
            string baseSchema = (string)this.GetPropValueOptional("BaseSchema");
            if (!String.IsNullOrEmpty(baseSchema))
            {
                sb.Append(MakeSqlBraket(baseSchema));
                sb.Append(Globals.Dot);
            }
            else if (!string.IsNullOrEmpty(baseServer)|| !string.IsNullOrEmpty(baseDatabase))
            {
                sb.Append(Globals.Dot);
            }
            string baseObject = (string)this.GetPropValue("BaseObject");
            if (!String.IsNullOrEmpty(baseObject))
            {
                sb.Append(MakeSqlBraket(baseObject));
            }

            createQuery.Add(sb.ToString());
            if (sp.IncludeScripts.Owner)
            {

                //script change owner if dirty
                ScriptOwner(createQuery, sp);
            }
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

        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
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


        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (Cmn.DatabaseEngineType.SqlAzureDatabase == this.DatabaseEngineType)
            {
                return null;
            }

            return new PropagateInfo[] {
                new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix ) };
        }

    }
}



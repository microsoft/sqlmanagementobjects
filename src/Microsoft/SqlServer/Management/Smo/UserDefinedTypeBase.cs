// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class UserDefinedType : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable, Cmn.IAlterable
    {
        private void init()
        {
            this.m_ExtendedProperties = null;
        }

        internal UserDefinedType(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            init();
        }

        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "UserDefinedType";
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == this.m_ExtendedProperties)
                {
                    this.m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return this.m_ExtendedProperties;
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
        }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string fullName = FormatFullNameForScripting(sp);
            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDT, "NOT",
                                            SqlString(GetName(sp)), SqlString(GetSchema(sp)));
                sb.Append(sp.NewLine);
            }

            // CREATE TYPE
            sb.AppendFormat("CREATE TYPE {0}", fullName);
            sb.Append(sp.NewLine);

            // EXTERNAL NAME
            sb.Append("EXTERNAL NAME ");

            string tempString = (string)this.GetPropValue("AssemblyName");
            if (string.Empty == tempString)
            {
                throw new PropertyNotSetException("AssemblyName");
            }

            sb.AppendFormat("[{0}]", SqlBraket(tempString));

            Property propClassName = Properties.Get("ClassName");
            if (propClassName.Value != null && ((string)propClassName.Value).Length > 0)
            {
                sb.AppendFormat(".[{0}]", SqlBraket((string)propClassName.Value));
            }

            sb.Append(sp.NewLine);

            queries.Add(sb.ToString());

            if (sp.IncludeScripts.Owner)
            {

                //script change owner if dirty
                ScriptOwner(queries, sp);
            }
        }

        /// <summary>
        /// Alter Schema for User Defined Type
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
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

            // DROP TYPE
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            string fullName = FormatFullNameForScripting(sp);
            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion < SqlServerVersion.Version130)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_UDT, "",
                                        SqlString(GetName(sp)), SqlString(GetSchema(sp)));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat("DROP TYPE {0}{1}",
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                fullName);
            sb.Append(sp.NewLine);

            dropQuery.Add(sb.ToString());
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
                        "AssemblyName",
                        "ClassName",
                        "ID",
                        "Owner"};
            List<string> list = GetSupportedScriptFields(typeof(UserDefinedType.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

    }
}


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
    [Facets.StateChangeEvent("CREATE_SCHEMA", "SCHEMA")]
    [Facets.StateChangeEvent("ALTER_SCHEMA", "SCHEMA")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "SCHEMA")] // For Owner
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Schema : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable,
        Cmn.IDropIfExists, IExtendedProperties, IScriptable, Cmn.IAlterable
    {
        internal Schema(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Schema";
            }
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
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            // need to see if it is an app role, defaults to false
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "Schema", this.FormatFullNameForScripting(sp),
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                    Scripts.INCLUDE_EXISTS_SCHEMA90, "", FormatFullNameForScripting(sp, false)));
                sb.Append(sp.NewLine);
            }

            //if 7.0, 8.0
            if (SqlServerVersionInternal.Version90 > sp.TargetServerVersionInternal)
            {
                throw new InvalidVersionSmoOperationException(this.ServerVersion);
            }
            else //9.0
            {
                sb.Append("DROP SCHEMA " +
                    ((sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty) +
                    FormatFullNameForScripting(sp, true));
            }

            dropQuery.Add(sb.ToString());
        }

        public void Create()
        {
            base.CreateImpl();
        }


        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal,
                ExceptionTemplates.SchemaDownlevel(
                    FormatFullNameForScripting(sp, true),
                    GetSqlServerName(sp)
                    ));

            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "Schema", this.FormatFullNameForScripting(sp),
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            StringBuilder createStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            createStmt.Append("CREATE ");
            createStmt.AppendFormat(SmoApplication.DefaultCulture, "SCHEMA {0}", FormatFullNameForScripting(sp, true));
            if (sp.IncludeScripts.Owner)
            {
                string owner = (string)GetPropValueOptional("Owner", string.Empty);
                if (owner.Length > 0)
                {
                    createStmt.AppendFormat(SmoApplication.DefaultCulture, " AUTHORIZATION {0}", MakeSqlBraket(owner));
                } 
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SCHEMA90, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "EXEC sys.sp_executesql N'{0}'", SqlString(createStmt.ToString()));
                sb.Append(sp.NewLine);
            }
            else
            {
                sb.Append(createStmt.ToString());
            }

            createQuery.Add(sb.ToString());
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
        }

        // generates the scripts for the alter action
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            //if 7.0, 8.0
            if (SqlServerVersionInternal.Version90 > sp.TargetServerVersionInternal)
            {
                throw new InvalidVersionSmoOperationException(this.ServerVersion);
            }

            ScriptChangeOwner(query, sp);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            // DataWarehouse is the only edition that doesn't support extended properties
            if (this.DatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse)
            {
                return new PropagateInfo[] { new PropagateInfo(ExtendedProperties, true, ExtendedProperty.UrnSuffix) };
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
            string[] fields =
            {
                "ID",
                "IsSystemObject",
                "Owner",
            };
            List<string> list = GetSupportedScriptFields(typeof(Schema.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();

        }
    }
}



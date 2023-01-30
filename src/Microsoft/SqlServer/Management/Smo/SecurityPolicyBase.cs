// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server Security Policy object
    ///</summary>
    public partial class SecurityPolicy : ScriptSchemaObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable, Cmn.IAlterable, IExtendedProperties
    {
        internal SecurityPolicy(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// </summary>
        /// <param name="parent">The parent Database</param>
        /// <param name="name">The name of the security policy</param>
        /// <param name="schema">The schema of the security policy</param>
        /// <param name="notForReplication">Whether or not the security policy is marked for replication</param>
        /// <param name="isEnabled">Whether the security policy is enabled or disabled</param>
        public SecurityPolicy(Database parent, string name, string schema, bool notForReplication, bool isEnabled) : base()
        {
            this.Parent = parent;
            this.Name = name;
            this.Schema = schema;
            this.NotForReplication = notForReplication;
            this.Enabled = isEnabled;

            // Default value for is_schema_bound.
            //
            this.IsSchemaBound = true;
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "SecurityPolicy";
            }
        }

        /// <summary>
        /// The schema of the Security Policy
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
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
        /// The name of the Security Policy
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
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

        SecurityPredicateCollection m_securityPredicates = null;
        /// <summary>
        /// The collection of security predicates for this security policy.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(SecurityPredicate))]
        public SecurityPredicateCollection SecurityPredicates
        {
            get
            {
                if (m_securityPredicates == null)
                {
                    m_securityPredicates = new SecurityPredicateCollection(this);
                }

                return m_securityPredicates;
            }
        }

        /// <summary>
        /// Collection of extended properties for this object.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                CheckObjectState();
                if (m_ExtendedProperties == null)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        /// <summary>
        /// Drop the Security Policy.
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
            
            string fullyFormattedName = FormatFullNameForScripting(sp);

            CheckObjectState();
            StringBuilder sb = new StringBuilder();
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "SecurityPolicy", fullyFormattedName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                   Scripts.INCLUDE_EXISTS_SECURITY_POLICY, String.Empty, fullyFormattedName));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.Append("DROP SECURITY POLICY " +
                ((sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130) ? "IF EXISTS " : string.Empty) +
                fullyFormattedName);

            if (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersionInternal < SqlServerVersionInternal.Version130)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Create the Security Policy
        /// </summary>
        public void Create()
        {
            // Can't create a policy with no security predicates.
            //
            if (this.SecurityPredicates.Count == 0)
            {
                throw new InvalidOperationException(ExceptionTemplates.SecurityPolicyNoPredicates(this.FullQualifiedName));
            }

            base.CreateImpl();
            SetSchemaOwned();
        }

        // CREATE SECURITY POLICY [ schema_name. ] security_policy_name
        //  { ADD FILTER PREDICATE tvf_schema_name.security_predicate_function_name 
        //      ( { column_name | arguments } [ , …n] ) ON table_schema_name.table_name } [ , ...n ]
        //  [ WITH ( STATE = { ON | OFF } ) ]
        //  [ NOT FOR REPLICATION ]
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            
            string fullyFormattedName = FormatFullNameForScripting(sp);

            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "SecurityPolicy", fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SECURITY_POLICY, "NOT", this.Name);
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE SECURITY POLICY {0} ", fullyFormattedName);

            bool first_predicate = true;
            foreach (SecurityPredicate pred in this.SecurityPredicates)
            {
                if (first_predicate)
                {
                    first_predicate = false;
                }
                else
                {
                    sb.Append(",");
                }

                pred.ScriptPredicate(sb, sp, forCreate: true);
            }

            // Script out the security policy options.
            //
            sb.Append(Globals.newline);
            sb.Append("WITH");

            var statementBuilder = new ScriptStringBuilder("");
            statementBuilder.SetParameter("STATE", this.Enabled ? Globals.On : Globals.Off, ParameterValueFormat.NotString);

            if (!sp.TargetEngineIsAzureSqlDw())
            {
                statementBuilder.SetParameter(Scripts.SP_SCHEMABINDING, this.IsSchemaBound ? Globals.On : Globals.Off, ParameterValueFormat.NotString);
            }

            sb.Append(statementBuilder.ToString(false));

            string isNotForReplication = Convert.ToString(this.GetPropValueOptional("NotForReplication"), SmoApplication.DefaultCulture);
            if (!String.IsNullOrEmpty(isNotForReplication) && bool.Parse(isNotForReplication))
            {
                sb.Append(Globals.newline);
                sb.Append("NOT FOR REPLICATION");
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
        /// Script the Security Policy
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
        /// Alter the Security Policy
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
        }

        //  ALTER SECURITY POLICY [ schema_name. ] security_policy_name
        //    (
        //      { ADD FILTER PREDICATE tvf_schema_name.security_predicate_function_name 
        //        ( { column_name | arguments } [ , …n] ) ON table_schema_name.table_name }
        //      |
        //      { ALTER FILTER PREDICATE tvf_schema_name.security_predicate_function_name 
        //        ( { column_name | arguments } [ , …n] ) ON table_schema_name.table_name }
        //          |
        //          { DROP FILTER PREDICATE ON table_schema_name.table_name }
        //    )  [ , ...n ]
        //    |
        //      WITH ( STATE = { ON | OFF } )
        //    |
        //      ADD NOT FOR REPLICATION
        //    |
        //      DROP NOT FOR REPLICATION
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            
            if (this.State == SqlSmoState.Creating)
            {
                return;
            }

            string fullyFormattedName = FormatFullNameForScripting(sp);
            
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string alterString = String.Format("ALTER SECURITY POLICY {0} ", fullyFormattedName);

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "SecurityPolicy", fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (this.SecurityPredicates.Count > 0)
            {
                sb.AppendFormat(alterString);
                // Alter security predicates.
                //
                bool isFirstPredicate = true;
                foreach (SecurityPredicate pred in this.SecurityPredicates)
                {
                    if (isFirstPredicate)
                    {
                        isFirstPredicate = false;
                    }
                    else
                    {
                        sb.Append(",");
                    }

                    pred.ScriptPredicate(sb, sp);
                }

                sb.Append(Globals.newline);
            }

            // Alter the security policy state
            //
            sb.Append(Globals.newline);
            sb.Append(alterString);
            string isEnabled = Convert.ToString(this.GetPropValue("Enabled"), SmoApplication.DefaultCulture);
            sb.Append("WITH (STATE = ");
            if(bool.Parse(isEnabled))
            {
                sb.Append("ON)");
            }
            else
            {
                sb.Append("OFF)");
            }

            sb.Append(Globals.newline);

            // Mark policy as for or not for replication.
            //
            string notForReplication = Convert.ToString(this.GetPropValue("NotForReplication"), SmoApplication.DefaultCulture);
            if (!String.IsNullOrEmpty(notForReplication))
            {
                sb.Append(Globals.newline);
                sb.Append(alterString);

                if (bool.Parse(notForReplication))
                {
                    sb.Append(" ADD NOT FOR REPLICATION");
                }
                else
                {
                    sb.Append(" DROP NOT FOR REPLICATION");
                }
            }

            alterQuery.Add(sb.ToString());

            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptChangeOwner(alterQuery, sp);
            }
        }

        /// <summary>
        /// Propagate states, but not actions to SecurityPredicates.
        /// </summary>
        /// <returns>The collection of child SecurityPredicates to update. </returns>
        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            ArrayList propInfo = new ArrayList();
            propInfo.Add(new PropagateInfo(SecurityPredicates, false /* don't propagate actions */, false /* don't propagate actions */));
            propInfo.Add(new PropagateInfo(!this.IsSupportedObject<ExtendedProperty>() ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix));
            PropagateInfo[] retArr = new PropagateInfo[propInfo.Count];
            propInfo.CopyTo(retArr, 0);

            return retArr;
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
                                                "Owner",
                                                "NotForReplication",
                                                "Enabled",
                                                "IsSchemaBound"
                              };
            List<string> list = GetSupportedScriptFields(typeof(SecurityPolicy.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


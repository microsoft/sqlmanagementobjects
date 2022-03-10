// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server security predicate object.
    ///</summary>
    public partial class SecurityPredicate : SqlSmoObject, Cmn.IAlterable, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IMarkForDrop
    {
        internal SecurityPredicate(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Parameterized constructor for creating a security predicate which targets a table.
        /// Populates properties from parameter values.
        /// </summary>
        /// <param name="parent">The parent security policy</param>
        /// <param name="table">The table to which this predicate applies</param>
        /// <param name="predicateDefinition">The predicate definition as a string</param>
        public SecurityPredicate(SecurityPolicy parent, Table table, string predicateDefinition) :
            this(parent, table.Schema, table.Name, table.ID, predicateDefinition)
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// </summary>
        /// <param name="parent">The parent security policy</param>
        /// <param name="targetObjectSchema">The schema which owns the target object</param>
        /// <param name="targetObjectName">The target object name</param>
        /// <param name="targetObjectId">The target object's object id</param>
        /// <param name="predicateDefinition">The predicate definition as a string</param>
        /// <remarks>This constructor allows creation of security predicates against views and other object types.</remarks>
        public SecurityPredicate(SecurityPolicy parent, string targetObjectSchema, string targetObjectName, int targetObjectId, string predicateDefinition)
        {
            this.SetParentImpl(parent);

            // Calculate an estimated security predicate ID. This is not guaranteed to be the true ID after creation, but prior
            // to creation we only need the ID to add this predicate to the parent policy's list of security predicates.
            //
            if (parent.SecurityPredicates.Count == 0)
            {
                this.SecurityPredicateID = 1;
            }
            else
            {
                this.SecurityPredicateID = parent.SecurityPredicates[parent.SecurityPredicates.Count - 1].SecurityPredicateID + 1;
            }

            this.key = new SecurityPredicateObjectKey(SecurityPredicateID);
            this.TargetObjectName = targetObjectName;
            this.TargetObjectSchema = targetObjectSchema;
            this.TargetObjectID = targetObjectId;
            this.PredicateDefinition = predicateDefinition;
            this.PredicateType = SecurityPredicateType.Filter;
            this.PredicateOperation = SecurityPredicateOperation.All;
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "SecurityPredicate";
            }
        }

        internal String FullQualifiedTargetName
        {
            get
            {
                return String.Format("[{0}].[{1}]", SqlBraket(this.TargetObjectSchema), SqlBraket(this.TargetObjectName));
            }
        }

        /// <summary>
        /// Gets or sets the predicate definition.  The getter will remove any unnecessary parentheses.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.String PredicateDefinition
        {
            get
            {
                // We need to remove any surrounding parentheses that are automatically added by the engine.
                //
                String predicateDefinition = (String) this.Properties.GetValueWithNullReplacement("PredicateDefinition");
                if (predicateDefinition != null)
                {
                    while (predicateDefinition[0] == '(' && predicateDefinition[predicateDefinition.Length - 1] == ')')
                    {
                        predicateDefinition = predicateDefinition.Substring(1, predicateDefinition.Length - 2);
                    }
                }

                return predicateDefinition;
            }
            set
            {
                Properties.SetValueWithConsistencyCheck("PredicateDefinition", value);
            }
        }

        /// <summary>
        /// Marks or unmarks the Security Predicate for drop on the next alter called on the parent policy
        /// </summary>
        /// <param name="dropOnAlter">Whether the predicate should be marked for drop.</param>
        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        /// <summary>
        /// Drop the Security Predicate
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
            
            string fullyFormattedSecPolName = this.Parent.FullQualifiedName;

            CheckObjectState();

            // need to see if it is an app role, defaults to false
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "SecurityPolicy", fullyFormattedSecPolName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SECURITY_PREDICATE, String.Empty, this.TargetObjectID, this.Parent.ID);
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SECURITY POLICY {0}", fullyFormattedSecPolName);
            ScriptPredicate(sb, sp);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Create the Security Predicate this will fail unless the parent Security Policy has been created.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        //ALTER SECURITY POLICY [ schema_name. ] security_policy_name
        //  { ADD FILTER PREDICATE tvf_schema_name.security_predicate_function_name 
        //      ( { column_name | arguments } [ , …n] ) ON table_schema_name.table_name }
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            
            string fullyFormattedSecPolName = this.Parent.FullQualifiedName;

            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "SecurityPolicy", fullyFormattedSecPolName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            int targetObjectID = Convert.ToInt32(this.GetPropValue("TargetObjectID"));
            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_SECURITY_PREDICATE, "NOT", this.TargetObjectID, this.Parent.ID);
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SECURITY POLICY {0}", fullyFormattedSecPolName);
            ScriptPredicate(sb, sp, forCreate:true);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Alter the Security Policy
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        //ALTER SECURITY POLICY [ schema_name. ] security_policy_name
        //        { ALTER FILTER PREDICATE tvf_schema_name.security_predicate_function_name 
        //            ( { column_name | arguments } [ , …n] ) ON table_schema_name.table_name }
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            
            if (!IsObjectDirty() || this.State == SqlSmoState.Creating)
            {
                return;
            }

            string fullyFormattedSecPolName = this.Parent.FullQualifiedName;
            StringBuilder sb = new StringBuilder();

            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "SecurityPolicy", fullyFormattedSecPolName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SECURITY POLICY {0}", fullyFormattedSecPolName);
            ScriptPredicate(sb, sp);

            alterQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Scripts out the security predicate based on the current state of the predicate for a create or alter security policy statement.
        /// </summary>
        /// <param name="sb">A stringbuilder with the target alter query.</param>
        /// <param name="sp">The scripting preferences.</param>
        /// <param name="forCreate">Whether we are scripting a security policy create, in which case we should always default to ADD.</param>
        internal void ScriptPredicate(StringBuilder sb, ScriptingPreferences sp, bool forCreate = false)
        {
            TypeConverter securityPredicateTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(SecurityPredicateType));
            TypeConverter securityPredicateOperationConverter = SmoManagementUtil.GetTypeConverter(typeof(SecurityPredicateOperation));
            string predicateType = securityPredicateTypeConverter.ConvertToInvariantString(this.PredicateType);
            string predicateOperation = securityPredicateOperationConverter.ConvertToInvariantString(this.PredicateOperation);
            string predicateDefinition = this.PredicateDefinition;
            string targetObjectName = this.FullQualifiedTargetName;
            sb.Append(Globals.newline);
            if (this.State == SqlSmoState.Creating || forCreate)
            {
                sb.AppendFormat("ADD {0} PREDICATE {1} ON {2}", predicateType, predicateDefinition, targetObjectName);
            }
            else if (this.State == SqlSmoState.ToBeDropped || sp.Behavior == ScriptBehavior.Drop || sp.Behavior == ScriptBehavior.DropAndCreate)
            {
                sb.AppendFormat("DROP {0} PREDICATE ON {1}", predicateType, targetObjectName);
            }
            else
            {
                sb.AppendFormat("ALTER {0} PREDICATE {1} ON {2}", predicateType, predicateDefinition, targetObjectName);
            }

            if (predicateOperation != string.Empty)
            {
                sb.AppendFormat(" {0}", predicateOperation);
            }
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
                                                "TargetObjectName",
                                                "TargetObjectSchema",
                                                "TargetObjectID",
                                                "PredicateDefinition",
                                                "PredicateType",
                                                "PredicateOperation"
                              };
            List<string> list = GetSupportedScriptFields(typeof(SecurityPredicate.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


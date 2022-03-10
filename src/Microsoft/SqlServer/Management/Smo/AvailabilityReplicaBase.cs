// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// An Availability Replica is an instance of SQL Server that is part of an Availability Group.
    /// The replica hosts copies of the databases in a group.
    /// Depending on its current roles, the replica can be the primary of the Availability Group
    /// or one of many secondaries.
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class AvailabilityReplica : NamedSmoObject, ICreatable, IDroppable, IDropIfExists, IAlterable, IScriptable
    {
        internal AvailabilityReplica(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        private const char ReadOnlyRoutingListReplicaNameSeparator = ',';
        private const char ReadOnlyRoutingLoadBalancingGroupStartCharacter = '(';
        private const char ReadOnlyRoutingLoadBalancingGroupEndCharacter = ')';

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get { return "AvailabilityReplica"; }
        }

        static AvailabilityReplica()
        {
            // See VSTS# 728443. When the entries in AlterableReplicaProperties are changed, modify the 
            // OrderedAlterableReplicaProperties accordingly.
            AlterableReplicaProperties = new Dictionary<string, PropertyType>();
            AddAlterableReplicaProperty(EndPointUrlPropertyName, PropertyType.Alterable);
            AddAlterableReplicaProperty(FailoverModePropertyName, PropertyType.Alterable);
            AddAlterableReplicaProperty(AvailabilityModePropertyName, PropertyType.Alterable);
            AddAlterableReplicaProperty(SessionTimeoutPropertyName, PropertyType.Alterable);
            AddAlterableReplicaProperty(BackupPriorityPropertyName, PropertyType.Alterable);
            AddAlterableReplicaProperty(SeedingModePropertyName, PropertyType.Alterable);
            AddAlterableReplicaProperty(ConnectionModeInPrimaryRolePropertyName, PropertyType.Alterable | PropertyType.PrimaryRoleProperty);
            AddAlterableReplicaProperty(ReadonlyRoutingListPropertyName, PropertyType.Alterable | PropertyType.PrimaryRoleProperty);
            AddAlterableReplicaProperty(ConnectionModeInSecondaryRolePropertyName, PropertyType.Alterable | PropertyType.SecondaryRoleProperty);
            AddAlterableReplicaProperty(ReadonlyRoutingConnectionUrlPropertyName, PropertyType.Alterable | PropertyType.SecondaryRoleProperty);
        }

        #region Public Interface

        /// <summary>
        /// An ordered list of replica server names to be used for routing read-only connections
        /// when this replica is in a primary role
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public StringCollection ReadonlyRoutingList
        {
            get
            {
                if (this.readonlyRoutingList == null)
                {
                    this.readonlyRoutingList = new StringCollection();
                    string delimitedList = string.Empty;

                    //We need to prime the delimited list from the backend,
                    //but since this is a readonly property, the priming doesn't 
                    //make sense if the object is still in the air.
                    if (this.State != SqlSmoState.Creating && this.ReadonlyRoutingListDelimited.IndexOf(ReadOnlyRoutingLoadBalancingGroupStartCharacter) == -1)
                    {
                        delimitedList = this.ReadonlyRoutingListDelimited;
                    }

                    if (!string.IsNullOrEmpty(delimitedList))
                    {
                        string[] list = delimitedList.Split(ReadOnlyRoutingListReplicaNameSeparator);
                        foreach (string s in list)
                        {
                            // s here is N'Replica' format, need to remove the marker
                            this.readonlyRoutingList.Add(s.Substring(2, s.Length - 3));
                        }
                    }
                }

                return this.readonlyRoutingList;
            }
        }

        /// <summary>
        /// Load balanced read-only routing list
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public IList<IList<string>> LoadBalancedReadOnlyRoutingList
        {
            get
            {
                if (this.loadBalancedReadonlyRoutingList == null)
                {
                    if (this.State != SqlSmoState.Creating)
                    {
                        this.loadBalancedReadonlyRoutingList = ConvertToReadOnlyRoutingList(this.ReadonlyRoutingListDelimited);
                    }
                    else
                    {
                        this.loadBalancedReadonlyRoutingList = new List<IList<string>>();
                    }
                }

                return this.loadBalancedReadonlyRoutingList;
            }
        }

        /// <summary>
        /// Display string of the load balanced read-only routing list
        /// </summary>
        public string LoadBalancedReadOnlyRoutingListDisplayString
        {
            get { return AvailabilityReplica.ConvertReadOnlyRoutingListToString(this.LoadBalancedReadOnlyRoutingList); }
        }

        /// <summary>
        /// Set the load balanced read-only routing list
        /// </summary>
        /// <param name="routingList"></param>
        public void SetLoadBalancedReadOnlyRoutingList(IList<IList<string>> routingList)
        {
            if (routingList == null)
            {
                throw new ArgumentNullException("routingList");
            }

            this.LoadBalancedReadOnlyRoutingList.Clear();
            if (routingList.Count != 0)
            {
                foreach (var item in routingList.Where(item => item != null && item.Count != 0))
                {
                    this.LoadBalancedReadOnlyRoutingList.Add(item);
                }
            }
        }

        /// <summary>
        /// Refresh the state of the object from the backend.
        /// </summary>
        public override void Refresh()
        {
            //refreshing the data from the server invalidates the values in the readonly routing list
            //since this property is outside the property bag, we will null it explicitly here.
            base.Refresh();
            this.readonlyRoutingList = null;
            this.loadBalancedReadonlyRoutingList = null;
        }

        /// <summary>
        /// Gets a bool flag indicating whether seeding mode is supported
        /// </summary>
        public bool IsSeedingModeSupported
        {
            get { return IsSupportedProperty(SeedingModePropertyName); }
        }

        #region ICreatable Members

        /// <summary>
        /// Create an availability replica for the availability group.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        #endregion

        #region IDroppable Members

        /// <summary>
        /// Drop an availaiblity replica from the availability group.
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

        #endregion

        #region IAlterable Members

        /// <summary>
        /// Alter an existing replica options
        /// </summary>
        public void Alter()
        {
            this.CheckObjectState(!this.ExecutionManager.Recording);
            base.AlterImpl();
        }

        #endregion

        #region IScriptable Members

        /// <summary>
        /// Generate a create script this availability replica.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate a script for this availability replica using the
        /// specified scripting options.
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        #endregion

        #endregion

        #region override methods

        /// <summary>
        /// Composes the create script for the availability replica object.
        /// </summary>
        /// <param name="query">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptCreate(System.Collections.Specialized.StringCollection query, ScriptingPreferences sp)
        {
            /* ========================================================================================
             * ADVANCED Availability Groups
             * ========================================================================================
             * 
             * CREATE AVAILABILITY GROUP �group_name�
             * REPLICA ON <replica_spec>[, ...n]
             * [;]
             * 
             * <replica_spec> ::=
             *      'server_instance' WITH
             *          (
             *              ENDPOINT_URL = 'TCP://system-address:port',
             *              AVAILABILITY_MODE = { SYNCHRONOUS_COMMIT | ASYNCHRONOUS_COMMIT },
             *              FAILOVER_MODE = { AUTOMATIC | MANUAL }
             *              [, <replica_option>[, ...n ]]
             *          )
             *
             * <replica_option> ::= 
             *          BACKUP_PRIORITY = <nnn>
             *          | PRIMARY_ROLE (<primary_role_option>[, ...n])
             *          | SECONDARY_ROLE (<secondary_role_option>[, ...n])
             *          | SESSION_TIMEOUT = seconds
             *
             * <primary_role_option> ::= 
             *          ALLOW_CONNECTIONS = { READ_WRITE | ALL }
             *          | READ_ONLY_ROUTING_LIST = { ( �<server_instance>� [ ,...n ] ) | NONE  }
             *
             * <secondary_role_option> ::= 
             *          ALLOW_CONNECTIONS = { NO | READ_ONLY | ALL }
             *          | READ_ONLY_ROUTING_URL = 'TCP://system-address:port'
             * 
             * ========================================================================================
             * BASIC Availability Groups
             * ========================================================================================
             * 
             * CREATE AVAILABILITY GROUP �group_name�
             * REPLICA ON <replica_spec>[, <replica_spec>]
             * [;]
             * 
             * <replica_spec> ::=
             *      'server_instance' WITH
             *          (
             *              ENDPOINT_URL = 'TCP://system-address:port',
             *              AVAILABILITY_MODE = { SYNCHRONOUS_COMMIT | ASYNCHRONOUS_COMMIT },
             *              FAILOVER_MODE = { AUTOMATIC | MANUAL }
             *              [, <replica_option>[, ...n ]]
             *          )
             *
             * <replica_option> ::= 
             *          BACKUP_PRIORITY = <nnn>
             *          | PRIMARY_ROLE (ALLOW_CONNECTIONS = ALL)
             *          | SECONDARY_ROLE (ALLOW_CONNECTIONS = NO)
             *          | SESSION_TIMEOUT = seconds
             */

            //sanity checks
            tc.Assert(null != query, "String collection should not be null");

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability replica doesn't exist before creating it
                string myName = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_REPLICA, "NOT", myName, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            //Script the availability group name
            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append(Scripts.ADD + Globals.space + AvailabilityGroup.ReplicaOn + Globals.space);
            script.Append(SqlSmoObject.MakeSqlString(this.Name) + Globals.space + Globals.With + Globals.space + Globals.LParen);
            //Add script for replica spec 
            script.Append(this.ScriptReplicaOptions(sp));
            script.Append(Globals.RParen);

            //Add statement terminator
            script.Append(Globals.statementTerminator);

            //Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            query.Add(script.ToString());

            return;
        }

        /// <summary>
        /// We will have to emit multiple Alter statements here since the engine does not allow multiple options in one shot.
        /// 
        /// Also, if both the sync and failover modes are changing together, then the order of the Alter statements emitted will
        /// depend on the start and end states. So, this will be handled as a special case.
        /// </summary>
        /// <param name="query">A collection of scripts corresponding to the different options being altered.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            /* ========================================================================================
             * ADVANCED Availability Groups
             * ========================================================================================
             * 
             * ALTER AVAILABILITY GROUP �group_name�
             * MODIFY REPLICA ON <replica_spec>
             * 
             * <replica_spec> ::= �server_name� WITH (<replica_option>) -- only one replica option at a time
             * 
             * <replica_option> ::= 
             *          ENDPOINT_URL = 'TCP://system-address:port� 
             *          | AVAILABILITY_MODE= { SYNCHRONOUS_COMMIT | ASYNCHRONOUS_COMMIT }
             *          | FAILOVER_MODE = { AUTOMATIC | MANUAL }
             *          | BACKUP_PRIORITY = <nnn>
             *          | PRIMARY_ROLE (<primary_role_option>)
             *          | SECONDARY_ROLE (<secondary_role_option>)
             *          | SESSION_TIMEOUT = seconds
             *
             * <primary_role_option> ::= 
             *          ALLOW_CONNECTIONS = { READ_WRITE | ALL }
             *          | READ_ONLY_ROUTING_LIST = { ( �<server_instance>� [ ,...n ] ) | NONE  }
             *
             * <secondary_role_option> ::= 
             *          ALLOW_CONNECTIONS = { NO | READ_ONLY | ALL }
             *          | READ_ONLY_ROUTING_URL = 'TCP://system-address:port'
             *          
             * ========================================================================================
             * BASIC Availability Groups
             * ========================================================================================
             * 
             * ALTER AVAILABILITY GROUP �group_name�
             * MODIFY REPLICA ON <replica_spec>
             * 
             * <replica_spec> ::= �server_name� WITH (<replica_option>) -- only one replica option at a time
             * 
             * <replica_option> ::= 
             *          ENDPOINT_URL = 'TCP://system-address:port� 
             *          | SECONDARY_ROLE (ALLOW_CONNECTIONS = NO)
             *          | PRIMARY_ROLE (ALLOW_CONNECTIONS = ALL)
             *          | AVAILABILITY_MODE= { SYNCHRONOUS_COMMIT | ASYNCHRONOUS_COMMIT }
             *          | FAILOVER_MODE = { AUTOMATIC | MANUAL }
             *          | SESSION_TIMEOUT = seconds
             *          | BACKUP_PRIORITY = <nnn>
             */

            //sanity checks
            tc.Assert(null != query, "String collection should not be null");

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            string availabilityModeScript = null;
            string failoverModeScript = null;

            foreach (string propertyName in OrderedAlterableReplicaProperties)
            {
                string propertyAlterScript = this.ScriptAlterOneOption(propertyName, sp);
                if (!string.IsNullOrEmpty(propertyAlterScript))
                {
                    if (propertyName == AvailabilityModePropertyName)
                    {
                        availabilityModeScript = propertyAlterScript;
                    }
                    else if (propertyName == FailoverModePropertyName)
                    {
                        failoverModeScript = propertyAlterScript;
                    }
                    else
                    {
                        query.Add(propertyAlterScript);
                    }
                }
            }

            //Now, if both failover mode and availability modes are changing
            //and we are moving to asynchronous mode, then we will change the failover mode first 
            //(we are guaranteed to have been in automatic going to manual)
            if (availabilityModeScript != null && failoverModeScript != null && this.AvailabilityMode == AvailabilityReplicaAvailabilityMode.AsynchronousCommit)
            {
                query.Add(failoverModeScript);
                query.Add(availabilityModeScript);
            }
            else //for all other cases will add the availability mode script first
            {
                if (availabilityModeScript != null)
                {
                    query.Add(availabilityModeScript);
                }
                if (failoverModeScript != null)
                {
                    query.Add(failoverModeScript);
                }
            }

            return;
        }

        internal string ScriptAlterOneOption(string propertyName, ScriptingPreferences sp)
        {
            string replicaOptionScript = this.ScriptReplicaOption(false, propertyName, sp);

            if (string.IsNullOrEmpty(replicaOptionScript))
            {
                return null;
            }

            //check for options that are part of the primary or secondary roles and if so, flank it inside the correct DDL
            if (this.IsPrimaryRoleProperty(propertyName))
            {
                replicaOptionScript = PrimaryRoleScript + Globals.LParen + replicaOptionScript + Globals.RParen;
            }
            else if (this.IsSecondaryRoleProperty(propertyName))
            {
                replicaOptionScript = SecondaryRoleScript + Globals.LParen + replicaOptionScript + Globals.RParen;
            }

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string scriptName = this.FormatFullNameForScripting(sp);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability replica exists before altering it
                string myName = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_REPLICA, "", myName, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append(ModifyReplicaScript + Globals.space + AvailabilityGroup.ReplicaOn + Globals.space);
            script.Append(SqlSmoObject.MakeSqlString(this.Name) + Globals.space + Globals.With + Globals.space + Globals.LParen);
            script.Append(replicaOptionScript + Globals.RParen);

            //Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            return script.ToString();
        }

        /// <summary>
        /// Create the script to drop the availability replica
        /// </summary>
        /// <param name="dropQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptDrop(System.Collections.Specialized.StringCollection dropQuery, ScriptingPreferences sp)
        {
            //sanity checks
            tc.Assert(null != dropQuery, "String collection should not be null");

            //ALTER AVAILABILITY GROUP �group_name�
            //REMOVE REPLICA ON �server_name� 

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability replica exists before dropping it
                string myName = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_REPLICA, "", myName, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append(Scripts.REMOVE + Globals.space + AvailabilityGroup.ReplicaOn + Globals.space + SqlSmoObject.MakeSqlString(this.Name));
            //Add statement terminator
            script.Append(Globals.statementTerminator);

            //Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            dropQuery.Add(script.ToString());
            return;
        }

        internal string ScriptReplicaOption(bool scriptAll, string propertyName, ScriptingPreferences scriptingPreferences)
        {
            // Only script availability mode and endpoint URL for configuration replica
            //
            if (AvailabilityMode == AvailabilityReplicaAvailabilityMode.ConfigurationOnly && !ConfigurationOnlyModeProperties.Contains(propertyName))
            {
                return null;
            }

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            //readonly routing list is a special case
            if (propertyName.Equals(ReadonlyRoutingListPropertyName))
            {
                script.Append(this.ScriptReadonlyRoutingList());
            }
            else if (IsSupportedProperty(propertyName, scriptingPreferences))
            {
                Property prop = GetPropertyOptional(propertyName);

                if (!prop.IsNull && (scriptAll || IsDirty(propertyName)))
                {
                    switch (propertyName)
                    {
                        case EndPointUrlPropertyName:
                            script.Append(EndpointUrlScript + Globals.space + Globals.EqualSign + Globals.space);
                            script.Append(SqlSmoObject.MakeSqlString(EndpointUrl));
                            break;

                        case AvailabilityModePropertyName:
                            script.Append(AvailabilityModeScript + Globals.space + Globals.EqualSign + Globals.space);
                            var availabilityModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaAvailabilityMode));
                            script.Append(availabilityModeTypeConverter.ConvertToInvariantString(this.AvailabilityMode));
                            break;

                        case FailoverModePropertyName:
                            script.Append(FailoverModeScript + Globals.space + Globals.EqualSign + Globals.space);
                            var failoverModetypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaFailoverMode));
                            script.Append(failoverModetypeConverter.ConvertToInvariantString(this.FailoverMode));
                            break;

                        case ConnectionModeInPrimaryRolePropertyName:
                            script.Append(AllowConnectionsScript + Globals.space + Globals.EqualSign + Globals.space);
                            var connectionModeInPrimaryRoleTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaConnectionModeInPrimaryRole));
                            script.Append(connectionModeInPrimaryRoleTypeConverter.ConvertToInvariantString(this.ConnectionModeInPrimaryRole));
                            break;

                        case ConnectionModeInSecondaryRolePropertyName:
                            script.Append(AllowConnectionsScript + Globals.space + Globals.EqualSign + Globals.space);
                            var connectionModeInSecondaryRoleTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaConnectionModeInSecondaryRole));
                            script.Append(connectionModeInSecondaryRoleTypeConverter.ConvertToInvariantString(this.ConnectionModeInSecondaryRole));
                            break;

                        case SessionTimeoutPropertyName:
                            script.Append(SessionTimeoutScript + Globals.space + Globals.EqualSign + Globals.space + this.SessionTimeout);
                            break;

                        case ReadonlyRoutingConnectionUrlPropertyName:
                            if (!string.IsNullOrEmpty(this.ReadonlyRoutingConnectionUrl))
                            {
                                script.Append(ReadonlyRoutingConnectionUrlScript + Globals.space + Globals.EqualSign + Globals.space);
                                script.Append(SqlSmoObject.MakeSqlString(this.ReadonlyRoutingConnectionUrl));
                            }
                            break;

                        case BackupPriorityPropertyName:
                            if (scriptingPreferences.TargetServerVersionInternal >= SqlServerVersionInternal.Version130)
                            {
                                Property basicAgProp = this.Parent.GetPropertyOptional(AvailabilityGroup.BasicAvailabilityGroupPropertyName);
                                if (basicAgProp.IsNull || (basicAgProp.Value.Equals(false)))
                                {
                                    script.Append(BackupPriorityScript + Globals.space + Globals.EqualSign + Globals.space + this.BackupPriority);
                                }
                            }
                            else
                            {
                                script.Append(BackupPriorityScript + Globals.space + Globals.EqualSign + Globals.space + this.BackupPriority);
                            }
                            break;

                        case SeedingModePropertyName:
                            var seedingModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaSeedingMode));
                            script.Append(SeedingModeScript + Globals.space + Globals.EqualSign + Globals.space + seedingModeTypeConverter.ConvertToInvariantString(this.SeedingMode));
                            break;

                        default:
                            break;
                    }
                }
            }

            return script.ToString();
        }

        internal string ScriptReplicaOptions(ScriptingPreferences scriptingPreferences)
        {
            // Ensure required properties are set.
            this.CheckRequiredPropertiesSetBeforeCreation();

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            //because of the oddities in the DDL syntax, we need to treat the properties modified 
            //through the primary and secondary replica roles differently and construct their script
            //in isolation
            StringCollection primaryRoleScripts = new StringCollection();
            StringCollection secondaryRoleScripts = new StringCollection();

            foreach (string propertyName in OrderedAlterableReplicaProperties)
            {
                string replicaOption = this.ScriptReplicaOption(true, propertyName, scriptingPreferences);

                if (!string.IsNullOrEmpty(replicaOption))
                {
                    if (this.IsPrimaryRoleProperty(propertyName))
                    {
                        primaryRoleScripts.Add(replicaOption);
                    }
                    else if (this.IsSecondaryRoleProperty(propertyName))
                    {
                        secondaryRoleScripts.Add(replicaOption);
                    }
                    else
                    {
                        script.Append(replicaOption + Globals.comma + Globals.space);
                    }
                }
            }

            //Add the primary and secondary role DDLs if there are any properties that need adding
            this.AppendReplicaRoleScripts(primaryRoleScripts, PrimaryRoleScript, script);

            //Add the secondary role DDLs if there are any properties that need adding
            this.AppendReplicaRoleScripts(secondaryRoleScripts, SecondaryRoleScript, script);

            if (script.Length > 0)
            {
                script.Remove(script.Length - 2, 2); //chop the last comma
            }

            return script.ToString();
        }

        internal string ScriptDistributedAvailabilityGroupReplicaOptions()
        {
            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            script.Append(Globals.LParen);

            script.Append(ListenerUrlScript + Globals.space + Globals.EqualSign + Globals.space);
            script.Append(SqlSmoObject.MakeSqlString(EndpointUrl));
            script.Append(Globals.comma + Globals.space);

            script.Append(AvailabilityModeScript + Globals.space + Globals.EqualSign + Globals.space);
            var availabilityModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaAvailabilityMode));
            script.Append(availabilityModeTypeConverter.ConvertToInvariantString(this.AvailabilityMode));
            script.Append(Globals.comma + Globals.space);

            script.Append(FailoverModeScript + Globals.space + Globals.EqualSign + Globals.space);
            var failoverModetypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaFailoverMode));
            script.Append(failoverModetypeConverter.ConvertToInvariantString(this.FailoverMode));
            script.Append(Globals.comma + Globals.space);

            var seedingModeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(AvailabilityReplicaSeedingMode));
            script.Append(SeedingModeScript + Globals.space + Globals.EqualSign + Globals.space + seedingModeTypeConverter.ConvertToInvariantString(this.SeedingMode));

            script.Append(Globals.RParen);

            return script.ToString();
        }

        private string ScriptReadonlyRoutingList()
        {
            string routingListTSQL;
            string clientLoadBalancedRoutingListTSQL = ConvertReadOnlyRoutingListToString(this.LoadBalancedReadOnlyRoutingList, tsqlCompatible: true);
            string serverRoutingListTSQL = this.State == SqlSmoState.Creating ? string.Empty : this.ReadonlyRoutingListDelimited;
            if (this.State != SqlSmoState.Creating && this.ReadonlyRoutingListDelimited.IndexOf(ReadOnlyRoutingLoadBalancingGroupStartCharacter) != -1)
            {
                routingListTSQL = clientLoadBalancedRoutingListTSQL;
            }
            else
            {
                string clientRoutingListTSQL = string.Join(",", ReadonlyRoutingList.Cast<string>().Select(replica => SqlSmoObject.MakeSqlString(replica)).ToArray());

                if ((this.State != SqlSmoState.Creating
                     && !string.Equals(clientRoutingListTSQL, serverRoutingListTSQL, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(clientLoadBalancedRoutingListTSQL, serverRoutingListTSQL, StringComparison.OrdinalIgnoreCase))
                    || (this.State == SqlSmoState.Creating
                        && this.ReadonlyRoutingList.Count != 0
                        && this.LoadBalancedReadOnlyRoutingList.Count != 0
                        ))
                {
                    throw new SmoException(ExceptionTemplatesImpl.CannotUpdateBothReadOnlyRoutingLists);
                }

                routingListTSQL = string.Equals(clientRoutingListTSQL, serverRoutingListTSQL, StringComparison.OrdinalIgnoreCase) ? clientLoadBalancedRoutingListTSQL : clientRoutingListTSQL;
            }

            StringBuilder finalScript = new StringBuilder();

            if (!string.Equals(routingListTSQL, serverRoutingListTSQL, StringComparison.OrdinalIgnoreCase))
            {
                finalScript.Append(ReadonlyRoutingListScript + Globals.space + Globals.EqualSign + Globals.space);

                if (!string.IsNullOrEmpty(routingListTSQL))
                {
                    finalScript.Append(Globals.LParen + routingListTSQL + Globals.RParen);
                }
                else
                {
                    finalScript.Append(NoneScript);
                }
            }

            return finalScript.ToString();
        }

        private void AppendReplicaRoleScripts(StringCollection replicaRolePropertyScriptCollection, string roleDDLScript, StringBuilder script)
        {
            if (replicaRolePropertyScriptCollection.Count > 0)
            {
                StringBuilder roleScript = new StringBuilder();
                roleScript.Append(roleDDLScript + Globals.LParen);

                foreach (string primaryScript in replicaRolePropertyScriptCollection)
                {
                    roleScript.Append(primaryScript + Globals.comma + Globals.space);
                }
                roleScript.Remove(roleScript.Length - 2, 2); //chop the last comma and space
                roleScript.Append(Globals.RParen);

                script.Append(roleScript.ToString() + Globals.comma + Globals.space);
            }

            return;
        }

        #endregion

        #region Constants

        //Scripting constants, the suffix 'Script' is added for disambiguation from class names
        internal static readonly string TargetNameScript = "TARGETNAME";
        internal static readonly string ModifyReplicaScript = "MODIFY";
        internal static readonly string EndpointUrlScript = "ENDPOINT_URL";
        internal static readonly string PrimaryRoleScript = "PRIMARY_ROLE";
        internal static readonly string SecondaryRoleScript = "SECONDARY_ROLE";
        internal static readonly string BackupPriorityScript = "BACKUP_PRIORITY";
        internal static readonly string SeedingModeScript = "SEEDING_MODE";
        internal static readonly string ListenerUrlScript = "LISTENER_URL";

        internal static readonly string ReadonlyRoutingConnectionUrlScript = "READ_ONLY_ROUTING_URL";
        internal static readonly string ReadonlyRoutingListScript = "READ_ONLY_ROUTING_LIST";
        internal static readonly string NoneScript = "NONE";

        internal static readonly string AllowConnectionsScript = "ALLOW_CONNECTIONS";

        internal static readonly string AvailabilityModeScript = "AVAILABILITY_MODE";

        internal static readonly string FailoverModeScript = "FAILOVER_MODE";

        internal static readonly string SessionTimeoutScript = "SESSION_TIMEOUT";

        //Constants needed for the Alter script
        internal const string EndPointUrlPropertyName = "EndpointUrl";
        internal const string AvailabilityModePropertyName = "AvailabilityMode";
        internal const string FailoverModePropertyName = "FailoverMode";
        internal const string ConnectionModeInPrimaryRolePropertyName = "ConnectionModeInPrimaryRole";
        internal const string ConnectionModeInSecondaryRolePropertyName = "ConnectionModeInSecondaryRole";
        internal const string SessionTimeoutPropertyName = "SessionTimeout";
        internal const string BackupPriorityPropertyName = "BackupPriority";
        internal const string SeedingModePropertyName = "SeedingMode";
        internal const string ReadonlyRoutingConnectionUrlPropertyName = "ReadonlyRoutingConnectionUrl";
        internal const string ReadonlyRoutingListPropertyName = "ReadonlyRoutingList";

        internal static readonly string[] RequiredPropertyNames = {EndPointUrlPropertyName, FailoverModePropertyName, AvailabilityModePropertyName};

        internal static readonly string[] ConfigurationOnlyModeProperties = {AvailabilityModePropertyName, EndPointUrlPropertyName};

        #endregion

        #region private members

        [Flags]
        private enum PropertyType
        {
            Alterable = 0x1,

            PrimaryRoleProperty = 0x2,

            SecondaryRoleProperty = 0x4,
        }

        private static Dictionary<string, PropertyType> AlterableReplicaProperties;

        // VSTS 728443: store the order of properties to ensure that the genetated scripts follow this order.  
        private static string[] OrderedAlterableReplicaProperties =
        {
            EndPointUrlPropertyName,
            FailoverModePropertyName,
            AvailabilityModePropertyName,
            SessionTimeoutPropertyName,
            BackupPriorityPropertyName,
            ConnectionModeInPrimaryRolePropertyName,
            ReadonlyRoutingConnectionUrlPropertyName,
            ReadonlyRoutingListPropertyName,
            ConnectionModeInSecondaryRolePropertyName,
            SeedingModePropertyName
        };

        private StringCollection readonlyRoutingList;
        private IList<IList<string>> loadBalancedReadonlyRoutingList;

        [SfcProperty(SfcPropertyFlags.Standalone)]
        private String ReadonlyRoutingListDelimited
        {
            get { return (String)this.Properties.GetValueWithNullReplacement("ReadonlyRoutingListDelimited"); }
        }

        private static readonly TraceContext tc = TraceContext.GetTraceContext(SmoApplication.ModuleName, "AvailabilityReplica");

        private bool IsDirty(string property)
        {
            return this.Properties.IsDirty(this.Properties.LookupID(property, PropertyAccessPurpose.Read));
        }

        private void CheckRequiredPropertiesSetBeforeCreation()
        {
            foreach (string propertyName in RequiredPropertyNames)
            {
                if (this.GetPropValueOptional(propertyName) == null)
                {
                    throw new PropertyNotSetException(propertyName);
                }
            }

            return;
        }

        private bool IsAlterableProperty(string propertyName)
        {
            PropertyType propertyType;
            bool found = AlterableReplicaProperties.TryGetValue(propertyName, out propertyType);

            tc.Assert(found, "Invalid property name: " + propertyName);

            return (propertyType & PropertyType.Alterable) == PropertyType.Alterable;
        }

        private bool IsPrimaryRoleProperty(string propertyName)
        {
            PropertyType propertyType;
            bool found = AlterableReplicaProperties.TryGetValue(propertyName, out propertyType);

            tc.Assert(found, "Invalid property name: " + propertyName);

            return (propertyType & PropertyType.PrimaryRoleProperty) == PropertyType.PrimaryRoleProperty;
        }

        private bool IsSecondaryRoleProperty(string propertyName)
        {
            PropertyType propertyType;
            bool found = AlterableReplicaProperties.TryGetValue(propertyName, out propertyType);

            tc.Assert(found, "Invalid property name: " + propertyName);

            return (propertyType & PropertyType.SecondaryRoleProperty) == PropertyType.SecondaryRoleProperty;
        }

        private static void AddAlterableReplicaProperty(string propertyName, PropertyType propertyType)
        {
#if DEBUG

            bool existed = false;

            foreach (string name in OrderedAlterableReplicaProperties)
            {
                if (name == propertyName)
                {
                    existed = true;
                    break;
                }
            }

            if (!existed)
            {
                throw new ArgumentOutOfRangeException("PropertyName");
            }

#endif

            AlterableReplicaProperties.Add(propertyName, propertyType);
        }

        /// <summary>
        /// Convert the read-only routing list from its string representation.
        /// </summary>
        /// <param name="readOnlyRoutingListDisplayString">The string representation of the read-only routing list. e.g. N'replica1',(N'replica2',N'replica3'),replica4</param>
        /// <returns>The read-only routing list</returns>
        private static IList<IList<string>> ConvertToReadOnlyRoutingList(string readOnlyRoutingListDisplayString)
        {
            var list = new List<IList<string>>();

            if (!string.IsNullOrEmpty(readOnlyRoutingListDisplayString))
            {
                var idx = 0;
                var listItem = new List<string>();
                var currentlyInALoadBalancingGroup = false;
                while (idx < readOnlyRoutingListDisplayString.Length)
                {
                    switch (readOnlyRoutingListDisplayString[idx])
                    {
                        case ReadOnlyRoutingLoadBalancingGroupStartCharacter:
                            currentlyInALoadBalancingGroup = true;
                            idx++;
                            break;

                        case ReadOnlyRoutingLoadBalancingGroupEndCharacter:
                            currentlyInALoadBalancingGroup = false;
                            list.Add(listItem);
                            listItem = new List<string>();
                            idx++;
                            break;

                        case ReadOnlyRoutingListReplicaNameSeparator:
                            idx++;
                            break;

                        case 'N':
                            // N'replicaName'
                            var replicaNameEndIndex = readOnlyRoutingListDisplayString.IndexOf('\'', idx + 2);
                            var replicaName = readOnlyRoutingListDisplayString.Substring(idx + 2, replicaNameEndIndex - idx - 2);
                            listItem.Add(replicaName);

                            if (!currentlyInALoadBalancingGroup)
                            {
                                list.Add(listItem);
                                listItem = new List<string>();
                            }

                            idx = replicaNameEndIndex + 1;
                            break;
                        default:
                            idx++;
                            break;
                    }
                }
            }
            return list;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Convert the read-only routing list to its string representation
        /// </summary>
        /// <param name="readOnlyRoutingList">The read-only routing list</param>
        /// <param name="tsqlCompatible">Whether the string need to be T-SQL compatible</param>
        /// <returns>The string representation of the specified read-only routing list</returns>
        public static string ConvertReadOnlyRoutingListToString(IList<IList<string>> readOnlyRoutingList, bool tsqlCompatible = false)
        {
            if (readOnlyRoutingList.Any(row => row.Any(replicaName =>
            {
                return string.IsNullOrEmpty(replicaName);
            })))
            {
                throw new InvalidArgumentException(ExceptionTemplatesImpl.ReadOnlyRoutingListContainsEmptyReplicaName);
            }

            return readOnlyRoutingList == null ? string.Empty :
                string.Join(ReadOnlyRoutingListReplicaNameSeparator.ToString(), readOnlyRoutingList.Select(row =>
                {
                    var rowStr = string.Join(ReadOnlyRoutingListReplicaNameSeparator.ToString(), row.Select(replica => tsqlCompatible ? SqlSmoObject.MakeSqlString(replica) : replica).ToArray());
                    return row.Count > 1 ? string.Format("{0}{1}{2}", ReadOnlyRoutingLoadBalancingGroupStartCharacter, rowStr, ReadOnlyRoutingLoadBalancingGroupEndCharacter) : rowStr;
                }).ToArray());
        }

        #endregion
    }
}


// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;


namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// An Availability Group (AG) is the unit of high availability. 
    /// It represents a collection of related databases that form the business critical application 
    /// that needs high availability and disaster recovery capability.
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class AvailabilityGroup : NamedSmoObject, ICreatable, IAlterable, IDroppable, IDropIfExists, IScriptable
    {
        internal AvailabilityGroup(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
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
                return "AvailabilityGroup";
            }
        }

        #region Collections

        AvailabilityReplicaCollection m_AvailabilityReplicas = null;

        /// <summary>
        /// Gets the cluster type of the availability group, if the primary server is 130 or lower, return Wsfc instead
        /// </summary>
        public AvailabilityGroupClusterType ClusterTypeWithDefault
        {
            get
            {
                // The cluster type is introduced in SQL 140
                return IsSupportedProperty(ClusterTypePropertyName) ? this.ClusterType : AvailabilityGroupClusterType.Wsfc;
            }
        }


        /// <summary>
        /// The collection of replicas participating in the availability group.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.OneToAny, typeof(AvailabilityReplica))]
        public AvailabilityReplicaCollection AvailabilityReplicas
        {
            get
            {
                if (m_AvailabilityReplicas == null)
                {
                    m_AvailabilityReplicas = new AvailabilityReplicaCollection(this, GetComparerFromCollation("Latin1_General_CI_AS"));
                }

                return m_AvailabilityReplicas;
            }
        }

        AvailabilityDatabaseCollection m_AvailabilityDatabases = null;
        /// <summary>
        /// The collection of availability databases contained in the availability group.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(AvailabilityDatabase))]
        public AvailabilityDatabaseCollection AvailabilityDatabases
        {
            get
            {
                if (m_AvailabilityDatabases == null)
                {
                    m_AvailabilityDatabases = new AvailabilityDatabaseCollection(this);
                }

                return m_AvailabilityDatabases;
            }
        }

        DatabaseReplicaStateCollection m_DatabaseReplicaStates = null;
        /// <summary>
        /// A collection of <seealso cref="DatabaseReplicaState"/> objects represeting the states of physical
        /// database replicas participating in the availability groups.
        /// On an Availability Replica in a primary role, the collection returns information
        /// on all Database Replicas on all Availability Replicas.
        /// On an Availability Replica in a secondary role, the collection returns information
        /// on just the local Database Replicas.
        /// 
        /// The collection is keyed on the "AvailabilityReplicaServerName" and "AvailabilityDatabaseName"
        /// properties of the DatabaseReplciaState object.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(DatabaseReplicaState))]
        public DatabaseReplicaStateCollection DatabaseReplicaStates
        {
            get
            {
                if (m_DatabaseReplicaStates == null)
                {
                    m_DatabaseReplicaStates = new DatabaseReplicaStateCollection(this);
                }

                return m_DatabaseReplicaStates;
            }
        }

        AvailabilityGroupListenerCollection m_AvailabilityGroupListeners = null;

        /// <summary>
        /// The collection of replicas participating in the availability group.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(AvailabilityGroupListener))]
        public AvailabilityGroupListenerCollection AvailabilityGroupListeners
        {
            get
            {
                if (m_AvailabilityGroupListeners == null)
                {
                    m_AvailabilityGroupListeners = new AvailabilityGroupListenerCollection(this);
                }

                return m_AvailabilityGroupListeners;
            }
        }
        #endregion

        #region Public interface

        #region Failover Members

        /// <summary>
        /// Perform a manual failover of this availability group to the server
        /// specified by the Parent property. This server should be participating
        /// in the availability group as a secondary replica.
        /// The result of the action will be to designate this server as the primary of
        /// this availability group.
        /// This action has no possibility of data loss.
        /// </summary>
        public void Failover()
        {
            // Make sure the object has been retrieved from the backend if we are going to execute the script
            this.CheckObjectState(!this.ExecutionManager.Recording);

            string script =
                Scripts.ALTER +
                Globals.space +
                AvailabilityGroup.AvailabilityGroupScript +
                Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) +
                Globals.space +
                AvailabilityGroup.FailoverScript + 
                Globals.statementTerminator;

            this.DoCustomAction(script, ExceptionTemplates.ManualFailoverFailed(this.Parent.Name, this.Name));
        }

        /// <summary>
        /// Perform a force failover of this availability group to the server
        /// specified by the Parent property. This server should be participating
        /// in the availability group as a secondary replica.
        /// The result of the action will be to designate this server as the primary of
        /// this availability group.
        /// This action has the possibility of data loss if the databases on the replica
        /// are not synchronized with the primary.
        /// </summary>
        public void FailoverWithPotentialDataLoss()
        {
            // Make sure the object has been retrieved from the backend if we are going to execute the script
            this.CheckObjectState(!this.ExecutionManager.Recording);

            string script =
                Scripts.ALTER +
                Globals.space +
                AvailabilityGroup.AvailabilityGroupScript +
                Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) +
                Globals.space +
                AvailabilityGroup.ForceFailoverAllowDataLossScript +
                Globals.statementTerminator;

            this.DoCustomAction(script, ExceptionTemplates.ForceFailoverFailed(this.Parent.Name, this.Name));
        }

        /// <summary>
        /// Demote the current replica as secondary
        /// </summary>
        public void DemoteAsSecondary()
        {
            // only applicable to sql server 130 and later
            ThrowIfBelowVersion130();

            // ALTER AVAILABILITY GROUP [name] SET (ROLE = SECONDARY);
            // Make sure the object has been retrieved from the backend if we are going to execute the script
            this.CheckObjectState(!this.ExecutionManager.Recording);

            string script =
                Scripts.ALTER +
                Globals.space +
                AvailabilityGroup.AvailabilityGroupScript +
                Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) +
                Globals.space +
                Scripts.SET +
                Globals.space +
                Globals.LParen +
                AvailabilityGroup.RoleScript +
                Globals.space +
                Globals.EqualSign +
                Globals.space +
                AvailabilityGroup.SecondaryScript +
                Globals.RParen +
                Globals.statementTerminator;

            this.DoCustomAction(script, ExceptionTemplates.ForceFailoverFailed(this.Parent.Name, this.Name));
        }

        #endregion Failover Members

        #region ICreatable Members

        /// <summary>
        /// Create an availability group that has been specified on the client on the back end.
        /// 
        /// The availability group creation will include any availability replicas or 
        /// availability databases added to the object before creation.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        #endregion

        #region IAlterable Members

        /// <summary>
        /// Alter an availability group.
        /// 
        /// This is called when the alterable AG properties are changed.
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        #endregion

        #region IDroppable Members


        /// <summary>
        /// Drop an availability group.
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

        #region IScriptable Members
        
        /// <summary>
        /// Generate the script for creating this availability group.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate a script for this availability group using the
        /// specified scripting options.
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        #endregion 

        #region Enum Methods
        
        /// <summary>
        /// Returns a DataTable with information about the cluster configuration of 
        /// the availability group. For each replica, a row is present for each node
        /// that hosts the replica. A replica can be hosted by multiple nodes if the 
        /// replica is hosted on an FCI. The schema of this table is:
        /// ReplicaName (sysname) | NodeName (sysname)  | MemberType (tinyint) | MemberState (tinyint) | NumberOfQuorumVotes (int) 
        /// </summary>
        /// <returns>A DataTable with information about the cluster configuration of the availability group.</returns>
        public DataTable EnumReplicaClusterNodes()
        {
            try
            {
                Request req = new Request(this.Urn.Value + "/ReplicaClusterNode");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumReplicaClusterNodes(this.Name), this, e);
            }
        }

        #endregion 

        #endregion
 
        #region Overrides

        /// <summary>
        /// Composes the create script for the availability group object.
        /// </summary>
        /// <param name="query">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptCreate(System.Collections.Specialized.StringCollection query, ScriptingPreferences sp)
        {
            /* ========================================================================================
             * ADVANCED Availability Groups
             * ----------------------------------------------------------------------------------------
             * Advanced AGs have been available since SQL 2012 when AlwaysOn was originally released, 
             * although at that time they were simply referred as "availabiliy groups.  This AG type
             * is only available on ENT, DEV or EVAL editions (skus).  The ag_option "ADVANCED | BASIC"
             * defaults to ADVANCED if not specied.  Internally, this class does not emit the ADVANCED 
             * ag_option when creating a non-Basic AG; this is done for backward compatibility reasons.
             * ========================================================================================
             * 
             * CREATE AVAILABILITY GROUP 'group_name'
             * [WITH (ADVANCED[, <ag_option>, ...n])]
             * [FOR DATABASE 'database_name'[, ...n]]
             * REPLICA ON <replica_spec>[, ...n]
             * [LISTENER 'dns_name' (<listener_option>)]
             * [;]
             * 
             * <ag_option> ::= 
             *          AUTOMATED_BACKUP_PREFERENCE = { PRIMARY | SECONDARY_ONLY| SECONDARY | NONE }
             *          | DTC_SUPPORT = { PER_DB | NONE }
             *          | DB_FAILOVER = { ON | OFF }
             *          | FAILURE_CONDITION_LEVEL = { 1 | 2 | 3 | 4 | 5 }
             *          | HEALTH_CHECK_TIMEOUT = milliseconds
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
             *          | READ_ONLY_ROUTING_LIST = { ( '<server_instance>' [ ,...n ] ) | NONE  }
             *
             * <secondary_role_option> ::= 
             *          ALLOW_CONNECTIONS = { NO | READ_ONLY | ALL }
             *          | READ_ONLY_ROUTING_URL = 'TCP://system-address:port'
             * 
             * <listener_option> ::= 
             *          {
             *              WITH DHCP [ ON ( <network_subnet_option> ) ] | WITH IP ( { <ip_address_option> )
             *          }
             *          PORT = listener_port
             * 
             * <network_subnet_option> ::= 'four_part_ipv4_address', 'four_part_ipv4_mask'  
             * <ip_address_option> ::= { 'four_part_ipv4_address', 'four_part_ipv4_mask' | 'ipv6_address' }
             * 
             * ========================================================================================
             * BASIC Availability Groups
             * ----------------------------------------------------------------------------------------
             * Basic AGs are being released in SQL 2016 and are in general a replacemnt for Database 
             * Mirroring.  In addition to ENT, DEV or EVAL editions, Basic AGs are available on STD 
             * editions.  
             * 
             * Basic AG have a more limited set of ag_options and replica_options for both CREATE and 
             * ALTER AVAILABILITY GROUP.  Finally, Basic AGs can only be created and dropped; the DDL 
             * does not allow ALTERing a Basic AG into an Advanced AG or vice-versa.  
             * ========================================================================================
             * 
             * CREATE AVAILABILITY GROUP 'group_name' 
             *  WITH (<with_option_spec> [ ,...n ] )  
             *  FOR [ DATABASE database_name [ ,...n ] ]  
             *  REPLICA ON <add_replica_spec> [ ,...n ]  
             *  AVAILABILITY GROUP ON <add_availability_group_spec> [ ,...2 ]  
             *  [ LISTENER 'dns_name' ( <listener_option> ) ]  
             * ; ]
             *
             * <with_option_spec>::=   
             *   AUTOMATED_BACKUP_PREFERENCE = { PRIMARY | SECONDARY_ONLY| SECONDARY | NONE }  
             * | FAILURE_CONDITION_LEVEL  = { 1 | 2 | 3 | 4 | 5 }   
             * | HEALTH_CHECK_TIMEOUT = milliseconds  
             * | DB_FAILOVER  = { ON | OFF }   
             * | DTC_SUPPORT  = { PER_DB | NONE }  
             * | [ BASIC | DISTRIBUTED ]
             * | REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT = { integer }
             * | CLUSTER_TYPE = { WSFC | EXTERNAL | NONE } 
             *
             * <add_replica_spec>::=
             * <server_instance> WITH
             *   (
             *      ENDPOINT_URL = 'TCP://system-address:port',  
             *      AVAILABILITY_MODE = { SYNCHRONOUS_COMMIT | ASYNCHRONOUS_COMMIT | CONFIGURATION_ONLY },  
             *      FAILOVER_MODE = { AUTOMATIC | MANUAL | EXTERNAL }  
             *      [ , <add_replica_option> [ ,...n ] ]  
             *   )
             *
             * <add_replica_option>::=  
             *      SEEDING_MODE = { AUTOMATIC | MANUAL }
             *    | BACKUP_PRIORITY = n
             *    | SECONDARY_ROLE ( {
             *           [ ALLOW_CONNECTIONS = { NO | READ_ONLY | ALL } ]
             *       [,] [ READ_ONLY_ROUTING_URL = 'TCP://system-address:port' ]
             *    } ) 
             *    | PRIMARY_ROLE ( {
             *           [ ALLOW_CONNECTIONS = { READ_WRITE | ALL } ]   
             *       [,] [ READ_ONLY_ROUTING_LIST = { ( '<server_instance>' [ ,...n ] ) | NONE } ]
             *       [,] [ READ_WRITE_ROUTING_URL = { ( '<server_instance>' ) ]
             *    } ) 
             *    | SESSION_TIMEOUT = integer
             *
             * <add_availability_group_spec>::=  
             * <ag_name> WITH  
             *   ( 
             *      LISTENER_URL = 'TCP://system-address:port',
             *      AVAILABILITY_MODE = { SYNCHRONOUS_COMMIT | ASYNCHRONOUS_COMMIT },
             *      FAILOVER_MODE = MANUAL,
             *      SEEDING_MODE = { AUTOMATIC | MANUAL }
             *   )
             *
             * <listener_option> ::=
             *  {
             *     WITH DHCP [ ON ( <network_subnet_option> ) ]
             *   | WITH IP ( { ( <ip_address_option> ) } [ , ...n ] ) [ , PORT = listener_port ]
             *  }
             *
             * <network_subnet_option> ::=
             *    'ip4_address', 'four_part_ipv4_mask'
             *
             * <ip_address_option> ::=  
             *    {
             *       'ip4_address', 'pv4_mask'
             *     | 'ipv6_address'
             *    }
             */

            // sanity checks
            tc.Assert(null != query, "String collection should not be null");

            // Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            bool isDistributed = false;

            if (IsSupportedProperty(DistributedPropertyName))
            {
                Property propIsDistributed = GetPropertyOptional(DistributedPropertyName);
                isDistributed = !propIsDistributed.IsNull && propIsDistributed.Value.Equals(true);
            }

            // Cannot create an availability group without at least one replica
            if (null == this.AvailabilityReplicas || this.AvailabilityReplicas.Count < 1)
            {
                throw new SmoException(ExceptionTemplates.ObjectWithNoChildren(AvailabilityGroup.UrnSuffix, AvailabilityReplica.UrnSuffix));
            }

            // Cannot create an availability group without the parent server instance be a replica
            // Exception here are distibuted availability groups, where replica name is just a friendly name of an existing AG
            // and it doesn't have to match the server name.
            //
            if (!isDistributed && null == this.AvailabilityReplicas[this.Parent.ConnectionContext.TrueName])
            {
                throw new SmoException(ExceptionTemplates.CannotCreateAvailabilityGroupWithoutCurrentIntance(this.Parent.Name, this.Name));
            }

            // Can create an availability group with only 1 listeners
            if (null != this.AvailabilityGroupListeners && this.AvailabilityGroupListeners.Count > 1)
            {
                throw new SmoException(ExceptionTemplates.ObjectWithMoreChildren(AvailabilityGroup.UrnSuffix, AvailabilityGroupListener.UrnSuffix));
            }

            if (null != this.AvailabilityGroupListeners && this.AvailabilityGroupListeners.Count == 1)
            {
                this.AvailabilityGroupListeners[0].ValidateIPAddresses();
            }

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);
            
            if(sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability group doesn't exist before creating it
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_GROUP, "NOT", FormatFullNameForScripting(sp, false));
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            //Script the availability group name
            script.Append(Scripts.CREATE + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Name) + Globals.newline);

            //Script Availability group options
            //Since there are no required options, this section is conditional on any option explicitly set before creation
            string groupOptions = this.ScriptCreateGroupOptions(sp.TargetServerVersionInternal);
            if (!string.IsNullOrEmpty(groupOptions))
            {
                script.Append(Globals.With + Globals.space + Globals.LParen + groupOptions + Globals.RParen + Globals.newline);
            }

            if (isDistributed)
            {
                // For distributed availability groups, we script two availability replicas only.
                //
                // This is the sample syntax for this case:
                //
                //  CREATE AVAILABILITY GROUP[dag]
                //    WITH(DISTRIBUTED)
                //    AVAILABILITY GROUP ON
                //      'AG1' WITH
                //      (
                //         LISTENER_URL = 'TCP://localhost:5022',
                //         AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT,
                //         FAILOVER_MODE = MANUAL,
                //         SEEDING_MODE = AUTOMATIC
                //      ), 
                //      'AG2' WITH
                //      (
                  //       LISTENER_URL = 'TCP://server2:5022',
                //         AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT,
                //         FAILOVER_MODE = MANUAL,
                //         SEEDING_MODE = AUTOMATIC
                //      )

                bool firstReplica = true;
                script.Append(AvailabilityGroup.AvailabilityGroupOn + Globals.newline+ Globals.tab);

                foreach (AvailabilityReplica ar in this.AvailabilityReplicas)
                {
                    if (firstReplica)
                    {
                        firstReplica = false;
                    }
                    else
                    {
                        script.Append(Globals.comma + Globals.newline + Globals.tab);
                    }
                 
                    script.Append(SqlSmoObject.MakeSqlString(ar.Name) + Globals.space + Globals.With + Globals.space);

                    script.Append(ar.ScriptDistributedAvailabilityGroupReplicaOptions());
                }
            }
            else
            {
                script.Append(Globals.For + Globals.space);

                //Append availability databases
                bool firstDatabase = true;
                foreach (AvailabilityDatabase aDb in this.AvailabilityDatabases)
                {
                    if (firstDatabase)
                    {
                        script.Append(AvailabilityGroup.DatabaseScript + Globals.space);
                        firstDatabase = false;
                    }
                    else
                    {
                        script.Append(Globals.comma + Globals.space);
                    }
                    script.Append(SqlSmoObject.MakeSqlBraket(aDb.Name));
                }
                script.Append(Globals.newline);

                //Now add the availability replicas
                bool firstReplica = true;
                script.Append(AvailabilityGroup.ReplicaOn + Globals.space);
                foreach (AvailabilityReplica ar in this.AvailabilityReplicas)
                {
                    if (firstReplica)
                    {
                        firstReplica = false;
                    }
                    else
                    {
                        script.Append(Globals.comma + Globals.newline + Globals.tab);
                    }
                    script.Append(SqlSmoObject.MakeSqlString(ar.Name) + Globals.space + Globals.With + Globals.space + Globals.LParen);

                    //Add script for replica spec 
                    script.Append(ar.ScriptReplicaOptions(sp));
                    script.Append(Globals.RParen);
                }
            }

            // script out the listener
            if (null != this.AvailabilityGroupListeners && this.AvailabilityGroupListeners.Count == 1)
            {
                script.Append(Globals.newline);
                script.Append(AvailabilityGroup.ListenerScript + Globals.space);
                script.Append(SqlSmoObject.MakeSqlString(this.AvailabilityGroupListeners[0].Name) + Globals.space + Globals.LParen);
                script.Append(this.AvailabilityGroupListeners[0].ScriptListenerOptions());
                script.Append(Globals.RParen);
            }
            //Add statement terminator
            script.Append(Globals.statementTerminator);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            string createScript = script.ToString();
            query.Add(createScript);
            tc.TraceInformation("Generated create script: " + createScript);

            return;
        }

        /// <summary>
        /// Composes the Alter script for an AvalabilityGroup object
        /// </summary>
        /// <param name="alterQuery">A collection of T-SQL scripts performing the Alter action.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            /* ========================================================================================
             * ADVANCED Availability Groups
             * ========================================================================================
             * 
             * ALTER AVAILABILITY GROUP group_name SET ( <ag_option> )
             * 
             * <ag_option> ::= 
             *          AUTOMATED_BACKUP_PREFERENCE = { PRIMARY | SECONDARY_ONLY| SECONDARY | NONE }
             *          | DB_FAILOVER = { ON | OFF }
             *          | FAILURE_CONDITION_LEVEL = { 1 | 2 | 3 | 4 | 5 }
             *          | HEALTH_CHECK_TIMEOUT = milliseconds
             *          | REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT = number_of_replicas
             *
             * ========================================================================================
             * BASIC Availability Groups
             * ========================================================================================
             * 
             * ALTER AVAILABILITY GROUP group_name SET ( <ag_option> )
             * 
             * <ag_option> ::= 
             *          DB_FAILOVER = { ON | OFF }
             *          | FAILURE_CONDITION_LEVEL = { 1 | 2 | 3 | 4 | 5 }
             *          | HEALTH_CHECK_TIMEOUT = milliseconds
             */

            //sanity checks
            tc.Assert(null != alterQuery, "String collection should not be null");

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            IEnumerable<string> propertyNames;
            if (IsSupportedProperty("BasicAvailabilityGroup") && BasicAvailabilityGroup)
            {
                propertyNames = BasicAlterableGroupPropertyNames;
            }
            else
            {
                propertyNames = AlterableGroupPropertyNames.Where(kvp => kvp.Key <= sp.TargetServerVersionInternal).SelectMany(kvp => kvp.Value);
            }

            foreach (string propertyName in propertyNames)
            {
                string optionScript = this.ScriptAlterOneOption(propertyName, sp);

                if (!string.IsNullOrEmpty(optionScript))
                {
                    alterQuery.Add(optionScript);
                }                
            }

            return;
        }

        /// <summary>
        /// Create the script to drop the availability group
        /// </summary>
        /// <param name="dropQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences. This is not used here since they have no 
        /// bearing on the script.</param>
        internal override void ScriptDrop(System.Collections.Specialized.StringCollection dropQuery, ScriptingPreferences sp)
        {
            //DROP AVAILABILITY GROUP 'group_name'

            //sanity checks
            tc.Assert(null != dropQuery, "String collection for the drop query should not be null");

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            StringBuilder script = new StringBuilder();
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability group exists before dropping it
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_GROUP, "", FormatFullNameForScripting(sp, false));
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            //The script is simple, we only need the name
            script.Append(Scripts.DROP + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space + SqlSmoObject.MakeSqlBraket(this.Name) + Globals.statementTerminator);

            //Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            string dropScript = script.ToString(); 
            dropQuery.Add(dropScript);
            tc.TraceInformation("Generated drop script: " + dropScript);

            return;
        }

        /// <summary>
        /// Propagate information to the ARs and ADs, listener collections
        /// </summary>
        /// <param name="action">the action to propagate</param>
        /// <returns></returns>
        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            if (PropagateAction.Create == action)
            {
                PropagateInfo[] propagateInfoArray = { new PropagateInfo(this.AvailabilityReplicas, false), new PropagateInfo(this.AvailabilityDatabases, false), new PropagateInfo(this.AvailabilityGroupListeners, false) };

                return propagateInfoArray;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// After create, we may need to update keys for listener ip address objects that use DHCP.
        /// </summary>
        protected override void PostCreate()
        {
            base.PostCreate();
            if (this.AvailabilityGroupListeners != null)
            {
                foreach (AvailabilityGroupListener listener in this.AvailabilityGroupListeners)
                {
                    listener.FetchIpAddressKeysPostCreate();
                }
            }
        }

        #endregion

        #region Constants

        //Scripting constants, the suffix 'Script' is added for disambiguation from class names
        internal static readonly string ForceFailoverAllowDataLossScript = "FORCE_FAILOVER_ALLOW_DATA_LOSS";
        internal static readonly string FailoverScript = "FAILOVER";
        internal static readonly string AvailabilityGroupScript = "AVAILABILITY GROUP";
        internal static readonly string DatabaseScript = "DATABASE";
        internal static readonly string ReplicaOn = "REPLICA ON";
        internal static readonly string AutomatedBackupPreferenceScript = "AUTOMATED_BACKUP_PREFERENCE";
        internal static readonly string BasicAvailabilityGroupScript = "BASIC";
        internal static readonly string DatabaseHealthTriggerScript = "DB_FAILOVER";
        internal static readonly string DtcSupportEnabledScript = "DTC_SUPPORT";
        internal static readonly string DtcSupportEnabledOnScript = "PER_DB";
        internal static readonly string DtcSupportEnabledOffScript = "NONE";
        internal static readonly string FailureConditionLevelScript = "FAILURE_CONDITION_LEVEL";
        internal static readonly string HealthCheckTimeoutScript = "HEALTH_CHECK_TIMEOUT";
        internal static readonly string ListenerScript = "LISTENER";
        internal static readonly string PortScript = "PORT";
        internal static readonly string ClusterTypeScript = "CLUSTER_TYPE";
        internal static readonly string RequiredSynchronizedSecondariesToCommitScript = "REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT";
        internal static readonly string RoleScript = "ROLE";
        internal static readonly string DistributedScript = "DISTRIBUTED";
        internal static readonly string AvailabilityGroupOn = "AVAILABILITY GROUP ON";

        //Property names
        internal const string AutomatedBackupPreferencePropertyName = "AutomatedBackupPreference";
        internal const string BasicAvailabilityGroupPropertyName = "BasicAvailabilityGroup";
        internal const string DatabaseHealthTriggerPropertyName = "DatabaseHealthTrigger";
        internal const string DtcSupportEnabledPropertyName = "DtcSupportEnabled";
        internal const string FailureConditionLevelPropertyName = "FailureConditionLevel";
        internal const string HealthCheckTimeoutPropertyName = "HealthCheckTimeout";
        internal const string ClusterTypePropertyName = "ClusterType";
        internal const string RequiredSynchronizedSecondariesToCommitPropertyName = "RequiredSynchronizedSecondariesToCommit";
        internal const string DistributedPropertyName = nameof(IsDistributedAvailabilityGroup);

        //Automated backup preference scripts
        internal static readonly string PrimaryScript = "PRIMARY";
        internal static readonly string SecondaryOnlyScript = "SECONDARY_ONLY";
        internal static readonly string SecondaryScript = "SECONDARY";
        internal static readonly string NoneScript = "NONE";

        //Cluster type scripts
        internal static readonly string WsfcScript = "WSFC";
        internal static readonly string ExternalScript = "EXTERNAL";

        internal static readonly Dictionary<SqlServerVersionInternal, string[]> CreatableGroupPropertyNames = new Dictionary<SqlServerVersionInternal, string[]>()
        {
            { SqlServerVersionInternal.Version120, new string[] {
                AutomatedBackupPreferencePropertyName,
                FailureConditionLevelPropertyName,
                HealthCheckTimeoutPropertyName,
            } },

            { SqlServerVersionInternal.Version130, new string[] {
                BasicAvailabilityGroupPropertyName,
                DatabaseHealthTriggerPropertyName,
                DtcSupportEnabledPropertyName,
                DistributedPropertyName,
            } },

            { SqlServerVersionInternal.Version140, new string[] {
                ClusterTypePropertyName,
                RequiredSynchronizedSecondariesToCommitPropertyName,
            } },
        };

        internal static readonly string[] BasicAlterableGroupPropertyNames =
        {
            DatabaseHealthTriggerPropertyName,
            FailureConditionLevelPropertyName,
            HealthCheckTimeoutPropertyName,
            DtcSupportEnabledPropertyName
        };

        internal static readonly Dictionary<SqlServerVersionInternal, string[]> AlterableGroupPropertyNames = new Dictionary<SqlServerVersionInternal, string[]>()
        {
            { SqlServerVersionInternal.Version120, new string[] {
                AutomatedBackupPreferencePropertyName,
                FailureConditionLevelPropertyName,
                HealthCheckTimeoutPropertyName
            } },

            { SqlServerVersionInternal.Version130, new string[] {
                DatabaseHealthTriggerPropertyName,
                DtcSupportEnabledPropertyName
            } },

            { SqlServerVersionInternal.Version140, new string[] {
                RequiredSynchronizedSecondariesToCommitPropertyName,
            } },
        };

        internal static string GetAvailabilityGroupClusterType(AvailabilityGroupClusterType availabilityGroupClusterType)
        {
            switch (availabilityGroupClusterType)
            {
                case AvailabilityGroupClusterType.Wsfc:
                    return WsfcScript;

                case AvailabilityGroupClusterType.None:
                    return NoneScript;

                case AvailabilityGroupClusterType.External:
                    return ExternalScript;
                default:
                    throw new ArgumentException(ExceptionTemplates.UnknownEnumerationWithValue("AvailabilityGroupClusterType", availabilityGroupClusterType));
            }
        }

        #endregion

        #region Private members

        private static readonly TraceContext tc = TraceContext.GetTraceContext(SmoApplication.ModuleName, "AvailabilityGroup");

        private bool IsDirty(string property)
        {
            return this.Properties.IsDirty(this.Properties.LookupID(property, PropertyAccessPurpose.Read));
        }

        private AvailabilityGroupAutomatedBackupPreference GetEffectiveAutomatedBackupPreference(SqlServerVersionInternal targetVersion)
        {
            if(targetVersion >= SqlServerVersionInternal.Version130) 
            {
                Property prop = GetPropertyOptional(BasicAvailabilityGroupPropertyName);

                if (!prop.IsNull && (prop.Value.Equals(true)))
                {
                    // "Primary" is the only allowed AutomatedBackupPreference for Basic AGs.
                    //
                    return AvailabilityGroupAutomatedBackupPreference.Primary;
                }
            }

            return AutomatedBackupPreference;
        }

        private string GetAutomatedBackupPreferenceScript(AvailabilityGroupAutomatedBackupPreference preference)
        {
            switch (preference)
            {
                case AvailabilityGroupAutomatedBackupPreference.Primary:
                    return PrimaryScript;
                    
                case AvailabilityGroupAutomatedBackupPreference.SecondaryOnly:
                    return SecondaryOnlyScript;

                case AvailabilityGroupAutomatedBackupPreference.Secondary:
                    return SecondaryScript;

                case AvailabilityGroupAutomatedBackupPreference.None:
                    return NoneScript;

                default:
                    throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("AvailabilityGroupAutomatedBackupPreference"));
            }
        }


        private string ScriptGroupOption(bool scriptAll, string propertyName, SqlServerVersionInternal targetServerVersion)
        {
            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            Property prop = GetPropertyOptional(propertyName);

            if (!prop.IsNull && (scriptAll || IsDirty(propertyName)))
            {
                switch (propertyName)
                {
                    case AutomatedBackupPreferencePropertyName:
                        script.Append(AutomatedBackupPreferenceScript + Globals.space + Globals.EqualSign + Globals.space);
                        script.Append(this.GetAutomatedBackupPreferenceScript(this.GetEffectiveAutomatedBackupPreference(targetServerVersion)));
                        break;

                    case BasicAvailabilityGroupPropertyName:
                        if (this.BasicAvailabilityGroup)
                        {
                            script.Append(BasicAvailabilityGroupScript);
                        }
                        break;

                    case DatabaseHealthTriggerPropertyName:
                        script.Append(DatabaseHealthTriggerScript + Globals.space + Globals.EqualSign + Globals.space);
                        script.Append(this.DatabaseHealthTrigger ? "ON" : "OFF");
                        break;

                    case DtcSupportEnabledPropertyName:
                        script.Append(DtcSupportEnabledScript + Globals.space + Globals.EqualSign + Globals.space);
                        script.Append(this.DtcSupportEnabled ? DtcSupportEnabledOnScript : DtcSupportEnabledOffScript);
                        break;

                    case FailureConditionLevelPropertyName:
                        script.Append(FailureConditionLevelScript + Globals.space + Globals.EqualSign + Globals.space);
                        script.Append((int)this.FailureConditionLevel);
                        break;

                    case HealthCheckTimeoutPropertyName:
                        script.Append(HealthCheckTimeoutScript + Globals.space + Globals.EqualSign + Globals.space);
                        script.Append(this.HealthCheckTimeout);
                        break;

                    case ClusterTypePropertyName:
                        // WSFC is the default when CLUSTER_TYPE is not specified, so don't emit it if ClusterType is Wsfc.
                        if (this.ClusterType != AvailabilityGroupClusterType.Wsfc)
                        {
                            script.Append(ClusterTypeScript + Globals.space + Globals.EqualSign + Globals.space);
                            script.Append(GetAvailabilityGroupClusterType(this.ClusterType));
                        }
                        break;

                    case RequiredSynchronizedSecondariesToCommitPropertyName:
                        script.Append(RequiredSynchronizedSecondariesToCommitScript + Globals.space + Globals.EqualSign + Globals.space);
                        script.Append(this.RequiredSynchronizedSecondariesToCommit);
                        break;
                    case DistributedPropertyName: 
                         if (this.IsDistributedAvailabilityGroup)
                         {
                             script.Append(DistributedScript);
                         }
                         break;
                    default:
                        break;
                }
            }

            return script.ToString();
        }

        private string ScriptCreateGroupOptions(SqlServerVersionInternal targetVersion)
        {
            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            bool firstOption = true;

            foreach (var propertyName in CreatableGroupPropertyNames.Where(kvp => kvp.Key <= targetVersion).SelectMany(kvp => kvp.Value))
            {
                string groupOption = this.ScriptGroupOption(true, propertyName, targetVersion);

                if (!string.IsNullOrEmpty(groupOption))
                {
                    if (firstOption)
                    {
                        firstOption = false;
                    }
                    else
                    {
                        script.Append(Globals.comma + Globals.newline);
                    }

                    script.Append(groupOption);
                }
            }

            return script.ToString();
        }

        private string ScriptAlterOneOption(string propertyName, ScriptingPreferences sp)
        {
            string groupOptionScript = this.ScriptGroupOption(false, propertyName, sp.TargetServerVersionInternal);

            if (string.IsNullOrEmpty(groupOptionScript))
            {
                return null;
            }

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability group exists before altering it
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_GROUP, "", FormatFullNameForScripting(sp, false));
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Name) + Globals.space + Scripts.SET + Globals.LParen + Globals.newline);
            script.Append(groupOptionScript); 
            script.Append(Globals.newline + Globals.RParen);

            //Add statement terminator
            script.Append(Globals.statementTerminator);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            //Close existence check, if necessary
            if (sp.IncludeScripts.ExistenceCheck)
            {
                script.Append(sp.NewLine);
                script.Append(Scripts.END);
                script.Append(sp.NewLine);
            }

            return script.ToString();
        }

        #endregion
    }
}


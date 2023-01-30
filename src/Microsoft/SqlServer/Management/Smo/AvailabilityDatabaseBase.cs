// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// An Availability Database represents a database that is part of an Availability Group.
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class AvailabilityDatabase : NamedSmoObject, ICreatable, IDroppable, IDropIfExists, IScriptable
    {

        internal AvailabilityDatabase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
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
                return "AvailabilityDatabase";
            }
        }

        #region Public interface

        #region ICreatable Members

        /// <summary>
        /// Create an availability database.
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        #endregion

        #region IDroppable Members

        /// <summary>
        /// Remove an availability database from the availability group.
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
        /// Generate the script for creating this availability database.
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Generate a script for this availability database using the
        /// specified scripting options.
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        #endregion 

        #region HADR Members

        /// <summary>
        /// Join the local copy of the database on the instance to the availability group.
        /// The local copy of the database is identified by the Name property.
        /// This action assumes the instance is already an availability replica in the group
        /// and the local copy is a locally restored copy of the database on the primary replica.
        /// </summary>
        public void JoinAvailablityGroup()
        {
            this.CheckObjectState(!this.ExecutionManager.Recording); //make sure the object has been retrieved from the backend if we are going to execute the script
            string availabilityGroupName = this.Parent.Name;

	    string waitScript = @"
-- Wait for the replica to start communicating
begin try
declare @conn bit
declare @count int
declare @replica_id uniqueidentifier 
declare @group_id uniqueidentifier
set @conn = 0
set @count = 30 -- wait for 5 minutes 

if (serverproperty('IsHadrEnabled') = 1)
	and (isnull((select member_state from master.sys.dm_hadr_cluster_members where upper(member_name COLLATE Latin1_General_CI_AS) = upper(cast(serverproperty('ComputerNamePhysicalNetBIOS') as nvarchar(256)) COLLATE Latin1_General_CI_AS)), 0) <> 0)
	and (isnull((select state from master.sys.database_mirroring_endpoints), 1) = 0)
begin
    select @group_id = ags.group_id from master.sys.availability_groups as ags where name = N'{0}'
	select @replica_id = replicas.replica_id from master.sys.availability_replicas as replicas where upper(replicas.replica_server_name COLLATE Latin1_General_CI_AS) = upper(@@SERVERNAME COLLATE Latin1_General_CI_AS) and group_id = @group_id
	while @conn <> 1 and @count > 0
	begin
		set @conn = isnull((select connected_state from master.sys.dm_hadr_availability_replica_states as states where states.replica_id = @replica_id), 1)
		if @conn = 1
		begin
			-- exit loop when the replica is connected, or if the query cannot find the replica status
			break
		end
		waitfor delay '00:00:10'
		set @count = @count - 1
	end
end
end try
begin catch
	-- If the wait loop fails, do not stop execution of the alter database statement
end catch
";
	    waitScript = String.Format(CultureInfo.InvariantCulture, waitScript, SqlSmoObject.EscapeString(availabilityGroupName, '\''));

            // ALTER DATABASE 'database_name' SET HADR {AVAILABILITY GROUP = 'group_name' | OFF} 
            string script = waitScript + Scripts.ALTER + Globals.space + AvailabilityGroup.DatabaseScript + Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) + Globals.space + Scripts.SET + Globals.space + Scripts.HADR + Globals.space +
                AvailabilityGroup.AvailabilityGroupScript + Globals.space + Globals.EqualSign + Globals.space +
                SqlSmoObject.MakeSqlBraket(availabilityGroupName) + Globals.statementTerminator;

            string exceptionMessage = ExceptionTemplates.DatabaseJoinAvailabilityGroupFailed(this.Parent.Parent.Name, availabilityGroupName, this.Name);
          
            this.DoCustomAction(script, exceptionMessage);
        }

        /// <summary>
        /// Remove the local copy of the database on the availability replica from the availability group.
        /// </summary>
        public void LeaveAvailabilityGroup()
        {
            this.CheckObjectState(!this.ExecutionManager.Recording); //make sure the object has been retrieved from the backend if we are going to execute the script

            //ALTER DATABASE 'database_name' SET HADR {AVAILABILITY GROUP = 'group_name' | OFF} 
            string script = Scripts.ALTER + Globals.space + AvailabilityGroup.DatabaseScript + Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) + Globals.space + Scripts.SET + Globals.space + Scripts.HADR +
                Globals.space + Globals.Off + Globals.statementTerminator;

            string exceptionMessage = ExceptionTemplates.DatabaseLeaveAvailabilityGroupFailed(this.Parent.Parent.Name, this.Parent.Name, this.Name);

            this.DoCustomAction(script, exceptionMessage);
        }

        /// <summary>
        /// Suspend data movement on this availability database.
        /// </summary>
        public void SuspendDataMovement()
        {
            this.CheckObjectState(!this.ExecutionManager.Recording); //make sure the object has been retrieved from the backend if we are going to execute the script

            //ALTER DATABASE 'database_name' SET HADR { SUSPEND | RESUME } 
            string script = Scripts.ALTER + Globals.space + AvailabilityGroup.DatabaseScript + Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) + Globals.space + Scripts.SET + Globals.space + Scripts.HADR +
                Globals.space + Scripts.SUSPEND + Globals.statementTerminator;

            string exceptionMessage = ExceptionTemplates.SuspendDataMovementFailed(this.Parent.Parent.Name, this.Parent.Name, this.Name);

            this.DoCustomAction(script, exceptionMessage);
        }

        /// <summary>
        /// Resume data movement on this availability database.
        /// </summary>
        public void ResumeDataMovement()
        {
            this.CheckObjectState(!this.ExecutionManager.Recording); //make sure the object has been retrieved from the backend if we are going to execute the script

            //ALTER DATABASE 'database_name' SET HADR { SUSPEND | RESUME } 
            string script = Scripts.ALTER + Globals.space + AvailabilityGroup.DatabaseScript + Globals.space +
                SqlSmoObject.MakeSqlBraket(this.Name) + Globals.space + Scripts.SET + Globals.space + Scripts.HADR +
                Globals.space + Scripts.RESUME + Globals.statementTerminator;

            string exceptionMessage = ExceptionTemplates.ResumeDataMovementFailed(this.Parent.Parent.Name, this.Parent.Name, this.Name);

            this.DoCustomAction(script, exceptionMessage);
        }

        #endregion

        #endregion

        #region override methods
        /// <summary>
        /// Composes the create script for the availability database object.
        /// </summary>
        /// <param name="query">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptCreate(System.Collections.Specialized.StringCollection query, ScriptingPreferences sp)
        {
            //sanity checks
            tc.Assert(null != query, "String collection should not be null");

            /*
             * ALTER AVAILABILITY GROUP 'group_name'
             * ADD DATABASE <database_name> [,...n]
             */

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);
            
            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability database doesn't exists before creating it
                string myName = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_DATABASE, "NOT", myName, parentName);
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            //Script the availability group name
            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append("ADD" + Globals.space + "DATABASE" + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Name));

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
        /// Create the script to drop the availability database
        /// </summary>
        /// <param name="dropQuery">A string collection representing the script. Since no
        /// batching is required, the collection will have only one string in the end.</param>
        /// <param name="sp">Scripting preferences.</param>
        internal override void ScriptDrop(System.Collections.Specialized.StringCollection dropQuery, ScriptingPreferences sp)
        {
            //sanity checks
            tc.Assert(null != dropQuery, "String collection should not be null");

            //ALTER AVAILABILITY GROUP 'group_name'
            //REMOVE DATABASE database_name <,..n> 

            //Ensure target server version is >= 11, and database engine is not azure
            ThrowIfBelowVersion110(sp.TargetServerVersionInternal);
            ThrowIfCloud(sp.TargetDatabaseEngineType, ExceptionTemplates.UnsupportedEngineTypeException);

            StringBuilder script = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            this.ScriptIncludeHeaders(script, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                //Check that the availability database exists before dropping it
                string myName = this.FormatFullNameForScripting(sp, false);
                string parentName = this.Parent.FormatFullNameForScripting(sp, false);
                script.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_AVAILABILITY_DATABASE, "", myName, parentName); 
                script.Append(sp.NewLine);
                script.Append(Scripts.BEGIN);
                script.Append(sp.NewLine);
            }

            //First, script the availability group name
            script.Append(Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Parent.Name) + Globals.newline);
            script.Append("REMOVE" + Globals.space + "DATABASE" + Globals.space);
            script.Append(SqlSmoObject.MakeSqlBraket(this.Name));

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
        #endregion

        #region private members
        
        private static readonly TraceContext tc = TraceContext.GetTraceContext(SmoApplication.ModuleName, "AvailabilityDatabase");

        #endregion
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal class Scripts
    {

        // permissions stuff
        public static string Priv_References = " REFERENCES ";
        public static string Priv_Select = " SELECT ";
        public static string Priv_Insert = " INSERT ";
        public static string Priv_Delete = " DELETE ";
        public static string Priv_Update = " UPDATE ";
        public static string Priv_Execute = " EXECUTE ";
        public static string Priv_CreateDatabase = " CREATE DATABASE ";
        public static string Priv_CreateDefault = " CREATE DEFAULT ";
        public static string Priv_CreateProcedure = " CREATE PROCEDURE ";
        public static string Priv_CreateRule = " CREATE RULE ";
        public static string Priv_CreateTable = " CREATE TABLE ";
        public static string Priv_CreateView = " CREATE VIEW ";
        public static string Priv_CreateFunction = " CREATE FUNCTION ";
        public static string Priv_DumpTable = " DUMP TABLE ";
        public static string Priv_CreateType = " CREATE TYPE ";
        public static string Priv_Control = " CONTROL ";
        public static string Priv_ViewDefinition = " VIEW DEFINITION ";
        public static string Priv_Alter = " ALTER ";
        public static string Priv_Drop = " DROP ";

        public static readonly string Priv_AllPrivs = " ALL ";

        public static readonly string GRANT = "GRANT ";
        public static readonly string REVOKEGRANT = " GRANT OPTION FOR ";
        public static readonly string GRANTGRANT = " WITH GRANT OPTION ";
        public static readonly string REVOKE = "REVOKE ";
        public static readonly string DENY = "DENY ";
        public static readonly string ENABLE = "ENABLE ";
        public static readonly string DISABLE = "DISABLE ";


        // db creation
        public static readonly string USEDB = "USE [{0}]";
        public static readonly string USEMASTER = "USE [master]";
        public static readonly string RECOVERY_BULK = "BULK_LOGGED";
        public static readonly string RECOVERY_FULL = "FULL";
        public static readonly string RECOVERY_SIMPLE = "SIMPLE";

        public static readonly string SP_ADDDBROLEMEMBER = "EXEC sp_addrolemember N'{0}', N'{1}'";
        public static readonly string SP_DROPDBROLEMEMBER = "EXEC sp_droprolemember N'{0}', N'{1}'";


        public static readonly string SHRINKFILE2 = " SHRINKFILE (N'{0}' , {1})";
        public static readonly string SHRINKFILE3 = " SHRINKFILE (N'{0}' , {1}, {2})";

        public static readonly string REG_WRITE_WRITE_PROP70 = "EXEC xp_regwrite N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'{0}', {1}, {2}";
        public static readonly string REG_WRITE_WRITE_PROP = "EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'{0}', {1}, {2}";
        public static readonly string REG_DELETE70 = "EXEC xp_regdeletevalue N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'{0}'";
        public static readonly string REG_DELETE = "EXEC xp_instance_regdeletevalue N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'{0}'";
        public static readonly string SRV_SET_OPTIONS80 = "EXEC master.dbo.sp_configure N'user options', {0} RECONFIGURE";
        public static readonly string SRV_SET_OPTIONS90 = "EXEC sys.sp_configure N'user options', {0} RECONFIGURE";

        public static readonly string SP_CONTROLPLANGUIDE_NAME = "EXEC sp_control_plan_guide @operation = {0}, @name = {1}";
        public static readonly string SP_CONTROLPLANGUIDE = "EXEC sp_control_plan_guide @operation = {0}";
        public static readonly string SP_CREATEPLANGUIDE = "EXEC sp_create_plan_guide @name = {0}";
        // code used to check if audit/audit specification exists (or not)
        public static readonly string INCLUDE_EXISTS_AUDIT = "IF {0} EXISTS (SELECT * FROM sys.server_audits WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_AUDIT_SPECIFICATION = "IF {0} EXISTS (SELECT * FROM sys.{1}_audit_specifications WHERE name = {2})";
        public static readonly string SP_RDA_REAUTHORIZE_DB = "EXEC sp_rda_reauthorize_db @credential = {0}, @with_copy = {1}";

        public static string DROP_DATABASEROLE_MEMBERS_80 =
@"
Begin

    DECLARE @RoleMemberName sysname
    DECLARE Member_Cursor CURSOR FOR
    select [name]
    from dbo.sysusers 
    where uid in ( 
        select memberuid
        from dbo.sysmembers
        where groupuid in (
            select uid
            FROM dbo.sysusers where [name] = @RoleName AND issqlrole = 1))

    OPEN Member_Cursor;

    FETCH NEXT FROM Member_Cursor
    into @RoleMemberName

    WHILE @@FETCH_STATUS = 0
    BEGIN

        exec sp_droprolemember @rolename=@RoleName, @membername= @RoleMemberName

        FETCH NEXT FROM Member_Cursor
        into @RoleMemberName
    END;

    CLOSE Member_Cursor;
    DEALLOCATE Member_Cursor;

end
";

        public static string DROP_DATABASEROLE_MEMBERS_110 =
@"
BEGIN
    DECLARE @RoleMemberName sysname
    DECLARE Member_Cursor CURSOR FOR
    select [name]
    from sys.database_principals 
    where principal_id in ( 
        select member_principal_id
        from sys.database_role_members
        where role_principal_id in (
            select principal_id
            FROM sys.database_principals where [name] = @RoleName AND type = 'R'))

    OPEN Member_Cursor;

    FETCH NEXT FROM Member_Cursor
    into @RoleMemberName
    
    DECLARE @SQL NVARCHAR(4000)

    WHILE @@FETCH_STATUS = 0
    BEGIN
        
        SET @SQL = 'ALTER ROLE '+ QUOTENAME(@RoleName,'[') +' DROP MEMBER '+ QUOTENAME(@RoleMemberName,'[')
        EXEC(@SQL)
        
        FETCH NEXT FROM Member_Cursor
        into @RoleMemberName
    END;

    CLOSE Member_Cursor;
    DEALLOCATE Member_Cursor;
END
";

        public static string DROP_DATABASEROLE_MEMBERS_90 =
@"
BEGIN
    DECLARE @RoleMemberName sysname
    DECLARE Member_Cursor CURSOR FOR
    select [name]
    from sys.database_principals 
    where principal_id in ( 
        select member_principal_id
        from sys.database_role_members

        where role_principal_id in (
            select principal_id
            FROM sys.database_principals where [name] = @RoleName AND type = 'R'))

    OPEN Member_Cursor;

    FETCH NEXT FROM Member_Cursor
    into @RoleMemberName
    
    WHILE @@FETCH_STATUS = 0
    BEGIN

        exec sp_droprolemember @rolename=@RoleName, @membername= @RoleMemberName

        FETCH NEXT FROM Member_Cursor
        into @RoleMemberName
    END;

    CLOSE Member_Cursor;
    DEALLOCATE Member_Cursor;
END
";

        public static string DROP_DATABASEROLE_MEMBERS_DW =
@"
BEGIN
    CREATE TABLE [#{0}]
    WITH( DISTRIBUTION = ROUND_ROBIN )
    AS
    SELECT  ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS Sequence, [name],
           'exec sp_droprolemember @rolename=['+ @RoleName +'], @membername='  +QUOTENAME([name]) AS sql_code
    from sys.database_principals 
    where principal_id in ( 
            select member_principal_id
            from sys.database_role_members
            where role_principal_id in (
                select principal_id
                FROM sys.database_principals where [name] = @RoleName AND type = 'R'));

    DECLARE @nbr_statements INT = (SELECT COUNT(*) FROM [#{0}]), @i INT = 1;
    WHILE   @i <= @nbr_statements
    BEGIN
        DECLARE @sql_code NVARCHAR(4000) = (SELECT sql_code FROM [#{0}] WHERE Sequence = @i);
        EXEC    sp_executesql @sql_code;
        SET     @i +=1;
    END
    DROP TABLE [#{0}]
END
";

        public static string DROP_SERVER_ROLE_MEMBERS =
@"
BEGIN
    DECLARE @RoleMemberName sysname
    DECLARE Member_Cursor CURSOR FOR
    select [name]
    from sys.server_principals
    where principal_id in ( 
        select member_principal_id 
        from sys.server_role_members 
        where role_principal_id in (
            select principal_id
            FROM sys.server_principals where [name] = @RoleName  AND type = 'R' ))

    OPEN Member_Cursor;

    FETCH NEXT FROM Member_Cursor
    into @RoleMemberName

    DECLARE @SQL NVARCHAR(4000)
        
    WHILE @@FETCH_STATUS = 0
    BEGIN
        
        SET @SQL = 'ALTER SERVER ROLE '+ QUOTENAME(@RoleName,'[') +' DROP MEMBER '+ QUOTENAME(@RoleMemberName,'[')
        EXEC(@SQL)
        
        FETCH NEXT FROM Member_Cursor
        into @RoleMemberName
    END;

    CLOSE Member_Cursor;
    DEALLOCATE Member_Cursor;
END
";

        public static string DECLARE_ROLE_MEMEBER =
@"
DECLARE @RoleName sysname
set @RoleName = N'{0}'
";
        public static readonly string IS_DBROLE_FIXED_OR_PUBLIC_90 =
@"
IF @RoleName <> N'public' and (select is_fixed_role from sys.database_principals where name = @RoleName) = 0";

        public static readonly string IS_SERVER_ROLE_FIXED_OR_PUBLIC =
@"
IF @RoleName <> N'public' and (select is_fixed_role from sys.server_principals where name = @RoleName) = 0";

        public static string INCLUDE_EXISTS_ROLE_MEMBERS90 = "IF  EXISTS (SELECT * FROM sys.database_principals WHERE name = @RoleName AND type = 'R')";
        public static string INCLUDE_EXISTS_ROLE_MEMBERS80 = "IF  EXISTS (SELECT * FROM dbo.sysusers WHERE name = @RoleName AND issqlrole = 1)";

        public static string INCLUDE_EXISTS_SERVER_ROLE_MEMBERS = "IF  EXISTS (SELECT * FROM sys.server_principals WHERE name = @RoleName AND type = 'R')";

        // code used to check if the object exists (or not)
        public static readonly string INCLUDE_EXISTS_TABLE80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)";
        public static readonly string INCLUDE_EXISTS_TABLE90 = "IF {0} EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{1}') AND type in (N'U'))";

        public static readonly string INCLUDE_EXISTS_VIEW90 = "IF {0} EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'{1}'))";
        public static readonly string INCLUDE_EXISTS_VIEW80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(id, N'IsView') = 1)";

        public static readonly string INCLUDE_EXISTS_PROCEDURE80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(id,N'IsProcedure') = 1)";
        public static readonly string INCLUDE_EXISTS_PROCEDURE90 = "IF {0} EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{1}') AND type in (N'P', N'PC'))";

        public static readonly string INCLUDE_EXISTS_NUMBERED_PROCEDURE80 = "IF {0} EXISTS (SELECT * FROM dbo.syscomments WHERE id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(id,N'IsProcedure') = 1 AND colid=1 AND number = {2})";
        public static readonly string INCLUDE_EXISTS_NUMBERED_PROCEDURE90 = "IF {0} EXISTS (SELECT * FROM sys.numbered_procedures WHERE object_id = OBJECT_ID(N'{1}') AND procedure_number = {2})";

        public static readonly string INCLUDE_EXISTS_TRIGGER80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(id, N'IsTrigger') = 1)";
        public static readonly string INCLUDE_EXISTS_TRIGGER90 = "IF {0} EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'{1}'))";

        public static readonly string INCLUDE_EXISTS_RULE_DEFAULT80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(id, N'{2}') = 1)";
        public static readonly string INCLUDE_EXISTS_RULE_DEFAULT90 = "IF {0} EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{1}') AND OBJECTPROPERTY(object_id, N'{2}') = 1)";

        public static readonly string INCLUDE_EXISTS_FUNCTION80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{1}') AND xtype in (N'FN', N'IF', N'TF'))";
        public static readonly string INCLUDE_EXISTS_FUNCTION90 = "IF {0} EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{1}') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))";

        public static readonly string INCLUDE_EXISTS_UDDT80 = "IF {0} EXISTS (SELECT * FROM systypes WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_UDDT90 = "IF {0} EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id WHERE st.name = N'{1}' AND ss.name = N'{2}')";

        public static readonly string INCLUDE_EXISTS_USER80 = "IF {0} EXISTS (SELECT * FROM dbo.sysusers WHERE name = N'{1}')";
        public static readonly string INCLUDE_EXISTS_USER90 = "IF {0} EXISTS (SELECT * FROM sys.database_principals WHERE name = N'{1}')";

        // Check if the statistic already exists on that particular table
        public static readonly string INCLUDE_EXISTS_STATISTIC90 = "if {0} exists (select * from sys.stats where name = {1} and object_id = object_id(N'{2}'))";
        public static readonly string INCLUDE_EXISTS_STATISTIC80 = "if {0} exists (select * from sysindexes si1, sysindexes si2 where si1.name = N'{1}' and si2.name = N'{2}' and si1.id = si2.id)";

        public static readonly string INCLUDE_EXISTS_LOGIN80 = "IF {0} EXISTS (SELECT * FROM master.dbo.syslogins WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_LOGIN90 = "IF {0} EXISTS (SELECT * FROM sys.server_principals WHERE name = {1})";

        public static readonly string INCLUDE_EXISTS_APPROLE80 = "IF {0} EXISTS (SELECT * FROM dbo.sysusers WHERE name = {1} AND isapprole = 1)";
        public static readonly string INCLUDE_EXISTS_APPROLE90 = "IF {0} EXISTS (SELECT * FROM sys.database_principals WHERE name = {1} AND type = 'A')";

        public static readonly string INCLUDE_EXISTS_DBROLE80 = "IF {0} EXISTS (SELECT * FROM dbo.sysusers WHERE name = {1} AND issqlrole = 1)";
        public static readonly string INCLUDE_EXISTS_DBROLE90 = "IF {0} EXISTS (SELECT * FROM sys.database_principals WHERE name = {1} AND type = 'R')";

        public static readonly string INCLUDE_EXISTS_SERVER_ROLE = "IF {0} EXISTS (SELECT * FROM sys.server_principals WHERE name = {1} AND type = 'R')";

        public static readonly string INCLUDE_EXISTS_CHECK80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID({1}) AND type = 'C')";
        public static readonly string INCLUDE_EXISTS_CHECK90 = "IF {0} EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID({1}) AND parent_object_id = OBJECT_ID({2}))";

        public static readonly string INCLUDE_EXISTS_FOREIGN_KEY80 = "IF {0} EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID({1}) AND type = 'F')";
        public static readonly string INCLUDE_EXISTS_FOREIGN_KEY90 = "IF {0} EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID({1}) AND parent_object_id = OBJECT_ID({2}))";

        public static readonly string INCLUDE_EXISTS_SCHEMA90 = "IF {0} EXISTS (SELECT * FROM sys.schemas WHERE name = {1})";

        public static readonly string INCLUDE_EXISTS_DATABASE80 = "IF {0} EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_DATABASE90 = "IF {0} EXISTS (SELECT name FROM sys.databases WHERE name = {1})";

        public static readonly string INCLUDE_IF_NOT_EXISTS_XPROCEDURE = "IF {0} EXISTS (SELECT * FROM sys.extended_procedures WHERE object_id = OBJECT_ID(N'{1}'))";
        public static readonly string INCLUDE_EXISTS_UDT = "IF {0} EXISTS (SELECT * FROM sys.assembly_types at INNER JOIN sys.schemas ss on at.schema_id = ss.schema_id WHERE at.name = N'{1}' AND ss.name=N'{2}')";
        public static readonly string INCLUDE_EXISTS_ASSEMBLY = "IF {0} EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'{1}')";
        public static readonly string INCLUDE_EXISTS_ASSEMBLY100 = "IF {0} EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'{1}' and is_user_defined = 1)";
        public static readonly string INCLUDE_EXISTS_EXTERNAL_LIBRARY = "IF {0} EXISTS (SELECT * FROM sys.external_libraries libs WHERE libs.name = N'{1}')";
        public static readonly string INCLUDE_EXISTS_UDA = "IF {0} EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{1}') AND type = N'AF')";
        public static readonly string INCLUDE_EXISTS_FT_CATALOG = "IF {0} EXISTS (SELECT * FROM sysfulltextcatalogs ftc WHERE ftc.name = N'{1}')";
        public static readonly string INCLUDE_EXISTS_FT_INDEX = "IF (OBJECTPROPERTY(OBJECT_ID(N'{0}'), 'TableFullTextCatalogId') {1} 0) ";
        public static readonly string INCLUDE_EXISTS_FT_INDEX90 = "IF {0} EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'{1}'))";
        public static readonly string INCLUDE_EXISTS_FT_STOPLIST = "IF {0} EXISTS (SELECT * FROM sys.fulltext_stoplists ftsl WHERE ftsl.name = N'{1}')";
        public static readonly string INCLUDE_EXISTS_SEARCH_PROPERTY_LIST = "IF {0} EXISTS (SELECT * FROM sys.registered_search_property_lists spl WHERE spl.name = N'{1}')";
        public static readonly string INCLUDE_EXISTS_SEARCH_PROPERTY = "IF {0} EXISTS (SELECT * FROM sys.registered_search_properties sp WHERE sp.search_property_list_id = {1} AND sp.property_name = N'{2}')";
        public static readonly string INCLUDE_EXISTS_DB_ROLE = "IF {0} EXISTS (SELECT * FROM sysusers u WHERE u.name = {1} AND u.issqlrole = 1 or u.isapprole = 1 ) AND LOWER({1}) != N'public'";

        public static readonly string INCLUDE_EXISTS_DATABASE_TRIGGER90 = "IF {0} EXISTS (SELECT * FROM sys.triggers WHERE name = {1} AND parent_class=0)";
        public static readonly string INCLUDE_EXISTS_SERVER_TRIGGER90 = "IF {0} EXISTS (SELECT * FROM master.sys.server_triggers WHERE name = {1} AND parent_class=100)";

        public static readonly string INCLUDE_EXISTS_ENDPOINT = "IF {0} EXISTS (SELECT * FROM sys.endpoints e WHERE e.name = {1}) ";
        public static readonly string INCLUDE_EXISTS_XML_COLLECTION = "IF {0} EXISTS (SELECT * FROM sys.xml_schema_collections c, sys.schemas s WHERE c.schema_id = s.schema_id AND (quotename(s.name) + '.' + quotename(c.name)) = N'{1}')";

        public static readonly string INCLUDE_EXISTS_SYNONYM = "IF {0} EXISTS (SELECT * FROM sys.synonyms WHERE name = {1} AND schema_id = SCHEMA_ID({2}))";
        public static readonly string INCLUDE_EXISTS_SECURITY_POLICY = "IF {0} EXISTS (SELECT * FROM sys.security_policies WHERE name = '{1}')";
        public static readonly string INCLUDE_EXISTS_SECURITY_PREDICATE = "IF {0} EXISTS (SELECT * FROM sys.security_predicates WHERE target_object_id = {1} and object_id = {2})";
        public static readonly string INCLUDE_EXISTS_SEQUENCE = "IF {0} EXISTS (SELECT * FROM sys.sequences WHERE name = {1} AND schema_id = SCHEMA_ID({2}))";
        public static readonly string INCLUDE_EXISTS_COLUMN_MASTER_KEY = "IF {0} EXISTS (SELECT * FROM sys.column_master_keys WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_COLUMN_ENCRYPTION_KEY = "IF {0} EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_CEK_VALUE = "IF {0} EXISTS (SELECT * FROM sys.column_encryption_key_values WHERE column_encryption_key_id = {1} and column_master_key_id = {2})";

        public static readonly string INCLUDE_EXISTS_EXTERNAL_DATA_SOURCE = "IF {0} EXISTS (SELECT * FROM sys.external_data_sources WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_EXTERNAL_FILE_FORMAT = "IF {0} EXISTS (SELECT * FROM sys.external_file_formats WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_PLANGUIDE = "IF {0} EXISTS (SELECT * FROM sys.plan_guides WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_DATABASE_DDL_TRIGGER = "IF {0} EXISTS (SELECT * FROM sys.triggers WHERE parent_class_desc = 'DATABASE' AND name = {1})";
        public static readonly string INCLUDE_EXISTS_SERVER_DDL_TRIGGER = "IF {0} EXISTS (SELECT * FROM master.sys.server_triggers WHERE parent_class_desc = 'SERVER' AND name = {1})";
        public static readonly string INCLUDE_EXISTS_INDEX90 = "IF {0} EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'{1}') AND name = {2})";
        public static readonly string INCLUDE_EXISTS_INDEX80 = "IF {0} EXISTS (SELECT * FROM dbo.sysindexes WHERE id = OBJECT_ID(N'{1}') AND name = {2})";
        public static readonly string INCLUDE_EXISTS_PARTITION_FUNCTION = "IF {0} EXISTS (SELECT * FROM sys.partition_functions WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_PARTITION_SCHEME = "IF {0} EXISTS (SELECT * FROM sys.partition_schemes WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_MESSAGE_TYPE = "IF {0} EXISTS (SELECT * FROM sys.service_message_types WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_SERVICE_CONTRACT = "IF {0} EXISTS (SELECT * FROM sys.service_contracts WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_SERVICE_ROUTE = "IF {0} EXISTS (SELECT * FROM sys.routes WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_SERVICE_QUEUE = "IF {0} EXISTS (SELECT * FROM sys.service_queues WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_BROKER_SERVICE = "IF {0} EXISTS (SELECT * FROM sys.services WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_BROKER_PRIORITY = "IF {0} EXISTS (SELECT * FROM sys.conversation_priorities WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_REMOTE_SERVICE_BINDING = "IF {0} EXISTS (SELECT * FROM sys.remote_service_bindings WHERE name = {1})";

        public static readonly string INCLUDE_EXISTS_AGENT_ALERT = "IF {0} EXISTS (SELECT name FROM msdb.dbo.sysalerts WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_BACKUP_DEVICE = "IF {0} EXISTS (SELECT name FROM master.dbo.sysdevices WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_CATEGORY = "IF {0} EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'{1}' AND category_class={2})";
        public static readonly string INCLUDE_EXISTS_CERTIFICATE = "IF {0} EXISTS (SELECT certificate_id FROM sys.certificates WHERE certificate_id = {1})";
        public static readonly string INCLUDE_EXISTS_CREDENTIAL = "IF {0} EXISTS (SELECT credential_id FROM sys.credentials WHERE credential_id = {1})";
        public static readonly string INCLUDE_EXISTS_DATABASESCOPEDCREDENTIAL = "IF {0} EXISTS (SELECT credential_id FROM sys.database_scoped_credentials WHERE credential_id = {1})";
        public static readonly string INCLUDE_EXISTS_CRYPTOGRAPHIC_PROVIDER = "IF {0} EXISTS (SELECT name FROM sys.cryptographic_providers WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_JOB = "IF {0} EXISTS (SELECT job_id FROM msdb.dbo.sysjobs_view WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_SCHEDULE = "IF {0} EXISTS (SELECT schedule_id FROM msdb.dbo.sysschedules_localserver_view WHERE schedule_id = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_JOBSCHEDULE = "IF {0} EXISTS (SELECT schedule_id FROM msdb.dbo.sysjobschedules WHERE schedule_id = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_JOBSTEP = "IF {0} EXISTS (SELECT * FROM msdb.dbo.sysjobsteps WHERE job_id = {1} and step_id = {2})";
        public static readonly string INCLUDE_EXISTS_AGENT_OPERATOR = "IF {0} EXISTS (SELECT name FROM msdb.dbo.sysoperators WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_PROXY = "IF {0} EXISTS (SELECT name FROM msdb.dbo.sysproxies WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_AGENT_TARGETSERVERGROUP = "IF {0} EXISTS (SELECT name FROM msdb.dbo.systargetservergroups WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_LINKED_SERVER80 = "IF {0} EXISTS (SELECT srvname FROM master.dbo.sysservers srv WHERE srv.srvid != 0 AND srv.srvname = {1})";
        public static readonly string INCLUDE_EXISTS_LINKED_SERVER90 = "IF {0} EXISTS (SELECT srv.name FROM sys.servers srv WHERE srv.server_id != 0 AND srv.name = {1})";

        public static readonly string INCLUDE_EXISTS_LINKED_SERVER_LOGIN80 = "IF {0} EXISTS (SELECT * FROM master.dbo.sysservers AS srv INNER JOIN master.dbo.sysxlogins lnklgn ON (lnklgn.ishqoutmap = 1) AND (lnklgn.srvid=CAST(srv.srvid AS int)) LEFT OUTER JOIN master.dbo.sysxlogins xlnklgn ON lnklgn.sid=xlnklgn.sid and ISNULL(xlnklgn.ishqoutmap,0) = 0 WHERE (ISNULL(xlnklgn.name, '')={1})and((srv.srvid != 0)))";
        public static readonly string INCLUDE_EXISTS_LINKED_SERVER_LOGIN90 = "IF {0} EXISTS (SELECT * FROM sys.servers AS srv INNER JOIN sys.linked_logins ll ON ll.server_id=CAST(srv.server_id AS int) LEFT OUTER JOIN sys.server_principals sp ON ll.local_principal_id = sp.principal_id WHERE (ISNULL(sp.name, '')={1})and((srv.server_id != 0)))";

        public static readonly string INCLUDE_EXISTS_MAIL_ACCOUNT = "IF {0} EXISTS (SELECT name FROM msdb.dbo.sysmail_account WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_MAIL_PROFILE = "IF {0} EXISTS (SELECT profile_id FROM msdb.dbo.sysmail_profile WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_DATABASE_ENCRYPTION_KEY = "IF {0} EXISTS (SELECT database_id FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID())";

        public static readonly string INCLUDE_EXISTS_AVAILABILITY_GROUP = "IF {0} EXISTS (SELECT name FROM sys.availability_groups WHERE name = {1})";

        public static readonly string INCLUDE_EXISTS_AVAILABILITY_REPLICA =
        "IF {0} EXISTS " +
        "(SELECT ar.replica_server_name FROM sys.availability_groups AS ag " +
        "INNER JOIN sys.availability_replicas AS ar ON (ar.group_id = ag.group_id) " +
        "WHERE (ar.replica_server_name = {1}) and (ag.name = {2}))";

        public static readonly string INCLUDE_EXISTS_AVAILABILITY_DATABASE =
        "IF {0} EXISTS " +
        "(SELECT db.name FROM sys.availability_groups AS ag " +
        "INNER JOIN sys.dm_hadr_database_replica_states AS dbrs ON (dbrs.group_id = ag.group_id) AND (dbrs.is_local = 1) " +
        "INNER JOIN sys.databases AS db ON db.database_id = dbrs.database_id " +
        "WHERE (db.Name = {1}) AND (ag.Name = {2}))";

        public static readonly string INCLUDE_EXISTS_AVAILABILITY_GROUP_LISTENER =
        "IF {0} EXISTS " +
        "(SELECT agl.dns_name FROM sys.availability_groups AS ag " +
        "INNER JOIN master.sys.availability_group_listeners AS agl ON (agl.group_id = ag.group_id) " +
        "WHERE agl.dns_name = {1})";

        // Resource Governor
        public static readonly string INCLUDE_EXISTS_RG_RESOUREPOOL = "IF {0} EXISTS ( SELECT name FROM sys.resource_governor_resource_pools WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_RG_EXTERNALRESOUREPOOL = "IF {0} EXISTS ( SELECT name FROM sys.resource_governor_external_resource_pools WHERE name = {1})";
        public static readonly string INCLUDE_EXISTS_RG_WORKLOADGROUP = "IF {0} EXISTS ( SELECT name FROM sys.resource_governor_workload_groups WHERE name = {1})";
        public static readonly string RESOURCE_GOVERNOR_RECONFIGURE = "ALTER RESOURCE GOVERNOR RECONFIGURE;";

        public static readonly string IF_SCHEMA_NOT_EXISTS_WITH_GIVEN_OWNER =
@"
-- if schema does not already exist with the same name and owner
declare @schema_uid int
declare @owner_uid  int
select @owner_uid  = principal_id from sys.database_principals where name = N'{1}'
select @schema_uid = principal_id from sys.schemas where name = N'{0}'
if (@schema_uid is null or      -- schema is not present
    @schema_uid <> @owner_uid)  -- does not have same owner
"; //parameter 0 is schema name and parameter 1 is user name

        public static readonly string IF_SCHEMA_EXISTS_WITH_GIVEN_OWNER =
@"
-- if schema already exists with the same name and owner
declare @schema_uid int
declare @owner_uid  int
select @owner_uid  = principal_id from sys.database_principals where name = N'{1}'
select @schema_uid = principal_id from sys.schemas where name = N'{0}'
if (@schema_uid is not null and     -- schema is present
    @schema_uid = @owner_uid)       -- has same owner
"; //parameter 0 is schema name and parameter 1 is user name

        // begin end
        public static readonly string BEGIN = "BEGIN";
        public static readonly string END = "END";

        //Any
        public static readonly string ANY = "ANY";
        public static readonly string DEFAULT = "DEFAULT";

        // connection options
        public static readonly string SET_ANSI_NULLS = "SET ANSI_NULLS {0}";
        public static readonly string SET_QUOTED_IDENTIFIER = "SET QUOTED_IDENTIFIER {0}";

        public static readonly string ENUM_STATISTICS = "DBCC SHOW_STATISTICS(N'[{0}].[{1}]', N'{2}')";
        public static readonly string ENUM_ACCOUNTINFO = "EXEC master.dbo.xp_logininfo";

        //CRUD
        public static readonly string CREATE = "CREATE";
        public static readonly string ALTER = "ALTER";
        public static readonly string DROP = "DROP";
        public static readonly string SET = "SET";
        public static readonly string CREATE_OR_ALTER = "CREATE OR ALTER";

        //join
        public static readonly string JOIN = "JOIN";

        //DML related
        public static readonly string DELETE = "DELETE";
        public static readonly string INSERT = "INSERT";
        public static readonly string UPDATE = "UPDATE";

        //HADR
        public static readonly string HADR = "HADR";
        public static readonly string SUSPEND = "SUSPEND";
        public static readonly string RESUME = "RESUME";
        public static readonly string REMOVE = "REMOVE";
        public static readonly string ADD = "ADD";

        //HEKATON
        public static readonly string MEMORY_OPTIMIZED = "MEMORY_OPTIMIZED";
        public static readonly string HASH = "NONCLUSTERED HASH";
        public static readonly string UNIQUE = "UNIQUE";
        public static readonly string PRIMARY_KEY = "PRIMARY KEY";
        public static readonly string CONSTRAINT = "CONSTRAINT";
        public static readonly string WITH_BUCKET_COUNT = "BUCKET_COUNT = {0} ,";
        public static readonly string WITH_MEMORY_OPTIMIZED = "WITH ( MEMORY_OPTIMIZED = ON )";
        public static readonly string WITH_MEMORY_OPTIMIZED_AND_DURABILITY = "WITH ( MEMORY_OPTIMIZED = ON , DURABILITY = {0} )";
        public static readonly string WITH_MEMORY_OPTIMIZED_AND_DURABILITY_AND_TEMPORAL_SYSTEM_VERSIONING = "WITH ( MEMORY_OPTIMIZED = ON , DURABILITY = {0}, {1} )";
        public static readonly string INDEX_NAME = "INDEX [{0}]";
        public static readonly string NATIVELY_COMPILED = "NATIVE_COMPILATION";
        public static readonly string SP_SCHEMABINDING = "SCHEMABINDING";
        public static readonly string INLINE_TYPE = "INLINE = ON";

        // STRETCH
        public static readonly string OFF_WITHOUT_DATA_RECOVERY = "OFF_WITHOUT_DATA_RECOVERY";

        //Index type
        public static readonly string NONCLUSTERED = "NONCLUSTERED";
        public static readonly string CLUSTERED = "CLUSTERED";

        //Polybase and GQ
        public static readonly string EXTERNAL_DATASOURCE_NAME = "DATA_SOURCE = {0}";

        // GraphDB
        //
        public static readonly string AS_NODE = "AS NODE";
        public static readonly string AS_EDGE = "AS EDGE";
    }

    internal class Globals
    {
        public static readonly string comma = ",";
        public static readonly string statementTerminator = ";";
        public static readonly string space = " ";
        public static readonly string commaspace = ", ";
        public static readonly string LParen = "(";
        public static readonly string RParen = ")";
        public static readonly string newline = Environment.NewLine;
        public static readonly string tab = "\t"; // 4 spaces may look better than \t
        public static readonly string percent = "%";
        public static readonly string LBracket = "[";
        public static readonly string RBracket = "]";
        public static readonly string Dot = ".";
        public static readonly string Go = "GO";
        public static readonly string On = "ON";
        public static readonly string Off = "OFF";
        public static readonly string For = "FOR";
        public static readonly string With = "WITH";
        public static readonly string EqualSign = "=";

        public static readonly int INIT_BUFFER_SIZE = 1024;

        public static readonly string InstanceNameSeparator = " ";
    }
}


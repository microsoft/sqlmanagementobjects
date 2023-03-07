-- parameters:
-- 1. create table #tempdep (objid int NOT NULL, objtype smallint NOT NULL)
--    contains source objects
-- 2. @find_referencing_objects defines ordering
--    1 order for drop
--    0 order for script

declare @must_set_nocount_off bit
set @must_set_nocount_off = 0

IF @@OPTIONS & 512 = 0 
   set @must_set_nocount_off = 1
set nocount on

declare @u int
declare @udf int
declare @v int
declare @sp int
declare @def int
declare @rule int
declare @tr int
declare @uda int
declare @uddt int
declare @xml int
declare @udt int
declare @assm int
declare @part_sch int
declare @part_func int
declare @synonym int
declare @udtt int
declare @ddltr int
declare @unknown int
declare @pg int

set @u = 3
set @udf = 0
set @v = 2
set @sp = 4
set @def = 6
set @rule = 7
set @tr = 8
set @uda = 11
set @synonym = 12
--above 100 -> not in sys.objects
set @uddt = 101
set @xml = 102
set @udt = 103
set @assm = 1000
set @part_sch = 201
set @part_func = 202
set @udtt = 104
set @ddltr = 203
set @unknown = 1001
set @pg = 204

-- variables for referenced type obtained from sys.sql_expression_dependencies
declare @obj int
set @obj = 20
declare @type int
set @type = 21
-- variables for xml and part_func are already there

create table #t1
(
	object_id int NULL,
	object_name sysname collate database_default NULL,
	object_schema sysname collate database_default NULL,
	object_db sysname NULL,
	object_svr sysname NULL,
	object_type smallint NOT NULL,
	relative_id int NOT NULL,
	relative_name sysname collate database_default NOT NULL,
	relative_schema sysname collate database_default NULL,
	relative_db sysname NULL,
	relative_svr sysname NULL,
	relative_type smallint NOT NULL,
	schema_bound bit NOT NULL,
	rank smallint NULL,
	degree int NULL
)

-- we need to create another temporary table to store the dependencies from sys.sql_expression_dependencies till the updated values are inserted finally into #t1
create table #t2
(
	object_id int NULL,
	object_name sysname collate database_default NULL,
	object_schema sysname collate database_default NULL,
	object_db sysname NULL,
	object_svr sysname NULL,
	object_type smallint NOT NULL,
	relative_id int NOT NULL,
	relative_name sysname collate database_default NOT NULL,
	relative_schema sysname collate database_default NULL,
	relative_db sysname NULL,
	relative_svr sysname NULL,
	relative_type smallint NOT NULL,
	schema_bound bit NOT NULL,
	rank smallint NULL
)

-- This index will ensure that we have unique parent-child relationship
create unique clustered index i1 on #t1(object_name, object_schema, object_db, object_svr, object_type, relative_name, relative_schema, relative_type) with (IGNORE_DUP_KEY=ON)

declare @iter_no int
set @iter_no = 1

declare @rows int
set @rows = 1

insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank) 
   select l.objid, l.objname, l.objschema, l.objdb, l.objtype, l.objid, l.objname, l.objschema, l.objdb, l.objtype, 1, @iter_no from #tempdep l

-- change the object_id of table types to their user_defined_id
update #t1 set object_id = tt.user_type_id, relative_id = tt.user_type_id
from sys.table_types as tt where tt.type_table_object_id = #t1.object_id and object_type = @udtt

while @rows > 0
begin
	set @rows = 0
	if (1 = @find_referencing_objects)
	begin
		-- HARD DEPENDENCIES
		-- these dependencies have to be in the same database only
		-- tables that reference uddts or udts
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select tbl.object_id, tbl.name, SCHEMA_NAME(tbl.schema_id), t.object_db, @u, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.columns as c on c.user_type_id = t.object_id
			join sys.tables as tbl on tbl.object_id = c.object_id
			where @iter_no = t.rank and (t.object_type = @uddt OR t.object_type = @udt) and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- udtts that reference uddts or udts
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select tt.user_type_id, tt.name, SCHEMA_NAME(tt.schema_id), t.object_db, @udtt, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.columns as c on c.user_type_id = t.object_id
			join sys.table_types as tt on tt.type_table_object_id = c.object_id
			where @iter_no = t.rank and (t.object_type = @uddt OR t.object_type = @udt) and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- tables/views that reference triggers
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select o.object_id, o.name, SCHEMA_NAME(o.schema_id), t.object_db, @tr, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.objects as o on o.parent_object_id = t.object_id and o.type = 'TR'
			where @iter_no = t.rank and (t.object_type = @u OR  t.object_type = @v) and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		

		-- table references table
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select tbl.object_id, tbl.name, SCHEMA_NAME(tbl.schema_id), t.object_db, @u, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.foreign_keys as fk on fk.referenced_object_id = t.object_id
			join sys.tables as tbl on tbl.object_id = fk.parent_object_id
			where @iter_no = t.rank and t.object_type = @u and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- uda references types
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select o.object_id, o.name, SCHEMA_NAME(o.schema_id), t.object_db, @uda, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.parameters as p on p.user_type_id = t.object_id
			join sys.objects as o on o.object_id = p.object_id and o.type = 'AF'
			where @iter_no = t.rank and t.object_type in (@udt, @uddt, @udtt) and (t.object_svr IS null and t.object_db = db_name())

		-- table,view references partition scheme
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select o.object_id, o.name, SCHEMA_NAME(o.schema_id), t.object_db, (case o.type when 'V' then @v else @u end), t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.indexes as idx on idx.data_space_id = t.object_id
			join sys.objects as o on o.object_id = idx.object_id
			where @iter_no = t.rank and t.object_type = @part_sch and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- partition scheme references partition function
		insert #t1 (object_id, object_name, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select ps.data_space_id, ps.name, t.object_db, @part_sch, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.partition_schemes as ps on ps.function_id = t.object_id
			where @iter_no = t.rank and t.object_type = @part_func and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- synonym refrences object
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select s.object_id, s.name, SCHEMA_NAME(s.schema_id), t.object_db, @synonym, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 0, @iter_no + 1
			from #t1 as t
			join sys.synonyms as s on object_id(s.base_object_name) = t.object_id
			where @iter_no = t.rank and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount
	
		-- DatabaseDdlTrigger 
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
        select  obj.object_id ,obj.name ,null,null,@ddltr,t.object_id, t.object_name,t.object_schema,t.object_db, t.object_type, 1,@iter_no + 1
         from #t1 as t
         join sys.sql_expression_dependencies as dp on 
         (dp.referenced_id = t.object_id Or (dp.referenced_entity_name = t.object_name collate database_default AND dp.is_caller_dependent = 1 ))
         join sys.triggers as obj on obj.object_id = dp.referencing_id and obj.parent_class = 0
         where @iter_no = t.rank
      set @rows = @rows + @@rowcount
      
	  --view, procedure references table, view, procedure
      --procedure references type
      --table(check) references procedure
      --trigger references table, procedure
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
        select  case when obj.type in ('C' , 'D') then obj.parent_object_id else obj.object_id end,case when obj.type in ('C','D' ) then OBJECT_NAME(obj.parent_object_id) else OBJECT_NAME(obj.object_id) end,schema_name(obj.schema_id),t.object_db,
               case when obj.type  in ('U', 'C','D') then @u when 'V' = obj.type then @v when 'TR' = obj.type then @tr 
               when obj.type in ( 'P', 'RF', 'PC' ) then @sp 
               when obj.type in ( 'TF', 'FN', 'IF', 'FS', 'FT' ) then @udf
               end,  t.object_id, t.object_name,t.object_schema,t.object_db, t.object_type, dp.is_schema_bound_reference,@iter_no + 1
         from #t1 as t
         join sys.sql_expression_dependencies as dp on 
         (dp.referenced_id = t.object_id Or (dp.referenced_entity_name = t.object_name collate database_default AND dp.is_caller_dependent = 1 ))
         join sys.objects as obj on obj.object_id = dp.referencing_id and obj.type in ( 'U', 'V', 'P', 'RF', 'PC', 'TR', 'TF', 'FN', 'IF', 'FS', 'FT', 'C' , 'D')
         where @iter_no = t.rank
      set @rows = @rows + @@rowcount
      
     


	end
	else
	begin
		-- SOFT DEPENDENCIES
		-- insert all values from sys.sql_expression_dependencies for the corresponding object
		-- first insert them in #t2, update them and then finally insert them in #t1
		insert #t2 (object_type, object_name, object_schema, object_db, object_svr, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select 
				case dep.referenced_class when 1 then @obj
				when 6 then @type
				when 10 then @xml
				when 21 then @part_func
				end,
			dep.referenced_entity_name,
			dep.referenced_schema_name,
			dep.referenced_database_name,
			dep.referenced_server_name,
			t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type,
			dep.is_schema_bound_reference, @iter_no + 1
			from #t1 as t
			join sys.sql_expression_dependencies as dep on dep.referencing_id = t.object_id
			where @iter_no = t.rank and t.object_svr IS NULL and t.object_db = db_name()

		-- insert all the dependency values in case of a table that references a check
		insert #t2 (object_type, object_name, object_schema, object_db, object_svr, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select 
				case dep.referenced_class when 1 then @obj
				when 6 then @type
				when 10 then @xml
				when 21 then @part_func
				end,
			dep.referenced_entity_name,
			dep.referenced_schema_name,
			dep.referenced_database_name,
			dep.referenced_server_name,
			t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type,
			dep.is_schema_bound_reference, @iter_no + 1
			from #t1 as t
			join sys.sql_expression_dependencies as d on d.referenced_id = t.object_id
			join sys.objects as o on o.object_id = d.referencing_id and o.type = 'C'
			join sys.sql_expression_dependencies as dep on dep.referencing_id = d.referencing_id and dep.referenced_id != t.object_id
			where @iter_no = t.rank and t.object_svr IS NULL and t.object_db = db_name() and t.object_type = @u

		-- insert all the dependency values in case of an object that belongs to another object whose dependencies are being found
		insert #t2 (object_type, object_name, object_schema, object_db, object_svr, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select
				case dep.referenced_class when 1 then @obj
				when 6 then @type
				when 10 then @xml
				when 21 then @part_func
				end,
			dep.referenced_entity_name,
			dep.referenced_schema_name,
			dep.referenced_database_name,
			dep.referenced_server_name,
			t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type,
			dep.is_schema_bound_reference, @iter_no + 1
			from #t1 as t
			join sys.objects as o on o.parent_object_id = t.object_id
			join sys.sql_expression_dependencies as dep on dep.referencing_id = o.object_id
			where @iter_no = t.rank and t.object_svr IS NULL and t.object_db = db_name()

		-- queries for objects with object_id null and object_svr null - resolve them
		-- we will build the query to resolve the objects 
		-- increase @rows as we bind the objects
		
		DECLARE db_cursor CURSOR
		FOR
			select distinct ISNULL(object_db, db_name()) from #t2 as t
			where t.rank = (@iter_no+1) and t.object_id IS NULL and t.object_svr IS NULL
		OPEN db_cursor
		DECLARE @dbname sysname
		FETCH NEXT FROM db_cursor INTO @dbname
		WHILE (@@FETCH_STATUS <> -1)
		BEGIN
			IF (db_id(@dbname) IS NULL) 
			BEGIN
				FETCH NEXT FROM db_cursor INTO @dbname
				CONTINUE
			END
			DECLARE @query nvarchar(MAX)
			-- when schema is not null 
			-- @obj
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = obj.object_id, object_type = 
							case when obj.type = ''U'' then ' + CAST(@u AS nvarchar(8)) +
							' when obj.type = ''V'' then ' + CAST(@v AS nvarchar(8)) +
							' when obj.type = ''TR'' then ' + CAST(@tr AS nvarchar(8)) +
							' when obj.type in ( ''P'', ''RF'', ''PC'' ) then ' + CAST(@sp AS nvarchar(8)) +
							' when obj.type in ( ''AF'' ) then ' + CAST(@uda AS nvarchar(8)) +
							' when obj.type in ( ''TF'', ''FN'', ''IF'', ''FS'', ''FT'' ) then ' + CAST(@udf AS nvarchar(8)) +
							' when obj.type = ''D'' then ' + CAST(@def AS nvarchar(8)) +
							' when obj.type = ''SN'' then ' + CAST(@synonym AS nvarchar(8)) +
							' else ' + CAST(@unknown AS nvarchar(8)) +
							' end
				from ' + quotename(@dbname) + '.sys.objects as obj 
				join ' + quotename(@dbname) + '.sys.schemas as sch on sch.schema_id = obj.schema_id
				where obj.name = #t2.object_name collate database_default
				and sch.name = #t2.object_schema collate database_default
				and #t2.object_type = ' + CAST(@obj AS nvarchar(8)) + ' and #t2.object_schema IS NOT NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)
			-- @type
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = t.user_type_id, object_type = case when t.is_assembly_type = 1 then ' + CAST(@udt AS nvarchar(8)) + ' when t.is_table_type = 1 then ' + CAST(@udtt AS nvarchar(8)) + ' else ' + CAST(@uddt AS nvarchar(8)) + ' end
				from ' + quotename(@dbname) + '.sys.types as t
				join ' + quotename(@dbname) + '.sys.schemas as sch on sch.schema_id = t.schema_id
				where t.name = #t2.object_name collate database_default
				and sch.name = #t2.object_schema collate database_default
				and #t2.object_type = ' + CAST(@type AS nvarchar(8)) + ' and #t2.object_schema IS NOT NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)

			-- @xml
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = x.xml_collection_id 
				from ' + quotename(@dbname) + '.sys.xml_schema_collections as x
				join ' + quotename(@dbname) + '.sys.schemas as sch on sch.schema_id = x.schema_id
				where x.name = #t2.object_name collate database_default
				and sch.name = #t2.object_schema collate database_default
				and #t2.object_type = ' + CAST(@xml AS nvarchar(8)) + ' and #t2.object_schema IS NOT NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)
			-- @part_func - schema is always null
			-- @schema is null
			-- consider schema as 'dbo'
			-- @obj
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = obj.object_id, object_schema = SCHEMA_NAME(obj.schema_id), object_type = 
							case when obj.type = ''U'' then ' + CAST(@u AS nvarchar(8)) +
							' when obj.type = ''V'' then ' + CAST(@v AS nvarchar(8)) +
							' when obj.type = ''TR'' then ' + CAST(@tr AS nvarchar(8)) +
							' when obj.type in ( ''P'', ''RF'', ''PC'' ) then ' + CAST(@sp AS nvarchar(8)) +
							' when obj.type in ( ''AF'' ) then ' + CAST(@uda AS nvarchar(8)) +
							' when obj.type in ( ''TF'', ''FN'', ''IF'', ''FS'', ''FT'' ) then ' + CAST(@udf AS nvarchar(8)) +
							' when obj.type = ''D'' then ' + CAST(@def AS nvarchar(8)) +
							' when obj.type = ''SN'' then ' + CAST(@synonym AS nvarchar(8)) +
							' else ' + CAST(@unknown AS nvarchar(8)) +
							' end
				from ' + quotename(@dbname) + '.sys.objects as obj 
				where obj.name = #t2.object_name collate database_default
				and SCHEMA_NAME(obj.schema_id) = ''dbo''
				and #t2.object_type = ' + CAST(@obj AS nvarchar(8)) + ' and #t2.object_schema IS NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)
			-- @type
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = t.user_type_id, object_schema = SCHEMA_NAME(t.schema_id), object_type = case when t.is_assembly_type = 1 then ' + CAST(@udt AS nvarchar(8)) + ' when t.is_table_type = 1 then ' + CAST(@udtt AS nvarchar(8)) + ' else ' + CAST(@uddt AS nvarchar(8)) + ' end
				from ' + quotename(@dbname) + '.sys.types as t
				where t.name = #t2.object_name collate database_default
				and SCHEMA_NAME(t.schema_id) = ''dbo''
				and #t2.object_type = ' + CAST(@type AS nvarchar(8)) + ' and #t2.object_schema IS NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)
			-- @xml
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = x.xml_collection_id, object_schema = SCHEMA_NAME(x.schema_id)
				from ' + quotename(@dbname) + '.sys.xml_schema_collections as x
				where x.name = #t2.object_name collate database_default
				and SCHEMA_NAME(x.schema_id) = ''dbo''
				and #t2.object_type = ' + CAST(@xml AS nvarchar(8)) + ' and #t2.object_schema IS NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)

			-- consider schema as t.relative_schema
			-- the parent object will have the default schema of user in case of dynamic schema binding
			-- @obj
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = obj.object_id, object_schema = SCHEMA_NAME(obj.schema_id), object_type = 
							case when obj.type = ''U'' then ' + CAST(@u AS nvarchar(8)) +
							' when obj.type = ''V'' then ' + CAST(@v AS nvarchar(8)) +
							' when obj.type = ''TR'' then ' + CAST(@tr AS nvarchar(8)) +
							' when obj.type in ( ''P'', ''RF'', ''PC'' ) then ' + CAST(@sp AS nvarchar(8)) +
							' when obj.type in ( ''AF'' ) then ' + CAST(@uda AS nvarchar(8)) +
							' when obj.type in ( ''TF'', ''FN'', ''IF'', ''FS'', ''FT'' ) then ' + CAST(@udf AS nvarchar(8)) +
							' when obj.type = ''D'' then ' + CAST(@def AS nvarchar(8)) +
							' when obj.type = ''SN'' then ' + CAST(@synonym AS nvarchar(8)) +
							' else ' + CAST(@unknown AS nvarchar(8)) +
							' end
				from ' + quotename(@dbname) + '.sys.objects as obj 
				join ' + quotename(@dbname) + '.sys.schemas as sch on sch.schema_id = obj.schema_id
				where obj.name = #t2.object_name collate database_default
				and sch.name = #t2.relative_schema collate database_default
				and #t2.object_type = ' + CAST(@obj AS nvarchar(8)) + ' and #t2.object_schema IS NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)

			-- @type
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = t.user_type_id, object_schema = SCHEMA_NAME(t.schema_id), object_type = case when t.is_assembly_type = 1 then ' + CAST(@udt AS nvarchar(8)) + ' when t.is_table_type = 1 then ' + CAST(@udtt AS nvarchar(8)) + ' else ' + CAST(@uddt AS nvarchar(8)) + ' end
				from ' + quotename(@dbname) + '.sys.types as t
				join ' + quotename(@dbname) + '.sys.schemas as sch on sch.schema_id = t.schema_id
				where t.name = #t2.object_name collate database_default
				and sch.name = #t2.relative_schema collate database_default
				and #t2.object_type = ' + CAST(@type AS nvarchar(8)) + ' and #t2.object_schema IS NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)

			-- @xml
			SET @query = 'update #t2 set object_db = N' + quotename(@dbname, '''') + ', object_id = x.xml_collection_id, object_schema = SCHEMA_NAME(x.schema_id)
				from ' + quotename(@dbname) + '.sys.xml_schema_collections as x
				join ' + quotename(@dbname) + '.sys.schemas as sch on sch.schema_id = x.schema_id
				where x.name = #t2.object_name collate database_default
				and sch.name = #t2.relative_schema collate database_default
				and #t2.object_type = ' + CAST(@xml AS nvarchar(8)) + ' and #t2.object_schema IS NULL 
				and (#t2.object_db IS NULL or #t2.object_db = ''' + @dbname + ''')
				and #t2.rank = (' + CAST(@iter_no AS nvarchar(8)) + '+1) and #t2.object_id IS NULL and #t2.object_svr IS NULL'
			EXEC (@query)

			-- update the shared object if any (schema is not null)
			update #t2 set object_db = 'master', object_id = o.object_id, object_type = @sp
			from sys.objects as o 
			join sys.schemas as sch on sch.schema_id = o.schema_id
			where o.name = #t2.object_name collate database_default and sch.name = #t2.object_schema collate database_default and 
			o.type in ('P', 'RF', 'PC') and #t2.object_id IS null and
			#t2.object_name LIKE 'sp/_%' ESCAPE '/' and #t2.object_db IS null and #t2.object_svr IS null

			-- update the shared object if any (schema is null)
			update #t2 set object_db = 'master', object_id = o.object_id, object_schema = SCHEMA_NAME(o.schema_id), object_type = @sp
			from sys.objects as o 
			where o.name = #t2.object_name collate database_default and SCHEMA_NAME(o.schema_id) = 'dbo' collate database_default  and 
			o.type in ('P', 'RF', 'PC') and 
			#t2.object_schema IS null and #t2.object_id IS null and
			#t2.object_name LIKE 'sp/_%' ESCAPE '/' and #t2.object_db IS null and #t2.object_svr IS null

			FETCH NEXT FROM db_cursor INTO @dbname
		END
		CLOSE db_cursor
		DEALLOCATE db_cursor

	update #t2 set object_type = @unknown where object_id IS NULL

		insert #t1 (object_id, object_name, object_schema, object_db, object_svr, object_type, relative_id, relative_name, relative_schema, relative_db, relative_svr, relative_type, schema_bound, rank)
			select object_id, object_name, object_schema, object_db, object_svr, object_type, relative_id, relative_name, relative_schema, relative_db, relative_svr, relative_type, schema_bound, rank 
			from #t2 where @iter_no + 1 = rank
		SET @rows = @rows + @@rowcount


		-- HARD DEPENDENCIES
		-- uddt or udt referenced by table
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select tp.user_type_id, tp.name, SCHEMA_NAME(tp.schema_id), t.object_db, case tp.is_assembly_type when 1 then @udt else @uddt end, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.columns as col on col.object_id = t.object_id
			join sys.types as tp on tp.user_type_id = col.user_type_id and tp.schema_id != 4
			where @iter_no = t.rank and t.object_type = @u and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount
		
		-- uddt or udt referenced by table type
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select tp.user_type_id, tp.name, SCHEMA_NAME(tp.schema_id), t.object_db, case tp.is_assembly_type when 1 then @udt else @uddt end, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.table_types as tt on tt.user_type_id = t.object_id
			join sys.columns as col on col.object_id = tt.type_table_object_id
			join sys.types as tp on tp.user_type_id = col.user_type_id and tp.schema_id != 4
			where @iter_no = t.rank and t.object_type = @udtt and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- table or view referenced by trigger
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select o.object_id, o.name, SCHEMA_NAME(o.schema_id), t.object_db, case o.type when 'V' then @v else @u end, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.triggers as tr on tr.object_id = t.object_id
			join sys.objects as o on o.object_id = tr.parent_id
			where @iter_no = t.rank and t.object_type = @tr and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount
	

		-- table referenced by table
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select tbl.object_id, tbl.name, SCHEMA_NAME(tbl.schema_id), t.object_db, @u, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.foreign_keys as fk on fk.parent_object_id = t.object_id
			join sys.tables as tbl on tbl.object_id = fk.referenced_object_id
			where @iter_no = t.rank and t.object_type = @u and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount
		
		-- Partition Schemes referenced by tables/views
		insert #t1 (object_id, object_name, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select ps.data_space_id, ps.name, t.object_db, @part_sch, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.indexes as idx on idx.object_id = t.object_id
			join sys.partition_schemes as ps on ps.data_space_id = idx.data_space_id
			where @iter_no = t.rank and t.object_type in (@u, @v) and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		-- Partition Function referenced by Partition Schemes
		insert #t1 (object_id, object_name, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select pf.function_id, pf.name, t.object_db, @part_func, t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 1, @iter_no + 1
			from #t1 as t
			join sys.partition_schemes as ps on ps.data_space_id = t.object_id
			join sys.partition_functions as pf on pf.function_id = ps.function_id
			where @iter_no = t.rank and t.object_type = @part_sch and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount

		
		-- objects referenced by synonym
		insert #t1 (object_id, object_name, object_schema, object_db, object_type, relative_id, relative_name, relative_schema, relative_db, relative_type, schema_bound, rank)
			select o.object_id, o.name, SCHEMA_NAME(o.schema_id), t.object_db, (case when o.type = 'U' then @u when o.type = 'V' then @v when o.type in ('P', 'RF', 'PC') then @sp when o.type = 'AF' then @uda else @udf end), t.object_id, t.object_name, t.object_schema, t.object_db, t.object_type, 0, @iter_no + 1
			from #t1 as t
			join sys.synonyms as s on s.object_id = t.object_id
			join sys.objects as o on o.object_id = OBJECT_ID(s.base_object_name) and o.type in ('U', 'V', 'P', 'RF', 'PC', 'AF', 'TF', 'FN', 'IF', 'FS', 'FT')
			where @iter_no = t.rank and t.object_type = @synonym and (t.object_svr IS null and t.object_db = db_name())
		set @rows = @rows + @@rowcount
	end
	set @iter_no = @iter_no + 1
end

update #t1 set rank = 0
-- computing the degree of the nodes
update #t1 set degree = (
	select count(*) from #t1 t
	where t.relative_id = #t1.object_id and t.object_id != t.relative_id)

-- perform the topological sorting
set @iter_no = 1
while 1 = 1
begin
	update #t1 set rank=@iter_no where degree = 0
	-- end the loop if no more rows left to process
	if (@@rowcount = 0) break
	update #t1 set degree = NULL where rank = @iter_no

	update #t1 set degree = (
		select count(*) from #t1 t
		where t.relative_id = #t1.object_id and t.object_id != t.relative_id
		and t.object_id in (select tt.object_id from #t1 tt where tt.rank = 0))
		where degree is not null

	set @iter_no = @iter_no + 1
end

--correcting naming mistakes of objects present in current database 
--This part need to be removed once SMO's URN comparision gets fixed
		DECLARE @collation sysname;
		DECLARE db_cursor CURSOR
		FOR
			select distinct ISNULL(object_db, db_name()) from #t1 as t
			where t.object_id IS NOT NULL and t.object_svr IS NULL
		OPEN db_cursor
		FETCH NEXT FROM db_cursor INTO @dbname
		WHILE (@@FETCH_STATUS <> -1)
		BEGIN
			IF (db_id(@dbname) IS NULL) 
			BEGIN
				FETCH NEXT FROM db_cursor INTO @dbname
				CONTINUE
			END
			
			---SET @collation = (select convert(sysname,DatabasePropertyEx(@dbname,'Collation')));
			SET @query = 'update #t1 set #t1.object_name = o.name,#t1.object_schema = sch.name from #t1  inner join '+ quotename(@dbname)+ '.sys.objects as o on #t1.object_id = o.object_id inner join '+ quotename(@dbname)+ '.sys.schemas as sch on sch.schema_id = o.schema_id  where o.name = #t1.object_name collate '+  @collation +' and sch.name = #t1.object_schema collate '+ @collation
			EXEC (@query)	


			FETCH NEXT FROM db_cursor INTO @dbname
		END
		CLOSE db_cursor
		DEALLOCATE db_cursor
	
--final select
select ISNULL(t.object_id, 0) as [object_id], t.object_name, ISNULL(t.object_schema, '') as [object_schema], ISNULL(t.object_db, '') as [object_db], ISNULL(t.object_svr, '') as [object_svr], t.object_type, ISNULL(t.relative_id, 0) as [relative_id], t.relative_name, ISNULL(t.relative_schema, '') as [relative_schema], relative_db, ISNULL(t.relative_svr, '') as [relative_svr], t.relative_type, t.schema_bound, ISNULL(CASE WHEN p.type= 'U' then @u when p.type = 'V' then @v end, 0) as [ptype], ISNULL(p.name, '') as [pname], ISNULL(SCHEMA_NAME(p.schema_id), '') as [pschema]
 from #t1 as t
 left join sys.objects as o on (t.object_type = @tr and o.object_id = t.object_id) or (t.relative_type = @tr and o.object_id = t.relative_id)
 left join sys.objects as p on p.object_id = o.parent_object_id
 order by rank desc
 
drop table #t1
drop table #t2
drop table #tempdep

IF @must_set_nocount_off > 0 
   set nocount off



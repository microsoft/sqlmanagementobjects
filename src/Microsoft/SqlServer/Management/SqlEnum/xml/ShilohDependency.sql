-- parameters:
-- 1. create table #tempdep (objid int NOT NULL, objtype smallint NOT NULL)
-- 	contains source objects
-- 2. @find_referencing_objects defines ordering
-- 	1 order for drop
-- 	0 order for script

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
declare @uddt int

set @u = 3
set @udf = 0
set @v = 2
set @sp = 4
set @def = 6
set @rule = 7
set @tr = 8
--above 100 -> not in dbo.sysobjects
set @uddt = 101


/*
 * Create #t1 as temp object holding areas.  Columns are:
 *	 object_id		- temp object id
 *	 object_type	 - temp object type
 *	 relative_id		- parent or child object id
 *	 relative_type	 - parent or child object type
 *	 rank	 - NULL means dependencies not yet evaluated, else nonNULL.
 *   soft_link - this row should not be used to compute ordering among objects
 *   object_name - name of the temp object
 *   object_schema - name the temp object's schema (if any)
 *   relative_name - name of the relative object
 *   relative_schema - name of the relative object's schema (if any)
 *   degree - the number of relatives that the object has, will be used for computing the rank
 */
create table #t1(
	object_id			int			NULL,
	object_type			smallint		NULL,
	relative_id			int			NULL,
	relative_type			smallint		NULL,
	rank			smallint		NULL,
	soft_link		bit		NULL,
	object_name			sysname		NULL,
	object_schema			sysname		NULL,
	relative_name		sysname		NULL,
	relative_schema		sysname		NULL,
	degree				int NULL
)

create unique clustered index i1 on #t1(object_id, object_type, relative_id, relative_type) with IGNORE_DUP_KEY

declare @iter_no int
set @iter_no = 1

declare @rows int
set @rows = 1

declare @rowcount_ck int
set @rowcount_ck = 0

insert #t1 (relative_id, relative_type, rank) 
	select l.objid, l.objtype, @iter_no from #tempdep l

while @rows > 0
begin
	set @rows = 0
	if( 1 = @find_referencing_objects )
	begin
		--tables that reference types ( parameters that reference types are in sql_dependencies )
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, c.id, case when o.type  in ('U') then @u 
										when o.type in ( 'P', 'RF', 'PC' ) then @sp 
										when o.type in ( 'TF', 'FN', 'IF' ) then @udf end, @iter_no + 1
			from #t1 as t
			join dbo.syscolumns as c on  c.xusertype = t.relative_id
			join dbo.sysobjects as o on o.id = c.id and o.type in ( 'U', 'P', 'RF', 'PC', 'TF', 'FN', 'IF')
			where @iter_no = t.rank and t.relative_type=@uddt
		set @rows = @rows + @@rowcount

		--tables that reference defaults ( only default objects )
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, clmns.id, @u, @iter_no + 1
			from #t1 as t
			join dbo.syscolumns as clmns on clmns.cdefault = t.relative_id
			join dbo.sysobjects as o on o.id = t.relative_id and (o.category & 0x0800) = 0
			where @iter_no = t.rank and t.relative_type = @def
		set @rows = @rows + @@rowcount

		--types that reference defaults ( only default objects )
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, tp.xusertype, @uddt, @iter_no + 1
			from #t1 as t
			join dbo.systypes as tp on tp.tdefault = t.relative_id
			join dbo.sysobjects as o on o.id = t.relative_id and (o.category & 0x0800) = 0
			where @iter_no = t.rank and t.relative_type = @def
		set @rows = @rows + @@rowcount

		--tables that reference rules
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, clmns.id, @u, @iter_no + 1
			from #t1 as t
			join dbo.syscolumns as clmns on clmns.domain = t.relative_id
			where @iter_no = t.rank and t.relative_type = @rule
		set @rows = @rows + @@rowcount

		--types that reference rules
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, tp.xusertype, @uddt, @iter_no + 1
			from #t1 as t
			join dbo.systypes as tp on tp.domain = t.relative_id
			where @iter_no = t.rank and t.relative_type = @rule
		set @rows = @rows + @@rowcount

		--table references table
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, fk.fkeyid, @u, @iter_no + 1
			from #t1 as t
			join dbo.sysreferences as fk on fk.rkeyid = t.relative_id
			where @iter_no = t.rank and t.relative_type = @u
		set @rows = @rows + @@rowcount

		--view, procedure references table, view, procedure
		--table(check) references procedure
		--trigger references table, procedure
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, case when 'C' = o.type then o.parent_obj else dp.id end, 
					case when o.type  in ('U', 'C') then @u when 'V' = o.type then @v when 'TR' = o.type then @tr 
					when o.type in ( 'P', 'RF', 'PC' ) then @sp 
					when o.type in ( 'TF', 'FN', 'IF' ) then @udf
					end, @iter_no + 1
			from #t1 as t
			join dbo.sysdepends as dp on dp.depid = t.relative_id and t.relative_type in ( @u, @v, @sp, @udf)
			join dbo.sysobjects as o on o.id = dp.id and o.type in ( 'U', 'V', 'P', 'RF', 'PC', 'TR', 'TF', 'FN', 'IF', 'C')
			where @iter_no = t.rank
		set @rows = @rows + @@rowcount

	end -- 1 = @find_referencing_objects
	else
	begin -- find referenced objects
		--check references table
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, dp.id, 77 /*place holder for check*/, @iter_no
			from #t1 as t
			join dbo.sysdepends as dp on dp.depid = t.relative_id and t.relative_type in (@u, @udf)
			join dbo.sysobjects as obj on obj.id = dp.id and obj.type  = 'C'
			where @iter_no = t.rank
		set @rowcount_ck = @@rowcount

		--view, procedure referenced by table, view, procedure
		--type referenced by procedure
		--check referenced by table
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select case when 77 = t.relative_type then obj2.parent_obj else t.relative_id end, 
				case when 77 = t.relative_type then @u else relative_type end, 
				dp.depid, case when 'U' = obj.type then @u 
					when 'V' = obj.type then @v 
					when 'TR' = obj.type then @tr 
					when obj.type in ( 'P', 'RF', 'PC' ) then @sp 
					when obj.type in ( 'TF', 'FN', 'IF' ) then @udf
					end, @iter_no + 1
			from #t1 as t
			join dbo.sysdepends as dp on dp.id = t.relative_id and t.relative_type in ( @u, @v, @sp, @udf, @tr, 77)
			join dbo.sysobjects as obj on obj.id = dp.depid and obj.type in ( 'U', 'V', 'P', 'RF', 'PC', 'TF', 'FN', 'IF', 'TR')
			left join dbo.sysobjects as obj2 on obj2.id = t.relative_id and 77 = t.relative_type
			where @iter_no = t.rank
		set @rows = @rows + @@rowcount

		if @rowcount_ck > 0 
		begin
			delete from #t1 where relative_type = 77
		end
		
		--table or view referenced by trigger
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, tr.parent_obj, case o.type when 'V' then @v else @u end, @iter_no + 1
			from #t1 as t
			join dbo.sysobjects as tr on tr.id = t.relative_id
		   join dbo.sysobjects as o on o.id = tr.parent_obj
			where @iter_no = t.rank and t.relative_type = @tr
		set @rows = @rows + @@rowcount
	
		--table referenced by table
		insert #t1 (object_id, object_type, relative_id, relative_type, rank)
			select t.relative_id, t.relative_type, fk.rkeyid, @u, @iter_no + 1
			from #t1 as t
			join dbo.sysreferences as fk on fk.fkeyid = t.relative_id
			where @iter_no = t.rank and t.relative_type = @u
		set @rows = @rows + @@rowcount

	end
	set @iter_no = @iter_no + 1
end --main loop

--objects that don't need to be in the loop because they don't reference anybody
if( 0 = @find_referencing_objects )
begin
	--alias types referenced by tables 
	insert #t1 (object_id, object_type, relative_id, relative_type, rank)
		select t.relative_id, t.relative_type, c.xusertype, @uddt, @iter_no + 1
		from #t1 as t
		join dbo.syscolumns as c on  c.id = t.relative_id
		join dbo.systypes as tp on tp.xusertype = c.xusertype and tp.xusertype > 256
		where t.relative_type in ( @u, @sp, @udf )

	if @@rowcount > 0 
	begin
		set @iter_no = @iter_no + 1
	end
	
	--defaults referenced by types
	insert #t1 (object_id, object_type, relative_id, relative_type, rank)
		select t.relative_id, t.relative_type, tp.tdefault, @def, @iter_no + 1
		from #t1 as t
		join dbo.systypes as tp on tp.xusertype = t.relative_id and tp.tdefault > 0
		join dbo.sysobjects as o on o.id = t.relative_id and (o.category & 0x0800) = 0
		where t.relative_type = @uddt

	--defaults referenced by tables( only default objects )
	insert #t1 (object_id, object_type, relative_id, relative_type, rank)
		select t.relative_id, t.relative_type, clmns.cdefault, @def, @iter_no + 1
		from #t1 as t
		join dbo.syscolumns as clmns on clmns.id = t.relative_id
		join dbo.sysobjects as o on o.id = clmns.cdefault and (o.category & 0x0800) = 0
		where t.relative_type = @u

	--rules referenced by types
	insert #t1 (object_id, object_type, relative_id, relative_type, rank)
		select t.relative_id, t.relative_type, tp.domain, @rule, @iter_no + 1
		from #t1 as t
		join dbo.systypes as tp on tp.xusertype = t.relative_id and tp.domain != 0
		where t.relative_type = @uddt

	--rules referenced by tables
	insert #t1 (object_id, object_type, relative_id, relative_type, rank)
		select t.relative_id, t.relative_type, clmns.domain, @rule, @iter_no + 1
		from #t1 as t
		join dbo.syscolumns as clmns on clmns.id = t.relative_id and clmns.domain != 0
		where t.relative_type = @u
end

--cleanup circular references
delete #t1 where object_id = relative_id and object_type=relative_type

--allow circular dependencies by cuting one of the branches
--mark as soft links dependencies between tables
-- at script time we will need to take care to script fks and checks separately
update #t1 set soft_link = 1 where ( object_type = @u and relative_type = @u )

--add independent objects first in the list
insert #t1 ( object_id, object_type, rank) 
	select t.relative_id, t.relative_type, 1 from #t1 t where t.relative_id not in ( select t2.object_id from #t1 t2 where not t2.object_id is null )

--delete initial objects
delete #t1 where object_id is null

update #t1 set rank = 0
-- computing the degree of the nodes
update #t1 set degree = (
		select count(*) 
		from #t1 t_alias 
		where t_alias.object_id = #t1.object_id and 
			t_alias.relative_id is not null and
			t_alias.soft_link is null)

-- perform topological sorting 
set @iter_no=1
while 1=1
begin 
	update #t1 set rank=@iter_no where degree=0
	-- end the loop if no more rows left to process
	if (@@rowcount=0) break
	update #t1 set degree=NULL where rank = @iter_no
	
	update #t1 set degree = (
		select count(*) 
			from #t1 t_alias 
			where t_alias.object_id = #t1.object_id and 
				t_alias.relative_id is not null and 
				t_alias.relative_id in (select t_alias2.object_id from #t1 t_alias2 where t_alias2.rank=0 and t_alias2.soft_link is null) and
				t_alias.rank=0 and t_alias.soft_link is null)
		where degree is not null
		
	set @iter_no=@iter_no+1
end

--add name schema
update #t1 set object_name = o.name, object_schema = user_name(o.uid)
from dbo.sysobjects AS o 
where o.id = object_id and object_type in ( @u, @udf, @v, @sp, @def, @rule)

update #t1 set object_name = o.name, relative_type = case op.type when 'V' then @v else @u end, object_schema = user_name(o.uid), relative_name = op.name, relative_schema = user_name(op.uid)
from dbo.sysobjects AS o 
join dbo.sysobjects AS op on op.id = o.parent_obj
where o.id = object_id and object_type = @tr

update #t1 set object_name = t.name, object_schema = user_name(t.uid)
from dbo.systypes AS t
where t.xusertype = object_id and object_type = @uddt

-- delete objects for which we could not resolve the table name or schema
-- because we may not have enough privileges
delete from #t1 where object_name is null or object_schema is null


--final select
select object_id, object_type, relative_id, relative_type, object_name, object_schema, relative_name, relative_schema
 from #t1 
 order by rank, relative_id

drop table #t1 
drop table #tempdep
 
IF @must_set_nocount_off > 0 
   set nocount off

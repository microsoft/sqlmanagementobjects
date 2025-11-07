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
set @pg = 204


/*
 * Create #t1 as temp object holding areas.  Columns are:
 *  object_id     - temp object id
 *  object_type    - temp object type
 *  relative_id      - parent or child object id
 *  relative_type  - parent or child object type
 *  rank  - NULL means dependencies not yet evaluated, else nonNULL.
 *   soft_link - this row should not be used to compute ordering among objects
 *   object_name - name of the temp object
 *   object_schema - name the temp object's schema (if any)
 *   relative_name - name of the relative object
 *   relative_schema - name of the relative object's schema (if any)
 *   degree - the number of relatives that the object has, will be used for computing the rank
 *   object_key - surrogate key that combines object_id and object_type
 *   relative_key - surrogate key that combines relative_id and relative_type
 */
create table #t1(
   object_id         int         NULL,
   object_type       smallint    NULL,
   relative_id       int         NULL,
   relative_type        smallint    NULL,
   rank        smallint    NULL,
   soft_link      bit      NULL,
   object_name       sysname     NULL,
   object_schema        sysname     NULL,
   relative_name     sysname     NULL,
   relative_schema      sysname     NULL,
   degree            int NULL,
   object_key bigint NULL,
   relative_key bigint NULL
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
      --tables that reference uddts or udts (parameters that reference types are in sql_dependencies )
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, c.object_id, @u, @iter_no + 1
         from #t1 as t
         join sys.columns as c on  c.user_type_id = t.relative_id
         join sys.tables as tbl on tbl.object_id = c.object_id -- eliminate views
         where @iter_no = t.rank and (t.relative_type=@uddt OR t.relative_type=@udt)
      set @rows = @rows + @@rowcount

      --tables that reference defaults ( only default objects )
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, clmns.object_id, @u, @iter_no + 1
         from #t1 as t
         join sys.columns as clmns on clmns.default_object_id = t.relative_id
         join sys.objects as o on o.object_id = t.relative_id and 0 = isnull(o.parent_object_id, 0)
         where @iter_no = t.rank and t.relative_type = @def
      set @rows = @rows + @@rowcount

      --types that reference defaults ( only default objects )
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, tp.user_type_id, @uddt, @iter_no + 1
         from #t1 as t
         join sys.types as tp on tp.default_object_id = t.relative_id
         join sys.objects as o on o.object_id = t.relative_id and 0 = isnull(o.parent_object_id, 0)
         where @iter_no = t.rank and t.relative_type = @def
      set @rows = @rows + @@rowcount

      --tables that reference rules
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, clmns.object_id, @u, @iter_no + 1
         from #t1 as t
         join sys.columns as clmns on clmns.rule_object_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @rule
      set @rows = @rows + @@rowcount

      --types that reference rules
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, tp.user_type_id, @uddt, @iter_no + 1
         from #t1 as t
         join sys.types as tp on tp.rule_object_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @rule
      set @rows = @rows + @@rowcount

      --tables that reference XmlSchemaCollections
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, c.object_id, @u, @iter_no + 1
         from #t1 as t
         join sys.columns as c on c.xml_collection_id = t.relative_id
         join sys.tables as tbl on tbl.object_id = c.object_id -- eliminate views
         where @iter_no = t.rank and t.relative_type = @xml
      set @rows = @rows + @@rowcount

      --procedures that reference XmlSchemaCollections
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, c.object_id, case when o.type in ( 'P', 'RF', 'PC' ) then @sp else @udf end, @iter_no + 1
         from #t1 as t
         join sys.parameters as c on c.xml_collection_id = t.relative_id
         join sys.objects as o on o.object_id = c.object_id
         where @iter_no = t.rank and t.relative_type = @xml
      set @rows = @rows + @@rowcount

      --udf, sp, uda, trigger all that reference assembly
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, am.object_id, (case o.type when 'AF' then @uda when 'PC' then @sp when 'FS' then @udf when 'FT' then @udf when 'TA' then @tr else @udf end), @iter_no + 1
         from #t1 as t
         join sys.assembly_modules as am on am.assembly_id = t.relative_id
         join sys.objects as o on am.object_id = o.object_id
         where @iter_no = t.rank and t.relative_type = @assm
      set @rows = @rows + @@rowcount

      -- CLR udf, sp, uda that reference udt
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select distinct t.relative_id, 
               t.relative_type, 
               am.object_id, 
               (case o.type 
                  when 'AF' then @uda 
                  when 'PC' then @sp 
                  when 'FS' then @udf 
                  when 'FT' then @udf 
                  when 'TA' then @tr 
                  else @udf end), 
               @iter_no + 1
         from #t1 as t
         join sys.parameters as sp on sp.user_type_id = t.relative_id
         join sys.assembly_modules as am on sp.object_id = am.object_id  
         join sys.objects as o on sp.object_id = o.object_id
         where @iter_no = t.rank and t.relative_type = @udt
      set @rows = @rows + @@rowcount

      --udt that reference assembly
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, at.user_type_id, @udt, @iter_no + 1
         from #t1 as t
         join sys.assembly_types as at on at.assembly_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @assm
      set @rows = @rows + @@rowcount

      --assembly that reference assembly
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, ar.assembly_id, @assm, @iter_no + 1
         from #t1 as t
         join sys.assembly_references as ar on ar.referenced_assembly_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @assm
      set @rows = @rows + @@rowcount

      --table references table
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, fk.parent_object_id, @u, @iter_no + 1
         from #t1 as t
         join sys.foreign_keys as fk on fk.referenced_object_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @u
      set @rows = @rows + @@rowcount

      --table,view references partition scheme
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)      
         select t.relative_id, t.relative_type, idx.object_id, case o.type when 'V' then @v else @u end, @iter_no + 1
         from #t1 as t
         join sys.indexes as idx on idx.data_space_id = t.relative_id 
         join sys.objects as o on o.object_id = idx.object_id
         where @iter_no = t.rank and t.relative_type = @part_sch
      set @rows = @rows + @@rowcount

      --partition scheme references partition function
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)      
         select t.relative_id, t.relative_type, ps.data_space_id, @part_sch, @iter_no + 1
         from #t1 as t
         join sys.partition_schemes as ps on ps.function_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @part_func
      set @rows = @rows + @@rowcount

      --non-schema-bound parameter references type
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, p.object_id, 
               case when obj.type in ( 'P', 'PC' ) then @sp else @udf
               end, @iter_no + 1
         from #t1 as t
         join sys.parameters as p on 
             p.user_type_id = t.relative_id and  t.relative_type in (@uddt, @udt)
         join sys.objects as obj on obj.object_id = p.object_id and obj.type in ( 'P',  'PC', 'TF', 'FN', 'IF', 'FS', 'FT')
         and ISNULL(objectproperty(obj.object_id, 'isschemabound'), 0) = 0
         where @iter_no = t.rank
      set @rows = @rows + @@rowcount

      -- plan guide references sp, udf, triggers
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)      
         select t.relative_id, t.relative_type, pg.plan_guide_id, @pg, @iter_no + 1
         from #t1 as t
         join sys.plan_guides as pg on pg.scope_object_id = t.relative_id
         where @iter_no = t.rank and t.relative_type in (@sp, @udf, @tr)
      set @rows = @rows + @@rowcount

      --view, procedure references table, view, procedure
      --procedure references type
      --table(check) references procedure
      --trigger references table, procedure
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, case when 'C' = obj.type then obj.parent_object_id else dp.object_id end, 
               case when obj.type  in ('U', 'C') then @u when 'V' = obj.type then @v when 'TR' = obj.type then @tr 
               when obj.type in ( 'P', 'RF', 'PC' ) then @sp 
               when obj.type in ( 'TF', 'FN', 'IF', 'FS', 'FT' ) then @udf
               end, @iter_no + 1
         from #t1 as t
         join sys.sql_dependencies as dp on 
            -- reference table, view procedure
            ( class < 2 and dp.referenced_major_id = t.relative_id and t.relative_type in ( @u, @v, @sp, @udf) )
            --reference type
             or ( 2 = class  and dp.referenced_major_id = t.relative_id and  t.relative_type in (@uddt, @udt))
            --reference xml namespace ( not supported by server right now )
            --or ( 3 = class  and dp.referenced_major_id = t.relative_id and @xml = t.relative_type )
         join sys.objects as obj on obj.object_id = dp.object_id and obj.type in ( 'U', 'V', 'P', 'RF', 'PC', 'TR', 'TF', 'FN', 'IF', 'FS', 'FT', 'C')
         where @iter_no = t.rank
      set @rows = @rows + @@rowcount

   end -- 1 = @find_referencing_objects
   else
   begin -- find referenced objects
      --check references table
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, dp.object_id, 77 /*place holder for check*/, @iter_no
         from #t1 as t
         join sys.sql_dependencies as dp on 
            -- reference table
            class < 2 and dp.referenced_major_id = t.relative_id and t.relative_type = @u
         join sys.objects as obj on obj.object_id = dp.object_id and obj.type  = 'C'
         where @iter_no = t.rank
      set @rowcount_ck = @@rowcount

      --non-schema-bound parameter references type
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select distinct 
            t.relative_id,	-- object_id
            t.relative_type,	-- object_type
            p.user_type_id,	-- relative_id
            case p.system_type_id when 240 then @udt else @uddt end,
            @iter_no + 1
         from #t1 as t
         join sys.parameters as p on
             p.object_id = t.relative_id and p.user_type_id > 256 and t.relative_type in ( @sp, @udf ,@uda )
             and ISNULL(objectproperty(p.object_id, 'isschemabound'), 0) = 0
         where @iter_no = t.rank
      set @rows = @rows + @@rowcount

      --view, procedure referenced by table, view, procedure
      --type referenced by procedure
      --check referenced by table
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select distinct 
            case when 77 = t.relative_type then obj2.parent_object_id else t.relative_id end,   -- object_id
            case when 77 = t.relative_type then @u else relative_type end,                -- object_type
            dp.referenced_major_id,                                              -- relative_id
            case                                                           -- relative_type
               when dp.class < 2 then 
                  case when 'U' = obj.type then @u 
                  when 'V' = obj.type then @v 
                  when 'TR' = obj.type then @tr 
				  when 'AF' = obj.type then @uda 
                  when obj.type in ( 'P', 'RF', 'PC' ) then @sp 
                  when obj.type in ( 'TF', 'FN', 'IF', 'FS', 'FT' ) then @udf
                  when exists (select * from sys.synonyms syn where syn.object_id = dp.referenced_major_id ) then @synonym
                  end
               when dp.class = 2 then (case 
                                    when exists (select * from sys.assembly_types sat where sat.user_type_id = dp.referenced_major_id) then @udt 
                                    else @uddt 
                                 end) 
            end, 
            @iter_no + 1
         from #t1 as t
         join sys.sql_dependencies as dp on 
            -- reference table, view procedure
            ( class < 2 and dp.object_id = t.relative_id and t.relative_type in ( @u, @v, @sp, @udf, @tr, @uda, 77) )
            --reference type
             or ( 2 = class  and dp.object_id = t.relative_id ) -- t.relative_type?
            --reference xml namespace ( not supported by server right now )
            --or ( 3 = class  and dp.referenced_major_id = t.relative_id and @xml = t.relative_type )
         left join sys.objects as obj on obj.object_id = dp.referenced_major_id and dp.class < 2 and obj.type in ( 'U', 'V', 'P', 'RF', 'PC', 'TF', 'FN', 'IF', 'FS', 'FT', 'TR', 'AF')
         left join sys.objects as obj2 on obj2.object_id = t.relative_id and 77 = t.relative_type
         where @iter_no = t.rank
      set @rows = @rows + @@rowcount

      if @rowcount_ck > 0 
      begin
         delete from #t1 where relative_type = 77
      end
      
      --table or view referenced by trigger
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, tr.parent_id, case o.type when 'V' then @v else @u end, @iter_no + 1
         from #t1 as t
         join sys.triggers as tr on tr.object_id = t.relative_id
		 join sys.objects as o on o.object_id = tr.parent_id
         where @iter_no = t.rank and t.relative_type = @tr
      set @rows = @rows + @@rowcount
      
      --table referenced by table
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, fk.referenced_object_id, @u, @iter_no + 1
         from #t1 as t
         join sys.foreign_keys as fk on fk.parent_object_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @u
      set @rows = @rows + @@rowcount

      --assembly referenced by assembly
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, ar.referenced_assembly_id, @assm, @iter_no + 1
         from #t1 as t
         join sys.assembly_references as ar on ar.assembly_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @assm
      set @rows = @rows + @@rowcount

      --assembly referenced by udt
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, at.assembly_id, @assm, @iter_no + 1
         from #t1 as t
         join sys.assembly_types as at on at.user_type_id = t.relative_id
         where @iter_no = t.rank and t.relative_type = @udt
      set @rows = @rows + @@rowcount

      -- assembly referenced by udf, sp, uda, trigger
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, am.assembly_id, @assm, @iter_no + 1
         from #t1 as t
         join sys.assembly_modules as am on am.object_id = t.relative_id
         where @iter_no = t.rank and t.relative_type in ( @udf, @sp, @uda, @tr)
      set @rows = @rows + @@rowcount

      -- udt referenced by CLR udf, sp, uda
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select distinct 
               t.relative_id, 
               t.relative_type, 
               at.user_type_id, 
               @udt, 
               @iter_no + 1
         from #t1 as t
         join sys.parameters as sp on sp.object_id = t.relative_id
         join sys.assembly_modules as am on am.object_id = sp.object_id
         join sys.assembly_types as at on sp.user_type_id = at.user_type_id
         where @iter_no = t.rank and t.relative_type in (@udf, @sp, @uda)
      set @rows = @rows + @@rowcount

      --clr types referenced by tables ( types referenced by parameters are in sql_dependencies )
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)
         select t.relative_id, t.relative_type, c.user_type_id, @udt, @iter_no + 1
         from #t1 as t
         join sys.columns as c on  c.object_id = t.relative_id
         join sys.assembly_types as tp on tp.user_type_id = c.user_type_id
         where @iter_no = t.rank and t.relative_type = @u
      set @rows = @rows + @@rowcount
      
      -- sp, udf, triggers referenced by plan guide
      insert #t1 (object_id, object_type, relative_id, relative_type, rank)      
         select t.relative_id, t.relative_type, pg.scope_object_id, (case o.type when 'P' then @sp when 'TR' then @tr else @udf end), @iter_no + 1
         from #t1 as t
         join sys.plan_guides as pg on pg.plan_guide_id = t.relative_id
	     join sys.objects as o on o.object_id = pg.scope_object_id
         where @iter_no = t.rank and t.relative_type = @pg
      set @rows = @rows + @@rowcount
         
   end
   set @iter_no = @iter_no + 1
end --main loop

--objects that don't need to be in the loop because they don't reference anybody
if( 0 = @find_referencing_objects )
begin
   --alias types referenced by tables ( types referenced by parameters are in sql_dependencies )
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, c.user_type_id, @uddt, @iter_no + 1
      from #t1 as t
      join sys.columns as c on  c.object_id = t.relative_id
      join sys.types as tp on tp.user_type_id = c.user_type_id and tp.is_user_defined = 1
      where t.relative_type = @u and tp.is_assembly_type = 0

   if @@rowcount > 0 
   begin
      set @iter_no = @iter_no + 1
   end
   
   --defaults referenced by types
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, tp.default_object_id, @def, @iter_no + 1
      from #t1 as t
      join sys.types as tp on tp.user_type_id = t.relative_id and tp.default_object_id > 0
      join sys.objects as o on o.object_id = tp.default_object_id and 0 = isnull(o.parent_object_id, 0)
      where t.relative_type = @uddt

   --defaults referenced by tables( only default objects )
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, clmns.default_object_id, @def, @iter_no + 1
      from #t1 as t
      join sys.columns as clmns on clmns.object_id = t.relative_id
      join sys.objects as o on o.object_id = clmns.default_object_id and 0 = isnull(o.parent_object_id, 0)
      where t.relative_type = @u

   --rules referenced by types
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, tp.rule_object_id, @rule, @iter_no + 1
      from #t1 as t
      join sys.types as tp on tp.user_type_id = t.relative_id and tp.rule_object_id > 0
      where t.relative_type = @uddt

   --rules referenced by tables
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, clmns.rule_object_id, @rule, @iter_no + 1
      from #t1 as t
      join sys.columns as clmns on clmns.object_id = t.relative_id and clmns.rule_object_id > 0
      where t.relative_type = @u

   --XmlSchemaCollections referenced by table
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, c.xml_collection_id, @xml, @iter_no + 1
      from #t1 as t
      join sys.columns as c on c.object_id = t.relative_id and c.xml_collection_id > 0
      where t.relative_type = @u

   --XmlSchemaCollections referenced by procedures
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)
      select t.relative_id, t.relative_type, c.xml_collection_id, @xml, @iter_no + 1
      from #t1 as t
      join sys.parameters as c on c.object_id = t.relative_id and c.xml_collection_id > 0
      where t.relative_type in ( @sp, @udf)

   --partition scheme referenced by table,view
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)      
      select t.relative_id, t.relative_type, ps.data_space_id, @part_sch, @iter_no + 1
      from #t1 as t
      join sys.indexes as idx on idx.object_id = t.relative_id 
      join sys.partition_schemes as ps on ps.data_space_id = idx.data_space_id
      where t.relative_type in (@u, @v)

   --partition function referenced by partition scheme
   insert #t1 (object_id, object_type, relative_id, relative_type, rank)      
      select t.relative_id, t.relative_type, ps.function_id, @part_func, @iter_no + 1
      from #t1 as t
      join sys.partition_schemes as ps on ps.data_space_id = t.relative_id
      where t.relative_type = @part_sch
      
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

-- compute the surrogate keys to make sorting easier
update #t1 set object_key = object_id + convert(bigint, 0xfFFFFFFF) * object_type
update #t1 set relative_key = relative_id + convert(bigint, 0xfFFFFFFF) * relative_type

create index index_key on #t1 (object_key, relative_key)

update #t1 set rank = 0
-- computing the degree of the nodes
update #t1 set degree = (
      select count(*) 
      from #t1 t_alias 
      where t_alias.object_key = #t1.object_key and 
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
         where t_alias.object_key = #t1.object_key and 
            t_alias.relative_key is not null and 
            t_alias.relative_key in (select t_alias2.object_key from #t1 t_alias2 where t_alias2.rank=0 and t_alias2.soft_link is null) and
            t_alias.rank=0 and t_alias.soft_link is null)
      where degree is not null
      
   set @iter_no=@iter_no+1
end

--add name schema
update #t1 set object_name = o.name, object_schema = schema_name(o.schema_id)
from sys.objects AS o 
where o.object_id = #t1.object_id and object_type in ( @u, @udf, @v, @sp, @def, @rule, @uda)

update #t1 set relative_type = case op.type when 'V' then @v else @u end, object_name = o.name, object_schema = schema_name(o.schema_id), relative_name = op.name, relative_schema = schema_name(op.schema_id)
from sys.objects AS o 
join sys.objects AS op on op.object_id = o.parent_object_id
where o.object_id = #t1.object_id and object_type = @tr

update #t1 set object_name = t.name, object_schema = schema_name(t.schema_id)
from sys.types AS t
where t.user_type_id = #t1.object_id and object_type in ( @uddt, @udt )

update #t1 set object_name = x.name, object_schema = schema_name(x.schema_id)
from sys.xml_schema_collections AS x
where x.xml_collection_id = #t1.object_id and object_type = @xml

update #t1 set object_name = p.name, object_schema = null
from sys.partition_schemes AS p
where p.data_space_id = #t1.object_id and object_type = @part_sch


update #t1 set object_name = p.name, object_schema = null
from sys.partition_functions AS p
where p.function_id = #t1.object_id and object_type = @part_func

update #t1 set object_name = pg.name, object_schema = null
from sys.plan_guides AS pg
where pg.plan_guide_id = #t1.object_id and object_type = @pg

update #t1 set object_name = a.name, object_schema = null
from sys.assemblies AS a
where a.assembly_id = #t1.object_id and object_type = @assm

update #t1 set object_name = syn.name, object_schema = schema_name(syn.schema_id)
from sys.synonyms AS syn
where syn.object_id = #t1.object_id and object_type = @synonym

-- delete objects for which we could not resolve the table name or schema
-- because we may not have enough privileges
delete from #t1 
where 
	object_name is null or 
	(object_schema is null  and object_type not in (@assm, @part_func, @part_sch, @pg))

--final select
select object_id, object_type, relative_id, relative_type, object_name, object_schema, relative_name, relative_schema
 from #t1 
 order by rank, relative_id
 
drop table #t1
drop table #tempdep

IF @must_set_nocount_off > 0 
   set nocount off

// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Data;

    using System.Globalization;

    internal class PostProcessFile : PostProcess
    {
        private bool firstTime = true;
        private float usedSpace;
        private float availableSpace;

        public PostProcessFile()
        {
        }

        protected override bool SupportDataReader
        {
            get { return false; }
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if (this.firstTime)
            {
                this.ExecuteQuery(data, dp);
                this.firstTime = false;
            }

            switch (name)
            {
                case "UsedSpace":
                    return this.usedSpace;
                case "AvailableSpace":
                    return this.availableSpace;
            }

            return data;
        }        

        public override void CleanRowData()
        {
            this.firstTime = true;
        }

        private void ExecuteQuery(object data, DataProvider dp)
        {
            var useStmt = string.Format(CultureInfo.InvariantCulture, "use [{0}];", Util.EscapeString(this.GetTriggeredString(dp, 2), ']'));

            var query =                                                         
                !ExecuteSql.IsContainedAuthentication(this.ConnectionInfo)
                ?
@"select 
CAST(CASE s.type WHEN 2 THEN s.size * CONVERT(float,8) ELSE dfs.allocated_extent_page_count*convert(float,8) END AS float) AS [UsedSpace],
CASE s.type WHEN 2 THEN 0 ELSE <msparam>{0}</msparam> - dfs.allocated_extent_page_count*convert(float,8) END AS [AvailableSpace] 
from 
sys.filegroups AS g
inner join sys.database_files AS s on ((s.type = 2 or s.type = 0) and (s.drop_lsn IS NULL)) AND (s.data_space_id=g.data_space_id)
left outer join sys.dm_db_file_space_usage as dfs ON dfs.database_id = db_id() AND dfs.file_id = s.file_id
where 
s.name = <msparam>{1}</msparam> and g.data_space_id = <msparam>{2}</msparam>"
                :
@"create table #tmpspc (Fileid int, FileGroup int, TotalExtents int, UsedExtents int, Name sysname, FileName nchar(520))
insert #tmpspc EXEC ('dbcc showfilestats');

SELECT
CAST(CASE s.type WHEN 2 THEN s.size * CONVERT(float,8) ELSE tspc.UsedExtents*convert(float,64) END AS float) AS [UsedSpace],
CASE s.type WHEN 2 THEN 0 ELSE <msparam>{0}</msparam> - tspc.UsedExtents*convert(float,64) END AS [AvailableSpace]
FROM
sys.filegroups AS g
INNER JOIN sys.database_files AS s ON ((s.type = 2 or s.type = 0) and (s.drop_lsn IS NULL)) AND (s.data_space_id=g.data_space_id)
LEFT OUTER JOIN #tmpspc tspc ON tspc.Fileid = s.file_id
where 
s.name = <msparam>{1}</msparam> and g.data_space_id = <msparam>{2}</msparam>;

drop table #tmpspc;";

            DataTable dt = ExecuteSql.ExecuteWithResults(
                useStmt +
                string.Format(
                    CultureInfo.InvariantCulture, 
                    query, 
                    this.GetTriggeredObject(dp, 3),
                    this.GetTriggeredString(dp, 0), 
                    this.GetTriggeredInt32(dp, 1)), 
                 this.ConnectionInfo);

            this.usedSpace = Convert.ToSingle(dt.Rows[0]["UsedSpace"], CultureInfo.InvariantCulture);
            this.availableSpace = Convert.ToSingle(dt.Rows[0]["AvailableSpace"], CultureInfo.InvariantCulture);
        }
    }
}
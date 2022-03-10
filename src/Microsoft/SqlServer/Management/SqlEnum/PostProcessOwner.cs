// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System.Collections.Specialized;

    using System.Data;

    using System.Globalization;
    using Microsoft.SqlServer.Management.Common;

    internal class PostProcessOwner : PostProcess
    {
        string uSid;
        bool firstTime = true;
        string ownerName;
        PostProcessOwner()
        {
        }

        protected override bool SupportDataReader
        {
            get { return false; }
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {

            if (firstTime)
            {
                ownerName = ExecuteQuery(data, dp);
                firstTime = false;
            }

            switch (name)
            {
                case "Owner":
                    return ownerName;
            }
            return data;
        }

        private string ExecuteQuery(object data, DataProvider dp)
        {

            uSid = GetTriggeredString(dp, 0);

            // If user name cannot be found in sys.sql_logins, use database_principals in master instead.
            // If the user doesn't exist in database_principals, we just return the object id
            // The reason we want to query database_principals in master is that AAD admin doesn't exist in sql_logins as today and it exists in database_principals.
            //
            string query = string.Format(CultureInfo.InvariantCulture,
                @"DECLARE @sid varbinary(max) 
                DECLARE @name varchar(max)
                SET @sid = {0}
                set @name = CAST(@sid as UNIQUEIDENTIFIER)
                select @name = name from sys.database_principals WITH (NOLOCK) where sid=@sid
                IF @@ROWCOUNT = 0
                BEGIN
                  select @name = name from sys.sql_logins WITH (NOLOCK) where sid=@sid
                END
                select @name", uSid);
            StringCollection sc = new StringCollection();
            sc.Add(query);
            DataTable dt;
            try
            {
                dt = ExecuteSql.ExecuteWithResults(sc, this.ConnectionInfo, "master");
            }
            catch (ExecutionFailureException)
            {
                return string.Empty;
            }
            catch (ConnectionFailureException)
            {
                return string.Empty;
            }   

            if (dt.Rows.Count == 0)
            {
                return string.Empty;
            }

            return dt.Rows[0][0].ToString();
        }

        public override void CleanRowData()
        {
            firstTime = true;
        }

    }
}
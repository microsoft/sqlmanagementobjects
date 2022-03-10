// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Globalization;
    using Microsoft.SqlServer.Management.Common;

    internal class PostProcessUser : PostProcess
    {
        string uSid;
        bool firstTime = true;
        string str;
        PostProcessUser()
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
                str = ExecuteQuery(data, dp);
                firstTime = false;
            }

            switch (name)
            {
                case "Login":
                case "Owner":
                    return str;


                case "UserType":
                    if (String.IsNullOrEmpty(str))
                    {
                        return 3;
                    }
                    else
                    {
                        return 0;
                    }

            }
            return data;
        }

        private string ExecuteQuery(object data, DataProvider dp)
        {

            uSid = GetTriggeredString(dp, 0);

            if (string.IsNullOrEmpty(uSid) ||
                uSid.Equals("0x00", StringComparison.InvariantCultureIgnoreCase) ||
                uSid.Equals("0x01", StringComparison.InvariantCultureIgnoreCase))
            {
                //If the SID for the user is invalid (as is such with the sys and INFORMATION_SCHEMA users, they are null),
                //0x01 (dbo) or 0x00 (guest) then just immediately return an empty string since there's no point in making
                //the query - it'll either be invalid syntax or be pointless since nothing there will map to 0x00/0x01
                return string.Empty;
            }

			string query = string.Format(CultureInfo.InvariantCulture, "Select name from sys.sql_logins WITH (NOLOCK) where sid={0}", uSid);
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
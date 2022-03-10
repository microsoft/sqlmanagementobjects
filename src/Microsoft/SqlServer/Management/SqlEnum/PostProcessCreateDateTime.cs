// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Globalization;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.SqlEnum;
namespace Microsoft.SqlServer.Management.Smo
{
    internal class PostProcessCreateDateTime : PostProcess
    {
        protected object GetDateTime(object oDate, object oTime)
        {
            if (IsNull(oDate))
            {
                return System.DBNull.Value;
            }
            int date = (Int32)oDate;

            if (IsNull(oTime))
            {
                return System.DBNull.Value;
            }
            int time = (Int32)oTime;

            if (date <= 0 || time < 0)
            {
                return System.DBNull.Value;
            }

            return new DateTime(date / 10000, (date / 100) % 100, date % 100, time / 10000, (time / 100) % 100, time % 100);
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            return GetDateTime(GetTriggeredObject(dp, 0), GetTriggeredObject(dp, 1));
        }
    }

    internal class PostProcessCreateDate : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            int date = GetTriggeredInt32(dp, 0);

            if (date > 0)
            {
                data = new DateTime(date / 10000, (date / 100) % 100, date % 100);
            }
            return data;
        }
    }

    internal class PostProcessCreateTime : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            int time = GetTriggeredInt32(dp, 0);

            if (time > 0)
            {
                data = new TimeSpan(time / 10000, (time / 100) % 100, time % 100);
            }
            return data;
        }
    }

    internal class PostProcessCreateDateSeconds1990 : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            int seconds = 0;
            object objSeconds = GetTriggeredObject(dp, 0);

            if (!IsNull(objSeconds))
            {
                seconds = (int)objSeconds;
            }

            if (seconds <= 0)
            {
                return DateTime.MinValue;
            }

            DateTime dt = new DateTime(1990, 1, 1);
            TimeSpan ts = new TimeSpan(seconds / 3600, (seconds % 3600) / 60, seconds % 60);
            data = dt.Add(ts);
            return data;
        }
    }

    internal class PostProcessCreateTimeSpanHMS : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            int seconds = 0;
            object objSeconds = GetTriggeredObject(dp, 0);

            if (!IsNull(objSeconds))
            {
                seconds = (int)objSeconds;
            }

            if (seconds <= 0)
            {
                return data;
            }

            DateTime dt = new DateTime(1990, 1, 1);
            TimeSpan ts = new TimeSpan(seconds / 3600, (seconds % 3600) / 60, seconds % 60);

            DateTime now = (DateTime)GetTriggeredObject(dp, 1);
            data = now - dt.Add(ts);
            return data;
        }
    }

    internal class PostProcessPermissionCode : PostProcess
    {
        private int GetSmoCodeFromSqlCodeYukon(string sqlCode, ObjectClass objClass)
        {
            int smoCode;
            sqlCode = sqlCode.TrimEnd();

            if (!GetSmoCodeFromSqlCode(sqlCode, out smoCode))
            {
                if ((ObjectClass.Server != objClass)
                && (ObjectClass.Database != objClass)) //ServerPermissions and DatabasePermissions should not be treated as ObjectPermissions.
                {
                    smoCode = (int)PermissionDecode.ToPermissionSetValueEnum<ObjectPermissionSetValue>(sqlCode);
                }
                else
                {
                    smoCode = -1;
                }
            }

            return smoCode;
        }

        private int GetSmoCodeFromSqlCodeShiloh(string sqlCode)
        {
            int smoCode;
            sqlCode = sqlCode.TrimEnd();

            if (!GetSmoCodeFromSqlCode(sqlCode, out smoCode))
            {
                smoCode = (int)PermissionDecode.ToPermissionSetValueEnum<ObjectPermissionSetValue>(sqlCode);
            }

            return smoCode;
        }

        /// <summary>
        /// Gets server or database permission code
        /// </summary>
        /// <param name="sqlCode"></param>
        /// <param name="smoCode"></param>
        /// <returns></returns>
        private bool GetSmoCodeFromSqlCode(string sqlCode, out int smoCode)
        {
            bool result = false;
            smoCode = -1;

            if ("Permission" == this.ObjectName)
            {
                string strParentObjName = this.Request.Urn.Parent.Type;
                if ("Server" == strParentObjName)
                {
                    result = true;
                    smoCode = (int)PermissionDecode.ToPermissionSetValueEnum<ServerPermissionSetValue>(sqlCode);
                }
                if ("Database" == strParentObjName)
                {
                    result = true;
                    smoCode = (int)PermissionDecode.ToPermissionSetValueEnum<DatabasePermissionSetValue>(sqlCode);
                }
            }
            return result;
        }

        string ShilohToYukonPermission(int permType)
        {
            switch (permType)
            {
                case 26: return "RF";       //References
                case 178: return "CRFN";     //Create Function
                case 193: return "SL";       //Select
                case 195: return "IN";       //Insert
                case 196: return "DL";       //Delete
                case 197: return "UP";       //Update
                case 198: return "CRTB";     //Create Table
                case 203: return "CRDB";     //Create Database
                case 207: return "CRVW";     //Create View
                case 222: return "CRPR";     //Create Procedure
                case 224: return "EX";       //Execute
                case 228: return "BADB";     //Backup Database
                case 233: return "CRDF";     //Create Default
                case 235: return "BALO";     //Backup Transaction ( LOG )
                case 236: return "CRRU";     //Create Rule       
            }
            return "";
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if (ExecuteSql.GetServerVersion(this.ConnectionInfo).Major < 9)
            {
                return GetSmoCodeFromSqlCodeShiloh(ShilohToYukonPermission(GetTriggeredInt32(dp, 0)));
            }
            return GetSmoCodeFromSqlCodeYukon(GetTriggeredString(dp, 0), (ObjectClass)GetTriggeredInt32(dp, 1));
        }
    }

    internal class PostProcessOwnObjects : PostProcess
    {
        string GetDatabaseLevel(DataProvider dp)
        {
            return String.Format(CultureInfo.InvariantCulture, "Server[@Name='{0}']/Database[@Name='{1}']",
                    Util.EscapeString(GetTriggeredString(dp, 1), '\''), Util.EscapeString(GetTriggeredString(dp, 2), '\''));
        }

        string GetUrn(DataProvider dp, string type, bool bWithSchema, string tentativeParent)
        {
            String urn = GetDatabaseLevel(dp);
            if (null != tentativeParent && !IsNull(dp, 5))
            {
                urn += String.Format(CultureInfo.InvariantCulture, "/{0}[@Name='{1}' and @Schema='{2}']",
                    tentativeParent, Util.EscapeString(GetTriggeredString(dp, 5), '\''), Util.EscapeString(GetTriggeredString(dp, 6), '\''));
            }
            if (!bWithSchema)
            {
                urn += String.Format(CultureInfo.InvariantCulture, "/{0}[@Name='{1}']", type, Util.EscapeString(GetTriggeredString(dp, 3), '\''));
            }
            else
            {
                urn += String.Format(CultureInfo.InvariantCulture, "/{0}[@Name='{1}' and @Schema='{2}']",
                    type, Util.EscapeString(GetTriggeredString(dp, 3), '\''), Util.EscapeString(GetTriggeredString(dp, 4), '\''));
            }
            return urn;
        }

        string GetUrn(DataProvider dp)
        {
            switch (GetTriggeredString(dp, 0))
            {
                //special cases
                case "ASSEMBLY": return GetUrn(dp, "SqlAssembly", false, null);
                case "SCHEMA": return GetUrn(dp, "Schema", false, null);
                case "UDDT": return GetUrn(dp, "UserDefinedDataType", true, null);
                case "UDT": return GetUrn(dp, "UserDefinedType", true, null);
                case "XMLSCHCOL": return GetUrn(dp, "XmlSchemaCollection", true, null);
                //sys.objects type column values
                case "AF": return GetUrn(dp, "UserDefinedAggregate", true, null);
                case "C ": return GetUrn(dp, "Check", false, "Table");
                case "D ": return IsNull(dp, 5) ? GetUrn(dp, "Default", true, null) : GetUrn(dp, "Default", false, "Table");
                case "F ": return GetUrn(dp, "ForeignKey", false, "Table");
                case "PK": return GetUrn(dp, "Index", false, "Table");
                case "P ": return GetUrn(dp, "StoredProcedure", true, null);
                case "PC": goto case "P ";
                case "FN": return GetUrn(dp, "UserDefinedFunction", true, null);
                case "FS": goto case "FN";
                case "FT": goto case "FN";
                case "R ": return GetUrn(dp, "Rule", true, null);
                case "RF": goto case "P ";
                case "SN": return GetUrn(dp, "Synonym", true, null);
                case "SO": return GetUrn(dp, "Sequence", true, null);
                case "SQ": return GetUrn(dp, "ServiceBroker/ServiceQueue", true, null);
                case "TA": return GetUrn(dp, "Trigger", true, "Table");
                case "TR": goto case "TA";
                case "IF": goto case "FN";
                case "TF": goto case "FN";
                case "S ": goto case "U ";
                case "U ": return GetUrn(dp, "Table", true, null);
                case "UQ": return GetUrn(dp, "Index", false, "Table");
                case "V ": return GetUrn(dp, "View", true, null);
                case "X ": return GetUrn(dp, "ExtendedStoredProcedure", true, null);
            }
            throw new InternalEnumeratorException(StringSqlEnumerator.FailedToCreateUrn(GetTriggeredString(dp, 0)));
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            return GetUrn(dp);
        }
    }

    internal class PostProcessSplitFourPartName : PostProcess
    {
        StringCollection m_listNames = null;

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if (m_listNames == null)
            {
                m_listNames = Util.SplitNames(this.GetTriggeredString(dp, 0), '.');
            }

            int pos = 0; //BaseObject

            switch (name)
            {
                case "BaseSchema": pos = 1; break;
                case "BaseDatabase": pos = 2; break;
                case "BaseServer": pos = 3; break;
            }

            if (pos >= m_listNames.Count)
            {
                return data;
            }

            return m_listNames[pos];
        }

        public override void CleanRowData()
        {
            m_listNames = null;
        }
    }

    internal class PostProcessFragmentation : PostProcess
    {
        bool calledUseDB;
        bool isInitDataRow;
        string[] contigData;

        internal PostProcessFragmentation()
        {
            calledUseDB = true;
            isInitDataRow = false;
            contigData = null;
        }

        string GetSql(DataProvider dp)
        {
            int indexId = GetTriggeredInt32(dp, 0);
            int tableId = GetTriggeredInt32(dp, 1);
            string database = GetTriggeredString(dp, 2);

            if (calledUseDB)
            {
                return String.Format(CultureInfo.InvariantCulture, "USE [{2}] DBCC SHOWCONTIG({0}, {1})", tableId, indexId, Util.EscapeString(database, ']'));
            }
            else
            {
                calledUseDB = false;
                return String.Format(CultureInfo.InvariantCulture, "DBCC SHOWCONTIG({0}, {1})", tableId, indexId);
            }
        }

        void InitRowData(DataProvider dp)
        {
            if (isInitDataRow)
            {
                return;
            }

            /*
            7942* DBCC SHOWCONTIG scanning 'authors' table...
            7943* Table: 'authors' (117575457); index ID: 2, database ID: 5
            7944* LEAF level scan performed.
            7945* - Pages Scanned................................: 1
            7946* - Extents Scanned..............................: 1
            7947* - Extent Switches..............................: 0
            7948* - Avg. Pages per Extent........................: 1.0
            7949* - Scan Density [Best Count:Actual Count].......: 100.00% [1:1]
            7950* - Logical Scan Fragmentation ..................: 0.00%
            7952* - Extent Scan Fragmentation ...................: 0.00%
            7953* - Avg. Bytes Free per Page.....................: 7208.0
            7954* - Avg. Page Density (full).....................: 10.95%
            */

            isInitDataRow = true;
            contigData = new string[9];

            ArrayList sqlExecMessages = ExecuteSql.ExecuteImmediateGetMessage(GetSql(dp), this.ConnectionInfo);

            foreach (SqlInfoMessageEventArgs sm in sqlExecMessages)
            {
                int colIndex = 0;

                for (int c = 0; c < sm.Errors.Count && colIndex < contigData.Length; c++)
                {
                    //Find the first line in the info stream
                    if (sm.Errors[c].Number == 7945)
                    {
                        do
                        {
                            SqlError e = sm.Errors[c];
                            int firstIdx = e.Message.LastIndexOf("..:", e.Message.Length - 1, StringComparison.Ordinal);
                            if (firstIdx < 0)
                            {
                                break;
                            }

                            firstIdx += 3;
                            int lastIdx = e.Message.LastIndexOf('%');

                            if (lastIdx <= firstIdx)
                            {
                                lastIdx = e.Message.Length;
                            }

                            contigData[colIndex++] = e.Message.Substring(firstIdx, lastIdx - firstIdx);
                            c++;
                        }
                        while (c < sm.Errors.Count && colIndex < contigData.Length);
                    }
                }
                //Data found so stop processing
                if (colIndex >= contigData.Length)
                {
                    return;
                }
            }

            //All data not found so null the array
            contigData = null;
        }


        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            InitRowData(dp);

            if (contigData != null)
            {
                switch (name)
                {
                    case "Pages":
                        return Int64.Parse(contigData[0], CultureInfo.InvariantCulture);
                    case "Extents":
                        return Int32.Parse(contigData[1], CultureInfo.InvariantCulture);
                    case "ExtentSwitches":
                        return Int32.Parse(contigData[2], CultureInfo.InvariantCulture);
                    case "ScanDensity":
                        return Double.Parse(contigData[4], CultureInfo.InvariantCulture);
                    case "LogicalFragmentation":
                        return Double.Parse(contigData[5], CultureInfo.InvariantCulture);
                    case "ExtentFragmentation":
                        return Double.Parse(contigData[6], CultureInfo.InvariantCulture);
                    case "AverageFreeBytes":
                        return Double.Parse(contigData[7], CultureInfo.InvariantCulture);
                    case "AveragePageDensity":
                        return Double.Parse(contigData[8], CultureInfo.InvariantCulture);
                }
            }

            return null;
        }

        public override void CleanRowData()
        {
            contigData = null;
            isInitDataRow = false;
        }
    }

    internal class PostProcessIPAddress : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if (IsNull(data))
            {
                return System.Net.IPAddress.None;
            }

            string ipaddress = (string)data;

            long newAddress = 0;
            string[] bits = ipaddress.Split(new char[] { '.' });
            // if the address did not come as x.x.x.x we return null
            if (bits.Length != 4)
            {
                return System.Net.IPAddress.None;
            }
            int byteindex = 0;
            // extract the bytes from the string array
            foreach (string s in bits)
            {
                long intval = (long)Int32.Parse(s, CultureInfo.InvariantCulture);
                newAddress += intval * (1 << ((byteindex++) * 8));
            }

            return new IPAddress(newAddress);
        }
    }

    internal class PostProcessIP6Address : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            if (IsNull(data))
            {
                return data;
            }

            string ipaddress = (string)data;

            return IPAddress.Parse((string)data);
        }
    }

    internal class PostProcessJobActivity : PostProcessCreateDateTime
    {
        DataTable dt = null;

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            DataRow row = null;
            Guid job_id = new Guid(GetTriggeredString(dp, 0));
            if (dt == null)
            {
                // If the Job query that was executed only returned one row, then we only call
                // sp_help_job for that one row. Otherwise we assume that the query was for
                // all jobs (such as expanding the Jobs folder in the Agent node in Object
                // Explorer). In that case we get a DataTable for all jobs.
                if (1 == dp.TableRowCount)
                {
                    dt = ExecuteSql.ExecuteWithResults(String.Format("exec msdb.dbo.sp_help_job @job_id='{0}'", job_id), this.ConnectionInfo);
                }
                else
                {
                    dt = ExecuteSql.ExecuteWithResults("exec msdb.dbo.sp_help_job", this.ConnectionInfo);
                }
            }

            //
            // find the row with our job_id, 
            // it's in the first column
            //
            foreach (DataRow r in dt.Rows)
            {
                if ((System.Guid)r[0] == job_id)
                {
                    row = r;
                    break;
                }
            }

            if (row == null)
            {
                return DBNull.Value;
            }

            switch (name)
            {
                case "CurrentRunRetryAttempt": return row["current_retry_attempt"];
                case "CurrentRunStatus": return row["current_execution_status"];
                case "CurrentRunStep": return row["current_execution_step"];
                case "HasSchedule": return ((int)row["has_schedule"]) != 0 ? true : false;
                case "HasServer": return ((int)row["has_target"]) != 0 ? true : false;
                case "HasStep": return ((int)row["has_step"]) != 0 ? true : false;
                case "LastRunDate": return GetDateTime(row["last_run_date"], row["last_run_time"]);
                case "LastRunOutcome": return row["last_run_outcome"];
                case "NextRunDate": return GetDateTime(row["next_run_date"], row["next_run_time"]);
                case "NextRunScheduleID": return row["next_run_schedule_id"];
                case "JobType": return row["type"];
            }
            return data;
        }
    }

    internal class PostProcessStatisticStream : PostProcessCreateDateTime
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            string statistic = GetTriggeredString(dp, 0);
            string table = GetTriggeredString(dp, 1);

            DataTable dt = ExecuteSql.ExecuteWithResults("DBCC SHOW_STATISTICS(" + Util.MakeSqlString(table) + ", " +
                            Util.MakeSqlString(statistic) + ") WITH STATS_STREAM", this.ConnectionInfo);

            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["Stats_Stream"];
            }
            return data;
        }
    }

    internal class PostProcessCreateSqlSecureString : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            object objStr = GetTriggeredObject(dp, 0);

            Microsoft.SqlServer.Management.Smo.Internal.SqlSecureString ss = new Microsoft.SqlServer.Management.Smo.Internal.SqlSecureString(objStr.ToString());

            return ss;
        }
    }
}

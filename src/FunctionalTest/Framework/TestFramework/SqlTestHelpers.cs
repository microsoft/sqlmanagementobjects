// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Reflection;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// General helper method/properties for use with the SQL Manageability Test Framework
    /// </summary>
    public static class SqlTestHelpers
    {
        /// <summary>
        /// Helper function to test the value of a specified property
        /// </summary>
        /// <param name="obj">The object containing the property to be tested</param>
        /// <param name="propertyName">The name of the property to test</param>
        /// <param name="expectedValue">The expected value of the property</param>
        /// <returns></returns>
        public static SqlTestResult TestReadProperty(object obj, string propertyName, object expectedValue)
        {
            var result = new SqlTestResult();

            if (obj == null)
            {
                result.AddFailures(
                    string.Format("Object is null! Property Name = '{0}' ExpectedValue = '{1}'",
                        propertyName,
                        expectedValue));
                return result;
            }

            PropertyInfo pi = obj.GetType().GetProperty(propertyName);
            if (pi == null)
            {
                result.AddFailures(
                    string.Format("Object of type '{0}' did not have expected property '{1}'",
                        obj.GetType(),
                        propertyName));
                return result;
            }

            object actualValue = pi.GetValue(obj, null);

            //One or both may be null so we use object.Equals
            if (object.Equals(actualValue, expectedValue) == false)
            {
                result.AddFailures(
                    string.Format(
                        "Property {0} was expected to be '{1}' (type '{2}') but actual value was '{3}' (type '{4}')",
                        propertyName,
                        expectedValue ?? "null",
                        expectedValue == null ? "null" : expectedValue.GetType().ToString(),
                        actualValue ?? "null",
                        actualValue == null ? "null" : actualValue.GetType().ToString()));
            }
            return result;

        }

        public static void CleanupOldDbs(SqlConnection sqlConnection)
        {
            bool wasOpen = sqlConnection.State == ConnectionState.Open;
            if (!wasOpen)
            {
                sqlConnection.Open();
            }
            try
            {

                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandTimeout = 60;
                    sqlCommand.CommandText = @"declare @dbname sysname
declare @createDate datetime
select @createDate = getdate()

declare dbc cursor for
	select dbs.name 
	from 
		sys.databases dbs
	where
    	dbs.database_id > 4
	    and dbs.name not like N'keep%'
        and dbs.name not like N'Keep%'
		and dbs.name not like N'LanguageSemantics'
		and dbs.name not in ('DWConfiguration', 'DWDiagnostics', 'DWQueue') --Required for Polybase
		--and dbs.is_read_only = 0
		--and dbs.user_access = 0
		and dbs.create_date < dateadd(hh, -1, @createDate)
		

open dbc
fetch next from dbc
	into @dbname
	
while @@FETCH_STATUS = 0
begin

	begin try
		-- delete the db
		print @dbname
		declare @esql nvarchar(512)
		select @esql = N'drop database ' + QUOTENAME(@dbname)
		exec sp_executesql @esql
	end try
	begin catch
		print 'error ' + @dbname
	end catch
	
	fetch next from dbc
		into @dbname
end

close dbc
deallocate dbc";
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (SqlException)
            {
            }
            finally
            {
                if (!wasOpen)
                {
                    sqlConnection.Close();
                }
            }
        }
    }
}

// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods, constants, etc dealing with the SMO Table object
    /// </summary>
    public static class TableObjectHelpers
    {
        /// <summary>
        /// Creates a FOR INSERT trigger definition with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound. 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="triggerNamePrefix"></param>
        /// <param name="textBody"></param>
        /// <returns></returns>
        public static Trigger CreateTriggerDefinition(this Table table, string triggerNamePrefix, string textBody)
        {
            var trigger = new Trigger(table, SmoObjectHelpers.GenerateUniqueObjectName(triggerNamePrefix));
            trigger.TextBody = textBody;
            trigger.TextHeader = string.Format("CREATE TRIGGER {0} ON {1}.{2} FOR INSERT AS",
                SmoObjectHelpers.SqlBracketQuoteString(trigger.Name),
                SmoObjectHelpers.SqlBracketQuoteString(table.Schema),
                SmoObjectHelpers.SqlBracketQuoteString(table.Name));
           TraceHelper.TraceInformation("Creating new trigger definition \"{0}\" on table \"{1}.{2}\"", trigger.Name, table.Schema, table.Name);
            return trigger;
        }

        /// <summary>
        /// Creates a FOR INSERT trigger with a uniquely generated name prefixed by the specified prefix and defined with the specified
        /// body and header. Optionally allows specifying the schema and whether the view is Schema Bound. 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="triggerNamePrefix"></param>
        /// <param name="textBody"></param>
        /// <returns></returns>
        public static Trigger CreateTrigger(this Table table, string triggerNamePrefix, string textBody)
        {
            var trigger = table.CreateTriggerDefinition(triggerNamePrefix, textBody);
            trigger.Create();
            return trigger;
        }

        /// <summary>
        /// Insert some ordered data into each row of the existed table. </summary>
        /// <param name="table">Smo object.</param>
        /// <param name="rowCount">The number of rows would be inserted. </param>
        public static Table InsertDataToTable(this Table table, int rowCount)
        {
            // Get the "@i, @i, ..., @i" string 
            string col_query = "";
            int cnt = table.Columns.Count;
            while (cnt != 0)
            {
                col_query += "@i";
                cnt--;
                if (cnt != 0)
                {
                    col_query += ",";
                }
            }

            // Generate the tsql of inserting data into table.
            string query = string.Format(@"BEGIN TRAN
                                           DECLARE @i INT
                                           SET @i = 0
                                           WHILE @i < {0}
                                           BEGIN
                                               INSERT INTO {1} VALUES ({2})
                                               SET @i = @i + 1
                                           END
                                           COMMIT TRAN",
                rowCount, table.ToString(), col_query);

            TraceHelper.TraceInformation("Inserting {0} rows data into the created table {1}", rowCount.ToString(),
                table.ToString());
            table.Parent.ExecuteNonQuery(query);
            table.Refresh();
            return table;
        }
    }
}
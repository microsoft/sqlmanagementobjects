// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Globalization;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;


    internal class RestorePlanInternal : SqlObject
    {
        public override EnumResult GetData(EnumResult erParent)
        {
            SqlEnumResult res = (SqlEnumResult)erParent;
            res.StatementBuilder.AddPrefix("DECLARE @db_name              sysname ,@restore_to_datetime  datetime \n");

            ComputeFixedProperties();
            String dbName = this.GetFixedStringProperty("DatabaseName", false);
            String dateTime = this.GetFixedStringProperty("BackupStartDate", false);

            if( null == dbName )
            {	
                throw new InternalEnumeratorException(StringSqlEnumerator.DatabaseNameMustBeSpecified);
            }
            else
            {
                res.StatementBuilder.AddPrefix(String.Format(CultureInfo.InvariantCulture, "select @db_name = N'{0}'\n", dbName));
            }
            
            if( null == dateTime )
            {	
                res.StatementBuilder.AddPrefix("select @restore_to_datetime = GETDATE()\n");
            }
            else
            {
                res.StatementBuilder.AddPrefix(String.Format(CultureInfo.InvariantCulture, "select @restore_to_datetime = N'{0}'\n", dateTime));
            }
            this.Filter = null;

            return base.GetData(erParent);
        }
    }
}

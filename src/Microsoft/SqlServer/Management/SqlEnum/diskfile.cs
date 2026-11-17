// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Globalization;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;

    internal class DiskFile : SqlObject
    {
        public override EnumResult GetData(EnumResult erParent)
        {
            SqlEnumResult res = (SqlEnumResult)erParent;
            res.StatementBuilder.AddPrefix("declare @Path nvarchar(255)\ndeclare @Name nvarchar(255)\n");

            ComputeFixedProperties();            
            var folderPath = this.GetFixedStringProperty("Path", removeEscape: false);
            var fullName = this.GetFixedStringProperty("FullName", removeEscape: false);
            
            if( (null == folderPath && null == fullName) || (null != folderPath && null != fullName) )
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.OnlyPathOrFullName);
            }
            
            if (null != folderPath)
            {
                res.StatementBuilder.AddPrefix(String.Format(CultureInfo.InvariantCulture, "select @Path = N'{0}'\n",
                    folderPath));
            }
            else
            {
                res.StatementBuilder.AddPrefix(@"select @Path = null;");
            }
            
            if (null != fullName)
            {
                res.StatementBuilder.AddPrefix(String.Format(CultureInfo.InvariantCulture, "select @Name = N'{0}'\n",
                    fullName));
            }
            else
            {
                res.StatementBuilder.AddPrefix(@"select @Name = null;");
            }

            this.Filter = null;

            return base.GetData(erParent);
        }
    }
}

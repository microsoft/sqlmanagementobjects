// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Globalization;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;

    internal class PrimaryFile : SqlObject
    {
        public override EnumResult GetData(EnumResult erParent)
        {
            SqlEnumResult res = (SqlEnumResult)erParent;
            res.StatementBuilder.AddPrefix("declare @Path nvarchar(255)\ndeclare @Name nvarchar(255)\n");

            ComputeFixedProperties();
            String sName = this.GetFixedStringProperty("Name", false);

            if (null == sName)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.PropMustBeSpecified("Name", "PrimaryFile"));
            }
            res.StatementBuilder.AddPrefix(String.Format(CultureInfo.InvariantCulture, "declare @fileName nvarchar(255)\nselect @fileName = N'{0}'\n", Util.EscapeString(sName, '\'')));
            this.Filter = null;

            return base.GetData(erParent);
        }
    }
}

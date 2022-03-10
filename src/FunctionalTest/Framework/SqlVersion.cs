// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;


namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    [Flags]
    public enum SqlVersion
    {
        AzureSawaV1 = 1,
        AzureSterlingV12 = 2,
        Sql2005 = 4,
        Sql2008 = 8,
        Sql2008R2 = 16,
        Sql2012 = 32,
        Sql2012SP1 = 64,
        Sql2014 = 128,
        Sql2016 = 256,
        Sql2017 = 512,
        Sqlv150 = 1024
    }
}

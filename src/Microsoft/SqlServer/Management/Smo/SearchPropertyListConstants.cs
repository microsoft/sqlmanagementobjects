// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    ///  static internal class to isolate the DDL changes
    ///  related to SearchPropertyList
    ///  </summary>
    static class SearchPropertyListConstants
    {
        public const String SearchPropertyList = "SEARCH PROPERTY LIST";
        public const String SearchProperty = "SEARCH PROPERTY";

        public const int MaxSearchPropertyListNameLength = 256;
        public const int MaxSearchPropertyNameLength = 256;
        public const int MaxSearchPropertyDescriptionLength = 512;
    }
}

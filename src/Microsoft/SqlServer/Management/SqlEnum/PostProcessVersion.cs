// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;

    internal class PostProcessVersion : PostProcess
    {
        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            return ConvertToValidVersion (GetTriggeredInt32 (dp, 0), GetTriggeredInt32 (dp, 1), GetTriggeredInt32 (dp, 2), GetTriggeredInt32 (dp, 3));
        }

        internal static Version ConvertToValidVersion (int major, int minor, int build, int revision)
        {
            return new Version (
                    -1 == major ? 0 : major,
                    -1 == minor ? 0 : minor,
                    -1 == build ? 0 : build,
                    -1 == revision ? 0 : revision);
        }
    }
}

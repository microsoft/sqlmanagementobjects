// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;

    // Fake definitions to keep the compiler happy - Codegen doesn't directly depend on these classes. Since it's an
    // internal tool, it would not be a big issue to work around any bugs on these, if found!
    class SfcObjectState{}
    class SfcInstance{}
    class SfcKey{}
    static class SfcUtility
    {
        //This is a fake method needed to work around the SFC.Util dependancies
        public static System.Reflection.Assembly LoadSqlCeAssembly(String name) { return null; }
    }
	
	
}

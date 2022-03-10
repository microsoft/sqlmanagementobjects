// Copyright (c) Microsoft.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// Helper methods for enum DatabaseEngineType
    /// </summary>
    internal static class DatabaseEngineTypeExtension
    {
       /// <summary>
       /// This method always returns false 
       /// Once we fix all the dependents to remove matrix related code, we shall remove this class
       /// </summary>
       /// <param name="databaseEngineType"></param>
       /// <returns></returns>
        //[Obsolete("Matrix code is currently being removed. Please remove the code that calls this method")]
        internal static bool IsMatrix(DatabaseEngineType databaseEngineType)
        {
            return false;
        }
    }
}

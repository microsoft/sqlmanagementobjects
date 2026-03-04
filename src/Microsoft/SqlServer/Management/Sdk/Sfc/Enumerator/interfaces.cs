// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using Microsoft.SqlServer.Management.Common;

    /// <summary>
    /// implemented by levels that need a configuration file
    /// </summary>
    public interface ISupportInitData
    {
        /// <summary>
        /// load the given file for the given version
        /// </summary>
        /// <param name="file"></param>
        /// <param name="ver"></param>
        void LoadInitData(String file, ServerVersion ver);
    }

    /// <summary>
    /// implemented by Smo Enumerator to read the Configuration files of the SMO Object
    /// </summary>
    public interface ISupportInitDatabaseEngineData 
    {

        /// <summary>
        /// load the given file for the given version, engine type and engine edition
        /// </summary>
        /// <param name="file">The file we're loading the object data from</param>
        /// <param name="ver">Version of the server to load data for</param>
        /// <param name="databaseEngineType">Engine type of the server to load data for</param>
        /// <param name="databaseEngineEdition">Engine edition of the server to load data for</param>
        void LoadInitData(String file, ServerVersion ver,DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition);
    }

    /// <summary>
    /// defines method by which the dependencies are requested
    /// </summary>
    public interface IEnumDependencies
    {
        /// <summary>
        /// returns the dependencies for the given XPATHs and flags
        /// </summary>
        /// <param name="ci"></param>
        /// <param name="rd"></param>
        /// <returns></returns>
        DependencyChainCollection EnumDependencies(Object ci, DependencyRequest rd);
    }

    /// <summary>
    /// can only be on the first object. if it wants to do some housekeeping with the connection
    /// </summary>
    public interface ISupportVersions
    {
        /// <summary>
        /// given the connection returns the server version
        /// </summary>
        /// <param name="conn">Connection object to get the engine type from</param>
        ServerVersion GetServerVersion(Object conn);
    }

    /// <summary>
    /// This interface is used to get the database engine type from the connection 
    /// </summary>
    public interface ISupportDatabaseEngineTypes
    {
        /// <summary>
        /// Get the DatabaseEngineType for the specified connection
        /// </summary>
        /// <param name="conn">Connection object to get the engine type from</param>
        DatabaseEngineType GetDatabaseEngineType(Object conn);
    }

    /// <summary>
    /// This interface is used to get the database engine edition from the connection
    /// </summary>
    public interface ISupportDatabaseEngineEditions
    {
        /// <summary>
        /// Get the DatabaseEngineEdition for the specified connection
        /// </summary>
        /// <param name="conn">Connection object to get the engine type from</param>
        DatabaseEngineEdition GetDatabaseEngineEdition(Object conn);
    }
}

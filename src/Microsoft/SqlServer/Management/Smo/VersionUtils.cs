// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    ///     Helper class to handle various version-related checks
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static public class VersionUtils
    {
        #region Version Bump work

        /// <summary>
        /// Check whether the target server version is greater than or equal to SQL 15 or SQL 12 on Azure.
        /// </summary>
        /// <param name="targetDatabaseEngineType">target database engine type</param>
        /// <param name="targetServerVersion">target server version</param>
        static internal bool IsTargetVersionSql15Azure12OrLater(DatabaseEngineType targetDatabaseEngineType, SqlServerVersion targetServerVersion)
        {
            return targetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || IsTargetServerVersionSQl15OrLater(targetServerVersion);
        }

        /// <summary>
        /// Check whether the target server version is greater than or equal to SQL 16 or SQL 12 on Azure.
        /// </summary>
        /// <param name="targetDatabaseEngineType">target database engine type</param>
        /// <param name="targetServerVersion">target server version</param>
        static internal bool IsTargetVersionSql16Azure12OrLater(DatabaseEngineType targetDatabaseEngineType, SqlServerVersion targetServerVersion)
        {
            return targetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || IsTargetServerVersionSQl16OrLater(targetServerVersion);
        }

        /// <summary>
        /// Check whether the current server version is greater than or equal to SQL 15 or SQL 12 on Azure.
        /// </summary>
        /// <param name="currentDatabaseEngineType">current database engine type</param>
        /// <param name="currentServerVersion">current server version</param>
        static internal bool IsSql15Azure12OrLater(DatabaseEngineType currentDatabaseEngineType, ServerVersion currentServerVersion)
        {
            return currentDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || IsSql15OrLater(currentServerVersion);
        }

        /// <summary>
        /// Check whether the current server version is greater than or equal to SQL 16 or SQL 12 on Azure.
        /// </summary>
        /// <param name="currentDatabaseEngineType">current database engine type</param>
        /// <param name="currentServerVersion">current server version</param>
        static internal bool IsSql16Azure12OrLater(DatabaseEngineType currentDatabaseEngineType, ServerVersion currentServerVersion)
        {
            return currentDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || IsSql16OrLater(currentServerVersion);
        }

        static internal bool IsSql15OrLater(SqlServerVersion targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version150 && currentServerVersion.Major >= 15;
        }

        static internal bool IsSql16OrLater(SqlServerVersion targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version160 && currentServerVersion.Major >= 16;
        }

        static internal bool IsTargetServerVersionSQl15OrLater(SqlServerVersion targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version150;
        }

        static internal bool IsTargetServerVersionSQl16OrLater(SqlServerVersion targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version160;
        }

        static public bool IsSql15OrLater(ServerVersion currentServerVersion)
        {
            return IsSql15OrLater(SqlServerVersion.Version150, currentServerVersion);
        }

        static public bool IsSql16OrLater(ServerVersion currentServerVersion)
        {
            return IsSql16OrLater(SqlServerVersion.Version160, currentServerVersion);
        }

        static internal bool IsSql14OrLater(SqlServerVersion targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version140 && currentServerVersion.Major >= 14;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 140 or higher
        /// </summary>
        /// <param name="currentServerVersion"></param>
        /// <returns>true if the version is 140 or higher</returns>
        static public bool IsSql14OrLater(ServerVersion currentServerVersion)
        {
            return IsSql14OrLater(SqlServerVersion.Version140, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl14OrLater(SqlServerVersion targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version140;
        }

        static internal bool IsSql13OrLater(SqlServerVersion targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version130 && currentServerVersion.Major >= 13;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 130 or higher
        /// </summary>
        /// <param name="targetServerVersion"></param>
        /// <param name="currentServerVersion"></param>
        /// <returns>true if the ServerVersion is 130 or higher</returns>
        static public bool IsSql13OrLater(ServerVersion currentServerVersion)
        {
            return IsSql13OrLater(SqlServerVersion.Version130, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl13OrLater(SqlServerVersion targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version130;
        }

        
        static internal bool IsSql12OrLater(SqlServerVersion targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version120 && currentServerVersion.Major >= 12;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 120 or higher
        /// </summary>
        /// <param name="targetServerVersion"></param>
        /// <param name="currentServerVersion"></param>
        /// <returns>true if the ServerVersion is 120 or higher</returns>
        static public bool IsSql12OrLater(ServerVersion currentServerVersion)
        {
            return IsSql12OrLater(SqlServerVersion.Version120, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl12OrLater(SqlServerVersion targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version120;
        }

        static internal bool IsSql11OrLater(SqlServerVersion targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version110 && currentServerVersion.Major >= 11;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 110 or higher
        /// </summary>
        /// <param name="currentServerVersion"></param>
        /// <returns></returns>
        static public bool IsSql11OrLater(ServerVersion currentServerVersion)
        {
            return IsSql11OrLater(SqlServerVersion.Version110, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl11OrLater(SqlServerVersion targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersion.Version110;
        }

        /// <summary>
        /// Check whether the  server version is greater than or equal to SQL 13 or SQL 12 on Azure.
        /// </summary>
        /// <param name="currentDatabaseEngineType">current database engine type</param>
        /// <param name="currentServerVersion">current server version</param>
        /// <param name="sp">scripting preferences, if exists, to check target database engine type and target server version</param>
        static internal bool IsSql13Azure12OrLater(DatabaseEngineType currentDatabaseEngineType, ServerVersion currentServerVersion, ScriptingPreferences sp)
        {
            if (sp != null)
            {
                if (sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    if (!IsTargetServerVersionSQl12OrLater(sp.TargetServerVersion))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!IsTargetServerVersionSQl13OrLater(sp.TargetServerVersion))
                    {
                        return false;
                    }
                }
            }

            if (currentDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return IsSql12OrLater(currentServerVersion);
            }
            else
            {
                return IsSql13OrLater(currentServerVersion);
            }
        }

        /// <summary>
        /// Check whether the  server version is greater than or equal to SQL 13 or SQL 12 on Azure.
        /// </summary>
        /// <param name="currentDatabaseEngineType">current database engine type</param>
        /// <param name="currentServerVersion">current server version</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        static public bool IsSql13Azure12OrLater(DatabaseEngineType currentDatabaseEngineType, ServerVersion currentServerVersion)
        {
            return IsSql13Azure12OrLater(currentDatabaseEngineType, currentServerVersion, null);
        }

        static internal ServerVersion HighestKnownServerVersion
        {
            get
            {
                return VersionUtils.Sql160ServerVersion;
            }
        }

        static internal ServerVersion Sql110ServerVersion
        {
            get
            {
                return new ServerVersion(11, 0);
            }
        }

        static internal ServerVersion Sql120ServerVersion
        {
            get
            {
                return new ServerVersion(12, 0);
            }
        }

        static internal ServerVersion Sql130ServerVersion
        {
            get
            {
                return new ServerVersion(13, 0);
            }
        }

        static internal ServerVersion Sql140ServerVersion
        {
            get
            {
                return new ServerVersion(14, 0);
            }
        }

        static internal ServerVersion Sql150ServerVersion
        {
            get
            {
                return new ServerVersion(15, 0);
            }
        }

        static internal ServerVersion Sql160ServerVersion
        {
            get
            {
                return new ServerVersion(16, 0);
            }
        }

        static internal SqlServerVersion Sql110TargetServerVersion
        {
            get
            {
                return SqlServerVersion.Version110;
            }
        }

        static internal SqlServerVersion Sql120TargetServerVersion
        {
            get
            {
                return SqlServerVersion.Version120;
            }
        }

        static internal SqlServerVersion Sql130TargetServerVersion
        {
            get
            {
                return SqlServerVersion.Version130;
            }
        }

        static internal SqlServerVersion Sql140TargetServerVersion
        {
            get
            {
                return SqlServerVersion.Version140;
            }
        }

        static internal SqlServerVersion Sql150TargetServerVersion
        {
            get
            {
                return SqlServerVersion.Version150;
            }
        }

        static internal SqlServerVersion Sql160TargetServerVersion
        {
            get
            {
                return SqlServerVersion.Version160;
            }
        }

        #endregion
    }
}

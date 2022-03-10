// Copyright (c) Microsoft.
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
        static internal bool IsTargetVersionSql15Azure12OrLater(DatabaseEngineType targetDatabaseEngineType, SqlServerVersionInternal targetServerVersion)
        {
            return targetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase || IsTargetServerVersionSQl15OrLater(targetServerVersion);
        }

        /// <summary>
        /// Check whether the target server version is greater than or equal to SQL 16 or SQL 12 on Azure.
        /// </summary>
        /// <param name="targetDatabaseEngineType">target database engine type</param>
        /// <param name="targetServerVersion">target server version</param>
        static internal bool IsTargetVersionSql16Azure12OrLater(DatabaseEngineType targetDatabaseEngineType, SqlServerVersionInternal targetServerVersion)
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

        static internal bool IsSql15OrLater(SqlServerVersionInternal targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version150 && currentServerVersion.Major >= 15;
        }

        static internal bool IsSql16OrLater(SqlServerVersionInternal targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version160 && currentServerVersion.Major >= 16;
        }

        static internal bool IsTargetServerVersionSQl15OrLater(SqlServerVersionInternal targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version150;
        }

        static internal bool IsTargetServerVersionSQl16OrLater(SqlServerVersionInternal targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version160;
        }

        static public bool IsSql15OrLater(ServerVersion currentServerVersion)
        {
            return IsSql15OrLater(SqlServerVersionInternal.Version150, currentServerVersion);
        }

        static public bool IsSql16OrLater(ServerVersion currentServerVersion)
        {
            return IsSql16OrLater(SqlServerVersionInternal.Version160, currentServerVersion);
        }

        static internal bool IsSql14OrLater(SqlServerVersionInternal targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version140 && currentServerVersion.Major >= 14;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 140 or higher
        /// </summary>
        /// <param name="currentServerVersion"></param>
        /// <returns>true if the version is 140 or higher</returns>
        static public bool IsSql14OrLater(ServerVersion currentServerVersion)
        {
            return IsSql14OrLater(SqlServerVersionInternal.Version140, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl14OrLater(SqlServerVersionInternal targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version140;
        }

        static internal bool IsSql13OrLater(SqlServerVersionInternal targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version130 && currentServerVersion.Major >= 13;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 130 or higher
        /// </summary>
        /// <param name="targetServerVersion"></param>
        /// <param name="currentServerVersion"></param>
        /// <returns>true if the ServerVersion is 130 or higher</returns>
        static public bool IsSql13OrLater(ServerVersion currentServerVersion)
        {
            return IsSql13OrLater(SqlServerVersionInternal.Version130, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl13OrLater(SqlServerVersionInternal targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version130;
        }

        
        static internal bool IsSql12OrLater(SqlServerVersionInternal targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version120 && currentServerVersion.Major >= 12;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 120 or higher
        /// </summary>
        /// <param name="targetServerVersion"></param>
        /// <param name="currentServerVersion"></param>
        /// <returns>true if the ServerVersion is 120 or higher</returns>
        static public bool IsSql12OrLater(ServerVersion currentServerVersion)
        {
            return IsSql12OrLater(SqlServerVersionInternal.Version120, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl12OrLater(SqlServerVersionInternal targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version120;
        }

        static internal bool IsSql11OrLater(SqlServerVersionInternal targetServerVersion, ServerVersion currentServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version110 && currentServerVersion.Major >= 11;
        }

        /// <summary>
        /// Returns whether the given ServerVersion is 110 or higher
        /// </summary>
        /// <param name="currentServerVersion"></param>
        /// <returns></returns>
        static public bool IsSql11OrLater(ServerVersion currentServerVersion)
        {
            return IsSql11OrLater(SqlServerVersionInternal.Version110, currentServerVersion);
        }

        static internal bool IsTargetServerVersionSQl11OrLater(SqlServerVersionInternal targetServerVersion)
        {
            return targetServerVersion >= SqlServerVersionInternal.Version110;
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
                    if (!IsTargetServerVersionSQl12OrLater(sp.TargetServerVersionInternal))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!IsTargetServerVersionSQl13OrLater(sp.TargetServerVersionInternal))
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

        static internal SqlServerVersionInternal Sql110TargetServerVersion
        {
            get
            {
                return SqlServerVersionInternal.Version110;
            }
        }

        static internal SqlServerVersionInternal Sql120TargetServerVersion
        {
            get
            {
                return SqlServerVersionInternal.Version120;
            }
        }

        static internal SqlServerVersionInternal Sql130TargetServerVersion
        {
            get
            {
                return SqlServerVersionInternal.Version130;
            }
        }

        static internal SqlServerVersionInternal Sql140TargetServerVersion
        {
            get
            {
                return SqlServerVersionInternal.Version140;
            }
        }

        static internal SqlServerVersionInternal Sql150TargetServerVersion
        {
            get
            {
                return SqlServerVersionInternal.Version150;
            }
        }

        static internal SqlServerVersionInternal Sql160TargetServerVersion
        {
            get
            {
                return SqlServerVersionInternal.Version160;
            }
        }

        #endregion
    }
}

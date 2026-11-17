// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Assessment
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.SqlServer.Management.Assessment.Checks;
    using Microsoft.SqlServer.Management.Assessment.Configuration;
    using Microsoft.SqlServer.Management.Smo;

    /// <summary>
    /// Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for checking SMO objects for best practice compliance.
    /// </summary>
    public static class SqlAssessmentExtensions
    {
        /// <summary>
        /// An assessment engine used by all extension methods. Use Configuration property of this object to customize configuration.
        /// </summary>
        public static readonly Engine Engine = new Engine();

        #region SqlSmoObject

        /// <summary>
        /// Finds all available checks for given <paramref name="smoObject"/>. 
        /// </summary>
        /// <param name="smoObject">Returned checks are applicable to this target <see cref="SqlSmoObject"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to select only specific checks.</param>
        /// <returns>Returns an <see cref="IEnumerable{ICheck}"/> for target <see cref="SqlSmoObject"/>.</returns>
        public static IEnumerable<ICheck> GetAssessmentItems(this SqlSmoObject smoObject, params string[] checkIdsOrTags)
        {
            if (smoObject == null)
            {
                throw new ArgumentNullException("smoObject");
            }

            var server = smoObject as Server;
            if (server != null)
            {
                return GetAssessmentItems(server, checkIdsOrTags);
            }

            var database = smoObject as Database;
            if (database != null)
            {
                return GetAssessmentItems(database, checkIdsOrTags);
            }

            var fileGroup = smoObject as FileGroup;
            if (fileGroup != null)
            {
                return GetAssessmentItems(fileGroup, checkIdsOrTags);
            }

            var availabilityGroup = smoObject as AvailabilityGroup;
            if (availabilityGroup != null)
            {
                return GetAssessmentItems(availabilityGroup, checkIdsOrTags);
            }

            return Array.Empty<ICheck>();
        }

        /// <summary>
        /// Asynchronously runs assessment for given <paramref name="smoObject"/> and gives a <see cref="List{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="smoObject">Target <see cref="SqlSmoObject"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="List{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="List{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this SqlSmoObject smoObject, params string[] checkIdsOrTags)
        {
            if (smoObject == null)
            {
                throw new ArgumentNullException("smoObject");
            }

            var server = smoObject as Server;
            if (server != null)
            {
                return GetAssessmentResultsList(server, checkIdsOrTags);
            }

            var database = smoObject as Database;
            if (database != null)
            {
                return GetAssessmentResultsList(database, checkIdsOrTags);
            }

            var fileGroup = smoObject as FileGroup;
            if (fileGroup != null)
            {
                return GetAssessmentResultsList(fileGroup, checkIdsOrTags);
            }

            var availabilityGroup = smoObject as AvailabilityGroup;
            if (availabilityGroup != null)
            {
                return GetAssessmentResultsList(availabilityGroup, checkIdsOrTags);
            }

            return Task.FromResult(new List<IAssessmentResult>());
        }
        
        /// <summary>
        /// Asynchronously runs assessment for given <paramref name="smoObject"/> and gives a <see cref="List{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="smoObject">Target <see cref="SqlSmoObject"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="List{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="List{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this SqlSmoObject smoObject, IEnumerable<ICheck> checks)
        {
            if (smoObject == null)
            {
                throw new ArgumentNullException("smoObject");
            }

            var server = smoObject as Server;
            if (server != null)
            {
                return GetAssessmentResultsList(server, checks);
            }

            var database = smoObject as Database;
            if (database != null)
            {
                return GetAssessmentResultsList(database, checks);
            }

            var fileGroup = smoObject as FileGroup;
            if (fileGroup != null)
            {
                return GetAssessmentResultsList(fileGroup, checks);
            }

            var availabilityGroup = smoObject as AvailabilityGroup;
            if (availabilityGroup != null)
            {
                return GetAssessmentResultsList(availabilityGroup, checks);
            }

            return Task.FromResult(new List<IAssessmentResult>());
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="smoObject"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="smoObject">Target <see cref="SqlSmoObject"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this SqlSmoObject smoObject, params string[] checkIdsOrTags)
        {
            if (smoObject == null)
            {
                throw new ArgumentNullException("smoObject");
            }

            return GetAssessmentResultsList(smoObject, checkIdsOrTags).Result;
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="smoObject"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="smoObject">Target <see cref="SqlSmoObject"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this SqlSmoObject smoObject, IEnumerable<ICheck> checks)
        {
            if (smoObject == null)
            {
                throw new ArgumentNullException("smoObject");
            }

            return GetAssessmentResultsList(smoObject, checks).Result;
        }

        #endregion SqlSmoObject

        #region AvailabilityGroup

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="availabilityGroup"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="availabilityGroup">Target <see cref="AvailabilityGroup"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this AvailabilityGroup availabilityGroup, params string[] checkIdsOrTags)
        {
            if (availabilityGroup == null)
            {
                throw new ArgumentNullException("availabilityGroup");
            }

            return GetAssessmentResultsList(availabilityGroup, checkIdsOrTags).Result;
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="availabilityGroup"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="availabilityGroup">Target <see cref="AvailabilityGroup"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this AvailabilityGroup availabilityGroup, IEnumerable<ICheck> checks)
        {
            if (availabilityGroup == null)
            {
                throw new ArgumentNullException("availabilityGroup");
            }

            return GetAssessmentResultsList(availabilityGroup, checks).Result;
        }

        /// <summary>
        /// Finds all available checks for given <paramref name="availabilityGroup"/>. 
        /// </summary>
        /// <param name="availabilityGroup">Returned checks are applicable to this target <see cref="AvailabilityGroup"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to select only specific checks.</param>
        /// <returns>Returns an <see cref="IEnumerable{ICheck}"/> for target <see cref="SqlSmoObject"/>.</returns>
        public static IEnumerable<ICheck> GetAssessmentItems(this AvailabilityGroup availabilityGroup, params string[] checkIdsOrTags)
        {
            if (availabilityGroup == null)
            {
                throw new ArgumentNullException("availabilityGroup");
            }

            ISqlObjectLocator locator = GetLocator(availabilityGroup);
            return GetAssessmentItems(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Asynchronously runs assessment for given <paramref name="availabilityGroup"/> and gives a <see cref="List{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="availabilityGroup">Target <see cref="AvailabilityGroup"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="List{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="List{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this AvailabilityGroup availabilityGroup, params string[] checkIdsOrTags)
        {
            if (availabilityGroup == null)
            {
                throw new ArgumentNullException("availabilityGroup");
            }

            var locator = GetLocator(availabilityGroup);
            return GetAssessmentResultsList(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="availabilityGroup"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="availabilityGroup">Target <see cref="AvailabilityGroup"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this AvailabilityGroup availabilityGroup, IEnumerable<ICheck> checks)
        {
            if (availabilityGroup == null)
            {
                throw new ArgumentNullException("availabilityGroup");
            }

            var locator = GetLocator(availabilityGroup);
            var checksCopy = checks.ToArray();
            return GetAssessmentResultsList(locator, checksCopy);
        }

        #endregion  AvailabilityGroup

        #region Database

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="database"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="database">Target <see cref="Database"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this Database database, params string[] checkIdsOrTags)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            return GetAssessmentResultsList(database, checkIdsOrTags).Result;
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="database"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="database">Target <see cref="Database"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this Database database, IEnumerable<ICheck> checks)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            return GetAssessmentResultsList(database, checks).Result;
        }

        /// <summary>
        /// Finds all available checks for given <paramref name="database"/>. 
        /// </summary>
        /// <param name="database">Returned checks are applicable to this target <see cref="Database"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to select only specific checks.</param>
        /// <returns>Returns an <see cref="IEnumerable{ICheck}"/> for target <see cref="SqlSmoObject"/>.</returns>
        public static IEnumerable<ICheck> GetAssessmentItems(this Database database, params string[] checkIdsOrTags)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            ISqlObjectLocator locator = GetLocator(database);
            return GetAssessmentItems(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Asynchronously runs assessment for given <paramref name="database"/> and gives a <see cref="List{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="database">Target <see cref="Database"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="List{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="List{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this Database database, params string[] checkIdsOrTags)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            var locator = GetLocator(database);
            return GetAssessmentResultsList(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="database"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="database">Target <see cref="Database"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this Database database, IEnumerable<ICheck> checks)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            var locator    = GetLocator(database);
            var checksCopy = checks.ToArray();
            return GetAssessmentResultsList(locator, checksCopy);
        }
        
        #endregion Database

        #region FileGroup

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="fileGroup"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="fileGroup">Target <see cref="FileGroup"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this FileGroup fileGroup, params string[] checkIdsOrTags)
        {
            if (fileGroup == null)
            {
                throw new ArgumentNullException("fileGroup");
            }

            return GetAssessmentResultsList(fileGroup, checkIdsOrTags).Result;
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="fileGroup"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="fileGroup">Target <see cref="FileGroup"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this FileGroup fileGroup, IEnumerable<ICheck> checks)
        {
            if (fileGroup == null)
            {
                throw new ArgumentNullException("fileGroup");
            }

            return GetAssessmentResultsList(fileGroup, checks).Result;
        }

        /// <summary>
        /// Finds all available checks for given <paramref name="fileGroup"/>. 
        /// </summary>
        /// <param name="fileGroup">Returned checks are applicable to this target <see cref="FileGroup"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to select only specific checks.</param>
        /// <returns>Returns an <see cref="IEnumerable{ICheck}"/> for target <see cref="SqlSmoObject"/>.</returns>
        public static IEnumerable<ICheck> GetAssessmentItems(this FileGroup fileGroup, params string[] checkIdsOrTags)
        {
            if (fileGroup == null)
            {
                throw new ArgumentNullException("fileGroup");
            }

            ISqlObjectLocator locator = GetLocator(fileGroup);
            return GetAssessmentItems(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Asynchronously runs assessment for given <paramref name="fileGroup"/> and gives a <see cref="List{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="fileGroup">Target <see cref="FileGroup"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="List{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="List{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this FileGroup fileGroup, params string[] checkIdsOrTags)
        {
            if (fileGroup == null)
            {
                throw new ArgumentNullException("fileGroup");
            }

            var locator = GetLocator(fileGroup);
            return GetAssessmentResultsList(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="fileGroup"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="fileGroup">Target <see cref="FileGroup"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this FileGroup fileGroup, IEnumerable<ICheck> checks)
        {
            if (fileGroup == null)
            {
                throw new ArgumentNullException("fileGroup");
            }

            var locator    = GetLocator(fileGroup);
            var checksCopy = checks.ToArray();
            return GetAssessmentResultsList(locator, checksCopy);
        }

        #endregion FileGroup

        #region Server

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="server"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="server">Target <see cref="Server"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this Server server, params string[] checkIdsOrTags)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            return GetAssessmentResultsList(server, checkIdsOrTags).Result;
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="server"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="server">Target <see cref="Server"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static IEnumerable<IAssessmentResult> GetAssessmentResults(this Server server, IEnumerable<ICheck> checks)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            return GetAssessmentResultsList(server, checks).Result;
        }

        /// <summary>
        /// Finds all available checks for given <paramref name="server"/>. 
        /// </summary>
        /// <param name="server">Returned checks are applicable to this target <see cref="Server"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to select only specific checks.</param>
        /// <returns>Returns an <see cref="IEnumerable{ICheck}"/> for target <see cref="SqlSmoObject"/>.</returns>
        public static IEnumerable<ICheck> GetAssessmentItems(this Server server, params string[] checkIdsOrTags)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            ISqlObjectLocator locator = GetLocator(server);
            return GetAssessmentItems(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Asynchronously runs assessment for given <paramref name="server"/> and gives a <see cref="List{IAssessmentResult}"/> describing discovered issues. 
        /// </summary>
        /// <param name="server">Target <see cref="Server"/>.</param>
        /// <param name="checkIdsOrTags">Optional array of <see cref="string"/> check ids or tags to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="List{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="List{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this Server server, params string[] checkIdsOrTags)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            var locator = GetLocator(server);
            return GetAssessmentResultsList(locator, checkIdsOrTags);
        }

        /// <summary>
        /// Synchronously runs assessment for given <paramref name="server"/> and gives a <see cref="IEnumerable{IAssessmentResult}"/> describing discovered issues.
        /// This method performs only given checks from <paramref name="checks"/>.
        /// </summary>
        /// <param name="server">Target <see cref="Server"/>.</param>
        /// <param name="checks">Optional <see cref="IEnumerable{ICheck}"/> to perform only selected checks.</param>
        /// <returns>
        /// Returns an <see cref="IEnumerable{IAssessmentResult}"/> for target <see cref="SqlSmoObject"/>.
        /// Returns an empty <see cref="IEnumerable{IAssessmentResult}"/> if no issues detected.
        /// </returns>
        public static Task<List<IAssessmentResult>> GetAssessmentResultsList(this Server server, IEnumerable<ICheck> checks)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            var locator = GetLocator(server);
            var checksCopy = checks.ToArray();
            return GetAssessmentResultsList(locator, checksCopy);
        }

        #endregion Server

        #region Implementation

        private static Task<List<IAssessmentResult>> GetAssessmentResultsList(ISqlObjectLocator locator, ICheck[] checksCopy)
        {
            if (checksCopy != null && checksCopy.Length > 0)
            {
                return Engine.GetAssessmentResultsList(locator, checksCopy);
            }

            return Engine.GetAssessmentResultsList(locator);
        }

        private static Task<List<IAssessmentResult>> GetAssessmentResultsList(ISqlObjectLocator locator, string[] checkIdsOrTags)
        {
            if (checkIdsOrTags != null && checkIdsOrTags.Length > 0)
            {
                return Engine.GetAssessmentResultsList(locator, checkIdsOrTags);
            }

            return Engine.GetAssessmentResultsList(locator);
        }

        private static IEnumerable<ICheck> GetAssessmentItems(ISqlObjectLocator locator, string[] checkIdsOrTags)
        {
            if (checkIdsOrTags != null && checkIdsOrTags.LongLength != 0)
            {
                return Engine.GetChecks(locator, checkIdsOrTags);
            }

            return Engine.GetChecks(locator);
        }

        private static SqlObjectLocator GetGenericLocator(Server server)
        {
            var result = new SqlObjectLocator
                {
                    Connection = (DbConnection)((ICloneable)server.ConnectionContext.SqlConnectionObject).Clone(),
                    Platform   = server.HostPlatform,
                    Version    = server.Version,
                    EngineEdition    = TranslateEdition(server.EngineEdition),
                    ServerName = server.Name
            };
            return result;
        }

        private static ISqlObjectLocator GetLocator(AvailabilityGroup availabilityGroup)
        {
            var locator = GetGenericLocator(availabilityGroup.Parent);
            locator.Name = availabilityGroup.Name;
            locator.Urn = availabilityGroup.Urn;
            locator.Type = SqlObjectType.AvailabilityGroup;
            return locator;
        }

        private static ISqlObjectLocator GetLocator(Database database)
        {
            var locator = GetGenericLocator(database.Parent);
            locator.Urn  = database.Urn;
            locator.Name = database.Name;
            locator.Type = SqlObjectType.Database;
            return locator;
        }

        private static ISqlObjectLocator GetLocator(FileGroup fileGroup)
        {
            var locator = GetGenericLocator(fileGroup.Parent.Parent);
            locator.Urn = fileGroup.Urn;
            locator.Name = fileGroup.Name;
            locator.Type = SqlObjectType.FileGroup;
            return locator;
        }

        private static ISqlObjectLocator GetLocator(Server server)
        {
            var locator = GetGenericLocator(server);
            locator.Urn = server.Urn;
            locator.Name = server.Name;
            locator.Type = SqlObjectType.Server;
            return locator;
        }

        private static SqlEngineEdition TranslateEdition(Edition edition)
        {
            return edition > 0 ? (SqlEngineEdition)(1 << ((int)edition - 1)) : SqlEngineEdition.None;
        }

        #endregion Implementation
    }
}

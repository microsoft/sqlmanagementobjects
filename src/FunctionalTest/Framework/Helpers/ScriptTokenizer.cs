// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    public class ScriptTokenizer
    {

        /// <summary>
        /// Regex to find instances of passwords in connection strings, which we'll then replace with
        /// the $(ConnStringPassword) token so we don't have passwords in the checked in files
        ///
        /// Note the check is a bit simplistic and doesn't cover all the edge cases - but until we come across
        /// an edge case just leaving it like this for times sake
        /// </summary>
        private static readonly Regex ConnStringPasswordRegex = new Regex("password=.*;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //Regex to match the SecretStore tokens. A SecretStore token is in the form
        //$(SecretStore:<SecretName>)
        //Where <SecretName> is the secret name, minus the common prefix
        //So a token like this $(SecretStore:SmoBaselineVerification_SqlToolsWasbKey/Secret) would
        //retrieve the value for a secret with the full name of SSMS_TEST_SECRET_PREFIX + SmoBaselineVerification_SqlToolsWasbKey/Secret
        private static readonly Regex SecretStoreRegex = new Regex(@"(\$\(SecretStore:(?<SecretName>.*?)\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Matches cluster domain names, which is in the form of
        ///     $(ServerName) OR $(SingleQuoteEscapedServerName) OR $(BracketEscapedServerName)
        ///     .
        ///     Any number of non-whitespace characters (non-greedy)
        ///     .com OR .net
        ///
        /// This is meant to be ran after the server name has been replaced since on Azure servers the server TrueName does not include
        /// the domain - so we want to keep that part separate.
        /// </summary>
        private static readonly Regex ClusterDomainNameRegex = new Regex(@"\$\((BracketEscapedServerName|SingleQuoteEscapedServerName|ServerName)\)\.\S*?\.(com|net)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// This matches multi-line T-SQL comments which start with /* and end with */
        /// </summary>
        private static readonly Regex MultiLineCommentsRegex = new Regex(@"\/\*.*\*\/", RegexOptions.Compiled | RegexOptions.Singleline);

        private const string TOKEN_DatabaseName = "$(DatabaseName)";
        private const string TOKEN_SingleQuoteEscapedDatabaseName = "$(SingleQuoteEscapedDatabaseName)";
        private const string TOKEN_BracketEscapedDatabaseName = "$(BracketEscapedDatabaseName)";
        private const string TOKEN_QuoteAndBracketEscapedDatabaseName = "$(QuoteAndBracketEscapedDatabaseName)";
        private const string TOKEN_ServerName = "$(ServerName)";
        private const string TOKEN_ServerInternalName = "$(ServerInternalName)";
        private const string TOKEN_SingleQuoteEscapedServerName = "$(SingleQuoteEscapedServerName)";
        private const string TOKEN_BracketEscapedServerName = "$(BracketEscapedServerName)";
        private const string TOKEN_BracketEscapedServerInternalName = "$(BracketEscapedServerInternalName)";
        private const string TOKEN_DefaultDataPath = "$(DefaultDataPath)";
        private const string TOKEN_TSqlPassword = "$(TSqlPassword)";
        private const string TOKEN_RandomTSqlPassword = "$(RandomTSqlPassword)";
        private const string TOKEN_RandomTSqlSecret = "$(RandomTSqlSecret)";
        private const string TOKEN_ConnStringPassword = "$(ConnStringPassword)";
        private const string TOKEN_ScriptDate = "$(ScriptDate)";
        private const string TOKEN_ScriptStatsStream = "$(StatsStream)";
        private const string TOKEN_ClusterDomainName = "$(ClusterDomainName)";
        private const string TOKEN_RandomGuid = "$(RandomGuid)";
        //The svr.Version string (usually Major.Minor.Build, no revision)
        private const string TOKEN_ServerVersion = "$(ServerVersion)";
        //The full Major.Minor.Build.Revision version string
        private const string TOKEN_ServerVersionString = "$(ServerVersionString)";
        //The SERVERPROPERTY('ProductVersion') from the Database-specific connection
        private const string TOKEN_DatabaseProductVersion = "$(DatabaseProductVersion)";
        // The name of the computer hosting the instance
        private const string TOKEN_ComputerName = "$(ComputerName)";
        // The data source specified in the connection string. Must be tokenized after server name
        public const string TOKEN_DataSource = "$(DataSource)";
        // Unnamed Clustered Index names are random. Must be tokenized.
        public const string TOKEN_ClusteredIndexName = "$(ClusteredIndexName)";
        // Edge constraint name
        public const string TOKEN_EdgeConstraintName = "$(EdgeConstraintName)";
        // Sql file paths (uri) must be tokenized
        private const string TOKEN_MasterDBPath = "$(MasterDBPath)";
        private const string TOKEN_MasterDBLogPath = "$(MasterDBLogPath)";
        private const string TOKEN_PrimaryFilePath = "$(PrimaryFilePath)";
        private const string TOKEN_LogFileName = "$(LogFileName)";
        private const string TOKEN_InstallDataDirectory = "$(InstallDataDirectory)";

        // The default backup directory.
        // Example of values:
        // - E:\SQLDIRS\IN\MSSQL15.MSSQLSERVER\MSSQL\Backup
        // - C:\Program Files\Microsoft SQL Server\MSSQL10.MSSQLSERVER\MSSQL\Backup
        private const string TOKEN_BackupDirectory = "$(BackupDirectory)";
        private const string TOKEN_SingleQuoteEscapedBackupDirectory = "$(SingleQuoteEscapedBackupDirectory)";

        // The default error log directory/path.
        // Example of values:
        // - E:\SQLDIRS\IN\MSSQL15.MSSQLSERVER\MSSQL\Log
        // - C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\Log
        private const string TOKEN_ErrorLogPath = "$(ErrorLogPath)";
        private const string TOKEN_SingleQuoteEscapedErrorLogPath = "$(SingleQuoteEscapedErrorLogPath)";

        /// <summary>
        /// Replaces certain strings in the specified string with tokens for generic comparison.
        ///
        /// Current replacements :
        ///     Database Name -> $(DatabaseName)
        ///     Database Name w/ ]'s escaped -> $(BracketEscapedDatabaseName)
        ///     Database Name w/ ' escaped -> $(SingleQuoteEscapedDatabaseName)
        ///     Server Name -> $(ServerName)
        ///     Server Name w/ ]'s escaped -> $(BracketEscapedServerName)
        ///     Server Name w/ ' escaped -> $(SingleQuoteEscapedServerName)
        ///     Cluster Domain Name -> $(ClusterDomainName) (for Azure servers)
        ///     Default Data Path (Path to data files) -> $(DefaultDataPath)
        ///     T-SQL Password (PASSWORD=N'*****') -> $(TSqlPassword)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        /// <remarks>Note that the password is NOT untokenized since there's no way to know what the new password is.
        /// Instead we'll just leave the token in so that the comparison will be true. </remarks>
        public static string TokenizeString(string str, Database database)
        {
            Management.Smo.Server svr = database.Parent;
            //Valid URN server name recognized by SMO should be the server true name. For On-premises server, it equals to
            //the real server name. For Azure, it should be the part before the first dot in its full DNS name, for example:
            //"<servername>" in "<servername>.database.windows.net"
            string trueServerName = svr.ConnectionContext.TrueName;
            string internalServerName = svr.InternalName.Substring(1, svr.InternalName.Length - 2);   // svr.InternalName is enclosed in square brackets, so we remove them

            //Regex for matching a quoted-and-escaped database/server name
            //  First group is the open quote character (' or [) followed by any number of non-closing quote characters (' or ])
            //  Next is the closing quote escaped database name
            //  And the final group is any number of non-closing quote characters followed by the closing quote char
            //This is so we can correctly identify and replace quoted database/server names and replace them with the
            //appropriate generic token but this is complicated by there potentially being other characters between the quotes
            var singleQuoteQuotedDatabaseNameRegex = new Regex(@"('[^']*)" + Regex.Escape(database.Name).Replace("'", "''") + @"([^']*)", RegexOptions.IgnoreCase);
            var bracketQuotedDatabaseNameRegex = new Regex(@"(\[[^\]]*)" + Regex.Escape(database.Name).Replace("]", "]]") + @"([[^\]]*)", RegexOptions.IgnoreCase);
            var singleQuoteQuotedServerNameRegex = new Regex(@"('[^']*)" + Regex.Escape(trueServerName).Replace("'", "''") + @"([^']*)", RegexOptions.IgnoreCase);
            var bracketQuotedServerNameRegex = new Regex(@"(\[[^\]]*)" + Regex.Escape(trueServerName).Replace("]", "]]") + @"([[^\]]*)", RegexOptions.IgnoreCase);
            var bracketQuotedServerNameInternalRegex = new Regex(@"(\[[^\]]*)" + Regex.Escape(internalServerName) + @"([[^\]]*)", RegexOptions.IgnoreCase);
            var clusteredIndexNameRegex = new Regex(@"(ClusteredIndex_[a-z0-9]{32})");
            var edgeConstraintNameRegex = new Regex(@"(EC__.*__[A-Z0-9a-z]*)");
            

            var scriptDateRegex = new Regex(@"(Script Date:.* [P|A]M)", RegexOptions.IgnoreCase);
            var scriptStatsStreamRegex = new Regex(@"(STATS_STREAM = (?:0[xX])?[0-9a-fA-F]+)", RegexOptions.IgnoreCase);
            //The string used to do the replacement, $1 and $2 are the 2 groups which consist of all the matched characters that are not
            //the actual DB name (so the prefix and postfix)
            const string replacementString = "$1{0}$2";

            str = singleQuoteQuotedDatabaseNameRegex.Replace(str, replacementString.FormatStr(TOKEN_SingleQuoteEscapedDatabaseName));
            str = bracketQuotedDatabaseNameRegex.Replace(str, replacementString.FormatStr(TOKEN_BracketEscapedDatabaseName));
            //There might be instances of the database name that aren't quoted, those can just be string replaced
            str = str.Replace(database.Name, TOKEN_DatabaseName, StringComparison.CurrentCultureIgnoreCase);
            str = singleQuoteQuotedServerNameRegex.Replace(str, replacementString.FormatStr(TOKEN_SingleQuoteEscapedServerName));
            str = bracketQuotedServerNameRegex.Replace(str, replacementString.FormatStr(TOKEN_BracketEscapedServerName));
            str = bracketQuotedServerNameInternalRegex.Replace(str, replacementString.FormatStr(TOKEN_BracketEscapedServerInternalName));
            str = clusteredIndexNameRegex.Replace(str, TOKEN_ClusteredIndexName);
            str = edgeConstraintNameRegex.Replace(str, TOKEN_EdgeConstraintName);
            
            if (trueServerName.Length >= internalServerName.Length)
            {
                //There might be instances of the server name that aren't quoted, those can just be string replaced
                str = str.Replace(trueServerName, TOKEN_ServerName, StringComparison.CurrentCultureIgnoreCase);
                //There might be instances of the server name (the internal name, which could be different from the trueServerName e.g. in case of a CNAME) that aren't quoted, those can just be string replaced
                str = str.Replace(internalServerName, TOKEN_ServerInternalName, StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                //There might be instances of the server name (the internal name, which could be different from the trueServerName e.g. in case of a CNAME) that aren't quoted, those can just be string replaced
                str = str.Replace(internalServerName, TOKEN_ServerInternalName, StringComparison.CurrentCultureIgnoreCase);
                //There might be instances of the server name that aren't quoted, those can just be string replaced
                str = str.Replace(trueServerName, TOKEN_ServerName, StringComparison.CurrentCultureIgnoreCase);
            }

            str = str.Replace(svr.VersionString, TOKEN_ServerVersionString);
            str = str.Replace(svr.Version.ToString(), TOKEN_ServerVersion);
            str = str.Replace(database.ExecutionManager.ConnectionContext.ProductVersion.ToString(), TOKEN_DatabaseProductVersion);
            //For Azure servers, their full name has two parts: true server name + domain name suffix. Let's tokenize
            //the domain name suffix as well

            //If the server name has a . in it then it is domain-qualified and as such the
            //true name won't include that part. We'll tokenize the domain separately.
            int firstDot = svr.Name.IndexOf('.');
            if (firstDot >= 0)
            {
                str = ClusterDomainNameRegex.Replace(str, "$($1)" + TOKEN_ClusterDomainName);
            }

            str = scriptDateRegex.Replace(str, TOKEN_ScriptDate);
            str = scriptStatsStreamRegex.Replace(str, TOKEN_ScriptStatsStream);

            //Some scripts generate a randomly generated password - since that makes comparison difficult let's try to
            //find it and replace it with a token instead.
            int passwordIndexStart = str.IndexOf("PASSWORD=N'", StringComparison.OrdinalIgnoreCase);
            if (passwordIndexStart >= 0)
            {
                passwordIndexStart += 11;
                int curIndex = passwordIndexStart;
                while (curIndex < str.Length)
                {
                    if (str[curIndex] == '\'')
                    {
                        //Skip escaped '
                        if (curIndex + 1 < str.Length && str[curIndex + 1] == '\'')
                        {
                            curIndex += 2;
                            continue;
                        }

                        //Replace password with token
                        str = str.Substring(0, passwordIndexStart) + TOKEN_TSqlPassword + str.Substring(curIndex);
                        break;
                    }
                    curIndex++;
                }
            }

            if (svr.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {

                // 'dynamic' tokens to match different filegroups on sql MI (names are GUIDs)
                foreach (FileGroup fileGroup in database.FileGroups)
                {
                    foreach (DataFile dataFile in fileGroup.Files)
                    {
                        str = str.Replace(dataFile.FileName, $"$({ dataFile.Name }_FileName)");
                    }
                }
            }

            //Azure doesn't expose the data file paths
            if (svr.ServerType != DatabaseEngineType.SqlAzureDatabase)
            {
                var defaultDataPath = string.IsNullOrEmpty(svr.Settings.DefaultFile) ? svr.MasterDBPath : svr.Settings.DefaultFile;
                //MasterDbPath doesn't have trailing slash so add it if it doesn't exist
                defaultDataPath += defaultDataPath.EndsWith(@"\") ? "" : @"\";

                //Paths are always quoted so need to escape the apostrophe
                defaultDataPath = defaultDataPath.Replace("'", "''");
                str = str.Replace(defaultDataPath, TOKEN_DefaultDataPath, StringComparison.CurrentCultureIgnoreCase);
                // If the servername has an instance name, just grab the computer name part 
                if (trueServerName.Contains(@"\"))
                {
                    var nameComponents = trueServerName.Split('\\');
                    str = str.Replace(nameComponents[0], TOKEN_ComputerName, StringComparison.CurrentCultureIgnoreCase);
                }
                
                // Take care of the Backup and Error Log directories...
                var backupDirectory = string.IsNullOrEmpty(svr.Settings.BackupDirectory) ? svr.BackupDirectory : svr.Settings.BackupDirectory;
                var escapedBackupDirectory = backupDirectory.Replace("'", "''");
                var errorLogPath = svr.ErrorLogPath;
                var escapedErrorLogPath = errorLogPath.Replace("'", "''");

                // Order matters. in the sense that, most likely, these two strings are identical (unless one
                // day we decide to put the SQL files under a folder whose name contains single-quote characters).
                // So, first we do the replacement looking for the unescaped string, then we do the replacement
                // using the escaped strings: if the two strings are identical, we prefer the TOKEN_BackupDirectory
                // over the TOKEN_SingleQuoteEscapedBackupDirectory.
                str = str.Replace(backupDirectory, TOKEN_BackupDirectory, StringComparison.CurrentCultureIgnoreCase);
                str = str.Replace(escapedBackupDirectory, TOKEN_SingleQuoteEscapedBackupDirectory, StringComparison.CurrentCultureIgnoreCase);

                // Same log used above applies to Error Log Path as well...
                if (!string.IsNullOrEmpty(errorLogPath))
                {
                    str = str.Replace(errorLogPath, TOKEN_ErrorLogPath, StringComparison.CurrentCultureIgnoreCase);
                    str = str.Replace(escapedErrorLogPath, TOKEN_SingleQuoteEscapedErrorLogPath, StringComparison.CurrentCultureIgnoreCase);
                }

                // InstallDataDirectory must be probed last because it's contained in almost all of the other paths (except for backup dir)
                str = str
                    .Replace(svr.MasterDBPath, TOKEN_MasterDBPath)
                    .Replace(svr.MasterDBLogPath, TOKEN_MasterDBLogPath)
                    .Replace(database.LogFiles[0].FileName, TOKEN_LogFileName)
                    .Replace(database.PrimaryFilePath, TOKEN_PrimaryFilePath)
                    .Replace(svr.InstallDataDirectory, TOKEN_InstallDataDirectory);

            }

            //A workaround needed to keep testing Database.baseline.xml for SqlOnDemand
            //In the second line of scripting parameters for all targets, the SIZE randomly reports either 8192KB or 73728KB
            //Setting either of those two as the expected baseline will cause the test to fail some of the time
            if (svr.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand && str.Contains("CREATE DATABASE"))
            {
                str = Regex.Replace(str, "73728KB", "8192KB");
            }
            //Tokenize the stripped values as well
            str = TokenizeStrippedValues(str);

            return str;
        }

        /// <summary>
        /// Strips out certain values from the given string, replacing them with a token. This is
        /// intended for values which once stripped can't be Untokenized since the values are
        /// random (or otherwise unknown) when comparing the baseline at a later time.
        ///
        ///     Connection String Password (password=...;) -> $(ConnStringPassword)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string TokenizeStrippedValues(string str)
        {
            //We can't have passwords in the files so strip the ones in the connection strings out. We currently
            //don't need the actual password values when running the tests anyways as just verifying that it
            //contains a password segment is enough for our tests
            str = ConnStringPasswordRegex.Replace(str, TOKEN_ConnStringPassword);

            return str;
        }

        /// <summary>
        /// Replaces certain token strings in the specified string with their actual values.
        ///
        /// Current replacements :
        ///     $(DatabaseName) -> Database Name
        ///     $(BracketEscapedDatabaseName) -> Database Name w/ ]'s escaped
        ///     $(SingleQuoteEscapedDatabaseName) -> Database Name w/ ' escaped
        ///     $(ServerName) -> Server Name
        ///     $(BracketEscapedServerName) -> Server Name w/ ]'s escaped
        ///     $(SingleQuoteEscapedServerName) -> Server Name w/ ' escaped
        ///        For Azure servers, $(ClusterDomainName) -> Server Domain Name
        ///     $(DefaultDataPath) -> Default Data Path (Path to data files)
        ///     $(RandomGuid) -> Random GUID string
        ///     $(RandomTSqlPassword) -> Password='[Random Guid String]'
        /// </summary>
        /// <param name="str"></param>
        /// <param name="database"></param>
        /// <param name="azureKeyVaultHelper"></param>
        /// <returns></returns>
        public static string UntokenizeString(string str, Database database, AzureKeyVaultHelper azureKeyVaultHelper)
        {
            Management.Smo.Server svr = database.Parent;
            //Valid URN server name recognized by SMO should be the server true name. For On-premise server, it equals to
            //the real server name. For Azure, it should be the part before the first dot in its full DNS name, for example:
            //"<servername>" in "<servername>.database.windows.net".
            string trueServerName = svr.ConnectionContext.TrueName;
            string internalServerName = svr.InternalName.Substring(1, svr.InternalName.Length - 2);   // svr.InternalName is enclosed in square brackets, so we remove them
            // If the connection string used "." or "localhost" it will not get the host name from SQL. Linux has lowercase host names, Windows upper.
            // SQL always returns upper.
            if (internalServerName != System.Environment.MachineName)
            {
                internalServerName = internalServerName.ToUpperInvariant();
            }

            //Server names are replaced as upper case since they're case insensitive, so for ease of comparison we put them all as upper
            string ret = str
                .Replace(TOKEN_DatabaseName, database.Name, StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_BracketEscapedDatabaseName, database.Name.Replace("]", "]]"), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_SingleQuoteEscapedDatabaseName, database.Name.Replace("'", "''"), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_QuoteAndBracketEscapedDatabaseName, database.Name.Replace("'", "''").Replace("]", "]]"), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_ServerName, trueServerName.ToUpper(), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_ServerInternalName, internalServerName, StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_BracketEscapedServerName, trueServerName.ToUpper().Replace("]", "]]"), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_BracketEscapedServerInternalName, internalServerName.Replace("]", "]]"), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_SingleQuoteEscapedServerName, trueServerName.ToUpper().Replace("'", "''"), StringComparison.CurrentCultureIgnoreCase)
                .Replace(TOKEN_RandomGuid, Guid.NewGuid().ToString())
                .Replace(TOKEN_ServerVersionString, svr.VersionString)
                .Replace(TOKEN_ServerVersion, svr.Version.ToString())
                .Replace(TOKEN_DatabaseProductVersion, database.ExecutionManager.ConnectionContext.ProductVersion.ToString())
                .Replace(TOKEN_RandomTSqlPassword, String.Format("PASSWORD='{0}'", Guid.NewGuid()))
                .Replace(TOKEN_RandomTSqlSecret, String.Format("SECRET='{0}'", Guid.NewGuid()));

            //If the server is domain-qualified (we'll assume by the presence of a .) then untokenize the domain name token
            int dotIndex = svr.Name.IndexOf('.');
            if (dotIndex >= 0)
            {
                string domain = svr.Name.Substring(dotIndex);
                ret = ret.Replace(TOKEN_ClusterDomainName, domain);
            }

            // Replace SQL Mi random paths
            if (svr.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                // 'dynamic' tokens to match different filegroups on sql MI (names are GUIDs)
                foreach (FileGroup fileGroup in database.FileGroups)
                {
                    foreach (DataFile dataFile in fileGroup.Files)
                    { 
                        ret = ret.Replace($"$({ dataFile.Name }_FileName)", dataFile.FileName);
                    }
                }

                
            }


            if (svr.ServerType != DatabaseEngineType.SqlAzureDatabase)
            {
                //Azure doesn't have this property, so we'll just leave it as an empty string
                var defaultDataPath = string.IsNullOrEmpty(svr.Settings.DefaultFile) ? svr.MasterDBPath : svr.Settings.DefaultFile;
                //MasterDbPath doesn't have trailing slash so add it if it doesn't exist
                defaultDataPath += defaultDataPath.EndsWith(@"\") ? "" : @"\";

                var backupDirectory = string.IsNullOrEmpty(svr.Settings.BackupDirectory) ? svr.BackupDirectory : svr.Settings.BackupDirectory;

                var errorLogPath = svr.ErrorLogPath;

                ret = ret.Replace(TOKEN_DefaultDataPath, defaultDataPath, StringComparison.CurrentCultureIgnoreCase);
                ret = ret.Replace(TOKEN_BackupDirectory, backupDirectory, StringComparison.CurrentCultureIgnoreCase);
                ret = ret.Replace(TOKEN_SingleQuoteEscapedBackupDirectory, backupDirectory, StringComparison.CurrentCultureIgnoreCase);
                ret = ret.Replace(TOKEN_ErrorLogPath, errorLogPath, StringComparison.CurrentCultureIgnoreCase);
                ret = ret.Replace(TOKEN_SingleQuoteEscapedErrorLogPath, errorLogPath, StringComparison.CurrentCultureIgnoreCase);

                // InstallDataDirectory must be probed last because it's contained in almost all of the other paths (except for backup dir)
                ret = ret
                    .Replace(TOKEN_MasterDBPath, svr.MasterDBPath)
                    .Replace(TOKEN_MasterDBLogPath, svr.MasterDBLogPath)
                    .Replace(TOKEN_LogFileName, database.LogFiles[0].FileName)
                    .Replace(TOKEN_PrimaryFilePath, database.PrimaryFilePath)
                    .Replace(TOKEN_InstallDataDirectory, svr.InstallDataDirectory);
            }

            //Replace all of the secret store tokens with their retrieved values
            //Note we prefix the secret name with a common test prefix before retrieving it
            foreach (Match match in SecretStoreRegex.Matches(ret))
            {
                if (azureKeyVaultHelper == null)
                {
                    throw new ArgumentNullException(nameof(azureKeyVaultHelper));
                }
                var secretName = match.Groups["SecretName"].Value;
                string secretValue;
                try
                {
                    secretValue = azureKeyVaultHelper.GetDecryptedSecret(AzureKeyVaultHelper.SSMS_TEST_SECRET_PREFIX + secretName);
                }
                catch
                {
                    secretValue = azureKeyVaultHelper.GetDecryptedSecret(secretName);
                }
                ret = ret.Replace(match.Value, secretValue);
            }

            if (trueServerName.Contains(@"\"))
            {
                ret = ret.Replace(TOKEN_ComputerName, trueServerName.Split('\\')[0],
                    StringComparison.CurrentCultureIgnoreCase);
            }

            ret = ret.Replace(TOKEN_DataSource, database.ExecutionManager.ConnectionContext.ServerInstance);
            return ret;
        }

        /// <summary>
        /// Removes multi-line T-SQL comments (blocks that start with /* and end with */) and then
        /// returns a trimmed version of the new string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveMultiLineComments(string s)
        {
            return MultiLineCommentsRegex.Replace(s, String.Empty).Trim();
        }        
    }
}
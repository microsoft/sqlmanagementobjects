// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Set of helper methods for running SQL scripts which contain tokenized values
    /// </summary>
    public  class ScriptHelpers
    {
        /// <summary>
        /// Constructs a new ScriptHelpers instance
        /// </summary>
        /// <param name="azureKeyVaultHelper">The helper that fetches AKV secrets identified in the tokenized script. Can be null if the scripts have no secrets.</param>
        public ScriptHelpers(AzureKeyVaultHelper azureKeyVaultHelper = null)
        {
            this.AzureKeyVaultHelper = azureKeyVaultHelper;
        }

        private AzureKeyVaultHelper AzureKeyVaultHelper { get; }

        /// <summary>
        /// Loads an embedded script resource and runs it against the specified database
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="database"></param>
        /// <param name="asm">The assembly to load the script resources from. Defaults to the Utils assembly if null</param>
        public void LoadAndRunScriptResource(string scriptName, Database database, Assembly asm = null)
        {
           TraceHelper.TraceInformation("Running script '{0}' against database {1}", scriptName, database.Name);

            asm = asm ?? Assembly.GetExecutingAssembly();

            string[] resourceNames = asm.GetManifestResourceNames();
            Stream scriptStream = null;
            if (resourceNames != null)
            {
                string resourceName = resourceNames.Where(x => System.StringComparer.Ordinal.Equals(x, scriptName)).FirstOrDefault();

                if (resourceName != null)
                {
                    scriptStream = asm.GetManifestResourceStream(resourceName);
                }
            }

            if (scriptStream == null)
            {
                throw new FailedOperationException(string.Format("Failed to load script resource {0}", scriptName));
            }

            using (var reader = new StreamReader(scriptStream))
            {
                string script = ScriptTokenizer.UntokenizeString(reader.ReadToEnd(), database, AzureKeyVaultHelper);
                try
                {
                    database.ExecuteNonQuery(script.FixNewLines());
                }
                catch (FailedOperationException se)
                {
                    //Throw a new exception here since FailedOperationExceptions have a couple nested exceptions, so to
                    //avoid having to iterate through them ourselves and append the messages we let the test framework
                    //handle that
                    throw new FailedOperationException(string.Format("Failed to execute script {0}", scriptName), se);
                }
            }
        }
    }
}

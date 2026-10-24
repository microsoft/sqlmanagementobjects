// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.SqlScriptPublish;

namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// Provides a static method to register the Notebook ISmoScriptWriter implementation
    /// with SqlScriptGenerator. Call <see cref="Register"/> at application startup before
    /// using <see cref="ScriptDestination.ToNotebook"/>.
    /// </summary>
    public static class NotebookScriptWriterRegistration
    {
        /// <summary>
        /// Registers the <see cref="NotebookFileWriter"/> factory with
        /// <see cref="SqlScriptGenerator.NotebookWriterFactory"/> so that
        /// ScriptDestination.ToNotebook uses the Notebook assembly's writer implementation.
        /// </summary>
        public static void Register()
        {
            SqlScriptGenerator.NotebookWriterFactory = (filePath) =>
            {
                return new NotebookFileWriter(filePath);
            };
        }
    }
}

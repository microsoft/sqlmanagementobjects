// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "this class is for json serialization", Scope = "type", Target = "Microsoft.SqlServer.Management.Smo.Notebook.NotebookModel")]
namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// Minimal class to represent core features of Jupyter notebooks. 
    /// </summary>
    internal class NotebookModel
    {
        /// <summary>
        /// Metadata object for the notebook. 
        /// Will typically contain kernelspec and language_info data
        /// </summary>
        public object metadata { get; set; }

        /// <summary>
        /// Notebook format major version number
        /// </summary>
        public int nbformat { get; set; }

        /// <summary>
        /// Notebook format minor version number
        /// </summary>
        public int nbformat_minor { get; set; }

        /// <summary>
        /// The contents of the Notebook
        /// </summary>
        public IList<object> cells { get; set; }

        /// <summary>
        /// Constructs a new NotebookModel with an empty list of cells
        /// </summary>
        /// <param name="majorVersion"></param>
        /// <param name="minorVersion"></param>
        public NotebookModel(int majorVersion, int minorVersion)
        {
            nbformat = majorVersion;
            nbformat_minor = minorVersion;
            cells = new List<object>();
        }
    }
}

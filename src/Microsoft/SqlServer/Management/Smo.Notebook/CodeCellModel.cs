// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "this class is for json serialization", Scope = "module")]

namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// Represents a (simplified) cell in a Jupyter notebook
    /// </summary>
    internal class CodeCellModel : CellModel
    {
        /// <summary>
        /// Outputs from running the cell source
        /// </summary>
        public IList<object> outputs { get; set; }

        /// <summary>
        /// Number of times the cell source was run
        /// </summary>
        public int execution_count { get; set; }

        public CodeCellModel() : base("code")
        {
            outputs = new List<object>();
        }
    }
}

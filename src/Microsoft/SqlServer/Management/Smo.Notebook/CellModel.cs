// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// Base class for common cell types
    /// </summary>
    internal class CellModel
    {
        /// <summary>
        /// The type of the cell. 
        /// Will typically be markdown or code
        /// </summary>
        public string cell_type { get; set; }

        /// <summary>
        /// The contents of the cell
        /// </summary>
        public IList<string> source { get; set; }

        /// <summary>
        /// metadata associated with the cell
        /// </summary>
        public object metadata { get; set; }

        /// <summary>
        /// Constructs a Notebool cell of the given type
        /// </summary>
        /// <param name="cellType"></param>
        public CellModel(string cellType)
        {
            cell_type = cellType;
            source = new List<string>();
        }
    }
}

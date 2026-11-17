// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// A cell of type markdown
    /// </summary>
    internal class MarkdownCellModel : CellModel
    {
        /// <summary>
        /// Constructs an empty MarkdownCellModel
        /// </summary>
        public MarkdownCellModel() : base("markdown")
        {

        }
    }
}

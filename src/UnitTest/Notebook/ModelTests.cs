// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.Smo.Notebook;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoNotebookUnitTests
{
    [TestClass]
    public class ModelTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookModel_constructor_initializes_empty_content()
        {
            var model = new NotebookModel(1, 2);
            Assert.Multiple(() =>
            {
                Assert.That(model.cells, Is.Empty, nameof(model.cells));
                Assert.That(model.nbformat, Is.EqualTo(1), nameof(model.nbformat));
                Assert.That(model.nbformat_minor, Is.EqualTo(2), nameof(model.nbformat_minor));
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CodeCellModel_constructor_initializes_empty_content()
        {
            var cell = new CodeCellModel();
            Assert.Multiple(() =>
            {
                Assert.That(cell.cell_type, Is.EqualTo("code"), nameof(cell.cell_type));
                Assert.That(cell.source, Is.Empty, nameof(cell.source));
                Assert.That(cell.execution_count, Is.Zero, nameof(cell.execution_count));
                Assert.That(cell.outputs, Is.Empty, nameof(cell.outputs));
            });
        }

    }
}

// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.SqlServer.Test
{
    public class UnitTestBase
    {
        private IDisposable nUnitDisposable;

        [TestInitialize()]
        public void BaseTestInitialize()
        {
            // nunit 3 asserts tie their lifetime to the nunit test runner by default
            // we force a reset of the assert scope here 
            nUnitDisposable = new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext();
            MyTestInitialize();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (nUnitDisposable != null)
            {
                nUnitDisposable.Dispose();
            }
            MyTestCleanup();
        }

        public virtual void MyTestInitialize()
        {
        }

        public virtual void MyTestCleanup()
        {

        }
    }
}
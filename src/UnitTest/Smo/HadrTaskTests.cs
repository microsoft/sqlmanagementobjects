// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Microsoft.SqlServer.Management.HadrModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class HadrTaskTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void HadrTask_Perform_preserves_original_exception_stack_trace()
        {
            var task = new StackTraceTestTask();

            var exception = Assert.Throws<FormatException>(() => task.Perform(new RunOncePolicy()));

            Assert.That(
                exception.StackTrace,
                Does.Contain(nameof(StackTraceTestTask.ThrowException)),
                "The rethrown exception should retain its original throw site.");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void HadrTask_Rollback_preserves_original_exception_stack_trace()
        {
            var task = new StackTraceTestTask();

            var exception = Assert.Throws<FormatException>(() => task.Rollback(new RunOncePolicy()));

            Assert.That(
                exception.StackTrace,
                Does.Contain(nameof(StackTraceTestTask.ThrowException)),
                "The rethrown exception should retain its original throw site.");
        }

        private sealed class StackTraceTestTask : HadrTask
        {
            public StackTraceTestTask()
                : base(nameof(StackTraceTestTask))
            {
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowException()
            {
                throw new FormatException("Test exception");
            }

            protected override void Perform(IExecutionPolicy policy)
            {
                ThrowException();
            }

            protected override void Rollback(IExecutionPolicy policy)
            {
                ThrowException();
            }
        }
    }
}

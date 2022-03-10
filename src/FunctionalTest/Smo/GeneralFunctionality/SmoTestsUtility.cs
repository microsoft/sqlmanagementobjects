// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// SMO tests utility class
    /// </summary>
    public class SmoTestsUtility
    {
        /// <summary>
        /// Executes delegated code and verifies the expected exception is thrown 
        /// along with the expected inner exception and message
        /// </summary>
        public static void AssertInnerException<T>(TestDelegate code, string expectedInnerMessage)
        {
            var e = Assert.Throws<FailedOperationException>(code);
            var inner = e.InnerException;

            Assert.That(inner, Is.InstanceOf<T>(), string.Format("actual and expected inner exceptions have different types"));
            Assert.That(inner.Message, Is.EqualTo(expectedInnerMessage), string.Format("actual and expected inner exception messages are different"));
        }

        /// <summary>
        /// Executes delegated code and verifies SQLException is thrown 
        /// along with the expected error number
        /// </summary>
        public static void VerifyServerError(TestDelegate code, int expectedErrorNumber)
        {
            var e = Assert.Throws<FailedOperationException>(code);
            var inner1 = e.InnerException;

            Assert.That(inner1, Is.InstanceOf<ExecutionFailureException>(), string.Format("actual and expected first-level exceptions have different types"));

            var inner2 = inner1.InnerException;

            Assert.That(inner2, Is.InstanceOf<SqlException>(), string.Format("actual and expected second-level exceptions have different types"));
            Assert.That(((SqlException)inner2).Number, Is.EqualTo(expectedErrorNumber), string.Format("actual and expected error numbers are different"));
        }
    }
}

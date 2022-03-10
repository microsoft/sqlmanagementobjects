// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Keeps track of the result of tests
    /// </summary>
    public class SqlTestResult
    {
        /// <summary>
        /// Whether the test succeeded, having any failures added will cause
        /// this to return FALSE
        /// </summary>
        private bool failed;
        public bool Succeeded
        {
            get { return failureReasons.Count == 0 && !failed; }
        }
        private IList<string> failureReasons = new List<string>();

        /// <summary>
        /// Adds a new failure string to the list of failure reasons for this result. This string will be
        /// formatted with the specified options. 
        /// </summary>
        /// <param name="formatString">Format string to add</param>
        /// <param name="args">Args to pass to the format string</param>
        public void AddFailure(string formatString, params object[] args)
        {
            this.AddFailure(string.Format(formatString, args));
        }

        /// <summary>
        /// Adds a new failure string to the list of failure reasons for this result. 
        /// </summary>
        /// <param name="failureReason"></param>
        public void AddFailure(string failureReason)
        {
            this.failureReasons.Add(failureReason);
        }
        /// <summary>
        /// Adds specified failure reasons to the list of failure reasons for this result
        /// </summary>
        /// <param name="failureReasons"></param>
        public void AddFailures(params string[] failureReasons)
        {
            foreach (string failureReason in failureReasons)
            {
                this.AddFailure(failureReason);
            }
        }

        /// <summary>
        /// Invokes the given action and logs any exception message as a failure
        /// </summary>
        /// <param name="a"></param>
        public Exception HandleException(Action a)
        {
            try
            {
                a();
            }
            catch (Exception e)
            {
                this.AddFailure(e.Message);
                return e;
            }
            return null;
        }

        /// <summary>
        /// Operator for merging two TestResults together. Will &amp; their respective
        /// result values and concat their list of failure reasons. 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static SqlTestResult operator &(SqlTestResult left, SqlTestResult right)
        {
            var ret = new SqlTestResult()
            {
                failureReasons = new List<string>(left.failureReasons).Concat(right.failureReasons).ToList(),
                failed  = left.failed || right.failed
            };
            return ret;
        }

        /// <summary>
        /// A string containing all the failure reasons given, with each reason being on a separate line.
        /// </summary>
        public string FailureReasons
        {
            get
            {
                return this.failureReasons.Count > 0 ?
                    "Failure Reasons : " + Environment.NewLine + string.Join(Environment.NewLine, failureReasons) :
                    String.Empty;
            }
        }
    }
}

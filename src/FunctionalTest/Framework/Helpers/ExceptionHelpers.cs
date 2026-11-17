// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Design;
using System.Text;

using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods/values for Exceptions
    /// </summary>
    public static class ExceptionHelpers
    {
        /// <summary>
        /// Builds up an exception message string by recursively iterating through all of the InnerException
        /// children and adding their message to the string.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string BuildRecursiveExceptionMessage(this Exception e)
        {
            StringBuilder msg = new StringBuilder();

            while (e != null)
            {
                _ = msg.AppendFormat("{0} -> ", e.Message);
                if (e is AggregateException ae)
                {
                    foreach (var ex in ae.InnerExceptions)
                    {
                        _ = msg.AppendLine(ex.BuildRecursiveExceptionMessage());
                    }
                    e = null;
                }
                else
                {
                    e = e.InnerException;
                }
            }
            
            //Trim off the last ->
            if (msg.Length > 2)
            {
                msg.Length -= 3;
            }

            return msg.ToString();
        }


        /// <summary>
        /// Checks if the specified exception is the expected exception
        /// based on the exception message.
        /// </summary>
        /// <param name="e">Exception to check.</param>
        /// <param name="errorMessage">Expected exception message.</param>
        /// <returns>True, if the expected exception.  False otherwise.</returns>
        public static bool IsExpectedException(Exception e, string errorMessage)
        {
            bool expectedException = false;
            while (e != null)
            {
                // validate expected error message
                if (e.Message.Equals(errorMessage, StringComparison.OrdinalIgnoreCase))
                {
                    expectedException = true;
                    break;
                }

                e = e.InnerException;
            }

            // if the expected error message was not found, the excepted was unexpected,
            // rethrow and terminate execution
            if (!expectedException)
            {
                return false;
            }

           TraceHelper.TraceInformation("Found expected exception {0} with error message '{1}'", e.GetType().Name, errorMessage);
            return true;
        }

        /// <summary>
        /// Returns the first InnerException of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static T FirstInstanceOf<T>(this Exception e) where T : Exception
        {
            while (e != null)
            {
                if (e is T t)
                {
                    return t;
                }
                e = e.InnerException;
            }
            return null;
        }
    }
}

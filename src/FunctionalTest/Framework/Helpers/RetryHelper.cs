// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Provides utility methods for retry logic
    /// </summary>
    public class RetryHelper
    {
        /// <summary>
        /// Will retry calling a method a number of times with a delay between each one if
        /// an exception is thrown while calling the specified method. 
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="retries">Number of times to retry calling the method</param>
        /// <param name="retryDelayMs">Delay between retry attempts in milliseconds</param>
        /// <param name="retryMessage">Base message to display in logs for each failure attempt</param>
        public static void RetryWhenExceptionThrown(Action method, int retries = 3, int retryDelayMs = 1000, string retryMessage = "") => RetryWhenExceptionThrown(method, DefaultWhen, retries, retryDelayMs, retryMessage);

        /// <summary>
        /// Will retry calling a method a number of times with a delay between each one if
        /// an exception is thrown while calling the specified method. 
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="whenFunc">Exception "when" handler to decide when a retry is ok</param>
        /// <param name="retries">Number of times to retry calling the method</param>
        /// <param name="retryDelayMs">Delay between retry attempts in milliseconds</param>
        /// <param name="retryMessage">Base message to display in logs for each failure attempt</param>
        public static void RetryWhenExceptionThrown(Action method, Func<Exception, bool> whenFunc, int retries = 3, int retryDelayMs = 1000, string retryMessage = "")
        {
            if (string.IsNullOrWhiteSpace(retryMessage))
            {
                retryMessage = string.Format("Method call {0} failed.", method.Method.Name);
            }
            do
            {
                try
                {
                    method();
                    break;
                }
                catch (Exception e) when (whenFunc(e))
                {
                    if (retries-- > 0)
                    {
                       TraceHelper.TraceInformation("{0} ({1} tries left) - {2}", retryMessage, retries, e.Message);
                        Thread.Sleep(retryDelayMs);
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (true);
        }

        private static bool DefaultWhen(Exception e) => true;
        
        /// <summary>
        /// Will retry calling a method a number of times with a delay between each one if
        /// an exception is thrown while calling the specified method. This will return
        /// successfully regardless of 
        /// </summary>
        /// <param name="method">Method to invoke</param>
        /// <param name="retries">Number of times to retry calling the method</param>
        /// <param name="retryDelayMs">Delay between retry attempts in milliseconds</param>
        /// <param name="retryMessage">Base message to display in logs for each failure attempt</param>
        /// <returns>TRUE if the method executed successfully (possibly with retries). FALSE if all retry
        /// attempts failed</returns>
        public static bool RetryWhenExceptionThrownNoFail(Action method, int retries = 3, int retryDelayMs = 1000,
            string retryMessage = "")
        {
            try
            {
                RetryHelper.RetryWhenExceptionThrown(method, retries, retryDelayMs, retryMessage);
                return true;
            }
            catch (Exception e)
            {
               TraceHelper.TraceInformation("Caught exception calling retry method but purposely ignoring - {0}", e.Message);
                //Don't let exceptions bubble up for this
                return false;
            }
        }
    }
}

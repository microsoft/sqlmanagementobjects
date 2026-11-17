// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    public static class DirectoryHelpers
    {
        /// <summary>
        /// Calls delete on a directory and then waits until Directory.Exists returns false, or until
        /// the retry count is exceeded. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="retryCount"></param>
        /// <param name="retrySleepTimeMs"></param>
        /// <remarks>This is to get around an issue where the call to Directory.Delete only marks the directory for deletion, so
        /// calling that and then immediately trying to write a file to that location occasionally results in an IOException</remarks>
        public static void SynchronousDirectoryDelete(string path, int retryCount = 3, int retrySleepTimeMs = 1000)
        {
            Directory.Delete(path, true);
            //An issue was occuring where the directory wasn't fully deleted until after the call
            //to CreateDirectory - which resulted in the directory not existing when saving the files below.
            //So now wait until it shows as actually deleted (or the retry count is exceeded)
            int i = 0;
            while (Directory.Exists(path))
            {
                if (i >= retryCount)
                {
                    throw new InvalidOperationException(
                        string.Format("Directory '{0}' isn't deleted even after waiting", path));
                }
                ++i;
                Thread.Sleep(retrySleepTimeMs);
            }
        }

        /// <summary>
        /// Deletes a file, retrying when another process temporarily prevents deletion.
        /// </summary>
        /// <param name="path">The file to delete.</param>
        /// <param name="retryCount">The number of retries after the initial attempt.</param>
        /// <param name="retrySleepTimeMs">The delay between retries.</param>
        public static void SynchronousFileDelete(string path, int retryCount = 3, int retrySleepTimeMs = 1000)
        {
            for (var attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    File.Delete(path);
                    if (!File.Exists(path))
                    {
                        return;
                    }
                }
                catch (IOException) when (attempt < retryCount)
                {
                }
                catch (UnauthorizedAccessException) when (attempt < retryCount)
                {
                }

                if (attempt == retryCount)
                {
                    throw new InvalidOperationException(
                        $"File '{path}' isn't deleted even after retrying");
                }

                Thread.Sleep(retrySleepTimeMs);
            }
        }
    }
}

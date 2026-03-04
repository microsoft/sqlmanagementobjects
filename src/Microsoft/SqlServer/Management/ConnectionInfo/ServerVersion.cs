// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;

    /// <summary>
    /// server version
    /// </summary>
    [Serializable]
    public class ServerVersion
    {
        int m_nMajor;
        int m_nMinor;
        int m_nBuildNumber = 0;

        /// <summary>
        /// initializes server version
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        public ServerVersion(int major, int minor)
        {
            m_nMajor = major;
            m_nMinor = minor;
        }

        public ServerVersion(int major, int minor, int buildNumber)
        {
            m_nMajor = major;
            m_nMinor = minor;
            m_nBuildNumber = buildNumber;
        }

        /// <summary>
        /// major version
        /// </summary>
        public int Major
        {
            get { return m_nMajor; }
        }

        /// <summary>
        /// minor version
        /// </summary>
        public int Minor
        {
            get { return m_nMinor; }
        }

        public int BuildNumber
        {
            get { return m_nBuildNumber; }
        }

        /// <summary>
        /// string representation in format Major.Minor.BuildNumber Example: 11.0.1234
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}.{2}", 
                this.Major, 
                this.Minor,
                this.BuildNumber);
        }
        
        /// <summary>
        /// Explicit conversion to .Net Version type
        /// </summary>
        public static explicit operator Version(ServerVersion serverVersion)
        {
            return new Version(serverVersion.Major, serverVersion.Minor, serverVersion.BuildNumber);
        }

    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Attribute to mark a test method with the specific server versions it supports. It does this
    /// through the various Min and Max properties, which combine to make two versions 
    /// (MinMajor.MinMinor.MinBuild.MinRevision and MaxMajor.MaxMinor.MaxBuild.MaxRevision). As long
    /// as the version for a server is between (inclusive) these two versions that server is considered
    /// supported.
    /// 
    /// The MinVersion has a default of 0.0.0.0 and the MaxVersion has a default of IntMax.IntMax.IntMax.IntMax.
    /// 
    /// Any combination of the version parts can be overridden as desired. So for example if you want to run
    /// a test against all SQL2016 servers you would specify the attribute like this:
    /// 
    /// [SupportedServerVersionRange(MinMajor = 13, MaxMajor = 13)]
    /// 
    /// Or if a feature was added in a specific build you could do something like this, which would run
    /// the test only if the Major part was 13 and the Minor part was >= 300. 
    /// 
    /// [SupportedServerVersionRange(MinMajor = 13, MinBuild = 300, MaxMajor = 13)]
    /// 
    /// There is also support for specifying a range only be applicable to a certain type/edition.
    /// 
    /// [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
    /// [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
    /// 
    /// These attributes on a test method will mean a server is considered supported if the MajorVersion >= 13 
    /// OR the EngineType is Azure and the MajorVersion is >= 12.
    /// </summary>
    /// <remarks>Note that by default not using this attribute will mean ALL server versions are valid</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedServerVersionRangeAttribute : SqlSupportedDimensionAttribute
    {
        //Attributes only allow constant values as parameters so we
        //need to have each value be exposed as its own property
        private int _minmajor = 0, _maxMajor = Int32.MaxValue;
        private int _minMinor = 0, _maxMinor = Int32.MaxValue;
        private int _minBuild = 0, _maxBuild = Int32.MaxValue;
        private int _minRevision = 0, _maxRevision = Int32.MaxValue;
        private DatabaseEngineType _engineType = DatabaseEngineType.Unknown;
        private DatabaseEngineEdition _engineEdition = DatabaseEngineEdition.Unknown;

        /// <summary>
        /// Constructs a SupportedServerVersionRangeAttribute with default settings to match every server
        /// </summary>
        public SupportedServerVersionRangeAttribute()
        {
            HostPlatform = null;
        }

        /// <summary>
        /// The Major (1st) part of the MinVersion version
        /// </summary>
        public int MinMajor
        {
            get { return _minmajor; }
            set { _minmajor = value; }
        }

        /// <summary>
        /// The Major (1st) part of the MaxVersion version
        /// </summary>
        public int MaxMajor
        {
            get { return _maxMajor; }
            set { _maxMajor = value; }
        }

        /// <summary>
        /// The Minor (2nd) part of the MinVersion version
        /// </summary>
        public int MinMinor
        {
            get { return _minMinor; }
            set { _minMinor = value; }
        }

        /// <summary>
        /// The Minor (2nd) part of the MaxVersion version
        /// </summary>
        public int MaxMinor
        {
            get { return _maxMinor; }
            set { _maxMinor = value; }
        }

        /// <summary>
        /// The Build (3rd) part of the MinVersion version
        /// </summary>
        public int MinBuild
        {
            get { return _minBuild; }
            set { _minBuild = value; }
        }

        /// <summary>
        /// The Build (3rd) part of the MaxVersion version
        /// </summary>
        public int MaxBuild
        {
            get { return _maxBuild; }
            set { _maxBuild = value; }
        }

        /// <summary>
        /// The Revision (4th) part of the MinVersion version
        /// </summary>
        public int MinRevision
        {
            get { return _minRevision; }
            set { _minRevision = value; }
        }

        /// <summary>
        /// The Revision (4th) part of the MaxVersion version
        /// </summary>
        public int MaxRevision
        {
            get { return _maxRevision; }
            set { _maxRevision = value; }
        }

        /// <summary>
        /// The DatabaseEngineType this version range applies to
        /// </summary>
        public DatabaseEngineType DatabaseEngineType
        {
            get { return _engineType; }
            set { _engineType = value; }
        }

        /// <summary>
        /// The server DatabaseEngineEdition this version range applies to
        /// </summary>
        public DatabaseEngineEdition Edition
        {
            get { return _engineEdition; }
            set { _engineEdition = value; }
        }

        /// <summary>
        /// Platform supported. Null if all platforms are supported
        /// </summary>
        public string HostPlatform { get; set; }

        /// <summary>
        /// Checks that the EngineEdition matches before calling IsSupported(Server)
        /// </summary>
        /// <param name="server"></param>
        /// <param name="serverDescriptor"></param>
        /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            //Special case for Azure Datawarehouse - if the server we're checking is
            //specifically a DW server then it's only supported if the test specifically
            //says it supports Datawarehouse. This is because most tests aren't actually
            //creating a datawarehouse DB so there's no reason to run them there (it'll
            //just be a normal Azure DB, which we already have another server covering)

            //Otherwise if the attribute was set to a specific edition and the editions
            //don't match return false (not supported)
            if (
                (serverDescriptor.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse || this.Edition != DatabaseEngineEdition.Unknown) &&
                this.Edition != serverDescriptor.DatabaseEngineEdition)
            {
                return false;
            }

            // if the xml provides the MajorVersion we can avoid a server query
            if (serverDescriptor.MajorVersion > 0 &&
                (MaxMajor < serverDescriptor.MajorVersion || MinMajor > serverDescriptor.MajorVersion))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(HostPlatform) && !string.IsNullOrEmpty(serverDescriptor.HostPlatform))
            {
                if (string.Compare(HostPlatform, serverDescriptor.HostPlatform, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return false;
                }
            }

            if (this.DatabaseEngineType != DatabaseEngineType.Unknown && serverDescriptor.DatabaseEngineType != this.DatabaseEngineType)
            {
                return false;
            }
            return IsSupported(server);
        }

        /// <summary>
        /// The server is supported if server.Version >= MinVersion and server.Version less than or = MaxVersion.
        /// By default the MinVersion is 0.0.0.0 and MaxVersion is IntMax.IntMax.IntMax.IntMax (which
        /// all versions will return true for)
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server)
       
        {
            if (this.DatabaseEngineType != DatabaseEngineType.Unknown && this.DatabaseEngineType != server.ServerType)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(HostPlatform) &&
                string.Compare(HostPlatform, server.HostPlatform, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                return false;
            }
            Version serverVersion = server.Version;
            Version minVersion = new Version(MinMajor, MinMinor, MinBuild, MinRevision);
            Version maxVersion = new Version(MaxMajor, MaxMinor, MaxBuild, MaxRevision);

            return serverVersion >= minVersion && serverVersion <= maxVersion;
        }

        public override bool IsSupported(TestDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            if(this.DatabaseEngineType != DatabaseEngineType.Unknown && this.DatabaseEngineType != serverDescriptor.DatabaseEngineType)
            {
                return false;
            }

            if (Edition != DatabaseEngineEdition.Unknown && Edition != serverDescriptor.DatabaseEngineEdition)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(HostPlatform) && !string.IsNullOrEmpty(serverDescriptor.HostPlatform))
            {
                if (string.Compare(HostPlatform, serverDescriptor.HostPlatform, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return false;
                }
            }
            var serverVersion = new Version(serverDescriptor.MajorVersion, 0);
            Version minVersion = new Version(MinMajor, MinMinor, MinBuild, MinRevision);
            Version maxVersion = new Version(MaxMajor, MaxMinor, MaxBuild, MaxRevision);

            return serverVersion >= minVersion && serverVersion <= maxVersion;
        }
    }
}

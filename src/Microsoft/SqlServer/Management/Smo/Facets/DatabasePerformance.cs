// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [Sfc.DisplayNameKey("IDatabasePerformanceFacet_Name")]
    [Sfc.DisplayDescriptionKey("IDatabasePerformanceFacet_Desc")]
    public interface IDatabasePerformanceFacet : Sfc.IDmfFacet
    {
        #region Interface properties
        [Sfc.DisplayNameKey("Database_AutoCloseName")]
        [Sfc.DisplayDescriptionKey("Database_AutoCloseDesc")]
        bool AutoClose
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Database_AutoShrinkName")]
        [Sfc.DisplayDescriptionKey("Database_AutoShrinkDesc")]
        bool AutoShrink
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Database_SizeName")]
        [Sfc.DisplayDescriptionKey("Database_SizeDesc")]
        double Size
        {
            get;
        }

        [Sfc.DisplayNameKey("IDatabasePerformanceFacet_DataAndLogFilesOnSeparateLogicalVolumesName")]
        [Sfc.DisplayDescriptionKey("IDatabasePerformanceFacet_DataAndLogFilesOnSeparateLogicalVolumesDesc")]
        bool DataAndLogFilesOnSeparateLogicalVolumes
        {
            get;
        }

        [Sfc.DisplayNameKey("IDatabasePerformanceFacet_CollationMatchesModelOrMasterName")]
        [Sfc.DisplayDescriptionKey("IDatabasePerformanceFacet_CollationMatchesModelOrMasterDesc")]
        bool CollationMatchesModelOrMaster
        {
            get;
        }

        [Sfc.DisplayNameKey("Database_IsSystemObjectName")]
        [Sfc.DisplayDescriptionKey("Database_IsSystemObjectDesc")]
        bool IsSystemObject
        {
            get;
        }

        [Sfc.DisplayNameKey("Database_StatusName")]
        [Sfc.DisplayDescriptionKey("Database_StatusDesc")]
        DatabaseStatus Status
        {
            get;
        }

        #endregion

    }

    /// <summary>
    /// The Database Performance facet implements logical properties and requires overriding Refresh and Alter
    /// Thus it is a new class.
    /// </summary>
    public class DatabasePerformanceAdapter : DatabaseAdapterBase, IDmfAdapter, IDatabasePerformanceFacet
    {

        #region Constructors
        public DatabasePerformanceAdapter(Microsoft.SqlServer.Management.Smo.Database obj) 
            : base (obj)
        {
        }
        #endregion

        #region Computed Properties
        /// <summary>
        /// Returns true if the database has its data and log files on different logical volumes.
        /// </summary>
        public bool DataAndLogFilesOnSeparateLogicalVolumes
        {
            get
            {
                
                // Collect log file drives
                List<string> logFileVolumes = new List<string>();
                foreach(LogFile logFile in this.Database.LogFiles)
                {
                    string drive = GetVolume(logFile.FileName);
                    if (!String.IsNullOrEmpty(drive) && !logFileVolumes.Contains(drive))
                    {
                        logFileVolumes.Add(drive);
                    }
                }

                return this.DataFileVolumeNotIn(logFileVolumes);
            }
        }

        /// <summary>
        /// Returns true if the collation of the database matches master or model.
        /// </summary>
        public bool CollationMatchesModelOrMaster
        {
            get
            {
                Debug.Assert(null != this.Database.Parent, "Database Performance facet Database Parent object is null");
                Debug.Assert(null != this.Database.Parent.Databases["master"], "Database Performance facet master database is null");
                Debug.Assert(null != this.Database.Parent.Databases["model"], "Database Performance facet model database is null");

                return ((this.Database.Collation == this.Database.Parent.Databases["master"].Collation) || (this.Database.Collation == this.Database.Parent.Databases["model"].Collation));
            }
        }
        #endregion


        #region Refresh
        public override void Refresh()
        {
            this.Database.Refresh();

            // Refresh other items needed by facet
            this.Database.LogFiles.Refresh();
            this.Database.FileGroups.Refresh();

            Debug.Assert(null != this.Database.Parent, "Database Performance facet Database Parent object is null");
            Debug.Assert(null != this.Database.Parent.Databases["master"], "Database Performance facet master database is null");
            Debug.Assert(null != this.Database.Parent.Databases["model"], "Database Performance facet model database is null");

            this.Database.Parent.Databases["master"].Refresh();
            this.Database.Parent.Databases["model"].Refresh();
        }
        #endregion

    }
}

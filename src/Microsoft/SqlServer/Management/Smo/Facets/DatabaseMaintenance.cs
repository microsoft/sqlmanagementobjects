// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [Sfc.DisplayNameKey("IDatabaseMaintenanceFacet_Name")]
    [Sfc.DisplayDescriptionKey("IDatabaseMaintenanceFacet_Desc")]
    public interface IDatabaseMaintenanceFacet : Sfc.IDmfFacet
    {

        #region Properties
        [Sfc.DisplayNameKey("Database_RecoveryModelName")]
        [Sfc.DisplayDescriptionKey("Database_RecoveryModelDesc")]
        RecoveryModel RecoveryModel
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Database_ReadOnlyName")]
        [Sfc.DisplayDescriptionKey("Database_ReadOnlyDesc")]
        bool ReadOnly
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Database_PageVerifyName")]
        [Sfc.DisplayDescriptionKey("Database_PageVerifyDesc")]
        PageVerify PageVerify
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Database_StatusName")]
        [Sfc.DisplayDescriptionKey("Database_StatusDesc")]
        DatabaseStatus Status
        {
            get;
        }

        [Sfc.DisplayNameKey("Database_LastBackupDateName")]
        [Sfc.DisplayDescriptionKey("Database_LastBackupDateDesc")]
        DateTime LastBackupDate
        {
            get;
        }

        [Sfc.DisplayNameKey("Database_LastLogBackupDateName")]
        [Sfc.DisplayDescriptionKey("Database_LastLogBackupDateDesc")]
        DateTime LastLogBackupDate
        {
            get;
        }

        [Sfc.DisplayNameKey("IDatabaseMaintenanceFacet_DataAndBackupOnSeparateLogicalVolumesName")]
        [Sfc.DisplayDescriptionKey("IDatabaseMaintenanceFacet_DataAndBackupOnSeparateLogicalVolumesDesc")]
        bool DataAndBackupOnSeparateLogicalVolumes
        {
            get;
        }

        [Sfc.DisplayNameKey("Database_TargetRecoveryTimeName")]
        [Sfc.DisplayDescriptionKey("Database_TargetRecoveryTimeDesc")]
        Int32 TargetRecoveryTime
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("Database_DelayedDurabilityName")]
        [Sfc.DisplayDescriptionKey("Database_DelayedDurabilityDesc")]
        DelayedDurability DelayedDurability
        {
            get;
            set;
        }

        #endregion

    }

    /// <summary>
    /// The Database Maintenance facet has logical properties.  It inherts from the DatabaseAdapter class.
    /// </summary>
    public class DatabaseMaintenanceAdapter : DatabaseAdapter, IDatabaseMaintenanceFacet
    {
        #region Constructors
        public DatabaseMaintenanceAdapter(Microsoft.SqlServer.Management.Smo.Database obj) 
            : base (obj)
        {
        }
        #endregion

        #region Computed Properties
        private void AddVolumesFromMediaFamily(string mediaSetId, List<string> backupFileVolumes)
        {
            Request req = new Request();
            req.Urn = "Server/BackupMediaSet[@ID='" + Urn.EscapeString(mediaSetId) + "']/MediaFamily";

            DataTable mediaFamilyEnum = this.Database.ExecutionManager.GetEnumeratorData(req);
            foreach(DataRow mediaFamily in mediaFamilyEnum.Rows)
            {
                string physicalDrive = Convert.ToString(mediaFamily["PhysicalDeviceName"], System.Globalization.CultureInfo.InvariantCulture);
                if (!String.IsNullOrEmpty(physicalDrive))
                {
                    string volume = GetVolume(physicalDrive);
                    if (!String.IsNullOrEmpty(volume) && !backupFileVolumes.Contains(volume))
                    {
                        backupFileVolumes.Add(volume);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the database has its data and backup files on different logical volumes.
        /// </summary>
        public bool DataAndBackupOnSeparateLogicalVolumes
        {
            get
            {
                List<string> backupFileVolumes = new List<string>();
                try
                {

                    DataRowCollection Files = this.Database.EnumBackupSets().Rows;
                    // now go to each media set to determine the volumes
                    List<string> mediaSetIds = new List<string>();
                    foreach (DataRow mediaSet in Files)
                    {
                        string mediaSetId = Convert.ToString(mediaSet["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);
                        if(!mediaSetIds.Contains(mediaSetId))
                        {
                            mediaSetIds.Add(mediaSetId);
                            this.AddVolumesFromMediaFamily(mediaSetId, backupFileVolumes);
                        }
                    }
                }
                catch (Exception e)
                {
                    SqlSmoObject.FilterException(e);
                    throw new FailedOperationException(ExceptionTemplates.UnableToRetrieveBackupHistory, this, e);
                }

                return (backupFileVolumes.Count > 0) && this.DataFileVolumeNotIn(backupFileVolumes);
            }
        }

        #endregion

    }
}

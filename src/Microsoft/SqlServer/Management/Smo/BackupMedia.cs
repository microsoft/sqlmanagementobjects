// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Backup Media class
    /// </summary>
    public sealed class BackupMedia
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackupMedia"/> class.
        /// </summary>
        /// <param name="dr">The DataRow from Msdb table</param>
        internal BackupMedia(DataRow dr)
        {
            byte deviceType = (byte)dr["device_type"];

            switch (deviceType)
            {
                case 2:
                    this.mediaType = DeviceType.File;
                    break;
                case 5:
                    this.mediaType = DeviceType.Tape;
                    break;
                case 6:
                    this.mediaType = DeviceType.Pipe;
                    break;
                case 9:
                    this.mediaType = DeviceType.Url;
                    break;
                case 102:
                case 105:
                case 106:
                    this.mediaType = DeviceType.LogicalDevice;
                    break;
            }
            if (this.MediaType == DeviceType.LogicalDevice)
            {
                this.mediaName = (string)dr["logical_device_name"];
            }
            else
            {
                this.mediaName = (string)dr["physical_device_name"];
            }

            this.familySequenceNumber = (byte)dr["family_sequence_number"];

            try
            {
                //This may not be present in some version.
                this.mirrorSequenceNumber = (byte)dr["mirror"];
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupMedia"/> class.
        /// </summary>
        internal BackupMedia()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupMedia"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="backupMediaType">Type of the backup media.</param>
        internal BackupMedia(string name, DeviceType backupMediaType)
        {
            this.mediaName = name;
            this.mediaType = backupMediaType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupMedia"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="backupMediaType">Type of the backup media.</param>
        /// <param name="credentialName">SQL Server credential name</param>
        internal BackupMedia(string name, DeviceType backupMediaType, string credentialName)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new FailedOperationException(ExceptionTemplates.SetName, this, new ArgumentNullException("MediaName"));
            }
            this.mediaName = name;
            this.mediaType = backupMediaType;
            this.credentialName = credentialName;
        }

        private string mediaName = string.Empty;
        /// <summary>
        /// Gets the backup media name.
        /// </summary>
        /// <value>The name of the backup media.</value>
        public string MediaName
        {
            get
            {
                return this.mediaName;
            }
        }

        private DeviceType mediaType = DeviceType.File;
        /// <summary>
        /// Gets or sets the type of the backup media.
        /// </summary>
        /// <value>The type of the backup media.</value>
        public DeviceType MediaType
        {
            get
            {
                return this.mediaType;
            }
        }

        private byte familySequenceNumber = 0;
        /// <summary>
        /// Gets the family sequence number.
        /// </summary>
        /// <value>The family sequence number.</value>
        public byte FamilySequenceNumber
        {
            get
            {
                if (this.label != null && this.familySequenceNumber == 0)
                {
                    this.familySequenceNumber = (byte)(int)this.label.Rows[0]["FamilySequenceNumber"];
                }
                return familySequenceNumber;
            }
        }

        private string credentialName = null;
        /// <summary>
        /// Gets or sets the credential name that is used by Backup to Url
        /// </summary>
        public string CredentialName
        {
            get
            {
                return this.credentialName;
            }
            set
            {
                this.credentialName = value;
            }
        }

        private byte mirrorSequenceNumber = 0;
        /// <summary>
        /// Gets the mirror sequence number.
        /// </summary>
        /// <value>The mirror sequence number.</value>
        public byte MirrorSequenceNumber
        {
            get
            {
                return mirrorSequenceNumber;
            }
        }

        /// <summary>
        /// SQL 11 PCU1 CU2 version is 11.0.3339.0
        /// Reference: http://hotfix.partners.extranet.microsoft.com/search.aspx?search=2790947
        /// </summary>
        internal ServerVersion BackupUrlDeviceSupportedServerVersion
        {
            get
            {
                return new ServerVersion(11, 0, 3339);
            }
        }

        internal Guid mediaSetId = new Guid();
        internal Guid mediaFamilyId = new Guid();

        internal Exception ReadException = null;

        private DataTable header;
        internal DataTable MediaHeader(Server server)
        {
            if (this.header == null && this.ReadException == null)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat(SmoApplication.DefaultCulture, "RESTORE HEADERONLY FROM {0}", BackupMedia.GetBackupMediaNameForScript(this.MediaName, this.MediaType));

                sb.Append(" WITH ");

                // Backup to Cloud in SQL 11 - Add credential name if CredentialName was set
                if (this.AddCredential(server.ServerVersion, sb))
                {
                    sb.Append(Globals.commaspace);
                }

                sb.Append(" NOUNLOAD");

                try
                {
                    this.header = server.ExecutionManager.ExecuteWithResults(sb.ToString()).Tables[0];
                }
                catch (Exception ex)
                {
                    this.ReadException = ex;
                }
            }
            return this.header;
        }

        private DataTable label;
        internal DataTable MediaLabel(Server server)
        {
            if (this.label == null && this.ReadException == null)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat(SmoApplication.DefaultCulture, "RESTORE LABELONLY FROM {0}", BackupMedia.GetBackupMediaNameForScript(this.MediaName, this.MediaType));

                sb.Append(" WITH ");

                // Backup to Cloud in SQL 11 - Add credential name if CredentialName was set
                if (this.AddCredential(server.ServerVersion, sb))
                {
                    sb.Append(Globals.commaspace);
                }

                sb.Append(" NOUNLOAD");

                try
                {
                    this.label = server.ExecutionManager.ExecuteWithResults(sb.ToString()).Tables[0];
                }
                catch (Exception ex)
                {
                    this.ReadException = ex;
                }
            }
            return this.label;
        }

        internal static string GetBackupMediaNameForScript(string name, DeviceType type)
        {
            string format = null;
            bool isIdentifier = false;
            switch (type)
            {
                case DeviceType.Tape:
                    format = " TAPE = N'{0}'";
                    isIdentifier = false;
                    break;
                case DeviceType.File:
                    format = " DISK = N'{0}'";
                    isIdentifier = false;
                    break;
                case DeviceType.LogicalDevice:
                    format = " [{0}]";
                    isIdentifier = true;
                    break;
                case DeviceType.VirtualDevice:
                    format = " VIRTUAL_DEVICE = N'{0}'";
                    isIdentifier = false;
                    break;
                case DeviceType.Pipe:
                    throw new WrongPropertyValueException(ExceptionTemplates.PipeDeviceNotSupported);
                case DeviceType.Url:
                    format = " URL = N'{0}'";
                    isIdentifier = false;
                    break;
                default:
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("DeviceType"));
            }

            return String.Format(SmoApplication.DefaultCulture, format,
                            isIdentifier ? SqlSmoObject.SqlBraket(name) : SqlSmoObject.SqlString(name));
        }

        internal bool AddCredential(ServerVersion targetVersion, StringBuilder sb)
        {
            bool stringAdded = false;

            if (!String.IsNullOrEmpty(this.credentialName))
            {
                // throw if version is less than SQL 11 PCU1
                if (!IsBackupUrlDeviceSupported(targetVersion))
                {
                    throw new UnsupportedFeatureException(ExceptionTemplates.CredentialNotSupportedError(credentialName,
                        targetVersion.ToString(),
                        BackupUrlDeviceSupportedServerVersion.ToString()));
                }

                // $ISSUE - VSTS# 1040954 -Backup To URL - Investigate Supporting URL, CredentialName as an identifier
                sb.AppendFormat(SmoApplication.DefaultCulture,
                        " CREDENTIAL = N'{0}' ",
                        SqlSmoObject.SqlString(this.credentialName));

                stringAdded = true;
            }

            return stringAdded;
        }

        /// <summary>
        /// Helper to check if BackupToUrl is supported on the connected server version
        /// </summary>
        /// <param name="currentServerVersion"></param>
        /// <returns></returns>
        internal bool IsBackupUrlDeviceSupported(ServerVersion currentServerVersion)
        {
            bool urlDeviceSupported = false;

            if (currentServerVersion.Major > BackupUrlDeviceSupportedServerVersion.Major) // If Major version greater than sql 11
            {
                urlDeviceSupported = true;
            }
            else if (currentServerVersion.Major == BackupUrlDeviceSupportedServerVersion.Major) // if SQL 11
            {
                // Compare minor version and build number 
                if (currentServerVersion.Minor >= BackupUrlDeviceSupportedServerVersion.Minor &&
                    currentServerVersion.BuildNumber >= BackupUrlDeviceSupportedServerVersion.BuildNumber)
                {
                    urlDeviceSupported = true;
                }
            }

            return urlDeviceSupported;
        }
    }

    /// <summary>
    /// Backup Media Set
    /// </summary>
    public sealed class BackupMediaSet
    {
        /// <summary>
        /// Creates a BackupMediaSet object
        /// Internal Constructor for  reading the msdb header.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="mediaSetID">The media set ID.</param>
        internal BackupMediaSet(Server server, int mediaSetID)
        {
            if (null == server)
            {
                throw new ArgumentNullException("Server");
            }
            this.mediaSetID = mediaSetID;
            this.server = server;
            Populate(mediaSetID);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupMediaSet"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="backupMediaList">The backup media list.</param>
        public BackupMediaSet(Server server, List<BackupMedia> backupMediaList)
        {
            if (null == server)
            {
                throw new FailedOperationException(ExceptionTemplates.InitObject, this,
                                                    new ArgumentNullException("Server"));
            }
            if (null == backupMediaList || backupMediaList.Count == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.InitObject, this,
                                                    new ArgumentNullException("BackupMediaList"));
            }
            this.server = server;
            this.mediaType = backupMediaList[0].MediaType;
            Populate(backupMediaList);
            CheckMediaSetComplete();
        }

        /// <summary>
        /// Is the info about this backupMediaSet present in msdb
        /// </summary>
        internal bool IsPresentInMsdb = true;

        private Server server;

        //internal guid and ID
        internal Guid mediaSetGuid = new Guid();
        internal int mediaSetID = -1;

        private string name;
        /// <summary>
        /// Gets the name of the media set.
        /// </summary>
        /// <value>The name of the media set.</value>
        public string Name
        {
            get
            {
                return Name;
            }
        }

        private string description;
        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get
            {
                return description;
            }
        }

        private int mirrorCount = 0;
        /// <summary>
        /// Gets the mirror count.
        /// </summary>
        /// <value>The mirror count.</value>
        public int MirrorCount
        {
            get
            {
                return mirrorCount;
            }
        }

        private byte familyCount;
        /// <summary>
        /// Gets the number of Family Backup media present in Backup media set.
        /// </summary>
        /// <value>The family count.</value>
        public byte FamilyCount
        {
            get
            {
                return familyCount;
            }
        }

        private DeviceType mediaType;
        /// <summary>
        /// Gets the device type of the media.
        /// </summary>
        /// <value>The Media type.</value>
        public DeviceType MediaType
        {
            get
            {
                return this.mediaType;
            }
        }

        private List<BackupMedia> backupMediaList = new List<BackupMedia>();
        /// <summary>
        /// Collection of Backup media that
        /// the Backup media set contains.
        /// </summary>
        /// <value>The backup media list.</value>
        public IEnumerable<BackupMedia> BackupMediaList
        {
            get
            {
                return (IEnumerable<BackupMedia>)backupMediaList;
            }
        }

        /// <summary>
        /// Populates the properties by reading
        /// the msdb.dbo.backupmediaset table.
        /// </summary>
        private void Populate(int mediaSetID)
        {
            String query = null;
            //BackupMediaSet properties
            if (this.server.Version.Major < 9)
            {
                query = string.Format(SmoApplication.DefaultCulture,
                    "SELECT bkms.media_uuid, bkms.name, bkms.description, bkms.media_family_count FROM msdb.dbo.backupmediaset bkms WHERE bkms.media_set_id = {0}", mediaSetID);
            }
            else if (this.server.Version.Major == 9)
            {
                query = string.Format(SmoApplication.DefaultCulture,
                    "SELECT bkms.media_uuid, bkms.name, bkms.description, bkms.media_family_count, bkms.mirror_count FROM msdb.dbo.backupmediaset bkms WHERE bkms.media_set_id = {0}", mediaSetID);
            }
            else if (this.server.Version.Major <= 11)
            {
                query = string.Format(SmoApplication.DefaultCulture,
                    "SELECT bkms.media_uuid, bkms.name, bkms.description, bkms.media_family_count, bkms.mirror_count, bkms.is_compressed FROM msdb.dbo.backupmediaset bkms WHERE bkms.media_set_id = {0}", mediaSetID);
            }
            else if (this.server.Version.Major > 11)
            {
                query = string.Format(SmoApplication.DefaultCulture,
                    "SELECT bkms.media_uuid, bkms.name, bkms.description, bkms.media_family_count, bkms.mirror_count, bkms.is_compressed, bkms.is_encrypted FROM msdb.dbo.backupmediaset bkms WHERE bkms.media_set_id = {0}", mediaSetID);
            }
            DataSet dataset = server.ExecutionManager.ExecuteWithResults(query);
            DataRow datarow = dataset.Tables[0].Rows[0];
            if (datarow["name"] is string)
            {
                this.name = (string)datarow["name"];
            }
            if (datarow["description"] is string)
            {
                this.description = (string)datarow["description"];
            }
            this.familyCount = (byte)datarow["media_family_count"];
            if (datarow["media_uuid"] is Guid)
            {
                this.mediaSetGuid = (Guid)datarow["media_uuid"];
            }
            if (this.server.Version.Major >= 9)
            {
                this.mirrorCount = (byte)datarow["mirror_count"];
            }

            //BackupMedia properties
            if (this.server.Version.Major < 9)
            {
                query = string.Format(SmoApplication.DefaultCulture,
                "SELECT bkms.logical_device_name, bkms.physical_device_name, bkms.device_type, family_sequence_number FROM msdb.dbo.backupmediafamily bkms WHERE bkms.media_set_id = {0} ORDER BY bkms.family_sequence_number", mediaSetID);
            }
            else
            {
                query = string.Format(SmoApplication.DefaultCulture,
                "SELECT bkms.logical_device_name, bkms.physical_device_name, bkms.device_type, family_sequence_number, mirror FROM msdb.dbo.backupmediafamily bkms WHERE bkms.media_set_id = {0} ORDER BY bkms.family_sequence_number, bkms.mirror", mediaSetID);
            }

            DataSet ds = server.ExecutionManager.ExecuteWithResults(query);
            foreach (DataRow dr1 in ds.Tables[0].Rows)
            {
                backupMediaList.Add(new BackupMedia(dr1));
            }
            this.IsPresentInMsdb = true;
        }

        /// <summary>
        /// Populates the properties by reading
        /// the BackupMedia Headers and labels.
        /// </summary>
        /// <param name="backupMediaList">The backup media list.</param>
        private void Populate(List<BackupMedia> backupMediaList)
        {
            bool first = true;
            foreach (BackupMedia bkMedia in backupMediaList)
            {
                try
                {
                    DataTable dt = bkMedia.MediaLabel(this.server);
                    DataRow datarow = dt.Rows[0];
                    if (first)
                    {
                        if (datarow["MediaName"] is string)
                        {
                            this.name = (string)datarow["MediaName"];
                        }
                        if (datarow["MediaDescription"] is string)
                        {
                            this.description = (string)datarow["MediaDescription"];
                        }
                        this.familyCount = (byte)(int)datarow["FamilyCount"];
                        if (this.server.Version.Major >= 9)
                        {
                            this.mirrorCount = (byte)(int)datarow["MirrorCount"];
                        }
                        if (datarow["MediaSetId"] is Guid)
                        {
                            this.mediaSetGuid = (Guid)datarow["MediaSetId"];
                        }
                    }
                    else
                    {
                        Guid tguid = new Guid();
                        if (datarow["MediaSetId"] is Guid)
                        {
                            tguid = (Guid)datarow["MediaSetId"];
                        }
                        if (this.mediaSetGuid != tguid)
                        {
                            throw new SmoException(ExceptionTemplates.MediaNotPartOfSet);
                        }
                    }
                    this.backupMediaList.Add(bkMedia);
                    first = false;
                }
                catch (ExecutionFailureException) { } //Ignore if label is unreadable
            }
        }

        /// <summary>
        /// Reads the backup set header.
        /// </summary>
        /// <returns>BackupSets in the device.</returns>
        internal List<BackupSet> ReadBackupSetHeader()
        {
            List<BackupSet> ret = new List<BackupSet>();
            foreach (BackupMedia bkMedia in this.BackupMediaList)
            {
                DataTable dt = bkMedia.MediaHeader(this.server);
                if (dt != null)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        ret.Add(new BackupSet(this.server, this, dr));
                    }
                    break;
                }
            }
            return ret;
        }

        /// <summary>
        /// Checks the media set is complete.
        /// Throws IncompleteBackupMediaSetException if not.
        /// </summary>
        internal void CheckMediaSetComplete()
        {
            for (int i = 0; i < this.FamilyCount; i++)
            {
                var item = from BackupMedia bkMedia in this.BackupMediaList
                           where bkMedia.FamilySequenceNumber == i + 1
                           select bkMedia;
                if (item == null || item.Count() == 0)
                {
                    throw new IncompleteBackupMediaSetException(this, i + 1);
                }
            }
        }

        /// <summary>
        /// Incomplete backup MediaSet Exception
        /// </summary>
        public class IncompleteBackupMediaSetException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="IncompleteBackupMediaSetException"/> class.
            /// </summary>
            /// <param name="backupMediaSet">The backup media set.</param>
            /// <param name="missingFamilyNumber">The missing family number.</param>
            internal IncompleteBackupMediaSetException(BackupMediaSet backupMediaSet, int missingFamilyNumber)
            {
                this.backupMediaSet = backupMediaSet;
                this.missingFamilyNumber = missingFamilyNumber;
            }

            /// <summary>
            /// Gets a message that describes the current exception.
            /// </summary>
            /// <value></value>
            /// <returns>
            /// The error message that explains the reason for the exception, or an empty string("").
            /// </returns>
            public override string Message
            {
                get
                {
                    if (this.backupMediaSet.BackupMediaList == null || this.backupMediaSet.BackupMediaList.Count() < 1)
                    {
                        return ExceptionTemplates.BackupMediaSetEmpty;
                    }
                    StringBuilder files = new StringBuilder();
                    foreach (BackupMedia bkMedia in this.backupMediaSet.BackupMediaList)
                    {
                        files.Append(bkMedia.MediaName + "; ");
                    }

                    return ExceptionTemplates.BackupMediaSetNotComplete(files.ToString(0, files.Length - 2),
                        this.backupMediaSet.FamilyCount, this.missingFamilyNumber);
                }
            }

            private BackupMediaSet backupMediaSet;
            private int missingFamilyNumber;
        }

    }

    /// <summary>
    /// Backup Media Device Type 
    /// </summary>
    public enum DeviceType
    {
        /// <summary>
        /// Logical Device
        /// </summary>
        LogicalDevice,
        /// <summary>
        /// Tape Device
        /// </summary>
        Tape,
        /// <summary>
        /// File
        /// </summary>
        File,
        /// <summary>
        /// Pipe
        /// </summary>
        Pipe,
        /// <summary>
        /// Virtual Device
        /// </summary>
        VirtualDevice,
        /// <summary>
        /// Url
        /// </summary>
        Url
    }
}

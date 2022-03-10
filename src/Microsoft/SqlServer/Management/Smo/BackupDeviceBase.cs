// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode (Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class BackupDevice : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
	{
        internal BackupDevice(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "BackupDevice";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            InitializeKeepDirtyValues();
            Property propType = Properties.Get("BackupDeviceType");
            if (null == propType.Value)
            {
                throw new PropertyNotSetException("BackupDeviceType");
            }

            Property physName = Properties.Get("PhysicalLocation");
            if (null == physName.Value)
            {
                throw new PropertyNotSetException("PhysicalLocation");
            }

            bool skipTapeLabel = false;

            string deviceType = string.Empty;
            switch ((BackupDeviceType)propType.Value)
            {
                case BackupDeviceType.Disk:
                    deviceType = "disk";
                    break;
                case BackupDeviceType.FloppyA: goto case BackupDeviceType.FloppyB;
                case BackupDeviceType.FloppyB:
                    deviceType = "disk";
                    //deviceType = "diskette"; 
                    break;
                case BackupDeviceType.Tape:
                    {
                        deviceType = "tape";
                        Property propSkipLabel = Properties.Get("SkipTapeLabel");
                        if (null != propSkipLabel.Value)
                        {
                            skipTapeLabel = (bool)propSkipLabel.Value;
                        }
                    }

                    break;
                case BackupDeviceType.Pipe:
                    if (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version90)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.BackupToPipesNotSupported(sp.TargetServerVersionInternal.ToString()));
                    }

                    deviceType = "pipe";
                    break;
                default:
                    throw new WrongPropertyValueException(ExceptionTemplates.UnsupportedBackupDeviceType(((BackupDeviceType)propType.Value).ToString()));
            }

            StringBuilder query = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string fullNameForScripting = FormatFullNameForScripting(sp, false);

            ScriptIncludeHeaders(query, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                query.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_BACKUP_DEVICE, "NOT", fullNameForScripting);
                query.Append(sp.NewLine);
            }

            query.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_addumpdevice  @devtype = N'{0}', @logicalname = {1}, @physicalname = N'{2}'",
                                        deviceType, fullNameForScripting, SqlString((string)physName.Value));
            if (skipTapeLabel)
            {
                query.Append(", @devstatus = N'skip'");
            }

            queries.Add(query.ToString());
        }



        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            string fullNameForScripting = FormatFullNameForScripting(sp, false);

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_BACKUP_DEVICE, "", fullNameForScripting);
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_dropdevice @logicalname = {0}", fullNameForScripting);

            dropQuery.Add(sb.ToString());
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// The ReadBackupHeader method returns a QueryResults object that enumerates the contents of the media maintained by a backup device.
        /// </summary>
        /// <returns></returns>
        public DataTable ReadBackupHeader()
        {
            try
            {
                return this.ExecutionManager.ExecuteWithResults("RESTORE HEADERONLY FROM " + FormatFullNameForScripting(new ScriptingPreferences()) + 
                                                                " WITH NOUNLOAD").Tables[0];
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReadBackupHeader, this, e);
            }
        }

        /// <summary>
        /// The ReadMediaHeader method returns a QueryResults object that enumerates the values of a backup media header record.
        /// </summary>
        /// <returns></returns>
        public DataTable ReadMediaHeader()
        {
            try
            {
                return this.ExecutionManager.ExecuteWithResults("RESTORE LABELONLY FROM " + FormatFullNameForScripting(new ScriptingPreferences()) + 
                                                                " WITH NOUNLOAD").Tables[0];
            }
            catch(Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReadMediaHeader, this, e);
            }
        }
    }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Extender class for Database Snapshot
    /// </summary>
    [CLSCompliant(false)]
    public class DatabaseSnapshotExtender : SmoObjectExtender<Database>
                                          , ISfcValidate
    {
        ReadOnlyCollection<DataFile> files;
        string name;

        /// <summary>
        /// default ctor
        /// </summary>
        public DatabaseSnapshotExtender()
            : base()
        {
        }

        /// <summary>
        /// ctor. Takes parent database object to aggregate on
        /// </summary>
        /// <param name="database"></param>
        public DatabaseSnapshotExtender(Database database)
            : base(database)
        {
            this.name = database.Name;
        }

        //[ExtendedPropertyAttribute("DatabaseSnapshotBaseName")]
        [ExtendedPropertyAttribute()]
        public ReadOnlyCollection<DataFile> Files
        {
            get
            {
                if (this.files == null)
                {
                    this.files = CreateFilesCollection();
                }
                return this.files;
            }
        }

        private ReadOnlyCollection<DataFile> CreateFilesCollection()
        {
            List<DataFile>  fileList = new List<DataFile>();

            if (!string.IsNullOrEmpty(this.Parent.DatabaseSnapshotBaseName))
            {
                this.Parent.FileGroups.Clear();

                if (this.Parent.Parent != null)
                {
                    Database baseDatabase = this.Parent.Parent.Databases[this.Parent.DatabaseSnapshotBaseName];
                    if (baseDatabase != null)
                    {
                        foreach (FileGroup baseFileGroup in baseDatabase.FileGroups)
                        {
                            FileGroup fileGroup = new FileGroup(this.Parent, baseFileGroup.Name);
                            fileGroup.IsDefault = baseFileGroup.IsDefault;
                            fileGroup.ReadOnly = baseFileGroup.ReadOnly;
                            this.Parent.FileGroups.Add(fileGroup);

                            foreach (DataFile baseDataFile in baseFileGroup.Files)
                            {
                                DataFile dataFile = new DataFile(fileGroup, baseDataFile.Name);
                                dataFile.FileName = Path.Combine(Path.GetDirectoryName(baseDataFile.FileName), this.Parent.Name + "-" + (string.IsNullOrEmpty(baseDataFile.Name) ? "Primary" : baseDataFile.Name) + ".ss");
                                dataFile.Growth = baseDataFile.Growth;
                                dataFile.GrowthType = baseDataFile.GrowthType;
                                dataFile.MaxSize = baseDataFile.MaxSize;
                                dataFile.Size = baseDataFile.Size;
                                dataFile.IsPrimaryFile = baseDataFile.IsPrimaryFile;
                                fileGroup.Files.Add(dataFile);
                                fileList.Add(dataFile); 
                            }
                        }
                    }
                }
            }

            return new ReadOnlyCollection<DataFile>(fileList);
        }

        protected override void parent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DatabaseSnapshotBaseName":
                    this.files = CreateFilesCollection();
                    break;
                case "Name":
                    ProcessDataFileNames();
                    this.name = this.Parent.Name;
                    break;
            }

            base.parent_PropertyChanged(sender, e);
        }
         
        void ProcessDataFileNames()
        {
            foreach (DataFile dataFile in this.Files)
            {
                ProcessDataFileName(dataFile, this.name, this.Parent.Name);
            }
        }

        void ProcessDataFileName(DataFile dataFile, string oldName, string newName)
        {
            string oldFileName = dataFile.FileName;
            string file = Path.GetFileNameWithoutExtension(oldFileName);
            string ext = Path.GetExtension(oldFileName);

            if (file.StartsWith(oldName, StringComparison.OrdinalIgnoreCase))
            {
                dataFile.FileName = Path.Combine(Path.GetDirectoryName(oldFileName),newName + ((file.Length > oldName.Length) ? file.Substring(oldName.Length) : "") + Path.GetExtension(oldFileName));
            }
        }


        #region ISfcValidate Members

        ValidationState ISfcValidate.Validate(string methodName, params object[] arguments)
        {
            ValidationState state = this.Parent.Validate(methodName, arguments);
            
            if (string.IsNullOrEmpty(this.Parent.Name))
            {
                state.AddError(ExceptionTemplates.PropertyNotSet("Name", "Snapshot"), "Name");
            }

            return state;
        }

        #endregion
    }
}

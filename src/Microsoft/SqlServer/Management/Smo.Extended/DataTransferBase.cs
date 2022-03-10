// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class DataTransferBase : TransferBase
    {
        //Ansi Padding setting of destination server
        internal bool destinationAnsiPadding;

        internal StringCollection compensationScript;

        public DataTransferBase() : base() { }

        public DataTransferBase(Database database) : base(database) { }

        public bool LogTransferDumps { get; set; }

        internal TransferWriter GetScriptLoadedTransferWriter()
        {
            //We need destination database name for USE DB etc
            if (string.IsNullOrEmpty(this.DestinationDatabase))
            {
                throw new PropertyNotSetException("DestinationDatabase");
            }

            //Before we get the object list we should set our target server  and other properties if possible
            double? modelSize = this.SetTargetServerInfoAndGetModelSize();
            Dictionary<string, string> strAryOldDbFileNames = null;
            Dictionary<string, string> strAryOldLogFileNames = null;
            Dictionary<string, double> oldFileSizes = null;
            string oldScriptName = this.Database.ScriptName;
            this.compensationScript = null;

            //Get the list of the objects to transfer
            var urnList = this.EnumObjects(false);

            if (this.CopySchema)
            {
                //Go through list and fix full text catalogs
                this.ProcessObjectList(urnList);
            }

            //Setup Script Maker with discoverer and get it
            var scriptMaker = GetScriptMaker();

            //Create new writer to dump script to
            var writer = new TransferWriter(this, scriptMaker);

            string fileStreamFolder = null;
            try
            {
                if (this.CreateTargetDatabase)
                {
                    //Make changes to database
                    if (this.Options.TargetDatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
                    {
                        fileStreamFolder = this.SetupDatabaseForDestinationScripting(modelSize, out strAryOldDbFileNames, out strAryOldLogFileNames, out oldFileSizes);
                    }

                    this.Database.ScriptName = this.DestinationDatabase;
                    urnList.Add(this.Database.Urn);
                }

                //Script objects and collect them in writer
                writer.SetEvents();
                scriptMaker.Script(urnList, writer);
                writer.ResetEvents();

                //get compensation script
                this.ScriptCompensation(scriptMaker);
            }
            finally
            {
                if (this.CreateTargetDatabase)
                {
                    //revert changes made to database
                    if (this.Options.TargetDatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
                    {
                        this.ResetDatabaseForDestinationScripting(modelSize, strAryOldDbFileNames, strAryOldLogFileNames, oldFileSizes, fileStreamFolder);
                    }

                    this.Database.ScriptName = oldScriptName;
                }
            }

            return writer;
        }

        private void ScriptCompensation(ScriptMaker scriptMaker)
        {
            if (this.CreateTargetDatabase)
            {
                scriptMaker.Preferences.IncludeScripts.ExistenceCheck = true;
                scriptMaker.Preferences.Behavior = ScriptBehavior.Drop;
                scriptMaker.Preferences.IncludeScripts.Ddl = true;
                scriptMaker.Preferences.IncludeScripts.Data = false;
                scriptMaker.Preferences.IncludeScripts.DatabaseContext = false;

                this.compensationScript = scriptMaker.Script(new Urn[] { this.Database.Urn });
                this.compensationScript.Insert(0, string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlSmoObject.SqlBraket("master")));
            }
        }

        private void ProcessObjectList(UrnCollection urnList)
        {
            foreach (var item in urnList)
            {
                switch (item.Type)
                {
                    case "FullTextCatalog":
                        var ftc = this.Database.Parent.GetSmoObject(item) as FullTextCatalog;
                        // Default to the server path for fulltext root paths
                        // This will force it to the server default.
                        ftc.RootPath = string.Empty;
                        break;
                    default:
                        break;
                }
            }
        }

        private void ResetDatabaseForDestinationScripting(double? modelSize, Dictionary<string, string> strAryOldDbFileNames, Dictionary<string, string> strAryOldLogFileNames, Dictionary<string, double> oldFileSizes, string fileStreamFolder)
        {
            if (fileStreamFolder != null)
            {
                this.Database.FilestreamDirectoryName = fileStreamFolder;
            }

            //Restore the old values for filegroups
            if (strAryOldDbFileNames.Count > 0 || oldFileSizes.Count > 0)
            {
                foreach (FileGroup fg in this.Database.FileGroups)
                {
                    foreach (DataFile df in fg.Files)
                    {
                        string oldName = null;
                        if (strAryOldDbFileNames.TryGetValue(df.FileName, out oldName))
                        {
                            df.FileName = oldName;
                        }

                        double oldSize = 0;
                        if (oldFileSizes.TryGetValue(df.FileName, out oldSize))
                        {
                            //restore the original size
                            df.Size = oldSize;
                        }
                    }
                }
            }

            //Restore the old values for LogFiles
            if (strAryOldLogFileNames.Count > 0)
            {
                foreach (LogFile lf in this.Database.LogFiles)
                {
                    string oldName = null;
                    if (strAryOldLogFileNames.TryGetValue(lf.FileName, out oldName))
                    {
                        lf.FileName = oldName;
                    }
                }
            }
        }

        private double? SetTargetServerInfoAndGetModelSize()
        {
            double? modelDbPrimarySize = null;
            ServerConnection destinationConn = GetDestinationServerConnection();
            if (destinationConn != null)
            {
                try
                {
                    //if destination connection is available we need to set its target server info
                    //otherwise we leave it as it is                
                    destinationConn.Connect();
                    // First get the dest server
                    Server destServer = new Server(destinationConn);
                    this.destinationAnsiPadding = destServer.UserOptions.AnsiPadding;

                    this.Scripter.Options.SetTargetServerInfo(destServer, true);
                    if (destServer.DatabaseEngineType == DatabaseEngineType.Standalone)
                    {
                        modelDbPrimarySize = this.GetModelDatabasePrimaryFileSize(destServer);
                    }

                }
                catch (Exception e)
                {
                    //Since Destination Server information is not necessary we are ignoring the exception
                    if (e is OutOfMemoryException)
                    {
                        throw;
                    }
                }
                finally
                {
                    destinationConn.Disconnect();
                }
            }

            return modelDbPrimarySize;
        }

        private double GetModelDatabasePrimaryFileSize(Server destServer)
        {
            // Get destination model database before disconnecting
            Database destinationModelDb = destServer.Databases["model"];

            // Get the size of the model database primary file 
            // in the destination server
            double modelDbPrimarySize = 0;
            foreach (FileGroup fg in destinationModelDb.FileGroups)
            {
                foreach (DataFile df in fg.Files)
                {
                    if (df.IsPrimaryFile)
                    {
                        modelDbPrimarySize = df.Size;
                        break;
                    }
                }
            }
            return modelDbPrimarySize;
        }

        private string SetupDatabaseForDestinationScripting(double? modelSize, out Dictionary<string, string> strAryOldDbFileNames, out Dictionary<string, string> strAryOldLogFileNames, out Dictionary<string, double> oldFileSizes)
        {
            //Cache the File Groups and Log Files
            strAryOldDbFileNames = new Dictionary<string, string>();
            strAryOldLogFileNames = new Dictionary<string, string>();
            oldFileSizes = new Dictionary<string, double>();
            string previousFileStreamFolder = null;
            // Set database files using custom information if
            // provided by the user.
            foreach (FileGroup fg in this.Database.FileGroups)
            {
                bool setDefaultFileStream = false;
                foreach (DataFile df in fg.Files)
                {
                    if (df.IsPrimaryFile && modelSize.HasValue && df.Size < modelSize.Value)
                    {
                        // The size specified for the primary file must be at least 
                        // as large as the primary file of the model database.
                        oldFileSizes.Add(df.FileName, df.Size);
                        df.Size = modelSize.Value;
                    }

                    if (this.DatabaseFileMappings.ContainsKey(df.FileName))
                    {
                        strAryOldDbFileNames.Add(this.DatabaseFileMappings[df.FileName], df.FileName);
                        df.FileName = this.DatabaseFileMappings[df.FileName];
                        if (this.Database.IsSupportedProperty("FilestreamDirectoryName") && fg.IsFileStream && !setDefaultFileStream && this.Database.FilestreamDirectoryName!=null)
                        {
                            previousFileStreamFolder = this.Database.FilestreamDirectoryName;
                            this.Database.FilestreamDirectoryName = Path.GetFileName(df.FileName);
                            setDefaultFileStream = true;
                        }
                    }
                    else
                    {
                        // Use default information
                        if (!String.IsNullOrEmpty(this.TargetDatabaseFilePath))
                        {
                            string fileName = Path.GetFileName(df.FileName);
                            var newFileName = PathWrapper.Combine(this.TargetDatabaseFilePath, fileName);
                            strAryOldDbFileNames.Add(newFileName, df.FileName);
                            df.FileName = newFileName;
                        }
                    }
                }
            }

            foreach (LogFile lf in this.Database.LogFiles)
            {
                if (this.DatabaseFileMappings.ContainsKey(lf.FileName))
                {
                    strAryOldLogFileNames.Add(this.DatabaseFileMappings[lf.FileName], lf.FileName);
                    lf.FileName = this.DatabaseFileMappings[lf.FileName];
                }
                else
                {
                    // Use default information
                    if (!String.IsNullOrEmpty(this.TargetLogFilePath))
                    {
                        string fileName = Path.GetFileName(lf.FileName);
                        var newFileName = PathWrapper.Combine(this.TargetLogFilePath, fileName);
                        strAryOldLogFileNames.Add(newFileName, lf.FileName);
                        lf.FileName = newFileName;
                    }
                }
            }
            return previousFileStreamFolder;
        }

        private ScriptMaker GetScriptMaker()
        {
            var scriptMaker = new ScriptMaker(this.Database.Parent);
            scriptMaker.Preferences = (ScriptingPreferences)this.Scripter.Options.GetScriptingPreferences().Clone();
            scriptMaker.Preferences.IncludeScripts.Ddl = this.CopySchema;
            scriptMaker.Preferences.IncludeScripts.Data = this.CopyData;
            scriptMaker.Preferences.IncludeScripts.DatabaseContext = false;
            scriptMaker.Prefetch = false;


            if (this.DropDestinationObjectsFirst)
            {
                scriptMaker.Preferences.Behavior = ScriptBehavior.DropAndCreate;
            }
            else
            {
                scriptMaker.Preferences.Behavior = ScriptBehavior.Create;
            }


            SmoDependencyDiscoverer dependencyDiscoverer = new SmoDependencyDiscoverer(this.Database.Parent);
            dependencyDiscoverer.Preferences = scriptMaker.Preferences;
            dependencyDiscoverer.Preferences.DependentObjects = false;
            dependencyDiscoverer.filteredUrnTypes = this.Options.GetSmoUrnFilterForDiscovery(this.Database.Parent).filteredTypes;
            scriptMaker.discoverer = dependencyDiscoverer;

            return scriptMaker;
        }
    }
}


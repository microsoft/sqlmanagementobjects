// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Notebook;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// Class that implements script generation based on a SqlScriptPublishModel
    /// </summary>
    public class SqlScriptGenerator
    {
        private SqlScriptPublishModel model;
        private SqlScriptOptions scriptOptions;
        private ScriptMaker scriptMaker;
        private List<Urn> urnList;

        #region Constructor
        /// <summary>
        /// SqlScriptGenerator Constructor
        /// </summary>
        /// <param name="model">model data object</param>
        public SqlScriptGenerator(SqlScriptPublishModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            this.model = model;
            this.scriptOptions = this.model.AdvancedOptions;

            SetupScriptMaker();

        }

        private void SetupScriptMaker()
        {
            this.scriptMaker = new ScriptMaker(this.model.Server);

            ScriptingOptions so = new ScriptingOptions(this.model.Server);
            SetScriptingOptions(so);
            scriptMaker.Preferences = so.GetScriptingPreferences();

            switch (this.scriptOptions.TypeOfDataToScript)
            {
                case SqlScriptOptions.TypeOfDataToScriptOptions.SchemaAndData:
                    scriptMaker.Preferences.IncludeScripts.Ddl = true;
                    scriptMaker.Preferences.IncludeScripts.Data = true;
                    break;
                case SqlScriptOptions.TypeOfDataToScriptOptions.DataOnly:
                    scriptMaker.Preferences.IncludeScripts.Ddl = false;
                    scriptMaker.Preferences.IncludeScripts.Data = true;
                    break;
                case SqlScriptOptions.TypeOfDataToScriptOptions.SchemaOnly:
                    scriptMaker.Preferences.IncludeScripts.Ddl = true;
                    scriptMaker.Preferences.IncludeScripts.Data = false;
                    break;
            }

            switch (this.scriptOptions.ScriptCreateDrop)
            {
                case SqlScriptOptions.ScriptCreateDropOptions.ScriptCreate:
                    scriptMaker.Preferences.Behavior = ScriptBehavior.Create;
                    break;
                case SqlScriptOptions.ScriptCreateDropOptions.ScriptDrop:
                    scriptMaker.Preferences.Behavior = ScriptBehavior.Drop;
                    break;
                case SqlScriptOptions.ScriptCreateDropOptions.ScriptCreateDrop:
                    scriptMaker.Preferences.Behavior = ScriptBehavior.DropAndCreate;
                    break;
            }

            SmoDependencyDiscoverer dependencyDiscoverer = new SmoDependencyDiscoverer(this.model.Server);

            // In the case of scriptAllObjects URNs are already discovered
            dependencyDiscoverer.Preferences = this.scriptMaker.Preferences;
            dependencyDiscoverer.Preferences.DependentObjects = (so.WithDependencies && !this.model.ScriptAllObjects);
            dependencyDiscoverer.Preferences.IgnoreDependencyError = true;
            dependencyDiscoverer.filteredUrnTypes = so.GetSmoUrnFilterForDiscovery(this.model.Server).filteredTypes;

            scriptMaker.discoverer = dependencyDiscoverer;

            // Subscribe to events
            this.scriptMaker.ObjectScripting += this.OnObjectScriptingProgress;
            this.scriptMaker.ScriptingProgress += this.OnScriptingProgress;
            this.scriptMaker.ScriptingError += this.OnScriptingError;
            if ((this.scriptOptions.IncludeUnsupportedStatements == SqlScriptOptions.BooleanTypeOptions.True) &&
                ((this.scriptMaker.Preferences.TargetServerVersion != ScriptingOptions.ConvertVersion(this.model.Server.Version)) ||
                (this.scriptMaker.Preferences.TargetDatabaseEngineType != this.model.Server.DatabaseEngineType)))
            {
                this.scriptMaker.Retry += this.OnRetryRequested;
            }
        }
        #endregion

        /// <summary>
        /// GetUrnList builds up the URN list based on the values set in this object.
        /// It will either use the Transfer object to determine the list or will enumerate.
        /// </summary>
        /// <returns></returns>
        internal void GetUrnList()
        {
            Transfer transfer = GetTransfer();

            this.urnList = new List<Urn>();
            this.scriptMaker.DatabasePrefetch = this.GetDatabasePrefetch();
            this.urnList.AddRange(this.model.SelectedObjects);
            if (this.model.ScriptAllObjects)
            {
                // We always script create database when ScriptAllObjects is true.
                // unless the user has said to skip it
                if (!this.model.SkipCreateDatabase)
                {
                    // Setting ScriptDatabaseCreate to true will make ScriptUseDatabase to be true as well.
                    ((SqlTransferOptions)this.scriptOptions).ScriptDatabaseCreate = SqlScriptOptions.BooleanTypeOptions.True;

                    this.urnList.Add(this.model.Server.Databases[this.model.DatabaseName].Urn);
                }

                this.urnList.AddRange(transfer.EnumObjects(false));
            }
            else 
            {
                // we build up the dependancy collection here, we'll need it later so we keep it
                AddScriptItems();
            }
        }

        internal void DoScript(ScriptOutputOptions outputOptions)
        {
            SqlScriptPublishModelTraceHelper.Assert(outputOptions != null, "outputOptions is null");
            if (outputOptions == null)
            {
                throw new ArgumentNullException("outputOptions");
            }

            ISmoScriptWriter writer = this.GetScriptWriter(outputOptions);

            try
            {
                this.scriptMaker.Script(this.urnList.ToArray(), writer);
            }
            catch (Exception e)
            {
                throw new SqlScriptPublishException(SR.ERROR_ScriptingFailed, e);
            }
            finally
            {
                this.CloseWriter(outputOptions, writer);
            }
        }

        private void CloseWriter(ScriptOutputOptions outputOptions, ISmoScriptWriter writer)
        {
            switch (outputOptions.ScriptDestination)
            {
                case ScriptDestination.ToClipboard:
                case ScriptDestination.ToEditor:
                    SmoStringWriter stringWriter = writer as SmoStringWriter;
                    this.model.RawScript = this.GetScript(stringWriter.FinalStringCollection);
                    break;

                case ScriptDestination.ToSingleFile:
                    SingleFileWriter filewriter = writer as SingleFileWriter;
                    filewriter.Close();
                    break;

                case ScriptDestination.ToFilePerObject:
                    FilePerObjectWriter filePerObjectWriter = writer as FilePerObjectWriter;
                    filePerObjectWriter.Close();
                    break;

                case ScriptDestination.ToNotebook:
                    (writer as NotebookFileWriter).Close();
                    break;

            }
        }

        private ISmoScriptWriter GetScriptWriter(ScriptOutputOptions outputOptions)
        {
            ISmoScriptWriter writer = null;

            Encoding encoding = (outputOptions.SaveFileType == ScriptFileType.Unicode) ? Encoding.Unicode : Encoding.Default;

            switch (outputOptions.ScriptDestination)
            {
                case ScriptDestination.ToClipboard:
                case ScriptDestination.ToEditor:
                    writer = new SmoStringWriter();
                    break;

                case ScriptDestination.ToSingleFile:
                    SingleFileWriter filewriter = new SingleFileWriter(outputOptions.SaveFileName, (outputOptions.SaveFileMode == ScriptFileMode.Append), encoding)
                    {
                        BatchTerminator = this.BatchTerminator,
                        ScriptBatchTerminator = this.ScriptBatchTerminator
                    };
                    writer = filewriter;
                    break;

                case ScriptDestination.ToFilePerObject:
                    FilePerObjectWriter filePerObjectWriter = new FilePerObjectWriter(outputOptions.SaveFileName)
                    {
                        AppendToFile = (outputOptions.SaveFileMode == ScriptFileMode.Append),
                        Encoding = encoding,
                        BatchTerminator = this.BatchTerminator,
                        ScriptBatchTerminator = this.ScriptBatchTerminator
                    };
                    writer = filePerObjectWriter;
                    break;

                case ScriptDestination.ToNotebook:
                    writer = new NotebookFileWriter(outputOptions.SaveFileName)
                    {
                        BatchTerminator = BatchTerminator,
                        ScriptBatchTerminator = ScriptBatchTerminator,
                        Indented = outputOptions.Indented
                    };
                    break;

                case ScriptDestination.ToCustomWriter:
                    writer = outputOptions.CustomSmoScriptWriter;
                    break;
            }

            return writer;
        }

        private string GetScript(StringCollection stringCollection)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in stringCollection)
            {
                sb.Append(item);
                if (this.ScriptBatchTerminator)
                {
                    //Ensure the batch separator is always on a new line (to avoid syntax errors)
                    //but don't write an extra if we already have one as this can affect definitions
                    //of objects such as Stored Procedures (see TFS#9125366)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}{2}",
                        item.EndsWith(Environment.NewLine) ? string.Empty : Environment.NewLine,
                        this.BatchTerminator,
                        Environment.NewLine);
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        void OnScriptingError(object sender, ScriptingErrorEventArgs e)
        {
            this.model.OnScriptError(sender, e);
        }

        void OnScriptingProgress(object sender, ScriptingProgressEventArgs e)
        {
            this.model.OnScriptingProgress(sender, e);
        }

        void OnObjectScriptingProgress(object sender, ObjectScriptingEventArgs e)
        {
            this.model.OnObjectScriptingProgress(sender, e);
        }


        private void OnRetryRequested(object sender, RetryRequestedEventArgs e)
        {
            e.PreText = SR.InvalidScriptPreText;
            e.PostText = SR.InvalidScriptPostText;
            // set the target database engine type to the same as the server
            e.ScriptingPreferences.TargetDatabaseEngineType = this.model.Server.ServerType;
            // set the target version to match the version of the source server.
            switch (this.model.Server.VersionMajor)
            {
                case 10:
                    if (this.model.Server.VersionMinor == 50)
                    {
                        e.ScriptingPreferences.TargetServerVersion = SqlServerVersion.Version105;
                    }
                    else
                    {
                        e.ScriptingPreferences.TargetServerVersion = SqlServerVersion.Version100;
                    }
                    break;
                case 9:
                    e.ScriptingPreferences.TargetServerVersion = SqlServerVersion.Version90;
                    break;
                case 8:
                    e.ScriptingPreferences.TargetServerVersion = SqlServerVersion.Version80;
                    break;
                default:
                    e.ScriptingPreferences.TargetServerVersion = SqlServerVersion.Version105;
                    break;
            }
            e.ShouldRetry = true;
        }

        private Transfer GetTransfer()
        {
            Transfer smoTransfer = new Transfer(this.model.Server.Databases[this.model.DatabaseName])
                // Since SqlScriptPublishModel is a UI-only construct, 
                // we will script external tables even though we can't script underlying source credential secrets
                {CopyExternalTables = true};

            // if the user wanted logins we need to go get those special
            if (this.scriptOptions.ScriptLogins == SqlScriptOptions.BooleanTypeOptions.True)
            {
                smoTransfer.CopyAllLogins = true;
            }

            SetScriptingOptions(smoTransfer.Options);

            smoTransfer.PrefetchObjects = false;

            return smoTransfer;
        }

        private IDatabasePrefetch GetDatabasePrefetch()
        {
            SmoDependencyDiscoverer depDiscoverer = this.scriptMaker.discoverer as SmoDependencyDiscoverer;
            HashSet<UrnTypeKey> filteredTypes = (depDiscoverer != null) ? depDiscoverer.filteredUrnTypes : new HashSet<UrnTypeKey>();

            IDatabasePrefetch prefetch = new GswDatabasePrefetch(this.model.Server.Databases[this.model.DatabaseName], this.scriptMaker.Preferences, filteredTypes);
            return prefetch;
        }

        private void AddScriptItems()
        {
            this.urnList.AddRange(this.model.SelectedObjects);

            // Need to add logins if they wanted those
            // if the user wanted logins we need to go get those special
            if (this.scriptOptions.ScriptLogins == SqlScriptOptions.BooleanTypeOptions.True)
            {
                foreach (Login login in this.model.Server.Logins)
                {
                    if (!login.IsSystemObject)
                    {
                        this.urnList.Add(login.Urn);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the scription options from the Options object to the passed in Smo scripting options object
        ///
        /// If the Transfer scripting options and the Scripter scripting options diverge too much then this
        /// function should move into the Options class set and made virtual so that each class can update the
        /// scripting options as necessary. This function really belongs there but not moving it at this time
        /// to reduce unecessary code churn.
        /// </summary>
        /// <param name="scriptingOptions"></param>
        private void SetScriptingOptions(ScriptingOptions scriptingOptions)
        {
            scriptingOptions.AllowSystemObjects = this.model.AllowSystemObjects;

            // setting this forces SMO to correctly script objects that have been renamed
            scriptingOptions.EnforceScriptingOptions = true;

            //We always want role memberships for users and database roles to be scripted
            scriptingOptions.IncludeDatabaseRoleMemberships = true;

            scriptingOptions.Bindings = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.Bindings);

            scriptingOptions.ContinueScriptingOnError = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ContinueScriptingOnError);

            scriptingOptions.ChangeTracking = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptChangeTracking);
            scriptingOptions.IncludeHeaders = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.IncludeDescriptiveHeaders);
            scriptingOptions.IncludeScriptingParametersHeader = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.IncludeScriptingParametersHeader);
            scriptingOptions.WithDependencies = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.GenerateScriptForDependentObjects); // always have SMO determine order
            scriptingOptions.ScriptDataCompression = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptDataCompressionOptions);
            scriptingOptions.ScriptXmlCompression = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptXmlCompressionOptions);

            // VBUMP
            if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script160Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version160;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script150Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version150;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script140Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version140;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script130Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version130;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script120Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version120;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script110Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version110;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script105Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version105;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script100Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version100;
            }
            else if (this.scriptOptions.ScriptCompatibilityOption == SqlScriptOptions.ScriptCompatibilityOptions.Script90Compat)
            {
                scriptingOptions.TargetServerVersion = SqlServerVersion.Version90;
            }
            else
            {
                //If you are getting this assertion fail it means you are working for higher
                //version of SQL Server. You need to update this part of code.
                SqlScriptPublishModelTraceHelper.Assert(false, "This part of the code is not updated corresponding to latest version change");
            }

            // for cloud scripting to work we also have to have Script Compat set to 105.
            // the defaults from scripting options should take care of it
            switch (this.scriptOptions.TargetDatabaseEngineType)
            {
                case SqlScriptOptions.ScriptDatabaseEngineType.SingleInstance:
                    scriptingOptions.TargetDatabaseEngineType = DatabaseEngineType.Standalone;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineType.SqlAzure:
                    scriptingOptions.TargetDatabaseEngineType = DatabaseEngineType.SqlAzureDatabase;
                    break;
            }

            switch (this.scriptOptions.TargetDatabaseEngineEdition)
            {
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerPersonalEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Personal;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerStandardEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Standard;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerEnterpriseEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Enterprise;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerExpressEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Express;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlAzureDatabaseEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDatabase;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlDatawarehouseEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlDataWarehouse;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerStretchEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlStretchDatabase;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerManagedInstanceEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlManagedInstance;
                    break;
                case SqlScriptOptions.ScriptDatabaseEngineEdition.SqlServerOnDemandEdition:
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlOnDemand;
                    break;
                default:
                    SqlScriptPublishModelTraceHelper.Assert(scriptingOptions.TargetDatabaseEngineEdition == DatabaseEngineEdition.Standard, "The default database engine edition is Standard.");
                    scriptingOptions.TargetDatabaseEngineEdition = DatabaseEngineEdition.Standard;
                    break;
            }

            scriptingOptions.SchemaQualify = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.SchemaQualify);
            scriptingOptions.SchemaQualifyForeignKeysReferences = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.SchemaQualify);
            scriptingOptions.IncludeIfNotExists = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.IncludeIfNotExists);
            // table this.options
            // WORKAROUND: there is currently a bug in SMO that setting Options.Indexes = false stops all indexes
            // including any of the DRI indexes. To stop this we simply have to check and see if the option is already
            // false. If it is, don't set it. This shouldn't cause any problems when SMO fixes their bug but who knows for sure.
            if (scriptingOptions.Indexes != SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptIndexes))
            {
                scriptingOptions.Indexes = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptIndexes);
            }

            scriptingOptions.DriChecks = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptCheckConstraints);
            scriptingOptions.DriForeignKeys = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptForeignKeys);
            scriptingOptions.DriPrimaryKey = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptPrimaryKeys);
            scriptingOptions.DriUniqueKeys = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.UniqueKeys);
            scriptingOptions.Triggers = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptTriggers);
            scriptingOptions.NoCollation = !SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.Collation);     // the collation question and scriptingOptions this.optionsData are inverse we need to NOT the request
            scriptingOptions.DriDefaults = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.Default);
            scriptingOptions.IncludeDatabaseContext = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptUseDatabase);
            scriptingOptions.ExtendedProperties = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptExtendedProperties);
            scriptingOptions.FullTextIndexes = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptFullTextIndexes);
            scriptingOptions.ConvertUserDefinedDataTypesToBaseType = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ConvertUDDTToBaseType);
            scriptingOptions.DriIncludeSystemNames = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptDriIncludeSystemNames);
            scriptingOptions.AnsiPadding = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptAnsiPadding);
            scriptingOptions.NoVardecimal = false; //making IncludeVarDecimal true for DPW

            // scripting of stats is a combination of the Statistics
            // and the OptimizerData flag
            switch (this.scriptOptions.ScriptStatistics)
            {
                case SqlScriptOptions.ScriptStatisticsOptions.ScriptStatsAll:
                    scriptingOptions.Statistics = true;
                    scriptingOptions.OptimizerData = true;
                    break;
                case SqlScriptOptions.ScriptStatisticsOptions.ScriptStatsDDL:
                    scriptingOptions.Statistics = true;
                    scriptingOptions.OptimizerData = false;
                    break;
                case SqlScriptOptions.ScriptStatisticsOptions.ScriptStatsNone:
                    scriptingOptions.Statistics = false;
                    scriptingOptions.OptimizerData = false;
                    break;
            }

            // If Histogram and Update Statics are True then include DriIncludeSystemNames and AnsiPadding by default
            if (scriptingOptions.Statistics == true && scriptingOptions.OptimizerData == true)
            {
                scriptingOptions.DriIncludeSystemNames = true;
                scriptingOptions.AnsiPadding = true;
            }

            scriptingOptions.Permissions = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptObjectLevelPermissions);
            scriptingOptions.ScriptOwner = SqlScriptOptions.ConvertBooleanTypeOptionToBoolean(this.scriptOptions.ScriptOwner);
        }

        private string BatchTerminator
        {
            get
            {
                return (this.model.ShellScriptingOptions != null) ? this.model.ShellScriptingOptions.BatchSeparator : "GO";
            }
        }

        private bool ScriptBatchTerminator
        {
            get
            {
                return (this.model.ShellScriptingOptions != null) ? this.model.ShellScriptingOptions.DelimitStatements : true;
            }
        }
    }
}

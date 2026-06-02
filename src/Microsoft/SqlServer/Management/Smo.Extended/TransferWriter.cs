// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Smo ScriptWriter to store scripts based on transactablity
    /// </summary>
    internal class TransferWriter : ISmoScriptWriter
    {
        private TransferBase transfer;
        private ScriptMaker scriptMaker;

        public List<string> Prologue { get; private set; }
        public List<string> Epilogue { get; private set; }
        public List<string> PreTransaction { get; private set; }
        public List<string> PostTransaction { get; private set; }

        public List<Urn> Tables { get; private set; }

        private bool dataScriptingStarted = false;
        private int orderedUrns = 0;
        private bool DropMode = false;
        private bool originalExistenceCheck = false;

        private IEnumerable<string> lastScriptFragment = null;

        private delegate bool WriteToCollection(ObjectScriptingEventArgs e, out List<string> collection);

        private Dictionary<string, WriteToCollection> actions = null;

        public TransferWriter(TransferBase transfer, ScriptMaker scriptMaker)
        {
            if (transfer == null)
            {
                throw new ArgumentNullException("transfer");
            }

            if (scriptMaker == null)
            {
                throw new ArgumentNullException("scriptMaker");
            }

            this.transfer = transfer;
            this.scriptMaker = scriptMaker;

            if (scriptMaker.Preferences.Behavior == ScriptBehavior.DropAndCreate)
            {
                this.DropMode = true;
                this.originalExistenceCheck = scriptMaker.Preferences.IncludeScripts.ExistenceCheck;
                this.scriptMaker.Preferences.IncludeScripts.ExistenceCheck = true;
            }

            string useDB = string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlSmoObject.SqlBraket(this.transfer.DestinationDatabase));

            this.Prologue = new List<string> { useDB };
            this.Epilogue = new List<string> { useDB };
            this.PreTransaction = this.transfer.CreateTargetDatabase ? new List<string>() : new List<string> { useDB };
            this.PostTransaction = new List<string> { useDB };

            this.Tables = new List<Urn>();
            InitializeActions();
        }

        private void InitializeActions()
        {
            this.actions = new Dictionary<string, WriteToCollection>();

            this.actions.Add("User", this.HandleSecurityObject);
            this.actions.Add("ApplicationRole", this.HandleSecurityObject);
            this.actions.Add("Role", this.HandleSecurityObject);

            this.actions.Add("Login", this.HandleLogin);

            this.actions.Add("FullTextCatalog", this.HandleFullTextObject);
            this.actions.Add("FullTextStopList", this.HandleFullTextObject);
            this.actions.Add("SearchPropertyList", this.HandleFullTextObject);
            this.actions.Add("FullTextIndex", this.HandleFullTextObject);

            this.actions.Add("Endpoint", this.HandleEndPoint);
            this.actions.Add("Database", this.HandleDatabase);
            this.actions.Add("ExtendedProperty", this.HandleExtendedProperty);
        }

        private bool HandleSecurityObject(ObjectScriptingEventArgs e, out List<string> collection)
        {
            collection = this.PreTransaction;
            return true;
        }

        private bool HandleLogin(ObjectScriptingEventArgs e, out List<string> collection)
        {
            collection = null;
            if (this.DropMode && this.transfer.PreserveLogins)
            {
                return false;
            }
            collection = this.PreTransaction;
            return HandleSecurityObject(e, out collection);
        }

        private bool HandleFullTextObject(ObjectScriptingEventArgs e, out List<string> collection)
        {
            collection = this.DropMode ? this.PreTransaction : this.PostTransaction;
            return true;
        }

        private bool HandleEndPoint(ObjectScriptingEventArgs e, out List<string> collection)
        {
            collection = null;
            //we need to divide it into two parts and CREATE DDL to pretransaction and move ownership and permissions to post transaction
            if (this.DropMode)
            {
                collection = this.PreTransaction;
                return true;
            }
            else
            {
                var endpoint = this.transfer.Database.Parent.GetSmoObject(e.Current) as Endpoint;
                var preferences = this.scriptMaker.Preferences.Clone() as ScriptingPreferences;
                preferences.Behavior = ScriptBehavior.Create;
                preferences.IncludeScripts.Owner = false;
                preferences.IncludeScripts.Permissions = false;

                var queries = new StringCollection();
                endpoint.ScriptCreateInternal(queries, preferences, true);

                foreach (var item in queries)
                {
                    this.PreTransaction.Add(item);
                }

                queries.Clear();
                endpoint.ScriptChangeOwner(queries, preferences);
                endpoint.AddScriptPermission(queries, preferences);

                foreach (var item in queries)
                {
                    this.PostTransaction.Add(item);
                }

                return false;
            }
        }

        private bool HandleDatabase(ObjectScriptingEventArgs e, out List<string> collection)
        {
            collection = this.PreTransaction;

            //If it is destination database we need to perform some logic for various script fragments
            //If not source database then added via objectlist we need to just put them in pre transaction
            if (e.Current == this.transfer.Database.Urn)
            {
                if (this.DropMode)
                {
                    // we do not drop the destiantion database so we will ignore it
                    return false;
                }
                else
                {
                    if (e.ScriptType == ObjectScriptingType.Object)
                    {
                        if (e.Original.Parent.Type.Equals("databasereadonly"))
                        {
                            collection = this.PostTransaction;
                        }
                        else
                        {
                            // we need to remove if exists and add USE DB
                            var database = this.transfer.Database.Parent.GetSmoObject(e.Current) as Database;
                            var preferences = this.scriptMaker.Preferences.Clone() as ScriptingPreferences;
                            preferences.Behavior = ScriptBehavior.Create;
                            preferences.IncludeScripts.Owner = false;
                            preferences.IncludeScripts.Permissions = false;
                            preferences.IncludeScripts.ExistenceCheck = false;

                            var queries = new StringCollection();
                            database.ScriptCreateInternal(queries, preferences, true);

                            EnumerableContainer container = new EnumerableContainer();
                            container.Add(queries);
                            container.Add(new List<string> { string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlSmoObject.SqlBraket(this.transfer.DestinationDatabase)) });

                            this.lastScriptFragment = container;
                        }
                    }
                    else if (e.ScriptType == ObjectScriptingType.OwnerShip)
                    {
                        if (!this.transfer.PreserveDbo)
                        {
                            // we do not want to preserve ownership of the database
                            return false;
                        }
                        collection = this.PostTransaction;
                    }
                    else if (e.ScriptType == ObjectScriptingType.Permission)
                    {
                        collection = this.PostTransaction;
                    }
                }
            }

            return true;
        }

        private bool HandleExtendedProperty(ObjectScriptingEventArgs e, out List<string> collection)
        {
            collection = null;
            switch (e.Current.Type)
            {
                case "FullTextCatalog":
                case "FullTextStopList":
                case "SearchPropertyList":
                    collection = this.DropMode ? this.PreTransaction : this.PostTransaction;
                    break;
                default:
                    collection = this.DropMode ? this.PreTransaction : (this.dataScriptingStarted ? this.Epilogue : this.Prologue);
                    break;
            }
            return true;
        }

        private void scriptMaker_ScriptingProgress(object sender, ScriptingProgressEventArgs e)
        {
            if (this.scriptMaker.Preferences.Behavior == ScriptBehavior.DropAndCreate)
            {
                if (e.ProgressStage == ScriptingProgressStages.OrderingDone)
                {
                    this.orderedUrns = e.Urns.Count;
                }
            }
        }

        private void scriptMaker_ObjectScripting(object sender, ObjectScriptingEventArgs e)
        {
            this.HandleScriptingEvent(e);
            this.CheckDropCreateState(e);
        }

        private void HandleScriptingEvent(ObjectScriptingEventArgs e)
        {
            // if it is none then there is no script to write
            // we do not need to drop anything if new database is going to be created
            if (e.ScriptType != ObjectScriptingType.None
                && !(this.DropMode && this.transfer.CreateTargetDatabase && e.Current.XPathExpression.Length >= 3 && e.Current.XPathExpression[1].Name == "Database"))
            {
                List<string> collection = null;

                //We script binding with data event
                if (e.ScriptType == ObjectScriptingType.Data)
                {
                    collection = this.Epilogue;
                }
                else
                {
                    WriteToCollection action = null;

                    if (this.actions.TryGetValue(e.Current.Type, out action))
                    {
                        if (!action(e, out collection))
                        {
                            collection = null;
                        }
                    }
                    else
                    {
                        collection = this.DropMode ? this.PreTransaction : (this.dataScriptingStarted ? this.Epilogue : this.Prologue);
                    }
                }

                if (this.lastScriptFragment != null && collection != null)
                {
                    collection.AddRange(this.lastScriptFragment);
                }
            }
            this.lastScriptFragment = null;
        }

        private void CheckDropCreateState(ObjectScriptingEventArgs e)
        {
            if (this.scriptMaker.Preferences.Behavior == ScriptBehavior.DropAndCreate)
            {
                this.orderedUrns--;

                if (this.orderedUrns == 0)
                {
                    this.DropMode = false;
                    this.scriptMaker.Preferences.IncludeScripts.ExistenceCheck = this.originalExistenceCheck;
                }
            }
        }

        #region ISmoScriptWriter methods

        public void ScriptObject(IEnumerable<string> script, Urn obj)
        {
            this.lastScriptFragment = script;
        }

        public void ScriptData(IEnumerable<string> dataScript, Urn table)
        {
            this.dataScriptingStarted = true;
            this.Tables.Add(table);
        }

        public void ScriptContext(string databaseContext, Urn obj)
        {
            //Since we have to script with database context off
            //We never this getting called
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Not implemented - not used by this writer
        /// </summary>
        public string Header { private get; set; }

        #endregion

        internal void SetEvents()
        {
            this.scriptMaker.ObjectScripting += new ObjectScriptingEventHandler(scriptMaker_ObjectScripting);
            this.scriptMaker.ScriptingProgress += new ScriptingProgressEventHandler(scriptMaker_ScriptingProgress);
        }

        internal void ResetEvents()
        {
            this.scriptMaker.ObjectScripting -= new ObjectScriptingEventHandler(scriptMaker_ObjectScripting);
            this.scriptMaker.ScriptingProgress -= new ScriptingProgressEventHandler(scriptMaker_ScriptingProgress);
        }
    }
}


// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This class is a dictionary of scriptcontainer
    /// </summary>
    internal class ScriptContainerFactory
    {
        private Dictionary<Urn, ScriptContainer> scriptCollection;
        private ScriptingPreferences scriptingPreferences;
        private RetryRequestedEventHandler retry;
        private HashSet<UrnTypeKey> filteredTypes;

        public ScriptContainerFactory(ScriptingPreferences sp, HashSet<UrnTypeKey> filteredTypes, RetryRequestedEventHandler retryEvent)
        {
            this.scriptingPreferences = sp;
            this.filteredTypes = filteredTypes;
            this.retry = retryEvent;
            this.scriptCollection = new Dictionary<Urn, ScriptContainer>();
        }

        /// <summary>
        /// Method to get container based on type and store it in dictionary
        /// </summary>
        /// <param name="obj"></param>
        public void AddContainer(SqlSmoObject obj)
        {
            if (!this.filteredTypes.Contains(new UrnTypeKey(obj.Urn)))
            {
                if (this.scriptingPreferences.IncludeScripts.Ddl)
                {
                    switch (obj.Urn.Type)
                    {
                        case "Table": TableScriptContainer tableContainer = new TableScriptContainer(obj as Table, this.scriptingPreferences, this.retry);
                            scriptCollection.Add(obj.Urn, tableContainer);
                            break;
                        case "Index": IndexScriptContainer indexContainer = new IndexScriptContainer(obj as Index, this.scriptingPreferences, this.retry);
                            scriptCollection.Add(obj.Urn, indexContainer);
                            break;
                        case "View": IdBasedObjectScriptContainer viewContainer = new IdBasedObjectScriptContainer(obj, this.scriptingPreferences, this.retry);
                            scriptCollection.Add(obj.Urn, viewContainer);
                            break;
                        default: ObjectScriptContainer objectContainer = new ObjectScriptContainer(obj, this.scriptingPreferences, this.retry);
                            scriptCollection.Add(obj.Urn, objectContainer);
                            break;
                    }
                }
                else
                {
                    if (obj.Urn.Type.Equals(Table.UrnSuffix))
                    {
                        TableScriptContainer tableContainer = new TableScriptContainer(obj as Table, this.scriptingPreferences, this.retry);
                        scriptCollection.Add(obj.Urn, tableContainer);
                    }
                }
            }
        }

        public bool TryGetValue(Urn key, out ScriptContainer value)
        {
            return this.scriptCollection.TryGetValue(key, out value);
        }
    }
}
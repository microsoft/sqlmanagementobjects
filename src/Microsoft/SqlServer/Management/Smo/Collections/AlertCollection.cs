// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System.Collections.Specialized;

namespace Microsoft.SqlServer.Management.Smo.Agent
{

    public sealed partial class AlertCollection : SimpleObjectCollectionBase<Alert, JobServer>
    {
        /// <summary>
        /// Returns the set of TSQL statements needed to create the Alerts, using default ScriptingOptions
        /// </summary>
        /// <returns></returns>
        public StringCollection Script() => Script(new ScriptingOptions());

        /// <summary>
        /// Returns the set of TSQL statements needed to create the Alerts, using the provided ScriptingOptions
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            if (Count <= 0)
            {
                return new StringCollection();
            }

            var scriptList = new SqlSmoObject[Count];
            var i = 0;
            foreach (SqlSmoObject o in this)
            {
                scriptList[i++] = o;
            }
            var scr = new Scripter(scriptList[0].GetServerObject())
            {
                Options = scriptingOptions
            };
            return scr.Script(scriptList);
        }

    }
}

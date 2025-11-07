// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    public sealed partial class OperatorCollection : SimpleObjectCollectionBase<Operator, JobServer>
    {

        ///<summary>
        /// Returns a TSQL script for creating the members of the collection, using default scripting options
        ///</summary>
        public StringCollection Script()
        {
            return Script(new ScriptingOptions());
        }

        ///<summary>
        /// Returns a TSQL script for creating the members of the collection, using the provided scripting options
        ///</summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            if( this.Count <= 0 )
            {
                return new StringCollection();
            }

            SqlSmoObject [] scriptList = new SqlSmoObject[this.Count];
            int i = 0;
            foreach(SqlSmoObject o in this)
            {
                scriptList[i++] = o;
            }
            Scripter scr = new Scripter(scriptList[0].GetServerObject());
            scr.Options = scriptingOptions;
            return scr.Script(scriptList);
        }

    }
}

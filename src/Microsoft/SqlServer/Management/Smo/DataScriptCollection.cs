// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// DataScriptCollection is a special collection that manages the
    /// writing of (potentially memory intensive) table data.
    /// </summary>
    internal class DataScriptCollection : IEnumerable<string>
    {

        private DataEnumerator dataEnumerator;
        private Table table;
        private ScriptingPreferences options;

        /// <summary>
        /// Creates a new instance of DataScriptCollection
        /// </summary>
        /// <param name="table">Table that we want to script out data values for</param>
        /// <param name="options">Scipting options specified for this table</param>
        public DataScriptCollection(Table table, ScriptingPreferences options)
        {
            if (table == null)
            {
                throw new ArgumentNullException(ExceptionTemplates.NullParameterTable);
            }

            if (options == null)
            {
                throw new ArgumentNullException(ExceptionTemplates.NullParameterScriptingOptions);
            }

            this.table = table;
            this.options = options;
        }

        /// <summary>
        /// Creates a new instance of DataScriptCollection
        /// </summary>
        /// <param name="dataEnumerator"></param>
        public DataScriptCollection(DataEnumerator dataEnumerator)
        {
            if (dataEnumerator == null)
            {
                throw new ArgumentNullException("dataEnumerator");
            }

            this.dataEnumerator = dataEnumerator;
        }

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            if (this.dataEnumerator == null)
            {
                this.dataEnumerator = new DataEnumerator(this.table, this.options);
            }
            return this.dataEnumerator;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (this.dataEnumerator == null)
            {
                this.dataEnumerator = new DataEnumerator(this.table, this.options);
            }

            return this.dataEnumerator;
        }

        #endregion
    }


}


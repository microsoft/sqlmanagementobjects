// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Extender class for Smo Objects
    /// </summary>
    [CLSCompliant(false)]
    public class SmoObjectExtender<T> : SfcObjectExtender<T> where T: SqlSmoObject, new()
    {
        /// <summary>
        /// default ctor
        /// </summary>
        public SmoObjectExtender()
            : base()
        {
            Initialize();
        }

        /// <summary>
        /// ctor. Takes parent database object to aggregate on
        /// </summary>
        /// <param name="database"></param>
        public SmoObjectExtender(T obj)
            : base(obj)
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // insert 'Name' into collection if T: NamedSmoObject
                RegisterParentProperty(typeof(T).GetProperty("Name"));
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Adds the Alter script of the child objects to the script.
        /// </summary>
        /// <param name="scriptParent">Whether to also script Alter for the parent object</param>
        protected void PropagateAlterToChildren(StringCollection script, bool scriptParent = false)
        {
            var preferences = new ScriptingPreferences(Parent);
            if (scriptParent)
            {
                Parent.ScriptAlter(script, preferences);
            }

            Parent.PropagateScript(script, preferences, SqlSmoObject.PropagateAction.Alter);
        }

        protected override ISfcPropertySet GetParentSfcPropertySet()
        {
            // if parent's object is not in 'correct' state - we have to use alternative way to get 
            // property provider
            if (this.Parent != null)
            {
                try
                {
                    this.Parent.CheckPendingState();
                }
                catch
                {
                    return new SqlPropertyCollection(this.Parent, this.Parent.GetPropertyMetadataProvider());
                }
            }

            return base.GetParentSfcPropertySet();
        }
    }
}

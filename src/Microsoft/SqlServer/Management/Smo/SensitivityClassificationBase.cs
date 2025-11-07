// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class SensitivityClassification : NamedSmoObject, IDroppable, IScriptable
    {
        internal SensitivityClassification(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return nameof(SensitivityClassification);
            }
        }

        /// <summary>
        /// Drops the object from the database
        /// </summary>
        public void Drop()
        {
            DropImpl();
        }

        public StringCollection Script() 
        {
            return ScriptImpl();
        }

        public StringCollection Script(ScriptingOptions scriptingOptions) 
        {
            return ScriptImpl(scriptingOptions);
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp) 
        {
            DataClassificationScriptGenerator generator = DataClassificationScriptGenerator.Create(this, sp);
            queries.Add(generator.Add());
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp) 
        {
            DataClassificationScriptGenerator generator = DataClassificationScriptGenerator.Create(this, sp);
            queries.Add(generator.Drop());
        }
    }
}
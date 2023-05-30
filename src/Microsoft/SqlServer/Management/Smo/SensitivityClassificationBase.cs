// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;
using System.Collections.Specialized;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class SensitivityClassification : NamedSmoObject, IScriptable
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
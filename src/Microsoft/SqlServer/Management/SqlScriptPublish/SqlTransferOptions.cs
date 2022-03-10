// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// TransferOptions is the set of options that are unique to the ScriptTransfer task in SMO
    /// It is derived from the SqlScriptOptions for all common options and behavior
    /// </summary>
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.SqlScriptPublish.SqlScriptOptionsSR")]
    internal sealed class SqlTransferOptions : SqlScriptOptions
    {
        private BooleanTypeOptions scriptDatabaseCreate = BooleanTypeOptions.False;

        public SqlTransferOptions(Version version)
            : base(version)
        {
            // This option set is used for transfering all objects and in
            // this case we need to set WithDependancies and AllIndexes to true
            // or the resulting script will likely not run because of either
            // ordering issues or missing indexes. So we default those to true
            // for this case.
            base.GenerateScriptForDependentObjects = SqlScriptOptions.BooleanTypeOptions.True;
            base.ScriptIndexes = SqlScriptOptions.BooleanTypeOptions.True;
        }

        [DisplayNameKey("ScriptDatabaseCreate")]
        [DisplayDescriptionKeyAttribute("genreatescriptdatabasecreate")]
        [DisplayCategoryKey("General")]
        [Browsable(false)]
        public BooleanTypeOptions ScriptDatabaseCreate
        {
            get
            {
                return this.scriptDatabaseCreate;
            }
            set
            {
                this.scriptDatabaseCreate = value;

                // we want to auto-default the USE database to true if they choose
                // to create a database
                if (this.scriptDatabaseCreate == BooleanTypeOptions.True)
                {
                    this.ScriptUseDatabase = BooleanTypeOptions.True;
                }
            }
        }
    }
}



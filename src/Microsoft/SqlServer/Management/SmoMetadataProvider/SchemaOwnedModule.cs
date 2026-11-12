// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class SchemaOwnedModule<S> : SchemaOwnedObject<S>
        where S : Smo.ScriptSchemaObjectBase
    {
        private bool moduleInfoRetrieved;
        private IDictionary<string, object> moduleInfo;

        protected SchemaOwnedModule(S smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
        }

        protected IDictionary<string, object> GetModuleInfo()
        {
            if (!this.moduleInfoRetrieved)
            {
                string definitionText = Utils.Module.GetDefinitionTest(this.m_smoMetadataObject);
                
                if (!string.IsNullOrEmpty(definitionText))
                {
                    this.moduleInfo = ParseUtils.RetrieveModuleDefinition(definitionText, new ParseOptions(string.Empty, this.IsQuotedIdentifierOn));
                }

                this.moduleInfoRetrieved = true;
            }

            return this.moduleInfo;
        }

        public abstract bool IsQuotedIdentifierOn { get; }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class ExtendedStoredProcedure : SchemaOwnedObject<Smo.ExtendedStoredProcedure>, IExtendedStoredProcedure
    {
        public ExtendedStoredProcedure(Smo.ExtendedStoredProcedure smoMetadataObject, Schema parent)
            : base(smoMetadataObject, parent)
        {
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return this.m_smoMetadataObject.IsSystemObject; }
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region ICallableModule Members
        public CallableModuleType ModuleType
        {
            get { return CallableModuleType.ExtendedStoredProcedure; }
        }

        public IScalarDataType ReturnType
        {
            get
            {
                // the return type of all stored procedures is int
                return SmoSystemDataTypeLookup.Instance.Int;
            }
        }

        public IMetadataOrderedCollection<IParameter> Parameters
        {
            // we have no info whatsoever on parameters so we'll return empty collection
            get { return Collection<IParameter>.EmptyOrdered; }
        }
        #endregion

        #region IUserDefinedFunctionModuleBase Members
        public bool IsEncrypted
        {
            get { return false; }
        }

        public IExecutionContext ExecutionContext
        {
            get { return null; }
        }
        #endregion
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlParser.Metadata;


namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class StoredProcedure : SchemaOwnedModule<Smo.StoredProcedure>, IStoredProcedure
    {
        private IMetadataOrderedCollection<IParameter> m_parameters;
        private IExecutionContext m_executionContext;
        private string bodyText;
        private bool isBodyTextSet;

        public StoredProcedure(Smo.StoredProcedure smoMetadataObject, Schema parent)
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
            get { return CallableModuleType.StoredProcedure; }
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
            get
            {
                if (this.m_parameters == null)
                {
                    Database database = this.Parent.Parent;
                    this.m_parameters = Utils.StoredProcedure.CreateParameterCollection(
                        database, this.m_smoMetadataObject);
                }

                return this.m_parameters;
            }
        }
        #endregion

        #region IUserDefinedFunctionModuleBase Members
        public bool IsEncrypted
        {
            get { return this.m_smoMetadataObject.IsEncrypted; }
        }

        public IExecutionContext ExecutionContext
        {
            get
            {
                if (this.m_executionContext == null)
                {
                    Database database = this.m_parent.Parent;
                    this.m_executionContext = Utils.GetExecutionContext(database, this.m_smoMetadataObject);
                }

                Debug.Assert(this.m_executionContext != null, "SmoMetadataProvider Assert", "executionContext != null");

                return this.m_executionContext;
            }
        }
        #endregion

        #region IStoredProcedure Members
        public string BodyText
        {
            get
            {
                // SMO will throw an exception if this property is accessed for CLR SPs
                // Parser cannot accept null/encrypted sql for parsing
                if (this.HasBodyText() &&
                    !this.isBodyTextSet)
                {
                    string sql;
                    if (Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "Text", out sql))
                    {
                        Debug.Assert(sql != null, "SmoMetadataProvider Assert", "sql != null");

                        this.bodyText = Utils.RetriveStoredProcedureBody(sql, this.IsQuotedIdentifierOn);
                    }
                    else
                    {
                        this.bodyText = null;
                    }
                    this.isBodyTextSet = true;
                }
                return this.bodyText;
            }
        }

        public bool ForReplication
        {
            get { return this.m_smoMetadataObject.ForReplication; }
        }

        public bool IsSqlClr
        {
            // todo: bug 281326 tracks this
            // SMO will throw if this property is accessed for design mode
            get 
            {
                ISfcSupportsDesignMode intfObj = this.m_smoMetadataObject as ISfcSupportsDesignMode;
                if ((null != intfObj) && (intfObj.IsDesignMode))
                {
                    return false;
                }
                return this.m_smoMetadataObject.ImplementationType == Smo.ImplementationType.SqlClr; 
            }
        }

        public bool IsRecompiled
        {
            get
            {
                // SMO will throw an exception if this property is accessed for CLR SPs
                return this.IsSqlClr? false : this.m_smoMetadataObject.Recompile;
            }
        }

        public bool Startup
        {
            get { return this.m_smoMetadataObject.Startup; }
        }

        public override bool IsQuotedIdentifierOn
        {
            get 
            {
                // SMO will throw an exception if this property is accessed for CLR SPs
                return this.IsSqlClr ? false : this.m_smoMetadataObject.QuotedIdentifierStatus; 
            }
        }
        #endregion

        private bool HasBodyText()
        {
            return !this.IsSqlClr && !this.IsEncrypted;
        }
    }
}

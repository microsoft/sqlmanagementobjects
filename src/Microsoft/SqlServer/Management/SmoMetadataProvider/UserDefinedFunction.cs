// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    abstract class UserDefinedFunction : SchemaOwnedModule<Smo.UserDefinedFunction>, IUserDefinedFunction
    {
        private IExecutionContext m_executionContext;
        private string bodyText;
        private bool isBodyTextSet;

        protected UserDefinedFunction(Smo.UserDefinedFunction function, Schema schema)
            : base(function, schema)
        {
            schema.AssertNameMatches(function.Schema, 
                "Schema object name does not match schema property of Smo.UserDefinedFunction!");
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return this.m_smoMetadataObject.IsSystemObject; }
        }

        public bool IsTableValuedFunction
        {
            get
            {
                return (this.m_smoMetadataObject.FunctionType == Smo.UserDefinedFunctionType.Table) ||
                    (this.m_smoMetadataObject.FunctionType == Smo.UserDefinedFunctionType.Inline);
            }
        }

        public bool IsScalarValuedFunction
        {
            get
            {
                return this.m_smoMetadataObject.FunctionType == Smo.UserDefinedFunctionType.Scalar;
            }
        }

        #region IUserDefinedFunction Members
        public string BodyText
        {
            get
            {
                // SMO will throw an exception if this property is accessed for CLR UDFs
                if (this.HasBodyText() &&
                    !this.isBodyTextSet)
                {
                    string sql;
                    if (Utils.TryGetPropertyObject<string>(this.m_smoMetadataObject, "Text", out sql))
                    {
                        Debug.Assert(sql != null, "SmoMetadataProvider Assert", "sql != null");

                        this.bodyText = Utils.RetriveFunctionBody(sql, this.IsQuotedIdentifierOn);
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

        public bool IsSchemaBound
        {
            get
            {
                // SMO throws an exception if we access this property for SqlClr 
                // functions. We will return false for those functions.
                return this.IsSqlClr ? false : this.m_smoMetadataObject.IsSchemaBound;
            }
        }

        public bool IsSqlClr
        {
            // SMO throws on ImplementationMode if in design mode
            // todo: 281326 tracks this
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

        public override bool IsQuotedIdentifierOn
        {
            get 
            {
                // SMO will throw an exception if this property is accessed for SqlClr functions.
                return this.IsSqlClr ? false : this.m_smoMetadataObject.QuotedIdentifierStatus; 
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

        #region IFunction Members
        abstract public IMetadataOrderedCollection<IParameter> Parameters { get;}
        #endregion

        private bool HasBodyText()
        {
            return !this.IsSqlClr && !this.IsEncrypted;
        }
    }
}

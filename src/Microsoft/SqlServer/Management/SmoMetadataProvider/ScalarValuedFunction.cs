// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class ScalarValuedFunction : UserDefinedFunction, IScalarValuedFunction
    {
        private IScalarDataType m_dataType;
        private ParameterCollection m_parameters;


        /// <summary>
        /// Creates a new instance of ScalarValuedFunction using the specified metadata
        /// object and that belongs to the specified schema.
        /// </summary>
        /// <param name="function">Underlying metadata object to be used by this object.</param>
        /// <param name="schema">Parent schema object.</param>
        public ScalarValuedFunction(Smo.UserDefinedFunction function, Schema schema)
            : base(function, schema)
        {
            Debug.Assert(this.IsScalarValuedFunction, "SmoMetadataProvider Assert", "this.IsScalarValuedFunction");
        }

        public override IMetadataOrderedCollection<IParameter> Parameters
        {
            get
            {
                if (this.m_parameters == null)
                {
                    Database database = this.Parent.Parent;
                    this.m_parameters = Utils.UserDefinedFunction.CreateParameterCollection(
                        database, this.m_smoMetadataObject.Parameters, this.GetModuleInfo());
                }

                return this.m_parameters;
            }
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IScalarFunction Members
        public bool IsAggregateFunction
        {
            get { return false; }
        }
        #endregion

        #region IScalar Members
        public ScalarType ScalarType
        {
            get { return ScalarType.ScalarFunction; }
        }

        public IScalarDataType DataType
        {
            get
            {
                if (this.m_dataType == null)
                {
                    Database database = this.Parent.Parent;
                    IDataType dataType = Utils.GetDataType(database, this.m_smoMetadataObject.DataType);

                    Debug.Assert(dataType != null, "SmoMetadataProvider Assert", "dataType != null");
                    Debug.Assert(dataType.IsScalar, "SmoMetadataProvider Assert", "dataType.IsScalar");

                    this.m_dataType = dataType as IScalarDataType;
                }

                Debug.Assert(this.m_dataType != null, "SmoMetadataProvider != null", "this.m_dataType != null");
                return this.m_dataType;
            }
        }

        public bool Nullable 
        {
            get { return true; }
        }

        #endregion

        #region ICallableModule Members
        public CallableModuleType ModuleType
        {
            get { return CallableModuleType.ScalarFunction; }
        }

        public IScalarDataType ReturnType
        {
            get { return this.DataType; }
        }
        #endregion

        #region IScalarValuedFunction Members
        public bool ReturnsNullOnNullInput
        {
            get {
#if DEBUG
                if (this.m_parent.IsSysSchema)
                {
                    bool retrieved;
                    try
                    {
                        bool value = this.m_smoMetadataObject.ReturnsNullOnNullInput;
                        retrieved = true;
                    }
                    catch (Smo.PropertyCannotBeRetrievedException)
                    {
                        retrieved = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail("SmoMetadataProvider Assert", "Unexpected exception: " + ex.GetType());
                        retrieved = false;
                    }
                    Debug.Assert(!retrieved, "SmoMetadataProvider Assert",
                        "ReturnsNullOnNullInput property should not be retrievable for SVF in sys schema");
                }
#endif
                bool? returnsNullOnNullInput;
                Utils.TryGetPropertyValue<bool>(this.m_smoMetadataObject, "ReturnsNullOnNullInput", out returnsNullOnNullInput);

                return this.m_parent.IsSysSchema ? false : returnsNullOnNullInput.GetValueOrDefault();
            }
        }
        #endregion
    }
}

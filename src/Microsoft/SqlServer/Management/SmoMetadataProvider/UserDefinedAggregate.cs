// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class UserDefinedAggregate : SchemaOwnedObject<Smo.UserDefinedAggregate>, IUserDefinedAggregate
    {
        private IScalarDataType m_dataType;
        private ParameterCollection m_parameters;

        public UserDefinedAggregate(Smo.UserDefinedAggregate aggregate, Schema schema)
            : base(aggregate, schema)
        {
        }

        public override int Id
        {
            get { return this.m_smoMetadataObject.ID; }
        }

        public override bool IsSystemObject
        {
            get { return false; }
        }

        public override T Accept<T>(ISchemaOwnedObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }

        #region IScalarFunction Members
        public bool IsAggregateFunction
        {
            get { return true; }
        }
        #endregion

        #region IFunction Members
        public IMetadataOrderedCollection<IParameter> Parameters
        {
            get
            {
                if (this.m_parameters == null)
                {
                    // annawaw-ISSUE-REVIEW 9/26/2010 Aggregates cannot have default values, no need to retrieve module info for them.

                    Database database = this.Parent.Parent;
                    this.m_parameters = Utils.UserDefinedFunction.CreateParameterCollection(
                        database, this.m_smoMetadataObject.Parameters, null);
                }

                return this.m_parameters;
            }
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
    }
}

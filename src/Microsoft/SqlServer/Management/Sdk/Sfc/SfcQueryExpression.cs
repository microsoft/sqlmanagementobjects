// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public sealed class SfcQueryExpression : IXmlSerializable
    {
        private Urn expression = null;

        public SfcQueryExpression()
        {
        }

        public SfcQueryExpression(string path)
        {
            this.expression = new Urn(path);
            if (!this.expression.IsValidUrn())
            {
                throw new SfcInvalidQueryExpressionException();
            }
        }

        public void SetExpression(XPathExpression expression)
        {
            this.expression = new Urn(expression.ToString());
            if (!this.expression.IsValidUrn())
            {
                throw new SfcInvalidQueryExpressionException();
            }
        }

        public override string ToString()
        {
            return this.expression.ToString();
        }
        
        /// For ease of use with the current Enumerator, allow us to
        /// be a Urn.
        internal Urn ToUrn()
        {
            return this.expression;
        }

        /// Returns the compiled form of this expression.
        public XPathExpression Expression
        {
            get
            {
                return this.expression.XPathExpression;
            }
        }

        /// Returns the expression as a string stripped of all filters.
        public string ExpressionSkeleton
        {
            get
            {
                return this.expression.XPathExpression.ExpressionSkeleton;
            }
        }

        /// <summary>
        /// Returns the string Type name of the expression leaf.
        /// </summary>
        /// <returns></returns>
        public string GetLeafTypeName()
        {
            if (this.expression.ToString().StartsWith("Server", StringComparison.Ordinal))
            {
                // Can't do that any more for SMO types. Caller beware!
                Debug.Assert(false);
            }

            return this.expression.Type;
        }

        #region IXmlSerializable Members

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null; //No implementation needed
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            this.expression = new Urn(
                SfcSecureString.XmlUnEscape(reader.ReadElementContentAsString()));
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteRaw("<SfcQueryExpression>");
            writer.WriteRaw(this.Expression.ToString());
            writer.WriteRaw("</SfcQueryExpression>");
        }

        #endregion
    }
}

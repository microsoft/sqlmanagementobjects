// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Collection of SoapPayloadMethod objects associated with a SoapPayload
    ///</summary>
    public sealed class SoapPayloadMethodCollection : SoapMethodCollectionBase<SoapPayloadMethod, SoapPayload>
    {

        internal SoapPayloadMethodCollection(SqlSmoObject parentInstance) : base((SoapPayload)parentInstance)
        {
        }

        public SoapPayloadMethod this[string name, string methodNamespace]
        {
            get
            {
                return GetObjectByKey(new SoapMethodKey(name, methodNamespace)) as SoapPayloadMethod;
            }
        }

        protected override string UrnSuffix => SoapPayloadMethod.UrnSuffix;

        public void Remove(SoapPayloadMethod soapMethod)
        {
            if (null == soapMethod)
                throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException(nameof(soapMethod)));

            RemoveObj(soapMethod, soapMethod.key);
        }

        internal override SoapPayloadMethod GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return new SoapPayloadMethod(this, key, state);
        }
    }
}


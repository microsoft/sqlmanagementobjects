// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for EvaluationHistory.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class EvaluationHistoryCollection : SfcDictionaryCollection<EvaluationHistory, EvaluationHistory.Key, Policy>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public EvaluationHistoryCollection(Policy parent)
            : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EvaluationHistory this[Int64 id]
        {
            get { return this[new EvaluationHistory.Key(id)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains (Int64 id)
        {
            return Contains(new EvaluationHistory.Key(id));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return EvaluationHistory.GetObjectFactory();
        }

    }

}

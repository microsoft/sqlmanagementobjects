// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for EvaluationDetails.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class EvaluationDetailCollection : SfcDictionaryCollection<EvaluationDetail, EvaluationDetail.Key, ConnectionEvaluationHistory>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public EvaluationDetailCollection(ConnectionEvaluationHistory parent)
            : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EvaluationDetail this[int id]
        {
            get { return this[new EvaluationDetail.Key(id)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(int id)
        {
            return Contains(new EvaluationDetail.Key(id));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return EvaluationDetail.GetObjectFactory();
        }

    }

}

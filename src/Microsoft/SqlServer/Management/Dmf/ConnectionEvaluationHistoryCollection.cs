// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for ConnectionEvaluationHistorys.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ConnectionEvaluationHistoryCollection : SfcDictionaryCollection<ConnectionEvaluationHistory, ConnectionEvaluationHistory.Key, EvaluationHistory>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public ConnectionEvaluationHistoryCollection(EvaluationHistory parent)
            : base(parent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ConnectionEvaluationHistory this[int id]
        {
            get { return this[new ConnectionEvaluationHistory.Key(id)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(int id)
        {
            return Contains(new ConnectionEvaluationHistory.Key(id));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return ConnectionEvaluationHistory.GetObjectFactory();
        }

    }

}

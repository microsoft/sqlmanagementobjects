// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// This is the collection for Server Groups
    /// </summary>
    public sealed class ServerGroupCollection : SfcCollatedDictionaryCollection<ServerGroup, ServerGroup.Key, ServerGroupParent>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="customComparer"></param>
        public ServerGroupCollection(ServerGroupParent parent, IComparer<string> customComparer)
            : base(parent, customComparer)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ServerGroup this[string name]
        {
            get { return this[new ServerGroup.Key(name)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return Contains(new ServerGroup.Key(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return ServerGroup.GetObjectFactory();
        }

        internal bool SkipInitialSqlLoad
        {
            set
            {
                this.Initialized = true;
            }
        }
    }

}

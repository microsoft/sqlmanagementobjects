// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;

using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// This is the collection for Registered Servers
    /// </summary>
    public sealed class RegisteredServerCollection : SfcCollatedDictionaryCollection<RegisteredServer, RegisteredServer.Key, ServerGroup>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="customComparer"></param>
        public RegisteredServerCollection(ServerGroup parent, IComparer<string> customComparer)
            : base(parent, customComparer)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public RegisteredServer this[string name]
        {
            get { return this[new RegisteredServer.Key(name)]; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return Contains(new RegisteredServer.Key(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcObjectFactory GetElementFactoryImpl()
        {
            return RegisteredServer.GetObjectFactory();
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

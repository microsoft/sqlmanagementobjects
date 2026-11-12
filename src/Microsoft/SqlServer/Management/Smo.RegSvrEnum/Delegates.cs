
//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void RegistrationEventHandler(object sender, RegistrationEventArgs e);

    /// <summary>
    /// 
    /// </summary>
    public sealed class RegistrationEventArgs : EventArgs
    {
        private RegistrationInfo node;
        
        /// <summary>
        /// 
        /// </summary>
        public RegistrationInfo Node
        {
            get
            {
                return this.node;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ParentRegistrationInfo Parent
        {
            get
            {
                return this.Node.Parent;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public RegistrationEventArgs(RegistrationInfo node)
        {
            this.node = node;
        }
    }
}

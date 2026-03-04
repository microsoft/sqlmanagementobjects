//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ParentRegistrationInfo : RegistrationInfo
    {
        /// <summary>
        /// 
        /// </summary>
        protected ParentRegistrationInfo()
        {
            this.children = new RegistrationInfoCollection();
        }

        internal bool HasChildWithName(string name)
        {

            return this.children.Contains(name);
        }

        /// <summary>
        /// generates unique name for the given existing name
        /// </summary>
        /// <param name="oldName"></param>
        /// <returns></returns>
        internal string GenerateUniqueName(string oldName)
        {
            string newName = oldName;
            int i = 1;

            while (this.children.Contains(newName))
            {
                newName = SRError.UniqueRegisteredNamePattern(i++, oldName);
            }
            return newName;
        }

        internal void RemoveChild(RegistrationInfo reg)
        {
           
            //NOTE; do NOT go via RegistrationProvider here, 
            //as this method is being called by it
            this.Children.Remove(reg); 
            
            // make sure to disconnect this node from its parent
            if (reg.Parent == this) //it might have been re-parented by the time we're called
            {
                reg.Parent = null;
            }
        }

        
        /// <summary>
        /// adds new child to our collection
        /// </summary>
        /// <param name="reg"></param>
        internal void AddChild(RegistrationInfo reg)
        {            
            reg.Parent = this;
            Children.Add(reg);
        }


        
        #region Public Properties		
        public RegistrationInfoCollection Children
        {
            get
            {
                return this.children;
            }
        }
        #endregion


        #region Private Data
        private RegistrationInfoCollection children;
        #endregion
    }
}

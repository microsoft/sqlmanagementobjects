using System;
using System.Collections.Generic;


namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// A nameable list of UIConnectionInfo objects
    /// </summary>
    public class UIConnectionGroupInfo : List<UIConnectionInfo>
    {
        private string name = String.Empty;

        /// <summary>
        /// The name of the group
        /// </summary>
        public string Name
        {
            get
            {
                string result = this.name;

                if (String.IsNullOrEmpty(this.name) && (this.Count == 1))
                {
                    result = this[0].DisplayName;
                }

                return result;
            }

            set
            {
                this.name = (value != null) ? value : String.Empty;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the UIConnectionGroupInfo class
        ///     that is empty and has the default initial capacity.
        /// </summary>
        public UIConnectionGroupInfo() {}
        
        /// <summary>
        /// Initializes a new instance of the UIConnectionGroupInfo class
        /// that contains elements copied from the specified collection and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public UIConnectionGroupInfo(IEnumerable<UIConnectionInfo> collection) : base(collection) {}

        /// <summary>
        /// Initializes a new instance of the UIConnectionGroupInfo class
        /// that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public UIConnectionGroupInfo(int capacity) : base(capacity) {}

        /// <summary>
        /// Initializes a new instance of the UIConnectionGroupInfo class
        /// that contains a shallow copy of the items from another
        /// UIConnectionGroupInfo
        /// </summary>
        /// <param name="other"></param>
        public UIConnectionGroupInfo(UIConnectionGroupInfo other) : base(other) 
        {
            this.name = other.name;
        }

        /// <summary>
        /// Initializes a new instance of the UIConnectionGroupInfo class
        /// with a single item in the group
        /// </summary>
        /// <param name="connectionInfo">The connection info to put in the group</param>
        public UIConnectionGroupInfo(UIConnectionInfo connectionInfo) : base(1)
        {
            if (connectionInfo != null)
            {
                this.Add(connectionInfo);
                this.name = connectionInfo.DisplayName;
            }
        }

        /// <summary>
        /// Create a new collection containing the same objects this group contains
        /// </summary>
        /// <returns>The copy of the group</returns>
        public UIConnectionGroupInfo ShallowCopy()
        {
            UIConnectionGroupInfo result = new UIConnectionGroupInfo(this);
            return result;
        }

        /// <summary>
        /// Create a new collection containing copies of the objects this group contains
        /// </summary>
        /// <param name="withNewConnectionIds">
        /// Whether the copied connection objects should have new IDs.  Set this to true if we want 
        /// different connections.  Set it to false if we want the copies to hash to the same
        /// values as their progenitors.
        /// </param>
        /// <returns>The copy of the group</returns>
        public UIConnectionGroupInfo DeepCopy(bool withNewConnectionIds)
        {
            UIConnectionGroupInfo result = new UIConnectionGroupInfo(this.Count);
            result.Name = this.Name;

            foreach (UIConnectionInfo connectionInfo in this)
            {
                UIConnectionInfo newConnectionInfo = new UIConnectionInfo(connectionInfo, withNewConnectionIds);
                result.Add(newConnectionInfo);
            }

            return result;
        }
    }
}

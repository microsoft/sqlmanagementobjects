// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class SmoObjectBase : Microsoft.SqlServer.Management.Sdk.Sfc.ISfcValidate
    {
        //The UserData property associates user-defined data with a SMO object instance.
        Object userData = null;
        public Object UserData
        {
            get { return userData; }
            set { userData = value; }
        }

        SqlSmoState m_state;
    
        /// <summary>
        /// Returns the state of the object
        /// </summary>
        public SqlSmoState State
        {
            get
            {
                return m_state;
            }
        }

        /// <summary>
        /// Sets the object state (Existing, Dropped etc.)
        /// Applications should avoid using this method.
        /// </summary>
        /// <param name="state"></param>
        public void SetState(SqlSmoState state)
        {
            if (m_state != state)
            {
                m_state = state;
                OnStateChanged();
            }
        }

        internal PropertyBagState propertyBagState;
        /// <summary>
        ///  Sets the state of the property bag (Empty, Lazy or Full).
        /// </summary>
        /// <param name="state"></param>
        internal void SetState(PropertyBagState state)
        {
            this.propertyBagState = state;
        }

        /// <summary>
        /// This function is overridden by instance classes that need to validate property 
        /// values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal virtual void ValidateProperty(Property prop, object value)
        {
        }

        ///<summary>
        /// Returns default value for the property, when object is in 'Pending' or 'Creating' state
        /// </summary>
        /// <param name="propname">property name</param>
        /// <returns></returns>
        internal virtual object GetPropertyDefaultValue(string propname)
        {
            return null;
        }

        /// <summary>
        /// Called when the value for this property is missing.
        /// </summary>
        /// <param name="propname"></param>
        /// <returns></returns>
        internal virtual object OnPropertyMissing(string propname, bool useDefaultValue)
        {
            if (useDefaultValue)
            {
                return GetPropertyDefaultValue(propname);
            }
            return null;
        }

        internal virtual void OnPropertyChanged(string propname)
        {

        }

        internal virtual bool ShouldNotifyPropertyChange
        {
            get { return false; }
        }

        internal virtual void OnPropertyMetadataChanged(string propname)
        {

        }

        internal virtual bool ShouldNotifyPropertyMetadataChange
        {
            get { return false; }
        }

        internal virtual void OnStateChanged()
        {
            OnPropertyChanged("State");
        }

        #region ISfcValidate Members

        [CLSCompliant(false)]
        public virtual Microsoft.SqlServer.Management.Sdk.Sfc.ValidationState Validate(string methodName, params object[] arguments)
        {
            return new Microsoft.SqlServer.Management.Sdk.Sfc.ValidationState();
        }

        #endregion
    }


}

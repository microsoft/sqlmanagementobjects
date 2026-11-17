// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public interface ISfcPropertyStorageProvider
    {
        object GetPropertyValueImpl(string propertyName);
        void SetPropertyValueImpl(string propertyName, object value);
    }

    /// <summary>
    /// SFC Default implementation to the default storage interface
    /// </summary>
    internal class SfcDefaultStorage : ISfcPropertyStorageProvider
    {
        private object[] propertiesStorage = null;
        private SfcInstance sfcObject = null;
        internal SfcDefaultStorage(SfcInstance sfcObject)
        {
            this.sfcObject = sfcObject; 
        }
        #region ISfcPropertyStorageProvider Members

        public object GetPropertyValueImpl(string propertyName)
        {
            if (this.propertiesStorage == null)
            {
                BuildPropertiesStorage();
            }
            //@TODO: Do we need to support retreiving properties even when they are null??
            //Current SFC implementation supports this, so I assume we need to do the same.
            sfcObject.Properties.SetRetrieved(propertyName, true);
            return propertiesStorage[sfcObject.Properties.LookupID(propertyName)];
        }

        public void SetPropertyValueImpl(string propertyName, object value)
        {
            if (this.propertiesStorage == null)
            {
                BuildPropertiesStorage();
            }
            sfcObject.Properties.SetDirty(propertyName, true);
            this.propertiesStorage[sfcObject.Properties.LookupID(propertyName)] = value;
        }

        private void BuildPropertiesStorage()
        {
            this.propertiesStorage = new object[sfcObject.Metadata.InternalStorageSupportedCount];
        }

        #endregion
    }
}

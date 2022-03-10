// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Class that wraps collections. PS expands collections in the pipeline by default
    /// for enumreable types, so this stops that.
    /// I moved this into SFC, as it needs to be shared between cmdlets and provider. I did
    /// not want to add a new shared GAC'ed assembly to setup for this, hence explains why it
    /// is here.
    /// </summary>
    public class SfcCollectionInfo
    {
        string m_displayName;
        object m_collection;

        public SfcCollectionInfo(string displayName, object collection)
        {
            m_displayName = displayName;
            m_collection = collection;
        }

        public string DisplayName
        {
            get
            {
                return m_displayName;
            }
        }

        public object Collection
        {
            get { return m_collection; }
        }
    }
}

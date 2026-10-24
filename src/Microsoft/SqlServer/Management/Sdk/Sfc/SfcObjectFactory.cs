// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public abstract class SfcObjectFactory
    {
        // The one and only creation method is implemented by the domain
        protected abstract SfcInstance CreateImpl();

        // Methods used internally in SFC. All of them create fully functional objects
        // that are either fully populated or are able to populate themselves
        //
        // Note: always call these methods in SFC and DO NOT create objects using reflection.
        // The factory knows how to create objects -- you don't.

        // This method creates a fully populated object. Everything is set
        internal SfcInstance Create(SfcInstance parent, IPropertyCollectionPopulator populator, SfcObjectState state)
        {
            SfcInstance obj = this.CreateImpl();
            obj.Parent = parent;
            obj.State = state;
            populator.Populate(obj.Properties);
            return obj;
        }

        // This method creates a lazy-populated object, which knows enough about itself to behave properly
        internal SfcInstance Create(SfcInstance parent, SfcKey key, SfcObjectState state)
        {
            SfcInstance obj = this.CreateImpl();
            obj.State = state;
            obj.KeyChain = new SfcKeyChain(key, parent.KeyChain);
            return obj;
        }

    }
}



// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // this is the class that contains common features of all collection classes
    //[StrongNameIdentityPermission(SecurityAction.InheritanceDemand, PublicKey="0x03bf8c39a7191dfd")]
    public abstract class ParameterCollectionBase : ArrayListCollectionBase
    {
        internal ParameterCollectionBase(SqlSmoObject parent) : base(parent)
        {
        }

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoArrayList(new SimpleObjectComparer(this.StringComparer), this);
        }
        
        public bool Contains(String name) 
        {
            return this.Contains(new SimpleObjectKey(name));
        }

        public void Remove(string name)
        {
            CheckCollectionLock();
            this.Remove(new SimpleObjectKey(name));
        }
        
        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        { 
            string name = urn.GetAttribute("Name");
            if( null == name || name.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            return new SimpleObjectKey(name);
        }

        /// <summary>
        /// Inserts the object into the collection. Because the key is the name, we need 
        /// to insert in the order of ID
        /// </summary>
        /// <param name="obj"></param>
        protected override void ImplAddExisting(SqlSmoObject obj)
        {
            Diagnostics.TraceHelper.Assert(obj.Properties.Contains("ID"));

            // use the most generic version of the GetPropValueOptional because 
            // ID is int in most cases, but it's byte for some objects (IndexedColumn)
            int objId = Convert.ToInt32(obj.GetPropValueOptional<object>("ID", -1),
                                           SmoApplication.DefaultCulture);

            for (int i = 0; i < InternalStorage.Count; i++)
            {
                SqlSmoObject currObj = InternalStorage.GetByIndex(i);
                Diagnostics.TraceHelper.Assert(currObj.Properties.Contains("ID"));

                int currObjId = Convert.ToInt32(currObj.GetPropValueOptional<object>("ID", -1),
                                                SmoApplication.DefaultCulture);
                if (-1 != currObjId)
                {
                    if (objId < currObjId)
                    {
                        InternalStorage.InsertAt(i, obj);
                        return;
                    }
                }
            }

            // if we could not find a position then insert it at the end
            InternalStorage.InsertAt(InternalStorage.Count, obj);
        }
    }
}


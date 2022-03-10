// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    public class SoapMethodObject : ScriptNameObjectBase
    {
        internal SoapMethodObject(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }

        // this default constructor has to be called by objects that do not know their parent
        // because they don't live inside a collection
        internal SoapMethodObject(ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
        }

        // this constructor called by objects thet are created in space
        protected internal SoapMethodObject()
            : base()
        {
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string Namespace
        {
            get 
            {
                return ((SoapMethodKey)key).Namespace;
            }
            set 
            {
                if( null == value )
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentNullException("Namespace"));
                }

                if ( this.State == SqlSmoState.Pending)
                {
                    // if the object is in Pending state we can set the schema
                    ((SoapMethodKey)key).Namespace = value;
                    UpdateObjectState();
                    return;
                }
                else if( this.State == SqlSmoState.Creating )
                {
                    // if the object is in Existing state we can set the schema only if the object 
                    // has not been added to the collection 
                    if( this.ObjectInSpace )
                    {
                        ((SoapMethodKey)key).Namespace = value;
                        UpdateObjectState();
                        return;
                    }
                }

                // all other cases are not valid, we have to throw
                throw new FailedOperationException(ExceptionTemplates.SetNamespace, this, new InvalidSmoOperationException(ExceptionTemplates.SetNamespace, this.State));
                
            }
        }

       internal override ObjectKeyBase GetEmptyKey()
        {
            return new SoapMethodKey(null, null);
        }
    }
}

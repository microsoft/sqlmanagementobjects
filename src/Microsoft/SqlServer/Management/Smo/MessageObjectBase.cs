// Copyright (c) Microsoft.
// Licensed under the MIT license.
namespace Microsoft.SqlServer.Management.Smo
{
    public class MessageObjectBase : SqlSmoObject
    {
        internal MessageObjectBase(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }

        // this default constructor has to be called by objects that do not know their parent
        // because they don't live inside a collection
        internal MessageObjectBase(ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
        }

        // this constructor called by objects thet are created in space
        protected internal MessageObjectBase()
            : base()
        {
        }

        internal override ObjectKeyBase GetEmptyKey()
        {
            return new MessageObjectKey(0, null);
        }
    }
}

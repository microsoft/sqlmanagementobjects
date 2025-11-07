// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Agent
{
    /// <summary>
    /// A collection of JobSchedule objects associated with a TParent instance
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    public sealed class JobScheduleCollection<TParent> : JobScheduleCollectionBase<TParent>
        where TParent : SqlSmoObject
    {

        internal JobScheduleCollection(SqlSmoObject parentInstance) : base((TParent)parentInstance)
        {
        }

        /// <summary>
        /// Returns the parent object
        /// </summary>
		public TParent Parent
        {
            get
            {
                return ParentInstance as TParent;
            }
        }

        /// <summary>
        /// Return the JobSchedule object with the given name, or null if it's not in the collection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
		public JobSchedule this[string name]
        {
            get
            {
                return GetObjectByKey(new ScheduleObjectKey(name, GetDefaultID())) as JobSchedule;
            }
        }
    }
}

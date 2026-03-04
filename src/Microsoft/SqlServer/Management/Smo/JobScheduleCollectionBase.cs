// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// A collection of JobSchedule objects associated with an instance of TParent
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    public class JobScheduleCollectionBase<TParent> : ArrayListCollectionBase<JobSchedule, TParent>
        where TParent : SqlSmoObject
    {
        protected override string UrnSuffix => JobSchedule.UrnSuffix;

        internal JobScheduleCollectionBase(SqlSmoObject parent) : base((TParent)parent)
        {
        }

        protected override void InitInnerCollection() => InternalStorage = new SmoArrayList<JobSchedule, TParent>(new ScheduleObjectComparer(StringComparer), this);

        internal void RemoveObject(string name, int id) => InternalStorage.Remove(new ScheduleObjectKey(name, id));

        public bool Contains(string name) => Contains(new ScheduleObjectKey(name, GetDefaultID()));

        public bool Contains(string name, int id) => Contains(new ScheduleObjectKey(name, id));

        internal static int GetDefaultID() => JobScheduleConstants.DefaultID;

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            string name = urn.GetAttribute("Name");
            if (null == name || (name.Length == 0 && !CanHaveEmptyName(urn)))
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            int id = GetDefaultID();
            string idstr = urn.GetAttribute("ID");
            if (null != idstr && idstr.Length > 0)
            {
                id = int.Parse(idstr, SmoApplication.DefaultCulture);
            }

            return new ScheduleObjectKey(name, id);
        }

        internal override JobSchedule GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new JobSchedule(this, key, state);

        public JobSchedule this[Guid scheduleuid]
        {
            get
            {
                if (!initialized && ParentInstance.State == SqlSmoState.Existing)
                {
                    InitializeChildCollection();
                }
                // The collection is not sorted by ScheduleUID, so we need linear search.
                // It's fine here since number of JobSchedules is typically small
                foreach (JobSchedule js in InternalStorage)
                {
                    if (js.ScheduleUid == scheduleuid)
                    {
                        return js;
                    }
                }
                // Return null when object not found. Wrong, but consistent with other indexes
                return null;
            }
        }
    }

    internal class ScheduleObjectComparer : ObjectComparerBase
    {
        internal ScheduleObjectComparer(IComparer stringComparer) : base(stringComparer)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            ScheduleObjectKey x = obj1 as ScheduleObjectKey;
            ScheduleObjectKey y = obj2 as ScheduleObjectKey;

            //if we have ID compare with ID
            if (y.ID > -1 && x.ID > -1)
            {
                return x.ID - y.ID;
            }

            // else compare with name
            return stringComparer.Compare(x.Name, y.Name);
        }
    }

    internal class ScheduleObjectKey : SimpleObjectKey
    {
        public ScheduleObjectKey(string name, int id) : base(name)
        {
            ID = id;
        }

        internal static StringCollection schemaFields;
        static ScheduleObjectKey()
        {
            schemaFields = new StringCollection
            {
                "Name",
                "ID"
            };
        }

        public int ID { get; set; }

        public override string UrnFilter
        {
            get
            {
                if (ID > -1)
                {
                    return string.Format(SmoApplication.DefaultCulture,
                                "@Name='{0}' and @ID='{1}'",
                                    Urn.EscapeString(Name), ID);
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture,
                                "@Name='{0}'", Urn.EscapeString(Name));
                }
            }
        }

        public override string ToString() => Name;

        public override StringCollection GetFieldNames() => schemaFields;

        public override ObjectKeyBase Clone() => new ScheduleObjectKey(Name, ID);

        internal override void Validate(Type objectType)
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
            }
        }

        public override bool IsNull
        {
            get { return null == Name; }
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new ScheduleObjectComparer(stringComparer);
    }

    /// <summary>
    /// Defines constants associated with JobSchedule objects
    /// </summary>
    public static class JobScheduleConstants
    {
        /// <summary>
        /// The ID value for a JobSchedule that hasn't been saved yet
        /// </summary>
        public const int DefaultID = -1;
    }


}


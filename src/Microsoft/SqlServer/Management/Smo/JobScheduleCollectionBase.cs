// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // this is the class that contains common features of all schema collection classes

    public class JobScheduleCollectionBase : ArrayListCollectionBase
	{
		internal JobScheduleCollectionBase(SqlSmoObject parent) : base(parent)
		{
		}

		protected override void InitInnerCollection()
		{
			InternalStorage = new SmoArrayList(new ScheduleObjectComparer(this.StringComparer), this);
		}

		internal void RemoveObject(string name, int id)
		{
			InternalStorage.Remove(new ScheduleObjectKey(name, id));
		}

		public bool Contains(String name)
		{
			return this.Contains(new ScheduleObjectKey(name, GetDefaultID()));
		}

		public bool Contains(String name, int id)
		{
			return this.Contains(new ScheduleObjectKey(name, id));
		}

		internal static int GetDefaultID()
		{
			return -1;
		}

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
                id = Int32.Parse(idstr, SmoApplication.DefaultCulture);
            }

            return new ScheduleObjectKey(name, id);
		}

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
                foreach ( JobSchedule js in this.InternalStorage )
				{
					if( js.ScheduleUid == scheduleuid )
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
		int id;

		public ScheduleObjectKey(String name, int id) : base(name)
		{
			this.id = id;
		}

		internal static StringCollection schemaFields;
		static ScheduleObjectKey()
		{
			schemaFields = new StringCollection();
			schemaFields.Add("Name");
			schemaFields.Add("ID");
		}

		public Int32 ID
		{
			get { return id; }
			set { id = value; }
		}

		public override string UrnFilter
		{
			get
			{
				if (id > -1)
                {
                    return string.Format(SmoApplication.DefaultCulture,
								"@Name='{0}' and @ID='{1}'",
									Urn.EscapeString(name), id );
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture,
								"@Name='{0}'", Urn.EscapeString(name));
                }
            }
		}

		public override string ToString()
		{
			return name;
		}

		public override StringCollection GetFieldNames()
		{
			return schemaFields;
		}

		public override ObjectKeyBase Clone()
		{
			return new ScheduleObjectKey(this.Name, this.ID);
		}

		internal override void Validate(Type objectType)
		{
			if (null == this.Name || this.Name.Length == 0)
			{
				throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
			}
		}

		public override bool IsNull
		{
			get { return (null == name ); }
		}

		public override ObjectComparerBase GetComparer(IComparer stringComparer)
		{
			return new ScheduleObjectComparer(stringComparer);
		}
	}


}


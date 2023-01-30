// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// base class for all generic collections
    /// </summary>
    public abstract class SimpleObjectCollectionBase : SortedListCollectionBase
	{
		internal SimpleObjectCollectionBase(SqlSmoObject parent) : base(parent)
		{
		}

		/// <summary>
		/// Initializes the storage
		/// </summary>
		protected override void InitInnerCollection()
		{
			InternalStorage = new SmoSortedList(new SimpleObjectComparer(this.StringComparer));
		}
		
		/// <summary>
		/// Contains
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool Contains(String name) 
		{
			if (null == name)
            {
                throw new FailedOperationException(ExceptionTemplates.Contains, this, new ArgumentNullException("name"));
            }

            return this.Contains(new SimpleObjectKey(name));
		}

		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");
			if( null == name || (name.Length == 0 && !CanHaveEmptyName(urn)))
			{
				throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
			}

			return new SimpleObjectKey(name);
		}
	}

	internal class SimpleObjectComparer : ObjectComparerBase
	{
		internal SimpleObjectComparer(IComparer stringComparer) : base(stringComparer)
		{
		}

		public override int Compare(object obj1, object obj2)
		{
			return stringComparer.Compare((obj1 as SimpleObjectKey).Name, (obj2 as SimpleObjectKey).Name);
		}
	}

	internal class SimpleObjectKey : ObjectKeyBase
	{
		protected String name;


		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="name"></param>
		public SimpleObjectKey(String name) : base()
		{
			this.name = name;
		}

		static SimpleObjectKey()
		{
			fields.Add("Name");
		}

		internal static readonly StringCollection fields = new StringCollection();

		/// <summary>
		/// Name of the object
		/// </summary>
		/// <value></value>
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format(SmoApplication.DefaultCulture, "[{0}]", 
											SqlSmoObject.SqlBraket(name));
		}

		/// <summary>
		/// GetExceptionName
		/// </summary>
		/// <returns></returns>
		public override string GetExceptionName()
		{
			return name;
		}

		/// <summary>
		/// Urn suffix that identifies this object
		/// </summary>
		/// <value></value>
		public override string UrnFilter
		{
			get { return string.Format(SmoApplication.DefaultCulture, "@Name='{0}'", Urn.EscapeString(name)); }
		}

		/// <summary>
		/// Return all fields that are used by this key.
		/// </summary>
		/// <returns></returns>
		public override StringCollection GetFieldNames()
		{
			return fields;
		}

		/// <summary>
		/// Clone the object.
		/// </summary>
		/// <returns></returns>
		public override ObjectKeyBase Clone()
		{
			return new SimpleObjectKey(this.Name);
		}
			
		internal override void Validate(Type objectType)
		{
			bool acceptEmptyName = (objectType.Equals(typeof(UserDefinedAggregateParameter)) ||
									objectType.Equals(typeof(UserDefinedFunctionParameter)));
			if( null == this.Name || (this.Name.Length == 0 && !acceptEmptyName))
			{
				throw new UnsupportedObjectNameException(ExceptionTemplates.UnsupportedObjectNameExceptionText(objectType.ToString())).SetHelpContext("UnsupportedObjectNameExceptionText");
			}
		}

		/// <summary>
		/// True if the key is null.
		/// </summary>
		/// <value></value>
		public override bool IsNull
		{
			get { return (null == name);}
		}

		/// <summary>
		/// Returns string comparer needed to compare the string portion of this key.
		/// </summary>
		/// <param name="stringComparer"></param>
		/// <returns></returns>
		public override ObjectComparerBase GetComparer(IComparer stringComparer)
		{
			return new SimpleObjectComparer(stringComparer);
		}

	}


}


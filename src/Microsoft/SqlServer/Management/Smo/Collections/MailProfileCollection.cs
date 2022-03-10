// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Mail
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class MailProfileCollection : SimpleObjectCollectionBase
	{


















		internal MailProfileCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlMail Parent
		{
			get
			{
				return this.ParentInstance as SqlMail;
			}
		}

		
		public MailProfile this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as MailProfile;
			}
		}


		// returns wrapper class
		public MailProfile this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as MailProfile;
                    
                



















			}
		}


		public void CopyTo(MailProfile[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public MailProfile ItemById(int id)
		{
			return (MailProfile)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(MailProfile);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new MailProfile(this, key, state);
		}



















		public void Add(MailProfile mailProfile) 
		{
			AddImpl(mailProfile);
		}


		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new SimpleObjectKey(name));
		}


		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");



            if( null == name || name.Length == 0)

				throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            return new SimpleObjectKey(name);        
        }


















	}
}

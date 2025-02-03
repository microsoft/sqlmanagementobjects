// Copyright (c) Microsoft Corporation.
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
    public sealed  class MailAccountCollection : SimpleObjectCollectionBase
	{


















		internal MailAccountCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlMail Parent
		{
			get
			{
				return this.ParentInstance as SqlMail;
			}
		}

		
		public MailAccount this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as MailAccount;
			}
		}


		// returns wrapper class
		public MailAccount this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as MailAccount;
                    
                



















			}
		}


		public void CopyTo(MailAccount[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public MailAccount ItemById(int id)
		{
			return (MailAccount)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(MailAccount);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new MailAccount(this, key, state);
		}



















		public void Add(MailAccount mailAccount) 
		{
			AddImpl(mailAccount);
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

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public  class SystemMessageCollection : MessageCollectionBase
	{

		internal SystemMessageCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public SystemMessage this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as SystemMessage;
			}
		}

		// returns wrapper class
		public SystemMessage this[Int32 id, string language]
		{
			get
			{
				return  GetObjectByKey(new MessageObjectKey(id, language)) as SystemMessage;
			}
		}

		public SystemMessage ItemByIdAndLanguage(Int32 id, string language)
		{
			return this[id, language];
		}

		public SystemMessage ItemByIdAndLanguageId(Int32 id, Int32 languageId)
		{
			Language lang = (ParentInstance as Server).Languages.ItemById(languageId);
			if(null != lang)
			{
				return this[id, lang.Name];
			}
			else
			{
				throw new FailedOperationException(ExceptionTemplates.ObjectDoesNotExist("LanguageID", languageId.ToString(SmoApplication.DefaultCulture)));
			}
		}
		
		protected override Type GetCollectionElementType()
		{
			return typeof(SystemMessage);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new SystemMessage(this, key, state);
		}

		public void CopyTo(SystemMessage[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
















	}
}


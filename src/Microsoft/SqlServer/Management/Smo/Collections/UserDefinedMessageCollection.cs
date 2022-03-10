// Copyright (c) Microsoft.
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
    public sealed class UserDefinedMessageCollection : MessageCollectionBase
	{

		internal UserDefinedMessageCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public UserDefinedMessage this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserDefinedMessage;
			}
		}

		// returns wrapper class
		public UserDefinedMessage this[Int32 id, string language]
		{
			get
			{
				return  GetObjectByKey(new MessageObjectKey(id, language)) as UserDefinedMessage;
			}
		}

		public UserDefinedMessage ItemByIdAndLanguage(Int32 id, string language)
		{
			return this[id, language];
		}

		public UserDefinedMessage ItemByIdAndLanguageId(Int32 id, Int32 languageId)
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
			return typeof(UserDefinedMessage);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new UserDefinedMessage(this, key, state);
		}

		public void CopyTo(UserDefinedMessage[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
















	}
}


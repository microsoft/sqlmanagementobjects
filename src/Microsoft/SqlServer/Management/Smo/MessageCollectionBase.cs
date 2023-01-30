// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    // this is the class that contains common features of all schema collection classes

    public class MessageCollectionBase: SortedListCollectionBase
	{
		internal MessageCollectionBase(SqlSmoObject parent) : base(parent)
		{
		}

		protected override void InitInnerCollection()
		{
			InternalStorage = new SmoSortedList(new MessageObjectComparer(this.StringComparer));
		}

		public void Remove(Int32 id)
		{
			this.Remove(new MessageObjectKey(id, GetDefaultLanguage()));
		}
		
		public void Remove(Int32 id, string language)
		{
			this.Remove(new MessageObjectKey(id, language));
		}

		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string id = urn.GetAttribute("ID");
			if( null == id || id.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("ID", urn.Type));
            }

            string language = urn.GetAttribute("Language");
			if( null == language || language.Length == 0)
            {
                language = GetDefaultLanguage();
            }

            return new MessageObjectKey(Int32.Parse(id, SmoApplication.DefaultCulture), language);
        }

		internal static string GetDefaultLanguage()
		{
			return "us_english";
		}

		public bool Contains(System.Int32 id, System.String language)
		{
			return Contains(new MessageObjectKey(id, language));
		}

		public bool Contains(System.Int32 id, System.Int32 languageId)
		{
			Language l = this.ParentInstance.GetServerObject().Languages.ItemById(languageId);
			if( null != l )
            {
                return Contains(new MessageObjectKey(id, l.Name));
            }
            else
            {
                throw new SmoException(ExceptionTemplates.UnknownLanguageId(languageId.ToString(SmoApplication.DefaultCulture)));
            }
        }
	}
	
	internal class MessageObjectComparer : ObjectComparerBase
	{
		internal MessageObjectComparer(IComparer stringComparer) : base(stringComparer)
		{
		}

		public override int Compare(object obj1, object obj2)
		{
			MessageObjectKey x = obj1 as MessageObjectKey;
			MessageObjectKey y = obj2 as MessageObjectKey;

			if( x.ID != y.ID )
            {
                return x.ID - y.ID;
            }
            else
            {
                return stringComparer.Compare(x.Language, y.Language);
            }
        }
	}

	internal class MessageObjectKey : ObjectKeyBase
	{
		private Int32 messageID;
		private String language;


		public MessageObjectKey(Int32 messageID, String language) : base()
		{
			this.messageID = messageID;
			this.language = language;
		}

		static MessageObjectKey()
		{
			fields = new StringCollection();
			fields.Add("ID");
			fields.Add("Language");
		}

		internal static StringCollection fields;

		public Int32 ID
		{
			get { return messageID; }
			set { messageID = value; } 
		}

		public string Language
		{
			get { return language; }
			set { language = value; } 
		}

		public override string UrnFilter
		{
			get 
			{ 
				if( null != language && language.Length > 0)
                {
                    return string.Format(SmoApplication.DefaultCulture, "@ID={0} and @Language='{1}'",
                                    messageID, Urn.EscapeString(language));
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture, "@ID={0}", messageID);
                }
            }
		}
			
		public override StringCollection GetFieldNames()
		{
			return fields;
		}
			
		public override string ToString()
		{
			if( null != language )
            {
                return string.Format(SmoApplication.DefaultCulture, "{0}:'{1}'", messageID, 
													SqlSmoObject.SqlString(language));
            }
            else
            {
                return messageID.ToString(SmoApplication.DefaultCulture);
            }
        }

		public override ObjectKeyBase Clone()
		{
			return new MessageObjectKey(this.ID, this.Language);
		}
			
		internal override void Validate(Type objectType)
		{
		}

		public override bool IsNull
		{
			get { return (0 == messageID || null == language); }
		}
		
		public override ObjectComparerBase GetComparer(IComparer stringComparer)
		{
			return new MessageObjectComparer(stringComparer);
		}
	}

	
}



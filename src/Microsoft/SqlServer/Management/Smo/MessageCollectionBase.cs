// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// A collection of MessageObjectBase instances associated with a Server
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public abstract class MessageCollectionBase<TObject> : SortedListCollectionBase<TObject, Server> 
        where TObject: MessageObjectBase
    {

        internal MessageCollectionBase(SqlSmoObject parent) : base((Server)parent)
        {
        }

        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<TObject>(new MessageObjectComparer(StringComparer));

        public Server Parent => ParentInstance as Server;

        public void Remove(int id) => Remove(new MessageObjectKey(id, GetDefaultLanguage()));

        public void Remove(int id, string language) => Remove(new MessageObjectKey(id, language));

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        { 
            var id = urn.GetAttribute("ID");
            if( null == id || id.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("ID", urn.Type));
            }

            var language = urn.GetAttribute("Language");
            if( null == language || language.Length == 0)
            {
                language = GetDefaultLanguage();
            }

            return new MessageObjectKey(int.Parse(id, SmoApplication.DefaultCulture), language);
        }

        internal static string GetDefaultLanguage() => "us_english";

        public bool Contains(int id, string language) => Contains(new MessageObjectKey(id, language));

        public bool Contains(int id, int languageId)
        {
            var l = ParentInstance.GetServerObject().Languages.ItemById(languageId);
            if( null != l )
            {
                return Contains(new MessageObjectKey(id, l.Name));
            }
            else
            {
                throw new SmoException(ExceptionTemplates.UnknownLanguageId(languageId.ToString(SmoApplication.DefaultCulture)));
            }
        }


        public TObject this[int id, string language] => GetObjectByKey(new MessageObjectKey(id, language)) as TObject;

        public TObject ItemByIdAndLanguage(int id, string language) => this[id, language];

        public TObject ItemByIdAndLanguageId(int id, int languageId)
        {
            var lang = (ParentInstance as Server).Languages.ItemById(languageId);
            return null != lang
                ? this[id, lang.Name]
                : throw new FailedOperationException(ExceptionTemplates.ObjectDoesNotExist("LanguageID", languageId.ToString(SmoApplication.DefaultCulture)));
        }
    }
    
    internal class MessageObjectComparer : ObjectComparerBase
    {
        internal MessageObjectComparer(IComparer stringComparer) : base(stringComparer)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            var x = obj1 as MessageObjectKey;
            var y = obj2 as MessageObjectKey;

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
        private int messageID;
        private string language;


        public MessageObjectKey(int messageID, string language) : base()
        {
            this.messageID = messageID;
            this.language = language;
        }

        
        internal static readonly StringCollection fields = new StringCollection
            {
                "ID",
                "Language"
            };

    public int ID
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

        public override StringCollection GetFieldNames() => fields;

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

        public override ObjectKeyBase Clone() => new MessageObjectKey(ID, Language);

        internal override void Validate(Type objectType)
        {
        }

        public override bool IsNull
        {
            get { return 0 == messageID || null == language; }
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new MessageObjectComparer(stringComparer);
    }
    
}

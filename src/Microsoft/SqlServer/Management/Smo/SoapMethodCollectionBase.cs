// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class SoapMethodCollectionBase<TObject,TParent>: SimpleObjectCollectionBase<TObject,TParent>
        where TObject : SoapMethodObject
        where TParent : SqlSmoObject
    {
        internal SoapMethodCollectionBase(TParent parent) : base(parent) { }
        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<TObject>(new SoapMethodComparer(StringComparer));

        internal override ObjectKeyBase KeyFromName(string name) => new SoapMethodKey(name, string.Empty);

        /// <summary>
        /// Removes the SoapMethod with the give name and namespace from the collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="methodNamespace"></param>
        public void Remove(string name, string methodNamespace) => Remove(new SoapMethodKey(name, methodNamespace));

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        { 
            var name = urn.GetAttribute("Name");
            if( null == name || name.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            var methodNamespace = urn.GetAttribute("Namespace");
            if(string.IsNullOrEmpty(methodNamespace))
            {
                methodNamespace = string.Empty;
            }

            return new SoapMethodKey(name, methodNamespace);
        }


        public bool Contains(string name, string methodNamespace) => Contains(new SoapMethodKey(name, methodNamespace));

        public new bool Contains(string name) => Contains(new SoapMethodKey(name, string.Empty));

    }
    
    internal class SoapMethodComparer : ObjectComparerBase
    {
        internal SoapMethodComparer(IComparer stringComparer) : base(stringComparer)
        {
        }

        //if the user has called Compare(name) after casting to SimpleObjectCollectionBase, the function Compare 
        //in SimpleObjectCollectionBase will be called so be prepared to handle SimpleObjectKey
        public override int Compare(object obj1, object obj2)
        {
            var x = (SoapMethodKey)obj1;
            var y = obj2 as SoapMethodKey;

            //if search schema is null search only by name
            if( null != y )
            {
                var xnamespace = x.Namespace ?? string.Empty;
                var ynamespace = y.Namespace ?? string.Empty;
                var i = stringComparer.Compare(xnamespace, ynamespace);
                if (0 != i)
                {
                    return i;
                }
            }
            var yname =  y?.Name ?? ((SimpleObjectKey)obj2).Name;
            return stringComparer.Compare(x.Name, yname);
        }

    }

    internal class SoapMethodKey : SimpleObjectKey
    {
        private string methodNamespace;


        public SoapMethodKey(string name, string methodNamespace) : base(name)
        {
            this.methodNamespace = methodNamespace;
        }

        internal static StringCollection soapMethodFields;
        
        static SoapMethodKey()
        {
            soapMethodFields = new StringCollection
            {
                nameof(Name),
                nameof(Namespace),
            };
        }

        public string Namespace 
        {
            get { return methodNamespace; }
            set { methodNamespace = value; } 
        }

        public override string UrnFilter
        {
            get 
            { 
                if( null != methodNamespace && methodNamespace.Length > 0 )
                {
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}' and @Namespace='{1}'", 
                                    Urn.EscapeString(Name), Urn.EscapeString(methodNamespace));
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}'", Urn.EscapeString(Name));
                }
            }
        }

        public override StringCollection GetFieldNames() => fields;

        public override string ToString()
        {
            if( null != methodNamespace && methodNamespace.Length > 0 )
            {
                return string.Format(SmoApplication.DefaultCulture, "{0}.{1}", 
                                    SqlSmoObject.MakeSqlBraket(Name), SqlSmoObject.MakeSqlBraket(methodNamespace));
            }
            else
            {
                return SqlSmoObject.MakeSqlBraket(Name);
            }
        }

          public override string GetExceptionName()
        {
            if (null != methodNamespace && methodNamespace.Length > 0)
            {
                return string.Format(SmoApplication.DefaultCulture, "{0}.{1}",
                                    Name, methodNamespace);
            }
            else
            {
                return Name;
            }
        }

        public override ObjectKeyBase Clone() => new SoapMethodKey(this.Name, this.Namespace);

        public override bool IsNull
        {
            get { return (null == Name|| null == methodNamespace); }
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new SoapMethodComparer(stringComparer);
    }

    
}



// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    // this is the class that contains common features of all schema collection classes

    public class SoapMethodCollectionBase: SimpleObjectCollectionBase
    {
        internal SoapMethodCollectionBase(SqlSmoObject parent) : base(parent)
        {
        }

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new SoapMethodComparer(this.StringComparer));
        }

        public void Remove(string name)
        {
            this.Remove(new SoapMethodKey(name, GetDefaultNamespace()));
        }
        
        public void Remove(string name, string methodNamespace)
        {
            this.Remove(new SoapMethodKey(name, methodNamespace));
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        { 
            string name = urn.GetAttribute("Name");
            if( null == name || name.Length == 0)
            {
                throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            }

            string methodNamespace = urn.GetAttribute("Namespace");
            if( null == methodNamespace || methodNamespace.Length == 0)
            {
                methodNamespace = GetDefaultNamespace();
            }

            return new SoapMethodKey(name, methodNamespace);
        }

        internal static string GetDefaultNamespace()
        {
            return string.Empty;
        }

        public bool Contains(string name, string methodNamespace)
        {
            return Contains(new SoapMethodKey(name, methodNamespace));
        }

        public new bool Contains(string name)
        {
            return Contains(new SoapMethodKey(name, GetDefaultNamespace()));
        }
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
            SoapMethodKey x = (SoapMethodKey)obj1;
            SoapMethodKey y = obj2 as SoapMethodKey;

            //if search schema is null search only by name
            if( null != y )
            {
                string xnamespace = null != x.Namespace ? x.Namespace : SoapMethodCollectionBase.GetDefaultNamespace();
                string ynamespace = null != y.Namespace ? y.Namespace : SoapMethodCollectionBase.GetDefaultNamespace();
                int i = stringComparer.Compare(xnamespace, ynamespace);
                if (0 != i)
                {
                    return i;
                }
            }
            string yname = null != y ? y.Name : ((SimpleObjectKey)obj2).Name;
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
            soapMethodFields = new StringCollection();
            soapMethodFields.Add("Name");
            soapMethodFields.Add("Namespace");
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
                                    Urn.EscapeString(name), Urn.EscapeString(methodNamespace));
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture, "@Name='{0}'", Urn.EscapeString(name));
                }
            }
        }
            
        public override StringCollection GetFieldNames()
        {
            return fields;
        }
            
        public override string ToString()
        {
            if( null != methodNamespace && methodNamespace.Length > 0 )
            {
                return string.Format(SmoApplication.DefaultCulture, "{0}.{1}", 
                                    SqlSmoObject.MakeSqlBraket(name), SqlSmoObject.MakeSqlBraket(methodNamespace));
            }
            else
            {
                return SqlSmoObject.MakeSqlBraket(name);
            }
        }

          public override string GetExceptionName()
        {
            if (null != methodNamespace && methodNamespace.Length > 0)
            {
                return string.Format(SmoApplication.DefaultCulture, "{0}.{1}",
                                    name, methodNamespace);
            }
            else
            {
                return name;
            }
        }

        public override ObjectKeyBase Clone()
        {
            return new SoapMethodKey(this.Name, this.Namespace);
        }
            
        public override bool IsNull
        {
            get { return (null == name|| null == methodNamespace); }
        }
        
        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new SoapMethodComparer(stringComparer);
        }
    }

    
}



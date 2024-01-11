// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class DatabaseCollection : SimpleObjectCollectionBase
	{


















		internal DatabaseCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public Database this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Database;
			}
		}


		// returns wrapper class
		public Database this[string name]
		{
			get
			{
                
    
                try
                 {
                     
                
    
	    			    return  GetObjectByName(name) as Database;
                    
                

                 }
                catch (Microsoft.SqlServer.Management.Common.ConnectionFailureException cfe)
                {                  
                    if (cfe.InnerException is SqlException)
                    {
                        if ((cfe.InnerException as SqlException).Number == 4060)
                        {                           
                            Microsoft.SqlServer.Management.Diagnostics.TraceHelper.LogExCatch(cfe);
                            // this exception occurs if the user doesn't have access to 
                            //  the database with the input name
                            //  in such a case the expected behavior is to return null  
                            return null;
                        }
                    }
                    throw cfe;
                }
                

			}
		}


		public void CopyTo(Database[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Database ItemById(int id)
		{
			return (Database)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(Database);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Database(this, key, state);
		}



















		public void Add(Database database) 
		{
			AddImpl(database);
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

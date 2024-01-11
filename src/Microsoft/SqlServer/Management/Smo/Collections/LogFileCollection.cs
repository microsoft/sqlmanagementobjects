// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class LogFileCollection : SimpleObjectCollectionBase
	{


















		internal LogFileCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}

		
		public LogFile this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as LogFile;
			}
		}


		// returns wrapper class
		public LogFile this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as LogFile;
                    
                



















			}
		}


		public void CopyTo(LogFile[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public LogFile ItemById(int id)
		{
			return (LogFile)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(LogFile);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new LogFile(this, key, state);
		}




		public void Remove(LogFile logFile)
		{
			if( null == logFile )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("logFile"));
			
			RemoveObj(logFile, new SimpleObjectKey(logFile.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(LogFile logFile) 
		{
			AddImpl(logFile);
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

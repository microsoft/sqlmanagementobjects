// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Data;

    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;


    /// <summary>
    ///base class for the post process classes</summary>
    internal abstract class PostProcess
    {
        Object m_ci;
        String m_objectName;
        Request m_req;
        SortedList m_HitFields;
        bool bLookUpOrdinalDone;

        //lookup for triggered columns in the result set. 
        object [] m_triggeredColumnsIdLookup;

        /// <summary>
        ///default constructor</summary>
        public PostProcess()
        {
            bLookUpOrdinalDone = false;
        }

        /// <summary>
        ///connection info</summary>
        internal Object ConnectionInfo
        {
            get	{ return m_ci; }
            set	{ m_ci = value; }
        }

        /// <summary>
        ///list of fields determined this post process to fire</summary>
        internal SortedList HitFields
        {
            get	{ return m_HitFields; }
            set	{ m_HitFields = value; }
        }

        /// <summary>
        ///number of fields determined this post process to fire</summary>
        internal int HitFieldsCount()
        {
            return m_HitFields.Count;
        }
 
        /// <summary>
        ///true is field caused this post process to fire</summary>
        internal bool GetIsFieldHit(string field)
        {
            return m_HitFields.ContainsKey(field);
        }

        /// <summary>
        ///level name</summary>
        internal string ObjectName
        {
            get	{ return m_objectName; }
            set	{ m_objectName = value; }
        }

        /// <summary>
        ///the user request that we are servicing</summary>
        internal Request Request
        {
            get	{ return m_req; }
            set	{ m_req = value; }
        }

        /// <summary>
        ///true if the lookup name-id has been init for triggered fields</summary>
        internal bool IsLookupInit()
        {
            return null != m_triggeredColumnsIdLookup;
        }

        /// <summary>
        ///true if this post process supports DataReader
        ///default is true</summary>
        protected virtual bool SupportDataReader
        {
            get { return true; }
        }

        /// <summary>
        ///throw exception if DataReader is not supported as the Request
        ///result type</summary>
        internal void CheckDataReaderSupport()
        {
            if( !this.SupportDataReader )
            {
                string props = string.Empty;
                bool bBegin = true;
                foreach(string f in this.HitFields.Keys)
                {
                    if( !bBegin )
                    {
                        props += " ,";
                    }
                    bBegin = false;
                    props += f;
                }
                throw new QueryNotSupportedEnumeratorException(StringSqlEnumerator.QueryNotSupportedPostProcess(props));
            }
        }
        
        /// <summary>
        ///init name-id lookup for triggered fields</summary>
        internal void InitNameBasedLookup(SqlObjectBase obj, StringCollection triggeredFields)
        {
            m_triggeredColumnsIdLookup = new object [triggeredFields.Count];
            int i = 0;
            foreach(string f in triggeredFields)
            {
                m_triggeredColumnsIdLookup[i++] = obj.GetAliasPropertyName(f);
            }
        }

        /// <summary>
        ///it assumes InitNameBasedLookup has already been called
        ///replaces AliasNames in m_triggeredColumnsIdLookup whit actual ordinal in the result set</summary>
        internal void UpdateFromNameBasedToOrdinalLookup(SortedList triggeredColumnsAliasNameLookup)
        {
            if( bLookUpOrdinalDone )
            {
                return;
            }
            bLookUpOrdinalDone = true;
            for(int i = 0; i < m_triggeredColumnsIdLookup.Length; i++)
            {
                m_triggeredColumnsIdLookup[i] = triggeredColumnsAliasNameLookup[m_triggeredColumnsIdLookup[i]];
            }
        }


        /// <summary>
        ///use to find a row in the dataset base on an string id column</summary>
        protected int BinarySearch(DataRowCollection col, string objectIdentifier, string columnName)
        {
            int start = 0; 
            int end = col.Count - 1;
            while( start <= end )
            {
                int mid = ( start + end ) / 2;
                int cmp = objectIdentifier.CompareTo(col[mid][columnName].ToString());
                if( -1 == cmp )
                {
                    end = mid - 1;
                }
                else if ( 1 == cmp )
                {
                    start = mid + 1;
                }
                else
                {
                    return mid;
                }
            }
            return -1;
        }

        /// <summary>
        ///use to find a row in the dataset base on an string id column
        ///and positions on first record</summary>
        protected int BinarySearchSetOnFirst(DataRowCollection col, string objectIdentifier, string columnName)
        {
            int row = BinarySearch(col, objectIdentifier, columnName);

            // if not found we're done
            if( row < 0 )
            {
                return row;
            }

            while( row > 0 && objectIdentifier == col[row - 1][columnName].ToString() )
            {
                row--;
            }
            
            return row;
        }

        /// <summary>
        ///true us data is DBNull</summary>
        protected bool IsNull(object data)
        {
            return data.GetType() == Type.GetType("System.DBNull");
        }

        /// <summary>
        ///true if field i in dp is DBNull</summary>
        protected bool IsNull(DataProvider dp, int i)
        {
            return IsNull(GetTriggeredObject(dp, i));
        }

        /// <summary>
        ///get value for triggered column i as Object</summary>
        protected object GetTriggeredObject(DataProvider dp, int i)
        {
            return dp.GetTrigeredValue((int)m_triggeredColumnsIdLookup[i]);
        }

        /// <summary>
        ///get value for triggered column i as Int32</summary>
        protected Int32 GetTriggeredInt32(DataProvider dp, int i)
        {
            return (Int32)GetTriggeredObject(dp, i);
        }

        /// <summary>
        ///get value for triggered column i as Bool</summary>
        protected bool GetTriggeredBool(DataProvider dp, int i)
        {
            return (bool)GetTriggeredObject(dp, i);
        }

        /// <summary>
        ///get value for triggered column i as String</summary>
        protected string GetTriggeredString(DataProvider dp, int i)
        {
            Object o = GetTriggeredObject(dp, i);
            if( IsNull(o) )
            {
                return null;
            }

            return (string)o;
        }

        /// <summary>
        ///aplly post process for data
        ///receive column name, column curent value, DataProvider for the result set
        ///return the new column value
        ///called for every row</summary>
        public abstract object GetColumnData(string name, object data, DataProvider dp);

        /// <summary>
        ///clean any state that was constructed while the rowset was processed</summary>
        public virtual void CleanRowData()
        {
        }
    }
}

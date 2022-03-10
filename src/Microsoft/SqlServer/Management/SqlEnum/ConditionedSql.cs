// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;

    ///<summary>
    ///class encapsulateing a conditioned sql
    ///that is a sql the is neccessary only when a property is requested</summary>
    [ComVisible(false)]
	internal abstract class ConditionedSql
	{
		bool m_used;	// is this conditioned sql already used ? ( important because it can be used only once )
		LinkMultiple m_LinkMultiple;
		StringCollection m_cols;

		///<summary>
		///default constructor</summary>
		protected ConditionedSql()
		{
			m_used = false;
			m_cols = new StringCollection();
		}

		///<summary>
		///set the triggering fields</summary>
		public void SetFields(StringCollection fields)
		{
			m_cols = fields;
		}

		///<summary>
		///get the list of triggering fields</summary>
		internal StringCollection Fields
		{
			get
			{
				return m_cols;
			}
		}

		///<summary>
		/// is this conditioned sql already used ? ( important because it can be used only once )</summary>
		public bool IsUsed
		{
			get
			{
				return m_used;
			}
		}

		///<summary>
		///return true if the field has caused the sql to be added</summary>
		public bool IsHit(String field)
		{
			if( !AcceptsMultipleHits && m_used )
            {
                return false;
            }

            return TestHit(field);
		}

		///<summary>
		///test if the field would make the tsql to be added
		/// ( it does not look if it was already used )</summary>
		protected bool TestHit(String field)
		{
			return IsDefault() || m_cols.Contains(field);
		}

		///<summary>
		///this sql must always be added no matter what fields are requested</summary>
		public bool IsDefault()
		{
			return 0 == m_cols.Count;
		}

		///<summary>
		///can be added multiple times</summary>
		protected virtual bool AcceptsMultipleHits
		{
			get { return false; }
		}

		///<summary>
		///has been added at least once</summary>
		public bool Used
		{
			get
			{
				return m_used;
			}
		}

		///<summary>
		///mark this sql as used</summary>
		public void MarkHit()
		{
			m_used = true;
		}

		///<summary>
		///mark this sql as not used</summary>
		public void ClearHit()
		{
			m_used = false;
		}

		///<summary>
		///get the link multiple </summary>
		public LinkMultiple LinkMultiple
		{
			get
			{
				return m_LinkMultiple;
			}
			set
			{
				m_LinkMultiple = value;
			}
		}

		///<summary>
		///intialize link multiple</summary>
		public void AddLinkMultiple(XmlReadMultipleLink xrmpl)
		{
			if( null != xrmpl )
			{
				m_LinkMultiple = new LinkMultiple();
				m_LinkMultiple.Init(xrmpl);
			}
		}

		///<summary>
		///get the list of fields for which we need their value</summary>
		public ArrayList LinkFields
		{
			get 
			{ 
				if( null == m_LinkMultiple )
				{
					return null;
				}
				return m_LinkMultiple.LinkFields; 
			}
		}

		///<summary>
		///add hit for the field from the object ( level ) obj, add record in the StamentBuilder sb</summary>
		public abstract void AddHit(string field, SqlObjectBase obj, StatementBuilder sb);
	}

	///<summary>
	/// a list of CondtionedSql</summary>
	[ComVisible(false)]
	internal class ConditionedSqlList
	{
		ArrayList m_conditionedSqlList;

		///<summary>
		///default constructor</summary>
		public ConditionedSqlList()
		{
			m_conditionedSqlList = new ArrayList();
		}

		///<summary>
		///how many conditioned sql are there</summary>
		public int Count
		{
			get { return m_conditionedSqlList.Count;}
		}

		///<summary>
		///add a conditioned sql</summary>
		public void Add(ConditionedSql obj)
		{
			m_conditionedSqlList.Add(obj);
		}

		///<summary>
		///clear used mark for all conditioned sqls</summary>
		public void ClearHits()
		{
			foreach(ConditionedSql scs in m_conditionedSqlList)
			{
				scs.ClearHit();
			}
		}

		///<summary>
		///add hit for every cionditioned sql</summary>
		public bool AddHits(SqlObjectBase obj, String field, StatementBuilder sb)
		{
			bool bIsHit = false;
			foreach(ConditionedSql scs in m_conditionedSqlList)
			{
				if( scs.IsHit(field) )
				{
					bIsHit = true;
					scs.MarkHit();
					scs.AddHit(field, obj, sb);
				}
			}
			return bIsHit;
		}

		///<summary>
		///add default conditioned sql </summary>
		public void AddDefault(StatementBuilder sb)
		{
			foreach(ConditionedSql scs in m_conditionedSqlList)
			{
				if( scs.IsDefault() )
				{
					scs.MarkHit();
				}
			}
		}

		///<summary>
		///int indexer</summary>
		public ConditionedSql this[int i]
		{
			get
			{
				return (ConditionedSql)m_conditionedSqlList[i];
			}
		}

		///<summary>
		///get an enumerator for foreach</summary>
		public IEnumerator GetEnumerator() 
		{
			return new ConditionedSqlListEnumerator(m_conditionedSqlList.GetEnumerator());
		}

		///<summary>
		/// nested enumerator class
		/// we need that to override the behaviour of SortedList
		/// that exposes an IEnumerator interface</summary>
		internal sealed class ConditionedSqlListEnumerator : IEnumerator 
		{
			private IEnumerator baseEnumerator;

			///<summary>
			///constructor</summary>
			internal ConditionedSqlListEnumerator(IEnumerator enumerator) 
			{
				this.baseEnumerator = enumerator;
			}

			///<summary>
			///get  current conditioned sql</summary>
			object IEnumerator.Current 
			{ 
				get 
				{	
					return baseEnumerator.Current;
				} 
			}
			
			///<summary>
			///move one position forward</summary>
			public bool MoveNext() 
			{
				return baseEnumerator.MoveNext();
			}

			///<summary>
			///reset the enumerator</summary>
			public void Reset() 
			{
				baseEnumerator.Reset();
			}
		}
	}

}

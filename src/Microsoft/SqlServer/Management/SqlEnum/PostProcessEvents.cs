// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Data;
    using Microsoft.SqlServer.Management.Sdk.Sfc;


    internal abstract class PostProcessDdlEvents : PostProcess
	{
		DataTable m_dtEvents;

		public PostProcessDdlEvents()
		{
			m_dtEvents = null;
		}

		protected DataTable GetEventsForAllRows()
		{
			if( null == m_dtEvents )
			{
				Request reqtext = new Request();
				reqtext.Urn = this.Request.Urn.Value + "/Event";

				reqtext.ResultType = ResultType.DataTable;
				reqtext.RequestFieldsTypes = RequestFieldsTypes.Request;
				reqtext.Fields = new String[] { "ObjectIdentifier", "EventTypeDescription" };

				reqtext.OrderByList = new OrderBy [] 
					{
						new OrderBy("ObjectIdentifier", OrderBy.Direction.Asc),
				};

				m_dtEvents = Enumerator.GetData(this.ConnectionInfo, reqtext);
			}
			return m_dtEvents;
		}

		protected abstract Object GetTriggerEvents(string objectIdentifier);

		/// <summary>
		/// Returns the new value for column
		/// </summary>
		/// <param name="name">column name</param>
		/// <param name="data">data that is already present in the column</param>
		/// <param name="dp">stores the result that we are going to return
		/// as a value for this column. It abstracts the data source (DataReader or Data Table)</param>
		/// <returns>new column value</returns>
		public override object GetColumnData(string name, object data, DataProvider dp)
		{
			if( IsNull(dp, 0) )
			{
				return System.DBNull.Value;
			}

			return GetTriggerEvents(GetTriggeredString(dp, 0));
		}

		protected override bool SupportDataReader
		{
			get 
			{ 
				return false;
			}
		}

		public override void CleanRowData()
		{
			m_dtEvents = null;
		}

	}

	internal partial class PostProcessDatabaseDdlTriggerEvents : PostProcessDdlEvents
	{
		public PostProcessDatabaseDdlTriggerEvents() : base()
		{
		}

		protected override Object GetTriggerEvents(string objectIdentifier)
		{
			DataTable dtEvents = GetEventsForAllRows();
			int i = BinarySearchSetOnFirst(dtEvents.Rows, objectIdentifier, "ObjectIdentifier");

			if (0 > i)
			{
				return System.DBNull.Value; // this can't be
			}

			DatabaseDdlTriggerEventSet evs = new DatabaseDdlTriggerEventSet();
			do
			{
				DatabaseDdlTriggerEvent evtCode = DatabaseDdlTriggerEventOffsetFromEventString(dtEvents.Rows[i]["EventTypeDescription"].ToString());
				evs.SetBitAt((int)evtCode.Value, true);
			}
			while (++i < dtEvents.Rows.Count && 
					objectIdentifier == dtEvents.Rows[i]["ObjectIdentifier"].ToString());

			return evs;
		}

	}

	internal partial class PostProcessServerDdlTriggerEvents : PostProcessDdlEvents
	{
		public PostProcessServerDdlTriggerEvents() : base()
		{
		}

		protected override Object GetTriggerEvents(string objectIdentifier)
		{
			DataTable dtEvents = GetEventsForAllRows();
			int i = BinarySearchSetOnFirst(dtEvents.Rows, objectIdentifier, "ObjectIdentifier");

			if( 0 > i )
			{
				return System.DBNull.Value; // this can't be
			}

			ServerDdlTriggerEventSet evs = new ServerDdlTriggerEventSet();
			do
			{
				ServerDdlTriggerEvent evtCode = ServerDdlTriggerEventOffsetFromEventString(dtEvents.Rows[i]["EventTypeDescription"].ToString());
				evs.SetBitAt((int)evtCode.Value, true );
			}
			while(	++i < dtEvents.Rows.Count && 
				objectIdentifier == dtEvents.Rows[i]["ObjectIdentifier"].ToString() );

			return evs;
		}

	}
}

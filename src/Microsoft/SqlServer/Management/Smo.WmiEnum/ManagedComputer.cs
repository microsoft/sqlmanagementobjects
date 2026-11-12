namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	using System;
	using System.Data;
	using System.Globalization;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

	internal class EnumManagedComputer : EnumObject
	{
		public EnumManagedComputer()
		{
			ObjectProperty op = null;

			op = new ObjectProperty();
			op.Name = "Name";
			op.Type = "System.String";
			op.Usage = ObjectPropertyUsages.Request;
			AddProperty(op);
		}

		public override EnumResult GetData(EnumResult erParent)
		{
			String sQueryName = this.GetFixedStringProperty("Name", true);
			if( null == sQueryName )
				throw new InternalEnumeratorException(StringWmiEnumerator.MachineNameMustBeSpecified);

			if( this.Request is WmiRequest )
			{
				string scope = "root/Microsoft/SqlServer/ComputerManagement" + AssemblyVersionInfo.MajorVersion.ToString(CultureInfo.InvariantCulture);
				if( String.Empty != sQueryName )
				{
					scope = "\\\\" + sQueryName + "/" + scope;
				}

				return new WmiEnumResult(scope);
			}
			
			DataTable dt = new DataTable();
			dt.Locale = CultureInfo.InvariantCulture;
			dt.Columns.Add(new DataColumn("Name", System.Type.GetType("System.String")));
			DataRow row = dt.NewRow();
			row["Name"] = sQueryName;
			dt.Rows.Add(row);

			ResultType res = Request.ResultType;
			if( ResultType.Default == res )
				res = ResultType.DataSet;

			if( ResultType.DataSet == res )
			{
				DataSet ds = new DataSet();
				ds.Locale = CultureInfo.InvariantCulture;
				ds.Tables.Add(dt);
				return new EnumResult(ds, res);
			}
			else if( ResultType.DataTable == res )
			{
				return new EnumResult(dt, res);
			}
			throw new ResultTypeNotSupportedEnumeratorException(res);
		}

		public override ResultType[] ResultTypes
		{
			get	{return new ResultType[2] { ResultType.DataSet, ResultType.DataTable };	}
		}
	}
}

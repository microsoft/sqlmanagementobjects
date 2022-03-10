// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo.Broker
{
    using System;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Text;

    internal class ServiceBrokerSqlObject : SqlObject
	{
		public override Assembly ResourceAssembly
		{
			get
			{
				return Assembly.GetExecutingAssembly();
			}
		}
	}

    internal class Util
    {
        // This is copy and paste from //depot/SQLMain/sql/komodo/src/shared/Smo/Enumerator/sql/src/Util.cs
        // Initially I made a class there public, but after a conversation with Marius we desided to have a
        // copy because of FxCop and documentaion implications. In the future we might make these assemblies
        // friends and merge implementations.

		//stops when a name is completed
		internal static String UnEscapeString(String escapedValue, char startEscapeChar, char escapeChar, ref int index)
		{
			StringBuilder sb = new StringBuilder();
			bool delete = false;
			bool needTerminator = false;

			char c;
			for(; index < escapedValue.Length; index++)
			{
				c = escapedValue[index];
				if( false == needTerminator && startEscapeChar == c )
				{
					needTerminator = true;
					continue;
				}
				else if( escapeChar == c )
				{
					if( false == delete )
					{
						delete = true;
						continue;
					}
					delete = false;
				}
				else if( true == delete && true == needTerminator )
				{
					break;
				}
				sb.Append(c);
			}
			return sb.ToString();
		}

		internal static StringCollection SplitNames(string name)
		{
            if (name == null) 
            {
                return null;
            }

			StringCollection listNames = new StringCollection();

			string s;
			int pos = -1;
			while(true)
			{
				++pos;
				s = Util.UnEscapeString(name, '[', ']', ref pos);
				if( s.Length > 0 )
				{
					listNames.Insert(0, s);
				}
				else
				{
					break;
				}
			}

            return listNames;
		}

    }

	internal class PostProcessSplitTriPartName : Microsoft.SqlServer.Management.Smo.PostProcess
	{
		StringCollection m_listNames = null;

        public override object GetColumnData(string name, object data, DataProvider dp)
		{
            if (m_listNames == null) 
            {
                string triggeredString = this.GetTriggeredString(dp, 0);
                if (triggeredString == null) 
                {
                    return data;
                }

                m_listNames = Util.SplitNames(triggeredString);
            }

			int pos = 0; //ProcedureName

			switch(name)
			{
				case "ProcedureSchema":pos = 1;break;
				case "ProcedureDatabase":pos = 2;break;
			}

			if( pos >= m_listNames.Count )
			{
				return data;
			}
			return m_listNames[pos];
		}

		public override void CleanRowData()
		{
			m_listNames = null;
		}

	}
}

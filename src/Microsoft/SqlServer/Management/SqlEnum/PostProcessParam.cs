// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using Microsoft.SqlServer.Management.Common;

    internal class PostProcessParam : PostProcess
	{
		SortedList m_textList;

		public PostProcessParam()
		{
			m_textList = new SortedList();
		}

		protected override bool SupportDataReader
		{
			get { return false; }
		}

/* sample stored procedure
 CREATE PROCEDURE insert_All_Types
	@numrows	int		=1,
	@charval        char(255)	='charval',
	@vcharval       varchar(255)	='vcharval',
	@textval        varchar(255)	='textval',
	@binaryval      binary(255)	=0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF,
	@vbinaryval     varbinary(255)	=0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF,
	@imageval       varbinary(255)	=0xAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA,
	@int1val        tinyint		=189,
	@int2val        smallint	=32767,
	@int4val        int		=21474836,
	@flt8val        float		=6.022E-23,
	@flt4val        real		=6.102E+14,
	@bitval         bit		=1,
	@moneyval       money		=314159265.665,
	@money4val      smallmoney	=112654.22,
	@dateval        datetime	='1/1/1753 14:30:20:999',
	@date4val       smalldatetime	='5/6/2079 14:30',
	@numericval     numeric(28,14)	=321654321.654987321,
	@decimalval     decimal(28,14)	=98765432164598.90879657
AS BEGIN
    print (N'Juts a test') 
END
*/

		private const string sSingleLineCommentSql = "(--[^\n]*)";
		private const string sSingleLineCommentC = "(//[^\n]*)";
        // In t-sql multiline comments can be nested. (unlike c# or c++). The following regular expression
        // matches the nested multiline comments. The regular expression actually keeps a count as DEPTH for the
        // nesting level and then matches the count to 0.-anchals
		private const string sNestedMultiLineComment = @"(/\*(?>/\*(?<DEPTH>)|\*/(?<-DEPTH>)|(.|[" + "\n" + @"])?)*(?(DEPTH)(?!))\*/)";
        // for performance reasons we do the depth counting after 2 levels of nesting.
        // before the change was made for nested multiline comments the multiline comment regex was: @"(/\*((\*(?=[^/]))|([^\*]))*\*/)"
        // The optimization is to use the multiline comment regex while a nested comment is not encountered.
        // the match starts with a /* then the next expression could be one of following:-
        //- not a / and not a *
        //- not a * followed by /
        //- not a / followed by *
        // any number of times these are matched.
        // If it doesn't match these then the third state is to match the whole of the nested multiline comment expression that starts with a /* i.e. only if a /* is found
        // The multiline comment does depth counting which appears to be the bottleneck for performance.
        // Then finally everything should be matched with a final trailing */ to close the starting expression.-anchals
        private const string sNestedMultiLineCommentOptimized = @"(/\*(([^/\*])|(\*(?=[^/]))|(/(?=[^\*])))*" + "|" + sNestedMultiLineComment + @"\*/)";
		private const string sSingleQuotedStringSql = "(N{0,1}'(('')|[^'])*)'"; 
		private const string sDoubleQuotedString = "(\"((\"\")|[^\"])*\")"; 
		private const string sBraketedWord = @"(\[((\]\])|[^\]])*\])";
		private const string sWord = @"([\w_][\w;\d_]*)";
		private const string sNumber = @"((\+|\-){0,1}((\d+\.\d*)|(\d*\.\d+)|(\d+))(e((\+)|(\-))\d+){0,1})"; //integer float scientific notation
		private const string sHexNumber = @"(0x[0-9a-f]+)"; //cannot have . or scientific notation
		//a parameter is a number ( decimel, hex, scientific ) a string a word or a braketed word
		private const string sParamValueQI = @"(?<val>(("+sSingleQuotedStringSql+")|("+sHexNumber+")|("+sNumber+")|("+sBraketedWord+")|("+sWord+")))";
		private const string sParamValue = @"(?<val>(("+sDoubleQuotedString+")|("+sSingleQuotedStringSql+")|("+sHexNumber+")|("+sNumber+")|("+sBraketedWord+")|("+sWord+")))";
		private const string sComma = @"(?<comma>,)";
		private const string sParanthesis = @"(\([\d, ]*\))";
		private const string sEq = @"(?<eq>=)";
		private const string sDelim = @"(?<delim>\b((AS)|(RETURNS))\b)"; 
		private const string sParam = @"(?:(?<param>@[\w_][\w\d_$$@#]*)((\s)|("+sSingleLineCommentSql+")|("+sNestedMultiLineCommentOptimized+"))*(AS){0,1})"; // M00_IDENT (in DdlParser.cs)
		private const string sGrammarQI = sNestedMultiLineCommentOptimized +"|"+ sSingleLineCommentSql +"|"+ sDoubleQuotedString +"|"+ sSingleLineCommentC+
			"|"+ sDelim +"|"+sParam +"|"+sParamValue+"|"+sComma+"|"+sEq+"|"+sParanthesis;
		private const string sGrammar = sNestedMultiLineCommentOptimized +"|"+ sSingleLineCommentSql +"|"+ sSingleLineCommentC+
			"|"+ sDelim +"|"+sParam +"|"+sParamValue+"|"+sComma+"|"+sEq+"|"+sParanthesis;

		private Regex m_r;

        static Regex sRegexQI;
        static Regex sRegex;
        static PostProcessParam()
        {
            // we don't use compiled option for creating regular expressions so that they are optimized for performance. see vsts#197125-anchals
            sRegexQI = new Regex(sGrammarQI, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            sRegex = new Regex(sGrammar, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
        }

        private void ParseParams(String sKey, String text, bool bQI)
		{
			m_textList[sKey] = new SortedList();

			if( null == m_r )
			{
				if( bQI )
				{
					m_r = sRegexQI;
				}
				else
				{
                    m_r = sRegex;
				}
			}

			String lastParam = null;
			bool bEq = false;
			for(Match m = m_r.Match(text); m.Success; m = m.NextMatch())
			{
				//did we find the end of the header ?
				if( m.Groups["delim"].Success )
				{
					break;
				}
				//did we find an '=' ? if so after this will be a default value
				if( m.Groups["eq"].Success )
				{
					bEq = true;
				}
				//if we found a comma this parameter has finished, no default value
				if( m.Groups["comma"].Success )
				{
					bEq = false;
					lastParam = null;
				}
				//if paramter value format and after equal and after a parameter name
				//than we found a default value
				if( true == bEq && null != lastParam && m.Groups["val"].Success )
				{
					((SortedList)m_textList[sKey])[lastParam] = m.Groups["val"].Value;
					bEq = false; //ok we've got the value
				}
				if( m.Groups["param"].Success )
				{
					lastParam = m.Groups["param"].Value;
				}
			}
		}

		String GetText(int id, String sDatabase, int number, Object ci, ref bool bQI)
		{
			String query = null;

            ServerVersion sVersion = ExecuteSql.GetServerVersion(ci);

            if (sVersion.Major >= 9)  //Yukon or later
            {
                query = String.Format(CultureInfo.InvariantCulture, "select c.definition,convert(bit,OBJECTPROPERTY(c.object_id,N'ExecIsQuotedIdentOn')) from [{0}].sys.sql_modules c where c.object_id = <msparam>{1}</msparam>", Util.EscapeString(sDatabase, ']'), id);
            }
            else //Shiloh or earlier
            {
                if (number > 0)
                {
                    query = String.Format(CultureInfo.InvariantCulture, "select c.text,convert(bit,OBJECTPROPERTY(c.id,N'ExecIsQuotedIdentOn')) from [{0}].dbo.syscomments c where c.id = <msparam>{1}</msparam> and c.number = <msparam>{2}</msparam> order by c.colid", Util.EscapeString(sDatabase, ']'), id, number);
                }
                else
                {
                    query = String.Format(CultureInfo.InvariantCulture, "select c.text,convert(bit,OBJECTPROPERTY(c.id,N'ExecIsQuotedIdentOn')) from [{0}].dbo.syscomments c where c.id = <msparam>{1}</msparam> order by c.colid", Util.EscapeString(sDatabase, ']'), id);
                }
            }

			DataTable dtText = ExecuteSql.ExecuteWithResults(query, ci);

			String sDefinitionData = String.Empty;
			foreach(DataRow rowText in dtText.Rows)
			{
				sDefinitionData += rowText[0].ToString();
			}
			if( dtText.Rows.Count > 0 )
			{
				Object o = dtText.Rows[0][1];
				if( !(o is DBNull) )
				{
					bQI = Boolean.Parse(o.ToString());
				}
			}

			return sDefinitionData;
		}

		bool IsProcessed(string sKey)
		{
			return null != m_textList[sKey];
		}

		string GetParam(string sKey, string sParamName)
		{
			string s = (string)(((SortedList)m_textList[sKey])[sParamName]);
			if( null == s )
			{
				return String.Empty;
			}
			return s;
		}

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            // Return data back if any present already
            if (data!=null && !(data is DBNull))
            {
                return data;
            }

            bool bIsParentSys = GetTriggeredBool(dp, 3);
            if (bIsParentSys)
            {
                // return empty string as default, consistent with the 
                // behavior of GetParam
                return String.Empty;
            }

            int id = GetTriggeredInt32(dp, 0);
            String sDatabase = GetTriggeredString(dp, 1);
            String sParamName = GetTriggeredString(dp, 2);
            object oNumber = GetTriggeredObject(dp, 4);
            int number = 0;
            if (oNumber is int)
            {
                number = (int)oNumber;
            }
            else
            {
                number = (short)oNumber;
            }

            String sKey = id.ToString(CultureInfo.InvariantCulture) + sDatabase + number.ToString(CultureInfo.InvariantCulture);

            if (!IsProcessed(sKey))
            {
                bool bQI = false;
                String sDefinitionData = GetText(id, sDatabase, number, this.ConnectionInfo, ref bQI);
                ParseParams(sKey, sDefinitionData, bQI);
            }

            return GetParam(sKey, sParamName);

        }
	}
}

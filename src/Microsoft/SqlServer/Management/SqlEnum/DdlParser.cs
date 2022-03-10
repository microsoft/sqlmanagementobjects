// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
#if STRACE
    using Microsoft.SqlServer.Management.Diagnostics;
#endif

    ///used by function CheckDdlHeader and ReadNameFromDdl
    ///to return headerInformation
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct DdlTextParserHeaderInfo
    {
        ///true if the script contains a CREATE keyword, false eitherwise
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public bool scriptForCreate;

        ///index in text where the CREATE keyword starts
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexCreate;

        ///true if the script contains a OR ALTER keyword, false otherwise
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public bool scriptContainsOrAlter;

        ///index in text where the OR ALTER keyword starts
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexOrAlterStart;

        ///index in text where the OR ALTER keyword ends
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexOrAlterEnd;

        ///index in text where the name starts, this is the full name including schema
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexNameStart;
        
        ///first index that is not part of the name anymore, this also includes the proc number
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexNameEnd;
        
        ///the type of object for text ex.:PROC, TRIGGER, VIEW etc.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string objectType;
        
        ///schema as extracted from the header
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string schema;
        
        ///name as extracted from the header
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string name;

        /// Database name as extracted from the header
        /// Will only be meaningful when we have a three part name, currently only
        /// the name of the table on which a trigger is created
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string database;
        
        ///if the text is a procedure with the number specified, it will 
        ///contain semicolon + number. ex.:";1" ";2" or ";3" etc.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string procedureNumber;

        ///start index of a name of an object used inside the text
        ///currently used for the table triger
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexNameStartSecondary;
        
        ///one beyond end index of a name of an object used inside the text
        ///currently used for the table triger
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int indexNameEndSecondary;

        ///schema as extracted from inside the header
        ///currently used for the schema of a table inside the triger text
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string schemaSecondary;			
        
        ///name as extracted from inside the header
        ///currently used for the name of a table inside the triger text
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string nameSecondary;

        ///database name as extracted from inside the header
        ///currently used for the name of a table inside the triger text
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public string databaseSecondary;
    }

    internal class DdlTextParserSingleton
    {
        internal bool hasParanthesis;
        internal string returnTableVariableName;
        internal Regex regex;
        internal Regex m_r_end;
    }
    ///<summary>
    ///encapsulates parsing text of database text objects</summary>
    [ComVisible(false)]
    internal class DdlTextParser
    {
        //
        // M00_IDENT: According to BOL (ms-help://MS.SQLCC.v9/MS.SQLSVR.v9.en/udb9/html/171291bb-f57f-4ad1-8cea-0b092d5d150c.htm),
        // regular identifiers can start with Unicode characters or special characters '_', '@' and '#'. Subsequent characters might
        // also include numbers and '$'.
        // Character '$' can be used in the middle of any identifier. Refer to bug 407863 for detailed repro.
        // Characters '@' and '#' have special meaning and should not appear in the context where DdlTextParser is used today,
        // but are included for completeness.
        //

        // Allow CL or LF as valid newline in comments
        private readonly static string sSingleLineCommentSql = "(--[^\n\r]*)";
        private readonly static string sSingleLineCommentC = "(//[^\n\r]*)";
        // In t-sql multiline comments can be nested. (unlike c# or c++). The following regular expression
        // matches the nested multiline comments. The regular expression actually keeps a count as DEPTH for the
        // nesting level and then matches the count to 0.-anchals
        private readonly static string sNestedMultiLineComment = @"(/\*(?>/\*(?<DEPTH>)|\*/(?<-DEPTH>)|(.|[" + "\n" + @"])?)*(?(DEPTH)(?!))\*/)";
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
        private readonly static string sNestedMultiLineCommentOptimized = @"(/\*(([^/\*])|(\*(?=[^/]))|(/(?=[^\*])))*\*/" + "|" + sNestedMultiLineComment + @")";
        private readonly static string sSingleQuotedString = "('(('')|[^'])*)'";
        private readonly static string sDoubleQuotedString = "(?<quoted_word>\"((\"\")|[^\"])*\")"; 
        private const string sWord = @"(?<word>[\w_@#][\w\d_$$@#]*)"; // Read about identifiers above (M00_IDENT)
        private readonly static string sBraketedWord = @"(?<braket_word>\[((\]\])|[^\]])*\])";
        private const string sProcNumber = @"(?<number_proc>;[\d]+)";
        private readonly static string sDot = @"(?<dot>\.)";
        private readonly static string sPharanthesis = @"(?<parant_open>\()|(?<parant_close>\))";
        private readonly static string sParam = @"(?:(?<param>@[\w_@#][\w\d_$$@#]*)((\s)|(" + sSingleLineCommentSql + ")|(" + sNestedMultiLineCommentOptimized + "))*(AS){0,1})"; // Read about identifiers above (M00_IDENT)
        private readonly static string sExec = @"(?<exec>\bEXECUTE\b)";
        private readonly static string sReturns = @"(?<returns>\bRETURNS\b)";
        private readonly static string sReturnsTable = @"(?<returns_table>\bRETURNS\s+TABLE\b)";
        private readonly static string sReturn = @"(?<return>\bRETURN\b)";
        private readonly static string sDelim1 = @"(?<delim1>\bAS\b\s*)";
        private readonly static string sDelim2 = @"(?<delim2>\bBEGIN\b)";
        internal readonly static DdlTextParserSingleton ddlTextParserSingleton =
            new DdlTextParserSingleton();
        

        ///<summary>
        ///default constructor , initializes grammars</summary>
        static DdlTextParser()
        {
            string sGrammar = sNestedMultiLineCommentOptimized + "|" + sSingleLineCommentSql + "|" + sSingleQuotedString + "|" + sDoubleQuotedString + "|" + sBraketedWord + "|" + sSingleLineCommentC +
                "|" + sParam + "|" + sDelim1 + "|" + sDelim2 + "|" + sPharanthesis + "|" + sExec + "|" + sReturnsTable + "|" + sReturns + "|" + sReturn + "|" + sDot + "|" + sWord + "|" + sProcNumber;
            // we don't use compiled option for creating regular expressions so that they are optimized for performance. see vsts#197125 -anchals
            ddlTextParserSingleton.regex = new Regex(sGrammar, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            
            string sGrammarEnd = sNestedMultiLineCommentOptimized_end + "|" + sSpace_end + "|" + sWord_end + "|" + sEol_end + 
                "|" + sSingleLineComment_end + "|" + sReject_end;
            ddlTextParserSingleton.m_r_end = new Regex(sGrammarEnd, RegexOptions.ExplicitCapture | RegexOptions.RightToLeft);
        }

        ///<summary>
        ///get index name start, index name end ( includes the eventual procedure number )  and 
        ///the name and eventually procedure number 
        ///the parsing is based on quoted identifier status</summary>
        private static bool ReadNameFromDdl(ref Match m, bool useQuotedIdentifier, ref DdlTextParserHeaderInfo headerInfo)
        {
            //
            //we read until we find something that is not a word, braket_word or quoted_word
            //in each case we take care that the name that we store is braketed
            //
            
            for(m = m.NextMatch(); m.Success; m = m.NextMatch())
            {
                if( m.Groups["word"].Success )
                {
                    headerInfo.name = "[" + Util.EscapeString(m.Groups["word"].Value, ']') + "]";
                    headerInfo.indexNameStart = m.Groups["word"].Index;
                    headerInfo.indexNameEnd = headerInfo.indexNameStart + m.Groups["word"].Length;
                    break;
                }
                if( m.Groups["braket_word"].Success )
                {
                    headerInfo.name = m.Groups["braket_word"].Value;
                    headerInfo.indexNameStart = m.Groups["braket_word"].Index;
                    headerInfo.indexNameEnd = headerInfo.indexNameStart + m.Groups["braket_word"].Length;
                    break;
                }
                if( m.Groups["quoted_word"].Success )
                {
                    if( false == useQuotedIdentifier )
                    {
                        return false;
                    }
                    headerInfo.name = m.Groups["quoted_word"].Value;
                    headerInfo.indexNameStart = m.Groups["quoted_word"].Index;
                    headerInfo.indexNameEnd = headerInfo.indexNameStart + m.Groups["quoted_word"].Length;
                    int idx = 0;
                    headerInfo.name = "[" + Util.UnEscapeString(headerInfo.name, '"', '"', ref idx) + "]";
                    break;
                }
            }

            //
            //we found something that is not a 'word', 
            //there's only one more thing of interested that we may have found
            //check if we can extract a procedure number
            //
            
            if( m.Success ) 
            {
                m = m.NextMatch();
                if (m.Groups["number_proc"].Success)
                {
                    headerInfo.procedureNumber = m.Groups["number_proc"].Value;
                    headerInfo.indexNameEnd = headerInfo.indexNameEnd + m.Groups["number_proc"].Length;
                }
            }
            return headerInfo.name.Length > 0;
        }

        ///<summary>
        ///get index name start, index name end ( includes the eventual procedure number )  and 
        ///the name, includes eventual schema and procedure number 
        ///the parsing is based on quoted identifier status
        ///sample valid names: [sss].[nnn], "sss"."nnn", sss.nnn, [nnn], "nnn", nnn, [sss]."nnn", "sss".[nnn], [sss].[nnn];1, ...
        ///</summary>
        private static bool ReadFullNameFromDdl(ref Match m, bool useQuotedIdentifier, ref DdlTextParserHeaderInfo headerInfo)
        {
            // get name and procedure number if available
            if( !ReadNameFromDdl(ref m, useQuotedIdentifier, ref headerInfo) )
            {
                return false;
            }

            // check if ReadNameFromDdl stopped in a '.'
            // than it means we only read the Schema;
            // store it and make another pass to
            // get name and procedure number if available
            if( m.Groups["dot"].Success )
            {
                headerInfo.schema = headerInfo.name;
                headerInfo.name = string.Empty;

                int idxSchemaStart = headerInfo.indexNameStart;

                headerInfo.procedureNumber = string.Empty;
                if( !ReadNameFromDdl(ref m, useQuotedIdentifier, ref headerInfo) )
                {
                    return false;
                }
                headerInfo.indexNameStart = idxSchemaStart;

                if (m.Groups["dot"].Success)
                {
                    // this means the name is a three part name, so we have to 
                    // do one more loop to extract the database name
                    headerInfo.database = headerInfo.schema;
                    headerInfo.schema = headerInfo.name;
                    headerInfo.name = string.Empty;

                    headerInfo.procedureNumber = string.Empty;
                    if (!ReadNameFromDdl(ref m, useQuotedIdentifier, ref headerInfo))
                    {
                        return false;
                    }
                    headerInfo.indexNameStart = idxSchemaStart;

                }
            }

            //check some basic rules
            TraceHelper.Assert(headerInfo.name.Length > 0 && headerInfo.indexNameStart > 0 && 
                        headerInfo.indexNameEnd > headerInfo.indexNameStart);
            return true;
        }

        ///<summary>
        ///based on forAlter the text for create it must start with create,for alter it must start with create or alter
        ///return index for name,( includes schema, name, proc number ) the name, schema and proc number
        ///</summary>
        public static bool CheckDdlHeader(string objectText, bool useQuotedIdentifier, out DdlTextParserHeaderInfo headerInfo)
        {
            return CheckDdlHeader(objectText, useQuotedIdentifier: useQuotedIdentifier, isOrAlterSupported: false, headerInfo: out headerInfo);
        }

        ///<summary>
        ///based on forAlter the text for create it must start with create,for alter it must start with create or alter
        ///create or alter is supported after SqlVersion.120
        ///return index for name,( includes schema, name, proc number ) the name, schema and proc number
        ///</summary>
        public static bool CheckDdlHeader(string objectText, bool useQuotedIdentifier, bool isOrAlterSupported, out DdlTextParserHeaderInfo headerInfo)
        {
            //init data
            // 0 is a valid value for indexes in text so initialize them to -1
            // initialize the string to empty string for easy checking
            headerInfo = new DdlTextParserHeaderInfo();
            headerInfo.indexCreate = -1;
            headerInfo.scriptContainsOrAlter = false;
            headerInfo.indexOrAlterStart = -1;
            headerInfo.indexOrAlterEnd = -1;
            headerInfo.indexNameEnd = -1;
            headerInfo.indexNameStart = -1;
            headerInfo.name = "";
            headerInfo.objectType = "";
            headerInfo.procedureNumber = "";
            headerInfo.schema = "";
            headerInfo.scriptForCreate = true;
            headerInfo.indexNameEndSecondary = -1;
            headerInfo.indexNameStartSecondary = -1;
            headerInfo.nameSecondary = "";
            headerInfo.schemaSecondary = "";			

            
            //	Search for a CREATE, an ALTER or CREATE OR ALTER keyword
            Match m = ddlTextParserSingleton.regex.Match(objectText);
            for(; m.Success; m = m.NextMatch())
            {
                if( m.Groups["word"].Success )
                {
                    //for handling ALTER script
                    if ( !headerInfo.scriptForCreate )
                    {
                        break;
                    }

                    //for handling CREATE script when OrAlter is not supported
                    if ( headerInfo.indexCreate != -1 && !isOrAlterSupported )
                    {
                        break;
                    }

                    //for create it must start with create.
                    //for alter it must start with create or alter
                    if( 0 == String.Compare("create", m.Groups["word"].Value, StringComparison.OrdinalIgnoreCase) )
                    {
                        headerInfo.indexCreate = m.Groups["word"].Index;
                        //for CREATE OR ALTER, it should keep processing "OR ALTER" construct
                        continue;
                    }

                    //OR ALTER should only appear after CREATE
                    if ( isOrAlterSupported && headerInfo.indexCreate != -1 )
                    {
                        if ( 0 == String.Compare("or", m.Groups["word"].Value, StringComparison.OrdinalIgnoreCase) )
                        {
                            headerInfo.indexOrAlterStart = m.Groups["word"].Index;
                            continue;
                        }
                        if ( 0 == String.Compare("alter", m.Groups["word"].Value, StringComparison.OrdinalIgnoreCase) )
                        {
                            //ALTER should only appear after OR
                            if (headerInfo.indexOrAlterStart != -1)
                            {
                                //for handling CREATE OR ALTER script
                                headerInfo.indexOrAlterEnd = m.Groups["word"].Index;
                                headerInfo.scriptContainsOrAlter = true;
                                continue;
                            }
                            //Otherwise, incorrect syntax
                            else
                            {
                                return false;
                            }
                        }
                        //for handling CREATE, CREATE OR ALTER script when OrAlter is supported
                        if ( headerInfo.indexOrAlterStart == -1 && headerInfo.indexOrAlterEnd == -1 || headerInfo.scriptContainsOrAlter )
                        {
                            break;
                        }
                        return false;
                    }
                    
                    if( 0 == String.Compare("alter", m.Groups["word"].Value, StringComparison.OrdinalIgnoreCase) )
                    {
                        headerInfo.scriptForCreate = false;
                        headerInfo.indexCreate = m.Groups["word"].Index;
                        continue;
                    }
                    return false;
                }
            }

            // search for object type: PROC, TRIGGER etc.
            for(; m.Success; m = m.NextMatch())
            {
                if( m.Groups["word"].Success )
                {
                    string matchValue1 = m.Groups["word"].Value;
                    if (matchValue1.Equals("materialized", StringComparison.OrdinalIgnoreCase)) 
                    {
                        // With "CREATE MATERIALIZED VIEW" introduced, the word "VIEW" (later, assigned as objectType) 
                        // can be the third match of group "word" instead of the second. 
                        // Therefore, check if the current macth is "materialized".if yes, the next match should be view. 
                        // Update regex match group to get the third match as objectType.
                        m = m.NextMatch();
                        string matchValue2 = m.Groups["word"].Value;
                        if (!matchValue2.Equals("view", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        headerInfo.objectType = matchValue2;
                    }
                    else
                    {
                        headerInfo.objectType = matchValue1;
                    }

                    break;
                }
            }

            bool retVal = ReadFullNameFromDdl(ref m, useQuotedIdentifier, ref headerInfo);

            //
            // if the current object is not a trigger or in case of failure then the work is done
            //
            
            if( !retVal || 0 != String.Compare("TRIGGER", headerInfo.objectType, StringComparison.OrdinalIgnoreCase) )
            {
                TraceHelper.Assert(headerInfo.objectType.Length > 0 && headerInfo.name.Length > 0 &&
                    headerInfo.indexCreate >= 0 && headerInfo.indexNameStart > 0 && 
                    headerInfo.indexNameEnd > headerInfo.indexNameStart && headerInfo.indexNameStartSecondary == -1);

                // this is not a failure exit! 				
                return retVal;
            }

            //
            // for trigers, continue to scan for the table name ... ON <table_name>
            //

            //look for ON
            for(; m.Success; m = m.NextMatch())
            {
                if( m.Groups["word"].Success )
                {
                    if( 0 != String.Compare("ON", m.Groups["word"].Value, StringComparison.OrdinalIgnoreCase) )
                    {
                        // invalid syntax, we should have found 'ON'
                        return false;
                    }
                    break;
                }
            }

            // now read the table name
            DdlTextParserHeaderInfo headerInfoTmp = new DdlTextParserHeaderInfo();
            headerInfoTmp.indexNameStart = -1;
            headerInfoTmp.indexNameEnd = -1;
            headerInfoTmp.schema = "";
            headerInfoTmp.name= "";
            
            retVal = ReadFullNameFromDdl(ref m, useQuotedIdentifier, ref headerInfoTmp);

            // copy the table name/schema in the secondary fields
            headerInfo.indexNameStartSecondary = headerInfoTmp.indexNameStart;
            headerInfo.indexNameEndSecondary = headerInfoTmp.indexNameEnd;
            headerInfo.schemaSecondary = headerInfoTmp.schema;
            headerInfo.nameSecondary= headerInfoTmp.name;
            headerInfo.databaseSecondary = headerInfoTmp.database;

            TraceHelper.Assert(headerInfo.objectType.Length > 0 && headerInfo.name.Length > 0 &&
                headerInfo.indexCreate >= 0 && headerInfo.indexNameStart > 0 && 
                headerInfo.indexNameEnd > headerInfo.indexNameStart &&
                headerInfo.nameSecondary.Length > 0 && headerInfo.indexNameStartSecondary > 0 && 
                headerInfo.indexNameEndSecondary > headerInfo.indexNameStartSecondary);

            return retVal;
        }


        ///<summary>
        ///returns the index where the header ends and the body starts
        ///</summary>
        public static int ParseDdlHeader(string objectText)
        {
            int parant_level = 0;
            ddlTextParserSingleton.hasParanthesis = false;
            bool bIgnoreDelim1 = false;
            ddlTextParserSingleton.returnTableVariableName = null;
            bool bWaitingForReturnTableVariableName = false;
            bool bWaitingForReturn = false;
            for(Match m = ddlTextParserSingleton.regex.Match(objectText); m.Success; m = m.NextMatch())
            {
                if( parant_level > 0 )
                {
                    if( m.Groups["parant_close"].Success )
                    {
                        parant_level--;
                    }
                    else if( m.Groups["parant_open"].Success )
                    {
                        parant_level++;
                    }
                    if( parant_level > 0 )
                    {
                        continue;
                    }
                }
                if( m.Groups["delim1"].Success )
                {
                    if( !bIgnoreDelim1 )
                    {
                        return m.Index + m.Length;
                    }
                    bIgnoreDelim1 = false;
                }
                else if( m.Groups["delim2"].Success )
                {
                    return m.Index;
                }
                else if( m.Groups["exec"].Success )
                {
                    //create proc p with execute /*kjahdfkja*/ as caller as select 1
                    //must ignore an 'as'
                    bIgnoreDelim1 = true;
                }
                else if( m.Groups["parant_open"].Success )
                {
                    parant_level++;
                    ddlTextParserSingleton.hasParanthesis = true;
                }
                else if( m.Groups["returns"].Success )
                {
                    bWaitingForReturnTableVariableName = true;
                }
                else if( bWaitingForReturnTableVariableName && m.Groups["param"].Success )
                {
                    ddlTextParserSingleton.returnTableVariableName = m.Groups["param"].Value;
                    bWaitingForReturnTableVariableName = false;
                }
                else if( m.Groups["returns_table"].Success )
                {
                    bWaitingForReturn = true;
                }
                else if( bWaitingForReturn && m.Groups["return"].Success )
                {
                    return m.Index;
                }
            }
            return 0;
        }

        // sNestedMultiLineCommentOptimized_End is same regular expression as sNestedMultiLineCommentOptimized.
        // The name is different probably to increase readability of code.-anchals
        private readonly static string sNestedMultiLineCommentOptimized_end = sNestedMultiLineCommentOptimized;
        private readonly static string sSpace_end = @"([ \t]+)";
        private readonly static string sWord_end = @"(?<word>[a-zA-Z0-9]+)";
        private readonly static string sEol_end = @"(?<eol>\n)";
        private readonly static string sSingleLineComment_end = @"(?<slcomm>--)";
        private readonly static string sReject_end = @"(?<reject>.)";

        ///<summary>
        ///return the index where the CHECK_OPTION is specified in the view text
        ///-1 if not found</summary>
        public static int ParseCheckOption(String ddlText)
        {
            string [] words = new string[] { "option", "check", "with" };
            int state = 0;
            int old_state = 0;
            int idx = -1;
            bool bShouldReject = false;

            for(Match m = ddlTextParserSingleton.m_r_end.Match(ddlText); m.Success; m = m.NextMatch())
            {
                if( !bShouldReject && 3 < state && m.Groups["reject"].Success )
                {
                    bShouldReject = true;
                }
                if( !bShouldReject && 3 > state && m.Groups["word"].Success )
                {
                    if( 0 != String.Compare(words[state++], m.Value, StringComparison.OrdinalIgnoreCase ) )
                    {
                        bShouldReject = true;
                    }
                    if( 3 == state )
                    {
                        idx = m.Index;
                    }
                }
                if( m.Groups["slcomm"].Success )
                {
                    state = old_state;
                    bShouldReject = false;
                }
                if( m.Groups["eol"].Success )
                {
                    if( true == bShouldReject )
                    {
                        return -1;
                    }
                    if( state == 3 )
                    {
                        return idx;
                    }
                    old_state = state;
                }
            }
            if( true == bShouldReject )
            {
                return -1;
            }
            if( 3 == state )
            {
                return idx;
            }
            return -1;
        }

    }
}

// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    internal sealed class XPathScanner {

        internal enum XPathTokenType {
            Comma                 = ',',
            Slash                 = '/',
            Caret                 = '^',
            At                    = '@',
            Dot                   = '.',
            LParens               = '(',
            RParens               = ')',
            LBracket              = '[',
            RBracket              = ']',
            Colon                 = ':',
            Semicolon             = ';',
            Star                  = '*',
            Plus                  = '+',
            Minus                 = '-',
            Eq                    = '=',
            Lt                    = '<',
            Gt                    = '>',
            Bang                  = '!',
            Dollar                = '$',
            Apos                  = '\'',
            Quote                 = '"',
            Union                 = '|',
            Eof                   = '\0',
            InvalidToken          = -1,
            DotDot                = -2,   // ..
            SlashSlash            = -3,   // //
            Name                  = -4,   // Xml _Name
            String                = -5,   // Quoted string constant
            Number                = -6,   // _Number constant

            Ne                    = -7,   // !=
            Le                    = -8,   // <=
            Ge                    = -9,   // >=
            And                   = -10,  // &&
            Or                    = -11,  // ||
            Element               = -30,  // element
            Attribute             = -31,  // attribute
            Textnode              = -32,  // textNode
            CData                 = -33,  // cdata
            Comment               = -34,  // comment
            Text                  = -35,  // textnode | cdata
            Pi                    = -36,  // pi
            Node                  = -38,  // node
            NodeValue             = -40,  // value
            NodeType              = -42,  // nodeType
            NodeName              = -43,  // nodeName
            End                   = -44,  // end
            Query                 = -45,
            Boolean               = -54,
            Negate                = -57,
            AxesAncestor          = -66,  // 'ancestor'
            AxesAncestorSelf      = -67,  // 'ancestor-or-self'
            AxesAttribute         = -68,  // 'attribute'
            AxesChild             = -69,  // 'child'
            AxesDescendant        = -70,  // 'descendant'
            AxesDescendantSelf    = -71,  // 'descendant-or-self'
            AxesFollowing         = -72,  // 'following'
            AxesFollowingSibling  = -73,  // 'following-sibling'
            AxesNameSpace         = -74,  // 'namespace'
            AxesParent            = -75,  // 'parent'
            AxesPreceding         = -76,  // 'preceding'
            AxesPrecedingSibling  = -77,  // 'preceding-sibling'
            AxesSelf              = -78,  // 'self'
            ColonColon            = -79,  // '::'
            Function              = -80,
            Div                   = -81,  // 'Div'
            Mod                   = -82,  // 'Mod'
            ProcessingInstruction = -83,  // 'processing-instruction' 
            FuncLast              = -84,  //"last"
            FuncPosition          = -85,  // "position"
            FuncCount             = -86,  //"count"
            FuncLocalName         = -87,  //"local-_Name"
            FuncNameSpaceUri      = -88,  //"namespace-uri"
            FuncName              = -89,  // "_Name"
            FuncString            = -90,  // "string"
            FuncBoolean           = -91,  // "boolean"
            FuncNumber            = -92,  // "_Number"
            FuncTrue              = -93,  // "true"
            FuncFalse             = -94,  // "false"
            FuncNot               = -95,  // "not"
            FuncID                = -96,
            FuncConcat            = -97,
            FuncStartsWith        = -98,
            FuncContains          = -99,
            FuncSubstringBefore   = -100,
            FuncSubstringAfter    = -101,
            FuncSubstring         = -102,
            FuncStringLength      = -103,
            FuncNormalizeSpace    = -104,
            FuncTranslate         = -105,
            FuncLang              = -106,
            FuncSum               = -107,
            FuncFloor             = -108,
            FuncCeiling           = -109,
            FuncRound             = -110,
            FuncLike              = -111,
            FuncIn                = -112,

            Unknown               = -1000,
        };

        private char _Lookahead;
        private String _PchNext;
        private int _PchNindex;
        private int _PchLength;
        private String _Tstring = null;
        private int _PchTokenIndex;
        private int _Ul=0;
        private XPathTokenType _Token;
        private String _Name ;
        private double _Number;
        private String _Prefix = String.Empty;
        private String _Urn = String.Empty;
        private static Hashtable _AxesTable ;
        private static Hashtable _FunctionTable ;
        private bool _Axes = false;


        internal static Hashtable AxesTable{
            get {
                if (_AxesTable == null ){
                    _AxesTable = new Hashtable(13);
                    _AxesTable.Add("child", XPathTokenType.AxesChild);
                    _AxesTable.Add("descendant", XPathTokenType.AxesDescendant);
                    _AxesTable.Add("parent", XPathTokenType.AxesParent);
                    _AxesTable.Add("ancestor", XPathTokenType.AxesAncestor);
                    _AxesTable.Add("following-sibling", XPathTokenType.AxesFollowingSibling);
                    _AxesTable.Add("preceding-sibling", XPathTokenType.AxesPrecedingSibling);
                    _AxesTable.Add("following", XPathTokenType.AxesFollowing);
                    _AxesTable.Add("preceding", XPathTokenType.AxesPreceding);
                    _AxesTable.Add("attribute", XPathTokenType.AxesAttribute);
                    _AxesTable.Add("namespace", XPathTokenType.AxesNameSpace);
                    _AxesTable.Add("self", XPathTokenType.AxesSelf);
                    _AxesTable.Add("descendant-or-self", XPathTokenType.AxesDescendantSelf);
                    _AxesTable.Add("ancestor-or-self", XPathTokenType.AxesAncestorSelf);
                }
                return _AxesTable;
            }
        }
                

        internal static Hashtable FunctionTable{
            get {
                if (_FunctionTable == null ){
                    _FunctionTable = new Hashtable(36);
                    _FunctionTable.Add("last", XPathTokenType.FuncLast);
                    _FunctionTable.Add("position", XPathTokenType.FuncPosition);
                    _FunctionTable.Add("name", XPathTokenType.FuncName);
                    _FunctionTable.Add("namespace-uri", XPathTokenType.FuncNameSpaceUri );
                    _FunctionTable.Add("local-name", XPathTokenType.FuncLocalName);
                    _FunctionTable.Add("count", XPathTokenType.FuncCount);
                    _FunctionTable.Add("id", XPathTokenType.FuncID);
                    _FunctionTable.Add("string", XPathTokenType.FuncString);
                    _FunctionTable.Add("concat", XPathTokenType.FuncConcat);
                    _FunctionTable.Add("starts-with", XPathTokenType.FuncStartsWith);
                    _FunctionTable.Add("contains", XPathTokenType.FuncContains);
                    _FunctionTable.Add("substring-before", XPathTokenType.FuncSubstringBefore);
                    _FunctionTable.Add("substring-after", XPathTokenType.FuncSubstringAfter);
                    _FunctionTable.Add("substring", XPathTokenType.FuncSubstring);
                    _FunctionTable.Add("string-length", XPathTokenType.FuncStringLength);
                    _FunctionTable.Add("normalize-space", XPathTokenType.FuncNormalizeSpace);
                    _FunctionTable.Add("translate", XPathTokenType.FuncTranslate);
                    _FunctionTable.Add("boolean", XPathTokenType.FuncBoolean);
                    _FunctionTable.Add("not", XPathTokenType.FuncNot);
                    _FunctionTable.Add("true", XPathTokenType.FuncTrue);
                    _FunctionTable.Add("false", XPathTokenType.FuncFalse);
                    _FunctionTable.Add("lang", XPathTokenType.FuncLang);
                    _FunctionTable.Add("number", XPathTokenType.FuncNumber);
                    _FunctionTable.Add("sum", XPathTokenType.FuncSum);
                    _FunctionTable.Add("floor", XPathTokenType.FuncFloor);
                    _FunctionTable.Add("ceiling", XPathTokenType.FuncCeiling);
                    _FunctionTable.Add("round", XPathTokenType.FuncRound);
                    _FunctionTable.Add("like", XPathTokenType.FuncLike);
                    _FunctionTable.Add("in", XPathTokenType.FuncIn);
                }
                return _FunctionTable;
            }
        }
                    
        internal char Lookahead {
            get {return _Lookahead;}
        }

        internal bool Axes {
            get {return _Axes;}
        }
        
        internal XPathTokenType Token {
            get {return _Token;}
        }

        internal String Name {
            get {return _Name;}
            set {_Name = value;}
        }

        internal double Number {
            get {return _Number;}
        }

        internal String Prefix {
            get {return _Prefix;}
            set {_Prefix = value;}
        }

        internal String Urn {
            get {return _Urn;}
            set {_Urn = value;}
        }

        internal String PchToken {
            get {            
                try {
                    return _PchNext.Substring(_PchTokenIndex);
                }
                catch (System.ArgumentException) {
                    return("\0");
                }
            }
        }

        internal String Tstring {
            get {return _Tstring;}
        }


        internal XPathScanner() {
        }

        internal void SetParseString(String parsestring) {
            this.Reset();
            
            _PchNext = parsestring.Copy();
            _PchLength = _PchNext.Length;
        }

        internal void Reset() {
            _Lookahead = ' ';
            _PchNext = String.Empty;
            _PchNindex = 0;
            _Tstring = String.Empty;
            _PchTokenIndex = 0;
            _Ul = 0;
            _Token = XPathTokenType.InvalidToken;
            _Name = String.Empty;
            _Number = 0;
            _Prefix = String.Empty;
            _Urn = String.Empty;
            _PchLength = 0;
        }


        internal void SkipSpace() {
            while (XmlCharType.IsWhiteSpace(_Lookahead) &&
                   (int)_Lookahead != (int)XPathTokenType.Eof)
            {
                Advance();
            }
        }

        internal void Advance() {
            if (((int)_Lookahead) != (int)XPathTokenType.Eof) {
                if (_PchNindex < _PchLength) {
                    _Lookahead = _PchNext[_PchNindex];
                    _PchNindex++;
                }
                else
                {
                    _Lookahead = '\0';
                }
            }

        }
        internal char TestAdvance() 
        {
            if (((int)_Lookahead) != (int)XPathTokenType.Eof) 
            {
                if (_PchNindex < _PchLength) 
                {
                    return _PchNext[_PchNindex];
                }
            }
            return '\0'; 
        }

        private void ScanString() {

            char endChar = _Lookahead;
            StringBuilder tbuffer = new StringBuilder();
            
            Advance();
            for(;;)
            {
                if(((int)_Lookahead == (int)XPathTokenType.Eof) )
                {
                    break;
                }
                //Found an instance of the quote character
                if ( _Lookahead == endChar )
                {
                    //Is the quote character escaped? (two of it in a row)
                    if (endChar != TestAdvance())
                    {
                        //Not escaped, that's the end of the string
                        break;
                    }
                    //Is escaped so add the character to the buffer
                    //(The second one will be added as well immediately below)
                    tbuffer.Append(_Lookahead);
                    Advance();
                }
                //Add character to the buffer and move the scanner forward
                tbuffer.Append(_Lookahead);
                Advance();
            }
            //If the character we're looking at is the end of the input we never
            //closed the string which is an error
            if (_Lookahead == '\0')
            {
                throw new XPathException(XPathExceptionCode.UnclosedString,PchToken);
            }

            Advance();

            //Set the output values (token type and value)
            _Token = XPathTokenType.String;
            _Tstring = tbuffer.ToString().Copy();
        }

        private int ScanName(ref int len) {
            int startindex = _PchNindex ;
            bool fFunction = false;
            if (!XmlCharType.IsStartNameChar(_Lookahead)) {
                _Token = XPathTokenType.Unknown;
                return(int)_Token;
            }

            while (true) {
                switch (_Lookahead) {
                    case '\0' : 
                        _PchNindex++;
                        goto cleanup;
                    case ':' :
                        if (':' == NextChar())
                        {
                            _Axes = true;
                        }

                        goto cleanup;
                    case '(' :
                        fFunction = true;
                        goto cleanup;
                    case  ')' : goto cleanup;
                    case  '|' :goto cleanup;
                    case '[' :goto cleanup;
                    case ']' :goto cleanup;
                    case ' ' :goto cleanup;
                    case '\n' :goto cleanup;
                    case '\r' :goto cleanup;
                    case '\t' :goto cleanup;
                    case '>' :goto cleanup;
                    case '<' :goto cleanup;
                    case '/' :goto cleanup;
                    case '=' :goto cleanup;
                    case '!' :goto cleanup;
                    case '+' :goto cleanup;
                    case '*' :goto cleanup;
                    case ';' :goto cleanup;
                    case ',' :
                        goto cleanup;
                    default :
                        if (!XmlCharType.IsNameChar(_Lookahead)) {
                            _Token = XPathTokenType.Unknown;
                            return(int) _Token;
                        }
                        else
                        {
                            Advance();
                        }

                        break;
                }
            }
            cleanup:
            Debug.Assert(_PchNindex > startindex, "How did this happen!");
            len = _PchNindex - startindex ;
            _Name = _PchNext.Substring(startindex -1,len);
            if (_Axes) {
                if (AxesTable.Contains(_Name))
                {
                    _Token = (XPathTokenType) AxesTable[_Name];
                }
                else
                {
                    _Token = XPathTokenType.Unknown;
                }

                _Name = String.Empty;

            }
            else if (fFunction)
            {
                _Token =  XPathTokenType.Function;
            }
            else {
                _Token = XPathTokenType.Name;

                if (_Lookahead == ':') {
                    _Prefix = _Name;
                    Advance();
                    if (ScanName(ref  _Ul) == -1000)
                    {
                        //This is needed for the case prefix:*
                        if (_Lookahead == '*') {
                            _Token = XPathTokenType.Name;
                            _Name = String.Empty;
                            Advance();
                        }
                        else
                        {
                            throw new XPathException(XPathExceptionCode.InvalidName,PchToken);
                        }
                    }
                }
            }
            return(int)_Token;
        }

        private void ScanNumber() {
            StringBuilder temp = new StringBuilder();
            bool haveDecimal = false;
            if (_Lookahead!= '.' && (( _Lookahead > '9') || (_Lookahead < '0'))) {
                _Token = XPathTokenType.Unknown;
                return;
            }
            if (_Lookahead == '.')
            {
                haveDecimal = true;
            }

            while (true) {
                temp.Append(_Lookahead);
                Advance();
                if ((_Lookahead > '9') || (_Lookahead < '0')) {
                    if (_Lookahead == '.') {
                        if (!haveDecimal) {
                            // This is the first decimal we have seen.
                            haveDecimal = true;
                        }
                        else {
                            // A decimal already appeared before.
                            _Token = XPathTokenType.Unknown;
                            return; 
                        }
                    }
                    else {
                        // something other than a digit or decimal was encountered
                        if (temp.Length == 1 && temp[0] == '.') {
                            _Token = XPathTokenType.Unknown;
                            return;
                        }
                        break;
                    }
                }
            }
            String snum = temp.ToString();
            _Number = Convert.ToDouble(snum, CultureInfo.InvariantCulture.NumberFormat);
            _Token = XPathTokenType.Number;
        }

        private char  NextChar() 
        {
            if (_PchNindex < _PchNext.Length )
            {
                return _PchNext[_PchNindex]; 
            }
            return '\0';
        }

        internal XPathTokenType NextToken() {

            SkipSpace();
            _Axes= false;
            _PchTokenIndex = _PchNindex;
            switch (_Lookahead) {
                case '\0'  : 
                    _Token = XPathTokenType.Eof;
                    break;
                case ',': goto case '#';
                case '@': goto case '#';
                case '(': goto case '#';
                case ')': goto case '#';
                case '[':goto case '#';
                case ']': goto case '#';
                case ';': goto case '#';
                case '*': goto case '#';
                case '+': goto case '#';
                case '-': goto case '#';
                case '=': goto case '#';
                case '#': 
                    _Token =  (XPathTokenType) Convert.ToInt32(_Lookahead);
                    Advance();
                    break;

                case ':': 
                    _Token = XPathTokenType.Colon;
                    Advance();
                    if (':' == _Lookahead) {
                        _Token = XPathTokenType.ColonColon;
                        Advance();
                    }
                    break;

                case '|': 
                    Advance();
                    _Token = XPathTokenType.Union;
                    break;

                case '<': 
                    _Token =  (XPathTokenType) Convert.ToInt32(_Lookahead, CultureInfo.InvariantCulture.NumberFormat);
                    Advance();
                    if (_Lookahead == '=') {
                        Advance();
                        _Token = XPathTokenType.Le;
                    }
                    break;

                case '>': 
                    _Token =  (XPathTokenType) Convert.ToInt32(_Lookahead);
                    Advance();
                    if (_Lookahead == '=') {
                        Advance();
                        _Token = XPathTokenType.Ge;
                    }
                    break;

                case '/':
                    _Token = XPathTokenType.Slash;
                    Advance();
                    if (_Lookahead == '/') {
                        Advance();
                        _Token = XPathTokenType.SlashSlash;
                    }
                    break;

                case '!': 
                    Advance();
                    if (_Lookahead == '=') {
                        Advance();
                        _Token = XPathTokenType.Ne;
                    }
                    else
                    {
                        _Token = XPathTokenType.Bang;
                    }

                    break;

                case '.': 
                    ScanNumber();
                    if (_Token == XPathTokenType.Unknown)
                    {
                        if (_Lookahead == '.') {
                            Advance();
                            _Token = XPathTokenType.DotDot;
                        }
                        else
                        {
                            _Token = XPathTokenType.Dot;
                        }
                    }

                    break;

                case '$':
                    Advance();
                    _Token = XPathTokenType.Dollar;

                    break;

                //BUG: TFS#8062030, while the scanner here allows double-quoted
                //strings the rest of Sfc assumes a "string" is single-quoted. 
                //See bug for more details but be aware that this may cause weird
                //side effects if double quotes are used.

                //I'm not disabling this functionality because it works fine in 
                //some situations and the risk of breaking some existing functionality
                //is too high.
                case '"': goto case '\'';
                case '\'': 
                    ScanString();
                    break;

                default:
                    int start = _PchNindex;
                    if (ScanName(ref  _Ul) == -1000) {
                        if (_PchNindex == start)
                        {
                            ScanNumber();
                        }
                    }
            break;
            }
            return _Token;
        }
    }
}

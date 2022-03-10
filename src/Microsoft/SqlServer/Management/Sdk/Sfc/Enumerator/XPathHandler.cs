// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Diagnostics;

    internal class XPathHandler {
        XPathScanner _Scanner;
        //static  readonly ParamInfo[] s_MethodTable =new ParamInfo[27];
        static Hashtable _MethodTable ;
        static readonly AstNode.RType[] temparray1 = {AstNode.RType.Error};
        static readonly AstNode.RType[] temparray2 = {AstNode.RType.NodeSet};                     
        static readonly AstNode.RType[] temparray3 = {AstNode.RType.Any};        
        static readonly AstNode.RType[] temparray4 = {AstNode.RType.String};                  
        static readonly AstNode.RType[] temparray5 = {AstNode.RType.String, AstNode.RType.String};
        static readonly AstNode.RType[] temparray6 = {AstNode.RType.String, AstNode.RType.Number, AstNode.RType.Number};
        static readonly AstNode.RType[] temparray7 = {AstNode.RType.String, AstNode.RType.String, AstNode.RType.String};
        static readonly AstNode.RType[] temparray8 = {AstNode.RType.Boolean};
        static readonly AstNode.RType[] temparray9 = {AstNode.RType.Number};        


        const String _OrString = "or";
        const String _AndString = "and";
        
        internal XPathHandler() {
            //Initarray();
        }

        void NameReset() {
            _Scanner.Urn = String.Empty;
            _Scanner.Prefix = String.Empty; 
            _Scanner.Name = String.Empty;
        }
       
        internal AstNode Run(XPathScanner scanner ) {
            _Scanner = scanner;
            _Scanner.Advance();
            if (_Scanner.NextToken() == XPathScanner.XPathTokenType.Eof)
            {
                throw new XPathException(XPathExceptionCode.TokenExpected);
            }

            AstNode result = ParseXPointer(null);
            if (_Scanner.Token != XPathScanner.XPathTokenType.Eof)
            {
                throw new XPathException(XPathExceptionCode.InvalidToken, _Scanner.PchToken);
            }

            Debug.Assert(result != null , "Some error went unchecked in XPathHandler");
            return result;
        }

        private AstNode ParseLocationPath(AstNode qyInput) {
            AstNode opnd = null;
            AstNode opndNext = null;

            if (_Scanner.Token == XPathScanner.XPathTokenType.Slash) {
                qyInput= new Root();

                _Scanner.NextToken();
                if (_Scanner.Token == XPathScanner.XPathTokenType.Eof)
                {
                    return qyInput;
                }
                else {
                    opndNext = ParseRelativeLocationPath(qyInput);
                    if (opndNext != null)
                    {
                        return opndNext;
                    }

                    return qyInput;
                }
            }
            else if (_Scanner.Token == XPathScanner.XPathTokenType.SlashSlash) {
                qyInput= new Root();
                opnd =  new Axis(Axis.AxisType.DescendantOrSelf,qyInput);
                _Scanner.NextToken();
                // '//' can not be the last thing in the query
                if (XPathScanner.XPathTokenType.Eof == _Scanner.Token || XPathScanner.XPathTokenType.Union == _Scanner.Token)
                {
                    throw new XPathException(XPathExceptionCode.TokenExpected,_Scanner.PchToken);
                }

                return ParseRelativeLocationPath(opnd);
            }
            return  ParseRelativeLocationPath(qyInput); 
        } // ParseLocationPath

        private AstNode ParseRelativeLocationPath(AstNode  qyInput) {
            AstNode  opnd = ParseStep(qyInput, false);
            if (opnd == null)
            {
                return null;
            }

            if (XPathScanner.XPathTokenType.SlashSlash == _Scanner.Token) {
                _Scanner.NextToken();
                opnd = new Axis(Axis.AxisType.DescendantOrSelf,opnd);
                opnd = ParseRelativeLocationPath(opnd);
            }
            if (XPathScanner.XPathTokenType.Slash == _Scanner.Token) {
                _Scanner.NextToken();
                opnd = ParseRelativeLocationPath(opnd);
                if ( opnd == null )
                {
                    throw new XPathException(XPathExceptionCode.QueryExpected, _Scanner.PchToken);
                }
            }
            return opnd;
        }

        private AstNode ParseStep(AstNode  qyInput, bool forQyCond) {
            AstNode  opnd = null;
            if (forQyCond) {
                // this is for any other query's query condition processing then the token should be a name or Star
                if (XPathScanner.XPathTokenType.Star != _Scanner.Token && XPathScanner.XPathTokenType.Name != _Scanner.Token)
                {
                    throw new XPathException(XPathExceptionCode.TokenExpected,_Scanner.PchToken);
                }
            }
            if (XPathScanner.XPathTokenType.Dot == _Scanner.Token) {
                opnd = new Axis(Axis.AxisType.Self , qyInput);
                _Scanner.NextToken();
                return opnd;
            }
            else if (XPathScanner.XPathTokenType.DotDot == _Scanner.Token) {
                opnd= new Axis(Axis.AxisType.Parent,qyInput);
                _Scanner.NextToken();
                return opnd;
            }
            opnd = ParseBasis(qyInput);

            if (opnd == null)
            {
                return opnd;
            }

            while (XPathScanner.XPathTokenType.LBracket == _Scanner.Token) {
                AstNode  opndArg = ParsePredicate(opnd);
                if (opnd.ReturnType != AstNode.RType.NodeSet)
                {
                    throw new XPathException(XPathExceptionCode.NodeSetExpected,_Scanner.PchToken);
                }

                opnd = new Filter(opnd, opndArg);
            } 
            return opnd;
        }

        private AstNode ParseBasis(AstNode  qyInput) {
            AstNode  opnd = null;
            XPathNodeType type = XPathNodeType.Element;

            switch (_Scanner.Token) {
                case XPathScanner.XPathTokenType.Function:
                    IsValidType(ref type); // this will throw if not valid
                    opnd = new Axis(Axis.AxisType.Child, qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type );
                    NameReset();
                    _Scanner.NextToken();
                    break;
                case XPathScanner.XPathTokenType.Name:
                    opnd = new Axis(Axis.AxisType.Child , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    NameReset();
                    _Scanner.NextToken();
                    break;

                case XPathScanner.XPathTokenType.At:
                    NameReset();
                    _Scanner.NextToken();

                    if (_Scanner.Token == XPathScanner.XPathTokenType.Name ||
                        _Scanner.Token == XPathScanner.XPathTokenType.Star ||
                        _Scanner.Token == XPathScanner.XPathTokenType.Function) {
                        if ('(' == _Scanner.Lookahead)
                        {
                            IsValidType(ref type); // this will throw if not valid type
                        }
                        else
                        {
                            type = XPathNodeType.Attribute;
                        }

                        opnd = new Axis(Axis.AxisType.Attribute , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type );
                    }
                    else
                    {
                        throw new XPathException(XPathExceptionCode.NodeTestExpected, _Scanner.PchToken);
                    }

                    NameReset();
                    _Scanner.NextToken();
                    break;

                case XPathScanner.XPathTokenType.Star:
                    opnd = new Axis(Axis.AxisType.Child, qyInput, String.Empty, 
                                    String.Empty, String.Empty, XPathNodeType.Element);
                    _Scanner.NextToken();
                    break;

                default:
                    opnd = ConstructAxesQuery(qyInput);
                    break;
            } 
            return opnd;
        }

        private AstNode ConstructAxesQuery(AstNode  qyInput) {

            // We do not support XPath axes anywhere, and this function is logically unreachable
            throw new XPathException(XPathExceptionCode.InvalidToken, _Scanner.PchToken);

#if false // CC_NOT_USED
            AstNode                     opnd      = null;
            XPathScanner.XPathTokenType axesToken = _Scanner.Token;
            XPathNodeType             type      = XPathNodeType.Element;

            if (!_Scanner.Axes)
                    return null;
            if (axesToken == XPathScanner.XPathTokenType.AxesAttribute)
                type = XPathNodeType.Attribute;
            _Scanner.NextToken();        
            CheckToken(XPathScanner.XPathTokenType.ColonColon);
            _Scanner.NextToken();    

            if (XPathScanner.XPathTokenType.Name != _Scanner.Token &&
                XPathScanner.XPathTokenType.Star != _Scanner.Token &&
                XPathScanner.XPathTokenType.Function != _Scanner.Token) {
                throw new XPathException(
                                        XPathExceptionCode.NodeTestExpected,
                                        _Scanner.PchToken);
            }
            if (XPathScanner.XPathTokenType.Function == _Scanner.Token &&
                '(' == _Scanner.Lookahead)
                IsValidType(ref type); // this will throw if not valid type
            else {
                if (XPathScanner.XPathTokenType.Star == _Scanner.Token)
                    NameReset();
            }

            switch (axesToken) {
                case XPathScanner.XPathTokenType.AxesChild:
                    opnd = new Axis(Axis.AxisType.Child, qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesAttribute:
                    opnd = new Axis(Axis.AxisType.Attribute, qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);        
                    break;
                case XPathScanner.XPathTokenType.AxesAncestor:
                    opnd = new Axis(Axis.AxisType.Ancestor , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesAncestorSelf:
                    opnd = new Axis(Axis.AxisType.AncestorOrSelf , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesDescendant:
                    opnd = new Axis(Axis.AxisType.Descendant , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesDescendantSelf:
                    opnd = new Axis(Axis.AxisType.DescendantOrSelf, qyInput, _Scanner.Urn,_Scanner.Prefix,_Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesSelf:
                    opnd = new Axis(Axis.AxisType.Self , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesParent:
                    opnd = new Axis(Axis.AxisType.Parent , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesPreceding:
                    opnd = new Axis(Axis.AxisType.Preceding , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesFollowing:
                    opnd = new Axis(Axis.AxisType.Following , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesPrecedingSibling:
                    opnd = new Axis(Axis.AxisType.PrecedingSibling , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesFollowingSibling:
                    opnd = new Axis(Axis.AxisType.FollowingSibling , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                case XPathScanner.XPathTokenType.AxesNameSpace:
                    opnd = new Axis(Axis.AxisType.Namespace , qyInput, _Scanner.Urn, _Scanner.Prefix, _Scanner.Name, type);
                    break;
                default:
                    // BUGBUG: should throw and error here if the token is not a known axes
                    return null;
            }

            NameReset();
            _Scanner.NextToken();
            return opnd;
#endif
        }

        private AstNode ParsePredicate(AstNode  qyInput) {
            AstNode  opnd = null;

            CheckToken(XPathScanner.XPathTokenType.LBracket);
            _Scanner.NextToken();
            opnd = ParseXPointer(qyInput);
            CheckToken(XPathScanner.XPathTokenType.RBracket);

            _Scanner.NextToken();
            return opnd;
        }

        private AstNode ParseXPointer(AstNode  qyInput) {
            AstNode  opnd1 = ParseAndExpr(qyInput);
            AstNode  opnd2;

            if (opnd1 == null)
            {
                throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
            }

            // check to see if we need to process an or
            if (_Scanner.Token == XPathScanner.XPathTokenType.Name && _Scanner.Prefix.Length <= 0 && _Scanner.Name.Equals(_OrString)) {
                _Scanner.NextToken();
                opnd2 = ParseXPointer(qyInput);
                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                }

                // this should either return a true or a false
                opnd1 = new Operator(Operator.Op.OR, opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParseAndExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParseEqualityExpr(qyInput);
            AstNode  opnd2;

            if (_Scanner.Token == XPathScanner.XPathTokenType.Name && _Scanner.Prefix.Length <= 0 && _Scanner.Name.Equals(_AndString)) {
                if (opnd1 == null)
                {
                    throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                }

                _Scanner.NextToken();
                opnd2 = ParseAndExpr(qyInput);

                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                }

                opnd1 = new Operator(Operator.Op.AND, opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParseEqualityExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParseRelationalExpr(qyInput);
            AstNode  opnd2;
            Operator.Op op = Operator.Op.INVALID;

            if (opnd1 == null)
            {
                return null;
            }

            if (_Scanner.Token == XPathScanner.XPathTokenType.Eq)
            {
                op = Operator.Op.EQ;
            }
            else  if (_Scanner.Token == XPathScanner.XPathTokenType.Ne)
            {
                op = Operator.Op.NE;
            }

            if (op != Operator.Op.INVALID) {
                _Scanner.NextToken();
                opnd2 = ParseEqualityExpr(qyInput);
                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                }

                opnd1 = new Operator(op, opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParseRelationalExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParseAdditiveExpr(qyInput);
            AstNode  opnd2;
            Operator.Op op = Operator.Op.INVALID;

            if (opnd1 == null)
            {
                return null;
            }

            switch (_Scanner.Token) {
                case XPathScanner.XPathTokenType.Lt : 
                    op = Operator.Op.LT;
                    break;
                case XPathScanner.XPathTokenType.Le : 
                    op = Operator.Op.LE;
                    break;
                case XPathScanner.XPathTokenType.Gt : 
                    op = Operator.Op.GT;
                    break;
                case XPathScanner.XPathTokenType.Ge : 
                    op = Operator.Op.GE;    
		    break;
            }        
            if (op != Operator.Op.INVALID) {
                _Scanner.NextToken();
                opnd2 = ParseRelationalExpr(qyInput);
                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                }

                opnd1 = new Operator(op, opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParseAdditiveExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParseMultiplicativeExpr(qyInput);
            AstNode  opnd2;
            Operator.Op op = Operator.Op.INVALID;
            if (opnd1 == null)
            {
                return null;
            }

            if (_Scanner.Token == XPathScanner.XPathTokenType.Plus)
            {
                op = Operator.Op.PLUS;
            }
            else if (_Scanner.Token == XPathScanner.XPathTokenType.Minus)
            {
                op = Operator.Op.MINUS;
            }

            if (op != Operator.Op.INVALID) {
                _Scanner.NextToken();
                opnd2 = ParseAdditiveExpr(qyInput);
                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.NumberExpected, _Scanner.PchToken);
                }

                opnd1 = new Operator(op, opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParseMultiplicativeExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParseUnaryExpr(qyInput);
            AstNode  opnd2;
            Operator.Op op = Operator.Op.INVALID;

            if (opnd1 == null)
            {
                return null;
            }

            if (_Scanner.Token == XPathScanner.XPathTokenType.Star )
            {
                op = Operator.Op.MUL;
            }
            else if (_Scanner.Token ==  XPathScanner.XPathTokenType.Name){
                    if (_Scanner.Name.Equals("div"))
                {
                    op = Operator.Op.DIV;
                }
                else if (_Scanner.Name.Equals("mod"))
                {
                    op = Operator.Op.MOD;
                }
            }
            if (Operator.Op.INVALID!= op) {
                _Scanner.NextToken();
                opnd2 = ParseMultiplicativeExpr(qyInput);

                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.NumberExpected, _Scanner.PchToken);
                }

                opnd1 = new Operator( op , opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParseUnaryExpr(AstNode  qyInput) {
            AstNode  opnd1 = null;

            if (XPathScanner.XPathTokenType.Minus == _Scanner.Token) {
                _Scanner.NextToken();
                opnd1 = ParseUnaryExpr(qyInput);
                if (opnd1 == null)
                {
                    throw new XPathException(XPathExceptionCode.NumberExpected, _Scanner.PchToken);
                }

                opnd1 = new Operator(Operator.Op.NEGATE, opnd1, null );

            }
            else
            {
                opnd1 = ParseUnionExpr(qyInput);
            }

            return opnd1;
        }

        private AstNode ParseUnionExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParsePathExpr(qyInput);
            AstNode  opnd2 = null;

            // check to see if we need to process a union
            if (XPathScanner.XPathTokenType.Union == _Scanner.Token) {
                // both operands for a union should be nodesets.
                if (opnd1 == null)
                {
                    throw new XPathException(XPathExceptionCode.TokenExpected, _Scanner.PchToken);
                }
                else if (opnd1.ReturnType != AstNode.RType.NodeSet)
                {
                    throw new XPathException(XPathExceptionCode.InvalidToken, _Scanner.PchToken);
                }

                _Scanner.NextToken();
                opnd2 = ParseUnionExpr(qyInput);

                // both operands for a union should be nodesets.
                if (opnd2 == null)
                {
                    throw new XPathException(XPathExceptionCode.TokenExpected, _Scanner.PchToken);
                }
                else if (opnd2.ReturnType != AstNode.RType.NodeSet)
                {
                    throw new XPathException(XPathExceptionCode.InvalidToken, _Scanner.PchToken);
                }

                opnd1 = new Operator(Operator.Op.UNION, opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParsePathExpr(AstNode qyInput) {
            AstNode  opnd1;
            AstNode  opnd2;

            if ((XPathScanner.XPathTokenType.Function == _Scanner.Token && !IsNodeType() )
                || XPathScanner.XPathTokenType.Dollar == _Scanner.Token
                || XPathScanner.XPathTokenType.LParens == _Scanner.Token
                || XPathScanner.XPathTokenType.Number == _Scanner.Token 
                || XPathScanner.XPathTokenType.String == _Scanner.Token) {
                opnd1 =ParseFilterExpr(qyInput);
                if (_Scanner.Token == XPathScanner.XPathTokenType.Slash) {                        // don't get the query unless the token is '/' or '//' because getQuery 
                        // raises an error if the opnd is not a query and it is okay for opnd1 to be
                        // not a query if it is not followed by a '/' or '//'

                        _Scanner.NextToken();
                        opnd1 = ParseRelativeLocationPath(opnd1);
                }
                else if (_Scanner.Token == XPathScanner.XPathTokenType.SlashSlash) {
                        opnd2 = new Axis(Axis.AxisType.DescendantOrSelf, opnd1);
                        _Scanner.NextToken();
                        opnd1 = ParseLocationPath(opnd2);
                }
            }
            else
            {
                opnd1 = ParseLocationPath(null);
            }

            return opnd1;
        }

        bool IsNodeType() {
            return _Scanner.Name == "node" 
                || _Scanner.Name == "text"
                || _Scanner.Name == "processing-instruction"
                || _Scanner.Name == "comment";
        }


        private AstNode ParseFilterExpr(AstNode  qyInput) {
            AstNode  opnd1 = ParsePrimaryExpr(qyInput);  
            AstNode  opnd2;
            while (XPathScanner.XPathTokenType.LBracket == _Scanner.Token) {
                // opnd must be a query
                opnd2 = ParsePredicate(opnd1);
                opnd1 = new Filter(opnd1, opnd2);
            }
            return opnd1;
        }

        private AstNode ParsePrimaryExpr(AstNode  qyInput) {
            AstNode  opnd = null;
            switch (_Scanner.Token) {
                case XPathScanner.XPathTokenType.String:
                    //Unescape the string since Operand expects the actual value
                    opnd = new Operand(Urn.UnEscapeString(_Scanner.Tstring));
                    break;
                case XPathScanner.XPathTokenType.Number:
                    opnd = new Operand(_Scanner.Number);
                    break;
                case XPathScanner.XPathTokenType.LParens:
                    _Scanner.NextToken();
                    opnd = ParseXPointer(qyInput);
                    if (opnd == null)
                    {
                        return null;
                    }

                    if (opnd.TypeOfAst != AstNode.QueryType.ConstantOperand)
                    {
                        opnd = new Group(opnd );
                    }

                    CheckToken(XPathScanner.XPathTokenType.RParens);
                    break;
                case XPathScanner.XPathTokenType.Dollar:
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.Name);
                    opnd = new Operand(_Scanner.Name, _Scanner.Prefix);
                    break;
                case XPathScanner.XPathTokenType.FuncFalse:
                    opnd = new Function(Function.FunctionType.FuncFalse);
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.LParens);
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.RParens);
                    break;
                case XPathScanner.XPathTokenType.FuncTrue:
                    opnd = new  Function(Function.FunctionType.FuncTrue);
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.LParens);
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.RParens);
                    break;
                case XPathScanner.XPathTokenType.FuncLast:
                    //if (qyInput == null)
                    //    throw new XPathException(XPathExceptionCode.QueryExpected, _Scanner.PchToken);
                    _Scanner.SkipSpace();
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.LParens);
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.RParens);
                    opnd = new Function(Function.FunctionType.FuncLast);
                    break;
                default:
                    opnd = ParseMethod(null); 
                    break;
            }
            _Scanner.NextToken();
            return opnd;
        }

        private AstNode ParseMethod(AstNode  qyInput) {
            if ((int)_Scanner.Lookahead != (int)XPathScanner.XPathTokenType.LParens) {
                throw new XPathException(XPathExceptionCode.FunctionExpected, _Scanner.PchToken);
            }
            if (_Scanner.Prefix.Length > 0 || !XPathScanner.FunctionTable.Contains(_Scanner.Name)) {
                return ParseXsltMethod(qyInput);
            }
            XPathScanner.XPathTokenType token  = (XPathScanner.XPathTokenType) XPathScanner.FunctionTable[_Scanner.Name];
            if (!MethodTable.Contains(token)) {
                return ParseXsltMethod(qyInput);
            }
            _Scanner.NextToken();
            AstNode  opnd = null;
            ParamInfo  paraminfo = (ParamInfo) MethodTable[token];
            ArrayList argList = new ArrayList();
            int argcount = 0;
            bool maxcheck = true;
            int count = 0;

            _Scanner.NextToken();
            if (token == XPathScanner.XPathTokenType.FuncConcat) {
                maxcheck = false;
                count = 0;
            }
            while (XPathScanner.XPathTokenType.RParens != _Scanner.Token) {

                if (maxcheck) {
                    count = argcount;
                    if (paraminfo.Maxargs == argcount)
                    {
                        throw new XPathException(XPathExceptionCode.InvalidNumArgs, _Scanner.PchToken);
                    }
                }

                opnd = ParseXPointer(qyInput);
                if (opnd == null)
                {
                    throw new XPathException(XPathExceptionCode.ExpressionExpected , _Scanner.PchToken);
                }

                if (paraminfo.ArgTypes[count] != AstNode.RType.Any && 
                    opnd.ReturnType != paraminfo.ArgTypes[count]) {
                    switch (paraminfo.ArgTypes[count]) {
                        case  AstNode.RType.NodeSet :
                            if (opnd.ReturnType != AstNode.RType.Variable && !(opnd is Function && opnd.ReturnType == AstNode.RType.Error) )
                            {
                                throw new XPathException(XPathExceptionCode.InvalidArgument , _Scanner.PchToken);
                            }

                            break;
                        case  AstNode.RType.String :
                            opnd = new Function(Function.FunctionType.FuncString, 
                                                opnd);
                            break;
                        case  AstNode.RType.Number :
                            opnd = new Function(Function.FunctionType.FuncNumber, 
                                                opnd);
                            break;
                        case  AstNode.RType.Boolean :
                            opnd = new Function(Function.FunctionType.FuncBoolean, 
                                                opnd);
                            break;
                    }
                }
                if (XPathScanner.XPathTokenType.Comma == _Scanner.Token) {
                    _Scanner.NextToken();
                    if (XPathScanner.XPathTokenType.RParens == _Scanner.Token)
                    {
                        throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                    }
                }

                argList.Add(opnd);
                argcount++;

            }
            if (paraminfo.Minargs > argcount)
            {
                throw new XPathException(XPathExceptionCode.InvalidNumArgs, _Scanner.PchToken);
            }

            opnd = new Function((Function.FunctionType)(-((int)token) - 84), argList);
            return opnd;
        }

        void CheckToken(XPathScanner.XPathTokenType t) {

            if (_Scanner.Token != t)
            {
                throw new XPathException(XPathExceptionCode.InvalidToken, _Scanner.PchToken);
            }
        }


        // NodeType can only be one of the following:
        // node(), text(), comment(), processing-instruction(), processing-instruction(LITERAL)
        void IsValidType(ref XPathNodeType  type) {

            if (_Scanner.Urn.Length > 0 || _Scanner.Prefix.Length > 0 )
            {
                throw new XPathException(XPathExceptionCode.NodeTestExpected, _Scanner.PchToken);
            }

            _Scanner.NextToken(); // token should now be LPARENS
            CheckToken(XPathScanner.XPathTokenType.LParens);

            if (_Scanner.Name == "node") {
                _Scanner.Name = String.Empty;
                type = XPathNodeType.All;
            }
            else if (_Scanner.Name ==  "text") {
                _Scanner.Name = String.Empty;
                type = XPathNodeType.Text; 
            }
            else if (_Scanner.Name == "processing-instruction") {
                type = XPathNodeType.ProcessingInstruction;
                if (')'  != _Scanner.Lookahead) {
                    _Scanner.NextToken();
                    CheckToken(XPathScanner.XPathTokenType.String);
                    _Scanner.Name = _Scanner.Tstring.Copy();
                }
                else
                {
                    _Scanner.Name = String.Empty;
                }
            }
            else if (_Scanner.Name == "comment") {
                _Scanner.Name = String.Empty;
                type = XPathNodeType.Comment;
            }
            else {
                throw new XPathException(XPathExceptionCode.NodeTestExpected, _Scanner.PchToken);
            }
            _Scanner.NextToken(); // should be RPAREN
            CheckToken(XPathScanner.XPathTokenType.RParens);
        }

/*
        bool IsFuncName() {
            if (_Scanner.Urn != null || _Scanner.Prefix != null)
                if (_Scanner.Urn.Length != 0 || _Scanner.Prefix.Length != 0)
                    return false;
            if ('(' != _Scanner.Lookahead)
                return false;

            if (XPathScanner.FunctionTable.Contains(_Scanner.Name)){
                _Scanner.Token = (XPathScanner.XPathTokenType) XPathScanner.FunctionTable[_Scanner.Name];
                return true;
            }

            return true;
        }
*/
        private struct ParamInfo {
            private int    _Minargs;
            private int    _Maxargs;
            private AstNode.RType[]  _ArgTypes;

            internal int Minargs {
                get {return _Minargs;}
            }

            internal int Maxargs {
                get {return _Maxargs;}
            }

            internal AstNode.RType[] ArgTypes {
                get {return _ArgTypes;}
            }

            internal ParamInfo(int minargs){
                _Minargs = minargs;
                _Maxargs = 0;
                _ArgTypes = null;
            }
            
            internal ParamInfo(int minargs,  int maxargs, AstNode.RType[] argTypes) {
                _Minargs = minargs;
                _Maxargs = maxargs;
                _ArgTypes = argTypes;
            }         
        } //ParamInfo

		//CHANGE TO INITLIAZIE ARRAY WHEN C# SUPPORTS IT

		static internal Hashtable MethodTable
		{
			get
			{
            
				if (_MethodTable == null)
				{
					_MethodTable =  new Hashtable(27);
                    
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncLast, new ParamInfo( 0 , 0, temparray1)); 
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncPosition, new ParamInfo(0 , 0,temparray1));

					_MethodTable.Add(XPathScanner.XPathTokenType.FuncName, new ParamInfo(0,1 , temparray2));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncNameSpaceUri, new ParamInfo(0, 1 , temparray2));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncLocalName, new ParamInfo(0, 1 , temparray2));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncCount, new ParamInfo(1, 1 , temparray2));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncSum, new ParamInfo(1, 1, temparray2));

  
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncNumber, new ParamInfo(0, 1, temparray3));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncBoolean, new ParamInfo(1, 1, temparray3));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncID, new ParamInfo(1, 1 , temparray3));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncString, new ParamInfo(0 , 1, temparray3));

					_MethodTable.Add( XPathScanner.XPathTokenType.FuncConcat, new ParamInfo(2 , 100, temparray4));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncLang, new ParamInfo(1, 1, temparray4));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncStringLength, new ParamInfo(0, 1, temparray4));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncNormalizeSpace, new ParamInfo(0, 1, temparray4));

					_MethodTable.Add(XPathScanner.XPathTokenType.FuncStartsWith, new ParamInfo(2, 2, temparray5));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncContains, new ParamInfo(2, 2, temparray5));
                    _MethodTable.Add(XPathScanner.XPathTokenType.FuncLike, new ParamInfo(2, 2, temparray5));
                    _MethodTable.Add(XPathScanner.XPathTokenType.FuncIn, new ParamInfo(2, 2, temparray5)); 
                    _MethodTable.Add(XPathScanner.XPathTokenType.FuncSubstringBefore, new ParamInfo(2, 2, temparray5));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncSubstringAfter, new ParamInfo(2, 2, temparray5));

    
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncSubstring, new ParamInfo(2, 3, temparray6));

                   

                
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncTranslate, new ParamInfo(3,3, temparray7));

     
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncNot, new ParamInfo(1,1, temparray8));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncTrue, new ParamInfo(0,0 ,temparray8));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncFalse, new ParamInfo(0,0, temparray8));


					_MethodTable.Add(XPathScanner.XPathTokenType.FuncFloor, new ParamInfo(1, 1, temparray9));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncCeiling, new ParamInfo(1, 1, temparray9));
					_MethodTable.Add(XPathScanner.XPathTokenType.FuncRound, new ParamInfo(1, 1, temparray9));
				}
				return _MethodTable;
			}
		}
                    
		private AstNode ParseXsltMethod(AstNode qyInput) 
		{
			AstNode  opnd = null;
			ArrayList argList = new ArrayList();
			int argcount = 0;
			string name = _Scanner.Name;
			string prefix = _Scanner.Prefix;
			NameReset();
			_Scanner.NextToken();
			_Scanner.NextToken();
			while (XPathScanner.XPathTokenType.RParens != _Scanner.Token) 
			{
				opnd = ParseXPointer(qyInput);
				if (XPathScanner.XPathTokenType.Comma == _Scanner.Token) 
				{
					_Scanner.NextToken();
					if (XPathScanner.XPathTokenType.RParens == _Scanner.Token)
                    {
                        throw new XPathException(XPathExceptionCode.ExpressionExpected,_Scanner.PchToken);
                    }
                }

				argList.Add(opnd);
				argcount++;
			}
			opnd = new Function(prefix, name, argList);
			return opnd;
		}
    } //class XPathHandler
} //namespace

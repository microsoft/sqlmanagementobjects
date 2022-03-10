// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;

    internal class Function : Microsoft.SqlServer.Management.Sdk.Sfc.AstNode {
        internal enum FunctionType {
            FuncLast = 0,
            FuncPosition,
            FuncCount,
            FuncLocalName,
            FuncNameSpaceUri,
            FuncName,
            FuncString,
            FuncBoolean,
            FuncNumber,
            FuncTrue,
            FuncFalse,
            FuncNot,
            FuncID,            
            FuncConcat,        
            FuncStartsWith,    
            FuncContains,    
            FuncSubstringBefore,
            FuncSubstringAfter,
            FuncSubstring,
            FuncStringLength,
            FuncNormalize,
            FuncTranslate,
            FuncLang,
            FuncSum,
            FuncFloor,
            FuncCeiling,
            FuncRound,
            FuncLike,
            FuncIn,
        FuncUserDefined,
        Error
        };

        private FunctionType _functionType = FunctionType.Error;
        private ArrayList _argumentList;

        private  String[] str = {
            "last()",
            "position()",
            "count()",
            "localname()",
            "namespaceuri()",
            "name()",
            "string()",
            "boolean()", 
            "number()",
            "true()",
            "false()",
            "not()",
            "id()",
            "concat()",
            "starts-with()",
            "contains()",
            "substring-before()",
            "substring-after()",
            "substring()",
            "string-length()",
            "normalize-space()",
            "translate()",
            "lang()",
            "sum()",
            "floor()", 
            "celing()",
            "round()",
            "like()",
            "in()",
            
        };

        private String _Name = null;
        private String _Prefix = null;

        internal Function(FunctionType ftype, ArrayList argumentList) {
            _functionType = ftype;
            _argumentList = new ArrayList(argumentList);
        }

        internal Function(String prefix, String name, ArrayList argumentList) {
            _functionType = FunctionType.FuncUserDefined;
            _Prefix = prefix;
            _Name = name;
            _argumentList = new ArrayList(argumentList);
        }

        internal Function(FunctionType ftype) {
            _functionType = ftype;
        }

        internal Function(FunctionType ftype, Microsoft.SqlServer.Management.Sdk.Sfc.AstNode arg) {
            _functionType = ftype;
            _argumentList = new ArrayList();
            _argumentList.Add(arg);
        }

        internal override QueryType TypeOfAst {
            get {return  QueryType.Function;}
        }

        internal override RType ReturnType {
            get {
                switch (_functionType) {
                    case FunctionType.FuncLast  : return RType.NodeSet;
                    case FunctionType.FuncPosition  : return RType.Number;
                    case FunctionType.FuncCount  : return RType.Number ;
                    case FunctionType.FuncID : return RType.NodeSet;
                    case FunctionType.FuncLocalName : return RType.String;
                    case FunctionType.FuncNameSpaceUri : return RType.String;
                    case FunctionType.FuncName  : return RType.String;
                    case FunctionType.FuncString  : return RType.String;
                    case FunctionType.FuncBoolean : return RType.Boolean; 
                    case FunctionType.FuncNumber : return RType.Number;
                    case FunctionType.FuncTrue: return RType.Boolean;
                    case FunctionType.FuncFalse : return RType.Boolean; 
                    case FunctionType.FuncNot : return RType.Boolean;
                    case FunctionType.FuncConcat : return RType.String;
                    case FunctionType.FuncStartsWith: return RType.Boolean;
                    case FunctionType.FuncContains : return RType.Boolean;
                    case FunctionType.FuncSubstringBefore: return RType.String; 
                    case FunctionType.FuncSubstringAfter  : return RType.String;
                    case FunctionType.FuncSubstring : return RType.String;
                    case FunctionType.FuncStringLength : return RType.Number;
                    case FunctionType.FuncNormalize : return RType.String;
                    case FunctionType.FuncTranslate : return RType.String;
                    case FunctionType.FuncLang : return RType.Boolean;
                    case FunctionType.FuncSum : return RType.Number;
                    case FunctionType.FuncFloor : return RType.Number; 
                    case FunctionType.FuncCeiling : return RType.Number;
                    case FunctionType.FuncRound : return RType.Number;
                    case FunctionType.FuncLike: return RType.Boolean;
                    case FunctionType.FuncIn: return RType.Boolean;
                }
                return RType.Error;
            }
        }

        internal FunctionType TypeOfFunction {
            get {return _functionType;}
        }

        internal ArrayList ArgumentList {
            get {return _argumentList;}
        }

        internal String Name {
            get {return _functionType == FunctionType.FuncUserDefined ? _Name : str[(int)_functionType];}
        }
    }
}

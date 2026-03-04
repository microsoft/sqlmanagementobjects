// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Facets;
using SMO = Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Operator Types
    /// </summary>
    public enum OperatorType
    {
	//
	// WARNING -- WARNING -- WARNING -- WARNING -- WARNING -- WARNING 
	//
	// Put new enum values near the end. See VSTS # 538101
	// Policy definitions on server side scripts have the enum values embedded
	//

        /// <summary>
        /// Default type to use when the type should be initialized before it is known
        /// </summary>
        NONE,
        /// <summary>
        /// Logical AND
        /// </summary>
        AND,
        /// <summary>
        /// Logical OR
        /// </summary>
        OR,
        /// <summary>
        /// Equals 
        /// </summary>
        EQ,
        /// <summary>
        /// Not equals 
        /// </summary>
        NE,
        /// <summary>
        /// Less than
        /// </summary>
        LT,
        /// <summary>
        /// Greater than
        /// </summary>
        GT,
        /// <summary>
        /// Less or equal to
        /// </summary>
        LE,
        /// <summary>
        /// Greater of equal to 
        /// </summary>
        GE,
        /// <summary>
        /// Equals to one of list items
        /// </summary>
        IN,
        /// <summary>
        /// Matches a string pattern
        /// </summary>
        LIKE,
        /// <summary>
        /// Reversed IN
        /// </summary>
        NOT_IN,
        /// <summary>
        /// Reversed LIKE
        /// </summary>
        NOT_LIKE,
        /// <summary>
        /// Bitwise equals
        /// </summary>
        BEQ,
        /// <summary>
        /// Bitwise not equals
        /// </summary>
        BNE,
    }

    /// <summary>
    /// Enum used for grouping System.Types into larger classes
    /// </summary>
    public enum TypeClass
    {
	//
	// WARNING -- WARNING -- WARNING -- WARNING -- WARNING -- WARNING 
	//
	// Put new enum values near the end. See VSTS # 538101
	// Policy definitions on server side scripts have the enum values embedded
	//

        /// <summary>
        /// Not supported type - functionally equivalent to null
        /// </summary>
        Unsupported = 0,
        /// <summary>
        /// Numeric types - includes integer, floating point, and fixed point
        /// </summary>
        Numeric,
        /// <summary>
        /// 
        /// </summary>
        String,
        /// <summary>
        /// 
        /// </summary>
        Bool,
        /// <summary>
        /// 
        /// </summary>
        DateTime,
        /// <summary>
        /// 
        /// </summary>
        Guid,
        /// <summary>
        /// 
        /// </summary>
        Array,
        /// <summary>
        /// Unknown return type
        /// </summary>
        Variant,
        /// <summary>
        /// Variable number of parameters
        /// </summary>
        VarArgs,
        /// <summary>
	///
        /// A bitmapped enumeration
        /// </summary>
        BitmappedEnum,
    }

    internal class SupportedTypeAttributes
    {
        internal TypeClass Class;

        internal List<OperatorType> SupportedOperators;

        internal SupportedTypeAttributes(TypeClass typeClass, List<OperatorType> operators)
        {
            this.Class = typeClass;
            this.SupportedOperators = operators;
        }
    }

    /// <summary>
    /// Static Factory class producing Evaluators
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class EvaluationFactory
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluationFactory");
        /// <summary>
        /// private constructor to prevent construction, as this object is not intended to be instanciated
        /// </summary>
        private EvaluationFactory() { }

        /// <summary>
        /// Indicates whether supplied Type is supported by ExpressionTree
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static bool IsTypeSupported(Type type)
        {
            return (type == typeof(DBNull) || null == type || supportedTypes.ContainsKey(type) || type.IsEnum);
        }

        /// <summary>
        /// Indiciate whether supplied Type can be used to construct a Constant
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static bool IsTypeSupportedForConstant(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (supportedTypes.ContainsKey(type))
            {
                TypeClass typeClass = supportedTypes[type].Class;
                return (typeClass == TypeClass.Numeric || typeClass == TypeClass.String);
            }
            else
            {
                return type.IsEnum;
            }
        }

        static readonly Dictionary<Type, SupportedTypeAttributes> supportedTypes = new Dictionary<Type, SupportedTypeAttributes>(21);
        static readonly Dictionary<TypeClass, List<OperatorType>> supportedTypeClass = new Dictionary<TypeClass, List<OperatorType>>();

        #region Supported Operators
        static readonly List<OperatorType> operatorsDefault = new List<OperatorType>(2);

        static EvaluationFactory()
        {
            List<OperatorType> operatorsNumeric = new List<OperatorType>(8);
            operatorsNumeric.Add(OperatorType.EQ);
            operatorsNumeric.Add(OperatorType.GE);
            operatorsNumeric.Add(OperatorType.GT);
            operatorsNumeric.Add(OperatorType.LE);
            operatorsNumeric.Add(OperatorType.LT);
            operatorsNumeric.Add(OperatorType.NE);
            operatorsNumeric.Add(OperatorType.IN);
            operatorsNumeric.Add(OperatorType.NOT_IN);

            List<OperatorType> operatorsString = new List<OperatorType>(6);
            operatorsString.Add(OperatorType.EQ);
            operatorsString.Add(OperatorType.NE);
            operatorsString.Add(OperatorType.LIKE);
            operatorsString.Add(OperatorType.IN);
            operatorsString.Add(OperatorType.NOT_LIKE);
            operatorsString.Add(OperatorType.NOT_IN);

            // We don't include AND & OR here so UI won't show them
            // And we block creation of operators with logical ops for bool constants
            // But logical operations on bool will succeed at evaluation time
            List<OperatorType> operatorsBool = new List<OperatorType>(2);
            operatorsBool.Add(OperatorType.EQ);
            operatorsBool.Add(OperatorType.NE);

            List<OperatorType> operatorsDateTime = new List<OperatorType>(6);
            operatorsDateTime.Add(OperatorType.EQ);
            operatorsDateTime.Add(OperatorType.GE);
            operatorsDateTime.Add(OperatorType.GT);
            operatorsDateTime.Add(OperatorType.LE);
            operatorsDateTime.Add(OperatorType.LT);
            operatorsDateTime.Add(OperatorType.NE);

            List<OperatorType> operatorsArray = new List<OperatorType>(4);
            operatorsArray.Add(OperatorType.EQ);
            operatorsArray.Add(OperatorType.NE);
            operatorsArray.Add(OperatorType.IN);
            operatorsArray.Add(OperatorType.NOT_IN);

            List<OperatorType> operatorsBitmappedEnum = new List<OperatorType>(2);
            operatorsBitmappedEnum.Add(OperatorType.BEQ);
            operatorsBitmappedEnum.Add(OperatorType.BNE);

            operatorsDefault.Add(OperatorType.EQ);
            operatorsDefault.Add(OperatorType.NE);

            supportedTypes.Add(typeof(int), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(byte), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(long), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(short), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(double), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(float), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(decimal), new SupportedTypeAttributes(TypeClass.Numeric, operatorsNumeric));
            supportedTypes.Add(typeof(string), new SupportedTypeAttributes(TypeClass.String, operatorsString));
            supportedTypes.Add(typeof(char), new SupportedTypeAttributes(TypeClass.String, operatorsString));
            supportedTypes.Add(typeof(bool), new SupportedTypeAttributes(TypeClass.Bool, operatorsBool));
            supportedTypes.Add(typeof(DateTime), new SupportedTypeAttributes(TypeClass.DateTime, operatorsDateTime));
            supportedTypes.Add(typeof(Guid), new SupportedTypeAttributes(TypeClass.Guid, operatorsDefault));
            supportedTypes.Add(typeof(Byte[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(int[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(long[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(short[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(double[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(float[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(decimal[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(string[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));
            supportedTypes.Add(typeof(char[]), new SupportedTypeAttributes(TypeClass.Array, operatorsArray));

            supportedTypeClass.Add(TypeClass.Numeric, operatorsNumeric);
            supportedTypeClass.Add(TypeClass.String, operatorsString);
            supportedTypeClass.Add(TypeClass.Bool, operatorsBool);
            supportedTypeClass.Add(TypeClass.DateTime, operatorsDateTime);
            supportedTypeClass.Add(TypeClass.Guid, operatorsDefault);
            supportedTypeClass.Add(TypeClass.Array, operatorsArray);
            supportedTypeClass.Add(TypeClass.BitmappedEnum, operatorsBitmappedEnum);

            // Special case for unverified Attributes
            supportedTypeClass.Add(TypeClass.Unsupported, operatorsDefault);
        }

        /// <summary>
        /// Returns a list of operators supported for the given type
        /// Empty list if evaluation for the type is not supported
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<OperatorType> SupportedOperators(Type type)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SupportedOperators", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(type);
                if (!IsTypeSupported(type))
                {
                    return new List<OperatorType>();
                }

                if (null == type || type.IsEnum)
                {
                    methodTraceContext.TraceParameterOut("returnVal", operatorsDefault);
                    return operatorsDefault;
                }

                return ((SupportedTypeAttributes)supportedTypes[type]).SupportedOperators;
            }
        }

        /// <summary>
        /// Returns a list of operators supported for the given TypeClass
        /// Throws if unxepected class requested
        /// </summary>
        /// <param name="typeClass"></param>
        /// <returns></returns>
        public static List<OperatorType> SupportedOperators(TypeClass typeClass)
        {
            traceContext.DebugAssert(supportedTypeClass.ContainsKey(typeClass),
                "Unexpected TypeClass requested for supported operators");

            return supportedTypeClass[typeClass];
        }

        #endregion Supported Operators


        /// <summary>
        /// Converts Like pattern to equivalent Regex pattern
        /// </summary>
        /// <param name="likePattern">Like pattern</param>
        /// <returns>Regex pattern</returns>
        public static string LikeToRegex(string likePattern)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("LikeToRegex", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(likePattern);
                const char cLikeAnyGroup = '%';
                const char cLikeAnyChar = '_';
                const string cRxAnyGroup = ".*";
                const char cRxAnyChar = '.';
                const char cOpenSquareBracket = '[';
                const char cCloseSquareBracket = ']';
                const string cRxEscapedOpenSquareBracket = "\\[";
                const string cRxEscapedCloseSquareBracket = "\\]";
                const char cOpenBracket = '(';
                const char cCloseBracket = ')';
                const string cRxEscapedOpenBracket = "\\(";
                const string cRxEscapedCloseBracket = "\\)";
                const char cLikeStar = '*';
                const string cRxEscapedStar = "\\*";
                const char cLikeDot = '.';
                const string cRxEscapedDot = "\\.";
                const char cLikeEscapeChar = '\\';
                const string cRxEscapedEscape = "\\\\";
                const char cUp = '^';
                const string cRxEscapedUp = "\\^";
                const char cDollar = '$';
                const string cRxEscapedDollar = "\\$";
                const char cPlus = '+';
                const string cRxEscapedPlus = "\\+";
                const char cQuestion = '?';
                const string cRxEscapedQuestion = "\\?";
                const string cRxMatchAtTheBeginningIgnoreMultiline = "\\A";
                const string cRxMatchAtTheEndIgnoreMultiline = "\\Z";

                Regex rx = new Regex("\\[.\\]");

                // preprocess - remove doubled quotes
                likePattern = likePattern.Replace("''", "'");


                bool enterEscapeMode = false;
                bool EscapeMode = false;
                bool OpenBracket = false;


                StringBuilder rxPattern = new StringBuilder();
                rxPattern.Append(cRxMatchAtTheBeginningIgnoreMultiline);

                for (int i = 0; i < likePattern.Length; i++)
                {
                    switch (likePattern[i])
                    {
                        case cLikeEscapeChar:
                            if (EscapeMode)
                            {
                                rxPattern.Append(cRxEscapedEscape);
                            }
                            else
                            {
                                enterEscapeMode = true;
                            }
                            break;
                        case cLikeAnyGroup:
                            if (EscapeMode)
                            {
                                rxPattern.Append(likePattern[i]);
                            }
                            else
                            {
                                rxPattern.Append(cRxAnyGroup);
                            }
                            break;
                        case cLikeAnyChar:
                            if (EscapeMode)
                            {
                                rxPattern.Append(likePattern[i]);
                            }
                            else
                            {
                                rxPattern.Append(cRxAnyChar);
                            }
                            break;
                        case cLikeStar:
                            rxPattern.Append(cRxEscapedStar);
                            break;
                        case cLikeDot:
                            rxPattern.Append(cRxEscapedDot);
                            break;
                        case cOpenSquareBracket:
                            if (EscapeMode)
                            {
                                rxPattern.Append(cRxEscapedOpenSquareBracket);
                            }
                            else
                            {
                                string s = String.Empty;
                                char c = Char.MinValue;

                                if (likePattern.Length >= i + 3)
                                {
                                    s = likePattern.Substring(i, 3);
                                    c = s[1];
                                }
                                else if (likePattern.Length >= i + 2)
                                {
                                    c = likePattern[i + 1];
                                }

                                if (rx.IsMatch(s))
                                {
                                    // Special case - escaping, using brackets (ex. '[%][_][/]')

                                    switch (s[1])
                                    {
                                        case cLikeStar:
                                            rxPattern.Append(cRxEscapedStar);
                                            break;
                                        case cOpenSquareBracket:
                                            rxPattern.Append(cRxEscapedOpenSquareBracket);
                                            break;
                                        case cCloseSquareBracket:
                                            rxPattern.Append(cRxEscapedCloseSquareBracket);
                                            break;
                                        case cLikeDot:
                                            rxPattern.Append(cRxEscapedDot);
                                            break;
                                        case cLikeEscapeChar:
                                            rxPattern.Append(cRxEscapedEscape);
                                            break;
                                        case cUp:
                                            rxPattern.Append(cRxEscapedUp);
                                            break;
                                        case cDollar:
                                            rxPattern.Append(cRxEscapedDollar);
                                            break;
                                        case cPlus:
                                            rxPattern.Append(cRxEscapedPlus);
                                            break;
                                        case cQuestion:
                                            rxPattern.Append(cRxEscapedQuestion);
                                            break;
                                        default:
                                            rxPattern.Append(s[1]);
                                            break;
                                    }
                                    i += 2;
                                    continue;
                                }

                                rxPattern.Append(likePattern[i]);

                                if (c == '^')
                                {
                                    // Special case - exclusion (ex. '[^abc]%'

                                    rxPattern.Append(c);
                                    i += 1;
                                }

                                OpenBracket = true;
                            }
                            break;
                        case cCloseSquareBracket:
                            if (EscapeMode)
                            {
                                rxPattern.Append(cRxEscapedCloseSquareBracket);
                            }
                            else
                            {
                                rxPattern.Append(likePattern[i]);
                                OpenBracket = false;
                            }
                            break;
                        case cOpenBracket:
                            rxPattern.Append(cRxEscapedOpenBracket);
                            break;
                        case cCloseBracket:
                            rxPattern.Append(cRxEscapedCloseBracket);
                            break;
                        case cUp:
                            rxPattern.Append(cRxEscapedUp);
                            break;
                        case cDollar:
                            rxPattern.Append(cRxEscapedDollar);
                            break;
                        case cPlus:
                            rxPattern.Append(cRxEscapedPlus);
                            break;
                        case cQuestion:
                            rxPattern.Append(cRxEscapedQuestion);
                            break;
                        default:
                            rxPattern.Append(likePattern[i]);
                            break;
                    }

                    if (enterEscapeMode)
                    {
                        EscapeMode = true;
                        enterEscapeMode = false;
                    }
                    else
                    {
                        EscapeMode = false;
                    }
                }

                if (OpenBracket)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ParsingArgumentException(likePattern, ExceptionTemplatesSR.ParsingUnclosedBracketMsg)));
                }

                rxPattern.Append(cRxMatchAtTheEndIgnoreMultiline);

                return rxPattern.ToString();
            }
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static TypeClass ClassifyType(object obj)
        {
            return ClassifyType(obj.GetType());
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static TypeClass ClassifyType(Type type)
        {
            if (supportedTypes.ContainsKey(type))
            {
                return ((SupportedTypeAttributes)supportedTypes[type]).Class;
            }
            else if (type.IsEnum)
            {
                return ((SupportedTypeAttributes)supportedTypes[typeof(int)]).Class;
            }
            else if (type.IsArray)
            {
                // Special case - some functions return object[], which is not an official supported type
                // Those functions don't have specific return type, it's determined at runtime
                // actual values in the array are supported types

                return TypeClass.Array;
            }

            return TypeClass.Unsupported;
        }

        internal static Evaluator ConstructEvaluator(object left, object right, OperatorType opType, FacetEvaluationContext context)
        {
            // Check for NULL first
            //
            if (null == left || null == right)
            {
                return new EvaluatorNull();
            }

            // Check for DBNull
            if (left is DBNull || right is DBNull)
            {
                return new EvaluatorDBNull();
            }

            // Arrays are treated separately as they are containers of values
            // Some functions return back an array of objects, where actual objects are of supported type
            //
            if (!(left is Array) && !IsTypeSupported(left.GetType()))
            {
                throw traceContext.TraceThrow(new UnsupportedObjectTypeException(left.GetType().Name, typeof(EvaluationFactory).Name));
            }
            if (!(right is Array) && !IsTypeSupported(right.GetType()))
            {
                throw traceContext.TraceThrow(new UnsupportedObjectTypeException(right.GetType().Name, typeof(EvaluationFactory).Name));
            }

            TypeClass leftTypeClass = ClassifyType(left);
            TypeClass rightTypeClass = ClassifyType(right);

            switch (opType)
            {
                case OperatorType.IN:
                case OperatorType.NOT_IN:
                    // This is a runtime validation
                    // Operator API cannot validate Attribute types, so illegal operators can be constructed
                    //
                    if (!SupportedOperators(left.GetType()).Contains(opType))
                    {
                        throw traceContext.TraceThrow(new OperatorNotApplicableException(ExpressionNodeOperator.OperatorTypeToString(opType), leftTypeClass.ToString()));
                    }
                    if (leftTypeClass == TypeClass.Array)
                    {
                        throw traceContext.TraceThrow(new InvalidInOperatorException(ExpressionNodeOperator.OperatorTypeToString(opType)));
                    }
                    if (leftTypeClass == TypeClass.Numeric && rightTypeClass == TypeClass.Array)
                    {
                        return new EvaluatorArrayNumeric();
                    }
                    else if (leftTypeClass == TypeClass.String && rightTypeClass == TypeClass.Array)
                    {
                        return new EvaluatorArrayString(context);
                    }
                    else
                    {
                        throw traceContext.TraceThrow(new ExpressionTypeMistmatchException(leftTypeClass.ToString(), rightTypeClass.ToString()));
                    }

                case OperatorType.BEQ:
                case OperatorType.BNE:
                    return new EvaluatorBitmappedEnum();

                case OperatorType.NONE:
                    traceContext.DebugAssert(false, "Unsupported Operator");
                    return null;

                default:
                    if (leftTypeClass != rightTypeClass)
                    {
                        throw traceContext.TraceThrow(new ExpressionTypeMistmatchException(leftTypeClass.ToString(), rightTypeClass.ToString()));
                    }

                    switch (leftTypeClass)
                    {
                        case TypeClass.Numeric:
                            return new EvaluatorNumeric();
                        case TypeClass.String:
                            return new EvaluatorString(context);
                        case TypeClass.Bool:
                            return new EvaluatorBool();
                        case TypeClass.DateTime:
                            return new EvaluatorDateTime();
                        case TypeClass.Guid:
                            return new EvaluatorGuid();
                        case TypeClass.Array:
                            return EvaluatorArray.ConstructEvaluator(left);
                        default:
                            traceContext.DebugAssert(false, "Unsupported TypeClass");
                            return null;
                    }
            }
        }

        internal static bool Evaluate(object left, object right, OperatorType opType, FacetEvaluationContext context)
        {
            Evaluator evaluator = null;

            evaluator = ConstructEvaluator(left, right, opType, context);

            if (null == evaluator)
            {
                traceContext.DebugAssert(false, String.Format("Failed to construct evaluator for types '{0}','{1}'", left.GetType().Name, right.GetType().Name));
            }

            evaluator.SetEvaluatorObjects(left, right);
            bool result = evaluator.Evaluate(opType);
            return result;
        }

        internal static bool Evaluate(object left, object right, OperatorType opType)
        {
            return Evaluate(left, right, opType, null);
        }

    }

    #region Evaluators
    internal abstract class Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "Evaluator");
        public abstract bool Evaluate(OperatorType opType);

        public abstract void SetEvaluatorObjects(object left, object right);

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static double ConvertToDouble(object number)
        {
            return Convert.ToDouble(number);
        }

        internal static Int64 ConvertToLong(object number)
        {
            return Convert.ToInt64(number);
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static string ConvertToString(object str)
        {
            return Convert.ToString(str);
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static bool ConvertToBool(object b)
        {
            return Convert.ToBoolean(b);
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static DateTime ConvertToDateTime(object dt)
        {
            return Convert.ToDateTime(dt);
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static Guid ConvertToGuid(object guid)
        {
            return (Guid)guid;
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static Array ConvertToArray(object array)
        {
            return (Array)array;
        }

        internal static object ConvertToAny(object input, TypeClass typeClass)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ConvertToAny"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(input, typeClass);
                switch (typeClass)
                {
                    case TypeClass.Numeric:
                        return ConvertToDouble(input);
                    case TypeClass.String:
                        return ConvertToString(input);
                    case TypeClass.Bool:
                        return ConvertToBool(input);
                    case TypeClass.DateTime:
                        return ConvertToDateTime(input);
                    case TypeClass.Guid:
                        return ConvertToGuid(input);
                    case TypeClass.Array:
                        return ConvertToArray(input);
                    default:
                        traceContext.DebugAssert(false, "Unsupported TypeClass");
                        methodTraceContext.TraceParameterOut("returnVal", null);
                        return null;  // Make the compiler happy
                }
            }
        }
    }

    internal class EvaluatorNull : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorNull");
        object left;
        object right;

        internal EvaluatorNull()
        {
            this.left = null;
            this.right = null;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            this.left = left;
            this.right = right;
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                if (false == (opType == OperatorType.EQ || opType == OperatorType.NE))
                {
                    methodTraceContext.TraceParameterOut("returnVal", false);
                    return false;
                }

                if (null == left && null == right)
                {
                    return (opType == OperatorType.EQ);
                }
                else
                {
                    return (opType == OperatorType.NE);
                }
            }
        }

    }

    /// <summary>
    /// Evaluates an expression that has a DBNull value on one of its sides (or both).
    /// DBNull always evaluates to false regardless of the operand on the other side
    /// </summary>
    internal class EvaluatorDBNull : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorDBNull");
        object left;
        object right;

        internal EvaluatorDBNull()
        {
            this.left = null;
            this.right = null;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            this.left = left;
            this.right = right;
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                return false;
            }
        }

    }

    /// <summary>
    /// Evaluates an expression with an enumerator
    /// </summary>
    internal class EvaluatorBitmappedEnum : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorEnum");
        object left;
        object right;

        internal EvaluatorBitmappedEnum()
        {
            this.left = null;
            this.right = null;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            this.left = left;
            this.right = right;
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }

        /// <summary>
        /// The evaluator asserts that at least the right operand is a bitmapped enum.
        /// The evaluation is done as follows:
        ///     BEQ: left AND right == right
        ///     BNE: left AND right != right
        /// </summary>
        /// <param name="opType">The operations type. This has to be == or !=.</param>
        /// <returns></returns>
        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);

                // Make sure at least the right operand is a bitmapped enum
                methodTraceContext.Assert(right.GetType().IsBitmappedEnum(), "Incorrect operand types used for bitmapped enum evaluation: leftType = " + left.GetType() + " rightType = " + right.GetType());

                //we need to cast the operands to integers for the bitwise ops to work
                //technically speaking, if the domain has defined an enumeratoin as a bitmap, there shouldn't 
                //be a legitimate value that is equal to 0, but since .Net does not enforce it statically, we will
                //check for it.
                int l = (int)left;
                int r = (int)right;
                bool equal = (l == 0 && r == 0) || ((l & r) == r);
                switch (opType)
                {
                    case OperatorType.BEQ:
                        return equal;

                    case OperatorType.BNE:
                        return !equal;

                    default: //we should never be here! The factory shouldn't create us with any other opType.
                        methodTraceContext.Assert(false, "Incorrect operator used for enum evaluation: " + opType);
                        return false;
                }
            }
        }
    }

    internal class EvaluatorString : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorString");
        string left;
        string right;
        IComparer stringComparer;
        CompareOptions compareOptions;

        public string Left
        {
            get { return this.left; }
            set
            {
                traceContext.TraceVerbose("Setting Left to: {0}", value);
                this.left = value;
            }
        }
        public string Right
        {
            get { return this.right; }
            set
            {
                traceContext.TraceVerbose("Setting Right to: {0}", value);
                this.right = value;
            }
        }

        internal EvaluatorString()
        {
            this.left = null;
            this.right = null;
            this.stringComparer = null;
        }

        internal EvaluatorString(FacetEvaluationContext context)
        {
            traceContext.TraceMethodEnter("EvaluatorString");
            // Tracing Input Parameters
            traceContext.TraceParameters(context);
            this.left = null;
            this.right = null;
            this.stringComparer = null;

            if (context != null)
            {
                SMO.SqlSmoObject smoobj = context.PhysicalTarget as SMO.SqlSmoObject;

                // If context represents a Smo object, get its StringComparer 
                // to compare strings, and get CultureInfo and CompareOptions 
                // for Regex matches
                if (null != smoobj)
                {
                    stringComparer = smoobj.StringComparer;
                    if (null != smoobj.StringComparer)
                    {
                        compareOptions = smoobj.StringComparer.CompareOptions;
                    }
                }
            }

            traceContext.TraceMethodExit("EvaluatorString");
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            this.Left = ConvertToString(left);
            this.Right = ConvertToString(right);
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                bool ret = false;

                switch (opType)
                {
                    case OperatorType.EQ:
                        if (stringComparer == null)
                        {
                            ret = (left == right);
                        }
                        else
                        {
                            ret = stringComparer.Compare(left, right) == 0;
                        }
                        break;
                    case OperatorType.NE:
                        if (stringComparer == null)
                        {
                            ret = (left != right);
                        }
                        else
                        {
                            ret = stringComparer.Compare(left, right) != 0;
                        }
                        break;
                    case OperatorType.LIKE:
                        ret = EvaluateLike(left, right);
                        break;
                    case OperatorType.NOT_LIKE:
                        ret = !EvaluateLike(left, right);
                        break;
                    default:
                        throw methodTraceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.String.ToString()));
                }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }

        bool EvaluateLike(string str, string likePattern)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EvaluateLike"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(str, likePattern);
                bool ret = false;

                try
                {
                    string rxPattern = EvaluationFactory.LikeToRegex(likePattern);

                    // figure out if we want case sensitivity in comparison or not
                    RegexOptions options = RegexOptions.None;
                    if (this.stringComparer != null &&
                        (this.compareOptions & CompareOptions.IgnoreCase) != 0)
                    {
                        options = RegexOptions.IgnoreCase;
                    }

                    // we are doing a culture invariant comparison
                    // without this option we would be using the culture 
                    // of the calling thread. 
                    // we cannot set the culture because this demands security
                    // permissions that are not available when running inside SQLCLR
                    // TODO: see VSTS 167647 to address this problem.
                    options |= RegexOptions.CultureInvariant;
                    Regex rx = new Regex(rxPattern, options);

                    ret = rx.IsMatch(str);
                }
                catch (System.ArgumentException)
                { }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }

    }

    internal class EvaluatorNumeric : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorNumeric");
        double left;
        double right;
        bool hasEnum;

        public double Left
        {
            get { return this.left; }
            set
            {
                traceContext.TraceVerbose("Setting Left to: {0}", value);
                this.left = value;
            }
        }
        public double Right
        {
            get { return this.right; }
            set
            {
                traceContext.TraceVerbose("Setting Right to: {0}", value);
                this.right = value;
            }
        }

        internal EvaluatorNumeric()
        {
            this.left = 0;
            this.right = 0;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SetEvaluatorObjects"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(left, right);
                // This is a numeric evaluator, the objects must be non-null.
                if (left == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("left"));
                }

                if (right == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("right"));
                }

                this.left = ConvertToDouble(left);
                this.right = ConvertToDouble(right);
                hasEnum = (left.GetType().IsEnum || right.GetType().IsEnum);
            }
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                bool ret = false;

                // Special case - Enum
                //
                if (hasEnum && opType != OperatorType.EQ && opType != OperatorType.NE)
                {
                    throw methodTraceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), typeof(Enum).ToString()));
                }

                switch (opType)
                {
                    case OperatorType.EQ:
                        ret = (left == right);
                        break;
                    case OperatorType.NE:
                        ret = (left != right);
                        break;
                    case OperatorType.GE:
                        ret = (left >= right);
                        break;
                    case OperatorType.GT:
                        ret = (left > right);
                        break;
                    case OperatorType.LE:
                        ret = (left <= right);
                        break;
                    case OperatorType.LT:
                        ret = (left < right);
                        break;
                    default:
                        throw methodTraceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.Numeric.ToString()));
                }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }
    }

    internal class EvaluatorDateTime : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorDateTime");
        DateTime left;
        DateTime right;

        internal EvaluatorDateTime()
        {
            this.left = DateTime.MinValue;
            this.right = DateTime.MinValue;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            this.left = ConvertToDateTime(left);
            this.right = ConvertToDateTime(right);
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                bool ret = false;

                switch (opType)
                {
                    case OperatorType.EQ:
                        ret = (left == right);
                        break;
                    case OperatorType.NE:
                        ret = (left != right);
                        break;
                    case OperatorType.GE:
                        ret = (left >= right);
                        break;
                    case OperatorType.GT:
                        ret = (left > right);
                        break;
                    case OperatorType.LE:
                        ret = (left <= right);
                        break;
                    case OperatorType.LT:
                        ret = (left < right);
                        break;
                    default:
                        throw methodTraceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.DateTime.ToString()));
                }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }
    }

    internal class EvaluatorGuid : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorGuid");
        Guid left;
        Guid right;

        internal EvaluatorGuid()
        {
            this.left = Guid.Empty;
            this.right = Guid.Empty;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            this.left = (Guid)left;
            this.right = (Guid)right;
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                bool ret = false;

                switch (opType)
                {
                    case OperatorType.EQ:
                        ret = (left == right);
                        break;
                    case OperatorType.NE:
                        ret = (left != right);
                        break;
                    default:
                        throw methodTraceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.Guid.ToString()));
                }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }
    }

    internal class EvaluatorBool : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorBool");
        bool left;
        bool right;

        internal EvaluatorBool()
        {
            this.left = false;
            this.right = false;
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SetEvaluatorObjects"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(left, right);
                // This is a boolean evaluator, the objects must be non-null.
                if (left == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("left"));
                }

                if (right == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("right"));
                }

                this.left = ConvertToBool(left);
                this.right = ConvertToBool(right);
            }
        }

        public override bool Evaluate(OperatorType opType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Evaluate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(opType);
                bool ret = false;

                switch (opType)
                {
                    case OperatorType.AND:
                        ret = (left && right);
                        break;
                    case OperatorType.OR:
                        ret = (left || right);
                        break;
                    case OperatorType.EQ:
                        ret = (left == right);
                        break;
                    case OperatorType.NE:
                        ret = (left != right);
                        break;
                    default:
                        throw methodTraceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.Bool.ToString()));
                }

                methodTraceContext.TraceParameterOut("returnVal", ret);
                return ret;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal abstract class EvaluatorArray : Evaluator
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorArray");
        Array left;
        Array right;
        object inObject;
        Evaluator evaluator;

        /// <summary>
        /// 
        /// </summary>
        protected Array Left
        {
            get
            {
                return this.left;
            }
            set
            {
                traceContext.TraceVerbose("Setting Left to: {0}", value);
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.left = (Array)value.Clone();
                Array.Sort(this.left);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected Array Right
        {
            get
            {
                return this.right;
            }
            set
            {
                traceContext.TraceVerbose("Setting Right to: {0}", value);
                // null arrays are not allowed, because later code looks at length
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.right = (Array)value.Clone();
                Array.Sort(this.right);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected object InObject
        {
            get
            {
                return this.inObject;
            }
            set
            {
                traceContext.TraceVerbose("Setting InObject to: {0}", value);
                // null arrays are not allowed, because later code looks at length
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.inObject = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected Evaluator Evaluator
        {
            get { return this.evaluator; }
            set
            {
                traceContext.TraceVerbose("Setting Evaluator to: {0}", value);
                this.evaluator = value;
            }
        }

        protected EvaluatorArray()
        {
            this.left = null;
            this.right = null;
            this.inObject = null;
        }

        public static Evaluator ConstructEvaluator(object arg)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ConstructEvaluator"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(arg);
                // Later the code looks at the array length, thus the arg must be non-null
                if (arg == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("arg"));
                }

                object obj = null;
                Array array = (Array)arg;

                for (int i = 0; i < array.Length; i++)
                {
                    obj = array.GetValue(i);
                    if (null != obj)
                    {
                        TypeClass typeClass = EvaluationFactory.ClassifyType(obj);
                        if (typeClass == TypeClass.Numeric)
                        {
                            return new EvaluatorArrayNumeric();
                        }
                        else if (typeClass == TypeClass.String)
                        {
                            return new EvaluatorArrayString();
                        }
                        else
                        {
                            methodTraceContext.TraceParameterOut("returnVal", null);
                            return null;
                        }
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", null);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected bool ArraysEqual()
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                evaluator.SetEvaluatorObjects(left.GetValue(i), right.GetValue(i));
                if (!evaluator.Evaluate(OperatorType.EQ))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Evaluate(OperatorType opType)
        {
            switch (opType)
            {
                case OperatorType.EQ:
                case OperatorType.NE:
                    if (null == Right)
                    {
                        throw traceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.Array.ToString()));
                    }

                    if (ArraysEqual())
                    {
                        return (opType == OperatorType.EQ);
                    }
                    else
                    {
                        return (opType == OperatorType.NE);
                    }
                case OperatorType.IN:
                case OperatorType.NOT_IN:
                    if (null == InObject)
                    {
                        throw traceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.Array.ToString()));
                    }

                    bool ret = false;
                    for (int i = 0; i < Right.Length; i++)
                    {
                        evaluator.SetEvaluatorObjects(InObject, Right.GetValue(i));
                        if (evaluator.Evaluate(OperatorType.EQ))
                        {
                            ret = true;
                            break;
                        }
                    }

                    if (ret)
                    {
                        // If we found a match, return TRUE for 'IN', FALSE for 'NOT_IN'
                        return (opType == OperatorType.IN);
                    }
                    else
                    {
                        // If we didn't find a match return TRUE for 'NOT_IN', FALSE for 'IN'
                        return (opType == OperatorType.NOT_IN);
                    }
                default:
                    throw traceContext.TraceThrow(new OperatorNotApplicableException(opType.ToString(), TypeClass.Array.ToString()));
            }
        }
    }


    internal class EvaluatorArrayNumeric : EvaluatorArray
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorArrayNumeric");
        internal EvaluatorArrayNumeric()
            : base()
        {
            this.Evaluator = new EvaluatorNumeric();
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            if (left is Array)
            {
                this.Left = ConvertToArray(left);
                this.Right = ConvertToArray(right);
            }
            else
            {
                this.InObject = ConvertToDouble(left);
                this.Right = ConvertToArray(right);
            }
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }
    }

    internal class EvaluatorArrayString : EvaluatorArray
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "EvaluatorArrayString");
        internal EvaluatorArrayString()
            : base()
        {
            this.Evaluator = new EvaluatorString();
        }

        internal EvaluatorArrayString(FacetEvaluationContext context)
            : base()
        {
            traceContext.TraceMethodEnter("EvaluatorArrayString");
            // Tracing Input Parameters
            traceContext.TraceParameters(context);
            this.Evaluator = new EvaluatorString(context);
            traceContext.TraceMethodExit("EvaluatorArrayString");
        }

        public override void SetEvaluatorObjects(object left, object right)
        {
            traceContext.TraceMethodEnter("SetEvaluatorObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(left, right);
            if (left is Array)
            {
                this.Left = ConvertToArray(left);
                this.Right = ConvertToArray(right);
            }
            else
            {
                this.InObject = ConvertToString(left);
                this.Right = ConvertToArray(right);
            }
            traceContext.TraceMethodExit("SetEvaluatorObjects");
        }
    }

    #endregion Evaluators
}

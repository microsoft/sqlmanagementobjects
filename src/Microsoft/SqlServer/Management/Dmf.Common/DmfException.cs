// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using System;
using System.Reflection;
#if NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.Serialization;
#if NETFRAMEWORK
using System.Security.Permissions;
#endif
using System.Text;
using Microsoft.SqlServer.Management.Dmf.Common;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Types of Dmf Exceptions
    /// </summary>
    public enum DmfExceptionType
    {
        /// Base type
        DmfException = 0,
        /// Assembly Already Registered (repeated registration)
        AssemblyAlreadyRegistered,
        /// Adapter Already Exists (repeated registration)
        AdapterAlreadyExists,
        /// Adapter has constructor not complying with sepification
        AdapterWrongNumberOfArguments,
        /// Incompatible types in operator
        ExpressionTypeMistmatch,
        /// Operator not defined for the type
        OperatorNotApplicable,
        /// Unexpected type of argument
        FunctionWrongArgumentType,
        /// Unexpected number of arguments for the function
        FunctionWrongArgumentsNumber,
        /// Null Facet (null reference)
        NullFacet,
        /// ExpressionNode deserialization exception
        ExpressionSerialization,
        /// Wrapper for System.FormatException
        TypeConversion,
        /// Unsupported Constant Type
        UnsupportedType,
        /// Tree cannot be evaluated
        BadExpressionTree,
        /// Given type is not supported by receiving host
        UnsupportedObjectType,
        /// The expression node is non-configurable
        ExpressionNodeNotConfigurable,
        /// Can't convert ExpressionNode to FilterNode
        ConversionNotSupported,
        ///
        InvalidOperand,
        ///
        InvalidInOperator,
        ///
        DmfSecurity,
        /// Generic Validation exception (base)
        ObjectValidation,
        /// Combination of set properties prevents object creation/modification
        ConflictingPropertyValues,
        ///
        ObjectAlreadyExists,
        ///
        MissingObject,
        ///
        PolicyEvaluation,
        /// <summary>
        /// The policy Job Schedule GUID is required when the policy execution mode is not None.
        /// </summary>
        MissingJobSchedule,
        ///
        BadEventData,
        /// Exception that gets thrown when an operation has failed.
        FailedOperation,
        /// The expression node is non-configurable operator
        ExpressionNodeNotConfigurableOperator,
        /// The property is read only and can't be modified
        NonConfigurableReadOnlyProperty,
        /// Unknown property
        MissingProperty,
        /// The property cannot be retrieved
        NonRetrievableProperty,
        /// There is no association between target type and facet
        MissingTypeFacetAssociation,
        /// Unexpected return type 
        FunctionWrongReturnType,
        /// Can't locate a SMO Server object in the hiearchy to run a query
        FunctionNoServer,
        /// Target is not a SMO object; can't execute T-SQL against it
        FunctionNotASmoObject,
        /// Bad date part
        FunctionBadDatePart,
        /// More than one column returned by SQL or WQL scalar functions
        FunctionTooManyColumns,
        /// <summary>
        /// The value specified for a property is invalid (e.g too long, unacceptable value)
        /// </summary>
        StringPropertyTooLong,
        /// <summary>
        /// Number of target sets mismatch between number supported and that specified in the object set
        /// </summary>
        TargetSetCountMismatch,
        /// <summary>
        /// Unsupported target set type specified in object set for a given facet
        /// </summary>
        UnsupportedTargetSetForFacet,
        /// <summary>
        /// At least one target set needs to be enabled for an Object Set
        /// </summary>
        NoTargetSetEnabled,
        /// property cannot be accessed because a restart of the service is pending
        RestartPending,
    }

    /// <summary>
    /// Base exception class for all SMO exception classes
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif

    public class DmfException : SqlServerManagementException
    {
        const int INIT_BUFFER_SIZE = 1024;

        /// <summary>
        /// Base constructor
        /// </summary>

#if NETFRAMEWORK
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
#endif
        public DmfException()
            : base()
        {
            Init();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
#if NETFRAMEWORK
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
#endif
        public DmfException(string message)
            : base(message)
        {
            Init();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
#if NETFRAMEWORK
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
#endif
        public DmfException(string message, Exception innerException)
            :
            base(message, innerException)
        {
            Init();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
#if NETFRAMEWORK
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
#endif

        protected DmfException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected virtual void Init()
        {
            Data.Add("HelpLink.ProdVer", ProdVer);
        }

        private static readonly string prodVer = ((AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)[0]).Version;
        /// <summary>
        /// Product Version
        /// </summary>
        protected static string ProdVer
        {
            get
            {
                return prodVer;
            }
        }

        /// <summary>
        /// Sets Help Context
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        internal protected DmfException SetHelpContext(string resource)
        {

            Data["HelpLink.EvtSrc"] = (resource);

            return this;
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public virtual DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.DmfException;
            }
        }


        /// <summary>
        /// will output a link to the help web site
        /// <!--http://www.microsoft.com/products/ee/transform.aspx?ProdName=Microsoft%20SQL%20Server&ProdVer=09.00.0000.00&EvtSrc=MSSQLServer&EvtID=15401-->
        /// </summary>
        public override string HelpLink
        {
            get
            {
                StringBuilder link = new StringBuilder(INIT_BUFFER_SIZE);
                link.Append(Data["HelpLink.BaseHelpUrl"] as string);
                link.Append("?");
                link.AppendFormat("ProdName={0}", Data["HelpLink.ProdName"] as string);

                if (Data.Contains("HelpLink.ProdVer"))
                    link.AppendFormat("&ProdVer={0}", Data["HelpLink.ProdVer"] as string);

                if (Data.Contains("HelpLink.EvtSrc"))
                    link.AppendFormat("&EvtSrc={0}", Data["HelpLink.EvtSrc"] as string);

                if (Data.Contains("HelpLink.EvtData1"))
                {
                    link.AppendFormat("&EvtID={0}", Data["HelpLink.EvtData1"] as string);
                    for (int i = 2; i < 10; i++)
                    {
                        if (Data.Contains("HelpLink.EvtData" + i))
                        {
                            link.Append("+");
                            link.Append(Data["HelpLink.EvtData" + i] as string);
                        }
                        else
                            break;
                    }
                }

                // this needs to be the last one so that it appears at the bottom of the
                // list of information displayed in the privacy confirmation dialog.
                link.AppendFormat("&LinkId={0}", Data["HelpLink.LinkId"] as string);

                return link.ToString().Replace(' ', '+');
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when FacetRepository attempts to scan the same assembly for the second time
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class AssemblyAlreadyRegisteredException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public AssemblyAlreadyRegisteredException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public AssemblyAlreadyRegisteredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an Exception for repeated attempt to register the same assembly
        /// </summary>
        /// <param name="assemblyName"></param>
        public AssemblyAlreadyRegisteredException(string assemblyName)
            : base()
        {
            this.assembly = assemblyName;
            Data["HelpLink.EvtData1"] = assemblyName;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private AssemblyAlreadyRegisteredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            assembly = (string)info.GetValue("assembly", typeof(string));
        }

#if NETFRAMEWORK

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("assembly", this.assembly);
            base.GetObjectData(info, context);
        }
#endif
        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            assembly = string.Empty;
            SetHelpContext("AssemblyAlreadyRegisteredException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.AssemblyAlreadyRegistered;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                if (assembly != null)
                {
                    return ExceptionTemplatesSR.AssemblyAlreadyRegistered(assembly);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        string assembly = null;

        /// <summary>
        /// Offending Assembly
        /// </summary>
        public string Assembly { get { return assembly; } }
    }

    /// <summary>
    /// This exception gets thrown when operator's arguments have incompatible types
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionTypeMistmatchException : DmfException
    {

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionTypeMistmatchException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionTypeMistmatchException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionTypeMistmatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeLeft"></param>
        /// <param name="typeRight"></param>
        public ExpressionTypeMistmatchException(string typeLeft, string typeRight)
            : base()
        {
            this.typeLeft = typeLeft;
            this.typeRight = typeRight;
        }
        private ExpressionTypeMistmatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            typeLeft = info.GetString("typeLeft");
            typeRight = info.GetString("typeRight");
        }

#if NETFRAMEWORK
        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("typeLeft", this.typeLeft);
            info.AddValue("typeRight", this.typeRight);
            base.GetObjectData(info, context);
        }
#endif
        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ExpressionTypeMistmatch;
            }
        }
        
        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.ExpressionTypeMistmatch(typeLeft, typeRight);

            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            typeLeft = string.Empty;
            typeRight = string.Empty;
            SetHelpContext("ExpressionTypeMistmatchException");
        }

        string typeLeft;
        string typeRight;

        /// <summary>
        /// Left operand type name
        /// </summary>
        public string TypeLeft { get { return typeLeft; } }

        /// <summary>
        /// Right operand type name
        /// </summary>
        public string TypeRight { get { return typeRight; } }
    }

    /// <summary>
    /// This exception gets thrown when operator's arguments have incompatible types
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class OperatorNotApplicableException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public OperatorNotApplicableException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public OperatorNotApplicableException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public OperatorNotApplicableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="operatorName"></param>
        /// <param name="typeName"></param>
        public OperatorNotApplicableException(string operatorName, string typeName)
            : base()
        {
            this.operatorName = operatorName;
            this.type = typeName;

            Data["HelpLink.EvtData1"] = operatorName;
            Data["HelpLink.EvtData2"] = typeName;
        }
        private OperatorNotApplicableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            operatorName = info.GetString("operatorName");
            type = info.GetString("type");
        }

#if NETFRAMEWORK
        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("operatorName", this.operatorName);
            info.AddValue("type", this.type);
            base.GetObjectData(info, context);
        }
#endif
        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.OperatorNotApplicable;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.OperatorNotApplicable(operatorName, type);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            operatorName = string.Empty;
            type = string.Empty;
            SetHelpContext("OperatorNotApplicableException");
        }

        string operatorName;
        string type;

        /// <summary>
        /// Operator name
        /// </summary>
        public string Operator { get { return operatorName; } }

        /// <summary>
        /// Type name
        /// </summary>
        public string Type { get { return type; } }
    }

    /// <summary>
    /// This exception gets thrown when function receives argument of unexpected type
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionWrongArgumentTypeException : DmfException
    {

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongArgumentTypeException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongArgumentTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongArgumentTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="receivedType"></param>
        /// <param name="expectedType"></param>
        public FunctionWrongArgumentTypeException(string functionName, string receivedType, string expectedType)
            : base()
        {
            this.functionName = functionName;
            this.receivedType = receivedType;
            this.expectedType = expectedType;

            Data["HelpLink.EvtData1"] = functionName;
        }
        private FunctionWrongArgumentTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            functionName = info.GetString("functionName");
            receivedType = info.GetString("receivedType");
            expectedType = info.GetString("expectedType");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("functionName", this.functionName);
            info.AddValue("receivedType", this.receivedType);
            info.AddValue("expectedType", this.expectedType);
            
            base.GetObjectData(info, context);
        }
        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionWrongArgumentType;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.FunctionWrongArgumentType(functionName, receivedType, expectedType);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            functionName = string.Empty;
            receivedType = string.Empty;
            expectedType = string.Empty;
            SetHelpContext("FunctionWrongArgumentTypeException");
        }

        string functionName;
        string receivedType;
        string expectedType;

        /// <summary>
        /// Function Name
        /// </summary>
        public string FunctionName { get { return functionName; } }

        /// <summary>
        /// Received type name
        /// </summary>
        public string ReceivedType { get { return receivedType; } }

        /// <summary>
        /// Expected type name
        /// </summary>
        public string ExpectedType { get { return expectedType; } }
    }

    /// <summary>
    /// This exception gets thrown when function receives unexpected number of arguments
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionWrongArgumentsNumberException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongArgumentsNumberException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongArgumentsNumberException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongArgumentsNumberException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="receivedCount"></param>
        /// <param name="expectedCount"></param>
        public FunctionWrongArgumentsNumberException(string functionName, int receivedCount, int expectedCount)
            : base()
        {
            this.functionName = functionName;
            this.receivedCount = receivedCount;
            this.expectedCount = expectedCount;

            Data["HelpLink.EvtData1"] = functionName;
        }
        private FunctionWrongArgumentsNumberException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            functionName = info.GetString("functionName");
            receivedCount = info.GetInt32("receivedCount");
            expectedCount = info.GetInt32("expectedCount");

            Data["HelpLink.EvtData1"] = functionName;
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if(info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("functionName", this.functionName);
            info.AddValue("receivedCount", this.receivedCount);
            info.AddValue("expectedCount", this.expectedCount);
            
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionWrongArgumentsNumber;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.FunctionWrongArgumentsNumber(functionName, receivedCount, expectedCount);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            functionName = string.Empty;
            receivedCount = 0;
            expectedCount = 0;
            SetHelpContext("FunctionWrongArgumentsNumberException");
        }

        string functionName;
        int receivedCount;
        int expectedCount;

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get { return functionName; } }

        /// <summary>
        /// Received number of arguments
        /// </summary>
        public int ReceivedCount { get { return receivedCount; } }

        /// <summary>
        /// Expected number of arguments
        /// </summary>
        public int ExpectedCount { get { return expectedCount; } }
    }

    /// <summary>
    /// This exception gets thrown when Adapter Factory encounters already registered {interface; object} pair 
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class AdapterAlreadyExistsException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public AdapterAlreadyExistsException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public AdapterAlreadyExistsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public AdapterAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AdapterAlreadyExistsException(string interfaceName, string typeName)
            : base()
        {
            this.interfaceName = interfaceName;
            this.objectTypeName = typeName;
        }
        private AdapterAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            interfaceName = info.GetString("interfaceName");
            objectTypeName = info.GetString("objectTypeName");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("interfaceName", this.interfaceName);
            info.AddValue("objectTypeName", this.objectTypeName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.AdapterAlreadyExists;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.AdapterAlreadyExists(interfaceName, objectTypeName);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            interfaceName = string.Empty;
            objectTypeName = string.Empty;
            SetHelpContext("AdapterAlreadyExistsException");
        }

        string interfaceName;
        string objectTypeName;

        /// <summary>
        /// Interface name
        /// </summary>
        public string Interface { get { return interfaceName; } }

        /// <summary>
        /// Object type name
        /// </summary>
        public string ObjectType { get { return objectTypeName; } }
    }


    /// <summary>
    /// This exception gets thrown when Adapter Factory encounters adapter constructor accepting other than 1 argument
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class AdapterWrongNumberOfArgumentsException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public AdapterWrongNumberOfArgumentsException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public AdapterWrongNumberOfArgumentsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AdapterWrongNumberOfArgumentsException(string adapter)
            : base()
        {
            this.adapter = adapter;
            Data["HelpLink.EvtData1"] = adapter;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private AdapterWrongNumberOfArgumentsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            adapter = (string)info.GetValue("adapter", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        ///
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("adapter", this.adapter);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            adapter = string.Empty;
            SetHelpContext("AdapterWrongNumberOfArgumentsException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.AdapterWrongNumberOfArguments;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                if (adapter != null)
                {
                    return ExceptionTemplatesSR.AdapterHasTooManyArguments(adapter);

                }
                else
                {
                    return string.Empty;
                }
            }
        }

        string adapter = null;

        /// <summary>
        /// Offending Adapter name
        /// </summary>
        public string Adapter { get { return adapter; } }
    }

    /// <summary>
    /// This exception gets thrown when function expects live adapter but gets NULL
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class NullFacetException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public NullFacetException()
            : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NullFacetException(string facet)
            : base()
        {
            this.facet = facet;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public NullFacetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        /// <summary>
        /// Base constructor
        /// </summary>
        private NullFacetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            facet = (string)info.GetValue("facet", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("facet", this.facet);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            facet = String.Empty;
            SetHelpContext("NullFacetException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.NullFacet;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.NullFacet(facet);
            }
        }

        string facet;

        /// <summary>
        /// 
        /// </summary>
        public string Facet { get { return facet; } }
    }

    /// <summary>
    /// This exception gets thrown when ExpressionNode deserialize encounters unxpected xml-node
    /// </summary>
    /// 
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionSerializationException : DmfException
    {
        /// <summary>
        /// XmlReader action
        /// </summary>
        public enum ReaderActionType
        {
            /// <summary>
            /// No action/unknown 
            /// </summary>
            Undefined,
            /// <summary>
            /// Move to next element
            /// </summary>
            Move,
            /// <summary>
            /// Read immediate xml-node
            /// </summary>
            Read
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionSerializationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private ExpressionSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            actionType = (ReaderActionType)info.GetValue("actionType", typeof(ReaderActionType));
            typeRead = (string)info.GetValue("typeRead", typeof(string));
            nameRead = (string)info.GetValue("nameRead", typeof(string));
            typeExpected = (string)info.GetValue("typeExpected", typeof(string));
            nameExpected = (string)info.GetValue("nameExpected", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("actionType", this.actionType);
            info.AddValue("typeRead", this.typeRead);
            info.AddValue("nameRead", this.nameRead);
            info.AddValue("typeExpected", this.typeExpected);
            info.AddValue("nameExpected", this.nameExpected);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Constructor for Move exception
        /// </summary>
        /// <param name="nameRead"></param>
        /// <param name="nameExpected"></param>
        public ExpressionSerializationException(string nameRead, string nameExpected)
        {
            actionType = ReaderActionType.Move;
            this.nameRead = nameRead;
            this.nameExpected = nameExpected;
        }

        /// <summary>
        /// Constructor for Read exception
        /// </summary>
        /// <param name="typeRead"></param>
        /// <param name="nameRead"></param>
        /// <param name="typeExpected"></param>
        /// <param name="nameExpected"></param>
        public ExpressionSerializationException(string typeRead, string nameRead, string typeExpected, string nameExpected)
        {
            actionType = ReaderActionType.Move;
            this.typeRead = typeRead;
            this.typeExpected = typeExpected;
            this.nameRead = nameRead;
            this.nameExpected = nameExpected;
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            actionType = ReaderActionType.Undefined;
            typeExpected = String.Empty;
            nameExpected = String.Empty;
            typeRead = String.Empty;
            nameRead = String.Empty;
            SetHelpContext("ExpressionSerializationException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ExpressionSerialization;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                switch (actionType)
                {

                    case (ReaderActionType.Move):
                        if (String.IsNullOrEmpty(nameRead))
                        {
                            return ExceptionTemplatesSR.CannotMoveToElement;
                        }
                        else
                        {
                            return ExceptionTemplatesSR.UnexpectedElement(nameRead, nameExpected);
                        }
                    case (ReaderActionType.Read):
                        if (String.IsNullOrEmpty(nameExpected))
                        {
                            return ExceptionTemplatesSR.UnexpectedType(typeRead, nameRead, typeExpected);
                        }
                        else
                        {
                            return ExceptionTemplatesSR.UnexpectedName(typeRead, nameRead, typeExpected, nameExpected);
                        }
                    default:
                        return base.Message;
                }
            }
        }

        ReaderActionType actionType;
        string typeExpected;
        string nameExpected;
        string typeRead;
        string nameRead;

        /// <summary>
        /// XmlReader action (assumed from parameters)
        /// </summary>
        public ReaderActionType ActionType { get { return actionType; } }

        /// <summary>
        /// Expected xml-node type name
        /// </summary>
        public string TypeExpected { get { return typeExpected; } }

        /// <summary>
        /// Expected xml-node name (if supplied)
        /// </summary>
        public string NameExpected { get { return nameExpected; } }

        /// <summary>
        /// Read xml-node type name
        /// </summary>
        public string TypeRead { get { return TypeRead; } }

        /// <summary>
        /// Read xml-node name 
        /// </summary>
        public string NameRead { get { return NameRead; } }

    }

    /// <summary>
    /// This exception gets thrown when ExpressionNode deserialize encounters unxpected xml-node
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class TypeConversionException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public TypeConversionException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public TypeConversionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public TypeConversionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private TypeConversionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            inputString = (string)info.GetValue("inputString", typeof(string));
            typeName = (string)info.GetValue("typeName", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("inputString", this.inputString);
            info.AddValue("typeName", this.typeName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="typeName"></param>
        /// <param name="innerException"></param>
        public TypeConversionException(string inputString, string typeName, Exception innerException)
            : base(String.Empty, innerException)
        {
            this.inputString = inputString;
            this.typeName = typeName;
        }


        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            inputString = String.Empty;
            typeName = String.Empty;
            SetHelpContext("TypeConversionException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.TypeConversion;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.CannotConvertToType(inputString, typeName);

            }
        }

        string inputString;
        string typeName;

        /// <summary>
        /// input string
        /// </summary>
        public string InputString { get { return inputString; } }

        /// <summary>
        /// type name
        /// </summary>
        public string TypeName { get { return typeName; } }
    }

    /// <summary>
    /// This exception gets thrown when ExpressionNode deserialize
    /// encounters an unexpected xml-node
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class UnsupportedTypeException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedTypeException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private UnsupportedTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            node = (String)info.GetValue("node", typeof(string));
            typeName = (string)info.GetValue("typeName", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("node", this.node);
            info.AddValue("typeName", this.typeName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node"></param>
        /// <param name="typeName"></param>
        public UnsupportedTypeException(string node, string typeName)
            : base()
        {
            this.node = node;
            this.typeName = typeName;
        }


        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            typeName = String.Empty;
            SetHelpContext("UnsupportedTypeException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.UnsupportedType;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.UnsupportedConstantType(node, typeName);

            }
        }

        string node;
        string typeName;

        /// <summary>
        /// node type name
        /// </summary>
        public string NodeType { get { return node; } }
        /// <summary>
        /// type name
        /// </summary>
        public string TypeName { get { return typeName; } }
    }

    /// <summary>
    /// Run-time exception for ExpressionTree evaluation
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class BadExpressionTreeException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public BadExpressionTreeException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public BadExpressionTreeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private BadExpressionTreeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            reason = (string)info.GetValue("reason", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("reason", this.reason);
            base.GetObjectData(info, context);
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reason"></param>
        public BadExpressionTreeException(string reason)
            : base()
        {
            this.reason = reason;
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            reason = String.Empty;
            SetHelpContext("BadExpressionTreeException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.BadExpressionTree;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.BadExpressionTree(reason);

            }
        }

        string reason;

        /// <summary>
        /// Deatiled message for exception 
        /// </summary>
        public string Reason { get { return reason; } }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class UnsupportedObjectTypeException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedObjectTypeException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedObjectTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedObjectTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private UnsupportedObjectTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            typeName = (string)info.GetValue("typeName", typeof(string));
            host = (string)info.GetValue("host", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("typeName", this.typeName);
            info.AddValue("host", this.host);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="host"></param>
        public UnsupportedObjectTypeException(string typeName, string host)
            : base()
        {
            this.typeName = typeName;
            this.host = host;
        }


        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            typeName = String.Empty;
            host = String.Empty;
            SetHelpContext("UnsupportedObjectTypeException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.UnsupportedObjectType;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.UnsupportedObjectType(typeName, host);

            }
        }

        string typeName;
        string host;

        /// <summary>
        /// type name
        /// </summary>
        public string TypeName { get { return typeName; } }
        /// <summary>
        /// hosting object/type name
        /// </summary>
        public string Host { get { return host; } }
    }

    /// <summary>
    /// This exception gets thrown when we attempt to configure an expression
    /// that contains a non-configurable expression node.
    /// </summary>
    /// 
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeConfigurationException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionNodeConfigurationException()
            : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionNodeConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionNodeConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private ExpressionNodeConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("ExpressionNodeConfigurationException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ExpressionNodeNotConfigurable;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.ExpressionNodeNotConfigurableGeneric;

            }
        }
    }

    /// <summary>
    /// This exception gets thrown when we attempt to configure an expression
    /// that contains a non-configurable expression node.
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeNotConfigurableException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionNodeNotConfigurableException()
            : base()
        {
        }

        private string subtype = null;

        /// <summary>
        /// Node type (ex. "EQ")
        /// </summary>
        public string Subtype { get { return subtype; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="subtype"></param>
        public ExpressionNodeNotConfigurableException(string subtype)
            : base()
        {
            this.subtype = subtype;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionNodeNotConfigurableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        /// <summary>
        /// Base constructor
        /// </summary>
        private ExpressionNodeNotConfigurableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            subtype = (string)info.GetValue("subtype", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("subtype", this.subtype);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("ExpressionNodeNotConfigurableException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ExpressionNodeNotConfigurable;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return (null == subtype) ? ExceptionTemplatesSR.ExpressionNodeNotConfigurableGeneric : ExceptionTemplatesSR.ExpressionNodeNotConfigurable (subtype);
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when we attempt to configure an expression
    /// that contains a non-configurable expression operators.
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ExpressionNodeNotConfigurableOperatorException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionNodeNotConfigurableOperatorException()
            : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionNodeNotConfigurableOperatorException(string message)
            : base(message)
        {
        }

        private string propertyName = null;
        private string expression = null;

        /// <summary>
        /// The property name to be set
        /// </summary>
        public string PropertyName { get { return propertyName; } }
        /// <summary>
        /// Expression
        /// </summary>
        public string Expression { get { return expression; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionNodeNotConfigurableOperatorException(string propertyName,
            string expression)
            : base()
        {
            this.propertyName = propertyName;
            this.expression = expression;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ExpressionNodeNotConfigurableOperatorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        /// <summary>
        /// Base constructor
        /// </summary>
        private ExpressionNodeNotConfigurableOperatorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.propertyName = (string)info.GetValue("propertyName", typeof(string));
            this.expression = (string)info.GetValue ("expression", typeof (string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("propertyName", this.propertyName);
            info.AddValue ("expression", this.expression);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("ExpressionNodeNotConfigurableExceptionOperator");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ExpressionNodeNotConfigurableOperator;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.ExpressionNodeNotConfigurableOperators (propertyName, expression);

            }
        }
    }


    /// <summary>
    /// This exception gets thrown when we attempt to configure some read only properties
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class NonConfigurableReadOnlyPropertyException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public NonConfigurableReadOnlyPropertyException()
            : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NonConfigurableReadOnlyPropertyException(string propertyName)
            : base()
        {
            this.propertyName = propertyName;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public NonConfigurableReadOnlyPropertyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor with additional property name
        /// </summary>
        public NonConfigurableReadOnlyPropertyException(string message, string propertyName, Exception innerException)
            : base(message, innerException)
        {
            this.propertyName = propertyName;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private NonConfigurableReadOnlyPropertyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = (string)info.GetValue("propertyName", typeof(string));
        }
        private string propertyName = null;

        /// <summary>
        /// Property Name
        /// </summary>
        public string PropertyName
        {
            internal set
            {
                this.propertyName = value;
            }
            get
            {
                return propertyName;
            }
        }



        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("NonConfigurableReadOnlyProperty");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.NonConfigurableReadOnlyProperty;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {

                return ExceptionTemplatesSR.NonConfigurableReadOnlyProperty(propertyName);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ConversionNotSupportedException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public ConversionNotSupportedException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ConversionNotSupportedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ConversionNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private ConversionNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            typeName = (string)info.GetValue("typeName", typeof(string));
            host = (string)info.GetValue("host", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("typeName", this.typeName);
            info.AddValue("host", this.host);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="host"></param>
        public ConversionNotSupportedException(string host, string typeName)
            : base()
        {
            this.typeName = typeName;
            this.host = host;
        }


        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            typeName = String.Empty;
            host = String.Empty;
            SetHelpContext("ConversionNotSupportedException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ConversionNotSupported;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.ConversionNotSupported(host, typeName);
            }
        }

        string typeName;
        string host;

        /// <summary>
        /// type name
        /// </summary>
        public string TypeName { get { return typeName; } }
        /// <summary>
        /// hosting object/type name
        /// </summary>
        public string Host { get { return host; } }
    }

    /// <summary>
    /// This exception gets thrown when operator's arguments have incompatible types
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class InvalidOperandException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public InvalidOperandException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public InvalidOperandException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public InvalidOperandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeType"></param>
        /// <param name="operand"></param>
        public InvalidOperandException(string nodeType, string operand)
            : base()
        {
            this.nodeType = nodeType;
            this.operand = operand;

            Data["HelpLink.EvtData1"] = nodeType;
            Data["HelpLink.EvtData2"] = operand;
        }
        private InvalidOperandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            nodeType = info.GetString("nodeType");
            operand = info.GetString("operand");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("nodeType", this.nodeType);
            info.AddValue("operand", this.operand);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.InvalidOperand;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.InvalidOperand(nodeType, operand);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            nodeType = string.Empty;
            operand = string.Empty;
            SetHelpContext("InvalidOperandException");
        }

        string nodeType;
        string operand;

        /// <summary>
        /// NodeType name
        /// </summary>
        public string NodeType { get { return nodeType; } }

        /// <summary>
        /// Type name
        /// </summary>
        public string Type { get { return operand; } }
    }

    /// <summary>
    /// IN Operator must have right operand List
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class InvalidInOperatorException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public InvalidInOperatorException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public InvalidInOperatorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public InvalidInOperatorException(string opType)
            : base()
        {
            this.opType = opType;

            Data["HelpLink.EvtData1"] = opType;
        }


        /// <summary>
        /// Base constructor
        /// </summary>
        private InvalidInOperatorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            opType = info.GetString("opType");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("opType", this.opType);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            opType = String.Empty;
            SetHelpContext("InvalidInOperatorException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.InvalidInOperator;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.InvalidInOperator(opType);

            }
        }

        string opType;

        /// <summary>
        /// Operator Type
        /// </summary>
        private string operatorTypeValue;

        /// <summary>
        /// 
        /// </summary>
        public string OperatorType
        {
            get
            {
                return operatorTypeValue;
            }
            set
            {
                operatorTypeValue = value;
            }
        }
    }

    /// <summary>
    /// Generic validation exception
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ObjectValidationException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public ObjectValidationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ObjectValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ObjectValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="objectName"></param>
        /// <param name="innerException"></param>
        public ObjectValidationException(string objectType, string objectName, Exception innerException)
            : base(String.Empty, innerException)
        {
            this.objectType = objectType;
            this.objectName = objectName;

            Data["HelpLink.EvtData1"] = objectType;
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="objectName"></param>
        public ObjectValidationException(string objectType, string objectName)
            : base()
        {
            this.objectType = objectType;
            this.objectName = objectName;

            Data["HelpLink.EvtData1"] = objectType;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private ObjectValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.objectType = info.GetString("objectType");
            this.objectName = info.GetString("objectName");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("objectType", this.objectType);
            info.AddValue("objectName", this.objectName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ObjectValidation;
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            this.objectName = String.Empty;
            this.objectType = String.Empty;
            SetHelpContext("ObjectValidationException");
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return String.Format("{0}", ExceptionTemplatesSR.ValidationFailed(objectType, objectName));

            }
        }

        /// <summary>
        /// Type of validated object
        /// </summary>
        private string objectType;
        /// <summary>
        /// Name of validated object
        /// </summary>
        private string objectName;

        /// <summary>
        /// Type of validated object
        /// </summary>
        public string ObjectType { get { return objectType; } }
        /// <summary>
        /// Name of validated object
        /// </summary>
        public string ObjectName { get { return objectName; } }
    }

    /// <summary>
    /// Object already exists (attempt to create a duplicate)
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ObjectAlreadyExistsException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public ObjectAlreadyExistsException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ObjectAlreadyExistsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ObjectAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="objectName"></param>
        public ObjectAlreadyExistsException(string objectType, string objectName)
            : base()
        {
            this.objectType = objectType;
            this.objectName = objectName;

            Data["HelpLink.EvtData1"] = objectType;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private ObjectAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.objectType = info.GetString("objectType");
            this.objectName = info.GetString("objectName");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("objectType", this.objectType);
            info.AddValue("objectName", this.objectName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ObjectAlreadyExists;
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            this.objectName = String.Empty;
            this.objectType = String.Empty;
            SetHelpContext("ObjectAlreadyExistsException");
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return String.Format("{0}", ExceptionTemplatesSR.ObjectAlreadyExists(objectType, objectName));
            }
        }

        /// <summary>
        /// Type of validated object
        /// </summary>
        private string objectType;
        /// <summary>
        /// Name of validated object
        /// </summary>
        private string objectName;

        /// <summary>
        /// Type of validated object
        /// </summary>
        public string ObjectType { get { return objectType; } }
        /// <summary>
        /// Name of validated object
        /// </summary>
        public string ObjectName { get { return objectName; } }
    }

    /// <summary>
    /// Object doesn't exists (attempt to reference non-existent object)
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class MissingObjectException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingObjectException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingObjectException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingObjectException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="objectName"></param>
        public MissingObjectException(string objectType, string objectName)
            : base()
        {
            this.objectType = objectType;
            this.objectName = objectName;

            Data["HelpLink.EvtData1"] = objectType;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private MissingObjectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.objectType = info.GetString("objectType");
            this.objectName = info.GetString("objectName");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("objectType", this.objectType);
            info.AddValue("objectName", this.objectName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.MissingObject;
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            this.objectName = String.Empty;
            this.objectType = String.Empty;
            SetHelpContext("MissingObjectException");
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return String.Format("{0}", ExceptionTemplatesSR.ObjectDoesntExist(objectType, objectName));
            }
        }

        /// <summary>
        /// Type of validated object
        /// </summary>
        private string objectType;
        /// <summary>
        /// Name of validated object
        /// </summary>
        private string objectName;

        /// <summary>
        /// Type of validated object
        /// </summary>
        public string ObjectType { get { return objectType; } }
        /// <summary>
        /// Name of validated object
        /// </summary>
        public string ObjectName { get { return objectName; } }
    }

    /// <summary>
    /// This exception gets thrown when object cannot be created or modified
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class ConflictingPropertyValuesException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public ConflictingPropertyValuesException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ConflictingPropertyValuesException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public ConflictingPropertyValuesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="type1"></param>
        /// <param name="name1"></param>
        /// <param name="type2"></param>
        /// <param name="name2"></param>
        public ConflictingPropertyValuesException(string mode, string type1, string name1, string type2, string name2)
            : base()
        {
            this.mode = mode;
            this.type1 = type1;
            this.name1 = name1;
            this.type2 = type2;
            this.name2 = name2;

            Data["HelpLink.EvtData2"] = type1;
            Data["HelpLink.EvtData3"] = type2;
        }
        private ConflictingPropertyValuesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.mode = info.GetString("mode");
            this.name1 = info.GetString("type1");
            this.type2 = info.GetString("name1");
            this.name2 = info.GetString("type2");
            this.name2 = info.GetString("name2");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("mode", this.mode);
            info.AddValue("type1", this.type1);
            info.AddValue("name1", this.name1);
            info.AddValue("type2", this.type2);
            info.AddValue("name2", this.name2);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.ConflictingPropertyValues;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return base.Message + "\n" + ExceptionTemplatesSR.NotSupported(type1, name1, type2, name2);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            type1 = String.Empty;
            name1 = String.Empty;
            type2 = String.Empty;
            name2 = String.Empty;
            SetHelpContext("ConflictingPropertyValuesException");
        }

        string type1;
        string name1;
        string type2;
        string name2;
        string mode;

        /// <summary>
        /// 
        /// </summary>
        public string Mode { get { return mode; } }

        /// <summary>
        /// 
        /// </summary>
        public string Type1 { get { return type1; } }
        /// <summary>
        /// 
        /// </summary>
        public string Name1 { get { return name1; } }
        /// <summary>
        /// 
        /// </summary>
        public string Type2 { get { return type2; } }
        /// <summary>
        /// 
        /// </summary>
        public string Name2 { get { return name2; } }
    }

    /// <summary>
    /// This exception gets thrown when a policy Job Schedule GUID is empty but the execution mode is not None
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class MissingJobScheduleException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingJobScheduleException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingJobScheduleException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingJobScheduleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private MissingJobScheduleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("MissingJobScheduleException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.MissingJobSchedule;
            }
        }
    }

    /// <summary>
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyEvaluationException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public PolicyEvaluationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public PolicyEvaluationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an Exception for repeated attempt to register the same assembly
        /// </summary>
        public PolicyEvaluationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private PolicyEvaluationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("PolicyEvaluationException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.PolicyEvaluation;
            }
        }
    }

    /// <summary>
    /// Exception that gets thrown when EVENTDATA blob is malformed
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class BadEventDataException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public BadEventDataException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public BadEventDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an Exception for repeated attempt to register the same assembly
        /// </summary>
        public BadEventDataException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private BadEventDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("PolicyEvaluationException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.BadEventData;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when an operation has failed
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FailedOperationException : DmfException
    {
        /// <summary>
        /// ctor
        /// </summary>
        public FailedOperationException()
            : base()
        {
            SetHelpContext("FailedOperationExceptionText");
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="message"></param>
        public FailedOperationException(string message)
            : base(message)
        {
            SetHelpContext("FailedOperationExceptionText");
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public FailedOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetHelpContext("FailedOperationExceptionText");
        }

        private FailedOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.operation = info.GetString("operation");
            this.failedObjectName = info.GetString("failedObjectName");
            this.failedObjectType = info.GetString("failedObjectType");
            SetHelpContext("FailedOperationExceptionText");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("operation", this.operation);
            info.AddValue("failedObjectName", this.failedObjectName);
            info.AddValue("failedObjectType", this.failedObjectType);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="failedObjectName"></param>
        /// <param name="failedObjectType"></param>
        /// <param name="innerException"></param>
#if NETFRAMEWORK
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.SqlServer.Management.Smo.SmoException.#ctor(System.String,System.Exception)")]
#endif
        public FailedOperationException(
            string operation, string failedObjectName, string failedObjectType, Exception innerException)
            : base("", innerException)
        {
            this.operation = operation;
            this.failedObjectName = failedObjectName;
            this.failedObjectType = failedObjectType;

            SetHelpContext("FailedOperationExceptionText");
            Data.Add("HelpLink.EvtData1", operation);
            if (null != failedObjectType)
            {
                Data.Add("HelpLink.EvtData2", failedObjectType);
            }
        }

        string operation = string.Empty;
        /// <summary>
        /// Operation that failed.
        /// </summary>
        public string Operation
        {
            get { return operation; }
            set { operation = value; }
        }

        string failedObjectName = string.Empty;
        /// <summary>
        /// Name of the object that failed.
        /// </summary>
        public string FailedObjectName
        {
            get { return failedObjectName; }
            set { failedObjectName = value; }
        }

        string failedObjectType = string.Empty;
        /// <summary>
        /// Type of the object that failed.
        /// </summary>
        public string FailedObjectType
        {
            get { return failedObjectType; }
            set { failedObjectType = value; }
        }

        /// <summary>
        /// Message
        /// </summary>
        /// <value></value>
        public override string Message
        {
            get
            {

                //if we haven't been already set a message and we have enough data to build one
                if (base.Message.Length <= 0 &&
                    null != operation &&
                    operation.Length > 0 &&
                    failedObjectName.Length > 0)
                {
                    return ExceptionTemplatesSR.FailedOperation(
                        operation, failedObjectType, failedObjectName);
                }
                else
                    return base.Message;
            }
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FailedOperation;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when we attempt to configure some read only properties
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class MissingPropertyException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MissingPropertyException()
            : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MissingPropertyException(string propertyName)
            : base()
        {
            this.propertyName = propertyName;
        }

        private string propertyName = null;

        /// <summary>
        /// Property Name
        /// </summary>
        public string PropertyName
        {
            internal set
            {
                this.propertyName = value;
            }
            get
            {
                return propertyName;
            }
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingPropertyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        /// <summary>
        /// Base constructor
        /// </summary>
        private MissingPropertyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = (string)info.GetValue("propertyName", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("MissingProperty");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.MissingProperty;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.MissingProperty(propertyName);
            }
        }
    }


    /// <summary>
    /// This exception gets thrown when we attempt to retrieve some properties which do not 
    /// apply to the instance
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class NonRetrievablePropertyException : DmfException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public NonRetrievablePropertyException()
            : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NonRetrievablePropertyException(string propertyName)
            : base()
        {
            this.propertyName = propertyName;
        }

        private string propertyName = null;

        /// <summary>
        /// Property Name
        /// </summary>
        public string PropertyName
        {
            internal set
            {
                this.propertyName = value;
            }
            get
            {
                return propertyName;
            }
        }

        /// <summary>
        /// Base constructor
        /// </summary>

        public NonRetrievablePropertyException(string propertyName, Exception innerException)
            : base(ExceptionTemplatesSR.NonRetrievableProperty(propertyName), innerException)
        {
            this.PropertyName = propertyName;            
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private NonRetrievablePropertyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = (string)info.GetValue("propertyName", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("NonRetrievableProperty");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.NonRetrievableProperty;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.NonRetrievableProperty(propertyName);

            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class MissingTypeFacetAssociationException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingTypeFacetAssociationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingTypeFacetAssociationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public MissingTypeFacetAssociationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private MissingTypeFacetAssociationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            typeName = (string)info.GetValue("typeName", typeof(string));
            facet = (string)info.GetValue("facet", typeof(string));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("typeName", this.typeName);
            info.AddValue("facet", this.facet);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="facet"></param>
        public MissingTypeFacetAssociationException(string typeName, string facet)
            : base()
        {
            this.typeName = typeName;
            this.facet = facet;
        }


        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            typeName = String.Empty;
            facet = String.Empty;
            SetHelpContext("MissingTypeFacetAssociationException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.MissingTypeFacetAssociation;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {

                return ExceptionTemplatesSR.MissingTypeFacetAssociation(typeName, facet);

            }
        }

        string typeName;
        string facet;

        /// <summary>
        /// type name
        /// </summary>
        public string TypeName { get { return typeName; } }
        /// <summary>
        /// hosting object/type name
        /// </summary>
        public string Facet { get { return facet; } }
    }

    /// <summary>
    /// This exception gets thrown when ExecuteSQLScalar is attempted against a non-SMO target
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionNotASmoObjectException : DmfException
    {

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionNotASmoObjectException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionNotASmoObjectException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionNotASmoObjectException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="targetType"></param>
        public FunctionNotASmoObjectException(string functionName, string targetType)
            : base()
        {
            this.functionName = functionName;
            this.targetType = targetType;

            Data["HelpLink.EvtData1"] = functionName;
        }
        private FunctionNotASmoObjectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            functionName = info.GetString("functionName");
            targetType = info.GetString("targetType");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("functionName", this.functionName);
            info.AddValue("targetType", this.targetType);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionNotASmoObject;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.FunctionNotASmoObject(functionName, targetType);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            functionName = string.Empty;
            targetType = string.Empty;
            SetHelpContext("FunctionNotASmoObjectException");
        }

        string functionName;
        string targetType;

        /// <summary>
        /// Function Name
        /// </summary>
        public string FunctionName { get { return functionName; } }

        /// <summary>
        /// Received type name
        /// </summary>
        public string TargetType { get { return targetType; } }

    }



    /// <summary>
    /// This exception gets thrown when the ExecuteSQL scalar function can't find a server
    /// to send its query to
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionNoServerException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionNoServerException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionNoServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an Exception for repeated attempt to register the same assembly
        /// </summary>
        public FunctionNoServerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private FunctionNoServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("FunctionNoServerException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionNoServer;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when function specifies a return value of unexpected type
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionWrongReturnTypeException : DmfException
    {

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongReturnTypeException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongReturnTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionWrongReturnTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="receivedType"></param>
        /// <param name="expectedType"></param>
        public FunctionWrongReturnTypeException(string functionName, string receivedType, string expectedType)
            : base()
        {
            this.functionName = functionName;
            this.receivedType = receivedType;
            this.expectedType = expectedType;

            Data["HelpLink.EvtData1"] = functionName;
        }
        private FunctionWrongReturnTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            functionName = info.GetString("functionName");
            receivedType = info.GetString("receivedType");
            expectedType = info.GetString("expectedType");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                info.AddValue("functionName", this.functionName);
                info.AddValue("receivedType", this.receivedType);
                info.AddValue("expectedType", this.expectedType);
            }
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionWrongReturnType;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.FunctionWrongReturnType(functionName, receivedType, expectedType);

            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            functionName = string.Empty;
            receivedType = string.Empty;
            expectedType = string.Empty;
            SetHelpContext("FunctionWrongReturnTypeException");
        }

        string functionName;
        string receivedType;
        string expectedType;

        /// <summary>
        /// Function Name
        /// </summary>
        public string FunctionName { get { return functionName; } }

        /// <summary>
        /// Received type name
        /// </summary>
        public string ReceivedType { get { return receivedType; } }

        /// <summary>
        /// Expected type name
        /// </summary>
        public string ExpectedType { get { return expectedType; } }
    }

    /// <summary>
    /// This exception gets thrown when a date function receives a bad date part string
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionBadDatePartException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionBadDatePartException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionBadDatePartException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an Exception for repeated attempt to register the same assembly
        /// </summary>
        public FunctionBadDatePartException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private FunctionBadDatePartException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("FunctionBadDatePartException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionBadDatePart;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when the ExecuteSql or ExecuteWql scalar functions execute
    /// queries that return more than one column
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FunctionTooManyColumnsException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionTooManyColumnsException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FunctionTooManyColumnsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an Exception for repeated attempt to register the same assembly
        /// </summary>
        public FunctionTooManyColumnsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private FunctionTooManyColumnsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            SetHelpContext("FunctionTooManyColumnsException");
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.FunctionTooManyColumns;
            }
        }
    }


    /// <summary>
    /// This exception gets thrown when value specified for a string property is longer than maximum length for that property
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class StringPropertyTooLongException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public StringPropertyTooLongException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public StringPropertyTooLongException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public StringPropertyTooLongException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        /// <param name="currentLength"></param>
        public StringPropertyTooLongException(string propertyName, int maxLength, int currentLength)
            : base()
        {
            this.propertyName = propertyName;
            this.maxLength = maxLength;
            this.currentLength = currentLength;

            Data["HelpLink.EvtData1"] = propertyName;
            Data["HelpLink.EvtData2"] = maxLength;
            Data["HelpLink.EvtData3"] = currentLength;
        }
        private StringPropertyTooLongException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
            maxLength = info.GetInt32("maxLength");
            currentLength = info.GetInt32("currentLength");
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            info.AddValue("maxLength", this.maxLength);
            info.AddValue("currentLength", this.currentLength);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.StringPropertyTooLong;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.StringPropertyTooLong(propertyName, maxLength, currentLength);
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            propertyName = string.Empty;
            maxLength = 0;
            currentLength = 0;
            SetHelpContext("StringPropertyTooLongException");
        }

        string propertyName;
        int maxLength;
        int currentLength;

        /// <summary>
        /// NodeType name
        /// </summary>
        public string PropertyName { get { return propertyName; } }

        /// <summary>
        /// Type name
        /// </summary>
        public int MaxLength { get { return maxLength; } }

        /// <summary>
        /// Length of current property value that is too long
        /// </summary>
        public int CurrentLength { get { return currentLength; } }
    }

    /// <summary>
    /// This exception is thrown when the TargetSets collection created automatically by the ObjectSet has been tampered with.
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class TargetSetCountMismatchException : DmfException
    {
        private string objectSetName;
        private string facetName;

        /// <summary>
        /// 
        /// </summary>
        public TargetSetCountMismatchException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public TargetSetCountMismatchException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public TargetSetCountMismatchException (string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectSetName"></param>
        /// <param name="facetName"></param>
        public TargetSetCountMismatchException(string objectSetName, string facetName) : base()
        {
            this.objectSetName = objectSetName;
            this.facetName = facetName;

            Data["HelpLink.EvtData1"] = objectSetName;
            Data["HelpLink.EvtData2"] = facetName;
        }
        private TargetSetCountMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            objectSetName = info.GetString("objectSetName");
            facetName = info.GetString("facetName");
        }
        /// <summary>
        /// 
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.TargetSetCountMismatch(objectSetName, facetName);
            }
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("objectSetName", this.objectSetName);
            info.AddValue("facetName", this.facetName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.TargetSetCountMismatch;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ObjectSetName { get { return objectSetName; } }
        /// <summary>
        /// 
        /// </summary>
        public string FacetName { get { return facetName; } }
    }

    /// <summary>
    /// This exception is thrown when the TargetSets collection created automatically by the ObjectSet has been tampered with and an
    /// unsupported target type has been defined for the given facet
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class UnsupportedTargetSetForFacetException : DmfException
    {
        private string targetSetSkeleton;
        private string objectSetName;
        private string facetName;

        /// <summary>
        /// 
        /// </summary>
        public UnsupportedTargetSetForFacetException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedTargetSetForFacetException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public UnsupportedTargetSetForFacetException (string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetSetSkeleton"></param>
        /// <param name="objectSetName"></param>
        /// <param name="facetName"></param>
        public UnsupportedTargetSetForFacetException(string targetSetSkeleton, string objectSetName, string facetName)
            : base()
        {
            this.targetSetSkeleton = targetSetSkeleton;
            this.objectSetName = objectSetName;
            this.facetName = facetName;

            Data["HelpLink.EvtData1"] = targetSetSkeleton;
            Data["HelpLink.EvtData2"] = objectSetName;
            Data["HelpLink.EvtData3"] = facetName;
        }
        private UnsupportedTargetSetForFacetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            targetSetSkeleton = info.GetString("targetSetSkeleton");
            objectSetName = info.GetString("objectSetName");
            facetName = info.GetString("facetName");
        }
        /// <summary>
        /// 
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.UnsupportedTargetSetForFacet(targetSetSkeleton, objectSetName, facetName);
            }
        }
        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("targetSetSkeleton", this.targetSetSkeleton);
            info.AddValue("objectSetName", this.objectSetName);
            info.AddValue("facetName", this.facetName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.UnsupportedTargetSetForFacet;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TargetSetSkeleton { get { return targetSetSkeleton; } }

        /// <summary>
        /// 
        /// </summary>
        public string ObjectSetName { get { return objectSetName; } }
        /// <summary>
        /// 
        /// </summary>
        public string FacetName { get { return facetName; } }
    }

    /// <summary>
    /// This exception is thrown when no Target Sets have been enabled for a given object set
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class NoTargetSetEnabledException : DmfException
    {
        private string objectSetName;

        /// <summary>
        /// 
        /// </summary>
        public NoTargetSetEnabledException()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectSetName"></param>
        public NoTargetSetEnabledException(string objectSetName)
            : base()
        {
            this.objectSetName = objectSetName;

            Data["HelpLink.EvtData1"] = objectSetName;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public NoTargetSetEnabledException (string message, Exception innerException)
            : base(message, innerException)
        {
        }
        private NoTargetSetEnabledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            objectSetName = info.GetString("objectSetName");
        }
        /// <summary>
        /// 
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.NoTargetSetEnabled(objectSetName);
            }
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("objectSetName", this.objectSetName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.NoTargetSetEnabled;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ObjectSetName { get { return objectSetName; } }
    }

    /// <summary>
    /// base class for generic RestartPendingException
    /// we need to create this so we can catch all exceptions of this form 
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public class RestartPendingException : DmfException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public RestartPendingException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public RestartPendingException (string message)
            : base (message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public RestartPendingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected RestartPendingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }

    /// <summary>
    /// This exception gets thrown when value specified for a string property is longer than maximum length for that property
    /// </summary>
    [Serializable]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class RestartPendingException<T> : RestartPendingException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public RestartPendingException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public RestartPendingException (string message)
            : base (message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public RestartPendingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="configValue"></param>
        /// <param name="runValue"></param>
        public RestartPendingException(string propertyName, T configValue, T runValue)
            : base()
        {
            this.propertyName = propertyName;
            this.configValue = configValue;
            this.runValue = runValue;

            Data["HelpLink.EvtData1"] = propertyName;
            Data["HelpLink.EvtData2"] = configValue;
            Data["HelpLink.EvtData3"] = runValue;
        }
        private RestartPendingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
            configValue = (T)info.GetValue("configValue", typeof(T));
            runValue = (T)info.GetValue("runValue", typeof(T));
        }

        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NETFRAMEWORK
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            info.AddValue("configValue", this.configValue);
            info.AddValue("runValue", this.runValue);

            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Exception Type
        /// </summary>
        public override DmfExceptionType DmfExceptionType
        {
            get
            {
                return DmfExceptionType.RestartPending;
            }
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplatesSR.ConfigValueMismatch(
                    propertyName, configValue.ToString(), runValue.ToString());
            }
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected override void Init()
        {
            base.Init();
            propertyName = string.Empty;
            SetHelpContext("StringPropertyTooLongException");
        }

        T configValue;

        /// <summary>
        /// 
        /// </summary>
        public T ConfigValue
        {
            get { return configValue; }
            set { configValue = value; }
        }

        T runValue;

        /// <summary>
        /// 
        /// </summary>
        public T RunValue
        {
            get { return runValue; }
            set { runValue = value; }
        }

        string propertyName;

        /// <summary>
        /// NodeType name
        /// </summary>
        public string PropertyName { get { return propertyName; } }

    }
}

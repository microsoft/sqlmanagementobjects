// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This is the base class for all SFC exceptions. Never throw this exception directly.
    /// </summary>
    [Serializable]
    public class SfcException : SqlServerManagementException
    {
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SfcException() : base() 
        {
            Init();
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SfcException(string message) : base(message) 
        {
            Init();
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SfcException(string message, Exception innerException) : base(message, innerException) 
        {
            Init();
        }

        protected SfcException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        
        private void Init()
        {
            Data.Add("HelpLink.ProdVer", ProdVer);
        }

        static SfcException()
        {
            prodVer = string.Empty;

            object[] attribs = SmoManagementUtil.GetExecutingAssembly().GetCustomAttributes(true);
            if( null != attribs )
            {
                foreach( object o in attribs )
                {
                    if( o is AssemblyFileVersionAttribute )
                    {
                        prodVer = ((AssemblyFileVersionAttribute)o).Version;
                        break;
                    }
                }
            }
        }

        private static string prodVer;
        protected static string ProdVer
        {
            get 
            {
                return prodVer;
            }
        }

        internal protected SfcException SetHelpContext(string resource)
        {
            // TODO: Need to figure out whether this is the right thing to do as we do not use
            // the same exception templates that SMO uses.
            Data["HelpLink.EvtSrc"] = ("Microsoft.SqlServer.Management.Sdk.Sfc.ExceptionTemplates." + resource);
            
            return this;
        }

        // TODO: We need to decide whether we keep this alive for SFC.
        //
        // Ideally this code would be shared with SmoException or pushed down into SqlServerManagementException
        //
        // Will output a link to the help web site
        // http://www.microsoft.com/products/ee/transform.aspx?ProdName=Microsoft%20SQL%20Server&ProdVer=09.00.0000.00&EvtSrc=MSSQLServer&EvtID=15401
        public override string HelpLink
        {
            get 
            {
                StringBuilder link = new StringBuilder();
                link.Append(Data["HelpLink.BaseHelpUrl"] as string);
                link.Append("?");
                link.AppendFormat("ProdName={0}", Data["HelpLink.ProdName"] as string);
                
                if( Data.Contains("HelpLink.ProdVer"))
                {
                    link.AppendFormat("&ProdVer={0}", Data["HelpLink.ProdVer"] as string);
                }

                if ( Data.Contains("HelpLink.EvtSrc"))
                {
                    link.AppendFormat("&EvtSrc={0}", Data["HelpLink.EvtSrc"] as string);
                }

                if ( Data.Contains("HelpLink.EvtData1") )
                {
                    link.AppendFormat("&EvtID={0}", Data["HelpLink.EvtData1"] as string);
                    for( int i = 2; i < 10; i++)
                    {
                        if( Data.Contains("HelpLink.EvtData" + i))
                        {
                            link.Append("+");
                            link.Append(Data["HelpLink.EvtData" + i] as string);
                        }
                        else
                        {
                            break;
                        }
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
    /// This exception gets thrown when a property is not set but it
    /// is required for the current operation.
    /// </summary>
    [Serializable]

    public sealed class SfcPropertyNotSetException : SfcException
    {
        string propertyName = null;
        
        public SfcPropertyNotSetException() : this(string.Empty, null)
        {
        }

        public SfcPropertyNotSetException(string propertyName) : this(propertyName, null) 
        {            
        }

        internal SfcPropertyNotSetException(string propertyName, Exception innerException) : base(string.Empty, innerException)
        {
            this.propertyName = propertyName;
            Init();
        }
        private SfcPropertyNotSetException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
            propertyName = (string)info.GetValue("propertyName", typeof(string));
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
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }
#endif

        public override string Message 
        {
            get 
            {
                return SfcStrings.PropertyNotSet(propertyName);
            }
        }
        
        private void Init()
        {
            SetHelpContext("SfcPropertyNotSetException");
        }
    }

    /// <summary>
    /// This exception gets thrown when a property is not set but it
    /// is required for the current operation.
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidKeyException: SfcException
    {
        private string keyName = null;

        public SfcInvalidKeyException()
            : base()
        {
            this.keyName= string.Empty;
        }

        public SfcInvalidKeyException(string keyName)
            : base()
        {
            this.keyName = keyName ;
        }

        public SfcInvalidKeyException(string keyName, Exception innerException)
            : base(SfcStrings.InvalidKey(keyName), innerException)
        {
            this.keyName = keyName;
        }

        private SfcInvalidKeyException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
            keyName = (string)info.GetValue("keyName", typeof(string));
        }
        
#if NETFRAMEWORK
        /// <summary>
        /// Serialization helper
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("keyName", this.keyName);
            base.GetObjectData(info, context);
        }
#endif

        public override string Message
        {
            get
            {
                return SfcStrings.InvalidKey(keyName);
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when a key chain is set on an object
    /// but the parent is already set to a different parent than the keychain parent
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidKeyChainException : SfcException
    {
        public SfcInvalidKeyChainException()
            : base()
        {
        }

        public SfcInvalidKeyChainException(string message)
            : base(message)
        {
        }


        public SfcInvalidKeyChainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SfcInvalidKeyChainException(Exception innerException)
            : base(SfcStrings.InvalidKeyChain, innerException)
        {
        }

        
        private SfcInvalidKeyChainException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
        
        public override string Message
        {
            get
            {
                return SfcStrings.InvalidKeyChain;
            }
        }
    }

    /// <summary>
    /// This exception is thrown when an invalid rename is attempted.
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidRenameException : SfcException
    {
        public SfcInvalidRenameException()
            : base()
        {
        }

        public SfcInvalidRenameException(string message)
            : base(message)
        {
        }

        public SfcInvalidRenameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidRenameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when an invalid move is attempted.
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidMoveException : SfcException
    {
        public SfcInvalidMoveException()
            : base()
        {
        }

        public SfcInvalidMoveException(string message)
            : base(message)
        {
        }

        public SfcInvalidMoveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidMoveException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception gets thrown when a property is not set but it
    /// is required for the current operation.
    /// </summary>
    [Serializable]

    public sealed class SfcObjectInitializationException: SfcException
    {
        private string objName = null;

        public SfcObjectInitializationException()
            : base()
        {
            this.objName= string.Empty;
        }

        public SfcObjectInitializationException(string keyName)
            : base()
        {
            this.objName = keyName ;
        }

        public SfcObjectInitializationException(string keyName, Exception innerException)
            : base(SfcStrings.SfcObjectInitFailed(keyName), innerException)
        {
            this.objName = keyName;
        }

        private SfcObjectInitializationException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
            objName = (string)info.GetValue("objName", typeof(string));
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
            info.AddValue("objName", this.objName);
            base.GetObjectData(info, context);
        }
#endif

        public override string Message
        {
            get
            {
                return SfcStrings.SfcObjectInitFailed(objName);
            }
        }
    }


    /// <summary>
    /// This exception is thrown any time an invalid argument is passed into an Sfc class or service.
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidArgumentException : SfcException
    {
        public SfcInvalidArgumentException()
            : base()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public SfcInvalidArgumentException(String message)
            : base(message)
        {
        }

        public SfcInvalidArgumentException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidArgumentException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

    }

    /// <summary>
    /// This exception is thrown any time a stream that is closed or in an invalid error state is passed into an Sfc class or service.
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidStreamException : SfcException
    {
        public SfcInvalidStreamException()
            : base()
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        public SfcInvalidStreamException(String message)
            : base(message)
        {
        }

        public SfcInvalidStreamException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown during serialization if the output generated is invalid.
    /// </summary>
    [Serializable]

    public sealed class SfcSerializationException : SfcException
    {
        /// <summary>
        /// TBD
        /// </summary>
        public SfcSerializationException()
            : base()
        {
        }

        public SfcSerializationException(string message)
            : base(message)
        {
        }

        public SfcSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SfcSerializationException(Exception innerException)
            : base(SfcStrings.SfcInvalidSerialization, innerException)
        {
        }

        private SfcSerializationException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown during serialization if the output generated is invalid.
    /// </summary>
    [Serializable]

    public sealed class SfcNonSerializableTypeException: SfcException
    {

        public SfcNonSerializableTypeException()
            : base()
        {
        }

        public SfcNonSerializableTypeException(string message)
            : base(message)
        {
        }

        public SfcNonSerializableTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcNonSerializableTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }


    /// <summary>
    /// This exception is thrown during deserialization if the Xml contains an unregistered domain
    /// </summary>
    [Serializable]

    public sealed class SfcUnregisteredXmlDomainException : SfcException
    {
        public SfcUnregisteredXmlDomainException()
            : base()
        {
        }

        public SfcUnregisteredXmlDomainException(string message)
            : base(message)
        {
        }

        public SfcUnregisteredXmlDomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcUnregisteredXmlDomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown during deserialization if the Xml contains an unregistered type within a domain
    /// </summary>
    [Serializable]

    public sealed class SfcUnregisteredXmlTypeException : SfcException
    {
        public SfcUnregisteredXmlTypeException()
            : base()
        {
        }

        public SfcUnregisteredXmlTypeException(string message)
            : base(message)
        {
        }

        public SfcUnregisteredXmlTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcUnregisteredXmlTypeException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown during deserialization if the Xml contains invalid properties.
    /// </summary>
    [Serializable]

    public sealed class SfcNonSerializablePropertyException : SfcException
    {
        public SfcNonSerializablePropertyException()
            : base()
        {
        }


        public SfcNonSerializablePropertyException(string message)
            : base(message)
        {
        }

        public SfcNonSerializablePropertyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcNonSerializablePropertyException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown during deserialization if the Xml contains invalid properties.
    /// </summary>
    [Serializable]

    public sealed class SfcUnsupportedVersionSerializationException : SfcException
    {
        public SfcUnsupportedVersionSerializationException()
            : base()
        {
        }


        public SfcUnsupportedVersionSerializationException(string message)
            : base(message)
        {
        }

        public SfcUnsupportedVersionSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcUnsupportedVersionSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown during deserialization if the Xml is either empty or does not contain
    /// any xml that could be deserialized
    /// </summary>
    [Serializable]

    public sealed class SfcEmptyXmlException : SfcException
    {
        public SfcEmptyXmlException()
            : base()
        {
        }

        public SfcEmptyXmlException(string message)
            : base(message)
        {
        }

        public SfcEmptyXmlException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcEmptyXmlException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        public override string Message
        {
            get
            {
                return SfcStrings.EmptySfcXml;
            }
        }
    }

    /// <summary>
    /// This exception is thrown during deserialization if a parent Type is given and it is not the correct Type to parent the top-level
    /// objects the Xml contains
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidXmlParentTypeException : SfcException
    {
        public SfcInvalidXmlParentTypeException()
            : base()
        {
        }

        public SfcInvalidXmlParentTypeException(string message)
            : base(message)
        {
        }


        public SfcInvalidXmlParentTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidXmlParentTypeException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when an invalid type of query is passed to the ObjectQuery
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidQueryExpressionException : SfcException
    {
        public SfcInvalidQueryExpressionException()
            : base()
        {
        }

        public SfcInvalidQueryExpressionException(string message)
            : base(message)
        {
        }

        public SfcInvalidQueryExpressionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidQueryExpressionException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }


    /// <summary>
    /// This exception is thrown during GetConnection(ObjectQueryMode) on a domain root object if a suitable connection cannot be returned
    /// to support the type of query mode requested. It is usually due to a muptiple query request with a server in single user mode, or some other
    /// inability to return a connection other than the current default one.
    /// </summary>
    [Serializable]

    public sealed class SfcQueryConnectionUnavailableException : SfcException
    {
        public SfcQueryConnectionUnavailableException()
        {
        }

        public SfcQueryConnectionUnavailableException(string message)
            :base(message)
        {
        }

        public SfcQueryConnectionUnavailableException(string message, Exception innerException)
            :base(message, innerException)
        {
        }

        private SfcQueryConnectionUnavailableException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        public override string Message
        {
            get
            {
                return SfcStrings.SfcQueryConnectionUnavailable;
            }
        }
    }

    /// <summary>
    /// This exception is thrown on attempt to perform an operation that is invalid for an object in given state
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidStateException : SfcException
    {
        public SfcInvalidStateException()
            : base()
        {
        }

        public SfcInvalidStateException(string message)
            : base(message)
        {
        }

        public SfcInvalidStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcInvalidStateException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown on attempt to perform an operation that is invalid for an object in given state
    /// </summary>
    [Serializable]

    public sealed class SfcCRUDOperationFailedException : SfcException
    {
        public SfcCRUDOperationFailedException()
            : base()
        {
        }

        public SfcCRUDOperationFailedException(string message)
            : base(message)
        {
        }

        public SfcCRUDOperationFailedException(string message,Exception innerException)
            : base(message,innerException)
        {
        }

        private SfcCRUDOperationFailedException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// Can't perform this operation when Parent isn't set
    /// </summary>
    [Serializable]

    public sealed class SfcMissingParentException : SfcException
    {
        public SfcMissingParentException()
            : base()
        {
        }


        public SfcMissingParentException(string message)
            : base(message)
        {
        }

        public SfcMissingParentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcMissingParentException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// Can't find scripting operation for this object
    /// </summary>
    [Serializable]

    public sealed class SfcObjectNotScriptableException : SfcException
    {
        public SfcObjectNotScriptableException()
            : base()
        {
        }

        public SfcObjectNotScriptableException(string message)
            : base(message)
        {
        }

        public SfcObjectNotScriptableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcObjectNotScriptableException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]

    public sealed class SfcSecurityException : SfcException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SfcSecurityException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcSecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcSecurityException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private SfcSecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception gets thrown when an invalid connection context mode change is attempted.
    /// </summary>
    [Serializable]

    public sealed class SfcInvalidConnectionContextModeChangeException : SfcException
    {
        private string fromMode = null;
        private string toMode = null;

        public SfcInvalidConnectionContextModeChangeException()
            : this(string.Empty)
        {
            
        }

        public SfcInvalidConnectionContextModeChangeException(string fromMode, string toMode)
            : this(fromMode, toMode, null)
        {
        }

        public SfcInvalidConnectionContextModeChangeException(string fromMode, string toMode, Exception innerException)
            : this(SfcStrings.SfcInvalidConnectionContextModeChange(fromMode, toMode), innerException)
        {
            this.fromMode = fromMode;
            this.toMode = toMode;
        }

        internal SfcInvalidConnectionContextModeChangeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal SfcInvalidConnectionContextModeChangeException(string message) : base(message, null)
        {
            this.fromMode = string.Empty;
            this.toMode = string.Empty;
        }

        private SfcInvalidConnectionContextModeChangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.fromMode = (string)info.GetValue("fromMode", typeof(string));
            this.toMode = (string)info.GetValue("toMode", typeof(string));
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
            info.AddValue("fromMode", this.fromMode);
            info.AddValue("toMode", this.toMode);
            base.GetObjectData(info, context);
        }
#endif


        public override string Message
        {
            get
            {
                return SfcStrings.SfcInvalidConnectionContextModeChange(fromMode, toMode);
            }
        }
    }

    /// <summary>
    /// This exception is thrown when SQLCE is not installed properly.
    /// </summary>
    [Serializable]

    public sealed class SfcSqlCeNotInstalledException : SfcException
    {
        public SfcSqlCeNotInstalledException()
            : base()
        {
        }

        public SfcSqlCeNotInstalledException(string message)
            : base(message)
        {
        }

        public SfcSqlCeNotInstalledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SfcSqlCeNotInstalledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }



    /// <summary>
    /// Thrown when a URN to PS Path conversion fails.
    /// </summary>
    [Serializable]

    public sealed class SfcPathConversionException : SfcException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SfcPathConversionException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcPathConversionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcPathConversionException(string message)
            : base(message)
        {
        }


        /// <summary>
        /// Base constructor
        /// </summary>
        private SfcPathConversionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when the Design Mode switch failed.
    /// </summary>
    [Serializable]

    public sealed class SfcDesignModeException : SfcException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SfcDesignModeException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcDesignModeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcDesignModeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private SfcDesignModeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when a property in not supported for current Server Version.
    /// </summary>
    [Serializable]

    public sealed class SfcUnsupportedVersionException : SfcException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SfcUnsupportedVersionException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcUnsupportedVersionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public SfcUnsupportedVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        private SfcUnsupportedVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

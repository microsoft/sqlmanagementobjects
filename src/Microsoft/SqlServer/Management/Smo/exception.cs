// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Server;
#if NETFRAMEWORK
using System.Security.Permissions;
#else
using System.Collections.Generic;
#endif

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Enumeration of exception types defined by
    /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType.
    /// </summary>
    public enum SmoExceptionType
    {
        SmoException = 0,
        MissingObjectException = 1,
        PropertyNotSetException = 2,
        WrongPropertyValueException = 3,
        PropertyTypeMismatchException = 4,
        UnknownPropertyException = 5,
        PropertyReadOnlyException = 6,
        InvalidSmoOperationException = 7,
        InvalidVersionSmoOperationException = 8,
        CollectionNotAvailableException = 9,
        PropertyCannotBeRetrievedException = 10,
        InternalSmoErrorException = 11,
        FailedOperationException = 12,
        UnsupportedObjectNameException = 13,
        ServiceRequestException = 14,
        UnsupportedVersionException = 15,
        PropertyWriteException = 16,
        UnsupportedFeatureException = 17,
        SfcDependencyException = 18,
        UnsupportedEngineTypeException = 19,
        InvalidScriptingOptionException = 20,
        ScriptWriterException = 21,
        UnsupportedCompatLevelException = 22,
        UnsupportedEngineEditionException = 23,
    }


    internal class SmoExceptionSingleton
    {
        internal string prodVer;
    }
    /// <summary>
    /// The base class for all SMO exception classes.
    /// </summary>
    [Serializable]
    public class SmoException : SqlServerManagementException
    {
        /// <summary>
        /// Called by the T:Microsoft.SqlServer.Management.Smo.SqlServerManagementException.
        /// Do not call directly.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SmoException()
            : base()
        {
            Init();
        }
        /// <summary>
        /// Called by the T:Microsoft.SqlServer.Management.Smo.SqlServerManagementException.
        /// Do not call directly.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SmoException(string message)
            : base(message)
        {
            Init();
        }
        /// <summary>
        /// Called by the T:Microsoft.SqlServer.Management.Smo.SqlServerManagementException.
        /// Do not call directly.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SmoException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SmoException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private void Init()
        {
            Data.Add("HelpLink.ProdVer", ProdVer);
        }

        static SmoException()
        {
            smoExceptionSingleton.prodVer = string.Empty;

            object[] attribs = typeof(SmoException).GetAssembly().GetCustomAttributes(true);
            if( null != attribs )
            {
                foreach( object o in attribs )
                {
                    if( o is AssemblyFileVersionAttribute )
                    {
                        smoExceptionSingleton.prodVer = ((AssemblyFileVersionAttribute)o).Version;
                        break;
                    }
                }
            }
        }

        static readonly SmoExceptionSingleton smoExceptionSingleton = new SmoExceptionSingleton();
        protected static string ProdVer
        {
            get
            {
                return smoExceptionSingleton.prodVer;
            }
        }

        internal protected SmoException SetHelpContext(string resource)
        {

            Data["HelpLink.EvtSrc"] = ("Microsoft.SqlServer.Management.Smo.ExceptionTemplates." + resource);

            return this;
        }

        /// <summary>
        /// Gets or sets the T:Microsoft.SqlServer.Management.Smo.SmoExceptionType.
        /// </summary>
        public virtual SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.SmoException;
            }
        }

        // will output a link to the help web site
        // http://www.microsoft.com/products/ee/transform.aspx?ProdName=Microsoft%20SQL%20Server&ProdVer=09.00.0000.00&EvtSrc=MSSQLServer&EvtID=15401
        /// <summary>
        /// Gets a link as string to the support web site.
        /// </summary>
        public override string HelpLink
        {
            get
            {
                StringBuilder link = new StringBuilder(Globals.INIT_BUFFER_SIZE);
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
    /// The exception thrown when an object is missing from a collection or
    /// when a collection does not exist under a server version.
    /// </summary>
    [Serializable]
    public sealed class MissingObjectException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.MissingObjectException.
        /// </summary>
        public MissingObjectException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.MissingObjectException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        public MissingObjectException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.MissingObjectException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public MissingObjectException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }

        private MissingObjectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("ObjectDoesNotExist");
        }
        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.MissingObjectException;
            }
        }

        /// <summary>
        /// Gets the message from the exception as a string.
        /// </summary>
        public override string Message
        {
            get
            {
                return base.Message;
            }
        }
    }

    /// <summary>
    /// The exception thrown when an action requires a property
    /// that has not been set by the user.
    /// </summary>
    [Serializable]
    public sealed class PropertyNotSetException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyNotSetException.
        /// </summary>
        public PropertyNotSetException() : base()
        {
            propertyName = string.Empty;
            Init();
        }

        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyNotSetException.
        /// </summary>
        /// <param name="message">Exception message as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public PropertyNotSetException(string message, Exception innerException) : base(message, innerException)
        {
            propertyName = string.Empty;
            Init();
        }

        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyNotSetException.
        /// </summary>
        /// <param name="propertyName">The name of the property that has not
        /// been set as string.</param>
        public PropertyNotSetException(string propertyName) : base()
        {
            this.propertyName = propertyName;
            Init();
        }

        private PropertyNotSetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = (string)info.GetString("propertyName");
            Init();
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }

        private void Init()
        {
            SetHelpContext("PropertyNotSetExceptionText");
        }
        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.PropertyNotSetException;
            }
        }

        /// <summary>
        /// Gets the message from the exception as a string.
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.PropertyNotSetExceptionText(propertyName));
            }
        }

        string propertyName;
        public string PropertyName { get { return propertyName; } }
    }

    /// <summary>
    /// The exception thrown during a scripting action when
    /// a property has an unusable value or there is a conflict between
    /// two or more properties.
    /// </summary>
    [Serializable]
    public sealed class WrongPropertyValueException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.WrongPropertyValueException.
        /// </summary>
        public WrongPropertyValueException() : base()
        {
            Init();
        }

        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.WrongPropertyValueException.
        /// </summary>
        /// <param name="message">The message of the exception as strng.</param>
        public WrongPropertyValueException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.WrongPropertyValueException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that
        /// caused the current exception.</param>
        public WrongPropertyValueException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.WrongPropertyValueException.
        /// </summary>
        /// <param name="propertyObject">The property whose value is incorrect as supplied.</param>
        public WrongPropertyValueException(Property propertyObject) : base()
        {
            this.property = propertyObject;
            Data["HelpLink.EvtData1"] = propertyObject.Name;
            Init();
        }

        private WrongPropertyValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            property= (Property)info.GetValue("property", typeof(Property));
            Init();
        }



        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
 #if NETFRAMEWORK
        //Adding security permissions,as there was a voilation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("property", this.property);
            base.GetObjectData(info, context);
        }

        private void Init()
        {
            SetHelpContext("WrongPropertyValueExceptionText");
        }
        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.WrongPropertyValueException;
            }
        }
        /// <summary>
        /// Gets the message as string from the exception.
        /// </summary>
        public override string Message
        {
            get
            {
                StringBuilder propertiesText = new StringBuilder();
                if( property != null )
                {
                    return string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.WrongPropertyValueExceptionText(property.Name,
                                    (null != property.Value)?property.Value.ToString():string.Empty));
                }
                else
                {
                    return base.Message;
                }

            }
        }

        Property property = null;
        public Property Property { get { return property; } }
    }

    /// <summary>
    /// The exception thrown if a value of the wrong type is assigned to a property.
    /// </summary>
    [Serializable]
    public sealed class PropertyTypeMismatchException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// Microsoft.SqlServer.Management.Smo.PropertyTypeMismatchException.
        /// </summary>
        public PropertyTypeMismatchException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// Microsoft.SqlServer.Management.Smo.PropertyTypeMismatchException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public PropertyTypeMismatchException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// Microsoft.SqlServer.Management.Smo.PropertyTypeMismatchException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public PropertyTypeMismatchException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// Microsoft.SqlServer.Management.Smo.PropertyTypeMismatchException.
        /// </summary>
        /// <param name="propertyName">The property that was mismatched.</param>
        /// <param name="receivedType">The incorrect type received for the property.</param>
        /// <param name="expectedType">The expected typ for the property.</param>
        public PropertyTypeMismatchException(string propertyName, string receivedType, string expectedType)
            : base()
        {
            this.propertyName = propertyName;
            this.receivedType = receivedType;
            this.expectedType = expectedType;

            Data["HelpLink.EvtData1"] = propertyName;
        }


        private PropertyTypeMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
            receivedType = info.GetString("receivedType");
            expectedType = info.GetString("expectedType");
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            info.AddValue("receivedType", this.receivedType);
            info.AddValue("expectedType", this.expectedType);
            base.GetObjectData(info, context);
        }


        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.PropertyTypeMismatchException;
            }
        }
        /// <summary>
        /// Gets the message for the exception as string.
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplates.PropertyTypeMismatchExceptionText(propertyName, receivedType, expectedType);
            }
        }

        void Init()
        {
            propertyName = string.Empty;
            receivedType = string.Empty;
            expectedType = string.Empty;
            SetHelpContext("PropertyTypeMismatchExceptionText");
        }

        string propertyName;
        public string PropertyName { get { return propertyName; } }
        string receivedType;
        public string ReceivedType { get { return receivedType; } }
        string expectedType;
        public string ExpectedType { get { return expectedType; } }
    }


    /// <summary>
    /// The exception thrown when trying to access a property that
    /// does not exist for a server version.
    /// </summary>
    [Serializable]
    public sealed class UnknownPropertyException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.UnknownPropertyException.
        /// </summary>
        public UnknownPropertyException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.UnknownPropertyException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public UnknownPropertyException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }

        //send empty string to base class so that it will not build a default message,
        //we will generate a proper message in the overloaded Message property
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.UnknownPropertyException.
        /// </summary>
        /// <param name="propertyName">The unknown property name.</param>
        public UnknownPropertyException(string propertyName) : base(string.Empty)
        {
            this.propertyName = propertyName;

            Data["HelpLink.EvtData1"] = propertyName;
        }

        internal UnknownPropertyException(string propertyName, string message) : base(message)
        {
            this.propertyName = propertyName;

            Data["HelpLink.EvtData1"] = propertyName;
        }

        private UnknownPropertyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
        }


        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnknownPropertyException;
            }
        }
        /// <summary>
        /// Gets the message from the
        /// T:Microsoft.SqlServer.Management.Smo.UnknownPropertyException.
        /// </summary>
        public override string Message
        {
            get
            {
                //if we haven't been already set a message build one
                if (base.Message.Length <= 0)
                {
                    return ExceptionTemplates.UnknownPropertyExceptionText(propertyName);
                }
                return base.Message;
            }
        }
        /// <summary>
        /// Initializes T:Microsoft.SqlServer.Management.Smo.UnknownPropertyException.
        /// </summary>
        void Init()
        {
            propertyName = string.Empty;
            supportedVersions = null;
            currentVersion = null;

            SetHelpContext("UnknownPropertyExceptionText");
        }

        string propertyName;
        ServerVersion [] supportedVersions;
        ServerVersion currentVersion;
        public string PropertyName { get { return propertyName; } }
        public ServerVersion [] SupportedVersions { get { return supportedVersions; } }
        public ServerVersion CurrentVersion { get { return currentVersion; } }

    }

    /// <summary>
    /// The exception that is thrown when trying to set a readonly property.
    /// </summary>
    [Serializable]
    public sealed class PropertyReadOnlyException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyReadOnlyException.
        /// </summary>
        public PropertyReadOnlyException() : base()
        {
            propertyName = string.Empty;
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyReadOnlyException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public PropertyReadOnlyException(string message, Exception innerException) : base(message, innerException)
        {
            propertyName = string.Empty;
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyReadOnlyException.
        /// </summary>
        /// <param name="propertyName">The name of the read-only property.</param>
        public PropertyReadOnlyException(string propertyName) : base()
        {
            this.propertyName = propertyName;
            Init();
            Data["HelpLink.EvtData1"] = propertyName;
        }


        private PropertyReadOnlyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }

        private void Init()
        {
            SetHelpContext("PropertyReadOnlyExceptionText");
        }
        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.PropertyReadOnlyException;
            }
        }
        /// <summary>
        /// Gets the message of the exception as string.
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplates.PropertyReadOnlyExceptionText(propertyName);
            }
        }

        string propertyName;
        public string PropertyName { get { return propertyName; } }

    }

    /// <summary>
    /// The exception thrown when trying to set a property that has been blocked.
    /// </summary>
    [Serializable]
    public sealed class PropertyWriteException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyWriteException.
        /// </summary>
        public PropertyWriteException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyWriteException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public PropertyWriteException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyWriteException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public PropertyWriteException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.PropertyWriteException.
        /// </summary>
        /// <param name="propertyName">The name as string of the blocked property that caused
        /// the exception.</param>
        /// <param name="objectKind">The kind of object as string of the blocked property.</param>
        /// <param name="objectName">The name of the object as string of the blocked property</param>
        /// <param name="reason">The reason for the property blockage.</param>
        public PropertyWriteException(string propertyName, string objectKind, string objectName, string reason) : base()
        {
            Init();
            this.propertyName = propertyName;
            this.objectKind = objectKind;
            this.objectName = objectName;
            this.reason = reason;
            Data["HelpLink.EvtData1"] = propertyName;

        }

        private PropertyWriteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            base.GetObjectData(info, context);
        }


        private void Init()
        {
            propertyName = string.Empty;
            objectKind = string.Empty;
            objectName = string.Empty;
            reason = string.Empty;
            SetHelpContext("PropertyWriteException");
        }
        /// <summary>
        /// Gets the type of exception from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.PropertyWriteException;
            }
        }
        /// <summary>
        /// Gets the message of the exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return ExceptionTemplates.FailedToWriteProperty(propertyName, objectKind, objectName, reason);
            }
        }

        string propertyName;
        string objectKind;
        string objectName;
        string reason;
        public string PropertyName { get { return propertyName; } }
    }

    /// <summary>
    /// The exception thrown when an operation cannot be performed
    /// in the current object state.
    /// </summary>
    [Serializable]
    public sealed class InvalidSmoOperationException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.InvalidSmoOperationException.
        /// </summary>
        public InvalidSmoOperationException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.InvalidSmoOperationException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public InvalidSmoOperationException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.InvalidSmoOperationException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public InvalidSmoOperationException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.InvalidSmoOperationException.
        /// </summary>
        /// <param name="opName">The invalid operation name as string.</param>
        /// <param name="state">The state as a
        /// T:Microsoft.SqlServer.Management.Smo.SqlSmoState object.</param>
        public InvalidSmoOperationException(string opName, SqlSmoState state) : base()
        {
            this.opName = opName;
            this.state = state;

            Data["HelpLink.EvtData1"] = opName;
        }


        private InvalidSmoOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            opName = info.GetString("opName");
            state = (SqlSmoState)info.GetValue("state", typeof(SqlSmoState));
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("opName", this.opName);
            info.AddValue("state", this.state);
            base.GetObjectData(info, context);
        }


        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.invalidSmoOperationException.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.InvalidSmoOperationException;
            }
        }
        /// <summary>
        /// Gets the message of the exception as string.
        /// </summary>
        public override string Message
        {
            get
            {
                if( opName.Length == 0 )
                {
                    return base.Message;
                }
                else
                {
                    return ExceptionTemplates.InvalidSmoOperationExceptionText(opName, state.ToString() );
                }
            }
        }

        void Init()
        {
            opName = string.Empty;
            state = SqlSmoState.Creating;

            SetHelpContext("InvalidSmoOperationExceptionText");
        }

        string opName;
        SqlSmoState state;
    }

    /// <summary>
    /// The exception thrown when an operation cannot be performed
    /// in the current version.
    /// </summary>
    [Serializable]
    public sealed class InvalidVersionSmoOperationException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.InvalidVersionSmoOperationException.
        /// </summary>
        public InvalidVersionSmoOperationException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.InvalidVersionSmoOperationException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public InvalidVersionSmoOperationException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.InvalidVersionSmoOperationException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public InvalidVersionSmoOperationException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.InvalidVersionSmoOperationException.
        /// </summary>
        /// <param name="version">The server version as
        /// T:Microsoft.SqlServer.Server.ServerVersion</param>
        public InvalidVersionSmoOperationException(ServerVersion version) : base()
        {
            this.version = version;
        }

        private InvalidVersionSmoOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            version = (ServerVersion)info.GetValue("version", typeof(ServerVersion));
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", this.version);
            base.GetObjectData(info, context);
        }


        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.InvalidVersionSmoOperationException;
            }
        }
        /// <summary>
        /// Gets the message of the exception as string.
        /// </summary>
        public override string Message
        {
            get
            {
                if (null != version)
                {
                    if (this.version.Major >= 9)
                    {
                        return ExceptionTemplates.InvalidVersionSmoOperation(LocalizableResources.ServerYukon);
                    }
                    else if (this.version.Major == 8)
                    {
                        return ExceptionTemplates.InvalidVersionSmoOperation(LocalizableResources.ServerShiloh);
                    }
                    else if (this.version.Major == 7)
                    {
                        return ExceptionTemplates.InvalidVersionSmoOperation(LocalizableResources.ServerSphinx);
                    }
                }
                return string.Empty;
            }
        }

        void Init()
        {
            version = null;
            SetHelpContext("InvalidVersionSmoOperation");
        }

        ServerVersion version;
    }

    /// <summary>
    /// The exception thrown when the user is asking for a collection
    /// not available for the current server version.
    /// </summary>
    [Serializable]
    public sealed class CollectionNotAvailableException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.CollectionNotAvailableException.
        /// </summary>
        public CollectionNotAvailableException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.CollectionNotAvailableException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public CollectionNotAvailableException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.CollectionNotAvailableException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public CollectionNotAvailableException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.CollectionNotAvailableException.
        /// </summary>
        /// <param name="colname">The name of the collection as string.</param>
        /// <param name="serverVersion">The server version as
        /// T:Microsoft.SqlServer.Server.ServerVersion</param>
        public CollectionNotAvailableException(string colname, ServerVersion serverVersion )
        {
            this.colname = colname;
            this.serverVersion = serverVersion;
        }

        private CollectionNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            colname = info.GetString("colname");
            serverVersion = (ServerVersion)info.GetValue("serverVersion", typeof(ServerVersion));
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a violation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("colname", this.colname);
            info.AddValue("serverVersion", this.serverVersion);
            base.GetObjectData(info, context);
        }


        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.CollectionNotAvailableException;
            }
        }

        private void Init()
        {
            colname = string.Empty;
            serverVersion = null;

            SetHelpContext("CollectionNotAvailable");
        }
        /// <summary>
        /// Gets the message of the exception as formatted string.
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.CollectionNotAvailable(colname, serverVersion != null ? serverVersion.ToString() : string.Empty ) );
            }
        }

        string colname;
        public string CollectionName { get { return colname; } }
        ServerVersion serverVersion;
        public ServerVersion ServerVersion { get { return serverVersion; } }

    }

    /// <summary>
    /// The exception thrown when the caller is asking for a property
    /// that returned null during enumeration.
    /// </summary>
    [Serializable]
    public sealed class PropertyCannotBeRetrievedException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyCannotBeRetrievedException.
        /// </summary>
        public PropertyCannotBeRetrievedException() : base()
        {
            propertyName = string.Empty;
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyCannotBeRetrievedException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public PropertyCannotBeRetrievedException(string message) : base(message)
        {
            propertyName = string.Empty;
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyCannotBeRetrievedException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public PropertyCannotBeRetrievedException(string message, Exception innerException) : base(message, innerException)
        {
            propertyName = string.Empty;
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.PropertyCannotBeRetrievedException.
        /// </summary>
        /// <param name="propertyName">The name of the unretrievable property as string.</param>
        /// <param name="failedObject">The failed object as T:System.Object.</param>
        public PropertyCannotBeRetrievedException(string propertyName, object failedObject) : base()
        {
            this.propertyName = propertyName;
            this.failedObject = failedObject;
            Init();
            Data["HelpLink.EvtData1"] = propertyName;
        }

        internal PropertyCannotBeRetrievedException(string propertyName, object failedObject, string reason) : base()
        {
            this.propertyName = propertyName;
            this.failedObject = failedObject;
            this.reason = reason;
            Init();
            Data["HelpLink.EvtData1"] = propertyName;
        }

        private PropertyCannotBeRetrievedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            propertyName = info.GetString("propertyName");
            reason = info.GetString("reason");
            // failed object will not be saved, because it belongs to the tree
            Init();
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a voilation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("propertyName", this.propertyName);
            info.AddValue("reason", this.reason);
            base.GetObjectData(info, context);
        }


        private void Init()
        {
            SetHelpContext("PropertyCannotBeRetrievedExceptionText");
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.PropertyCannotBeRetrievedException;
            }
        }
        /// <summary>
        /// Gets the message of the exception as string.
        /// </summary>
        public override string Message
        {

            get
            {
                string message = null;
                if (null != propertyName && propertyName.Length > 0 && null != failedObject)
                {
                    message = ExceptionTemplates.PropertyCannotBeRetrievedExceptionText(propertyName,
                                                SqlSmoObject.GetTypeName(failedObject.GetType().Name),
                                                failedObject.ToString());
                }
                else
                {
                    message = base.Message;
                }
                if (reason.Length > 0)
                {
                    message += " " + reason;
                }
                return message;
            }
        }
        string reason = string.Empty;

        string propertyName;
        public string PropertyName { get { return propertyName; } }

        object failedObject;
        public object FailedObject { get { return failedObject; } }

    }

    /// <summary>
    /// The exception thrown when there is an internal error with a
    /// T:Microsoft.SqlServer.Management.Smo object.
    /// </summary>
    [Serializable]
    public sealed class InternalSmoErrorException : SmoException
    {
        /// <summary>
        /// Constructor for T:Microsoft.SqlServer.Management.Smo.InternalSmoException.
        /// </summary>
        public InternalSmoErrorException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for T:Microsoft.SqlServer.Management.Smo.InternalSmoException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public InternalSmoErrorException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for T:Microsoft.SqlServer.Management.Smo.InternalSmoException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public InternalSmoErrorException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }

        private InternalSmoErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("InternalSmoErrorException");
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.InternalSmoErrorException;
            }
        }

    }

    /// <summary>
    /// The exception thrown when an operation has failed.
    /// </summary>
    [Serializable]
    public sealed class FailedOperationException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.FailedOperationException.
        /// </summary>
        public FailedOperationException() : base()
        {
            SetHelpContext("FailedOperationExceptionText");
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.FailedOperationException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public FailedOperationException(string message) : base(message)
        {
            SetHelpContext("FailedOperationExceptionText");
        }

        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.FailedOperationException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public FailedOperationException(string message, Exception innerException) : base(message, innerException)
        {
            SetHelpContext("FailedOperationExceptionText");
        }

        private FailedOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            reason = info.GetString("reason");
            SetHelpContext("FailedOperationExceptionText");
        }

        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a voilation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("reason", this.reason);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Called by the T:Microsoft.SqlServer.Management.Smo.SqlServerManagementException.
        /// Do not call directly.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.SqlServer.Management.Smo.SmoException.#ctor(System.String,System.Exception)")]
        public FailedOperationException(string operation, object failedObject, Exception innerException)
            : base("", innerException)
        {
            this.operation= operation;
            this.failedObject = failedObject;

            SetHelpContext("FailedOperationExceptionText");
            Data.Add("HelpLink.EvtData1", operation);
            if( null != failedObject )
            {
                Data.Add("HelpLink.EvtData2", failedObject.GetType().Name);
            }
        }

        /// <summary>
        /// Called by the T:Microsoft.SqlServer.Management.Smo.SqlServerManagementException.
        /// Do not call directly.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.SqlServer.Management.Smo.SmoException.#ctor(System.String,System.Exception)")]
        public FailedOperationException(string operation, object failedObject, Exception innerException, string reason)
            : base("", innerException)
        {
            this.operation= operation;
            this.failedObject = failedObject;
            this.reason = reason;

            SetHelpContext("FailedOperationExceptionText");
            Data.Add("HelpLink.EvtData1", operation);
            if( null != failedObject )
            {
                Data.Add("HelpLink.EvtData2", failedObject.GetType().Name);
            }
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.FailedOperationException;
            }
        }

        string operation = string.Empty;
        /// <summary>
        /// Gets or sets a string value that specifies the operation.
        /// </summary>
        public string Operation
        {
            get { return operation; }
            set { operation = value; }
        }

        [NonSerialized]
        object failedObject = null;
        /// <summary>
        /// Gets or sets the T:System.Object that failed.
        /// </summary>
        public object FailedObject
        {
            get { return failedObject; }
            set { failedObject = value; }
        }

        string reason = string.Empty;

        /// <summary>
        /// Gets the message of the exception.
        /// </summary>
        /// <value></value>
        public override string Message
        {
            get
            {
                //if we haven't been already set a message and we have enough data to build one
                if( base.Message.Length <= 0 && null != operation && operation.Length > 0 && null != failedObject )
                {
                    //build a message
                    string msg = string.Empty;

                    if (failedObject is SqlSmoObject)
                    {
                        // message for the objects that we know
                        msg = ExceptionTemplates.FailedOperationExceptionText(operation,
                                                SqlSmoObject.GetTypeName(failedObject.GetType().Name),
                                                ((SqlSmoObject)failedObject).key.GetExceptionName() );
                    }
                    else if (failedObject is AbstractCollectionBase)
                    {
                        // this message is for collections
                        SqlSmoObject parentObj = ((AbstractCollectionBase)failedObject).ParentInstance;
                        msg = ExceptionTemplates.FailedOperationExceptionTextColl(operation,
                                                SqlSmoObject.GetTypeName(failedObject.GetType().Name),
                                                SqlSmoObject.GetTypeName(parentObj.GetType().Name),
                                                parentObj.key.GetExceptionName());


                    }
                    else
                    {
                        // all other cases just report that the operation failed
                        msg = ExceptionTemplates.FailedOperationExceptionText2(operation);
                    }

                    return msg  + (reason != null?(" " +reason):string.Empty);
                }
                else
                {
                    return base.Message;
                }
            }
        }
    }

    /// <summary>
    /// The exception thrown when the user is trying to create
    /// an object with a name that cannot be supported, such as a null string.
    /// </summary>
    [Serializable]
    public sealed class UnsupportedObjectNameException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedObjectNameException.
        /// </summary>
        public UnsupportedObjectNameException() : base()
        {
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedObjectNameException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public UnsupportedObjectNameException(string message) : base(message)
        {
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedObjectNameException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public UnsupportedObjectNameException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private UnsupportedObjectNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnsupportedObjectNameException;
            }
        }

    }

    /// <summary>
    /// This exception incapsulates a service provider error.
    /// </summary>
    [Serializable]
    public sealed class ServiceRequestException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.ServiceRequestException.
        /// </summary>
        public ServiceRequestException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.ServiceRequestException.
        /// </summary>
        /// <param name="message">The message of the exception as string.</param>
        public ServiceRequestException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.ServiceRequestException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public ServiceRequestException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }

        internal  ServiceRequestException(UInt32 retcode) : base()
        {
            Init();
            errorCode = retcode;
        }

        private ServiceRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
            errorCode = info.GetUInt32("errorCode");
        }
        /// <summary>
        /// Serialization helper method.
        /// </summary>
        /// <param name="info">T:System.Runtime.Serialization.SerializationInfo object
        /// that contains the data needed to serialize or deserialize an object.</param>
        /// <param name="context">T:System.Runtime.Serialization.StreamingContext that
        /// contains the source and destination of a given serialized stream.</param>
#if NETFRAMEWORK
        //Adding security permissions,as there was a voilation while implementing ISerializable
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("errorCode", this.errorCode);
            base.GetObjectData(info, context);
        }

        private void Init()
        {
            // generic service request error
            SetHelpContext("ServiceRequestException");
        }
        /// <summary>
        /// Gets the  type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.ServiceRequestException;
            }
        }

        UInt32 errorCode = 0;

        // this is actually an UInt32, but this type is not CLS-compliant
        // so we expose it as int
        /// <summary>
        /// Gets the error code as an integer.
        /// </summary>
        public  Int32 ErrorCode
        {
            get
            {
                return SmoApplication.ConvertUInt32ToInt32(errorCode);
            }
        }

        /// <summary>
        /// The message of the exception.
        /// </summary>
        public override string Message
        {
            get
            {
                // return an error from the error massage map if we can
                if( errorCode < ServiceErrorMessageMap.Length  )
                {
                    // get the error messsage.
                    return ServiceErrorMessageMap[errorCode];
                }
                else
                {
                    // decide if the error code is a WMI or not
                    if( 	( 0x80041001 <= errorCode && errorCode <= 0x80041067 ) ||
                        ( 0x80042001 <= errorCode && errorCode <= 0x80042002 ) )
                    {
                        // we have a WMI error code, but we don't map it to a string
                        // because there is no way of doing this via API
                        return ExceptionTemplates.WMIException(errorCode.ToString("X", SmoApplication.DefaultCulture));
                    }
                    else
                    {
                        if (SqlContext.IsAvailable)
                        {
                            return ExceptionTemplates.Win32Error(ErrorCode.ToString());
                        }
                            return ExceptionTemplates.UnknownError;
                    }
                }
            }
        }

        // this is the list of Service error messages we currently have.
        // Subsequent additions need to update this list
        private string[] ServiceErrorMessageMap = new string[] {
                                            ExceptionTemplates.ServiceError0,
                                            ExceptionTemplates.ServiceError1,
                                            ExceptionTemplates.ServiceError2,
                                            ExceptionTemplates.ServiceError3,
                                            ExceptionTemplates.ServiceError4,
                                            ExceptionTemplates.ServiceError5,
                                            ExceptionTemplates.ServiceError6,
                                            ExceptionTemplates.ServiceError7,
                                            ExceptionTemplates.ServiceError8,
                                            ExceptionTemplates.ServiceError9,
                                            ExceptionTemplates.ServiceError10,
                                            ExceptionTemplates.ServiceError11,
                                            ExceptionTemplates.ServiceError12,
                                            ExceptionTemplates.ServiceError13,
                                            ExceptionTemplates.ServiceError14,
                                            ExceptionTemplates.ServiceError15,
                                            ExceptionTemplates.ServiceError16,
                                            ExceptionTemplates.ServiceError17,
                                            ExceptionTemplates.ServiceError18,
                                            ExceptionTemplates.ServiceError19,
                                            ExceptionTemplates.ServiceError20,
                                            ExceptionTemplates.ServiceError21,
                                            ExceptionTemplates.ServiceError22,
                                            ExceptionTemplates.ServiceError23,
                                            ExceptionTemplates.ServiceError24
                                            };
    }


    /// <summary>
    /// The exception gets thrown when an operation has failed.
    /// </summary>
    [Serializable]
    public sealed class UnsupportedVersionException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedVersionException.
        /// </summary>
        public UnsupportedVersionException() : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedVersionException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        public UnsupportedVersionException(string message) : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedVersionException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public UnsupportedVersionException(string message, Exception innerException) : base(message, innerException)
        {
            Init();
        }

        private UnsupportedVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("UnsupportedVersion");
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnsupportedVersionException;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when an operation has failed
    /// </summary>
    [Serializable]
    public sealed class UnsupportedEngineTypeException : SmoException
    {
        public UnsupportedEngineTypeException()
            : base()
        {
            Init();
        }

        public UnsupportedEngineTypeException(string message)
            : base(message)
        {
            Init();
        }

        public UnsupportedEngineTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private UnsupportedEngineTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("UnsupportedEngineType");
        }

        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnsupportedEngineTypeException;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when an unsupported engine edition is detected.
    /// </summary>
    [Serializable]
    public sealed class UnsupportedEngineEditionException : SmoException
    {
        public UnsupportedEngineEditionException()
            : base()
        {
            Init();
        }

        public UnsupportedEngineEditionException(string message)
            : base(message)
        {
            Init();
        }

        public UnsupportedEngineEditionException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private UnsupportedEngineEditionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("UnsupportedEngineEdition");
        }

        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnsupportedEngineEditionException;
            }
        }
    }

    /// <summary>
    /// The exception thrown when an object is not supported by Sql Express
    /// </summary>
    [Serializable]
    public sealed class UnsupportedFeatureException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedFeatureException.
        /// </summary>
        public UnsupportedFeatureException()
            : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedFeatureException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        public UnsupportedFeatureException(string message)
            : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedFeatureException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public UnsupportedFeatureException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private UnsupportedFeatureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("UnsupportedFeatureException");
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnsupportedFeatureException;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when a dependency operation failed
    /// TODO: this will move to SFC once we have generalized the dependency classes (see SfcDependencyDiscovery.cs).
    /// </summary>
    [Serializable]
    public sealed class SfcDependencyException : SmoException
    {
        public SfcDependencyException()
            : base()
        {
            Init();
        }

        public SfcDependencyException(string message)
            : base(message)
        {
            Init();
        }

        public SfcDependencyException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private SfcDependencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("SfcDependencyException");
        }

        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.SfcDependencyException;
            }
        }
    }

    /// <summary>
    /// This exception gets thrown when invalid scripting options are set
    /// </summary>
    [Serializable]
    public sealed class InvalidScriptingOptionException : SmoException
    {
        public InvalidScriptingOptionException()
            : base()
        {
            Init();
        }

        public InvalidScriptingOptionException(string message)
            : base(message)
        {
            Init();
        }

        public InvalidScriptingOptionException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private InvalidScriptingOptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("InvalidScriptingOptionException");
        }

        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.InvalidScriptingOptionException;
            }
        }
    }

    /// <summary>
    /// The exception thrown when script writing fails.
    /// </summary>
    [Serializable]
    public sealed class ScriptWriterException : SmoException
    {
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.ScriptWriterException.
        /// </summary>
        public ScriptWriterException()
            : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.ScriptWriterException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        public ScriptWriterException(string message)
            : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the T:Microsoft.SqlServer.Management.Smo.ScriptWriterException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public ScriptWriterException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private ScriptWriterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("ScriptWriterException");
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.ScriptWriterException;
            }
        }

    }

        /// <summary>
    /// The exception gets thrown when an operation is executed with an unsupported compatability
    /// level for that operation specified.
    /// </summary>
    [Serializable]
    public sealed class UnsupportedCompatLevelException : SmoException
    {
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedCompatLevelException.
        /// </summary>
        public UnsupportedCompatLevelException()
            : base()
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedCompatLevelException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        public UnsupportedCompatLevelException(string message)
            : base(message)
        {
            Init();
        }
        /// <summary>
        /// Constructor for the
        /// T:Microsoft.SqlServer.Management.Smo.UnsupportedCompatLevelException.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="innerException">The T:System.Exception instance that caused
        /// the current exception.</param>
        public UnsupportedCompatLevelException(string message, Exception innerException)
            : base(message, innerException)
        {
            Init();
        }

        private UnsupportedCompatLevelException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Init();
        }

        private void Init()
        {
            SetHelpContext("UnsupportedCompatLevelException");
        }
        /// <summary>
        /// Gets the type of exeption from the
        /// T:Microsoft.SqlServer.Management.Smo.SmoExceptionType enumeration.
        /// </summary>
        public override SmoExceptionType SmoExceptionType
        {
            get
            {
                return SmoExceptionType.UnsupportedCompatLevelException;
            }
        }
    }
}


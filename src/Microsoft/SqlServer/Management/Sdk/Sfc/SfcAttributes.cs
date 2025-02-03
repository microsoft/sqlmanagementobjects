// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Microsoft.SqlServer.Management.Sdk.Sfc.Metadata
{
    /// <summary>
    /// Various attribute helper methods
    /// </summary>
    internal class AttributeUtilities
    {
        /// <summary>
        /// Returns the argument values for a given instance
        /// </summary>
        /// <param name="instance">Instance for which property values need to be retrieved</param>
        /// <param name="properties">Names of properties</param>
        /// <returns>Array of property values</returns>
        public static object[] GetValuesOfProperties(object instance, string[] properties)
        {

            object[] returnArray = new object[properties.Length];
            int c = 0;
            foreach (string propertyName in properties)
            {
                object returnValue = GetValueOfProperty(instance, SplitNames(propertyName));
                
                if ((returnValue != null) && 
                    (returnValue.GetType() == typeof(string))
                    )
                {
                    returnValue = SfcSecureString.EscapeSquote((string)returnValue);
                }

                returnArray[c++] = returnValue;
            }
            return returnArray;
        }

        /// <summary>
        /// Gets the argument value based on instance and property name
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object GetValueOfProperty(object instance, string name)
        {

            PropertyInfo pi = null;  
            if ( !SfcMetadataDiscovery.TryGetCachedPropertyInfo(instance.GetType().TypeHandle, name, out pi))
            {
                try
                {
                    // First check inherited types as well
                    pi = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                }
                catch (AmbiguousMatchException)
                {
                    // But if we got multiple hits, recheck only the most derived type itself
                    pi = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                }
            }

            return pi.GetValue(instance, null);
        }

        /// <summary>
        /// Gets the argument value based on instance and property name list
        /// </summary>
        /// <param name="instance">Instance that holds the first property in the list</param>
        /// <param name="names">List of property names (such as Parent, Parent, Name)</param>
        /// <returns></returns>
        public static object GetValueOfProperty(object instance, List<String> names)
        {
            object i = instance;

            foreach (string name in names)
            {
                i = GetValueOfProperty(i, name);
            }
            return i;
        }

        /// <summary>
        /// Splits a propertyname into separate elements.
        /// Example: "Parent.Parent.Name"
        /// The properties are separated by a '.'
        /// The '.' may be escaped: "\."
        /// Example: "Dot\.Net"
        /// An escape may be escaped: "\\"
        /// Example: "Slash\\Dot"
        /// Any other escape is ignored: "\n" for example will not have a special meaning
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static List<String> SplitNames(string propertyName)
        {
            List<String> names = new List<String>();
            StringBuilder sb = new StringBuilder();

            int x = 0;
            bool esc = false;
            while (x < propertyName.Length)
            {
                char c = propertyName[x++];
                if (c == '\\')
                {
                    if (esc)
                    {
                        sb.Append(c);
                        esc = false;
                    }
                    else
                    {
                        esc = true;
                    }
                }
                else if (c == '.')
                {
                    if (esc)
                    {
                        sb.Append(c);
                        esc = false;
                    }
                    else
                    {
                        // property delimiter found
                        names.Add(sb.ToString());
                        sb = new StringBuilder();
                    }
                }
                else
                {
                    if (esc)
                    {
                        sb.Append('\\');
                        sb.Append(c);
                        esc = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                
            }
            if (esc) // string ended with \
            {
                sb.Append('\\');
            }
            if (sb.Length > 0)
            {
                names.Add(sb.ToString());
            }

            return names;
        }
    }


    /// <summary>
    /// Reference resolver delegate type for single object targets.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public delegate object ReferenceResolverDelegate(object instance, params object[] parameters);

    /// <summary>
    /// Reference resolver factory delegate type called to obtain a single target reference resolver interface.
    /// </summary>
    /// <param name="parameters">Any parameters needed for the resolver method.</param>
    /// <returns>The resolver interface which can be used to resolve from a source to a target.</returns>
    public delegate ISfcReferenceResolver SfcReferenceResolverFactoryDelegate(string[] parameters);

    /// <summary>
    /// Reference resolver factory delegate type called to obtain an enumerable target references resolver interface.
    /// </summary>
    /// <param name="parameters">Any parameters needed for the resolver method.</param>
    /// <returns>The resolver interface which can be used to resolve from a source to a target enumeration.</returns>
    public delegate ISfcReferenceCollectionResolver SfcReferenceCollectionResolverFactoryDelegate(string[] parameters);


    // TODO: some references may point to multipe types (DataType)
    // The reference attribute class and meta classes needs to be updated to support this

    /// <summary>
    /// Attribute for (soft) references to a single target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    [CLSCompliant(false)]
    public class SfcReferenceAttribute : Attribute
    {
        internal const string SfcReferenceResolverFactoryMethodName = "GetReferenceResolver";
        internal const string SfcReferenceCollectionResolverFactoryMethodName = "GetReferenceCollectionResolver";
        private string[] m_args = null;
        private string m_pathExpression = null;
        private ISfcReferenceResolver m_sfcReferenceResolver;
        private Delegate m_resolver = null;
        private Delegate m_urnResolver = null;
        private string[] m_keys = null;
        private Type m_type = null;

        /// <summary>
        /// Reference will be resolved by creating an instance of the given resolver type.
        /// </summary>
        /// <param name="resolverType">The type to instantiate a collection resolver from.</param>
        public SfcReferenceAttribute(Type resolverType)
            : this(resolverType, (string[])null)
        {
        }
        
        /// <summary>
        /// Reference will be resolved by creating an instance of the given resolver type with optional keys and parameters.
        /// </summary>
        /// <param name="resolverType">The type to instantiate a resolver from.</param>
        /// <param name="parameters">Any parameters needed for the resolver method.</param>
        public SfcReferenceAttribute(Type resolverType, string[] parameters)
        {
            if (resolverType == null)
            {
                throw new ArgumentNullException("resolverType", SfcStrings.SfcNullArgumentToSfcReferenceAttribute(typeof(SfcReferenceAttribute).Name));
            }
            this.m_args = parameters;

            // This particular ctor of SfcReference behaves like the SfcReferenceCollection ones.
            // It leaves the other patterns alone for back compat since they employ a delegate way of resolving,
            // whereas this ctor just calls a static factory method to get an interface to resolve with later on.

            // Parameters are passed to both the factory call and the eventual resolver call since we do not know
            // whether a particular resolver makes use of them in a one-time bound sense (factory call) or ongoing unbound sense (resolve call).
            // We also don't know or care whether a particular resolver factory makes a new instance each time or always uses the same one.
            // The preference is obviously to return a singleton if there is no private state kept from call to call.

            MethodInfo resolverFactoryMethod = resolverType.GetMethod(SfcReferenceAttribute.SfcReferenceResolverFactoryMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            // Not optional and a bug if not defined
            TraceHelper.Assert(resolverFactoryMethod != null, String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} resolver factory method on type '{1}' not found. Note: this method must be defined as a static method.", SfcReferenceAttribute.SfcReferenceResolverFactoryMethodName, resolverType.FullName));

            Delegate factory = resolverFactoryMethod.CreateDelegate(typeof(SfcReferenceResolverFactoryDelegate));

            this.m_sfcReferenceResolver = ((SfcReferenceResolverFactoryDelegate)factory)(parameters) as ISfcReferenceResolver;
            if (this.m_sfcReferenceResolver == null)
            {
                throw new InvalidOperationException(SfcStrings.SfcNullInvalidSfcReferenceResolver(resolverType.Name, typeof(ISfcReferenceResolver).Name));
            }
        }

        /// <summary>
        /// Reference will be resolved with values of passed in property names
        /// </summary>
        /// <param name="urnTemplate"></param>
        /// <param name="parameters">Parameters for reference type</param>
        /// <param name="referenceType"></param>
        public SfcReferenceAttribute(Type referenceType, string urnTemplate, params string[] parameters)
        {
            m_pathExpression = urnTemplate;
            m_args = parameters;
            m_type = referenceType;
        }

        /// <summary>
        /// Reference will be resolved by calling a delegate
        /// </summary>
        /// <param name="methodName">A static resolver method.</param>
        /// <param name="parameters">Arguments that will be passed into the method.</param>
        /// <param name="referenceType"></param>
        /// <param name="resolverType"></param>
        public SfcReferenceAttribute(Type referenceType, Type resolverType, string methodName, params string[] parameters)
            : this(referenceType,null,resolverType,methodName,parameters)
        {
        }

        /// <summary>
        /// Reference will be resolved with values of passed in property names
        /// </summary>
        /// <param name="referenceType"></param>
        /// <param name="keys">All keys if this is a multi-key reference</param>
        /// <param name="urnTemplate"></param>
        /// <param name="parameters">Parameters for path expression</param>
        public SfcReferenceAttribute(Type referenceType, string[] keys, string urnTemplate, params string[] parameters)
        {
            m_keys = keys;
            m_pathExpression = urnTemplate;
            m_args = parameters;
            m_type = referenceType;
        }

        /// <summary>
        /// Reference will be resolved by calling a delegate
        /// </summary>
        /// <param name="referenceType"></param>
        /// <param name="keys">All keys if this is a multi-key reference</param>
        /// <param name="resolverType">Type on which resolver exists.</param>
        /// <param name="methodName">A static resolver method.</param>
        /// <param name="parameters">Arguments that will be passed into the method.</param>
        public SfcReferenceAttribute(Type referenceType, string[] keys, Type resolverType, string methodName, params string[] parameters)
        {
            m_keys = keys;

            MethodInfo resolver = resolverType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            // Not optional and a bug if not defined
            TraceHelper.Assert(resolver != null, String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} method on type {1} not found. Note: this method must be defined as a static method.", methodName, resolverType.FullName));

            m_resolver = resolver.CreateDelegate(typeof(ReferenceResolverDelegate));

            resolver = resolverType.GetMethod("ResolveUrn", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            // If the type implemented a URN resolver, then create the delegate for it too
            if (resolver != null)
            {
                m_urnResolver = resolver.CreateDelegate(typeof(ReferenceResolverDelegate));
            }

            m_args = parameters;
            m_type = referenceType;
        }

        /// <summary>
        /// Urn template that takes the form of a String.Format string. Example "Server/Login[@Name = '{0}']".
        /// </summary>
        public string UrnTemplate
        {
            get { return m_pathExpression; }
        }

        /// <summary>
        /// Arguments for the urnTemplate.
        /// </summary>
        public string[] Arguments
        {
            get { return m_args; }
        }

        /// <summary>
        /// Custom resolver method.
        /// </summary>
        public Delegate Resolver
        {
            get { return m_resolver; }
        }
        
        /// <summary>
        /// Custom resolver interface.
        /// </summary>
        public ISfcReferenceResolver InstanceResolver
        {
            get { return this.m_sfcReferenceResolver; }
        }

        /// <summary>
        /// The keys that participate in the reference.
        /// </summary>
        public string[] Keys
        {
            get
            {
                return m_keys;
            }
            set
            {
                m_keys = value;
            }
        }

        /// <summary>
        /// The type of the reference
        /// </summary>
        public System.Type Type
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type = value;
            }
        }

        /// <summary>
        /// Resolve a strongly-typed single target object from the given source object.
        /// If a custom resolver interface is defined, it uses that.
        /// Otherwise if a custom resolver delegate helper is defined it uses that.
        /// In the absence of custom resolvers, it evaluates the given Urn path expression template with arguments.
        /// </summary>
        /// <typeparam name="S">The type of the source instance to resolve from.</typeparam>
        /// <typeparam name="T">The type of the target instance.</typeparam>
        /// <param name="instance">The source instance to resolve from.</param>
        /// <returns>The resolved target instance according to the rules mentioned, or null if there is no custom resolver or valid Urn path given.</returns>
        public T Resolve<T, S>(S instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance", SfcStrings.SfcNullArgumentToResolve);
            }

            if (this.m_sfcReferenceResolver != null)
            {
                // It is a custom resolver interface
                var resolver = this.m_sfcReferenceResolver as ISfcReferenceResolver<T, S>;
                if (resolver == null)
                {
                    // The resolver isn't really of the generic type requested so we have to throw
                    throw new InvalidOperationException(SfcStrings.SfcNullInvalidSfcReferenceResolver(this.m_sfcReferenceResolver.GetType().Name, typeof(ISfcReferenceResolver<T, S>).Name));
                }
                return resolver.Resolve(instance, this.m_args);
            }

            // Must be a custom resolver delegate, or a Urn path-style resolve
            return (T)this.ResolveDelegateOrPath(instance);
        }

        /// <summary>
        /// Resolve a single target object from the given source object.
        /// If a custom resolver interface is defined, it uses that.
        /// Otherwise if a custom resolver delegate helper is defined it uses that.
        /// In the absence of custom resolvers, it evaluates the given Urn path expression template with arguments.
        /// </summary>
        /// <param name="instance">The source instance to resolve from.</param>
        /// <returns>The resolved target instance according to the rules mentioned, or null if there is no custom resolver or valid Urn path given.</returns>
        public object Resolve(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance", SfcStrings.SfcNullArgumentToResolve);
            }

            // Resolve using the interface
            if (m_sfcReferenceResolver != null)
            {
                return this.m_sfcReferenceResolver.Resolve(instance, this.m_args);
            }

            // Must be a custom resolver delegate, or a Urn path-style resolve
            return this.ResolveDelegateOrPath(instance);
        }

        private object ResolveDelegateOrPath(object instance)
        {
            // Resolve using a helper
            if (m_resolver != null)
            {
                return ((ReferenceResolverDelegate)m_resolver)(instance, AttributeUtilities.GetValuesOfProperties(instance, m_args));
            }

            // Resolve by path expression
            object result = null;
            if (m_pathExpression != null)
            {
                if (m_args != null && m_args.Length != 0)
                {
                    SfcObjectQuery objectQuery = null;
                    string urnString = String.Format(CultureInfo.InvariantCulture, m_pathExpression, AttributeUtilities.GetValuesOfProperties(instance, m_args));
                    Urn objectUrn = new Urn(urnString);
                    SqlStoreConnection sfcSqlConnection = null;
                    ISfcDomain sfcDomain = null;
                    //First extract information from the instance
                    if (instance is IAlienObject)
                    {
                        IAlienObject alien = instance as IAlienObject;
                        return alien.Resolve(urnString);
                    }
                    else if (instance is SfcInstance)
                    {
                        sfcDomain = ((SfcInstance)instance).KeyChain.Domain;

                        // Guard against disconnected use,
                        // so we cannot Resolve reference by Urn path unless we are connected
                        // (i.e., we cannot process in-memory XPath navigation and such).
                        if (sfcDomain.ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                        {
                            return null;
                        }
                        sfcSqlConnection = sfcDomain.GetConnection() as SqlStoreConnection;
                    }
                    else
                    {
                        //TODO: Throw?
                    }

                    string domainRoot = objectUrn.XPathExpression[0].Name;
                    if(string.Compare(domainRoot,"Server",StringComparison.OrdinalIgnoreCase) == 0)//SMO
                    {
                        TraceHelper.Assert(false); // can't happen any more
                    }
                    else
                    {
                        foreach(SfcDomainInfo domain in SfcRegistration.Domains)
                        {
                            if(string.Compare(domainRoot,domain.Name,StringComparison.OrdinalIgnoreCase) == 0)//SFC Based Domains
                            {
                                sfcDomain = domain.RootType.Assembly().CreateInstance(domain.RootTypeFullName,
                                    false,BindingFlags.CreateInstance,null, new object[] { sfcSqlConnection },
                                    System.Globalization.CultureInfo.InvariantCulture, null) as ISfcDomain;
                                objectQuery = new SfcObjectQuery(sfcDomain);
                                break;
                            }
                        }
                        if (objectQuery == null)
                        {
                            //TODO: Throw?
                        }
                    }

                    int countObjects = 0;
                    foreach (object obj in objectQuery.ExecuteIterator(new SfcQueryExpression(urnString), null, null))
                    {
                        result = obj;
                        countObjects++;
                    }
                    if (countObjects > 1)
                    {
                        //TODO: Throw?
                    }
                }
            }
            return result; // for now
        }

        /// <summary>
        /// Returns the Urn reference. Returns null if the attribute does not have a pathexpression defined.
        /// </summary>
        public Urn GetUrn(object instance)
        {
            try
            {
                // Resolve using a helper
                if (m_urnResolver != null)
                {
                    return (Urn)((ReferenceResolverDelegate)m_urnResolver)(instance, AttributeUtilities.GetValuesOfProperties(instance, m_args));
                }
                if (m_pathExpression == null)
                {
                    return null;
                }

                string urnString = String.Format(m_pathExpression, AttributeUtilities.GetValuesOfProperties(instance, m_args));
                return new Urn(urnString);
            }
            catch(TargetInvocationException e)
            {
               if (e.InnerException != null && 0 == String.Compare(e.InnerException.GetType().FullName, "Microsoft.SqlServer.Management.Smo.UnknownPropertyException", StringComparison.Ordinal))
               {
                   throw new SfcUnsupportedVersionException(SfcStrings.PropertyNotsupported, e);
               }
               else
               {
                   throw;
               }
            }
            
            
        }
    }

    /// <summary>
    /// Attribute that allows valid values to be returned that can be applied to
    /// a property that is also a soft reference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    [CLSCompliant(false)]
    public class SfcReferenceSelectorAttribute : Attribute
    {
        private string[] m_args = null;
        private string m_pathExpression = null;
        private string m_field = null;

        public SfcReferenceSelectorAttribute(string pathExpression, string field, params string[] parameters)
        {
            m_pathExpression = pathExpression;
            m_field = field;
            m_args = parameters;
        }

        /// <summary>
        /// Path expression that is used to resolve the reference
        /// </summary>
        public string PathExpression
        {
            get { return m_pathExpression; }
        }

        /// <summary>
        /// Arguments for the PathExpression
        /// </summary>
        public object[] Arguments
        {
            get { return m_args; }
        }

        /// <summary>
        /// The field that needs to be returned by the query
        /// </summary>
        public string Field
        {
            get { return m_field; }
        }

#if false // CC_NOT_USED
// Not used, and broken.
        public List<object> Select(object instance) // TODO: this needs to be List<ISfcInstance>
        {
            // Resolve by path expression
            if (m_pathExpression != null)
            {
                if (m_args != null && m_args.Length != 0)
                {
                    string urnString = String.Format(m_pathExpression, AttributeUtilities.GetValuesOfProperties(instance, m_args));
                    // TODO: query domain for list of objects. Don't forget to narrow the results by field.
                }
            }
            return null; // for now
        }
#endif
    }

    
#region SfcReferenceCollectionAttribute

    /// <summary>
    /// Attribute for (soft) references to an enumerable target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    [CLSCompliant(false)]
    public class SfcReferenceCollectionAttribute : Attribute
    {
        private ISfcReferenceCollectionResolver resolver = null;
        private string[] args = null;

        /// <summary>
        /// Reference collection will be resolved by the given resolver object.
        /// </summary>
        /// <param name="resolverType">The type to instantiate a collection resolver from.</param>
        public SfcReferenceCollectionAttribute(Type resolverType)
            : this(resolverType, null, null)
        {
        }
            
        /// <summary>
        /// Reference collection will be resolved by creating an instance of the given resolver type with optional keys and parameters.
        /// </summary>
        /// <param name="resolverType">The type to instantiate a collection resolver from.</param>
        /// <param name="parameters">Any parameters needed for the resolve.</param>
        public SfcReferenceCollectionAttribute(Type resolverType, params string[] parameters)
        {
            if (resolverType == null)
            {
                throw new ArgumentNullException("resolverType", SfcStrings.SfcNullArgumentToSfcReferenceAttribute(typeof(SfcReferenceCollectionAttribute).Name));
            }
            this.args = parameters;

            // This ctor just calls a static factory method to get an interface to resolve with later on.

            // Parameters are passed to both the factory call and the eventual resolver call since we do not know
            // whether a particular resolver makes use of them in a one-time bound sense (factory call) or ongoing unbound sense (resolve call).
            // We also don't know or care whether a particular resolver factory makes a new instance each time or always uses the same one.
            // The preference is obviously to return a singleton if there is no private state kept from call to call.

            MethodInfo resolverFactoryMethod = resolverType.GetMethod(SfcReferenceAttribute.SfcReferenceCollectionResolverFactoryMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            // Not optional and a bug if not defined
            TraceHelper.Assert(resolverFactoryMethod != null, String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} resolver factory method on type '{1}' not found. Note: this method must be defined as a static method.", SfcReferenceAttribute.SfcReferenceCollectionResolverFactoryMethodName, resolverType.FullName));

            Delegate factory = resolverFactoryMethod.CreateDelegate(typeof(SfcReferenceCollectionResolverFactoryDelegate));

            this.resolver = ((SfcReferenceCollectionResolverFactoryDelegate)factory)(parameters) as ISfcReferenceCollectionResolver;
            if (this.resolver == null)
            {
                throw new InvalidOperationException(SfcStrings.SfcNullInvalidSfcReferenceResolver(resolverType.Name, typeof(ISfcReferenceCollectionResolver).Name));
            }
        }

        /// <summary>
        /// Custom resolver interface.
        /// </summary>
        public ISfcReferenceCollectionResolver CollectionResolver
        {
            get { return this.resolver; }
        }
        
        /// <summary>
        /// Arguments for the resolver.
        /// </summary>
        public string[] Arguments
        {
            get { return this.args; }
        }

        /// <summary>
        /// Resolve a target enumerable from the given source object.
        /// </summary>
        /// <param name="instance">The source instance to resolve from.</param>
        /// <returns>The resolved target enumerable.</returns>
        public IEnumerable ResolveCollection(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance", SfcStrings.SfcNullArgumentToResolveCollection);
            }

            // Resolve with the client's instance collection resolver.
            // TODO: If you want a "Urn stencil" style of resolving, create one that does that and pass it to the attribute ctor.

            return this.resolver.ResolveCollection(instance, this.args);
        }

        /// <summary>
        /// Resolve a strongly-typed target enumerable from the given source object.
        /// </summary>
        /// <typeparam name="S">The type of the source instance to resolve from.</typeparam>
        /// <typeparam name="T">The type of the target instance.</typeparam>
        /// <param name="instance">The source instance to resolve from.</param>
        /// <returns>The resolved target enumerable.</returns>
        public IEnumerable<T> ResolveCollection<T, S>(S instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance", SfcStrings.SfcNullArgumentToResolveCollection);
            }

            // Resolve with the client's instance collection resolver.
            // TODO: If you want a "Urn stencil" style of resolving, create one that does that and pass it to the attribute ctor.

            var resolver = this.resolver as ISfcReferenceCollectionResolver<T, S>;
            if (resolver == null)
            {
                // The resolver isn't really of the generic type requested so we have to throw
                throw new InvalidOperationException(SfcStrings.SfcNullInvalidSfcReferenceResolver(this.resolver.GetType().Name, typeof(ISfcReferenceCollectionResolver<T, S>).Name));
            }
            return resolver.ResolveCollection(instance, this.args);
        }


    }

#endregion

    /// <summary>
    /// Base class for various attribute classes. May be used directly, but typically one of the derived form is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public abstract class SfcRelationshipAttribute : Attribute
    {
        private SfcRelationship m_relationship = SfcRelationship.None;
        private SfcCardinality m_cardinality = SfcCardinality.None;
        private Type m_containsType = null;

        protected SfcRelationshipAttribute()
        {
            // Use defaults.
        }

        /// <param name="relationship">Specifies the relationship to its parent</param>
        protected SfcRelationshipAttribute(SfcRelationship relationship)
        {
            m_relationship = relationship;
        }

        /// <param name="relationship">Specifies the relationship to its parent</param>
        /// <param name="cardinality">Specifies the cardinality of the type in relationship to its parent</param>
        protected SfcRelationshipAttribute(SfcRelationship relationship, SfcCardinality cardinality)
        {
            m_relationship = relationship;
            m_cardinality = cardinality;
        }

        /// <param name="relationship">Specifies the relationship to its parent</param>
        /// <param name="cardinality">Specifies the cardinality of the type in relationship to its parent</param>
        /// <param name="containsType">Specifies the type the container holds</param>
        protected SfcRelationshipAttribute(SfcRelationship relationship, SfcCardinality cardinality, Type containsType)
        {
            m_relationship = relationship;
            m_cardinality = cardinality;
            m_containsType = containsType;
        }

        /// <summary>
        /// Specifies the relationship to its parent
        /// </summary>
        public SfcRelationship Relationship
        {
            get
            {
                return m_relationship;
            }
            internal set
            {
                m_relationship = value;
            }
        }

        /// <summary>
        /// Specifies the cardinality of the type in relationship to its parent
        /// </summary>
        public SfcCardinality Cardinality
        {
            get
            {
                return m_cardinality;
            }
            internal set
            {
                m_cardinality = value;
            }
        }

        /// <summary>
        /// Specifies the type this container holds. Only available for containers.
        /// </summary>
        public Type ContainsType
        {
            get
            {
                return m_containsType;
            }
            internal set
            {
                m_containsType = value;
            }
        }
    }

    public enum SfcContainerRelationship
    {
        ObjectContainer = 0,
        ChildContainer,
    }

    public enum SfcContainerCardinality
    {
        ZeroToAny = 0,
        OneToAny,
    }


    /// <summary>
    /// Relationship types for objects
    /// </summary>
    public enum SfcObjectRelationship
    {
        Object = 0,
        ParentObject,
        ChildObject,
    }

    /// <summary>
    /// Cardinality for objects
    /// </summary>
    public enum SfcObjectCardinality
    {
        One = 0,
        ZeroToOne,
    }

    /// <summary>
    /// Attribute for object relationships (such a Server.FullTextService)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SfcObjectAttribute : SfcRelationshipAttribute
    {
        private SfcObjectFlags m_flags = SfcObjectFlags.None;

        public SfcObjectFlags Flags
        {
            get { return m_flags; }
            set { m_flags = value; }
        }

        public bool Design
        {
            get { return (m_flags & SfcObjectFlags.Design) == SfcObjectFlags.Design; }
            set { m_flags = value ? m_flags | SfcObjectFlags.Design : m_flags & ~SfcObjectFlags.Design; }
        }

        public bool Deploy
        {
            get { return (m_flags & SfcObjectFlags.Deploy) == SfcObjectFlags.Deploy; }
            set { m_flags = value ? m_flags | SfcObjectFlags.Deploy : m_flags & ~SfcObjectFlags.Deploy; }
        }

        public bool NaturalOrder
        {
            get { return (m_flags & SfcObjectFlags.NaturalOrder) == SfcObjectFlags.NaturalOrder; }
            set
            {
                m_flags = value ? m_flags | SfcObjectFlags.NaturalOrder : m_flags & ~SfcObjectFlags.NaturalOrder;
            }
        }

        // For object
        public SfcObjectAttribute(SfcObjectFlags flags)
            : this(SfcObjectRelationship.Object, SfcObjectCardinality.One, flags)
        {
            this.Flags = flags;
        }

        // For object
        public SfcObjectAttribute()
            : this(SfcObjectFlags.None)
        {
        }

        // For object
        public SfcObjectAttribute(SfcObjectCardinality cardinality, SfcObjectFlags flags)
            : this(SfcObjectRelationship.Object, cardinality)
        {
            this.Flags = flags;
        }

        // For object
        public SfcObjectAttribute(SfcObjectCardinality cardinality)
            : this(cardinality, SfcObjectFlags.None)
        {
        }

        // For object/childobject
        public SfcObjectAttribute(SfcObjectRelationship relationship, SfcObjectFlags flags)
        {
            SfcRelationship r = SfcRelationship.None;

            switch (relationship)
            {
                case SfcObjectRelationship.ChildObject:
                    r = SfcRelationship.ChildObject;
                    break;
                case SfcObjectRelationship.Object:
                    r = SfcRelationship.Object;
                    break;
                case SfcObjectRelationship.ParentObject:
                    r = SfcRelationship.ParentObject;
                    break;
            }
            this.Relationship = r;
            this.Cardinality = SfcCardinality.One;
            this.Flags = flags;
        }

        // For object/childobject
        public SfcObjectAttribute(SfcObjectRelationship relationship)
            : this(relationship, SfcObjectFlags.None)
        {
        }

        // For object/childobject
        public SfcObjectAttribute(SfcObjectRelationship relationship, SfcObjectCardinality cardinality, SfcObjectFlags flags)
        {
            SfcRelationship r = SfcRelationship.None;
            SfcCardinality c = SfcCardinality.None;

            switch (relationship)
            {
                case SfcObjectRelationship.ChildObject:
                    r = SfcRelationship.ChildObject;
                    break;
                case SfcObjectRelationship.Object:
                    r = SfcRelationship.Object;
                    break;
                case SfcObjectRelationship.ParentObject:
                    r = SfcRelationship.ParentObject;
                    break;
            }
            this.Relationship = r;

            switch (cardinality)
            {
                case SfcObjectCardinality.One:
                    c = SfcCardinality.One;
                    break;
                case SfcObjectCardinality.ZeroToOne:
                    c = SfcCardinality.ZeroToOne;
                    break;
            }
            this.Cardinality = c;
            this.Flags = flags;
        }

        // For object/childobject
        public SfcObjectAttribute(SfcObjectRelationship relationship, SfcObjectCardinality cardinality)
            : this(relationship, cardinality, SfcObjectFlags.None)
        {
        }

        // For containers
        public SfcObjectAttribute(Type containsType, SfcObjectFlags flags)
        {
            this.Cardinality = SfcCardinality.ZeroToAny;
            this.ContainsType = containsType;
            this.Relationship = SfcRelationship.ObjectContainer;
            this.Flags = flags;
        }

        // For containers
        public SfcObjectAttribute(Type containsType)
            : this(containsType, SfcObjectFlags.None)
        {
        }

        // For containers
        public SfcObjectAttribute(SfcContainerCardinality cardinality, Type containsType, SfcObjectFlags flags)
        {
            SfcCardinality c = SfcCardinality.None;

            switch (cardinality)
            {
                case SfcContainerCardinality.OneToAny:
                    c = SfcCardinality.OneToAny;
                    break;
                case SfcContainerCardinality.ZeroToAny:
                    c = SfcCardinality.ZeroToAny;
                    break;
            }
            this.Cardinality = c;
            this.ContainsType = containsType;
            this.Relationship = SfcRelationship.ObjectContainer;
            this.Flags = flags;
        }

        // For containers
        public SfcObjectAttribute(SfcContainerCardinality cardinality, Type containsType)
            : this(cardinality, containsType, SfcObjectFlags.None)
        {
        }


        // For containers
        public SfcObjectAttribute(SfcContainerRelationship relationship, SfcContainerCardinality cardinality, Type containsType, SfcObjectFlags flags)
        {
            SfcRelationship r = SfcRelationship.None;
            SfcCardinality c = SfcCardinality.None;

            switch (relationship)
            {
                case SfcContainerRelationship.ChildContainer:
                    r = SfcRelationship.ChildContainer;
                    break;
                case SfcContainerRelationship.ObjectContainer:
                    r = SfcRelationship.ObjectContainer;
                    break;
            }
            this.Relationship = r;

            switch (cardinality)
            {
                case SfcContainerCardinality.OneToAny:
                    c = SfcCardinality.OneToAny;
                    break;
                case SfcContainerCardinality.ZeroToAny:
                    c = SfcCardinality.ZeroToAny;
                    break;
            }
            this.Cardinality = c;
            this.ContainsType = containsType;
            this.Flags = flags;
        }

        // For containers
        public SfcObjectAttribute(SfcContainerRelationship relationship, SfcContainerCardinality cardinality, Type containsType)
            : this(relationship, cardinality, containsType, SfcObjectFlags.None)
        {
        }
    }


    /// <summary>
    /// Indicates this property or class is to be ignored
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SfcIgnoreAttribute : SfcRelationshipAttribute
    {
        public SfcIgnoreAttribute()
            : base(SfcRelationship.Ignore)
        {
        }
    }

    /// <summary>
    /// Attribute for excluding the resolve for some properties for some types
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class SfcInvalidForTypeAttribute : Attribute
    {
        private Type excludedType = null;
        /// <summary>
        /// Exclude list will be set to the list coming for the constructor
        /// </summary>
        /// <param name="excludedType">The excluded type for a specific property</param>
        public SfcInvalidForTypeAttribute(Type excludedType)
        {
            this.excludedType = excludedType;
        }

        /// <summary>
        /// Get the excluded types list.
        /// </summary>
        public Type ExcludedType
        {
            get
            {
                return this.excludedType;
            }
        }
    }

    /// <summary>
    /// Attribute to skip serialization of properties. Currently, the attribute only applies to container type relationships.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SfcNonSerializableAttribute : Attribute
    {
        public SfcNonSerializableAttribute()
        {
        }
    }

    /// <summary>
    /// Attribute for regular properties, which data is to be managed by the defining class.
    /// These are typically scalar properties, but can be of arbitrary complexity.
    /// Example: Database.Size, Table.Name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SfcPropertyAttribute : Attribute
    {
        private SfcPropertyFlags m_flags;
        private string m_defaultValue;

        public SfcPropertyAttribute()
            : this(SfcPropertyFlags.Required, string.Empty)
        {
        }

        public SfcPropertyAttribute(SfcPropertyFlags flags)
            : this(flags, string.Empty)
        {
        }

        public SfcPropertyAttribute(SfcPropertyFlags flags, string defaultValue)
        {
            m_flags = flags;
            m_defaultValue = defaultValue;
        }

        public SfcPropertyFlags Flags
        {
            get { return m_flags; }
            set { m_flags = value; }
        }

        public bool Computed
        {
            get { return (m_flags & SfcPropertyFlags.Computed) == SfcPropertyFlags.Computed; }
            set 
            {
                if (value)
                {
                    Required = false;
                }
                m_flags = value ? m_flags | SfcPropertyFlags.Computed : m_flags & ~SfcPropertyFlags.Computed; 
            }
        }

        public bool Data
        {
            get { return (m_flags & SfcPropertyFlags.Data) == SfcPropertyFlags.Data; }
            set 
            {
                if (value)
                {
                    Required = false;
                }
                m_flags = value ? m_flags | SfcPropertyFlags.Data : m_flags & ~SfcPropertyFlags.Data; 
            }
        }

        public bool Encrypted
        {
            get { return (m_flags & SfcPropertyFlags.Encrypted) == SfcPropertyFlags.Encrypted; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.Encrypted : m_flags & ~SfcPropertyFlags.Encrypted; }
        }

        public bool Expensive
        {
            get { return (m_flags & SfcPropertyFlags.Expensive) == SfcPropertyFlags.Expensive; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.Expensive : m_flags & ~SfcPropertyFlags.Expensive; }
        }

        public bool Standalone
        {
            get { return (m_flags & SfcPropertyFlags.Standalone) == SfcPropertyFlags.Standalone; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.Standalone : m_flags & ~SfcPropertyFlags.Standalone; }
        }

        public bool SqlAzureDatabase
        {
            get { return (m_flags & SfcPropertyFlags.SqlAzureDatabase) == SfcPropertyFlags.SqlAzureDatabase; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.SqlAzureDatabase : m_flags & ~SfcPropertyFlags.SqlAzureDatabase; }
        }

        public bool ReadOnlyAfterCreation
        {
            get { return (m_flags & SfcPropertyFlags.ReadOnlyAfterCreation) == SfcPropertyFlags.ReadOnlyAfterCreation; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.ReadOnlyAfterCreation : m_flags & ~SfcPropertyFlags.ReadOnlyAfterCreation; }
        }

        public bool Required
        {
            get { return (m_flags & SfcPropertyFlags.Required) == SfcPropertyFlags.Required; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.Required : m_flags & ~SfcPropertyFlags.Required; }
        }

        public bool Design
        {
            get { return (m_flags & SfcPropertyFlags.Design) == SfcPropertyFlags.Design; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.Design : m_flags & ~SfcPropertyFlags.Design; }
        }

        public bool Deploy
        {
            get { return (m_flags & SfcPropertyFlags.Deploy) == SfcPropertyFlags.Deploy; }
            set { m_flags = value ? m_flags | SfcPropertyFlags.Deploy : m_flags & ~SfcPropertyFlags.Deploy; }
        }

        public string DefaultValue
        {
            get { return m_defaultValue; }
        }
    }

    /// <summary>
    /// Attribute to specify a identifying key (such a Database.Name)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SfcKeyAttribute : Attribute
    {
        private int m_position = 0;

        public SfcKeyAttribute()
        {
            // Use default
        }

        public SfcKeyAttribute(int position)
        {
            m_position = position;
        }

        /// <summary>
        /// Key ordinal
        /// </summary>
        public int Position
        {
            get { return m_position; }
        }
    }

    /// <summary>
    /// Attribute to specify a valid possible parent of a Type.
    /// Use one of these for each possible parent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class SfcParentAttribute : Attribute
    {
        private string m_parentName = String.Empty;

        public SfcParentAttribute()
        {
            // Use default
        }

        public SfcParentAttribute(string parentName)
        {
            m_parentName = parentName;
        }

        /// <summary>
        /// The possible parent type name. The name can be namespace qualified, or if it is not then the
        /// namespace of the type containing this atrribute will be used.
        /// </summary>
        public string Parent
        {
            get { return m_parentName; }
        }
    }

    /// <summary>
    /// Attribute class to specify supported version of a property or class
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SfcVersionAttribute : Attribute
    {
        private Version m_begin = null, m_end = null;

        /// <param name="beginMajor"></param>
        /// <param name="beginMinor"></param>
        /// <param name="beginBuild"></param>
        /// <param name="beginRevision"></param>
        /// <param name="endMajor"></param>
        /// <param name="endMinor"></param>
        /// <param name="endBuild"></param>
        /// <param name="endRevision"></param>
        public SfcVersionAttribute(int beginMajor, int beginMinor, int beginBuild, int beginRevision, int endMajor, int endMinor, int endBuild, int endRevision)
        {
            m_begin = new Version(beginMajor, beginMinor, beginBuild, beginRevision);
            m_end = new Version(endMajor, endMinor, endBuild,endRevision);
        }

        /// <param name="beginMajor"></param>
        /// <param name="beginMinor"></param>
        /// <param name="beginBuild"></param>
        /// <param name="beginRevision"></param>
        public SfcVersionAttribute(int beginMajor, int beginMinor, int beginBuild, int beginRevision)
        {
            m_begin = new Version(beginMajor, beginMinor, beginBuild, beginRevision);
        }

        public SfcVersionAttribute(int beginMajor, int endMajor)
        {
            m_begin = new Version(beginMajor, 0);
            m_end = new Version(endMajor, 0);
        }

        public SfcVersionAttribute(int beginMajor)
        {
            m_begin = new Version(beginMajor, 0);
        }

        /// <summary>
        /// Version on which support for this property started. Null if start version is undefined.
        /// </summary>
        public System.Version BeginVersion
        {
            get
            {
                return m_begin;
            }
        }

        /// <summary>
        /// Version on which support for this property ended. Null if open-ended.
        /// </summary>
        public System.Version EndVersion
        {
            get
            {
                return m_end;
            }
        }
    }

    /// <summary>
    /// Attribute class to specify supported SKU of a property or class
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SfcSkuAttribute : Attribute
    {
        private string[] m_skus = null;
        private bool m_exclusive = false;

        /// <param name="skuName">SKU name.</param>
        /// <param name="exclusive">Indicates whether the SKU that is specified, is exclusive or inclusive.</param>
        public SfcSkuAttribute(string skuName, bool exclusive)
        {
            m_skus = new string[1];
            m_skus[0] = skuName;
            m_exclusive = exclusive;
        }

        /// <param name="skuNames">List of SKU names.</param>
        /// <param name="exclusive">Indicates whether the SKU's that are specified, are exclusive or inclusive.</param>
        public SfcSkuAttribute(string[] skuNames, bool exclusive)
        {
            m_skus = skuNames;
            m_exclusive = exclusive;
        }

        /// <summary>
        /// List of SKUs
        /// </summary>
        public string[] SkuNames
        {
            get
            {
                return m_skus;
            }
        }

        /// <summary>
        /// Indicates whether the SKU's that are specified, are exclusive support of the property or inclusive.
        /// </summary>
        public bool Exclusive
        {
            get
            {
                return m_exclusive;
            }
        }
    }

    /// <summary>
    /// Attribute that is used when the type name is different from the Enumerator type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SfcElementTypeAttribute : Attribute
    {
        private string m_elementTypeName;

        /// <summary>
        /// Creates a new instance of the SfcElementTypeAttribute
        /// </summary>
        /// <param name="elementTypeName">The element type name</param>
        public SfcElementTypeAttribute(string elementTypeName)
        {
            m_elementTypeName = elementTypeName;
        }

        public string ElementTypeName
        {
            get { return m_elementTypeName; }
            set { m_elementTypeName = value; }
        }
    }

    /// <summary>
    /// Attribute for regular sfc elements.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    internal class SfcElementAttribute : Attribute
    {
        private SfcElementFlags m_flags;

        /// <summary>
        /// Creates a new instance of the SfcElementAttribute
        /// </summary>
        /// <param name="flags"></param>
        public SfcElementAttribute(SfcElementFlags flags)
        {
            m_flags = flags;
        }

        public bool Standalone
        {
            get { return (m_flags & SfcElementFlags.Standalone) == SfcElementFlags.Standalone; }
            set { m_flags = value ? m_flags | SfcElementFlags.Standalone : m_flags & ~SfcElementFlags.Standalone; }
        }

        public bool SqlAzureDatabase
        {
            get { return (m_flags & SfcElementFlags.SqlAzureDatabase) == SfcElementFlags.SqlAzureDatabase; }
            set { m_flags = value ? m_flags | SfcElementFlags.SqlAzureDatabase : m_flags & ~SfcElementFlags.SqlAzureDatabase; }
        }
    }

    /// <summary>
    /// Indicates whether Powershell (OE?) should be able to browse (cd) into this node
    /// The default is true (no attribute means true).
    /// If set to false, then the node is not visible in Powershell (OE?)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class SfcBrowsableAttribute : Attribute
    {
        private bool m_isBrowsable = true;

        /// <summary>
        /// Creates a new instance of the SfcElementTypeAttribute
        /// </summary>
        /// <param name="isBrowsable"></param>
        public SfcBrowsableAttribute(bool isBrowsable)
        {
            m_isBrowsable = isBrowsable;
        }

        public bool IsBrowsable
        {
            get { return m_isBrowsable; }
            set { m_isBrowsable = value; }
        }
    }

    /// <summary>
    /// Attribute to specify the type of serialization adapter to use for a particular property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SfcSerializationAdapterAttribute : Attribute
    {
        private Type sfcSerializationAdapterType;

        public SfcSerializationAdapterAttribute(Type adapterType)
        {
            //the adapter type should be a valid one. Otherwise this attribute shouldn't be used.
            if (adapterType == null)
            {
                throw new ArgumentNullException("adapterType");
            }

            this.sfcSerializationAdapterType = adapterType;
        }

        /// <summary>
        /// The type of the serialization adapter
        /// </summary>
        public Type SfcSerializationAdapterType
        {
            get { return this.sfcSerializationAdapterType; }
        }
    }
}


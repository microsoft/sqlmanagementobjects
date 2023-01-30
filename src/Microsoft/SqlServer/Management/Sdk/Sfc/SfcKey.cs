// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// The SfcKey class is the base class for all nested Key classes which every SfcInstance-derived object must have.
    /// It implements equality and hashing but not comparison, since Keys are only required to know if they are equal or not.
    /// 
    /// Keys are meant to be immutable once constructed, hence a Key should not contain data which can change during the Key object's lifetime.
    /// Do not use field(s) as part of a Key which are not truly part of the identity the Key represents. We may enforce this by disallowing set()
    /// on the internal properties for the Key.
    /// 
    /// Any ordering or collation needs are addressed by implementing:
    /// 1. IComparable on the SfcKey-derived class
    /// 2. IComparer or IComparer&lt;T> on the SfcCollection-derived collection class
    /// </summary>
    public abstract class SfcKey : IEquatable<SfcKey>
    {
        #region Object methods to override
        /// <summary>
        /// Each Key must do proper value comparison of its data members.
        /// Do *not* rely solely on reference equality checking like object does.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public abstract override bool Equals(object obj);

        /// <summary>
        /// Each Key must provide a reasonable hash code for its data members.
        /// Internally, strings can use their default hash codes, and numeric values can be used directly or bit-shifted and truncated to int.
        /// Multiple hash codes for internal data can usually be XOR'd together to produce a decent result.
        /// </summary>
        /// <returns></returns>
        public abstract override int GetHashCode();

        /// <summary>
        /// Each Key
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetUrnFragment();
        }
        #endregion

        #region IEquatable methods to override
        public abstract bool Equals(SfcKey akey);
        #endregion

        #region Public misc methods to override

        /// <summary>
        /// Each Key must implement how to produce a valid XPath-oriented identity string fragmentwhich is used
        /// to build complete and valid Urn from a SfcKeyChain of Key[].
        /// </summary>
        /// <returns></returns>
        public abstract string GetUrnFragment();

        /// <summary>
        /// The Type of the instance class associated with this key class.
        /// Default impl is the way a nested key class would do it for compatiblity with existing models using that technique.
        /// This should be overriden in any key class which is not a nested class of the instance type.
        /// </summary>
        public virtual Type InstanceType { get { return this.GetType().DeclaringType; } }
        #endregion
    }

    #region IUrnFragment definition and implementation
    /// <summary>
    /// IUrnFragment is a communication interface between SFC and key creation mechanisms in domains
    /// </summary>
    public interface IUrnFragment
    {
        string Name { get; }
        Dictionary<string,object> FieldDictionary { get; }
    }

    class XPathExpressionBlockImpl : IUrnFragment
    {
        XPathExpressionBlock xpBlock;
        Dictionary<string,object> fieldDict;

        public XPathExpressionBlockImpl( XPathExpressionBlock xpBlock )
        {
            this.xpBlock = xpBlock;
        }

        string IUrnFragment.Name
        {
            get
            {
                return xpBlock.Name;
            }
        }

        Dictionary<string,object> IUrnFragment.FieldDictionary
        {
            get
            {
                if( fieldDict == null )
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    foreach (DictionaryEntry entry in xpBlock.FixedProperties)
                    {
                        FilterNodeConstant filterNode = (FilterNodeConstant)entry.Value;
                        dict.Add(entry.Key.ToString(), Urn.UnEscapeString((string)filterNode.Value));
                    }
                    fieldDict = dict;
                }
                return fieldDict;
            }
        }
    }

    #endregion

    /// <summary>
    /// Keys of domain roots must inherit from this class, not SfcKey
    /// </summary>
    public abstract class DomainRootKey : SfcKey
    {
        private ISfcDomain m_Domain;

        protected DomainRootKey( ISfcDomain domain )
        {
            m_Domain = domain;
        }

        public ISfcDomain Domain
        {
            get { return m_Domain; }
            set { m_Domain = value; }
        }
    }

    /// <summary>
    /// A SfcKeyChain is a domain root and a key list which is a full identity path to an Sfc instance object.
    /// The domain root determines which domain and instance it represents.
    /// The list of SfcAbstractKeys determines what domain type instance objects at each level it represents.
    /// They can be compared for equality and used as unique dictionary keys even when mixed domains are present.
    /// </summary>
    public sealed class SfcKeyChain : IEquatable<SfcKeyChain>, IUrn
    {
        #region Private data
        SfcKey m_ThisKey;
        SfcKeyChain    m_Parent;
        #endregion

        #region Constructors
        /// <summary>
        /// The top-level constructor used to create Server-level SfcKeyChains from the domain instance root itself.
        /// This is the starting point for which domain instance a SfcKeyChain applies to, for purposes ranging from
        /// SfcKeyChain, datagrid/property bag caching to OQ-maintained object dictionaries.
        /// </summary>
        /// <param name="topKey"></param>
        internal SfcKeyChain( DomainRootKey topKey )
        {
            m_ThisKey = topKey;
            m_Parent = null;
        }

        /// <summary>
        /// The constructor to be used for all SfcKeyChains which are not topmost Server-level ones.
        /// This is commonly used to construct SfcKeyChains in a step-down fashion from a common parent SfcKeyChain.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        internal SfcKeyChain( SfcKey key, SfcKeyChain parent )
        {
            m_ThisKey = key;
            m_Parent = parent;
        }

        /// <summary>
        /// The constructor to be used for creating SfcKeyChains when the fully qualified URN is known.
        /// This is used when a fully qualified URN is known but we do not have an object yet
        /// </summary>
        /// <param name="urn"></param>
        /// <param name="domain"></param>
        public SfcKeyChain(Urn urn, ISfcDomain domain)
        {
            XPathExpression urnXPathExpr = urn.XPathExpression;

            // walk through each level of the URN. This indicates the type and keys for each level
            // we use the XPathExpressionBlock to access each level, however 
            // the only access to a XPathExpressionBlock is via the indexer
            for (int block = 0; block < urnXPathExpr.Length; block++)
            {
                XPathExpressionBlock xpBlock = urnXPathExpr[block];
                SfcKey thisLevelKey = domain.GetKey( new XPathExpressionBlockImpl(xpBlock) );

                if( block == 0 )
                {
                    SfcInstance instRoot = (SfcInstance)domain;
                    if( !thisLevelKey.Equals(instRoot.AbstractIdentityKey) )
                    {
                        throw new SfcInvalidQueryExpressionException(SfcStrings.BadQueryForConnection(thisLevelKey.ToString(),instRoot.ToString()));
                    }
                    m_Parent = null;
                    m_ThisKey = thisLevelKey;
                }
                else
                {
                    m_Parent = new SfcKeyChain( m_ThisKey, m_Parent );
                    m_ThisKey = thisLevelKey;
                }
            }
        }

        #endregion

        #region Private Client and Server equality impls
        /// <summary>
        /// Compares for body equality of the Keys in the Keychains once either the client or server part has been checked.
        /// This logic is common to both cases.
        /// </summary>
        /// <param name="kc">The other keychain. It cannot be null.</param>
        /// <returns></returns>
        private bool BodyEquals(SfcKeyChain kc)
        {
            // Quick early exit: if leaf keys are of different types, get out right away.
            // However, if they are of the same type, we still cannot guarantee that they are the same depth.
            // Nothing prevents you from using a type in multiple skeletal contexts.
            if (this.m_ThisKey.GetType() != kc.m_ThisKey.GetType())
            {
                return false;
            }

            // Check each Key level from the bottom-up, since the lower Keys are likely to be not equal more frequently
            // especially in comparing SfcKeyChains from similarly-leveled items such as siblings.
            // Luckily this is easier anyhow since we store keychains with a reference to its parent keychain.
            SfcKeyChain thisChain = this;
            SfcKeyChain otherChain = kc;

            do
            {
                // $ISSUE: VSTS 111656: EDDUDE: LOCALE: How would we convey a Comparer to here?
                // Each level of a keychain could need a different one.
                // And they should match what the corresponding object's collection(s) would use.
                // This is a big deal as soon as someone expects heavy collation support for an object using Sfc.
                // I would support IComparer in all SfcCollections and KeyChain levels as a base feature.

                // Compare like levels, except skip comparing the root key level since
                // Client and Server comparison rules differ for that and are handled by the caller.
                // NOTE: If you ever support partial keychains as valid, you would have to cast to DomainRootKey to check
                // instead of assuming a null parent means we are at the DomainRootKey entry.
                if (thisChain.m_Parent != null && !thisChain.m_ThisKey.Equals(otherChain.m_ThisKey))
                {
                    return false;
                }

                thisChain = thisChain.m_Parent;
                otherChain = otherChain.m_Parent;
            } while (thisChain != null && otherChain != null);

            // We match if we are both leaf-exhausted at the same time,
            // otherwise it is possible one is a subset of the other from the bottom-up.
            return (thisChain == null && otherChain == null);
        }

        /// <summary>
        /// Compares for client equality of the same instance object in the same object tree.
        /// </summary>
        /// <param name="otherKeychain">The keychain to compare.</param>
        /// <returns>True if equivalent, otherwise false.</returns>
        public bool ClientEquals(SfcKeyChain otherKeychain)
        {
            // If both are the same instance, we are always equal
            if (System.Object.ReferenceEquals(this, otherKeychain))
            {
                return true;
            }

            if ((object)otherKeychain == null)
            {
                return false;
            }

            // The domain instance must be identical if we represent the same client object tree instance.
            if (this.Domain != otherKeychain.Domain)
            {
                return false;
            }

            return BodyEquals(otherKeychain);
        }

        /// <summary>
        /// Compares for server equivalence of the same logical server-side entity regardless of client-side object tree.
        /// </summary>
        /// <param name="otherKeychain">The keychain to compare.</param>
        /// <returns>True if equivalent, otherwise false.</returns>
        public bool ServerEquals(SfcKeyChain otherKeychain)
        {
            // If both are the same instance, we are always equal
            if (System.Object.ReferenceEquals(this, otherKeychain))
            {
                return true;
            }

            if ((object)otherKeychain == null)
            {
                return false;
            }

            // The domain instances don't have to match (unlike ClientEquals),
            // but the logical domain and server instance names do via a string Invariant match
            if (this.Domain.DomainName != otherKeychain.Domain.DomainName || this.Domain.DomainInstanceName != otherKeychain.Domain.DomainInstanceName)
            {
                return false;
            }

            return BodyEquals(otherKeychain);
        }

        /// <summary>
        /// Compares for body descendancy using the Keys in the Keychains once either the client or server part has been checked.
        /// This logic is common to both cases.
        /// Inside is defined as comparing as equal from the top-down until the shorter (containing) keychain is exhausted.
        /// Note that comparing as equivalent, or either being null means not inside.
        /// </summary>
        /// <param name="kc">The other keychain. It cannot be null.</param>
        /// <returns></returns>
        private bool BodyDescendant(SfcKeyChain kc)
        {
            SfcKeyChain thisChain = this;
            SfcKeyChain otherChain = kc;

            // Stack the keys in each keychain so we can compare them top-down
            Stack<SfcKey> thisStack = new Stack<SfcKey>();
            Stack<SfcKey> otherStack = new Stack<SfcKey>();
            while (thisChain != null && otherChain != null)
            {
                thisStack.Push(thisChain.m_ThisKey);
                otherStack.Push(otherChain.m_ThisKey);

                thisChain = thisChain.m_Parent;
                otherChain = otherChain.m_Parent;
            }

            // If the containing keychain is not shorter than the one supposedly inside of it, we fail already
            if (thisChain != null || otherChain == null)
            {
                return false;
            }

            // Finish pushing the other chain up to its top so that we
            // can compare both stacks from the same root.
            while (otherChain != null)
            {
                otherStack.Push(otherChain.m_ThisKey);
                otherChain = otherChain.m_Parent;
            }

            // Pop the top entries for both stacks representing the root key level.
            // Client and Server comparison rules differ for that and are handled by the caller.
            // NOTE: If you ever support partial keychains as valid, you would have to cast to DomainRootKey to check
            // instead of assuming a null parent means we are at the DomainRootKey entry.
            thisStack.Pop();
            otherStack.Pop();

            // Compare keys top-down. We already know the supposed containing one ("this") has fewer levels.
            // If we make it through the loop, the longer given keychain is considered to be inside "this".
            while (thisStack.Count != 0)
            {
                // $ISSUE: VSTS 111656: LOCALE: How would we convey a Comparer to here? Each level of a keychain could need a different one,
                // or default to InvariantCulture if not specified.
                if (!thisStack.Peek().Equals(otherStack.Peek()))
                {
                    return false;
                }

                thisStack.Pop();
                otherStack.Pop();
            }

            return true;
        }

        /// <summary>
        /// Check if the given keychain is inside this one, using the rules of ClientEquality().
        /// Inside is defined as comparing as equal from the top-down until the shorter (containing) keychain is exhausted.
        /// Note that comparing as equivalent, or either being null means not inside.
        /// </summary>
        /// <param name="otherKeychain">The keychain to check.</param>
        /// <returns>True if the given keychain is a descendant, else false.</returns>
        public bool IsClientAncestorOf(SfcKeyChain otherKeychain)
        {
            // If we are the same keychain reference, we cannot have a descendant relationship
            if (System.Object.ReferenceEquals(this, otherKeychain))
            {
                return true;
            }

            if ((object)otherKeychain == null)
            {
                return false;
            }

            // The domain instance must be identical to continue comparing as a descendant
            if (this.Domain != otherKeychain.Domain)
            {
                return false;
            }

            return BodyDescendant(otherKeychain);

        }

        /// <summary>
        /// Check if the given keychain is inside this one, using the rules of ServerEquality().
        /// Inside is defined as comparing as equal from the top-down until the shorter (containing) keychain is exhausted.
        /// Note that comparing as equivalent, or either being null means not inside.
        /// </summary>
        /// <param name="otherKeychain">The keychain to check.</param>
        /// <returns>True if the given keychain is a descendant, else false.</returns>
        public bool IsServerAncestorOf(SfcKeyChain otherKeychain)
        {
            // If we are the same keychain reference, we cannot have a descendant relationship
            if (System.Object.ReferenceEquals(this, otherKeychain))
            {
                return true;
            }

            if ((object)otherKeychain == null)
            {
                return false;
            }

            // The domain instances don't have to match (unlike ClientEquals),
            // but the logical domain and server instance names do via a string Invariant match
            // to continue comparing as a descendant
            if (this.Domain.DomainName != otherKeychain.Domain.DomainName || this.Domain.DomainInstanceName != otherKeychain.Domain.DomainInstanceName)
            {
                return false;
            }

            return BodyDescendant(otherKeychain);
        }
        #endregion

        #region Object methods overriden
        /// <summary>
        /// Two SfcKeyChains are equal iff
        /// 1. They are both from the same domain and domain instance tree
        /// 2. They have the same number of Keys
        /// 3. Each Key is equal, checked from the bottom up to avoid nearby-but-not-equal comparisons
        /// Each Key class must implement a reasonable operator== that compares the Key values and not rely on the default object.Equals(object)
        /// which only checks for reference equality.
        /// </summary>
        /// <param name="obj">The object to compare, which must be a keychain.</param>
        /// <returns>True if equivalent, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            SfcKeyChain kc = obj as SfcKeyChain;
            if (kc == null)
            {
                return false;
            }

            return this.Equals(kc);
        }

        /// <summary>
        /// The hash code for a SfcKeyChain is simply a XOR of all its component Key hash codes.
        /// Each Key class must implement a reasonable GetHashCode() for this to distribute itself well.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            SfcKeyChain chain = this; 

            do
            {
                hash ^= chain.m_ThisKey.GetHashCode();
                chain = chain.m_Parent;
            }
            while( chain != null );

            return hash;
        }

        #endregion

        #region IEquatable<SfcKeyChain> impl
        /// <summary>
        /// The strongly-typed equality check used by the strongly-typed operators.
        /// Compares for client equality of the same instance in the same object tree.
        /// </summary>
        /// <param name="otherKeychain">The keychain to compare.</param>
        /// <returns>True if equivalent, otherwise false.</returns>
        public bool Equals(SfcKeyChain otherKeychain)
        {
            return ClientEquals(otherKeychain);
        }
        #endregion

        #region Operators
        /// <summary>
        /// The == operator compares two SfcKeyChains.
        /// 1. If both are null valued, they are equal.
        /// 2. If both are to the same object reference, they are equal.
        /// 3. If one is null but not the other, they are not equal.
        /// 4. If the number of key level in the two SfcKeyChains are not the same, they are not equal.
        /// 5. If the contents of each key level in both SfcKeyChains does not compare as equal, they are not equal.
        /// If it passes all of these tests, they are considered equal.
        /// </summary>
        /// <param name="leftOperand">The first keychain to compare.</param>
        /// <param name="rightOperand">The second keychain to compare.</param>
        /// <returns>True if equivalent, otherwise false.</returns>
        static public bool operator ==(SfcKeyChain leftOperand, SfcKeyChain rightOperand)
        {
            if ((object)leftOperand == null)
            {
                return ((object)rightOperand == null);
            }

            return leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// The != operator compares two SfcKeyChains for inequality by using the == operator and negating the result.
        /// See the == operator on the SfcKeyChain class for details on equality checking.
        /// </summary>
        /// <param name="leftOperand">The first keychain to compare.</param>
        /// <param name="rightOperand">The second keychain to compare.</param>
        /// <returns>True if equivalent, otherwise false.</returns>
        static public bool operator !=(SfcKeyChain leftOperand, SfcKeyChain rightOperand)
        {
            return !(leftOperand == rightOperand);
        }
        #endregion

        #region Public misc
        /// <summary>
        /// Get the Sfc object associated with this SfcKeyChain. This will look the object up in the hierarchy
        /// starting from the root of this key chain, and create new one (with putting it into collection)
        /// on demand. This is important so we never get orphaned objects
        /// </summary>
        public SfcInstance GetObject()
        {
            // If we are asked to get the domain root, we already have it within the SfcKeyChain. Just go get it
            if( this.LeafKey is DomainRootKey )
            {
                return (SfcInstance)this.Domain;
            }

            // If either of these is null, we can't proceed
            if (this.Parent == null || this.LeafKey == null)
            {
                return null;
            }

            // Look the object up in its parent
            SfcInstance parent = this.Parent.GetObject();

            string elementTypeName = this.LeafKey.InstanceType.Name;

            ISfcCollection collection = parent.GetChildCollection(elementTypeName);

            // This will create the object on demand
            SfcInstance currentObject = collection.GetObjectByKey(this.LeafKey);

            return currentObject;
        }

        /// <summary>
        /// Get the SfcKeyChain for the parent of this one.
        /// An SfcKeyChain is immutable so you must construct a new one to change the parent.
        /// </summary>
        /// <returns></returns>
        public SfcKeyChain Parent
        {
            get
            {
                return m_Parent;
            }

            //Direct mutation of parent is only used for CRUD-aware moves internally.
            //It assumes *all* descendants share a reference to this keychain to be 100% effective.
            internal set
            {
                m_Parent = value;
            }
        }

        /// <summary>
        /// Get the key of the leaf node
        /// An SfcKeyChain is immutable so you must construct a new one to change the leaf key.
        /// </summary>
        /// <returns></returns>
        public SfcKey LeafKey
        {
            get
            {
                return m_ThisKey;
            }

            //Direct mutation of leaf key is only used for CRUD-aware renames internally.
            //It assumes *all* descendants share a reference to this keychain to be 100% effective.
            internal set
            {
                m_ThisKey = value;
            }
        }

        /// <summary>
        /// Get the key of the root node
        /// </summary>
        /// <returns></returns>
        public DomainRootKey RootKey
        {
            get
            {
                // Drill down to the bottom and get RootKey
                SfcKeyChain chain = this; 
                while( chain.m_Parent != null )
                {
                    chain = chain.m_Parent;
                }

                DomainRootKey rootKey = (DomainRootKey)chain.m_ThisKey;
                return rootKey;
            }
        }

        public bool IsRooted
        {
            get
            {
                // If the top of the chain isn't a DomainRootKey, we are not an absolute (full) path.
                SfcKeyChain chain = this;
                while (chain.m_Parent != null)
                {
                    chain = chain.m_Parent;
                }

                return chain.m_ThisKey != null && chain.m_ThisKey is DomainRootKey;
            }
        }

        public bool IsConnected
        {
            get
            {
                // If the top of the chain isn't a DomainRootKey, we are not connected. End of story.
                SfcKeyChain chain = this;
                while (chain.m_Parent != null)
                {
                    chain = chain.m_Parent;
                }

                return chain.m_ThisKey != null 
                    && chain.m_ThisKey is DomainRootKey 
                    && ((DomainRootKey)chain.m_ThisKey).Domain.ConnectionContext.Mode != SfcConnectionContextMode.Offline;
            }
        }

        /// <summary>
        /// Return the domain interface for given KeyChain
        /// </summary>
        /// <returns>ISfcDomain</returns>
        public ISfcDomain Domain
        {
            get
            {
                return RootKey.Domain;
            }
        }

        /// <summary>
        /// A SfcKeyChain can always be converted into the equivalent Urn format.
        /// </summary>
        /// <returns></returns>
        public Urn Urn
        {
            get
            {
                return new Urn(this);
            }
        }

        /// <summary>
        /// The ToString() implementation should return a string representation usable in sorting by an external comparer.
        /// It should not respresent an XPath or any other format, unless intended as an actual collation format.
        /// TODO: Either add a GetComparerString() virtual, or make Keys do comprisons using an externally-provided Comparer.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // As we embed SfcKeyChains deeper into the Enumerator core and eventually supplant Urns we will need this property less and less.
            StringBuilder sb = new StringBuilder();
            bool first = true;
            SfcKeyChain chain = this;

            do
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Insert(0,@"/");
                }
                sb.Insert(0, chain.m_ThisKey.GetUrnFragment());

                chain = chain.m_Parent;

            }while( chain != null );

            return sb.ToString();
        }

        #endregion

#region IUrn support
        XPathExpression IUrn.XPathExpression
        {
            get { return new XPathExpression(this.ToString()); }
        }

        String IUrn.Value
        {
            get { return this.ToString(); }
            set
            {
                // KeyChain-based Urns are immutable. $TODO$: change it to a more meaningful exception
                throw new InvalidOperationException();
            }
        }

        String IUrn.DomainInstanceName
        {
            get
            {
                return this.Domain.DomainInstanceName;
            }
        }

#endregion

    }

    #region NamedKey<T>

    /// <summary>
    /// The single string name key for an instance class.
    /// </summary>
    public class NamedKey<T> : SfcKey, IEquatable<NamedKey<T>>
        where T : SfcInstance
    {
        private string keyName;

        /// <summary>
        /// Default constructor for a name key.
        /// </summary>
        public NamedKey()
            : base()
        {
            this.keyName = string.Empty;
        }

        /// <summary>
        /// Construct a name key from another name key.
        /// </summary>
        /// <param name="other">The other name key.</param>
        public NamedKey(NamedKey<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            this.keyName = other.Name;
        }

        /// <summary>
        /// Construct a name key from a string.
        /// </summary>
        /// <param name="name">The name string.</param>
        public NamedKey(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            this.keyName = name ?? string.Empty;
        }

        /// <summary>
        /// Construct a name key from a field dictionary.
        /// </summary>
        /// <param name="fields">The dictionary of field value pairs.</param>
        public NamedKey(IDictionary<string, object> fields)
        {
            // this will throw if the field is not found.
            this.keyName = (string)fields["Name"];
            if (this.keyName == null)
            {
                throw new ArgumentNullException("fields[Name]");
            }
        }

        /// <summary>
        /// The name key value.
        /// </summary>
        public string Name
        {
            get { return this.keyName; }
        }

        /// <summary>
        /// Compare a name key to this key for value equality.
        /// </summary>
        /// <param name="obj">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || !(obj is NamedKey<T>)) { return false; }
            return this.Equals((NamedKey<T>)obj);
        }

        /// <summary>
        /// Compare a name key to this key for value equality.
        /// </summary>
        /// <param name="other">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public override bool Equals(SfcKey other)
        {
            if (object.ReferenceEquals(other, null) || !(other is NamedKey<T>)) { return false; }
            return this.Equals((NamedKey<T>)other);
        }
        
        /// <summary>
        /// Compare a name key to this key for value equality.
        /// </summary>
        /// <param name="other">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public bool Equals(NamedKey<T> other)
        {
            if (object.ReferenceEquals(this, other)) { return true; }
            if (object.ReferenceEquals(other, null)) { return false; }
            return string.CompareOrdinal(this.Name, other.Name) == 0;
        }

        /// <summary>
        /// Generate a hash code for the key.
        /// </summary>
        /// <returns>The hash code for the key value.</returns>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        /// <summary>
        /// The string value for the key.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Equals static operator for a key.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if the keys are equal; otherwise false.</returns>
        public new static bool Equals(object leftOperand, object rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? true : false; }
            return leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// Compare two keys for value equality.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if both keys are equal or are both null; otherwise false.</returns>
        public static bool operator ==(NamedKey<T> leftOperand, NamedKey<T> rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? true : false; }
            return leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// Compare two keys for value inequality.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if both keys are not equal or only one is null; otherwise false.</returns>
        public static bool operator !=(NamedKey<T> leftOperand, NamedKey<T> rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? false : true; }
            return !leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// The instance type which this key represents.
        /// </summary>
        public sealed override Type InstanceType { get { return typeof(T); } }

        /// <summary>
        /// The Urn level name corresponding to the instance type which this key respresents.
        /// It is normally the instance type class name.
        /// Overriding this typically is done with typeof(T).urnName or similar.
        /// </summary>
        protected virtual string UrnName { get { return typeof(T).Name; } }

        /// <summary>
        /// The Urn level fragment obtains its name from the key name property by default.
        /// Override in a derived key class if you need a different fragment format.
        /// </summary>
        /// <returns></returns>
        public override string GetUrnFragment()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", this.UrnName, SfcSecureString.EscapeSquote(this.Name));
        }

    }

    #endregion

    #region SchemaNamedKey<T>

    /// <summary>
    /// The composite string schema and name key for an instance class.
    /// </summary>
    public class SchemaNamedKey<T> : SfcKey, IEquatable<SchemaNamedKey<T>>
        where T : SfcInstance
    {
        private string keySchema;
        private string keyName;

        /// <summary>
        /// Default constructor for a schema name key
        /// </summary>
        public SchemaNamedKey()
            : base()
        {
            this.keySchema = string.Empty;
            this.keyName = string.Empty;
        }

        /// <summary>
        /// Construct a schema name key from a name string and default schema.
        /// </summary>
        /// <param name="name">The name key value.</param>
        public SchemaNamedKey(string name)
            : this("dbo", name)
        {
        }

        /// <summary>
        /// Construct a schema name key from strings.
        /// </summary>
        /// <param name="schema">The schema string.</param>
        /// <param name="name">The name string.</param>
        public SchemaNamedKey(string schema, string name)
        {
            if (schema == null || schema.Length == 0)
            {
                throw new ArgumentNullException("schema");
            }
            if (name == null || name.Length == 0)
            {
                throw new ArgumentNullException("name");
            }
            this.keySchema = schema;
            this.keyName = name;
        }

        /// <summary>
        /// Construct a schema name key from another schema name key.
        /// </summary>
        /// <param name="other">The other schema name key.</param>
        public SchemaNamedKey(SchemaNamedKey<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            this.keySchema = other.Schema;
            this.keyName = other.Name;
        }
        
        /// <summary>
        /// Construct a schema name key from a field dictionary.
        /// </summary>
        /// <param name="fields">The dictionary of field value pairs.</param>
        public SchemaNamedKey(IDictionary<string, object> fields)
        {
            // this will throw if the field is not found.
            this.keySchema = (string)fields["Schema"];
            this.keyName = (string)fields["Name"];
        }

        /// <summary>
        /// The schema key value.
        /// </summary>
        public string Schema
        {
            get { return this.keySchema; }
        }

        /// <summary>
        /// The name key value.
        /// </summary>
        public string Name
        {
            get { return this.keyName; }
        }

        /// <summary>
        /// Compare a schema name key to this key for value equality.
        /// </summary>
        /// <param name="obj">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || !(obj is SchemaNamedKey<T>)) { return false; }
            return this.Equals((SchemaNamedKey<T>)obj);
        }

        /// <summary>
        /// Compare a schema name key to this key for value equality.
        /// </summary>
        /// <param name="other">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public override bool Equals(SfcKey other)
        {
            if (object.ReferenceEquals(other, null) || !(other is SchemaNamedKey<T>)) { return false; }
            return this.Equals((SchemaNamedKey<T>)other);
        }

        /// <summary>
        /// Compare a schema name key to this key for value equality.
        /// </summary>
        /// <param name="other">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public bool Equals(SchemaNamedKey<T> other)
        {
            if (object.ReferenceEquals(this, other)) { return true; }
            if (object.ReferenceEquals(other, null)) { return false; }
            return string.CompareOrdinal(this.Name, other.Name) == 0 && string.CompareOrdinal(this.Schema, other.Schema) == 0;
        }
        
        /// <summary>
        /// Generate a hash code for the key.
        /// </summary>
        /// <returns>The hash code for the key value.</returns>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Schema.GetHashCode();
        }

        /// <summary>
        /// The string value for the key.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Schema + "." + this.Name;
        }

        /// <summary>
        /// Equals static operator for a key.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if the keys are equal; otherwise false.</returns>
        public new static bool Equals(object leftOperand, object rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? true : false; }
            return leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// Compare two keys for value equality.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if both keys are equal or are both null; otherwise false.</returns>
        public static bool operator ==(SchemaNamedKey<T> leftOperand, SchemaNamedKey<T> rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? true : false; }
            return leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// Compare two keys for value inequality.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if both keys are not equal or only one is null; otherwise false.</returns>
        public static bool operator !=(SchemaNamedKey<T> leftOperand, SchemaNamedKey<T> rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? false : true; }
            return !leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// The instance type which this key represents.
        /// </summary>
        public sealed override Type InstanceType { get { return typeof(T); } }

        /// <summary>
        /// The Urn level name corresponding to the instance type which this key respresents.
        /// It is normally the instance type class name.
        /// Overriding this typically is done with typeof(T).urnName or similar.
        /// </summary>
        protected virtual string UrnName { get { return typeof(T).Name; } }

        /// <summary>
        /// The Urn level fragment obtains its name from the key schema and name properties by default.
        /// Override in a derived key class if you need a different fragment format.
        /// </summary>
        /// <returns></returns>
        public override string GetUrnFragment()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}' and @Schema = '{2}']", this.UrnName, SfcSecureString.EscapeSquote(this.keyName), SfcSecureString.EscapeSquote(Schema));
        }

    }

    #endregion

    #region NamedDomainKey<T>

    /// <summary>
    /// The domain key with a string name for a general SFC domain root instance class.
    /// </summary>
    public class NamedDomainKey<T> : DomainRootKey, IEquatable<NamedDomainKey<T>>
        where T : ISfcDomain                       
    {
        private string keyName;

        /// <summary>
        /// Default constructor for a general SFC named domain key.
        /// The caller must remember to set the Root property post-construction.
        /// </summary>
        public NamedDomainKey()
            : this(null, string.Empty)
        {
        }

        /// <summary>
        /// Construct a general SFC named domain key from a domain root instance using its domain instance name.
        /// </summary>
        /// <param name="domain">The domain instance.</param>
        public NamedDomainKey(ISfcDomain domain)
            : this(domain, domain.DomainInstanceName)
        {
        }

        /// <summary>
        /// Construct a general SFC named domain key from a domain root instance using the given name.
        /// </summary>
        /// <param name="domain">The domain instance.</param>
        /// <param name="name">The name string.</param>
        public NamedDomainKey(ISfcDomain domain, string name)
            : base(domain)
        {
            this.keyName = name;
        }

        /// <summary>
        /// Construct a general SFC named domain key from a domain root instance using the given field dictionary.
        /// </summary>
        /// <param name="domain">The domain instance.</param>
        /// <param name="fields">The dictionary of field value pairs.</param>
        public NamedDomainKey(ISfcDomain domain, IDictionary<string, object> fields)
            : base(domain)
        {
            // this will throw if the field is not found.
            this.keyName = (string)fields["Name"];
        }

        /// <summary>
        /// The name key value.
        /// </summary>
        public string Name
        {
            get { return this.keyName; }
        }

        /// <summary>
        /// Compare a named domain key to this key for value equality.
        /// </summary>
        /// <param name="obj">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || !(obj is NamedDomainKey<T>)) { return false; }
            return this.Equals((NamedDomainKey<T>)obj);
        }

        /// <summary>
        /// Compare a named domain key to this key for value equality.
        /// </summary>
        /// <param name="other">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public override bool Equals(SfcKey other)
        {
            if (object.ReferenceEquals(other, null) || !(other is NamedDomainKey<T>)) { return false; }
            return this.Equals((NamedDomainKey<T>)other);
        }

        /// <summary>
        /// Compare a named domain key to this key for value equality.
        /// </summary>
        /// <param name="other">A key to compare.</param>
        /// <returns>True if the keys are equal in value; otherwise false.</returns>
        public bool Equals(NamedDomainKey<T> other)
        {
            if (object.ReferenceEquals(this, other)) { return true; }
            if (object.ReferenceEquals(other, null)) { return false; }
            return base.Domain.Equals(other.Domain) && string.CompareOrdinal(this.Name, other.Name) == 0;
        }

        /// <summary>
        /// Generate a hash code for the key.
        /// </summary>
        /// <returns>The hash code for the key value.</returns>
        public override int GetHashCode()
        {
            return this.Domain.GetHashCode() ^ this.Name.GetHashCode();
        }

        /// <summary>
        /// The string value for the key.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Domain.DomainName + "," + this.Name;
        }

        /// <summary>
        /// Equals static operator for a key.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if the keys are equal; otherwise false.</returns>
        public new static bool Equals(object leftOperand, object rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? true : false; }
            // Only compare two named domain keys
            return ((NamedDomainKey<T>)leftOperand).Equals(rightOperand);
        }

        /// <summary>
        /// Compare two keys for equality.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if both keys are equal or are both null; otherwise false.</returns>
        public static bool operator ==(NamedDomainKey<T> leftOperand, NamedDomainKey<T> rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? true : false; }
            return leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// Compare two keys for inequality.
        /// </summary>
        /// <param name="leftOperand">A key to compare.</param>
        /// <param name="rightOperand">A key to compare.</param>
        /// <returns>True if both keys are not equal or only one is null; otherwise false.</returns>
        public static bool operator !=(NamedDomainKey<T> leftOperand, NamedDomainKey<T> rightOperand)
        {
            if (object.ReferenceEquals(leftOperand, null)) { return object.ReferenceEquals(rightOperand, null) ? false : true; }
            return !leftOperand.Equals(rightOperand);
        }

        /// <summary>
        /// The instance type which this key represents.
        /// </summary>
        public sealed override Type InstanceType { get { return typeof(T); } }
        
        /// <summary>
        /// The Urn level name corresponding to the instance type which this key respresents.
        /// It is normally the instance type class name.
        /// Overriding this typically is done with typeof(T).urnName or similar.
        /// </summary>
        protected virtual string UrnName { get { return typeof(T).Name; } }

        /// <summary>
        /// The Urn level fragment obtains its name from the key name property by default.
        /// Override in a derived key class if you need a different fragment format.
        /// </summary>
        /// <returns></returns>
        public override string GetUrnFragment()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", this.UrnName, SfcSecureString.EscapeSquote(this.Name));
        }

    }

    #endregion
}

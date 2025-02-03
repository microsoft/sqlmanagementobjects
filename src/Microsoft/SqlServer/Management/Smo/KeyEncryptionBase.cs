using System;
using System.Text;
using System.Data;
using System.Globalization;
using System.Collections;
using System.Collections.Specialized;
using System.Security.Cryptography;

using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class KeyEncryption : SqlSmoObject, Cmn.ICreatable, Cmn.IDroppable, Cmn.IMarkForDrop, IScriptable
    {

        internal KeyEncryption(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public KeyEncryption(SymmetricKey parent, SymmetricKeyEncryptionType symmetricKeyEncryptionType, string certificateOrPasswordOrSymmetricKey) :
            base(parent.KeyEncryptions, new KeyEncryptionKey(certificateOrPasswordOrSymmetricKey, symmetricKeyEncryptionType), SqlSmoState.Creating)
        {
            this.symmetricKeyEncryptionType = symmetricKeyEncryptionType;
            this.optionValue = certificateOrPasswordOrSymmetricKey;
        }


        // returns the name of the type in the urn expression
        internal static string UrnSuffix
        {
            get
            {
                return "KeyEncryption";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);

            Encoding encoding = Encoding.Unicode;


            string thumbPrint = String.Empty;

            /*
               Explanation of the 
               SHA-1 hash of the certificate with which the key is encrypted 
                                    OR 
               The guid of the symmetric key with which the key is encrypted
            */

            switch (symmetricKeyEncryptionType)
            {
                case SymmetricKeyEncryptionType.Password:
                    {

                        HashAlgorithm sha = new SHA1CryptoServiceProvider();

                        byte[] result = sha.ComputeHash(encoding.GetBytes(this.optionValue.ToString()));

                        thumbPrint = encoding.GetString(result);
                        break;
                    }

                case SymmetricKeyEncryptionType.Certificate:
                    {
                        Server server = (Server)(Parent.Parent.Parent);
                        thumbPrint = encoding.GetString(server.Certificates[this.optionValue.ToString()].Signature);
                        break;
                    }

                case SymmetricKeyEncryptionType.SymmetricKey:
                    {
                        Database db = (Database)(Parent.Parent);
                        thumbPrint = db.SymmetricKeys[this.optionValue.ToString].KeyGuid.ToString();
                        break;
                    }

            }


            KeyEncryptionKey key = new KeyEncryptionKey(thumbPrint, this.symmetricKeyEncryptionType);

            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}[{1}]", UrnSuffix, key.UrnFilter);
        }


        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder createQuery = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptIncludeHeaders(createQuery, sp, "Symmetric Key");

            createQuery.AppendFormat(SmoApplication.DefaultCulture, "ALTER SYMMETRIC KEY [{0}] ",
                                         SqlBraket(((SymmetricKey)(ParentColl.ParentInstance)).FormatFullNameForScripting(sp)));
            createQuery.AppendFormat(SmoApplication.DefaultCulture, "ADD ENCRYPTION BY  {0} ", GetEncryptingMechanism());

            queries.Add(createQuery.ToString());
        }

        public void Drop()
        {
            base.DropImpl();
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            SymmetricKey parent = (SymmetricKey)ParentColl.ParentInstance;

            ScriptIncludeHeaders(sb, sp, "Symmetric Key");

            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER SYMMETRIC KEY {1} ", parent.Name);
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP ENCRYPTION BY  {1} ", GetEncryptingMechanism());

            queries.Add(sb.ToString());
        }

        internal StringBuilder GetEncryptingMechanism()
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            switch (symmetricKeyEncryptionType)
            {
                case SymmetricKeyEncryptionType.Password: sb.AppendFormat(SmoApplication.DefaultCulture, "PASSWORD = '{0}' ", SqlString(optionValue.ToString())); break;
                case SymmetricKeyEncryptionType.Certificate: sb.AppendFormat(SmoApplication.DefaultCulture, "CERTIFICATE  [{0}] ", SqlBraket(optionValue.ToString())); break;
                case SymmetricKeyEncryptionType.SymmetricKey: sb.AppendFormat(SmoApplication.DefaultCulture, "SYMMETRIC KEY [{0}] ", SqlBraket(optionValue.ToString())); break;
            }
            return sb;
        }

        private SymmetricKeyEncryptionType symmetricKeyEncryptionType;
        private SqlSecureString optionValue;

        /// <summary>
        /// Sets the  Password that is used by the proxy
        /// </summary>
        public void SetEncryptionOptions(SymmetricKeyEncryptionType symmetricKeyEncryptionType, string optionValue)
        {

            try
            {
                this.symmetricKeyEncryptionType = symmetricKeyEncryptionType;

                if (null == optionValue)
                    throw new ArgumentNullException("optionValue");

                this.optionValue = optionValue;

            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SetEncryptionOptions, this, e);
            }
        }

        /// <summary>
        /// Generate object creation script using default scripting options
        /// </summary>
        /// <returns></returns>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Marks the opbject for drop
        /// </summary>
        /// <param name="dropOnAlter"></param>
        /// <returns></returns>
        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }


    }

}




// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// The BackupEncryptionOptions represents encryption options for backup operations.
    /// </summary>
    public sealed class BackupEncryptionOptions
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupEncryptionOptions"/> class.
        /// </summary>
        public BackupEncryptionOptions()
        {
            this.noEncryption = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupEncryptionOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="encryptorType">The encryptor type.</param>
        /// <param name="encryptorName">The encryptor name (server certificate name or server asymmetric key name).</param>
        public BackupEncryptionOptions(BackupEncryptionAlgorithm algorithm, BackupEncryptorType encryptorType, string encryptorName)
        {
            this.noEncryption = false;
            this.Algorithm = algorithm;
            this.EncryptorType = encryptorType;
            this.EncryptorName = encryptorName;
        }

        #endregion

        #region Properties

        private bool noEncryption;

        /// <summary>
        /// Gets or sets whether encryption is disabled.
        /// </summary>
        public bool NoEncryption
        {
            get
            {
                return this.noEncryption;
            }
            set
            {
                this.noEncryption = value;
            }
        }

        private BackupEncryptionAlgorithm? algorithm;

        /// <summary>
        /// Gets or sets the encryption algorithm.
        /// </summary>
        public BackupEncryptionAlgorithm? Algorithm
        {
            get
            {
                return this.algorithm;
            }
            set
            {
                this.algorithm = value;
            }
        }

        private BackupEncryptorType? encryptorType;

        /// <summary>
        /// Gets or sets the encryptor type.
        /// </summary>
        public BackupEncryptorType? EncryptorType
        {
            get
            {
                return this.encryptorType;
            }
            set
            {
                this.encryptorType = value;
            }
        }

        private string encryptorName;

        /// <summary>
        /// Gets or sets the encryptor name (server certificate name or server asymmetric key name).
        /// </summary>
        public string EncryptorName
        {
            get
            {
                return this.encryptorName;
            }
            set
            {
                this.encryptorName = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Script the T-SQL encryption option.
        /// </summary>
        /// <returns>The script fragment of encryption option.</returns>
        internal string Script()
        {
            String script;

            if (this.noEncryption)
            {
                script = string.Empty;
            }
            else
            {
                // check if required parameters are provided
                //
                if (!this.algorithm.HasValue)
                {
                    throw new PropertyNotSetException("Algorithm");
                }
                if (!this.encryptorType.HasValue)
                {
                    throw new PropertyNotSetException("EncryptorType");
                }
                if (String.IsNullOrEmpty(this.encryptorName))
                {
                    throw new PropertyNotSetException("EncryptorName");
                }

                string algorithm = GetAlgorithmString(this.algorithm.Value);
                string encryptorType = GetEncryptorTypeString(this.encryptorType.Value);

                script = String.Format(
                    SmoApplication.DefaultCulture,
                    "ENCRYPTION(ALGORITHM = {0}, {1} = [{2}])",
                    algorithm,
                    encryptorType,
                    SqlSmoObject.SqlBraket(this.encryptorName));

            }

            return script;
        }

        /// <summary>
        /// Gets the string value of the encryption algorithm.
        /// </summary>
        /// <param name="algorithm">algorithm</param>
        /// <returns>The string value.</returns>
        public static string GetAlgorithmString(BackupEncryptionAlgorithm algorithm)
        {
            String stringValue = String.Empty;
            switch (algorithm)
            {
                case BackupEncryptionAlgorithm.Aes128:
                    stringValue = "AES_128";
                    break;

                case BackupEncryptionAlgorithm.Aes192:
                    stringValue = "AES_192";
                    break;

                case BackupEncryptionAlgorithm.Aes256:
                    stringValue = "AES_256";
                    break;

                case BackupEncryptionAlgorithm.TripleDes:
                    stringValue = "TRIPLE_DES_3KEY";
                    break;

                default:
                    // Debug assert that this is not hit
                    //
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Unknown BackupEncryptionAlgorithm: {0}", algorithm));
            }
            return stringValue;
        }

        /// <summary>
        /// Gets the string value of the encryptor type.
        /// </summary>
        /// <param name="encryptorType">encryptor type</param>
        /// <returns>The string value.</returns>
        private static string GetEncryptorTypeString(BackupEncryptorType encryptorType)
        {
            String script = String.Empty;
            switch (encryptorType)
            {
                case BackupEncryptorType.ServerCertificate:
                    script = "SERVER CERTIFICATE";
                    break;

                case BackupEncryptorType.ServerAsymmetricKey:
                    script = "SERVER ASYMMETRIC KEY";
                    break;

                default:
                    // Debug assert that this is not hit
                    //
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Unknown BackupEncryptorType: {0}", encryptorType));
            }
            return script;
        }

        #endregion
    }
}

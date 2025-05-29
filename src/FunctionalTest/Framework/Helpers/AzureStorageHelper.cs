// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.IO;
using System.Linq;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Contains methods for configuring Azure storage access by SQL instances
    /// </summary>
    public class AzureStorageHelper
    {
        private ArmClient ArmClient => new ArmClient(CredentialProvider.GetCredential());

        public string Id { get; private set; }
        public ICredential CredentialProvider { get; }

        public AzureStorageHelper(string id, ICredential credentialProvider)
        {
            Id = id.Trim();
            CredentialProvider = credentialProvider;
        }

        public string StorageAccountName => Id.Substring(Id.LastIndexOf('/') + 1);
        public Credential EnsureCredential(Management.Smo.Server server)
        {
            var credential = new Credential(server, StorageAccountName + Guid.NewGuid());
            credential.Create(StorageAccountName, GetStorageAccountAccessKey(Id));
            return credential;
        }

        public string GetStorageAccountAccessKey(string storageAccountResourceId)
        {
            TraceHelper.TraceInformation($"Fetching storage access key for {storageAccountResourceId}");
            var storageAccount = ArmClient.GetStorageAccountResource(new Azure.Core.ResourceIdentifier(storageAccountResourceId));
            return storageAccount.GetKeys().First().Value;
        }

        /// <summary>
        /// Downloads a blob from the given blob URL in Azure storage
        /// </summary>
        /// <param name="blobUrl"></param>
        /// <returns></returns>
        public string DownloadBlob(string blobUrl)
        {
            var ext = Path.GetExtension(blobUrl);
            var path = Path.GetTempFileName();
            var finalPath = Path.ChangeExtension(path, ext);
            File.Move(path, finalPath);
            var blobClient = new BlobClient(new Uri(blobUrl), CredentialProvider.GetCredential());
            using (var rsp = blobClient.DownloadTo(finalPath))
            {
                if (rsp.IsError)
                {
                    throw new InvalidOperationException($"Unable to download blob '{blobUrl}'. Error: {rsp.ReasonPhrase}");
                }
            }
            return finalPath;
        }
    }
}

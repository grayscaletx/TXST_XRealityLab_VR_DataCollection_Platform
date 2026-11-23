using Common.Constants;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureStorage
{
    public class ContainerService
    {
        /// <summary>
        /// Stores the file "directory/name" in the given container.
        /// Azure Blob Storage is used when a storage connection string is configured,
        /// otherwise the file is copied to the local storage folder.
        /// The public URL of the file is always BaseLink.Combine(container, name).
        /// </summary>
        public static async Task<bool> UploadOperationAsync(string directory, string name, string container)
        {
            try
            {
                string sourceFile = Path.Combine(directory, name);

                if (!AzureGPV2Credentials.UseAzure)
                {
                    string targetFolder = Path.Combine(AzureGPV2Credentials.LOCAL_STORAGE_ROOT, container);
                    Directory.CreateDirectory(targetFolder);
                    File.Copy(sourceFile, Path.Combine(targetFolder, name), true);
                    return true;
                }

                var storageAccount = CloudStorageAccount.Parse(AzureGPV2Credentials.STORAGE_ACCOUNT_CONNECTION_STRING);
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                var cloudBlobContainer = cloudBlobClient.GetContainerReference(container);
                await cloudBlobContainer.CreateIfNotExistsAsync();
                await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(name);
                await cloudBlockBlob.UploadFromFileAsync(sourceFile);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Storage] Upload of '" + name + "' to '" + container + "' failed: " + ex.Message);
                return false;
            }
        }
    }
}

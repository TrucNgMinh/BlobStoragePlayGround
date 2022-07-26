using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace ProductApi.Services;

public class BlobServiceClientService
{
    
    private static readonly string blobUri = "https://trucngstoragesample.blob.core.windows.net";

    
    public static void GetBlobServiceClientSAS(ref BlobServiceClient blobServiceClient,
        string accountName, string sasToken)
    {
        sasToken = "sp=racwdl&st=2022-07-26T06:09:21Z&se=2022-07-26T14:09:21Z&sv=2021-06-08&sr=c&sig=kB0Zo1d9xPvU3pHf7fq5B8TNierZusTyVtjFlBN7lzk%3D";
        blobServiceClient = new BlobServiceClient
            (new Uri($"{blobUri}?{sasToken}"), null);
    }
    
    public static void GetBlobServiceClient(ref BlobServiceClient blobServiceClient, string accountName)
    {
        TokenCredential credential = new DefaultAzureCredential();

        blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);          
    }
    
    public static void GetBlobServiceClientAzureAD(ref BlobServiceClient blobServiceClient,
        string accountName, string clientID, string clientSecret, string tenantID)
    {

        TokenCredential credential = new ClientSecretCredential(
            tenantID, clientID, clientSecret, new TokenCredentialOptions());

        blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);
    }
    private static async Task<BlobContainerClient> CreateSampleContainerAsync(BlobServiceClient blobServiceClient)
    {
        // Name the sample container based on new GUID to ensure uniqueness.
        // The container name must be lowercase.
        string containerName = "container-" + Guid.NewGuid();

        try
        {
            // Create the container
            BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(containerName);

            if (await container.ExistsAsync())
            {
                Console.WriteLine("Created container {0}", container.Name);
                return container;
            }
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine("HTTP error code {0}: {1}",
                e.Status, e.ErrorCode);
            Console.WriteLine(e.Message);
        }

        return null;
    }
    
    private static void CreateRootContainer(BlobServiceClient blobServiceClient)
    {
        try
        {
            // Create the root container or handle the exception if it already exists
            BlobContainerClient container =  blobServiceClient.CreateBlobContainer("$root");

            if (container.Exists())
            {
                Console.WriteLine("Created root container.");
            }
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine("HTTP error code {0}: {1}",
                e.Status, e.ErrorCode);
            Console.WriteLine(e.Message);
        }
    }
    
    private static async Task DeleteSampleContainerAsync(BlobServiceClient blobServiceClient, string containerName)
    {
        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

        try
        {
            // Delete the specified container and handle the exception.
            await container.DeleteAsync();
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine("HTTP error code {0}: {1}",
                e.Status, e.ErrorCode);
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }
    
    private static async Task DeleteContainersWithPrefixAsync(BlobServiceClient blobServiceClient, string prefix)
    {
        Console.WriteLine("Delete all containers beginning with the specified prefix");

        try
        {
            foreach (BlobContainerItem container in blobServiceClient.GetBlobContainers())
            {
                if (container.Name.StartsWith(prefix))
                { 
                    Console.WriteLine("\tContainer:" + container.Name);
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container.Name);
                    await containerClient.DeleteAsync();
                }
            }

            Console.WriteLine();
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine(e.Message);
            Console.ReadLine();
            throw;
        }
    }
    
    public static async Task RestoreContainer(BlobServiceClient client, string containerName)
    {
        await foreach (BlobContainerItem item in client.GetBlobContainersAsync
                           (BlobContainerTraits.None, BlobContainerStates.Deleted))
        {
            if (item.Name == containerName && (item.IsDeleted == true))
            {
                try 
                { 
                    await client.UndeleteBlobContainerAsync(containerName, item.VersionId);
                }
                catch (RequestFailedException e)
                {
                    Console.WriteLine("HTTP error code {0}: {1}",
                        e.Status, e.ErrorCode);
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
    
    private static async Task ReadContainerPropertiesAsync(BlobContainerClient container)
    {
        try
        {
            // Fetch some container properties and write out their values.
            var properties = await container.GetPropertiesAsync();
            Console.WriteLine($"Properties for container {container.Uri}");
            Console.WriteLine($"Public access level: {properties.Value.PublicAccess}");
            Console.WriteLine($"Last modified time in UTC: {properties.Value.LastModified}");
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }
    
    public static async Task AddContainerMetadataAsync(BlobContainerClient container)
    {
        try
        {
            IDictionary<string, string> metadata =
                new Dictionary<string, string>();

            // Add some metadata to the container.
            metadata.Add("docType", "textDocuments");
            metadata.Add("category", "guidance");

            // Set the container's metadata.
            await container.SetMetadataAsync(metadata);
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }
    
    public static async Task ReadContainerMetadataAsync(BlobContainerClient container)
    {
        try
        {
            var properties = await container.GetPropertiesAsync();

            // Enumerate the container's metadata.
            Console.WriteLine("Container metadata:");
            foreach (var metadataItem in properties.Value.Metadata)
            {
                Console.WriteLine($"\tKey: {metadataItem.Key}");
                Console.WriteLine($"\tValue: {metadataItem.Value}");
            }
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }
    
    async static Task ListContainers(BlobServiceClient blobServiceClient, 
        string prefix, 
        int? segmentSize)
    {
        try
        {
            // Call the listing operation and enumerate the result segment.
            var resultSegment = blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, prefix, default)
                    .AsPages(default, segmentSize);

            await foreach (Azure.Page<BlobContainerItem> containerPage in resultSegment)
            {
                foreach (BlobContainerItem containerItem in containerPage.Values)
                {
                    Console.WriteLine("Container name: {0}", containerItem.Name);
                }

                Console.WriteLine();
            }
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine(e.Message);
            Console.ReadLine();
            throw;
        }
    }
}
// See https://aka.ms/new-console-template for more information

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProductApi.Services;

try
{
    BlobServiceClient blobClientService = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=trucngstoragesample;AccountKey=t9yRRh4o9HiiunkSvLrunNj80X+U17mWF8RI8jY77KlrlGSjeqnyUuaw+n3oKvgrIVDDDB85Igl7+AStHzNCBQ==;EndpointSuffix=core.windows.net");

    BlobContainerClient  blobContainerClient = blobClientService.GetBlobContainerClient("trucngblob");
    
    Console.WriteLine(blobContainerClient);
    
    foreach (BlobItem blobItem in blobContainerClient.GetBlobs())
    {
        Console.WriteLine("\t" + blobItem.Name);
    }


}
catch (RequestFailedException e)
{
    Console.WriteLine(e.Message);
    Console.ReadLine();
    throw;
}
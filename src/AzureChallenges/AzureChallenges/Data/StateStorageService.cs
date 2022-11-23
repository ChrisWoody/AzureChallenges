using Azure.Storage.Blobs;

namespace AzureChallenges.Data;

public class StateStorageService
{
    private readonly BlobContainerClient _blobContainerClient;

    private StateStorageService(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient;
    }

    public static async Task<StateStorageService> Create(string storageAccountConnectionString)
    {
        var blobServiceClient = new BlobServiceClient(storageAccountConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("states");
        await blobContainerClient.CreateIfNotExistsAsync();
        return new StateStorageService(blobContainerClient);
    }

    public byte[]? GetFile(string filename)
    {
        var blobClient = _blobContainerClient.GetBlobClient(filename);
        if (!(blobClient.Exists()))
            return null;

        var content = blobClient.DownloadContent();
        return content.Value.Content.ToArray();
    }

    public void SaveFile(string filename, byte[] content)
    {
        var blobClient = _blobContainerClient.GetBlobClient(filename);
        blobClient.Upload(new BinaryData(content), overwrite: true);
    }
}
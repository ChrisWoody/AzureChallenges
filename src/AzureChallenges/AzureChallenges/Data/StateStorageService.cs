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

    public async Task<byte[]?> GetFile(string filename)
    {
        var blobClient = _blobContainerClient.GetBlobClient(filename);
        if (!(await blobClient.ExistsAsync()))
            return null;

        var content = await blobClient.DownloadContentAsync();
        return content.Value.Content.ToArray();
    }

    public async Task SaveFile(string filename, byte[] content)
    {
        var blobClient = _blobContainerClient.GetBlobClient(filename);
        await blobClient.UploadAsync(new BinaryData(content), overwrite: true);
    }
}
using System.Text.Json;
using Azure.Storage.Blobs;

namespace AzureChallenges.Data;

public class StateService
{
    private readonly BlobContainerClient _blobContainerClient;

    private StateService(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient;
    }

    public static async Task<StateService> Create(string storageAccountConnectionString)
    {
        var blobServiceClient = new BlobServiceClient(storageAccountConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient("states");
        await blobContainerClient.CreateIfNotExistsAsync();
        return new StateService(blobContainerClient);
    }

    public async Task<State> GetState()
    {
        // TODO determine current user id/name
        var blobClient = _blobContainerClient.GetBlobClient("someusernameorid.json");
        if (!(await blobClient.ExistsAsync()))
            return new State();

        var content = await blobClient.DownloadContentAsync();
        return content.Value.Content.ToObjectFromJson<State>();
    }

    public async Task SaveState(State state)
    {
        // TODO determine current user id/name
        var blobClient = _blobContainerClient.GetBlobClient("someusernameorid.json");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state);
        await blobClient.UploadAsync(new BinaryData(bytes), overwrite: true);
    }

    public async Task ChallengeCompleted(Challenge challenge)
    {
        // TODO determine current user id/name
        // TODO consider leasing
        var state = await GetState();

        state.CompletedChallenges = state.CompletedChallenges.Concat(new[] {challenge.ChallengeDefinition.Id})
            .Distinct().Order().ToArray();
        if (challenge.ChallengeDefinition.Id == Guid.Parse("ad713b6f-0f21-4889-95ee-222ef1302735"))
            state.SubscriptionId = challenge.Input;
        else if (challenge.ChallengeDefinition.Id == Guid.Parse("6e224d1a-40f2-48c7-bf38-05b47962cddf"))
            state.ResourceGroup = challenge.Input;

        await SaveState(state);
    }
}

public class State
{
    public string SubscriptionId { get; set; }
    public string ResourceGroup { get; set; }
    public Guid[] CompletedChallenges { get; set; } = Array.Empty<Guid>();
}
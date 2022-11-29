using Microsoft.AspNetCore.Components.Authorization;

namespace AzureChallenges.Data;

public class StateService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly StateCache _stateCache;

    public StateService(AuthenticationStateProvider authenticationStateProvider, StateCache stateCache)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _stateCache = stateCache;
    }

    public async Task<State> GetState()
    {
        var filename = await GetFilename();
        return _stateCache.Get(filename);
    }

    public async Task SaveState(State state)
    {
        var filename = await GetFilename();
        await _stateCache.SetAsync(filename, state);
    }

    public async Task ChallengeCompleted(Challenge challenge)
    {
        var state = await GetState();

        state.CompletedChallenges = state.CompletedChallenges.Concat(new[] {challenge.ChallengeDefinition.Id})
            .Distinct().Order().ToArray();
        if (challenge.ChallengeDefinition.Id == Guid.Parse("ad713b6f-0f21-4889-95ee-222ef1302735"))
            state.SubscriptionId = challenge.Input;
        else if (challenge.ChallengeDefinition.Id == Guid.Parse("6e224d1a-40f2-48c7-bf38-05b47962cddf"))
            state.ResourceGroup = challenge.Input;
        else if (challenge.ChallengeDefinition.Id == Guid.Parse("15202bbe-94ad-4ebf-aa15-ed93b5cef11e"))
            state.StorageAccount = challenge.Input;
        else if (challenge.ChallengeDefinition.Id == Guid.Parse("59159eab-8a58-484a-880a-fc787a00cdfc"))
            state.KeyVault = challenge.Input;
        else if (challenge.ChallengeDefinition.Id == Guid.Parse("60730b90-d133-43be-9e5a-1c181a24f921"))
            state.SqlServer = challenge.Input;
        else if (challenge.ChallengeDefinition.Id == Guid.Parse("cc194d7d-4866-46f9-b8f7-a193bd7f3810"))
            state.AppService = challenge.Input;

        await SaveState(state);
    }

    private async Task<string> GetFilename()
    {
        var identity = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var name = identity.User.Identity.Name.ToLower().Replace(' ', '_');
        return $"{name}.json";
    }

    public async Task ClearStateCacheForUser()
    {
        var filename = await GetFilename();
        await _stateCache.ClearKeyFromCache(filename);
    }
}

public class State
{
    public string SubscriptionId { get; set; }
    public string ResourceGroup { get; set; }
    public string StorageAccount { get; set; }
    public string KeyVault { get; set; }
    public string SqlServer { get; set; }
    public string AppService { get; set; }
    public Guid[] CompletedChallenges { get; set; } = Array.Empty<Guid>();
}
namespace AzureChallenges.Data;

public abstract class ChallengeServiceBase
{
    protected readonly StateService StateService;
    protected readonly AzureProvider AzureProvider;
    protected readonly IConfiguration Configuration;
    protected readonly ILogger<ChallengeServiceBase> Logger;

    protected ChallengeServiceBase(StateService stateService, AzureProvider azureProvider, IConfiguration configuration, ILogger<ChallengeServiceBase> logger)
    {
        StateService = stateService;
        AzureProvider = azureProvider;
        Configuration = configuration;
        Logger = logger;
    }

    public async Task<State> GetState()
    {
        return await StateService.GetState();
    }

    public async Task<Challenge[]> GetChallenges()
    {
        var state = await StateService.GetState();

        return GetChallengeDefinitions()
            .Select(c => new Challenge
            {
                ChallengeDefinition = c,
                Completed = state.CompletedChallenges.Any(id => id == c.Id)
            }).ToArray();
    }

    protected abstract IEnumerable<ChallengeDefinition> GetChallengeDefinitions();

    public async Task ClearState()
    {
        await StateService.SaveState(new State());
    }

    public async Task ClearStateCache()
    {
        await StateService.ClearStateCacheForUser();
    }

    public async Task CheckChallenge(Challenge challenge)
    {
        try
        {
            await challenge.ChallengeDefinition.ValidateFunc(challenge);
            if (challenge.Completed && !string.IsNullOrWhiteSpace(challenge.Success))
            {
                await StateService.ChallengeCompleted(challenge);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unexpected exception occurred");
            challenge.Error = e.Message;
            challenge.Completed = false;
        }
    }
}

public class ChallengeDefinition
{
    public Guid Id { get; init; }
    public ResourceType ResourceType { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Statement { get; set; }
    public string Hint { get; init; }
    public Func<Challenge, Task> ValidateFunc { get; init; }
    public ChallengeType ChallengeType { get; init; }
    public string[] QuizOptions { get; init; }
    public Func<State, bool>? CanShowChallenge { get; set; }
    public string Link { get; set; }
}

public class Challenge
{
    public ChallengeDefinition ChallengeDefinition { get; set; }
    public bool Checking { get; set; }
    public bool Completed { get; set; }
    public string Input { get; set; }
    public string Error { get; set; }
    public string Success { get; set; }
}

public class Section
{
    public string AzurePortalUrl { get; set; }
    public Challenge[] Challenges { get; set; }
}

public class SectionData
{
    public string SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; }
    public string ResourceName { get; set; }
}

public enum ResourceType
{
    ResourceGroup,
    StorageAccount,
    KeyVault,
    SqlServer,
    AppService,
    ServiceEndpoint,
}

public enum ChallengeType
{
    ExistsWithInput,
    CheckConfigured,
    Quiz
}
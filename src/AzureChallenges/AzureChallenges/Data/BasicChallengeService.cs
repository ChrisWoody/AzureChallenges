namespace AzureChallenges.Data;

public class BasicChallengeService : ChallengeServiceBase
{
    public BasicChallengeService(
        StateService stateService,
        AzureProvider azureProvider,
        IConfiguration configuration,
        ILogger<ChallengeServiceBase> logger)
        : base(stateService, azureProvider, configuration, logger)
    {
    }

    protected override IEnumerable<ChallengeDefinition> GetChallengeDefinitions()
    {
        return Array.Empty<ChallengeDefinition>();
    }
}
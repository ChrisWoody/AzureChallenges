namespace AzureChallenges.Data;

public class PrivateEndpointChallengeService : ChallengeServiceBase
{
    public PrivateEndpointChallengeService(
        StateService stateService,
        AzureProvider azureProvider,
        IConfiguration configuration,
        ILogger<ChallengeServiceBase> logger)
        : base(stateService, azureProvider, configuration, logger)
    {
    }

    protected override IEnumerable<ChallengeDefinition> GetChallengeDefinitions()
    {
        return new[]
        {
            new ChallengeDefinition
            {
                Id = Guid.Parse("642ea46b-bb11-40fa-9dde-ee0f310ab541"),
                ResourceType = ResourceType.PrivateEndpoint,
                Name = "Prepare for Private Endpoints",
                Description = "Though we can setup a Private Endpoint alongside the Service Endpoints and IP restrictions, its much cooler to disable these and only allow access over the Private Endpoint.",
                Statement = "For your Storage Account, Key Vault and SQL Server, disable 'public access' to them. Have you made the changes?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new[]
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Well done!";
                    }
                    else
                        c.Error = "OK I'll wait";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue() && s.VirtualNetwork.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("2c10748b-f339-4ae6-a9c2-3c5d4f11f3e3"),
                ResourceType = ResourceType.PrivateEndpoint,
                Name = "Check your website",
                Description = "With public access disabled (which removes the Service Endpoint you setup before) your website will no longer be able to access the resources.",
                Statement = "Restart your App Service (this will clear any lingering connections it has to the resources). Go to your website and refresh to see that they (shouldn't) connect. Ready to fix that?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new[]
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Well done!";
                    }
                    else
                        c.Error = "OK I'll wait";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue() && s.VirtualNetwork.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("326ffc2b-b9c9-4d27-ac94-16308eb8ba55"),
                ResourceType = ResourceType.PrivateEndpoint,
                Name = "Setup Private Endpoints",
                Description = "I'm not going to provide too much direction and checks here to increase the challenge a bit (also because I've run out of time...).",
                Statement = "Create Private Endpoints on your Storage Account, Key Vault and SQL Server. Have you set those up?",
                Hint = "I came across some issues when trying to do this, have a go at understanding what the issue is and how to resolve it. Let me know if you need a hand.",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new[]
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Well done!";
                    }
                    else
                        c.Error = "OK I'll wait";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue() && s.VirtualNetwork.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("79efb6b2-13ea-437f-a751-466d412d4212"),
                ResourceType = ResourceType.PrivateEndpoint,
                Name = "Check your website again",
                Description = "It might take a few moments, but your website should now be able to connect",
                Statement = "Refresh your website, is it connecting successfully now?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new[]
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Excellent!";
                    }
                    else
                        c.Error = "Other than waiting a moment for things to connect, check the error messages to get an idea of what issues its having.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue() && s.VirtualNetwork.HasValue()
            },
        };
    }
}
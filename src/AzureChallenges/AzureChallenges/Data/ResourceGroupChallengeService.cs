namespace AzureChallenges.Data;

public class ResourceGroupChallengeService : ChallengeServiceBase
{
    public ResourceGroupChallengeService(
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
                Id = Guid.Parse("ad713b6f-0f21-4889-95ee-222ef1302735"),
                ResourceType = ResourceType.ResourceGroup,
                Name = "Subscription",
                Description = "Before you create the Resource Group you should determine which Subscription it will live under.",
                Statement = "What is the Subscription Id?",
                Hint = "It's best to use a 'development' subscription, this means you'll have access to create/update resources and benefit from dev/test pricing.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (c.Input.HasValue() && Guid.TryParse(c.Input, out _))
                    {
                        if (await AzureProvider.SubscriptionExists(c.Input))
                        {
                            c.Completed = true;
                            c.Success = "Success!";
                        }
                        else
                            c.Error = $"Could not found subscription with id '{c.Input}'.";
                    }
                    else
                        c.Error = $"Subscription Id is not in a valid GUID format '{c.Input}'.";
                },
                CanShowChallenge = s => true
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("6e224d1a-40f2-48c7-bf38-05b47962cddf"),
                ResourceType = ResourceType.ResourceGroup,
                Name = "Create",
                Description = "Useful for grouping Azure services together, go and create one now in the subscription you specified earlier. You can also specify its Location which represents an Azure data centre, however resources can exist in another data centre if you want but its nice to keep them together for consistency.",
                Statement = "What is the name of the resource group you've created?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (c.Input.HasValue() && await AzureProvider.ResourceGroupExists(state.SubscriptionId, c.Input))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = $"Could not find resource group '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("23aca336-d1c4-4b66-806c-cddb6629f5a0"),
                ResourceType = ResourceType.ResourceGroup,
                Name = "Quiz",
                Description = "Resource Groups sit just below Subscriptions in the resource hierarchy, and below them the Azure resources themselves.",
                Statement = "What does a Subscription sit below in the resource hierarchy?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new[]
                {
                    "Storage Account", "Management Group", "Tenant", "The sky"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Tenant")
                    {
                        c.Completed = true;
                        c.Success = "That's right! When you login with AD auth to a system it will be going through a Tenant which helps manage the Azure Active Directory instance, usually this is to access resources that happen to live within that Tenant but that doesn't always have to the case.";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
        };
    }
}
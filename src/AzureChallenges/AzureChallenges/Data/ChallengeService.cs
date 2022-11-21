namespace AzureChallenges.Data;

public class ChallengeService
{
    private readonly StateService _stateService;
    private readonly AzureProvider _azureProvider;

    public ChallengeService(StateService stateService, AzureProvider azureProvider)
    {
        _stateService = stateService;
        _azureProvider = azureProvider;
    }

    public async Task<State> GetState()
    {
        return await _stateService.GetState();
    }

    public async Task<Challenge[]> GetChallenges(ResourceType resourceType)
    {
        var state = await _stateService.GetState();

        return GetChallenges()
            .Where(x => x.ResourceType == resourceType)
            .Select(c => new Challenge
            {
                ChallengeDefinition = c,
                Completed = state.CompletedChallenges.Any(id => id == c.Id)
            }).ToArray();
    }

    public async Task<bool> CanShowSection(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.ResourceGroup:
                return true;
            case ResourceType.StorageAccount:
                return (await GetChallenges(ResourceType.ResourceGroup)).All(c => c.Completed);
            default:
                throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
        }
    }

    private IEnumerable<ChallengeDefinition> GetChallenges()
    {
        return new[]
        {
            new ChallengeDefinition
            {
                Id = Guid.Parse("ad713b6f-0f21-4889-95ee-222ef1302735"),
                ResourceType = ResourceType.ResourceGroup,
                Name = "Subscription",
                Description = "Before you create the Resource Group you should determine which subscription it will live under. What is the subscription id?",
                Hint = "It's best to use a 'development' subscription, this means you'll have access to create/update resources and benefit from dev/test pricing.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (!string.IsNullOrWhiteSpace(c.Input) && Guid.TryParse(c.Input, out _))
                    {
                        if (await _azureProvider.SubscriptionExists(c.Input))
                        {
                            c.Completed = true;
                        }
                        else
                        {
                            c.Error = $"Could not found subscription with id '{c.Input}'.";
                        }
                    }
                    else
                    {
                        c.Error = $"Subscription Id is not in a valid GUID format '{c.Input}'.";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("6e224d1a-40f2-48c7-bf38-05b47962cddf"),
                ResourceType = ResourceType.ResourceGroup,
                Name = "Create",
                Description = "Useful for grouping Azure services together, go and create one now in the subscription you specified earlier. What is the name of the resource group you've created?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) && await _azureProvider.ResourceGroupExists(state.SubscriptionId, c.Input))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = $"Could not find resource group '{c.Input}'.";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("23aca336-d1c4-4b66-806c-cddb6629f5a0"),
                ResourceType = ResourceType.ResourceGroup,
                Name = "Quiz",
                Description = "Subscriptions are a level above Resource Groups, whats the level above a Subscription?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Storage Account", "Management Group", "Tenant", "The sky"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Tenant")
                        c.Completed = true;
                    else
                        c.Error = "Sorry that's not correct";
                }
            },

            new ChallengeDefinition
            {
                Id = Guid.Parse("15202bbe-94ad-4ebf-aa15-ed93b5cef11e"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Create",
                Description = "Storage accounts do stuff, create one, what's the name?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) && await _azureProvider.StorageAccountExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = $"Could not find storage account '{c.Input}'.";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("94fbda2b-b310-484f-960a-b7ac804aea1e"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Secure Transfer",
                Description = "All requests should be over HTTPS",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) &&
                        await _azureProvider.StorageAccountHttpsTrafficOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = "Storage Account is not configured correctly";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("23bd7ee0-ab51-4a91-a4dc-ddb5f4cf4877"),
                ResourceType = ResourceType.StorageAccount,
                Name = "TLS1.2",
                Description = "All our requests should be over TLS1.2, this allows us to enforce it at the resource level too.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) &&
                        await _azureProvider.StorageAccountTls12Configured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = "Storage Account is not configured correctly";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ab8c1780-738a-49d2-9474-db6a01865c99"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Public network access",
                Description = "Rarely would we have files publicly accessible, so its best practice to not allow them to be configured",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) &&
                        await _azureProvider.StorageAccountPublicBlobAccessEnabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = "Storage Account is not configured correctly";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("c39e95d7-daaf-4635-9ecb-9a78cafff9b8"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Shared access key",
                Description = "To avoid having the full connection string of a storage account, we can configure AD-only auth to it",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) &&
                        await _azureProvider.StorageAccountSharedKeyAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = "Storage Account is not configured correctly";
                    }
                }
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("50354c41-a4ce-4090-8f64-db87c2e539cb"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Public network access",
                Description = "If we're not making our data public accessible, we don't need the storage account to be accessible either. There may be cases where we want to access it but with credentials, but viewing it from a 'secure first' perspective lets lock it down as much as possible.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) &&
                        await _azureProvider.StorageAccountPublicNetworkAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = "Storage Account is not configured correctly";
                    }
                }
            }
        };
    }

    public async Task CheckChallenge(Challenge challenge)
    {
        try
        {
            await challenge.ChallengeDefinition.ValidateFunc(challenge);
            if (challenge.Completed)
            {
                await _stateService.ChallengeCompleted(challenge);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            challenge.Error = e.Message;
            challenge.Completed = false;
        }
    }

    public async Task ClearState()
    {
        await _stateService.SaveState(new State());
    }
}

public class ChallengeDefinition
{
    public Guid Id { get; init; }
    public ResourceType ResourceType { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Hint { get; init; }
    public Func<Challenge, Task> ValidateFunc { get; init; }
    public ChallengeType ChallengeType { get; init; }
    public string[] QuizOptions { get; init; }
}

public class Challenge
{
    public ChallengeDefinition ChallengeDefinition { get; set; }
    public bool Checking { get; set; }
    public bool Completed { get; set; }
    public string Input { get; set; }
    public string Error { get; set; }
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
    StorageAccount
}

public enum ChallengeType
{
    ExistsWithInput,
    CheckConfigured,
    Quiz
}
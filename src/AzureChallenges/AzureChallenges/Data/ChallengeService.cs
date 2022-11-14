using System.Text;

namespace AzureChallenges.Data;

public class ChallengeService
{
    private readonly StateService _stateService;

    public ChallengeService(StateService stateService)
    {
        _stateService = stateService;
    }

    public async Task<Section> GetResourceGroupSection()
    {
        var state = await _stateService.GetState();

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(state.SubscriptionId))
        {
            sb.Append($"https://portal.azure.com/#{"tenantname"}/resource/subscriptions/{state.SubscriptionId}");
            if (!string.IsNullOrWhiteSpace(state.ResourceGroup))
            {
                sb.Append($"/resourceGroups/{state.ResourceGroup}");
            }
        }

        return new Section
        {
            AzurePortalUrl = sb.ToString(),
            Challenges = GetChallenges()
                .Where(x => x.ResourceType == ResourceType.ResourceGroup)
                .Select(c => new Challenge
                {
                    ChallengeDefinition = c,
                    Completed = state.CompletedChallenges.Any(id => id == c.Id)
                }).ToArray()
        };
    }

    private static IEnumerable<ChallengeDefinition> GetChallenges()
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
                RequiresInput = true,
                ValidateFunc = async c =>
                {
                    if (Guid.TryParse(c.Input, out var subscriptionId))
                    {
                        // TODO actually check subscription exists instead of a random guid
                        if (subscriptionId == Guid.Parse("cc89fb4b-6c0b-4cd5-84e6-da12983db997"))
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
                RequiresInput = true,
                ValidateFunc = async c =>
                {
                    // TODO actually check resource group exists
                    if (c.Input == "testrg")
                    {
                        c.Completed = true;
                    }
                    else
                    {
                        c.Error = $"Could not find resource group '{c.Input}'.";
                    }
                }
            }
        };
    }

    public async Task CheckChallenge(Challenge challenge)
    {
        await Task.Delay(1000); // TODO will remove, just simulating a live environment

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
}

public class ChallengeDefinition
{
    public Guid Id { get; set; }
    public ResourceType ResourceType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Hint { get; set; }
    public bool RequiresInput { get; set; }
    public Func<Challenge, Task> ValidateFunc { get; set; }

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

public enum ResourceType
{
    ResourceGroup,
    StorageAccount
}
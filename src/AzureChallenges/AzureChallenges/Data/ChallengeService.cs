namespace AzureChallenges.Data;

public class ChallengeService
{
    public async Task<Challenge[]> GetResourceGroupChallenges()
    {
        return GetChallenges().Select(c => new Challenge
        {
            ChallengeDefinition = c,
            Checking = false,
            Completed = false, // populate from state
            Error = "",
            Input = ""
        }).ToArray();
    }

    private static IEnumerable<ChallengeDefinition> GetChallenges()
    {
        return new[]
        {
            new ChallengeDefinition
            {
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
        await Task.Delay(1000);

        try
        {
            await challenge.ChallengeDefinition.ValidateFunc(challenge);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            challenge.Error = e.Message;
        }
    }
}

public class ChallengeDefinition
{
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

public enum ResourceType
{
    ResourceGroup,
    StorageAccount
}
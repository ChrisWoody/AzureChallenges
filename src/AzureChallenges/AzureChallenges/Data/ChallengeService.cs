﻿namespace AzureChallenges.Data;

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
    public Guid Id { get; set; }
    public ResourceType ResourceType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Hint { get; set; }
    public Func<Challenge, Task> ValidateFunc { get; set; }
    public ChallengeType ChallengeType { get; set; }
    public string[] QuizOptions { get; set; }

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
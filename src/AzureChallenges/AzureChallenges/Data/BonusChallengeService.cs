namespace AzureChallenges.Data;

public class BonusChallengeService : ChallengeServiceBase
{
    public BonusChallengeService(
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
                Id = Guid.Parse("caaae38b-0ba9-4594-aa12-4b9b283c32bf"),
                ResourceType = ResourceType.Bonus,
                Name = "Write your own 'check' website",
                Description = "Have a go creating your own website that shows your App Service can successfully connect to all of the other resources with its Managed Identity.",
                Statement = "Hit the button to complete the challenge",
                Hint = "When testing locally you need to make sure you AD account has access to those services and that you can connect to them from your machine.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    c.Completed = true;
                    c.Success = "Well done!";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("015a8a1c-545f-4d93-8126-5d9e3d03291b"),
                ResourceType = ResourceType.Bonus,
                Name = "Setup a Azure Bastion instance",
                Description = "Azure Bastion is very helpful if you've setup Private Endpoints, an Azure Bastion instance would allow you to RDP into the Virtual Network and access resources. Have a go at " +
                              "provisioning one in your Virtual Network, and try to connect/query your resources.",
                Statement = "Hit the button to complete the challenge",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    c.Completed = true;
                    c.Success = "Well done!";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("f34d4d50-1d6d-48fc-b6db-13af95c01f3e"),
                ResourceType = ResourceType.Bonus,
                Name = "Setup a Network Security Group",
                Description = "Network Security Groups (NSGs) can be joined to subnets and/or NICs to control requests inbound to them or outbound from them. Have a go blocking traffic from different sources and destinations.",
                Statement = "Hit the button to complete the challenge",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    c.Completed = true;
                    c.Success = "Well done!";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("7f09ebc9-5e71-441d-a221-d91d1e21f4c6"),
                ResourceType = ResourceType.Bonus,
                Name = "Setup a peered Virtual Network",
                Description = "Virtual Network peering allows you to join two or more Virtual Networks together in one larger Virtual Network. Have a go creating another Virtual Network with a resource attached (i.e. another Storage Account), peer it to your original Virtual Network, and try have you website connect to the new resource.",
                Statement = "Hit the button to complete the challenge",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    c.Completed = true;
                    c.Success = "Well done!";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("97e70a69-6a64-4d47-b85e-eac8cbbcdedd"),
                ResourceType = ResourceType.Bonus,
                Name = "Cleanup your resources",
                Description = "Feel free to play around in your Resource Group, but once you're done its time to delete everything. If you've made it this far I hope you've had fun and learned something useful about Azure and some ways that we can secure it.",
                Statement = "Hit the button to complete the challenge",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    c.Completed = true;
                    c.Success = "Well done, and thanks for playing!";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue() && s.StorageAccount.HasValue() && s.KeyVault.HasValue()
            },
        };
    }
}
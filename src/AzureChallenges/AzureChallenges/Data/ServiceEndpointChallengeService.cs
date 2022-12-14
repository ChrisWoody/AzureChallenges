namespace AzureChallenges.Data;

public class ServiceEndpointChallengeService : ChallengeServiceBase
{
    public ServiceEndpointChallengeService(
        StateService stateService,
        AzureProvider azureProvider,
        IConfiguration configuration,
        ILogger<ChallengeServiceBase> logger)
        : base(stateService, azureProvider, configuration, logger)
    {
    }

    protected override IEnumerable<ChallengeDefinition> GetChallengeDefinitions()
    {
        // service endpoint notes
        // - connect app service
        // - can still view website?
        
        return new[]
        {
            new ChallengeDefinition
            {
                Id = Guid.Parse("19247c68-09a7-4b9a-bc59-fd10186ce546"),
                ResourceType = ResourceType.ServiceEndpoint,
                Name = "Create Virtual Network",
                Description = "Virtual Networks allow you to simplify how Azure resources are connected. Usually by default all Azure resources connect to each other publicly," +
                              " but by using Virtual Networks we can restrict traffic to resources from our allowed list of IPs and from within that Virtual Network.",
                Statement = "Create a Virtual Network in your Resource Group with a 10.0.0.0/16 'address space' and a 10.0.0.0/24 'default' subnet, don't worry about the extra security features for now. What is your Virtual Network called?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (c.Input.HasValue() && await AzureProvider.VirtualNetworkExistsAndIsConfigured(state.SubscriptionId, state.ResourceGroup, c.Input))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = $"Could not find Virtual Network '{c.Input}', or it is not configured correctly.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("fa34eecd-28f6-4a9b-85b4-5e0b0040e42d"),
                ResourceType = ResourceType.ServiceEndpoint,
                Name = "Quiz",
                Statement = "For the 'default' subnet, what is the 'Available IPs' number?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (c.Input.HasValue() && c.Input == "251")
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's incorrect";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.VirtualNetwork.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("202b860e-f21e-49d5-a627-d78e7fc976b9"),
                ResourceType = ResourceType.ServiceEndpoint,
                Name = "Enable Service Endpoints",
                Description = "Resources like VMs can connect directly to a Virtual Network, however resources like Storage Accounts and Key Vaults require a special connection " +
                              "to be able to join a Virtual Network. This is where Service Endpoints come in, they allow these other Azure resources to join Virtual Networks and access resources contained within them.",
                Statement = "On the 'default' subnet, enable the 'Microsoft.KeyVault', 'Microsoft.Sql' and 'Microsoft.Storage' Service Endpoints.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.VirtualNetwork.HasValue() && await AzureProvider.VirtualNetworkSubnetHasServiceEndpointsConfigured(state.SubscriptionId, state.ResourceGroup, state.VirtualNetwork))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Virtual Network is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.VirtualNetwork.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("2885cc72-a4c9-4e27-85e9-3645af5634de"),
                ResourceType = ResourceType.ServiceEndpoint,
                Name = "Connect Storage Account to Virtual Network",
                Description = "Now that the Service Endpoints are enabled, we can join resources to it.",
                Statement = "Connect your Storage Account to the Virtual Network's 'default' subnet.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.VirtualNetwork.HasValue() && await AzureProvider.StorageAccountIsConnectedToVirtualNetwork(state.SubscriptionId, state.ResourceGroup, state.VirtualNetwork, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.VirtualNetwork.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("1ca0121b-1ef7-4a3e-b8a7-93c67ce89cfd"),
                ResourceType = ResourceType.ServiceEndpoint,
                Name = "Connect Key Vault to Virtual Network",
                Description = "Now that the Service Endpoints are enabled, we can join resources to it.",
                Statement = "Connect your Key Vault to the Virtual Network's 'default' subnet.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.VirtualNetwork.HasValue() && await AzureProvider.KeyVaultIsConnectedToVirtualNetwork(state.SubscriptionId, state.ResourceGroup, state.VirtualNetwork, state.KeyVault))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Key Vault is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.VirtualNetwork.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("47345b68-5e21-420a-8814-12ba904cd7a0"),
                ResourceType = ResourceType.ServiceEndpoint,
                Name = "Connect SQL Server to Virtual Network",
                Description = "Now that the Service Endpoints are enabled, we can join resources to it.",
                Statement = "Connect your SQL Server to the Virtual Network's 'default' subnet.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.VirtualNetwork.HasValue() && await AzureProvider.SqlServerIsConnectedToVirtualNetwork(state.SubscriptionId, state.ResourceGroup, state.VirtualNetwork, state.SqlServer))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.VirtualNetwork.HasValue() && s.SqlServer.HasValue()
            },
        };
    }
}
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
            // Resource Group --------------------------------------------------------------------------------------------------------
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
                    if (c.Input.HasValue() && Guid.TryParse(c.Input, out _))
                    {
                        if (await _azureProvider.SubscriptionExists(c.Input))
                            c.Completed = true;
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
                Description = "Useful for grouping Azure services together, go and create one now in the subscription you specified earlier. What is the name of the resource group you've created?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.ResourceGroupExists(state.SubscriptionId, c.Input))
                        c.Completed = true;
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
                Description = "Resource Groups sit just below Subscriptions in the resource hierarchy, what does a Subscription sit below?",
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
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },

            // Storage Account --------------------------------------------------------------------------------------------------------
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
                        c.Completed = true;
                    else
                        c.Error = $"Could not find Storage Account '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
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
                    if (!string.IsNullOrWhiteSpace(state.StorageAccount) && await _azureProvider.StorageAccountHttpsTrafficOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                        c.Completed = true;
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
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
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountTls12Configured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                        c.Completed = true;
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ab8c1780-738a-49d2-9474-db6a01865c99"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Public blob access",
                Description = "Rarely would we have files publicly accessible, so its best practice to not allow them to be configured",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountPublicBlobAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                        c.Completed = true;
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
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
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountSharedKeyAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                        c.Completed = true;
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
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
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountPublicNetworkAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                        c.Completed = true;
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("fab1e5f0-e14a-4593-b672-aa9b41c153b6"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Challenge prep and Quiz",
                Description = "In preparation for future challenges, create another storage account that we'll use to store logs. Unlike the Storage Account you've just configured, for this new one make sure that 'Shared Access Key' and 'Public Network Access' are both allowed, and that it's located in the same region as the original Storage Account. Have you created this new Storage Account?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No", "Maybe", "I don't know"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                        c.Completed = true;
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            // TODO validate that this works
            new ChallengeDefinition
            {
                Id = Guid.Parse("a0a62f76-77cb-4cde-b2b0-c54da6ac00eb"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Diagnostic settings",
                Description = "Several Azure resources support 'Diagnostic Settings' which allow you to log operations against the resource. For this challenge configure the Diagnostic Settings for 'blob' on your original Storage Account with StorageRead, StorageWrite and StorageDelete enabled, uploading to your newer 'log' storage account.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountBlobDiagnosticSettingsConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                        c.Completed = true;
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("c9ceb040-3e34-4b4b-a655-9634b522490c"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Quiz",
                Description = "Upload a random file to the original storage account, then check the 'log' storage account to see the results. What is the file extension of the log files?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (string.Equals(c.Input, "json", StringComparison.InvariantCultureIgnoreCase))
                        c.Completed = true;
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },


            // Key Vault --------------------------------------------------------------------------------------------------------
            new ChallengeDefinition
            {
                Id = Guid.Parse("59159eab-8a58-484a-880a-fc787a00cdfc"),
                ResourceType = ResourceType.KeyVault,
                Name = "Create",
                Description = "Key Vaults are cool for storing secrets and certificates",
                Hint = "For the purpose of these challenges, make sures it's created with the 'vault access policy', which is the default.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.KeyVaultExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                        c.Completed = true;
                    else
                        c.Error = $"Could not find Key Vault '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("b93ec8ad-17d1-46c7-817e-db7d2b76125d"),
                ResourceType = ResourceType.KeyVault,
                Name = "Assign user",
                Description = "By creating the key vault you get full access to it, assign someone else with just Secret 'Get' and 'List' permissions",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.KeyVault.HasValue() && await _azureProvider.KeyVaultSecretAccessConfigured(state.SubscriptionId, state.ResourceGroup, state.KeyVault))
                        c.Completed = true;
                    else
                        c.Error = "Key Vault is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.KeyVault.HasValue()
            },

            // SQL Server --------------------------------------------------------------------------------------------------------
            new ChallengeDefinition
            {
                Id = Guid.Parse("60730b90-d133-43be-9e5a-1c181a24f921"),
                ResourceType = ResourceType.SqlServer,
                Name = "Create",
                Description = "SQL Server stores pools and databases",
                Hint = "NOTE you won't be able to create a SQL Server via the Azure Portal because of our Azure Policy and that functionality is missing, use the command line OR i can provision for you",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.SqlServerExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                        c.Completed = true;
                    else
                        c.Error = $"Could not find Sql Server '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("3153aaa4-6cb3-4688-a13c-6d5da6db12ca"),
                ResourceType = ResourceType.SqlServer,
                Name = "TLS1.2",
                Description = "We should at a minimum be using TLS 1.2",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.SqlServer.HasValue() && await _azureProvider.SqlServerTls12Configured(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
                        c.Completed = true;
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue()
            },

            // App Service --------------------------------------------------------------------------------------------------------
            new ChallengeDefinition
            {
                Id = Guid.Parse("cc194d7d-4866-46f9-b8f7-a193bd7f3810"),
                ResourceType = ResourceType.AppService,
                Name = "Create",
                Description = "App Services allow us to host websites and run background jobs",
                Hint = "A Basic tier App Service Plan is fine for this exercise, it's cheap and we'll delete it later.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.AppServiceExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                        c.Completed = true;
                    else
                        c.Error = $"Could not find Sql Server '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("129ad12d-6e94-4ac6-bc3f-efc2c2c5c5d5"),
                ResourceType = ResourceType.AppService,
                Name = "HTTPS Only",
                Description = "Regardless if we're using the App Service as a website of a webjob runner, we should always be using HTTPS.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceHttpsOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                        c.Completed = true;
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("89d7bafc-d52b-4c3c-9a5d-2bfd4cb21e2e"),
                ResourceType = ResourceType.AppService,
                Name = "Always On",
                Description = "If we're paying for the app service regardless if its actively used or not, we should have 'Always On' enabled, this improves cold start time for accessing the website and deployments.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceAlwaysOnConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                        c.Completed = true;
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("75f2e941-a8f2-4e35-8a0b-f0ef43a8b8bd"),
                ResourceType = ResourceType.AppService,
                Name = "TLS 1.2",
                Description = "Should always be using TLS 1.2 at least",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceTls12Configured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                        c.Completed = true;
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ec4db30e-02f3-48e7-a37b-749587d7a7d2"),
                ResourceType = ResourceType.AppService,
                Name = "FTP Disabled",
                Description = "We never use FTP to deploy to an App Service, it should be disabled, or at least only allow FTPS (their lingo, basically SFTP).",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceFtpDisabled(state.SubscriptionId, state.ResourceGroup, state.AppService))
                        c.Completed = true;
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ab1144b4-951f-4948-908b-996d95dcdef8"),
                ResourceType = ResourceType.AppService,
                Name = "System assigned identity",
                Description = "We don't want to have to manage credentials to services like Storage Accounts or Key Vaults, so configure the App Service to have a System assigned identity.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceSystemIdentityAssigned(state.SubscriptionId, state.ResourceGroup, state.AppService))
                        c.Completed = true;
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("2ac3e0e9-0fef-4302-897f-17411684ea51"),
                ResourceType = ResourceType.AppService,
                Name = "IP Security Restriction",
                Description = "If the website is only for internal use, we should be IP restricting to your office IP. This doesn't replace authentication/authorisation best practices, it's just another layer of security.",
                Hint = "If you're working from home you can use your home IP instead of the office IP.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceIpAccessRestriction(state.SubscriptionId, state.ResourceGroup, state.AppService))
                        c.Completed = true;
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
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

    public async Task ClearStateCache()
    {
        await _stateService.ClearStateCacheForUser();
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
    public Func<State, bool>? CanShowChallenge { get; set; }
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
    StorageAccount,
    KeyVault,
    SqlServer,
    AppService,
}

public enum ChallengeType
{
    ExistsWithInput,
    CheckConfigured,
    Quiz
}
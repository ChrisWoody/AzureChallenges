namespace AzureChallenges.Data;

public class ChallengeService
{
    private readonly StateService _stateService;
    private readonly AzureProvider _azureProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(StateService stateService, AzureProvider azureProvider, IConfiguration configuration, ILogger<ChallengeService> logger)
    {
        _stateService = stateService;
        _azureProvider = azureProvider;
        _configuration = configuration;
        _logger = logger;
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
                Description = "Before you create the Resource Group you should determine which Subscription it will live under.",
                Statement = "What is the Subscription Id?",
                Hint = "It's best to use a 'development' subscription, this means you'll have access to create/update resources and benefit from dev/test pricing.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (c.Input.HasValue() && Guid.TryParse(c.Input, out _))
                    {
                        if (await _azureProvider.SubscriptionExists(c.Input))
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
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.ResourceGroupExists(state.SubscriptionId, c.Input))
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
                QuizOptions = new []
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

            // Storage Account --------------------------------------------------------------------------------------------------------
            new ChallengeDefinition
            {
                Id = Guid.Parse("15202bbe-94ad-4ebf-aa15-ed93b5cef11e"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Create",
                Description = "Storage accounts do stuff, create one.",
                Statement = "What is the name of the Storage Account you've created?",
                Hint = "Try to keep your resources in the same Location as the Resource Group, and don't worry too much about the various options when creating a storage account, we'll configure them in the next set of challenges.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) && await _azureProvider.StorageAccountExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Description = "All requests should be over HTTPS, especially if we're dealing with sensitive data.",
                Statement = "Make 'Secure transfer required' is enabled on the Storage Account.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (!string.IsNullOrWhiteSpace(state.StorageAccount) && await _azureProvider.StorageAccountHttpsTrafficOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Statement = "Make sure the minimum TLS version is configured as '1.2' on the Storage Account",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountTls12Configured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Depending on how you provisioned the Storage Account, you may have noticed you couldn't create it unless you specified TLS 1.2. Our Azure Policy prevents most services from being provisioned or updated unless TLS 1.2 is set.";
                    }
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ab8c1780-738a-49d2-9474-db6a01865c99"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Public Blob access",
                Description = "Rarely would we have files publicly accessible, so its best practice to not allow them to be configured",
                Statement = "Make sure 'Allow Public blob access' is disabled on the Storage Account",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountPublicBlobAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("50354c41-a4ce-4090-8f64-db87c2e539cb"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Public Network Access",
                Description = "Even if we don't allow blobs to be publicly (i.e. anonymously) accessible, its still possible to connect to a Storage Account from anywhere. There is a flag that allows you to disable any public network access, which means only connections can be made from within a virtual network.",
                Statement = "We'll leave this flag disabled for now and come back to it later. Is that OK with you?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("c39e95d7-daaf-4635-9ecb-9a78cafff9b8"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Shared account key access",
                Description = "To avoid having the full connection string of a storage account, we can configure AD-only auth to it",
                Statement = "Make sure 'Allow storage account key access' is disabled on the Storage Account'",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountSharedKeyAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Description = "In preparation for future challenges, create another storage account that we'll use to store logs. Unlike the Storage Account you've just configured, for this new one make sure that 'Shared Access Key' and 'Public Network Access' are both allowed, and that it's located in the same region as the original Storage Account.",
                Statement = "Have you created this new 'log' Storage Account?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No", "Maybe", "I don't know"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("a0a62f76-77cb-4cde-b2b0-c54da6ac00eb"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Diagnostic settings",
                Description = "Several Azure resources support 'Diagnostic Settings' which allow you to log operations against the resource, with this you can audit all the operations against a resource, including who accessed it and when. Also by disabling the Shared Access Key and requiring AD Auth, this can be correlated to an identity.",
                Statement = "Configure the Diagnostic Settings for 'blob' on your original Storage Account with StorageRead, StorageWrite and StorageDelete enabled, uploading to your new 'log' storage account",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.StorageAccount.HasValue() && await _azureProvider.StorageAccountBlobDiagnosticSettingsConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Description = "Upload a random file to the original Storage Account (you'll need to create a Container first), then check the 'log' Storage Account to see the results. Note it may take a few minutes for the logs to appear, and they are not stored in the '$logs' container.",
                Statement = "What is the file extension of the log files?",
                Hint = "You can access your original Storage Account via the Azure Portal or the Azure Storage Explorer. Or even via the command line if you're feeling adventurous.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (string.Equals(c.Input, "json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("e3f14e1d-4c92-4ce7-b7c2-eaeba5710aa6"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Quiz",
                Description = "The three operations that we're monitoring (read, write and delete) each have their own container. Find the log file for the 'write' operation, which will contain information about the file you uploaded.",
                Statement = "What is the 'operationName' used for when you uploaded the file?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (string.Equals(c.Input, "PutBlob", StringComparison.InvariantCultureIgnoreCase))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Statement = "Create a Key Vault. What is its name?",
                Hint = "For the purpose of these challenges, make sure it's created with the 'vault access policy' (which is the default).",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.KeyVaultExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = $"Could not find Key Vault '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("93e5852f-c668-4764-a6d5-6ec977054628"),
                ResourceType = ResourceType.KeyVault,
                Name = "Diagnostic settings",
                Description = "See what happens and who did what",
                Statement = $"Configure the Diagnostic Settings on the Key Vault to your logging Storage Account, with 'audit 'and 'allLogs' enabled",
                Hint = "It may take a litte bit for the logs to appear in the Storage Account, don't worry to much about it, you can look at them later.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.KeyVault.HasValue() && await _azureProvider.KeyVaultDiagnosticSettingsConfigured(state.SubscriptionId, state.ResourceGroup, state.KeyVault))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Key Vault is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("b93ec8ad-17d1-46c7-817e-db7d2b76125d"),
                ResourceType = ResourceType.KeyVault,
                Name = "Assign user",
                Description = "By creating the key vault you get full access to it, however you should grant yourself and whoever needs access limited read-only permissions",
                Statement = $"Assign the '{_configuration["WebsiteServicePrincipalName"]}' user to your Key Vault only with Secret 'Get' and 'List' permissions",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.KeyVault.HasValue() && await _azureProvider.KeyVaultSecretAccessConfigured(state.SubscriptionId, state.ResourceGroup, state.KeyVault, _configuration["WebsiteServicePrincipalObjectId"]))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Key Vault is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("c19e0669-a94d-4056-851a-ad7147292c8b"),
                ResourceType = ResourceType.KeyVault,
                Name = "Secrets",
                Description = "Secrets allow you to store secrets, they can have versions and expirations.",
                Statement = $"Generate a Secret with 'super-secret' as the name and any value that you want, which the website will retrieve.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();

                    if (state.KeyVault.HasValue())
                    {
                        var secretValue = await _azureProvider.GetKeyVaultSecretValue(state.KeyVault, "super-secret");
                        if (!string.IsNullOrWhiteSpace(secretValue))
                        {
                            c.Success = $"The super secret: {secretValue}";
                            c.Completed = true;
                        }
                        else
                            c.Error = "Key Vault is not configured correctly";
                    }
                    else
                        c.Error = "Key Vault is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("a5da83f2-47ab-4db5-9c33-2a529190220c"),
                ResourceType = ResourceType.KeyVault,
                Name = "Quiz",
                Statement = "What is the text on the button that allows you to view the secret's value in the Azure portal?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (string.Equals(c.Input, "show secret value", StringComparison.InvariantCultureIgnoreCase))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("50354c41-a4ce-4090-8f64-db87c2e539cb"),
                ResourceType = ResourceType.KeyVault,
                Name = "Public Network Access",
                Description = "Similar to the Storage Account, it is possible to restrict access to the Key Valut to only over a virtual network.",
                Statement = "But again we'll leave this flag disabled for now, OK?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
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
                // TODO consider providing more direction around the auth, likely support sql admin and ad auth and they can choose which one to connect with later on
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.SqlServerExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("fd64f4d9-43ac-43ca-b22e-933320bc4623"),
                ResourceType = ResourceType.SqlServer,
                Name = "Auditing",
                Description = "With Auditing on SQL Server, it is possible to see every query made against databases on the server, including by who, when and how long the query took to run.",
                Statement = "Configure Auditing on your SQL Server, pointing to your 'log' storage account.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.SqlServer.HasValue() && await _azureProvider.SqlServerAuditingEnabled(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("33e20f16-68d2-431f-9691-91a95a5105d4"),
                ResourceType = ResourceType.SqlServer,
                Name = "IP Restriction",
                Description = "By default SQL Server will block all incoming requests unless you allow them via IP Restrictions, vnets or allowing all Azure resources.",
                Statement = "Configure an IP Restriction on your SQL Server with any IP (i.e. your work or home IP)",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.SqlServer.HasValue() && await _azureProvider.SqlServerAnyIpRestriction(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ac5e0f0e-87e7-4c5c-a4f5-342e95e1f2b6"),
                ResourceType = ResourceType.SqlServer,
                Name = "Allowing Azure Resources",
                Description = "If we just had an IP restriction to connect via the office, services like an App Service will fail to connect to the SQL Server to query a database. We'll go through a couple of ways to connect securely later, but for now we'll just allow any Azure resource to connect.",
                Statement = "Configure the 'Allow Azure services and resources to access this server' exception on your SQL Server",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.SqlServer.HasValue() && await _azureProvider.SqlServerAllowAzureResourcesException(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("0533517c-d2d4-4c48-86e4-19e7d11de4d8"),
                ResourceType = ResourceType.SqlServer,
                Name = "Query the master database",
                Description = "Since you've added an IP Restriction so that you can connect to SQL Server, you can now query it to see the auditing logs that are generated.",
                Statement = "Connect to the SQL Server however you want (C#, Linqpad or SSMS to name a few) and run any query you want. Have you run the query?",
                Hint = "The query could just be getting the current time of the SQL Server, it doesn't matter as long as its run.",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
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
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.SqlServer.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("c1b75b2e-f606-4193-be84-46a7fb126c1c"),
                ResourceType = ResourceType.SqlServer,
                Name = "Quiz",
                Statement = "What is the file extension of the SQL audit log files?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (string.Equals(c.Input, "xel", StringComparison.InvariantCultureIgnoreCase))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("c1b75b2e-f606-4193-be84-46a7fb126c1c"),
                ResourceType = ResourceType.SqlServer,
                Name = "Optional Quiz - Inspect audit log",
                Description = "If you have SQL Server Management Studio (SSMS) installed you can view the audit log file. " +
                              "By default it only shows the 'name' and 'timestamp' columns, you can choose whichs columns appear including 'statement' which is the actual SQL query that was run.",
                Statement = "What is the 'action_name' of the query you ran? Use 'who knows' if you'd like to skip this challenge.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    if (string.Equals(c.Input, "BATCH COMPLETED", StringComparison.InvariantCultureIgnoreCase))
                    {
                        c.Completed = true;
                        c.Success = "Nice work!";
                    }
                    else if (string.Equals(c.Input, "who knows", StringComparison.InvariantCultureIgnoreCase))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "Sorry that's not correct";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.StorageAccount.HasValue()
            },

            // App Service --------------------------------------------------------------------------------------------------------
            new ChallengeDefinition
            {
                Id = Guid.Parse("cc194d7d-4866-46f9-b8f7-a193bd7f3810"),
                ResourceType = ResourceType.AppService,
                Name = "Create",
                Description = "App Services allow us to host websites and run background jobs",
                Statement = "Create an App Service on Basic tier, .NET 7 and without Application Insights. What is the name of the App Service?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (c.Input.HasValue() && await _azureProvider.AppServiceExists(state.SubscriptionId, state.ResourceGroup, c.Input))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = $"Could not find App Service '{c.Input}'.";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("129ad12d-6e94-4ac6-bc3f-efc2c2c5c5d5"),
                ResourceType = ResourceType.AppService,
                Name = "HTTPS Only",
                Description = "Regardless if we're using the App Service as a website or a webjob runner, we should always be using HTTPS.",
                Statement = "Make sure the 'HTTPS Only' flag is enabled",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceHttpsOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Statement = "Make sure the 'Always On' flag is enabled",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceAlwaysOnConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Statement = "Maked sure TLS is configured at 1.2",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceTls12Configured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Even with our Azure Policy for TLS 1.2, any newly created App Service will default to TLS 1.2 anyway";
                    }
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
                Statement = "Make sure 'FTP State' is set to 'Disabled'",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceFtpDisabled(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ab1144b4-951f-4948-908b-996d95dcdef8"),
                ResourceType = ResourceType.AppService,
                Name = "System assigned Identity",
                Description = "We don't want to have to manage credentials to services like Storage Accounts or Key Vaults, so configure the App Service to have a System assigned identity.",
                Statement = "Enable the 'System assigned' Identity on the App Service",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceSystemIdentityAssigned(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
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
                Statement = "Add an IP Restriction on your app service (either for the office IP or your home IP)",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceIpAccessRestriction(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("f69e66a5-6e89-4229-a6aa-478a53ec7f9a"),
                ResourceType = ResourceType.AppService,
                Name = "App Service logs",
                Description = "Though App Service support Diagnostic logs (setup the same as the Storage Account and Key Vault), for this challenge we'll look at the 'App Service logs'. " +
                              "This allows us to store logs on the App Service's local storage or export to a Storage Account. With local storage configured you can use log streaming too.",
                Statement = "Configure 'Application logging (Blob)' and 'Web server logging' to your 'log' Storage Account. You can put them in 'logs' and 'logs-iis' containers respectively if you want.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceLogsConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
                    {
                        c.Completed = true;
                        c.Success = "Success!";
                    }
                    else
                        c.Error = "App Service is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("ae4d8fc4-f3bc-48aa-8c87-b1ee0be2b495"),
                ResourceType = ResourceType.AppService,
                Name = "Optional - Inspect generated logs",
                Description = "Navigate to your website a few times and have a look at the IIS logs. These will show when a HTTP request is made, the path, response status code and more.",
                Statement = "Did you inspect the logs?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    c.Completed = true;
                    c.Success = "Wonderful!";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("49b09563-44e6-4203-8f49-cede211d9bba"),
                ResourceType = ResourceType.AppService,
                Name = "'Connection checker' website configuration",
                Description = "You have provisioned a couple of services now, lets validate that the App Service can connect to them with it's identity. Before we deploy the 'Connection checker' website you'll need to configure some 'Application settings' on your App Service.",
                Statement = $"Add the following Application settings: StorageAccountName = {{your Storage Account name}}, KeyVaultName = {{your Key Vault name}}, SqlServerName = {{your SQL Server name}} and TenantId = {_configuration["TenantId"]}. Have you added those settings?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
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
                        c.Error = "OK i'll wait";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("9d2692c6-4c3c-4443-a75a-2ed0584572f6"),
                ResourceType = ResourceType.AppService,
                Name = "'Connection checker' website upload",
                Description = "There are various ways to deploy to an App Service, here we'll use the Kudu portal and have it nicely deploy our .zip for us.",
                Statement = "Right-click and save the zip, navigate to 'https://<website name here>.scm.azurewebsites.net/ZipDeployUI' and drag the zip onto the page. After that's completed navigate to your website 'https://<website name here>.azurewebsites.net'. You might see some errors but that's OK! Are you ready to continue?",
                Link = "/downloads/AzureChallenges.ConnectionCheckerWebsite.zip",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes")
                    {
                        c.Completed = true;
                        c.Success = "OK!";
                    }
                    else
                        c.Error = ":(";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("cf1a2b36-95b6-4f22-962a-f38ca9b0d1f0"),
                ResourceType = ResourceType.AppService,
                Name = "Granting App Service access to Storage Account",
                Description = "In preparation of the next set of challenges, you'll need to grant your App Services to the other resources you've provisioned.",
                Statement = "Grant your App Service's managed identity the 'Storage Blob Data Contributor' role on your Storage Account.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceAssignedToStorageAccount(state.SubscriptionId, state.ResourceGroup, state.AppService, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Excellent! If you go back to your website it should show that it can connect successfully now.";
                    }
                    else
                        c.Error = "Storage Account is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue() && s.StorageAccount.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("09c5cc76-3c48-4eab-adf2-adf99b446814"),
                ResourceType = ResourceType.AppService,
                Name = "Granting App Service access to Key Vault",
                Description = "In preparation of the next set of challenges, you'll need to grant your App Services to the other resources you've provisioned.",
                Statement = "Grant your App Service's managed identity Secret 'Get' and 'List' permissions to your Key Vault.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceAssignedToKeyVault(state.SubscriptionId, state.ResourceGroup, state.AppService, state.KeyVault))
                    {
                        c.Completed = true;
                        c.Success = "Excellent! If you go back to your website it should show that it can connect successfully now.";
                    }
                    else
                        c.Error = "Key Vault is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue() && s.KeyVault.HasValue()
            },
            new ChallengeDefinition
            {
                Id = Guid.Parse("35e98698-9059-4fbf-b2cf-c37ff23c2d9b"),
                ResourceType = ResourceType.AppService,
                Name = "Granting App Service access to SQL Server",
                Description = "In preparation of the next set of challenges, you'll need to grant your App Services to the other resources you've provisioned.",
                Statement = "Set your App Service's managed identity as the Active Directory Admin on your SQL Server.",
                Hint = "Strictly speaking we shouldn't be assigning the App Service as an Admin on the server, its too-much access to all databases on the server, but for now it's OK.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await _stateService.GetState();
                    if (state.AppService.HasValue() && await _azureProvider.AppServiceAssignedToSqlServer(state.SubscriptionId, state.ResourceGroup, state.AppService, state.SqlServer))
                    {
                        c.Completed = true;
                        c.Success = "Excellent! If you go back to your website it should show that it can connect successfully now.";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue() && s.SqlServer.HasValue()
            },

            // Could provide basic code for a website that will check it can connect to key vault/storage/database server, showing how to build/deploy it and setting the urls
            // Once they've deployed it it will show it can't connect, have them wire up their website's identity to access the resources (list permisions on keyvault, admin on db server just for simplicity, blob contributor)
            // Refreshing the page should show it can now connect
            // Next step is to configure them in vnets and service endpoints, confirm via the website that it can still connect
            // Then making all the services non-public, setting up private links, updating urls to those services likely, and confirm it can still connect
            // Might be a 'try this, but dont have much instructions for you at the moment'
        };
    }

    public async Task CheckChallenge(Challenge challenge)
    {
        try
        {
            await challenge.ChallengeDefinition.ValidateFunc(challenge);
            if (challenge.Completed && !string.IsNullOrWhiteSpace(challenge.Success))
            {
                await _stateService.ChallengeCompleted(challenge);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception occurred");
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
    public string Statement { get; set; }
    public string Hint { get; init; }
    public Func<Challenge, Task> ValidateFunc { get; init; }
    public ChallengeType ChallengeType { get; init; }
    public string[] QuizOptions { get; init; }
    public Func<State, bool>? CanShowChallenge { get; set; }
    public string Link { get; set; }
}

public class Challenge
{
    public ChallengeDefinition ChallengeDefinition { get; set; }
    public bool Checking { get; set; }
    public bool Completed { get; set; }
    public string Input { get; set; }
    public string Error { get; set; }
    public string Success { get; set; }
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
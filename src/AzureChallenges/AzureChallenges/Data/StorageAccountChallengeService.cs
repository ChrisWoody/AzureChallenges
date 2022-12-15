namespace AzureChallenges.Data;

public class StorageAccountChallengeService : ChallengeServiceBase
{
    public StorageAccountChallengeService(
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
                Id = Guid.Parse("15202bbe-94ad-4ebf-aa15-ed93b5cef11e"),
                ResourceType = ResourceType.StorageAccount,
                Name = "Create",
                Description = "Storage Accounts are a cost-effective (mostly) way of storing lots of files (called blobs), but also support other services like Files, Queues and (nosql) Tables.",
                Statement = "Create a Storage Account in your Resource Group. What is the name of the Storage Account",
                Hint = "Try to keep your resources in the same Location as the Resource Group, and don't worry too much about the various options when creating a Storage Account, we'll configure them in the next set of challenges.",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (!string.IsNullOrWhiteSpace(c.Input) && await AzureProvider.StorageAccountExists(state.SubscriptionId, state.ResourceGroup, c.Input))
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
                Description = "In general, all requests to a service should be over HTTPS, especially if we're dealing with sensitive data which we may be storing on the Storage Account.",
                Statement = "Enable 'Secure transfer required' on the Storage Account.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (!string.IsNullOrWhiteSpace(state.StorageAccount) && await AzureProvider.StorageAccountHttpsTrafficOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
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
                Description = "While we are only allowing HTTPS connections, we should also be using TLS1.2 at a minimum. We can enforce this on the Storage Account.",
                Statement = "Make sure the minimum TLS version is configured as '1.2' on the Storage Account",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.StorageAccount.HasValue() && await AzureProvider.StorageAccountTls12Configured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Depending on how you provisioned the Storage Account, you may have noticed you couldn't create it unless you specified TLS 1.2. " +
                                    "Our Azure Policy prevents most services from being provisioned or updated unless TLS 1.2 is set.";
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
                Description = "Rarely would we have files publicly accessible, specifically 'anonymously' accessible. " +
                              "Even though we can mark our containers as 'private', it doesn't stop someone from changing it or creating a public container or file.",
                Statement = "Disable 'Allow Public blob access' on the Storage Account",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.StorageAccount.HasValue() && await AzureProvider.StorageAccountPublicBlobAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
                    {
                        c.Completed = true;
                        c.Success = "Success! If we need files to be publicly accessible, we should consider the use case and sensitivity of the files.";
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
                Description = "Even if we don't allow blobs to be publicly (i.e. anonymously) accessible, its still possible to connect to a Storage Account from anywhere in the world by default. " +
                              "There is a flag that allows you to disable any public network access, which means only connections can be made from within a virtual network.",
                Statement = "We'll leave this flag disabled for now and come back to it later. Is that OK with you?",
                ChallengeType = ChallengeType.Quiz,
                QuizOptions = new []
                {
                    "Yes", "No", "Nah yeah"
                },
                ValidateFunc = async c =>
                {
                    if (c.Input == "Yes" || c.Input == "Nah yeah")
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
                Name = "Shared Account Key access",
                Description = "By default Storage Accounts use a Shared Access Key To avoid having the full connection string of a storage account, we can configure AD-only auth to it",
                Statement = "Disable 'Allow storage account key access' on the Storage Account'",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.StorageAccount.HasValue() && await AzureProvider.StorageAccountSharedKeyAccessDisabled(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
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
                Name = "Challenge prep",
                Description = "In preparation for future challenges, create another storage account that we'll use to store logs. " +
                              "Unlike the Storage Account you've just configured for this new one make sure that 'Shared Access Key' and 'Public Network Access' are both allowed/enabled, " +
                              "and that it's located in the same region as the original Storage Account.",
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
                Description = "Several Azure resources support 'Diagnostic Settings' which allow you to log operations against the resource, with this you can audit all the operations against a resource, " +
                              "including who accessed it and when. Also by disabling the Shared Access Key (which means it requires AD Auth), this can be correlated to an identity.",
                Statement = "Configure the Diagnostic Settings for 'blob' on your original Storage Account with 'StorageRead', 'StorageWrite' and 'StorageDelete' enabled, pointing to your new 'log' storage account",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.StorageAccount.HasValue() && await AzureProvider.StorageAccountBlobDiagnosticSettingsConfigured(state.SubscriptionId, state.ResourceGroup, state.StorageAccount))
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
        };
    }
}
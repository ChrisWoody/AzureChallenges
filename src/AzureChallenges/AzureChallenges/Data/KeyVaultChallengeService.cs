namespace AzureChallenges.Data;

public class KeyVaultChallengeService : ChallengeServiceBase
{
    public KeyVaultChallengeService(
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
                Id = Guid.Parse("59159eab-8a58-484a-880a-fc787a00cdfc"),
                ResourceType = ResourceType.KeyVault,
                Name = "Create",
                Description = "Key Vaults are cool for storing secrets and certificates",
                Statement = "Create a Key Vault. What is its name?",
                Hint = "For the purpose of these challenges, make sure it's created with the 'vault access policy' (which is the default).",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (c.Input.HasValue() && await AzureProvider.KeyVaultExists(state.SubscriptionId, state.ResourceGroup, c.Input))
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
                    var state = await StateService.GetState();
                    if (state.KeyVault.HasValue() && await AzureProvider.KeyVaultDiagnosticSettingsConfigured(state.SubscriptionId, state.ResourceGroup, state.KeyVault))
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
                Statement = $"Assign the '{Configuration["WebsiteServicePrincipalName"]}' user to your Key Vault only with Secret 'Get' and 'List' permissions",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.KeyVault.HasValue() && await AzureProvider.KeyVaultSecretAccessConfigured(state.SubscriptionId, state.ResourceGroup, state.KeyVault, Configuration["WebsiteServicePrincipalObjectId"]))
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
                    var state = await StateService.GetState();

                    if (state.KeyVault.HasValue())
                    {
                        var secretValue = await AzureProvider.GetKeyVaultSecretValue(state.KeyVault, "super-secret");
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
        };
    }
}
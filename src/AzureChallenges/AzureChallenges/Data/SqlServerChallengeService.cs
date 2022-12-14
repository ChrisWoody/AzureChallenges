namespace AzureChallenges.Data;

public class SqlServerChallengeService : ChallengeServiceBase
{
    public SqlServerChallengeService(
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
                Id = Guid.Parse("60730b90-d133-43be-9e5a-1c181a24f921"),
                ResourceType = ResourceType.SqlServer,
                Name = "Create",
                Description = "SQL Server stores pools and databases",
                Hint = "NOTE you won't be able to create a SQL Server via the Azure Portal because of our Azure Policy and that functionality is missing, use the command line OR i can provision for you",
                // TODO consider providing more direction around the auth, likely support sql admin and ad auth and they can choose which one to connect with later on
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (c.Input.HasValue() && await AzureProvider.SqlServerExists(state.SubscriptionId, state.ResourceGroup, c.Input))
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
                    var state = await StateService.GetState();
                    if (state.SqlServer.HasValue() && await AzureProvider.SqlServerTls12Configured(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
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
                    var state = await StateService.GetState();
                    if (state.SqlServer.HasValue() && await AzureProvider.SqlServerAuditingEnabled(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
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
                    var state = await StateService.GetState();
                    if (state.SqlServer.HasValue() && await AzureProvider.SqlServerAnyIpRestriction(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
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
                    var state = await StateService.GetState();
                    if (state.SqlServer.HasValue() && await AzureProvider.SqlServerAllowAzureResourcesException(state.SubscriptionId, state.ResourceGroup, state.SqlServer))
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
        };
    }
}
﻿namespace AzureChallenges.Data;

public class AppServiceChallengeService : ChallengeServiceBase
{
    public AppServiceChallengeService(
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
                Id = Guid.Parse("cc194d7d-4866-46f9-b8f7-a193bd7f3810"),
                ResourceType = ResourceType.AppService,
                Name = "Create",
                Description = "App Services allow us to host websites and run background jobs, along with scaling out automatically to handle incoming load. A variety of frameworks could be used including .net, node, python and php.",
                Statement = "Create an App Service on Windows, at the Basic tier with .NET 7 (without Application Insights) in your Resource Group. What is the name of the App Service?",
                ChallengeType = ChallengeType.ExistsWithInput,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (c.Input.HasValue() && await AzureProvider.AppServiceExists(state.SubscriptionId, state.ResourceGroup, c.Input))
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
                Description = "Regardless if we're using the App Service as a website or a webjob runner, we should always be using HTTPS when we make requests to the website.",
                Statement = "Make sure the 'HTTPS Only' flag is enabled",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceHttpsOnlyConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                Description = "Not security related, but if we're paying for the App Service regardless if its actively used or not, we should have 'Always On' enabled, " +
                              "this improves cold start time for accessing the website and also for deployments.",
                Statement = "Make sure the 'Always On' flag is enabled",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceAlwaysOnConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                Description = "No brainer here, always use at least TLS 1.2",
                Statement = "Make sure TLS is configured at 1.2 on the App Service",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceTls12Configured(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                Description = "We never use FTP to deploy to an App Service, so we should disable it, or if we do at least only allow FTPS (their lingo, basically SFTP).",
                Statement = "Make sure 'FTP State' is set to 'Disabled'",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceFtpDisabled(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                Description = "We don't want to have to manage credentials to services like Storage Accounts or Key Vaults, " +
                              "so configure the App Service to have a System assigned identity. A service principal will be registered for us with the same name as the App Service.",
                Statement = "Enable the 'System assigned' Identity on the App Service",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceSystemIdentityAssigned(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceIpAccessRestriction(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                Description = "Though App Services support Diagnostic logs (setup the same way as the Storage Account and Key Vault), for this challenge we'll look at the 'App Service logs'. " +
                              "This allows us to store logs on the App Service's local storage or export to a Storage Account. With local storage configured you can use log streaming too.",
                Statement = "Configure 'Application logging (Blob)' and 'Web server logging' to your 'log' Storage Account. You can put them in 'logs' and 'logs-iis' containers respectively if you want.",
                ChallengeType = ChallengeType.CheckConfigured,
                ValidateFunc = async c =>
                {
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceLogsConfigured(state.SubscriptionId, state.ResourceGroup, state.AppService))
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
                Description = "Navigate to your website a few times and have a look at the IIS logs. These will show when HTTP request is made and detail the path, response status code and more.",
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
                Statement = $"Add the following Application settings to your App Service: StorageAccountName = {{your Storage Account name}}, KeyVaultName = {{your Key Vault name}}, SqlServerName = {{your SQL Server name}} and TenantId = {Configuration["TenantId"]}. Have you added those settings?",
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
                Description = "There are various ways to deploy to an App Service, but here we'll use the Kudu portal and have it nicely deploy our .zip for us.",
                Statement = "Right-click and save the zip linked below, navigate to 'https://<website name here>.scm.azurewebsites.net/ZipDeployUI' and drag the zip onto the page. " +
                            "After that's completed navigate to your website 'https://<website name here>.azurewebsites.net'. You might see some errors but that's OK! Are you ready to continue?",
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
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceAssignedToStorageAccount(state.SubscriptionId, state.ResourceGroup, state.AppService, state.StorageAccount))
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
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceAssignedToKeyVault(state.SubscriptionId, state.ResourceGroup, state.AppService, state.KeyVault))
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
                    var state = await StateService.GetState();
                    if (state.AppService.HasValue() && await AzureProvider.AppServiceAssignedToSqlServer(state.SubscriptionId, state.ResourceGroup, state.AppService, state.SqlServer))
                    {   
                        c.Completed = true;
                        c.Success = "Excellent! If you go back to your website it should show that it can connect successfully now.";
                    }
                    else
                        c.Error = "SQL Server is not configured correctly";
                },
                CanShowChallenge = s => s.SubscriptionId.HasValue() && s.ResourceGroup.HasValue() && s.AppService.HasValue() && s.SqlServer.HasValue()
            },
        };
    }
}
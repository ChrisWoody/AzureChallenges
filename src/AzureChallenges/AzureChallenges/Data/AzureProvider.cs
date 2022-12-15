using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureChallenges.Data;

public class AzureProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureProvider> _logger;
    private readonly TokenCredential _credential;
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public AzureProvider(IConfiguration configuration, ILogger<AzureProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _credential = string.IsNullOrWhiteSpace(_configuration["ClientId"]) || string.IsNullOrWhiteSpace(_configuration["ClientSecret"])
            ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = _configuration["TenantId"] })
            : new ClientSecretCredential(_configuration["TenantId"], _configuration["ClientId"], _configuration["ClientSecret"]);

        _httpClient.BaseAddress = new Uri("https://management.azure.com");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // https://learn.microsoft.com/en-au/rest/api/resources/subscriptions/get
    public async Task<bool> SubscriptionExists(string subscriptionId)
    {
        var response = await Get($"/subscriptions/{subscriptionId}?api-version=2020-01-01");
        return response.IsSuccessStatusCode;
    }

    // https://learn.microsoft.com/en-au/rest/api/resources/resource-groups/get
    public async Task<bool> ResourceGroupExists(string subscriptionId, string resourceGroupName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}?api-version=2021-04-01");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StorageAccountExists(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        try
        {
            await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Storage Account");
            return false;
        }
    }

    public async Task<bool> StorageAccountTls12Configured(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var storageAccount = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);

        return storageAccount.Properties.MinimumTlsVersion == "TLS1_2";
    }

    public async Task<bool> StorageAccountPublicNetworkAccessDisabled(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var storageAccount = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);

        return storageAccount.Properties.PublicNetworkAccess == "Disabled";
    }

    public async Task<bool> StorageAccountPublicBlobAccessDisabled(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var storageAccount = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);

        return storageAccount.Properties.AllowBlobPublicAccess == false;
    }

    public async Task<bool> StorageAccountSharedKeyAccessDisabled(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var storageAccount = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);

        return storageAccount.Properties.AllowSharedKeyAccess == false;
    }

    public async Task<bool> StorageAccountHttpsTrafficOnlyConfigured(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var storageAccount = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);

        return storageAccount.Properties.SupportsHttpsTrafficOnly;
    }

    // https://learn.microsoft.com/en-au/rest/api/storagerp/storage-accounts/get-properties
    private async Task<StorageAccount> GetStorageAccount(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}?api-version=2022-05-01");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StorageAccount>(_jsonSerializerOptions);
    }

    public async Task<bool> StorageAccountBlobDiagnosticSettingsConfigured(string subscriptionId, string resourceGroupName, string storageAccountName)
    {
        var resourceUrl = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}/blobServices/default";
        var diagnosticSettings = await GetDiagnosticSettings(resourceUrl);

        if (!diagnosticSettings.Any())
        {
            return false;
        }

        return diagnosticSettings.Any(d =>
            !string.IsNullOrWhiteSpace(d.Properties.StorageAccountId) && (d.Properties.Logs.All(l =>
                l.Enabled && l.Category is "StorageRead" or "StorageWrite" or "StorageDelete")));
    }

    // https://learn.microsoft.com/en-us/rest/api/monitor/diagnostic-settings/list?tabs=HTTP
    private async Task<DiagnosticSettings[]> GetDiagnosticSettings(string resourceUri)
    {
        var response = await Get($"{resourceUri}/providers/Microsoft.Insights/diagnosticSettings?api-version=2021-05-01-preview");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var readFromJsonAsync = await response.Content.ReadFromJsonAsync<DiagnosticSettingsResponse>(_jsonSerializerOptions);
        return readFromJsonAsync.Value;
    }

    public async Task<bool> KeyVaultExists(string subscriptionId, string resourceGroupName, string keyVaultName)
    {
        try
        {
            await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Key Vault");
            return false;
        }
    }

    public async Task<bool> KeyVaultSecretAccessConfigured(string subscriptionId, string resourceGroupName, string keyVaultName, string websiteServicePrincipalId)
    {
        var keyVault = await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName);
        return keyVault.Properties.AccessPolicies.Any(x => x.ObjectId == websiteServicePrincipalId && x.Permissions.Secrets.All(e => e is "get" or "list"));
    }

    // https://learn.microsoft.com/en-au/rest/api/keyvault/secrets/get-secret/get-secret?tabs=HTTP
    public async Task<string> GetKeyVaultSecretValue(string keyVaultName, string secretName)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { "https://vault.azure.net/.default" }), CancellationToken.None);
        var keyVaultClient = new HttpClient();
        keyVaultClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        keyVaultClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await keyVaultClient.GetAsync($"https://{keyVaultName}.vault.azure.net/secrets/{secretName}?api-version=7.3");
        response.EnsureSuccessStatusCode();
        var keyVaultSecret = await response.Content.ReadFromJsonAsync<KeyVaultSecret>(_jsonSerializerOptions);

        return keyVaultSecret.Value;
    }

    // https://learn.microsoft.com/en-au/rest/api/keyvault/keyvault/vaults/get?tabs=HTTP
    private async Task<KeyVault> GetKeyVault(string subscriptionId, string resourceGroupName, string keyVaultName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}?api-version=2022-07-01");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KeyVault>(_jsonSerializerOptions);
    }

    public async Task<bool> KeyVaultDiagnosticSettingsConfigured(string subscriptionId, string resourceGroupName, string keyVaultName)
    {
        var resourceUrl = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}";
        var diagnosticSettings = await GetDiagnosticSettings(resourceUrl);

        if (!diagnosticSettings.Any())
        {
            return false;
        }

        return diagnosticSettings.Any(d =>
            !string.IsNullOrWhiteSpace(d.Properties.StorageAccountId) && (d.Properties.Logs.All(l =>
                l.Enabled && l.CategoryGroup is "audit" or "allLogs")));
    }

    public async Task<bool> SqlServerExists(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        try
        {
            await GetSqlServer(subscriptionId, resourceGroupName, sqlServerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting SQL Server");
            return false;
        }
    }

    public async Task<bool> SqlServerTls12Configured(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        var sqlServer = await GetSqlServer(subscriptionId, resourceGroupName, sqlServerName);
        return sqlServer.Properties.MinimalTlsVersion == "1.2";
    }

    // https://learn.microsoft.com/en-au/rest/api/sql/2021-02-01-preview/servers/get?tabs=HTTP
    private async Task<SqlServer> GetSqlServer(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{sqlServerName}?api-version=2021-02-01-preview");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SqlServer>(_jsonSerializerOptions);
    }

    // https://learn.microsoft.com/en-us/rest/api/sql/2021-02-01-preview/server-blob-auditing-policies/get?tabs=HTTP
    public async Task<bool> SqlServerAuditingEnabled(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{sqlServerName}/auditingSettings/default?api-version=2021-02-01-preview");
        response.EnsureSuccessStatusCode();
        var auditing = await response.Content.ReadFromJsonAsync<SqlServerAuditing>(_jsonSerializerOptions);
        return auditing.Properties.State.Equals("enabled", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(auditing.Properties.StorageEndpoint);
    }

    // https://learn.microsoft.com/en-us/rest/api/sql/2022-05-01-preview/firewall-rules/list-by-server?tabs=HTTP
    public async Task<bool> SqlServerAnyIpRestriction(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{sqlServerName}/firewallRules?api-version=2022-05-01-preview");
        response.EnsureSuccessStatusCode();
        var auditing = await response.Content.ReadFromJsonAsync<SqlServerIpRestrictions>(_jsonSerializerOptions);
        return auditing.Value.Any(x => !x.Name.Equals("AllowAllWindowsAzureIps", StringComparison.OrdinalIgnoreCase));
    }

    // https://learn.microsoft.com/en-us/rest/api/sql/2022-05-01-preview/firewall-rules/list-by-server?tabs=HTTP
    public async Task<bool> SqlServerAllowAzureResourcesException(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{sqlServerName}/firewallRules?api-version=2022-05-01-preview");
        response.EnsureSuccessStatusCode();
        var auditing = await response.Content.ReadFromJsonAsync<SqlServerIpRestrictions>(_jsonSerializerOptions);
        return auditing.Value.Any(x => x.Name.Equals("AllowAllWindowsAzureIps", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> SqlServerDisallowAzureResourcesException(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        return !await SqlServerAllowAzureResourcesException(subscriptionId, resourceGroupName, sqlServerName);
    }

    public async Task<bool> AppServiceExists(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        try
        {
            await GetAppService(subscriptionId, resourceGroupName, appServiceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting App Service");
            return false;
        }
    }

    public async Task<bool> AppServiceHttpsOnlyConfigured(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.Properties.HttpsOnly;
    }

    public async Task<bool> AppServiceSystemIdentityAssigned(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.Identity?.Type == "SystemAssigned";
    }

    public async Task<bool> AppServiceAlwaysOnConfigured(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.SiteConfigProperties.AlwaysOn;
    }

    public async Task<bool> AppServiceTls12Configured(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.SiteConfigProperties.MinTlsVersion == "1.2";
    }

    public async Task<bool> AppServiceFtpDisabled(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.SiteConfigProperties.FtpsState == "Disabled";
    }

    public async Task<bool> AppServiceIpAccessRestriction(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.SiteConfigProperties.IpSecurityRestrictions?.Any(x => !string.IsNullOrWhiteSpace(x.IpAddress) && x.IpAddress != "Any") ?? false;
    }

    public async Task<bool> AppServiceLogsConfigured(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{appServiceName}/config/logs?api-version=2022-03-01");
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<AppServiceLogs>(_jsonSerializerOptions);
        return !string.IsNullOrWhiteSpace(logs.Properties.ApplicationLogs.AzureBlobStorage.SasUrl) &&
               !string.IsNullOrWhiteSpace(logs.Properties.HttpLogs.AzureBlobStorage.SasUrl);
    }

    public async Task<bool> AppServiceAssignedToStorageAccount(string subscriptionId, string resourceGroupName, string appServiceName, string storageAccountName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);

        var rbacs = await GetRbacs($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}");
        foreach (var rbac in rbacs.Where(x => x.Properties.PrincipalId == appService.Identity.PrincipalId))
        {
            var rbacDefinition = await GetRbacDefinition(rbac.Properties.RoleDefinitionId);
            if (rbacDefinition.Properties.RoleName == "Storage Blob Data Contributor")
                return true;
        }

        return false;
    }

    public async Task<bool> AppServiceAssignedToKeyVault(string subscriptionId, string resourceGroupName, string appServiceName, string keyVaultName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        var keyVault = await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName);

        return keyVault.Properties.AccessPolicies.Any(x => x.ObjectId == appService.Identity.PrincipalId && x.Permissions.Secrets.All(e => e is "get" or "list"));
    }

    public async Task<bool> AppServiceAssignedToSqlServer(string subscriptionId, string resourceGroupName, string appServiceName, string sqlServerName)
    {
        var sqlServer = await GetSqlServer(subscriptionId, resourceGroupName, sqlServerName);
        return sqlServer.Properties.Administrators?.Login == appServiceName;
    }

    // https://learn.microsoft.com/en-us/rest/api/authorization/role-assignments/list-for-resource?tabs=HTTP
    private async Task<Rbac[]> GetRbacs(string resourceUrl)
    {
        var response = await Get(resourceUrl + "/providers/Microsoft.Authorization/roleAssignments?api-version=2022-04-01");
        response.EnsureSuccessStatusCode();
        var rbacResponse = await response.Content.ReadFromJsonAsync<RbacResponse>(_jsonSerializerOptions);
        return rbacResponse.Value;
    }

    // https://learn.microsoft.com/en-us/rest/api/authorization/role-definitions/get?tabs=HTTP
    private async Task<RbacDefinition> GetRbacDefinition(string rbacDefinitionUrl)
    {
        var response = await Get(rbacDefinitionUrl + "?api-version=2022-04-01");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RbacDefinition>(_jsonSerializerOptions);
    }

    // https://learn.microsoft.com/en-au/rest/api/appservice/web-apps/get
    // https://learn.microsoft.com/en-au/rest/api/appservice/web-apps/get-configuration
    private async Task<AppService> GetAppService(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{appServiceName}?api-version=2022-03-01");
        response.EnsureSuccessStatusCode();
        var appService = await response.Content.ReadFromJsonAsync<AppService>(_jsonSerializerOptions);

        response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{appServiceName}/config/web?api-version=2022-03-01");
        response.EnsureSuccessStatusCode();
        var siteConfig = await response.Content.ReadFromJsonAsync<AppServiceSiteConfig>(_jsonSerializerOptions);

        appService.SiteConfigProperties = siteConfig.Properties;

        return appService;
    }

    public async Task<bool> VirtualNetworkExistsAndIsConfigured(string subscriptionId, string resourceGroupName, string virtualNetworkName)
    {
        try
        {
            var virtualNetwork = await GetVirtualNetwork(subscriptionId, resourceGroupName, virtualNetworkName);
            return virtualNetwork.Properties.AddressSpace.AddressPrefixes.Any(x => x == "10.0.0.0/16") &&
                   virtualNetwork.Properties.Subnets.Any(x => x is { Name: "default", Properties.AddressPrefix: "10.0.0.0/24" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Virtual Network");
            return false;
        }
    }

    public async Task<bool> VirtualNetworkSubnetHasServiceEndpointsConfigured(string subscriptionId, string resourceGroupName, string virtualNetworkName)
    {
        try
        {
            var virtualNetwork = await GetVirtualNetwork(subscriptionId, resourceGroupName, virtualNetworkName);
            return virtualNetwork.Properties.Subnets.Any(x =>
                x is { Name: "default", Properties.ServiceEndpoints.Length: >= 3 }
                && x.Properties.ServiceEndpoints.All(e => e.Service is "Microsoft.KeyVault" or "Microsoft.Sql" or "Microsoft.Storage"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Virtual Network");
            return false;
        }
    }

    public async Task<bool> StorageAccountIsConnectedToVirtualNetwork(string subscriptionId, string resourceGroupName, string virtualNetworkName, string storageAccountName)
    {
        var storageAccount = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName);
        return storageAccount.Properties.NetworkAcls?.VirtualNetworkRules.Any(x => x.Id.EndsWith($"{virtualNetworkName}/subnets/default")) ?? false;
    }

    public async Task<bool> KeyVaultIsConnectedToVirtualNetwork(string subscriptionId, string resourceGroupName, string virtualNetworkName, string keyVaultName)
    {
        var keyVault = await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName);
        return keyVault.Properties.NetworkAcls?.VirtualNetworkRules.Any(x => x.Id.EndsWith($"{virtualNetworkName}/subnets/default")) ?? false;
    }

    // https://learn.microsoft.com/en-au/rest/api/sql/2022-05-01-preview/virtual-network-rules/list-by-server?tabs=HTTP
    public async Task<bool> SqlServerIsConnectedToVirtualNetwork(string subscriptionId, string resourceGroupName, string virtualNetworkName, string sqlServerName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{sqlServerName}/virtualNetworkRules?api-version=2022-05-01-preview");
        response.EnsureSuccessStatusCode();
        var sqlServerVirtualNetworkRules = await response.Content.ReadFromJsonAsync<SqlServerVirtualNetworkRuleResponse>(_jsonSerializerOptions);
        return sqlServerVirtualNetworkRules.Value.Any(x => x.Properties.VirtualNetworkSubnetId.EndsWith($"{virtualNetworkName}/subnets/default"));
    }

    public async Task<bool> AppServiceIsConnectedToVirtualNetwork(string subscriptionId, string resourceGroupName, string virtualNetworkName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.Properties.VirtualNetworkSubnetId?.EndsWith($"{virtualNetworkName}/subnets/default") ?? false;
    }

    // https://learn.microsoft.com/en-au/rest/api/virtualnetwork/virtual-networks/get?tabs=HTTP
    private async Task<VirtualNetwork> GetVirtualNetwork(string subscriptionId, string resourceGroupName, string virtualNetwork)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetwork}?api-version=2022-07-01");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VirtualNetwork>(_jsonSerializerOptions);
    }

    private async Task<HttpResponseMessage> Get(string path)
    {
        await PrepareHttpClient();
        return await _httpClient.GetAsync(path);
    }

    private async Task PrepareHttpClient()
    {
        if (_tokenExpiry == DateTimeOffset.MinValue || DateTimeOffset.UtcNow.AddMinutes(5) >= _tokenExpiry)
        {
            var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.core.windows.net/.default" }), CancellationToken.None);
            _tokenExpiry = token.ExpiresOn;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }
    }

    public class Settings
    {
        public string TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }

    private class StorageAccount
    {
        public StorageAccountProperties Properties { get; set; }
    }

    private class StorageAccountProperties
    {
        public string? PublicNetworkAccess { get; set; }
        public string? MinimumTlsVersion { get; set; }
        public bool AllowBlobPublicAccess { get; set; }
        public bool AllowSharedKeyAccess { get; set; }
        public bool SupportsHttpsTrafficOnly { get; set; }
        public StorageAccountPropertiesNetworkAcl NetworkAcls { get; set; }
    }

    private class StorageAccountPropertiesNetworkAcl
    {
        public StorageAccountPropertiesNetworkAclVirtualNetworkRule[] VirtualNetworkRules { get; set; }
    }

    private class StorageAccountPropertiesNetworkAclVirtualNetworkRule
    {
        public string Id { get; set; }
    }

    private class KeyVault
    {
        public KeyVaultProperties Properties { get; set; }
    }

    private class KeyVaultProperties
    {
        public KeyVaultAccessPolicy[] AccessPolicies { get; set; }
        public string PublicNetworkAccess { get; set; }
        public KeyVaultNetworkAcls NetworkAcls { get; set; }
    }

    private class KeyVaultAccessPolicy
    {
        public string ObjectId { get; set; }
        public KeyVaultAccessPolicyPermissions Permissions { get; set; }
    }

    private class KeyVaultAccessPolicyPermissions
    {
        public string[] Keys { get; set; }
        public string[] Secrets { get; set; }
        public string[] Certificates { get; set; }
    }

    private class KeyVaultSecret
    {
        public string Value { get; set; }
    }

    private class KeyVaultNetworkAcls
    {
        public KeyVaultNetworkAclsVirtualNetworkRules[] VirtualNetworkRules { get; set; }
    }

    private class KeyVaultNetworkAclsVirtualNetworkRules
    {
        public string Id { get; set; }
    }

    private class SqlServer
    {
        public SqlServerProperties Properties { get; set; }
    }

    private class SqlServerProperties
    {
        public string MinimalTlsVersion { get; set; }
        public SqlServerPropertiesAdministrator? Administrators { get; set; }
    }

    private class SqlServerPropertiesAdministrator
    {
        public string Login { get; set; }
    }

    private class SqlServerAuditing
    {
        public SqlServerAuditingProperties Properties { get; set; }
    }

    private class SqlServerAuditingProperties
    {
        public string State { get; set; }
        public string StorageEndpoint { get; set; }
    }

    private class SqlServerIpRestrictions
    {
        public SqlServerIpRestriction[] Value { get; set; }
    }

    private class SqlServerIpRestriction
    {
        public string Name { get; set; }
    }

    private class SqlServerVirtualNetworkRuleResponse
    {
        public SqlServerVirtualNetworkRule[] Value { get; set; }
    }

    private class SqlServerVirtualNetworkRule
    {
        public SqlServerVirtualNetworkRuleProperties Properties { get; set; }
    }

    private class SqlServerVirtualNetworkRuleProperties
    {
        public string VirtualNetworkSubnetId { get; set; }
    }

    private class AppService
    {
        public AppServiceProperties Properties { get; set; }
        public AppServiceIdentity Identity { get; set; }

        [JsonIgnore]
        public AppServiceSiteConfigProperties SiteConfigProperties { get; set; }
    }

    private class AppServiceProperties
    {
        public bool HttpsOnly { get; set; }
        public string VirtualNetworkSubnetId { get; set; }
    }

    private class AppServiceIdentity
    {
        public string PrincipalId { get; set; }
        public string Type { get; set; }
    }

    private class AppServiceSiteConfig
    {
        public AppServiceSiteConfigProperties Properties { get; set; }
    }

    private class AppServiceSiteConfigProperties
    {
        public bool AlwaysOn { get; set; }
        public string MinTlsVersion { get; set; }
        public string FtpsState { get; set; }
        public AppServiceSiteConfigIpSecurityRestriction[] IpSecurityRestrictions { get; set; }
    }

    private class AppServiceSiteConfigIpSecurityRestriction
    {
        public string IpAddress { get; set; }
    }

    private class AppServiceLogs
    {
        public AppServiceLogsProperties Properties { get; set; }
    }

    private class AppServiceLogsProperties
    {
        public AppServiceLogsPropertiesLogs ApplicationLogs { get; set; }
        public AppServiceLogsPropertiesLogs HttpLogs { get; set; }
    }

    private class AppServiceLogsPropertiesLogs
    {
        public AppServiceLogsPropertiesLogsBlob AzureBlobStorage { get; set; }
    }

    private class AppServiceLogsPropertiesLogsBlob
    {
        public string SasUrl { get; set; }
    }

    private class DiagnosticSettingsResponse
    {
        public DiagnosticSettings[] Value { get; set; }
    }

    private class DiagnosticSettings
    {
        public DiagnosticSettingsProperties Properties { get; set; }
    }

    private class DiagnosticSettingsProperties
    {
        public string StorageAccountId { get; set; }
        public DiagnosticSetting[] Logs { get; set; }
    }

    private class DiagnosticSetting
    {
        public string Category { get; set; }
        public string CategoryGroup { get; set; }
        public bool Enabled { get; set; }
    }

    private class RbacResponse
    {
        public Rbac[] Value { get; set; }
    }

    private class Rbac
    {
        public RbacProperties Properties { get; set; }
    }

    private class RbacProperties
    {
        public string RoleDefinitionId { get; set; }
        public string PrincipalId { get; set; }
    }

    private class RbacDefinition
    {
        public RbacDefinitionProperties Properties { get; set; }
    }

    private class RbacDefinitionProperties
    {
        public string RoleName { get; set; }
    }

    private class VirtualNetwork
    {
        public VirtualNetworkProperties Properties { get; set; }
    }

    private class VirtualNetworkProperties
    {
        public VirtualNetworkAddressSpace AddressSpace { get; set; }
        public VirtualNetworkSubnet[] Subnets { get; set; }
    }

    private class VirtualNetworkAddressSpace
    {
        public string[] AddressPrefixes { get; set; }
    }

    private class VirtualNetworkSubnet
    {
        public string Name { get; set; }
        public VirtualNetworkSubnetProperties Properties { get; set; }
    }

    private class VirtualNetworkSubnetProperties
    {
        public string AddressPrefix { get; set; }
        public VirtualNetworkServiceEndpoint[] ServiceEndpoints { get; set; }
    }

    private class VirtualNetworkServiceEndpoint
    {
        public string Service { get; set; }
    }
}
using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureChallenges.Data;

public class AzureProvider
{
    private readonly TokenCredential _credential;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public AzureProvider(Settings settings)
    {
        _credential = string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ClientSecret)
            ? new DefaultAzureCredential(new DefaultAzureCredentialOptions {TenantId = settings.TenantId})
            : new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);

        _httpClient = new HttpClient();
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
        catch (Exception)
        {
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

    public async Task<bool> KeyVaultExists(string subscriptionId, string resourceGroupName, string keyVaultName)
    {
        try
        {
            await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> KeyVaultSecretAccessConfigured(string subscriptionId, string resourceGroupName, string keyVaultName)
    {
        var keyVault = await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName);
        return keyVault.Properties.AccessPolicies.Any(x => x.Permissions.Secrets.All(e => e is "get" or "list"));
    }

    // https://learn.microsoft.com/en-au/rest/api/keyvault/keyvault/vaults/get?tabs=HTTP
    private async Task<KeyVault> GetKeyVault(string subscriptionId, string resourceGroupName, string keyVaultName)
    {
        var response = await Get($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}?api-version=2022-07-01");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KeyVault>(_jsonSerializerOptions);
    }

    public async Task<bool> SqlServerExists(string subscriptionId, string resourceGroupName, string sqlServerName)
    {
        try
        {
            await GetSqlServer(subscriptionId, resourceGroupName, sqlServerName);
            return true;
        }
        catch (Exception)
        {
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

    public async Task<bool> AppServiceExists(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        try
        {
            await GetAppService(subscriptionId, resourceGroupName, appServiceName);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> AppServiceHttpsOnlyConfigured(string subscriptionId, string resourceGroupName, string appServiceName)
    {
        var appService = await GetAppService(subscriptionId, resourceGroupName, appServiceName);
        return appService.Properties.HttpsOnly;
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
    }

    private class KeyVault
    {
        public KeyVaultProperties Properties { get; set; }
    }

    private class KeyVaultProperties
    {
        public KeyVaultAccessPolicy[] AccessPolicies { get; set; }
        public string PublicNetworkAccess { get; set; }
    }

    private class KeyVaultAccessPolicy
    {
        public KeyVaultAccessPolicyPermissions Permissions { get; set; }
    }

    private class KeyVaultAccessPolicyPermissions
    {
        public string[] Keys { get; set; }
        public string[] Secrets { get; set; }
        public string[] Certificates { get; set; }
    }

    private class SqlServer
    {
        public SqlServerProperties Properties { get; set; }
    }

    private class SqlServerProperties
    {
        public string MinimalTlsVersion { get; set; }
    }

    private class AppService
    {
        public AppServiceProperties Properties { get; set; }

        [JsonIgnore]
        public AppServiceSiteConfigProperties SiteConfigProperties { get; set; }
    }

    private class AppServiceProperties
    {
        public bool HttpsOnly { get; set; }
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
    }
}
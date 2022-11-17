using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;

namespace AzureChallenges.Data;

public class AzureProvider
{
    private readonly TokenCredential _credential;
    private readonly HttpClient _httpClient;

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
        var response = await Get($"/subscriptions/{subscriptionId}/resourcegroups/{resourceGroupName}?api-version=2021-04-01");
        return response.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> Get(string path)
    {
        await PrepareHttpClient();
        return await _httpClient.GetAsync(path);
    }

    private async Task PrepareHttpClient()
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.core.windows.net/.default" }), CancellationToken.None);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    public class Settings
    {
        public string TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }
}
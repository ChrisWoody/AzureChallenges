using System.Data;
using System.Net.Mime;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", async () =>
{
    // Prepare the credential for future requests
    TokenCredential credential = string.IsNullOrWhiteSpace(app.Configuration["ClientId"]) || string.IsNullOrWhiteSpace(app.Configuration["ClientSecret"])
        ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = app.Configuration["TenantId"] })
        : new ClientSecretCredential(app.Configuration["TenantId"], app.Configuration["ClientId"], app.Configuration["ClientSecret"]);

    // Try connect to the storage account and list its containers
    var containers = Array.Empty<string>();
    var storageError = "";
    try
    {
        var storageClient = new BlobServiceClient(new Uri($"https://{app.Configuration["StorageAccountName"]}.blob.core.windows.net/"), credential);
        containers = storageClient.GetBlobContainers().Select(x => x.Name).ToArray();
    }
    catch (Exception e)
    {
        storageError = $"Failed to connect to Storage Account '{app.Configuration["StorageAccountName"]}': {e.Message}";
    }

    // Try connect to the key vault and list its secret names
    var keyVaultSecretNames = Array.Empty<string>();
    var keyVaultError = "";
    try
    {
        var secretClient = new SecretClient(new Uri($"https://{app.Configuration["KeyVaultName"]}.vault.azure.net/"), credential);
        keyVaultSecretNames = secretClient.GetPropertiesOfSecrets().Select(x => x.Name).ToArray();
    }
    catch (Exception e)
    {
        keyVaultError = $"Failed to connect to Key Vault '{app.Configuration["KeyVaultName"]}': {e.Message}";
    }

    // Try connect to the sql server and query the master database
    DateTime? sqlServerTime = null;
    var sqlServerError = "";
    try
    {
        await using var sqlConnection = new SqlConnection($"Data Source={app.Configuration["SqlServerName"]}.database.windows.net;Initial Catalog=master;Encrypt=true");
        var accessToken = await credential.GetTokenAsync(new TokenRequestContext(new[] {"https://database.windows.net/.default"}), CancellationToken.None);
        sqlConnection.AccessToken = accessToken.Token;
        sqlConnection.Open();
        await using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandText = "select getdate()";

        var result = sqlCommand.ExecuteScalar();
        sqlServerTime = result as DateTime?;
        if (sqlServerTime == null)
        {
            sqlServerError = $"Could connect but the result isn't expected: {result}";
        }
    }
    catch (Exception e)
    {
        sqlServerError = $"Failed to connect to Sql Server '{app.Configuration["SqlServer"]}': {e.Message}";
    }

    // Build a simple (but nice looking) page to show if we can connect to the services or not
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@4.0.0/dist/css/bootstrap.min.css\" integrity=\"sha384-Gn5384xqQ1aoWXA+058RXPxPg6fy4IWvTNh0E263XmFcJlSAwiGgFAW/dAiS6JXm\" crossorigin=\"anonymous\">");
    sb.AppendLine("<meta charset='utf-8'/>");
    sb.AppendLine("<title>Connection Checker Website</title>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body style=\"text-align: center;\">");

    sb.AppendLine($"<p>Last ran: {TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("W. Australia Standard Time"))}</p>");

    sb.AppendLine($@"<div class=""card"">
  <div class=""card-header"" style=""background-color: {(string.IsNullOrWhiteSpace(storageError) ? "lightgreen" : "orangered")}"">
    <h3>Storage Account - {(string.IsNullOrWhiteSpace(storageError) ? "Connected" : "Could not connect")}</h3>
  </div>
  <div class=""card-body"">
    <p class=""card-text"">{(string.IsNullOrWhiteSpace(storageError) ? "Containers: " + string.Join(", ", containers) : "Error: " + storageError)}</p>
  </div>
</div>");

    sb.AppendLine($@"<div class=""card"">
  <div class=""card-header"" style=""background-color: {(string.IsNullOrWhiteSpace(keyVaultError) ? "lightgreen" : "orangered")}"">
    <h3>Key Vault - {(string.IsNullOrWhiteSpace(keyVaultError) ? "Connected" : "Could not connect")}</h3>
  </div>
  <div class=""card-body"">
    <p class=""card-text"">{(string.IsNullOrWhiteSpace(keyVaultError) ? "Secret names: " + string.Join(", ", keyVaultSecretNames) : "Error: " + keyVaultError)}</p>
  </div>
</div>");

    sb.AppendLine($@"<div class=""card"">
  <div class=""card-header"" style=""background-color: {(string.IsNullOrWhiteSpace(sqlServerError) ? "lightgreen" : "orangered")}"">
    <h3>SQL Server - {(string.IsNullOrWhiteSpace(sqlServerError) ? "Connected" : "Could not connect")}</h3>
  </div>
  <div class=""card-body"">
    <p class=""card-text"">{(string.IsNullOrWhiteSpace(sqlServerError) ? "SQL Server time: " + sqlServerTime : "Error: " + sqlServerError)}</p>
  </div>
</div>");

    sb.AppendLine("</body>");
    sb.AppendLine("</html>");

    return Results.Extensions.Html(sb.ToString());
});

app.Run();

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-7.0#customizing-results
internal static class ResultsExtensions
{
    public static IResult Html(this IResultExtensions resultExtensions, string html)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);

        return new HtmlResult(html);
    }
}

internal class HtmlResult : IResult
{
    private readonly string _html;

    public HtmlResult(string html)
    {
        _html = html;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(_html);
        return httpContext.Response.WriteAsync(_html);
    }
}
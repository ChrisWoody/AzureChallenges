using System.ComponentModel;
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
    string[]? containers = null;
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
    string[]? keyVaultSecretNames = null;
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

        if (sqlCommand.ExecuteScalar() is DateTime result)
        {
            sqlServerTime = result;
        }
        else
        {
            sqlServerError = $"Could connect but the result isn't expected";
        }
    }
    catch (Exception e)
    {
        keyVaultError = $"Failed to connect to Sql Server '{app.Configuration["SqlServer"]}': {e.Message}";
    }

    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("<meta charset='utf-8'/>");
    sb.AppendLine("<title>Connection Checker Website</title>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");
    if (string.IsNullOrWhiteSpace(storageError))
    {
        sb.AppendLine($"<p style=\"\">Successfully connected to the '{app.Configuration["StorageAccountName"]}' Storage Account!</p>");
        foreach (var container in containers)
        {
            sb.AppendLine($"<p>- {container}</p>");
        }
    }
    else
    {
        sb.AppendLine($"<p style=\"\">Failed to connect to the '{app.Configuration["StorageAccountName"]}' Storage Account</p>");
        sb.AppendLine($"<p>{storageError}</p>");
    }

    if (string.IsNullOrWhiteSpace(keyVaultError))
    {
        sb.AppendLine($"<p style=\"\">Successfully connected to the '{app.Configuration["KeyVaultName"]}' Key Vault!</p>");
        foreach (var keyVaultSecretName in keyVaultSecretNames)
        {
            sb.AppendLine($"<p>- {keyVaultSecretName}</p>");
        }
    }
    else
    {
        sb.AppendLine($"<p style=\"\">Failed to connect to the '{app.Configuration["KeyVaultName"]}' Key Vault</p>");
        sb.AppendLine($"<p>{keyVaultError}</p>");
    }

    if (string.IsNullOrWhiteSpace(sqlServerError))
    {
        sb.AppendLine($"<p style=\"\">Successfully connected to the '{app.Configuration["SqlServerName"]}' SQL Server!</p>");
        sb.AppendLine($"<p>Result of query: {sqlServerTime}</p>");
    }
    else
    {
        sb.AppendLine($"<p style=\"\">Failed to connect to the '{app.Configuration["SqlServerName"]}' SQL Server</p>");
        sb.AppendLine($"<p>{sqlServerError}</p>");
    }
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
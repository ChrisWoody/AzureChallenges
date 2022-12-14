using System.Security.Claims;
using AzureChallenges.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<StateService>();
builder.Services.AddScoped<ResourceGroupChallengeService>();
builder.Services.AddScoped<StorageAccountChallengeService>();
builder.Services.AddScoped<KeyVaultChallengeService>();
builder.Services.AddScoped<SqlServerChallengeService>();
builder.Services.AddScoped<AppServiceChallengeService>();
builder.Services.AddScoped<BasicChallengeService>();
builder.Services.AddSingleton(await StateStorageService.Create(builder.Configuration["StorageAccountConnctionString"]));
builder.Services.AddSingleton<AzureProvider>();
builder.Services.AddSingleton<StateCache>();

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// TODO add security headers
// TODO add logging support

app.Use(async (context, next) =>
{
    if (!context.User.HasClaim(c => c.Type == ClaimTypes.Name))
    {
        var claimsIdentity = context.User.Identity as ClaimsIdentity;
        if (context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-NAME"))
        {
            var requestHeader = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
            claimsIdentity?.AddClaim(new Claim(ClaimTypes.Name, requestHeader));
        }
        else
        {
            claimsIdentity?.AddClaim(new Claim(ClaimTypes.Name, "unauthenticated"));
        }
    }
    await next.Invoke();
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

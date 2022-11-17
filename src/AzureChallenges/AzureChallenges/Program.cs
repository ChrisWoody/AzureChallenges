using System.Security.Claims;
using AzureChallenges.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<StateService>();
builder.Services.AddScoped<ChallengeService>();
builder.Services.AddSingleton(await StateStorageService.Create(builder.Configuration["StorageAccountConnctionString"]));
builder.Services.AddSingleton(new AzureProvider(new AzureProvider.Settings
{
    TenantId = builder.Configuration["TenantId"],
    ClientId = builder.Configuration["ClientId"],
    ClientSecret = builder.Configuration["ClientSecret"]
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// TODO add security headers

// TODO confirm that with app service authentication on that the 'name' claim should be set on login, so this would only be set for local dev
app.Use(async (context, next) =>
{
    if (!context.User.HasClaim(c => c.Type == ClaimTypes.Name))
    {
        var claimsIdentity = context.User.Identity as ClaimsIdentity;
        claimsIdentity?.AddClaim(new Claim(ClaimTypes.Name, "unauthenticated"));
    }
    await next.Invoke();
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

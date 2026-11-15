using FreeGameRadar.Web.Data;
using FreeGameRadar.Web.Options;
using FreeGameRadar.Web.Services;
using FreeGameRadar.Web.Sources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FreeGameRadarOptions>(builder.Configuration.GetSection("FreeGameRadar"));
builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpClient("FreeGameRadar", (provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FreeGameRadarOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.HttpTimeoutSeconds));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FreeGameRadar/0.2 (+https://github.com/mgiakalaras/FreeGameRadar)");
});

builder.Services.AddSingleton<OfferRepository>();
builder.Services.AddSingleton<IGameOfferSource, GamerPowerSource>();
builder.Services.AddSingleton<IGameOfferSource, CheapSharkSource>();
builder.Services.AddSingleton<OfferRefreshService>();
builder.Services.AddHostedService<OfferRefreshHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<OfferRepository>();
    await repository.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "FreeGameRadar", version = "0.2.1" }));

app.Run();

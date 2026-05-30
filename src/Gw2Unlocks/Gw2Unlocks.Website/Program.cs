using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Common;
using Gw2Unlocks.UnlockClassifier.Cache;
using Gw2Unlocks.Website;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddCacheDir()
                .AddClassifierCache()
                .AddHostedService<SiteGeneratorService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
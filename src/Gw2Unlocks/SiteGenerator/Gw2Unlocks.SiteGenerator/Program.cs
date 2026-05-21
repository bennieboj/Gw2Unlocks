using Gw2Unlocks.SiteGenerator;

using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gw2Unlocks.UnlockClassifier.Cache;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddCacheDir()
                .AddClassifierCache()
                .AddHostedService<SiteGeneratorService>();

var host = builder.Build();
await host.RunAsync();
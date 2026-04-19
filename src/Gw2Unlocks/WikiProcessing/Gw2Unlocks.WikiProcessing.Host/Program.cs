using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Common;
using Gw2Unlocks.Wiki.Cache;
using Gw2Unlocks.WikiProcessing.Cache;
using Gw2Unlocks.WikiProcessing.Implementation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddCacheWikiAsCache()
                .AddWikiGraphSource()
                .AddCacheDir()
                .AddJsonCacheWikiProcessing();

builder.Services.AddUpdater();

var host = builder.Build();
await host.RunAsync();
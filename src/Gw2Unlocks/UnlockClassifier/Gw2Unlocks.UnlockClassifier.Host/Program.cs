using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Common;
using Gw2Unlocks.UnlockClassifier.Cache;
using Gw2Unlocks.UnlockClassifier.Implementation;
using Gw2Unlocks.WikiProcessing.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddJsonCacheApiSource()
                .AddCacheDir()
                .AddJsonCacheWikiProcessingSource();

builder.Services.AddClassifier()
                .AddClassifierCache();

var host = builder.Build();
await host.RunAsync();
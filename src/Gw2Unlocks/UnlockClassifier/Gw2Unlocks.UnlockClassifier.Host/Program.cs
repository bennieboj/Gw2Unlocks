using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Common;
using Gw2Unlocks.IconSpriteSheet.Cache;
using Gw2Unlocks.UnlockClassifier.Cache;
using Gw2Unlocks.UnlockClassifier.Implementation;
using Gw2Unlocks.WikiProcessing.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddJsonCacheApiSource()
                .AddCacheDir()
                .AddJsonCacheWikiProcessingSource()
                .AddIconSpriteSheetCache();

builder.Services.AddClassifier()
                .AddClassifierCache();

var host = builder.Build();
await host.RunAsync();

// RunAsync always reports success, so the exit code is carried by Environment.ExitCode instead:
// that is what lets a regressed classification or a crash fail the pipeline step.
return Environment.ExitCode;
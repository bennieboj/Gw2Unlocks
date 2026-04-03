using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Api.Implementation;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.CacheUpdater;
using Gw2Unlocks.Common;
using Gw2Unlocks.Wiki.Cache;
using Gw2Unlocks.Wiki.Implementation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddApiSource()
                .AddJsonCacheApi()
                .AddCacheDir()
                .AddWikiSource()
                .AddCacheWikiAsCache();

builder.Services.AddUpdater();

var host = builder.Build();
//AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
//{
//    try
//    {
//        await using var scope = host.Services.CreateAsyncScope();
//        var disposableService = scope.ServiceProvider.GetRequiredService<IDotnetOsInteractionMarker>();
//        await disposableService.DisposeAsync();
//    }
//    catch (ObjectDisposedException) { /* happens when pressing Ctrl+C */}
//    catch (Exception ex)
//    {
//        Console.Error.WriteLine(ex.ToString());
//    }
//};
await host.RunAsync();
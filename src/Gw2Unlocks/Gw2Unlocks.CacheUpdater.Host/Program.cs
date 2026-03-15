using Gw2Unlocks.Cache.SqlLite;
using Gw2Unlocks.CacheUpdater;
using Gw2Unlocks.CacheUpdater.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;


var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

//builder.Logging.SetupLogging(builder.Configuration);
builder.Services.AddGw2Client()
                .AddUpdater()
                .AddSqlLiteGw2Cache("db.sqlite");

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
//await host.RunAsync();

//might move this to background service later
var updater = host.Services.GetRequiredService<IUpdater>();
await updater.UpdateItems(CancellationToken.None);
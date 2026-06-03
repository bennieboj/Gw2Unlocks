using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.Common;
using Gw2Unlocks.UnlockClassifier.Cache;
using Gw2Unlocks.Website;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

static void ConfigureServices(ILoggingBuilder loggingBuilder, IServiceCollection services, IConfiguration configuration)
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
    loggingBuilder.SetupLogging(configuration);

    services.AddCacheDir()
            .AddClassifierCache()
            .AddHostedService<SiteGeneratorService>();
}

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";
if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureServices(builder.Logging, builder.Services, builder.Configuration);

    //builder.WebHost.UseUrls("http://*:5000"); // uncomment to host on local network, view on other device
    var app = builder.Build();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.Run();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);
    ConfigureServices(builder.Logging, builder.Services, builder.Configuration);
    var app = builder.Build();
    app.Run();
}





using GuildWars2;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Hedging;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.HttpStatusCode;

namespace Gw2Unlocks.Api;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddGw2Client(this IServiceCollection services)
    {
        var builder = services
            .AddHttpClient<Gw2Client>(client =>
            {
                // Infinite timeout since Polly handles timeouts
                client.Timeout = Timeout.InfiniteTimeSpan;
            });


        builder.ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                AutomaticDecompression =
                    System.Net.DecompressionMethods.GZip |
                    System.Net.DecompressionMethods.Deflate
            });


        builder.AddResilienceHandler("Gw2Resiliency", builder =>
            {
                builder
                    .AddTimeout(Gw2Resiliency.TotalTimeoutStrategy)
                    .AddRetry(Gw2Resiliency.RetryStrategy)
                    .AddCircuitBreaker(Gw2Resiliency.CircuitBreakerStrategy)
                    //.AddHedging(Gw2Resiliency.HedgingStrategy)
                    .AddTimeout(Gw2Resiliency.AttemptTimeoutStrategy);
            });
        return builder;
    }
}

internal static class Gw2Resiliency
{
    public static readonly TimeoutStrategyOptions TotalTimeoutStrategy = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };

    public static readonly RetryStrategyOptions<HttpResponseMessage> RetryStrategy = new()
    {
        MaxRetryAttempts = 10,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = static args => ShouldHandleCommon(args.Outcome)
    };

    public static readonly CircuitBreakerStrategyOptions<HttpResponseMessage> CircuitBreakerStrategy = new()
    {
        ShouldHandle = static args => ShouldHandleCommon(args.Outcome)
    };

    public static readonly HedgingStrategyOptions<HttpResponseMessage> HedgingStrategy = new()
    {
        Delay = TimeSpan.FromSeconds(10),
        ShouldHandle = static args => ShouldHandleCommon(args.Outcome)
    };

    public static readonly TimeoutStrategyOptions AttemptTimeoutStrategy = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static async ValueTask<bool> ShouldHandleCommon(Outcome<HttpResponseMessage> outcome)
    {
        return outcome switch
        {
            { Exception: OperationCanceledException } => true,
            { Exception: HttpRequestException } => true,
            { Exception: TimeoutRejectedException } => true,
            { Exception: BrokenCircuitException } => true,
            { Result.StatusCode: RequestTimeout } => true,
            { Result.StatusCode: TooManyRequests } => true,
            { Result.StatusCode: InternalServerError } => true,
            { Result.StatusCode: BadGateway } => true,
            { Result.StatusCode: ServiceUnavailable } => await ShouldRetryOn503(outcome),
            { Result.StatusCode: GatewayTimeout } => true,
            { Result.IsSuccessStatusCode: false, Result.Content.Headers.ContentLength: 0 } => true,
            { Result.IsSuccessStatusCode: false } => await GetText(outcome) is
                "endpoint requires authentication" or "unknown error" or "ErrBadData" or "ErrTimeout",
            _ => false
        };
    }

    private static async ValueTask<bool> ShouldRetryOn503(Outcome<HttpResponseMessage> outcome)
    {
        var text = await GetText(outcome) ?? string.Empty;

        // retry if text does NOT contain these phrases
        return !text.Equals("API not active", StringComparison.OrdinalIgnoreCase) &&
               !text.Contains("API Temporarily disabled", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> GetText(Outcome<HttpResponseMessage> attempt)
    {
        if (attempt.Result is null)
        {
            return null;
        }

        var contentType = attempt.Result.Content.Headers.ContentType?.MediaType;

        if (contentType is null)
        {
            return null;
        }

        // IMPORTANT: buffer the Content to make ReadAsStreamAsync return a rewindable MemoryStream
        await attempt.Result.Content.LoadIntoBufferAsync().ConfigureAwait(false);

        // ALSO IMPORTANT: do not dispose the MemoryStream because subsequent ReadAsStreamAsync calls return the same instance
        Stream content = await attempt.Result.Content.ReadAsStreamAsync().ConfigureAwait(false);
        try
        {
            switch (contentType)
            {
                case "application/json":
                    {
                        using JsonDocument json = await JsonDocument.ParseAsync(content).ConfigureAwait(false);
                        return json.RootElement.TryGetProperty("text", out JsonElement text)
                            ? text.GetString()
                            : null;
                    }

                case "text/html":
                    {
                        var html = await attempt.Result.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return html.Length > 500 ? html[..500] : html;
                    }

                default:
                    return null;
            }
        }
        finally
        {
            // ALSO IMPORTANT: rewind the stream for subsequent reads
            if (content.CanSeek)
            {
                content.Position = 0;
            }
        }
    }

}
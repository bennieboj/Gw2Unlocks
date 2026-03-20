using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace Gw2Unlocks.Testing.Common;

public sealed class XunitLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new XunitLogger(output, categoryName);

    public void Dispose() { }

    private sealed class XunitLogger(ITestOutputHelper output, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            output.WriteLine($"[{logLevel}] {category}: {formatter(state, exception)}");

            if (exception != null)
            {
                output.WriteLine(exception.ToString());
            }
        }
    }
}

using Microsoft.Extensions.Logging;

namespace RippleSync.Tests.Shared.TestDoubles.Logging;
public static class LoggerDoubles
{
    public static class Fakes
    {
        public class FakeLogger<T> : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}

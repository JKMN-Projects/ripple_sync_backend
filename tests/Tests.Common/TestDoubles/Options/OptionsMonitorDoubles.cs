
using Microsoft.Extensions.Options;

namespace RippleSync.Tests.Common.TestDoubles.Options;
public static class OptionsMonitorDoubles
{

    public static class Stubs
    {
        public class FixedOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class, new()
        {
            public FixedOptionsMonitor(TOptions options)
            {
                CurrentValue = options;
            }

            public TOptions CurrentValue { get; }
            public TOptions Get(string? name)
                => CurrentValue;

            public IDisposable OnChange(Action<TOptions, string> listener) => NullDisposable.Instance;
            private class NullDisposable : IDisposable
            {
                public static readonly NullDisposable Instance = new();
                public void Dispose() { }
            }
        }
    }
}

using System.Net.Http;
using GitVersion.Agents;
using GitVersion.Telemetry;

namespace GitVersion.Testing.Telemetry;

/// <summary>Provides scripted service doubles without adding callback constructors to production reporting.</summary>
internal static class TelemetryTestDoubles
{
    /// <summary>Defers a test's build-agent selection until Resolve is called.</summary>
    internal sealed class CallbackBuildAgentResolver(Func<ICurrentBuildAgent> resolve) : IBuildAgentResolver
    {
        /// <summary>Runs the scripted resolver.</summary>
        public ICurrentBuildAgent Resolve() => resolve();
    }

    /// <summary>Supplies exact sampling draws and records forbidden draws in policy tests.</summary>
    internal sealed class CallbackRandom(Func<double> next) : Random
    {
        /// <summary>Returns the scripted draw.</summary>
        public override double NextDouble() => next();
    }

    /// <summary>Captures or rejects policy disclosure through the notice service contract.</summary>
    internal sealed class CallbackNotice(Action<string> show) : ITelemetryNotice
    {
        /// <summary>Runs the scripted disclosure.</summary>
        public void Show(string message) => show(message);
    }

    /// <summary>Captures deferred runtime reads through the runtime service contract.</summary>
    internal sealed class CallbackRuntimeContextReader(Func<TelemetryRuntimeContext> read) : ITelemetryRuntimeContextReader
    {
        /// <summary>Returns the scripted runtime dimensions.</summary>
        public TelemetryRuntimeContext Read() => read();
    }

    /// <summary>Exercises the real transport send/disposal path with a fake HTTP client.</summary>
    internal sealed class CallbackTransport(Func<HttpClient> createClient) : TelemetryTransport
    {
        /// <summary>Creates the fake client only when delivery begins.</summary>
        protected override HttpClient CreateClient() => createClient();
    }
}

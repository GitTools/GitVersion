using System.Net;
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

    /// <summary>Keeps campaign eligibility independent of the machine's current date.</summary>
    internal sealed class FixedClock : TimeProvider
    {
        /// <summary>Returns a UTC instant inside the test campaign's collection window.</summary>
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Captures outgoing request details and returns a chosen status without using the network.</summary>
    internal sealed class CaptureHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal List<string> Bodies { get; } = [];
        internal Uri? Endpoint { get; private set; }
        internal HttpMethod? Method { get; private set; }
        internal string? ContentType { get; private set; }
        /// <summary>Records the request endpoint, method, content type, and body before returning the configured response.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Endpoint = request.RequestUri;
            Method = request.Method;
            ContentType = request.Content!.Headers.ContentType!.MediaType;
            Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status);
        }
    }

    /// <summary>Simulates either an immediate network error or a request stalled until cancellation.</summary>
    internal sealed class FailureHandler(bool timeout) : HttpMessageHandler
    {
        internal CancellationToken Token { get; private set; }
        /// <summary>Throws an offline error or captures the cancellation token and waits for its deadline.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!timeout)
            {
                throw new HttpRequestException("Offline");
            }
            Token = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}

using System.Net;
using System.Net.Http;
using static GitVersion.Testing.Telemetry.TelemetryTestDoubles;

namespace GitVersion.Telemetry.Tests;

/// <summary>Verifies the transport owns resources without making external HTTP requests.</summary>
public class TelemetryTransportTests
{
    /// <summary>Creates a client only at send time and disposes both client handler and response content.</summary>
    [Test]
    public async Task DeliveryDisposesClientAndResponse()
    {
        using var handler = new OwnershipHandler();
        var creations = 0;
        var transport = new CallbackTransport(() =>
        {
            creations++;
            return new HttpClient(handler);
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/");

        creations.ShouldBe(0);
        await transport.SendAsync(request, CancellationToken.None);

        creations.ShouldBe(1);
        handler.Sends.ShouldBe(1);
        handler.Disposed.ShouldBeTrue();
        handler.ResponseContent.Disposed.ShouldBeTrue();
    }

    /// <summary>Releases the client promptly even if the underlying handler ignores cancellation.</summary>
    [Test]
    public async Task CancellationDisposesClientWhenHandlerIgnoresToken()
    {
        using var handler = new OwnershipHandler(ignoreCancellation: true);
        var transport = new CallbackTransport(() => new HttpClient(handler));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/");
        using var cancellation = new CancellationTokenSource();
        try
        {
            var send = transport.SendAsync(request, cancellation.Token);
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
            await cancellation.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(() => send.WaitAsync(TimeSpan.FromSeconds(3)));
            handler.Sends.ShouldBe(1);
            handler.Disposed.ShouldBeTrue();
        }
        finally
        {
            handler.ReleasePendingRequest();
            handler.ResponseContent.Dispose();
        }
    }

    /// <summary>Captures response disposal independently of handler disposal.</summary>
    private sealed class OwnershipContent() : StringContent("ignored body")
    {
        internal bool Disposed { get; private set; }

        /// <summary>Records ownership release and delegates content cleanup.</summary>
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>Returns an owned response or deliberately waits without observing cancellation.</summary>
    private sealed class OwnershipHandler(bool ignoreCancellation = false) : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal OwnershipContent ResponseContent { get; } = new();
        internal int Sends { get; private set; }
        internal bool Disposed { get; private set; }

        /// <summary>Signals receipt and models cooperative delivery or an uncooperative handler.</summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Sends++;
            Started.TrySetResult();
            return ignoreCancellation
                ? this.pending.Task
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = ResponseContent });
        }

        /// <summary>Releases the deliberately pending task during test cleanup.</summary>
        internal void ReleasePendingRequest() => this.pending.TrySetCanceled();

        /// <summary>Records HTTP client disposal through the handler it owns.</summary>
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}

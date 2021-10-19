namespace GitVersion.Logging;

public static class Disposable
{
    public static IDisposable Create(Action disposer) => new AnonymousDisposable(disposer);

    public static readonly IDisposable Empty = Create(() => { });

    private sealed class AnonymousDisposable : IDisposable
    {
        public AnonymousDisposable(Action disposer) => this.disposer = disposer ?? throw new ArgumentNullException(nameof(disposer));

        public void Dispose()
        {
            this.disposer?.Invoke();
            this.disposer = null;
        }

        private Action? disposer;
    }
}

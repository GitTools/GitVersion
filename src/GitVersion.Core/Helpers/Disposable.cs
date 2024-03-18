using GitVersion.Extensions;

namespace GitVersion.Helpers;

public static class Disposable
{
    public static IDisposable Create(Action disposer) => new AnonymousDisposable(disposer);

    public static readonly IDisposable Empty = Create(() => { });

    private sealed class AnonymousDisposable(Action disposer) : IDisposable
    {
        public void Dispose()
        {
            this.disposer?.Invoke();
            this.disposer = null;
        }

        private Action? disposer = disposer.NotNull();
    }
}

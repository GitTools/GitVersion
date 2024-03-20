using GitVersion.Extensions;

namespace GitVersion.Helpers;

public static class Disposable
{
    public static IDisposable Create(Action disposer) => new AnonymousDisposable(disposer);
    public static IDisposable<T> Create<T>(T value, Action disposer) => new AnonymousDisposable<T>(value, disposer);

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

    private sealed class AnonymousDisposable<T>(T value, Action disposer) : IDisposable<T>
    {
        public void Dispose()
        {
            this.disposer?.Invoke();
            this.disposer = null;
        }

        private Action? disposer = disposer.NotNull();
        public T Value => value;
    }
}

public interface IDisposable<out T> : IDisposable
{
    T Value { get; }
}

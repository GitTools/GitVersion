using GitVersion.Extensions;

namespace GitVersion.Helpers;

/// <summary>Factory methods for creating lightweight <see cref="IDisposable"/> wrappers around cleanup actions.</summary>
public static class Disposable
{
    /// <summary>Creates an <see cref="IDisposable"/> that invokes <paramref name="disposer"/> when disposed.</summary>
    public static IDisposable Create(Action disposer) => new AnonymousDisposable(disposer);

    /// <summary>Creates an <see cref="IDisposable{T}"/> that holds <paramref name="value"/> and invokes <paramref name="disposer"/> when disposed.</summary>
    public static IDisposable<T> Create<T>(T value, Action disposer) => new AnonymousDisposable<T>(value, disposer);

    /// <summary>A no-op disposable that does nothing when disposed.</summary>
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

/// <summary>An <see cref="IDisposable"/> that also exposes a value of type <typeparamref name="T"/>.</summary>
public interface IDisposable<out T> : IDisposable
{
    /// <summary>Gets the value held by this disposable wrapper.</summary>
    T Value { get; }
}

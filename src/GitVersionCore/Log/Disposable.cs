using System;

namespace GitVersion.Logging
{
    public static class Disposable
    {
        static Disposable()
        {
            Empty = Create(() => { });
        }

        public static IDisposable Create(Action disposer) => new AnonymousDisposable(disposer);

        public static readonly IDisposable Empty;

        private sealed class AnonymousDisposable : IDisposable
        {
            public AnonymousDisposable(Action disposer)
            {
                _disposer = disposer ?? throw new ArgumentNullException(nameof(disposer));
            }

            public void Dispose()
            {
                _disposer?.Invoke();
                _disposer = null;
            }

            private Action _disposer;
        }
    }
}
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
                this.disposer = disposer ?? throw new ArgumentNullException(nameof(disposer));
            }

            public void Dispose()
            {
                disposer?.Invoke();
                disposer = null;
            }

            private Action disposer;
        }
    }
}
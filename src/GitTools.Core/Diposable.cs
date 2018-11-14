namespace GitTools
{
    using System;
    using Logging;

    /// <summary>
    /// Base class for disposable objects.
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(Disposable));

        readonly object _syncRoot = new object();

        bool _disposing;
        
        /// <summary>
        /// Finalizes an instance of the <see cref="Disposable"/> class.
        /// </summary>
        ~Disposable()
        {
            Dispose(false);
        }
        
        bool IsDisposed { get; set; }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the managed resources.
        /// </summary>
        protected virtual void DisposeManaged()
        {
        }

        /// <summary>
        /// Disposes the unmanaged resources.
        /// </summary>
        protected virtual void DisposeUnmanaged()
        {
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        void Dispose(bool isDisposing)
        {
            lock (_syncRoot)
            {
                if (!IsDisposed)
                {
                    if (!_disposing)
                    {
                        _disposing = true;

                        if (isDisposing)
                        {
                            try
                            {
                                DisposeManaged();
                            }
                            catch (Exception ex)
                            {
                                Log.ErrorException("Error while disposing managed resources of '{0}'.", ex, GetType().FullName);
                            }
                        }

                        try
                        {
                            DisposeUnmanaged();
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorException("Error while disposing unmanaged resources of '{0}'.", ex, GetType().FullName);
                        }

                        IsDisposed = true;
                        _disposing = false;
                    }
                }
            }
        }
    }
}
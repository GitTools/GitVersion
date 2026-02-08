namespace GitVersion.Testing;

/// <summary>
/// A test logger that captures log messages for assertions.
/// </summary>
/// <typeparam name="T">The type the logger is for.</typeparam>
public class TestLogger<T>(Action<string>? logAction = null) : ILogger<T>
{
    private readonly List<string> messages = [];

    public IReadOnlyList<string> Messages => this.messages;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        this.messages.Add(message);
        logAction?.Invoke(message);
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

/// <summary>
/// A logger factory that creates TestLogger instances.
/// </summary>
public class TestLoggerFactory(Action<string>? logAction = null) : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) { }

    public ILogger CreateLogger(string categoryName) => new TestLogger<object>(logAction);

    public void Dispose() { }

    /// <summary>
    /// Registers this factory and its loggers with the service collection.
    /// Removes any existing logger registrations to ensure this factory is used.
    /// </summary>
    public void RegisterWith(IServiceCollection services)
    {
        services.RemoveAll<ILoggerFactory>();
        services.RemoveAll(typeof(ILogger<>));
        services.AddSingleton<ILoggerFactory>(this);
        services.AddSingleton(typeof(ILogger<>), typeof(FactoryLogger<>));
    }
}

/// <summary>
/// A generic logger implementation that gets created from the ILoggerFactory.
/// </summary>
public class FactoryLogger<T>(ILoggerFactory loggerFactory) : ILogger<T>
{
    private readonly ILogger logger = loggerFactory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => this.logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => this.logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        this.logger.Log(logLevel, eventId, state, exception, formatter);
}

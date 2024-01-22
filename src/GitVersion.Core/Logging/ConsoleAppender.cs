namespace GitVersion.Logging;

internal class ConsoleAppender : ILogAppender
{
    private readonly object locker;
    private readonly IDictionary<LogLevel, (ConsoleColor, ConsoleColor)> palettes;
    public ConsoleAppender()
    {
        this.locker = new();
        this.palettes = CreatePalette();
    }
    public void WriteTo(LogLevel level, string message)
    {
        lock (this.locker)
        {
            try
            {
                var (backgroundColor, foregroundColor) = this.palettes[level];

                Console.BackgroundColor = backgroundColor;
                Console.ForegroundColor = foregroundColor;

                if (level is LogLevel.Error or LogLevel.Fatal)
                {
                    Console.Error.Write(message);
                }
                else
                {
                    Console.Write(message);
                }
            }
            finally
            {
                Console.ResetColor();
                if (level is LogLevel.Error or LogLevel.Fatal)
                {
                    Console.Error.WriteLine();
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }
    }

    private static IDictionary<LogLevel, (ConsoleColor backgroundColor, ConsoleColor foregroundColor)> CreatePalette()
    {
        var background = Console.BackgroundColor;
        var palette = new Dictionary<LogLevel, (ConsoleColor, ConsoleColor)>
        {
            { LogLevel.Error, (ConsoleColor.DarkRed, ConsoleColor.White) },
            { LogLevel.Warn, (background, ConsoleColor.Yellow) },
            { LogLevel.Info, (background, ConsoleColor.White) },
            { LogLevel.Debug, (background, ConsoleColor.DarkGray) }
        };
        return palette;
    }
}

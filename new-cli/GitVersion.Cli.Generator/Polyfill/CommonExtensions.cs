namespace GitVersion.Polyfill;

public static class CommonExtensions
{
    public static T NotNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string name = "")
        where T : class => value ?? throw new ArgumentNullException(name);
}

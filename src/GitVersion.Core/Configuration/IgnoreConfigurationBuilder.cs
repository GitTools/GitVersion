namespace GitVersion.Configuration;

internal sealed class IgnoreConfigurationBuilder
{
    public static IgnoreConfigurationBuilder New => new();

    private DateTimeOffset? before;

    private HashSet<string> shas = new();

    public IgnoreConfigurationBuilder WithBefore(DateTimeOffset? value)
    {
        this.before = value;
        return this;
    }

    public IgnoreConfigurationBuilder WithShas(IEnumerable<string> values)
    {
        WithShas(new HashSet<string>(values));
        return this;
    }

    public IgnoreConfigurationBuilder WithShas(params string[] values)
    {
        WithShas(new HashSet<string>(values));
        return this;
    }

    public IgnoreConfigurationBuilder WithShas(HashSet<string> value)
    {
        this.shas = value;
        return this;
    }

    public IIgnoreConfiguration Build() => new IgnoreConfiguration()
    {
        Before = before,
        Shas = shas
    };
}

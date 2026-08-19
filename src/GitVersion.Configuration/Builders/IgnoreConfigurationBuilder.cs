namespace GitVersion.Configuration;

internal sealed class IgnoreConfigurationBuilder
{
    public static IgnoreConfigurationBuilder New => new();

    private DateTimeOffset? before;

    private HashSet<string> shas = [];

    private HashSet<string> tags = [];

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

    public IgnoreConfigurationBuilder WithTags(IEnumerable<string> values)
    {
        WithTags(new HashSet<string>(values));
        return this;
    }

    public IgnoreConfigurationBuilder WithTags(params string[] values)
    {
        WithTags(new HashSet<string>(values));
        return this;
    }

    public IgnoreConfigurationBuilder WithTags(HashSet<string> value)
    {
        this.tags = value;
        return this;
    }

    public IIgnoreConfiguration Build() => new IgnoreConfiguration
    {
        Before = this.before,
        Shas = this.shas,
        Tags = this.tags
    };
}

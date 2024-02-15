namespace GitVersion.Configuration
{
    public interface IIgnoreConfiguration
    {
        DateTimeOffset? Before { get; }

        ISet<string> Shas { get; }

        public bool IsEmpty => Before == null && Shas.Count == 0;
    }
}

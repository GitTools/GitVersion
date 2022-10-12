namespace GitVersion.Configurations;

[Serializable]
public class ConfigurationException : GitVersionException
{
    public ConfigurationException(string msg)
        : base(msg)
    {
    }
}

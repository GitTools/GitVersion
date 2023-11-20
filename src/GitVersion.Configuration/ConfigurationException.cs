namespace GitVersion.Configuration;

[Serializable]
public class ConfigurationException : GitVersionException
{
    public ConfigurationException(string msg)
        : base(msg)
    {
    }

    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ConfigurationException(string messageFormat, params object[] args) : base(messageFormat, args)
    {
    }

    public ConfigurationException()
    {
    }
}

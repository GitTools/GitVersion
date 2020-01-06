namespace GitVersion.Configuration
{
    public interface IConfigFileLocatorFactory
    {
        IConfigFileLocator Create();
    }
}
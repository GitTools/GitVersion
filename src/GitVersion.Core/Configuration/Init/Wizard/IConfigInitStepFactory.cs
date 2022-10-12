namespace GitVersion.Configurations.Init.Wizard;

public interface IConfigInitStepFactory
{
    T CreateStep<T>() where T : notnull;
}

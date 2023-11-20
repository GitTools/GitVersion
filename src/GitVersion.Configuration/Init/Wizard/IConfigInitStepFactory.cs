namespace GitVersion.Configuration.Init.Wizard;

internal interface IConfigInitStepFactory
{
    T CreateStep<T>() where T : notnull;
}

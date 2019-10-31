namespace GitVersion.Configuration.Init.Wizard
{
    public interface IConfigInitStepFactory
    {
        T CreateStep<T>();
    }
}

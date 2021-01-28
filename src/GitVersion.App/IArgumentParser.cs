namespace GitVersion
{
    public interface IArgumentParser
    {
        Arguments ParseArguments(string commandLineArguments);
        Arguments ParseArguments(string[] commandLineArguments);
    }
}

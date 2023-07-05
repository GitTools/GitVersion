namespace GitVersion;

internal interface IArgumentParser
{
    Arguments ParseArguments(string commandLineArguments);
    Arguments ParseArguments(string[] commandLineArguments);
}

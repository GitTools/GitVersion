using System.Collections.Generic;

namespace GitVersion
{
    public interface IArgumentParser
    {
        Arguments ParseArguments(string commandLineArguments);
        Arguments ParseArguments(List<string> commandLineArguments);
    }
}
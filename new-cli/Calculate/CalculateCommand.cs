using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Calculate
{
    public class CalculateCommand : Command
    {
        public CalculateCommand(): base("calculate", "Calculates the version object from the git history.")
        {
            AddOption(new Option<string>("--work-dir", "The working directory with the git repository"));
            Handler = CommandHandler.Create<CalculateOptions>(Calculate);
        }

        private void Calculate(CalculateOptions o)
        {
            Console.WriteLine($"Command : 'calculate', LogFile : '{o.LogFile}', WorkDir : '{o.WorkDir}' ");
        }
    }
}
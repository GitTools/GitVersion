using System.CommandLine;

namespace GitVersion.Cli
{

    public class RootCommand : System.CommandLine.RootCommand
    {
        public RootCommand(CalculateCommand calculateCommand) : base("Versioning for your git repository, solved!")
        {
            AddGlobalOptions();
            this.AddCommand(calculateCommand);
        }

        private void AddGlobalOptions()
        {          
            var option = new Option("--log-to") { Argument = new LogToArgument() };      
            this.AddGlobalOption(option);
        }
    }
}

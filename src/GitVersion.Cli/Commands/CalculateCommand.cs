using GitVersion.Logging;
using LibGit2Sharp;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace GitVersion.Cli
{
    public class CalculateCommand : Command
    {
        private readonly Logging.IConsole console;
        private readonly IGitVersionCalculator gitversionCalculator;
        private readonly CommandWrapper executor;

        public CalculateCommand(Logging.IConsole console, IGitVersionCalculator gitversionCalculator, CommandWrapper executor) : base("calculate", "Calculates version information from your git repository")
        {
            this.console = console;
            this.gitversionCalculator = gitversionCalculator;
            this.executor = executor;
            AddOptions();

            this.Handler = CommandHandler.Create<GlobalOptions, CalculateOptions>(ExecuteAsync);
        }

        private void AddOptions()
        {
            this.AddOption(new Option<bool>(
                 "--normalize",
                 "Attempt to mutate your git repository so gitversion has enough information (local branches, commit history etc) to calculate."));
            this.AddOption(new Option<string>(
                 "--dot-git-dir",
                 "The path of the .git folder. If not specified, will be discovered from the envrionment current working directory."));
            this.AddOption(new Option<string>(
                 "--no-cache",
                 "Whether to disable caching of the version variables. If true is specified, any previous cached calculation will be ignored and the new calculation won't be cached either."));

            this.AddOption(new Option<string>(
                "--branch",
                 "The target branch to calculate a version against."));

        }

        private async Task<int> ExecuteAsync(GlobalOptions globalOptions, CalculateOptions options)
        {
            // The executor wraps execution of the command logic inside somethng that
            // will do error handling according to the old behaviour.
            return await executor.Execute(globalOptions, async () =>
            {
                // if .git directory not specified, discover it from current environment working directory.
                if (string.IsNullOrWhiteSpace(options.DotGitDir))
                {
                    options.DotGitDir = Repository.Discover(globalOptions.WorkingDirectory);
                }

                if (options.Normalize ?? false)
                {
                    Normalize();
                }

                //TODO: support the config override param, and also inside the CalculateVersionVariables it does a prepare - we should be able
                // to pass args to control that as well. i.e using the above (options.Normalize) arg.
                var variables = this.gitversionCalculator.CalculateVersionVariables(options.NoCache, null);
                console.WriteLine(variables.ToString());
                return 0;
            });
        }

        private void Normalize()
        {
            executor.Log.Info("TODO: Implement --normalize");
           // throw new NotImplementedException();
        }
    }

    public class CalculateOptions
    {
        public CalculateOptions()
        {

        }
        public CalculateOptions(bool? normalize)
        {
            //GlobalOptions = globalOptions;
            Normalize = normalize;
        }
        public bool? Normalize { get; }
        public bool? NoCache { get; }
        /// <summary>
        /// The path of the .git folder. If not specified, will be discovered from the envrionment current working directory.
        /// </summary>
        public string DotGitDir { get; set; }

        public string Branch { get; set; }
    }

}

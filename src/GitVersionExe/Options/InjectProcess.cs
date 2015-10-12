namespace GitVersion.Options
{
    using System.Collections.Generic;
    using CommandLine;
    using CommandLine.Text;

    [Verb("inject-process", 
        HelpText = "Executes target executable injecting GitVersion variables as environmental variables.")]
    class InjectProcess : LoggingOptions
    {
        [Option('e', "executable", HelpText = "Path to executable filename.", Required = true)]
        public string ExecutableFileName { get; set; }

        [Option('a', "arguments")]
        public string Arguments { get; set; }

        [Option('v', "variables", HelpText = "Variables to inject, defaults to all.")]
        public IEnumerable<string> Variables { get; set; }

        [Usage(ApplicationAlias = "GitVersion")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Inject all gitversion variables into MyApp", new InjectProcess { ExecutableFileName = "MyApp.exe" });
                yield return new Example("Inject only SemVer variable into MyApp", 
                    new InjectProcess { ExecutableFileName = "MyApp.exe", Variables = new[] {"SemVer"}});
                yield return new Example("Start MyApp with --verbose and --short argument injecting all GitVersion variables", 
                    new InjectProcess { ExecutableFileName = "MyApp.exe", Arguments = "--verbose --short" });
            }
        }
    
    }
}

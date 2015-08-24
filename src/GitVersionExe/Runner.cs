using System;

namespace GitVersion
{
    using System.Linq;
    using GitVersion.Helpers;
    using GitVersion.Options;

    class InitRunner
    {
        public static void Run(InitOptions opts)
        {

            throw new NotImplementedException(opts.GetType().Name);
        }
    }

    class InspectRunner
    {
        public static void Run(InspectOptions opts)
        {
            var inputVariables = new InputVariables()
                {
                    TargetPath = opts.Path,
                };

            var fs = new FileSystem();
            var allVariables = SpecifiedArgumentRunner.GetVariables(fs, inputVariables);

            // TODO: allow more variables
            var showVariable = opts.Variables.First();

            switch (showVariable)
            {
                case null:
                    // TODO: allow more output formatters
                    Console.WriteLine(JsonOutputFormatter.ToJson(allVariables));
                    break;

                default:
                    string part;
                    if (!allVariables.TryGetValue(showVariable, out part))
                    {
                        throw new WarningException(string.Format("'{0}' variable does not exist", showVariable));
                    }
                    Console.WriteLine(part);
                    break;
            }
        }
    }
    
    class InjectBuildServerRunner
    {
        public static void Run(InjectBuildServerOptions opts)
        {
            var inputVariables = new InputVariables(); // TODO: how to map to input variables
            var fs = new FileSystem();
            var variables = SpecifiedArgumentRunner.GetVariables(fs, inputVariables);

            foreach (var buildServer in BuildServerList.GetApplicableBuildServers())
            {
                buildServer.WriteIntegration(Console.WriteLine, variables);
            }
        }
    }

     
}

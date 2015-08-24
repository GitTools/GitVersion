using System;

namespace GitVersion
{
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
            throw new NotImplementedException(opts.GetType().Name);
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

namespace GitVersion.Runners
{
    using System;
    using GitVersion.Helpers;
    using GitVersion.Options;

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
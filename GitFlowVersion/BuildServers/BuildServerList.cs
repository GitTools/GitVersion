namespace GitFlowVersion
{
    using System.Collections.Generic;
    using BuildServers;

    public static class BuildServerList
    {
        public static List<IBuildServer> BuildServers = new List<IBuildServer>
            {
                new ContinuaCi(),
                new TeamCity(),
                new ProcessEnvironment()
            };

    }
}

namespace GitFlowVersion
{
    using System.Collections.Generic;

    public static class BuildServerList
    {
        public static List<IBuildServer> BuildServers = new List<IBuildServer>
            {
                new ContinuaCi(),
                new TeamCity()
            };

    }
}

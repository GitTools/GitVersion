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

        public static IEnumerable<IBuildServer> GetApplicableBuildServers()
        {
            foreach (var buildServer in BuildServers)
            {
                if (buildServer.CanApplyToCurrentContext())
                {
                    Logger.WriteInfo(string.Format("Applicable build agent found: '{0}'.", buildServer.GetType().Name));
                    yield return buildServer;
                }
            }
        }
    }
}

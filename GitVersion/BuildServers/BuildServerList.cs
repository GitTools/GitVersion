namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public static class BuildServerList
    {
        public static List<IBuildServer> BuildServers = new List<IBuildServer>
            {
                new ContinuaCi(),
                new TeamCity()
            };

        public static Func<IEnumerable<IBuildServer>> Selector = () => DefaultSelector();

        public static void ResetSelector()
        {
            Selector = DefaultSelector;
        }

        public static IEnumerable<IBuildServer> GetApplicableBuildServers()
        {
            return Selector();
        }

        static IEnumerable<IBuildServer> DefaultSelector()
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

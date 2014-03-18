namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public static class BuildServerList
    {
        static List<IBuildServer> BuildServers;

        public static Func<Arguments, IEnumerable<IBuildServer>> Selector = arguments => DefaultSelector(arguments);

        public static void ResetSelector()
        {
            Selector = DefaultSelector;
        }

        public static IEnumerable<IBuildServer> GetApplicableBuildServers(Arguments arguments)
        {
            return Selector(arguments);
        }

        static IEnumerable<IBuildServer> DefaultSelector(Arguments arguments)
        {
            if (BuildServers == null)
            {
                BuildServers = new List<IBuildServer>
                {
                    new ContinuaCi(arguments),
                    new TeamCity(arguments)
                };
            }

            var buildServices = new List<IBuildServer>();

            foreach (var buildServer in BuildServers)
            {
                try
                {
                    if (buildServer.CanApplyToCurrentContext())
                    {
                        Logger.WriteInfo(string.Format("Applicable build agent found: '{0}'.", buildServer.GetType().Name));
                        buildServices.Add(buildServer);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning(string.Format("Failed to check build server '{0}': {1}", buildServer.GetType().Name, ex.Message));
                }
            }

            return buildServices;
        }
    }
}

namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;
    using GitFlowVersion.Integration.ContinuaCI;

    public class IntegrationManager
    {
        private static readonly IntegrationManager _default = new IntegrationManager();
        private readonly List<IIntegration> _integrations = new List<IIntegration>();

        public IntegrationManager()
        {
            // Note: to not have to use reflection, for now register all manually
            _integrations.Add(new TeamCity());
            _integrations.Add(new ContinuaCi());
        }

        public IEnumerable<IIntegration> Integrations { get { return _integrations; } }

        // Note: method because of the build task project
        public static IntegrationManager Default()
        {
            return _default;
        }

        public bool IsRunningInBuildAgent()
        {
            foreach (var integration in Integrations)
            {
                if (integration.IsRunningInBuildAgent())
                {
                    return true;
                }
            }

            return false;
        }
    }
}

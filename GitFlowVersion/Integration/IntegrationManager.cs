namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using ContinuaCI;

    public class IntegrationManager
    {
        private static readonly IntegrationManager _default = new IntegrationManager();
        private readonly Dictionary<string, IIntegration> _integrations = new Dictionary<string, IIntegration>();
        private readonly IIntegration _localIntegration = new LocalIntegration();

        public IntegrationManager()
        {
            // Note: to not have to use reflection, for now register all manually
            _integrations.Add("TEAM_CITY", new TeamCity());
            _integrations.Add("CONTINUA_CI", new ContinuaCi());
        }

        public IDictionary<string, IIntegration> Integrations 
        {
            get
            {
                var res = new Dictionary<string, IIntegration>(_integrations)
                          {
                              { "LOCAL", _localIntegration }
                          };

                return res;
            } 
        }

        // Note: method because of the build task project
        public static IntegrationManager Default()
        {
            return _default;
        }

        /// <summary>
        /// Retrieves the <see cref="IIntegration"/> identified by the provided code.
        /// </summary>
        /// <param name="integrationCode">The code</param>
        /// <returns>The <see cref="IIntegration"/> identified by <paramref name="integrationCode"/></returns>
        /// <exception cref="KeyNotFoundException">Thrown when no matching <see cref="IIntegration"/> has been found.</exception>
        public IIntegration GetByCode(string integrationCode)
        {
            IIntegration res;

            if (Integrations.TryGetValue(integrationCode, out res))
            {
                return res;
            }

            throw new KeyNotFoundException(
                string.Format("No IIntegration matching '{0}' code has been found. Available ones are : {1}."
                , integrationCode
                , string.Join(", ", Integrations.Keys)));
        }

        /// <summary>
        /// This will return the sole matching <see cref="IIntegration"/>.
        /// <para>If more than one <see cref="IIntegration"/> seem to apply, an <see cref="ErrorException"/> will be thrown.</para>
        /// </summary>
        /// <returns>The <see cref="IIntegration"/> matching the current execution context.</returns>
        public IIntegration Detect()
        {
            var res = _integrations.Values
                        .Where(integration => integration.CanApplyToCurrentContext())
                        .ToList();

            if (res.Count > 1)
            {
                throw new ErrorException("More than one IIntegration seem to apply to current execution context.");
            }

            if (res.Count == 1)
            {
                return res[0];
            }

            return _localIntegration;
        }
    }
}

using GitVersion.Extensions;

namespace GitVersion.Configuration.SupportedWorkflows
{
    internal static class WorkflowManager
    {
        private static readonly string ResourceNameTemplate = DetermineResourceNameTemplate();

        private static string DetermineResourceNameTemplate()
        {
            var fullClassName = typeof(WorkflowManager).FullName!;
            var resourceNamePrefix = fullClassName[..(fullClassName.Length - nameof(WorkflowManager).Length - 1)];
            return $"{resourceNamePrefix}.{{0}}.yml";
        }

        public static Dictionary<object, object?>? GetOverrideConfiguration(string? workflow)
        {
            if (string.IsNullOrEmpty(workflow)) return null;

            var resourceName = GetResourceName(workflow);
            var embeddedResource = resourceName.ReadAsStringFromEmbeddedResource(typeof(WorkflowManager).Assembly);
            return ConfigurationSerializer.Deserialize<Dictionary<object, object?>>(embeddedResource);
        }

        private static string GetResourceName(string workflow)
            => ResourceNameTemplate.Replace("{0}", workflow.Replace('/', '.'));
    }
}

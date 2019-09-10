using System.Collections.Generic;
using GitVersion.Common;

namespace GitVersionTask.Tests.Helpers
{
    public class TestEnvironment : IEnvironment
    {
        private IDictionary<string, string> map;

        public TestEnvironment()
        {
            map = new Dictionary<string, string>();
        }

        public string GetEnvironmentVariable(string variableName)
        {
            return map.TryGetValue(variableName, out var val) ? val : null;
        }

        public void SetEnvironmentVariable(string variableName, string value)
        {
            map[variableName] = value;
        }
    }
}

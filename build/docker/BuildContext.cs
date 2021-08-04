using System.Collections.Generic;
using Cake.Core;
using Common.Utilities;
using Docker.Utilities;

namespace Docker
{
    public class BuildContext : BuildContextBase
    {
        public bool IsDockerOnLinux { get; set; }

        public BuildCredentials? Credentials { get; set; }
        public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();


        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}

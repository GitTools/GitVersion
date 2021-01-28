using System;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class ConfigFileLocatorFactory : IConfigFileLocatorFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly IOptions<GitVersionOptions> options;

        public ConfigFileLocatorFactory(IFileSystem fileSystem, IOptions<GitVersionOptions> options)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IConfigFileLocator Create()
        {
            return string.IsNullOrWhiteSpace(options.Value.ConfigInfo.ConfigFile)
                ? new DefaultConfigFileLocator(fileSystem) as IConfigFileLocator
                : new NamedConfigFileLocator(fileSystem, options);
        }
    }
}

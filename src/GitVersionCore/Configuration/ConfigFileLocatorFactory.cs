using System;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class ConfigFileLocatorFactory : IConfigFileLocatorFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly IOptions<Arguments> options;

        public ConfigFileLocatorFactory(IFileSystem fileSystem, IOptions<Arguments> options)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IConfigFileLocator Create()
        {
            return string.IsNullOrWhiteSpace(options.Value.ConfigFile)
                ? new DefaultConfigFileLocator(fileSystem) as IConfigFileLocator
                : new NamedConfigFileLocator(fileSystem, options);
        }
    }
}

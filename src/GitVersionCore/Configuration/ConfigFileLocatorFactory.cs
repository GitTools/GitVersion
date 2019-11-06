using System;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class ConfigFileLocatorFactory : IConfigFileLocatorFactory
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IOptions<Arguments> options;

        public ConfigFileLocatorFactory(IFileSystem fileSystem, ILog log, IOptions<Arguments> options)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IConfigFileLocator Create()
        {
            return string.IsNullOrWhiteSpace(options.Value.ConfigFile)
                ? new DefaultConfigFileLocator(fileSystem, log) as IConfigFileLocator
                : new NamedConfigFileLocator(fileSystem, log, options);
        }
    }
}

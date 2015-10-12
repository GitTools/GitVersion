namespace GitVersion.Runners
{
    using GitVersion.Helpers;
    using GitVersion.Options;

    class InitRunner
    {
        public static void Run(InitOptions opts)
        {
            var fs = new FileSystem();
            ConfigurationProvider.Init(opts.Path, fs, new ConsoleAdapter());
        }
    }
}
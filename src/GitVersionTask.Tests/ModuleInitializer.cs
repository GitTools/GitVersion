using GitVersion.Helpers;
using GitVersion.Log;

namespace GitVersionTask.Tests
{
    /// <summary>
    /// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
    /// </summary>
    public static class ModuleInitializer
    {
        /// <summary>
        /// Initializes the module.
        /// </summary>
        public static void Initialize()
        {
            var log = new NullLog();
            void WriteLine(string s) => log.Info(s);

            Logger.SetLoggers(
                WriteLine,
                WriteLine,
                WriteLine,
                WriteLine);
        }

    }
}

using Cake.Frosting;

namespace GitVersion.Build
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .UseWorkingDirectory("..")
                .Run(args);
        }
    }
}

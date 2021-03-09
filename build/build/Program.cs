using Build;
using Cake.Frosting;

new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseWorkingDirectory("../..")
    .Run(args);

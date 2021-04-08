using Cake.Frosting;
using Chores;
using Common.Lifetime;

new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseWorkingDirectory("../..")
    .Run(args);

using System;
using Cake.Frosting;
using chores;
using Common.Lifetime;

new CakeHost()
    .UseContext<BuildContext>()
    .UseLifetime<BuildLifetime>()
    .UseTaskLifetime<BuildTaskLifetime>()
    .UseWorkingDirectory("../..")
    .Run(args);

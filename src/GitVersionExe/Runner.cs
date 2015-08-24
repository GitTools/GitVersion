using System;

namespace GitVersion
{
    using GitVersion.Options;

    class InitRunner
    {
        public static void Run(InitOptions opts)
        {

            throw new NotImplementedException(opts.GetType().Name);
        }
    }

    class InspectRunner
    {
        public static void Run(InspectOptions opts)
        {
            throw new NotImplementedException(opts.GetType().Name);
        }
    }
    
    class InjectBuildServerRunner
    {
        public static void Run(InjectBuildServerOptions opts)
        {
            throw new NotImplementedException(opts.GetType().Name);
        }
    }

     
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion
{
    public abstract class GitVersionModule : IGitVersionModule
    {
        public abstract void RegisterTypes(IServiceCollection services);

        protected static IEnumerable<Type> FindAllDerivedTypes<T>(Assembly assembly)
        {
            var derivedType = typeof(T);
            return assembly.GetTypes().Where(t => t != derivedType && derivedType.IsAssignableFrom(t));
        }
    }
}

## UNDERSTAND THIS

1. On .NET Core, our MSBuild tasks cannot be executed within the AssemblyLoadContext that they are loaded into my MsBuild.
This is because our MSBuild tasks have dependencies that must be loaded (like libgit2sharp etc) and we have no way to extend
the MsBuild AssemblyLoadContext that MsBuild has loaded them into, to help satisfy loading these dependencies from the nuget package folder.

2. When running on the full .NET framework this isn't a problem because in that case we can access the current AppDomain and add AssemblyResolve handlers to load these dependencies as needed.

3. On .NET Core then, we create a new AssemblyLoadContext, and load try to execute our task within it but this is tricky because:
   - If you try to load the same task Type and use it in another AssemblyloadContext (for example by creating an instance of it and calling Execute) you will
   get weird errors along the lines of "cannot cast type A to type B"even though the two types are the same.

   To workaround this issue then, the Task running within MsBuild's AssemblyLoadContext must load a Type that is not being used in it's AssemblyLoadContext. It can then interact with that type by invoking methods via reflection.
   It's the responsibility of this "reverse proxy" to execute the required task logic within our custom AssemblyLoadContext, where any dependencies that are needed will be loaded on demand from the Nuget package folder location.

4. Even though things need to be done differently on full .NET and .NET Core due to the issues above,
   we want the execution pipeline for our MsBuild tasks to look consistent irrespective of Platform.

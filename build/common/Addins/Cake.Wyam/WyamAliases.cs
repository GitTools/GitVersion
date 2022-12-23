#nullable disable
namespace Common.Addins.Cake.Wyam;

/// <summary>
/// <para>Contains functionality related to <see href="https://github.com/Wyam2/wyam">Wyam2</see>.</para>
/// <para>
/// In order to use the commands for this alias, include the following in your build.cake file to download and install from NuGet.org, or specify the ToolPath within the WyamSettings class:
/// <code>
/// #addin "nuget:?package=Cake.Wyam2"
/// #tool "nuget:?package=Wyam2"
/// </code>
/// </para>
/// </summary>
/// <remarks>
/// Make sure to remove existing references to old Cake.Wyam addin (https://www.nuget.org/packages/Wyam/).
/// </remarks>
[CakeAliasCategory("Wyam2")]
public static class WyamAliases
{
    /// <summary>
    /// Runs Wyam2 using the specified settings.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <example>
    /// <code>
    ///     Wyam();
    /// </code>
    /// </example>
    [CakeMethodAlias]
    public static void Wyam(this ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        Wyam(context, new WyamSettings());
    }

    /// <summary>
    /// Runs Wyam2 using the specified settings.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="settings">The settings.</param>
    /// <example>
    /// <code>
    ///     Wyam(new WyamSettings()
    ///     {
    ///         OutputPath = Directory("C:/Output")
    ///     });
    /// </code>
    /// </example>
    [CakeMethodAlias]
    public static void Wyam(this ICakeContext context, WyamSettings settings)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        WyamRunner runner = new WyamRunner(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        runner.Run(settings);
    }
}

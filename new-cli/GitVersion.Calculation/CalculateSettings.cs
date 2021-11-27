using GitVersion.Command;

namespace GitVersion.Calculation;

[Command("calculate", "Calculates the version object from the git history.")]
public class CalculateSettings : GitVersionSettings
{
}
using GitVersion.Command;

namespace GitVersion.Normalization
{
    [Command("normalize", "Normalizes the git repository for GitVersion calculations.")]
    public record NormalizeSettings : GitVersionSettings
    {
    }
}
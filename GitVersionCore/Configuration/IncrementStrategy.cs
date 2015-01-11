namespace GitVersion
{
    public enum IncrementStrategy
    {
        None,
        Major,
        Minor,
        Patch,
        /// <summary>
        /// Uses the increment strategy from the branch the current branch was branched from
        /// </summary>
        Inherit 
    }
}
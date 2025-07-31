namespace GitVersion.Formatting
{
    internal interface IInputSanitizer
    {
        string SanitizeEnvVarName(string name);

        string SanitizeFormat(string format);

        string SanitizeMemberName(string memberName);
    }
}

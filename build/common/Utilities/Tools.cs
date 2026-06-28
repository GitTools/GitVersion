namespace Common.Utilities;

public static class Tools
{
    public const string CodecovUploaderCmd = "CodecovUploader";

    public static readonly Dictionary<string, string> Versions = new()
    {
        { CodecovUploaderCmd, "0.8.0" }
    };
}

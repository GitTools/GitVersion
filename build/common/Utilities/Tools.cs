namespace Common.Utilities;

public static class Tools
{
    public const string CodecovUploaderCmd = "CodecovUploader";

    public static readonly IReadOnlyDictionary<string, string> Versions = new Dictionary<string, string>()
    {
        { CodecovUploaderCmd, "0.8.0" }
    };
}

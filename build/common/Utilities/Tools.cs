namespace Common.Utilities;

public class Tools
{
    public const string CodecovUploaderCmd = "CodecovUploader";

    public static readonly Dictionary<string, string> Versions = new()
    {
        { CodecovUploaderCmd, "0.7.1" },
    };
}

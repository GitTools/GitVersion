namespace GitVersionTask
{
    internal static class SupportedLanguageConstants
    {
        internal const string VBDOTNET = "VB";
        internal const string FSHARP = "F#";
        internal const string CSHARP = "C#";
        internal const string CPLUSPLUS = "C++";

        internal static readonly string[] SUPPORTED_LANGUAGES =
        {
            VBDOTNET,
            CSHARP,
            FSHARP,
            CPLUSPLUS,
        };

        internal const string FILEEXTENSION_VBDOTNET = "vb";
        internal const string FILEEXTENSION_CSHARP = "cs";
        internal const string FILEEXTENSION_FSHARP = "fs";
        internal const string FILEEXTENSION_CPLUSPLUS = "h";

        internal static readonly string[] SUPPORTED_LANGUAGE_FILEEXTENSIONS =
        {
            FILEEXTENSION_VBDOTNET,
            FILEEXTENSION_CSHARP,
            FILEEXTENSION_FSHARP,
            FILEEXTENSION_CPLUSPLUS,
        };
    }
}
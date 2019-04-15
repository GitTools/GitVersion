namespace GitVersionTask
{
    using System;

    public class TaskUtils
    {
        public static string GetFileExtension(string language)
        {
            switch(language)
            {
                case "C#":
                    return "cs";

                case "F#":
                    return "fs";

                case "VB":
                    return "vb";

                default:
                    throw new Exception($"Unknown language detected: '{language}'");
            }
        }
    }
}
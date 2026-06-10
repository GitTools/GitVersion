namespace GitVersion;

/// <summary>Describes the working directory and file name details for writing version output to a file.</summary>
public sealed record FileWriteInfo(string WorkingDirectory, string FileName, string FileExtension);

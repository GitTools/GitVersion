namespace GitVersion.Output.WixUpdater;

internal readonly record struct WixVersionContext(string WorkingDirectory) : IConverterContext;

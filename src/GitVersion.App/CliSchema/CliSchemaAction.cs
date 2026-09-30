using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitVersion;

// Parsing uses this informational marker to suppress value validation. Output is handled
// by ArgumentParser so an explicitly supplied schema option also takes precedence over help.
internal sealed class CliSchemaAction : SynchronousCommandLineAction
{
    public override bool ClearsParseErrors => true;
    public override int Invoke(ParseResult parseResult) => 0;
}

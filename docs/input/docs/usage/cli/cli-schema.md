---
Order: 25
Title: CLI schema
Description: Discover GitVersion commands and options using OpenCLI
---

GitVersion 7 exposes its command tree as [OpenCLI][opencli] 0.1 JSON:

```shell
gitversion --cli-schema
gitversion config --cli-schema
gitversion config migrate --cli-schema
```

Each invocation writes the same complete document to stdout, followed by a newline,
and exits successfully. The document describes commands, aliases, options,
positional arguments, value arity, and accepted values where available. The
application version comes from the executable's assembly metadata.

The option is available with the default v7 argument parser. The temporary
`GITVERSION_ARGUMENT_PARSER_VERSION=v6` fallback does not support it. Configuration
version and Git backend selectors are independent of schema export.

Export works outside a Git repository. It does not load configuration, calculate
a version, migrate configuration, or write files. An explicitly parsed
`--cli-schema` takes precedence over `--help` and `--version` in either order.
Unrecognized options and extra positional arguments still produce diagnostics.
After `--`, or when consumed as another option's string value, the token does not
request schema export. Use the bare `--cli-schema` flag without a value.

The schema is derived from the active parser's command definitions. Inherited
options remain declared on their parent command with `recursive: true`; child
commands list their local options. For unbounded value arity, `maximum` is omitted.
Enum value lists show canonical names; parser support for case-insensitive names
and numeric enum values is described in `gitversion.*` metadata. These names do
not introduce new parser restrictions.

[opencli]: https://opencli.org/

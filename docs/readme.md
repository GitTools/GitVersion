# GitVersion Documentation

This is the directory in which the [GitVersion documentation hosted on GitHub
Pages][gitversion.net] resides.

## Contributing

Improvements to the documentation is highly welcomed and is as easy
as finding the `.md` file you want to change and editing it directly within
GitHub's web interface.

If you want to do more elaborate changes, we would appreciate if you could test
the documentation locally before submitting a pull request. This involves
[forking][forking] this repository and then serving up the documentation locally
on your machine, clicking around in it to ensure that everything works as
expected.

## Documentation organization

The documentation is grouped into Getting started, Guides, Concepts, Reference,
Troubleshooting, and Upgrading. The curated sidebar is defined in
`input/Shared/Sidebar/_ChildPages.cshtml`. It groups existing pages by task
without changing their public URLs. When adding a page, update its landing page
and the relevant sidebar group. Keep contributor and community links in the footer.

The current main-branch documentation describes v7 development. Match command
examples and output-variable names to that version, and keep legacy arguments
in migration guidance.

Configuration prose is authored in
`input/docs/reference/mdsource/configuration.source.md`. Do not edit
`input/docs/reference/configuration.md` directly. Follow
`.github/workflows/mkdocs.yml`: run `mdsnippets --write-header false` from
`docs/input` after editing the source. Include both the source and regenerated
Markdown in a documentation change. The topical configuration pages link to the
complete reference so existing setting anchors remain stable.

Preserve existing URLs when reorganizing. If a page must move, retain its old
URL with Wyam's `RedirectFrom` metadata and preserve or map its fragment anchors.

## Serving the documentation locally

The preview builds Current, 6.8, and 5.12 with the same templates, styles, and
homepage. Choose an edition in the navigation. Released prose comes from its
tagged source; API signatures come from the matching published assemblies, with
package XML comments supplemented from that source. No rendered `gh-pages` files
are used as input.

Current API signatures come from freshly built `GitVersion.App` and
`GitVersion.MsBuild` projects, including their referenced assemblies. BuildDocs
creates this assembly bundle itself, locally and in GitHub Actions; no existing
application binaries are required. Every edition is checked against its public
type inventory, and missing API pages or local links fail the documentation build.

`versions.json` defines the editions. At each build, 6.8 and 5.12 resolve to the
latest stable patch in that minor line. A build uses one resolved patch throughout.
Exact inputs are recorded in `artifacts/docs/inputs.json`; the site exposes a
portable `build-inputs.json` receipt. Package archives and source snapshots are
cached under `artifacts/docs/cache`, verified by checksum, and downloaded again
only for new inputs or an empty cache. Metadata lookup still needs network access;
an unavailable feed fails the build instead of silently selecting an older patch.

Presentation is owned by this checkout: `input/_*.cshtml`, `input/Shared`,
`input/assets`, `input/index.cshtml`, and `theme`. Release Markdown and section
landing pages keep their original content. Shared homepage commands adjust to
the selected edition. To add an edition, add its major.minor ID, matching tool
framework, and unique URL prefix to the manifest.

PreviewDocs watches shared documentation inputs and rebuilds all editions. Refresh
the browser after a successful rebuild. A failed rebuild keeps the last successful
site available. Build task changes require restarting the preview. The local
server listens at `http://localhost:5080`; `/6.8/` and `/5.12/` are the release
editions, and `/5.12.0/` URLs redirect to `/5.12/`.

Without PowerShell, use `dotnet run --project build/docs/docs.csproj -- --target=PreviewDocs`.
Run the focused input-selection checks with `dotnet run --file docs/scripts/check-docs-inputs.cs`.

To serve up the documentation locally, you need to run the following
commands:

```shell
./build.ps1 -Stage build -Target BuildPrepare
./build.ps1 -Stage build -Target Build
./build.ps1 -Stage docs -Target PreviewDocs
```

### On Windows

On Windows, you need to run the following commands in a PowerShell
terminal:

```shell
./build.ps1 -Stage build -Target BuildPrepare
./build.ps1 -Stage build -Target Build
./build.ps1 -Stage docs -Target PreviewDocs
```

### On Unix

First you need to [install PowerShell on macOS][ps-mac] or [Linux][ps-linux],
then execute the following commands:

```shell
./build.ps1 -Stage build -Target BuildPrepare
./build.ps1 -Stage build -Target Build
./build.ps1 -Stage docs -Target PreviewDocs
```

After pressing enter, the documentation will be generated and then served under
a local web server. Information about the URL that can be used to view the docs
will be shown in the output. Copy/paste this URL into a browser window.

[gitversion.net]: https://gitversion.net/

[forking]: https://guides.github.com/activities/forking/

[ps-mac]: https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-7.1

[ps-linux]: https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-7.1

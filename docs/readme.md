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

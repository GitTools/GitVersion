---
Title: Output and formatting
Description: Assembly versions, custom formats, and build numbers.
---
These settings control additional representations of the calculated version or its integration into builds. Choose an [output variable](/docs/reference/variables) before defining a custom format.

| Setting | Detailed reference |
| --- | --- |
| `assembly-versioning-format` | [assembly-versioning-format](/docs/reference/configuration#assembly-versioning-format) |
| `assembly-versioning-scheme` | [assembly-versioning-scheme](/docs/reference/configuration#assembly-versioning-scheme) |
| `assembly-file-versioning-format` | [assembly-file-versioning-format](/docs/reference/configuration#assembly-file-versioning-format) |
| `assembly-file-versioning-scheme` | [assembly-file-versioning-scheme](/docs/reference/configuration#assembly-file-versioning-scheme) |
| `assembly-informational-format` | [assembly-informational-format](/docs/reference/configuration#assembly-informational-format) |
| `custom-version-format` | [custom-version-format](/docs/reference/configuration#custom-version-format) |
| `commit-date-format` | [commit-date-format](/docs/reference/configuration#commit-date-format) |
| `pre-release-weight` | [pre-release-weight](/docs/reference/configuration#pre-release-weight) |
| `tag-pre-release-weight` | [tag-pre-release-weight](/docs/reference/configuration#tag-pre-release-weight) |
| `update-build-number` | [update-build-number](/docs/reference/configuration#update-build-number) |

Defaults can depend on the selected workflow and branch. The linked reference supplies accepted values, scope, and examples. Inspect the effective result with `dotnet-gitversion --show-config`.

[All settings](/docs/reference/configuration-topics#alphabetical-settings-index) · [Complete configuration reference](/docs/reference/configuration)


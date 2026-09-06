---
Title: Branch configuration
Description: Matching, inheritance, and merge behavior.
---
Branch settings apply through the effective configuration. A label changes the calculated semantic version. Use [workflow examples](/docs/learn/branching-strategies) to understand the effect of changes.

| Setting | Detailed reference |
| --- | --- |
| `branches` | [branches](/docs/reference/configuration#branches) |
| `regex` | [regex](/docs/reference/configuration#regex) |
| `source-branches` | [source-branches](/docs/reference/configuration#source-branches) |
| `is-source-branch-for` | [is-source-branch-for](/docs/reference/configuration#is-source-branch-for) |
| `increment` | [increment](/docs/reference/configuration#increment) |
| `label` | [label](/docs/reference/configuration#label) |
| `label-number-pattern` | [label-number-pattern](/docs/reference/configuration#label-number-pattern) |
| `mode` | [mode](/docs/reference/configuration#mode) |
| `is-main-branch` | [is-main-branch](/docs/reference/configuration#is-main-branch) |
| `is-release-branch` | [is-release-branch](/docs/reference/configuration#is-release-branch) |
| `prevent-increment-of-merged-branch` | [prevent-increment-of-merged-branch](/docs/reference/configuration#prevent-increment-of-merged-branch) |
| `prevent-increment-when-branch-merged` | [prevent-increment-when-branch-merged](/docs/reference/configuration#prevent-increment-when-branch-merged) |
| `prevent-increment-when-current-commit-tagged` | [prevent-increment-when-current-commit-tagged](/docs/reference/configuration#prevent-increment-when-current-commit-tagged) |
| `track-merge-message` | [track-merge-message](/docs/reference/configuration#track-merge-message) |
| `track-merge-target` | [track-merge-target](/docs/reference/configuration#track-merge-target) |
| `tracks-release-branches` | [tracks-release-branches](/docs/reference/configuration#tracks-release-branches) |

Defaults can depend on the selected workflow and branch. The linked reference supplies accepted values, scope, and examples. Inspect the effective result with `dotnet-gitversion --show-config`.

[All settings](/docs/reference/configuration-topics#alphabetical-settings-index) · [Complete configuration reference](/docs/reference/configuration)


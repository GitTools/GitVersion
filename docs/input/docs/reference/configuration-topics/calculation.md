---
Title: Version calculation
Description: Sources, increments, and history filters.
---
These settings affect which version is selected and how it is incremented. Review them with the [calculation overview][calculation-overview].

| Setting | Detailed reference |
| --- | --- |
| `strategies` | [strategies][strategies] |
| `next-version` | [next-version][next-version] |
| `tag-prefix` | [tag-prefix][tag-prefix] |
| `version-in-branch-pattern` | [version-in-branch-pattern][version-in-branch-pattern] |
| `semantic-version-format` | [semantic-version-format][semantic-version-format] |
| `commit-message-incrementing` | [commit-message-incrementing][commit-message-incrementing] |
| `major-version-bump-message` | [major-version-bump-message][major-version-bump-message] |
| `minor-version-bump-message` | [minor-version-bump-message][minor-version-bump-message] |
| `patch-version-bump-message` | [patch-version-bump-message][patch-version-bump-message] |
| `no-bump-message` | [no-bump-message][no-bump-message] |
| `version-bump-reset-message` | [version-bump-reset-message][version-bump-reset-message] |
| `merge-message-formats` | [merge-message-formats][merge-message-formats] |
| `ignore` | [ignore][ignore] |

Defaults can depend on the selected workflow and branch. The linked reference supplies accepted values, scope, and examples. Inspect the effective result with `dotnet-gitversion --show-config`.

[All settings][all-settings] · [Complete configuration reference][complete-configuration-reference]

[calculation-overview]: /docs/learn/how-it-works

[strategies]: /docs/reference/configuration#strategies

[next-version]: /docs/reference/configuration#next-version

[tag-prefix]: /docs/reference/configuration#tag-prefix

[version-in-branch-pattern]: /docs/reference/configuration#version-in-branch-pattern

[semantic-version-format]: /docs/reference/configuration#semantic-version-format

[commit-message-incrementing]: /docs/reference/configuration#commit-message-incrementing

[major-version-bump-message]: /docs/reference/configuration#major-version-bump-message

[minor-version-bump-message]: /docs/reference/configuration#minor-version-bump-message

[patch-version-bump-message]: /docs/reference/configuration#patch-version-bump-message

[no-bump-message]: /docs/reference/configuration#no-bump-message

[version-bump-reset-message]: /docs/reference/configuration#version-bump-reset-message

[merge-message-formats]: /docs/reference/configuration#merge-message-formats

[ignore]: /docs/reference/configuration#ignore

[all-settings]: /docs/reference/configuration-topics#alphabetical-settings-index

[complete-configuration-reference]: /docs/reference/configuration

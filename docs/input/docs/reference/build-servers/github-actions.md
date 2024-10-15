---
Order: 70
Title: GitHub Actions
Description: |
    Details on the GitHub Actions Workflow support in GitVersion
---

## Installation and usage

For GitHub Actions you can install the action from [GitTools Bundle](https://github.com/marketplace/actions/gittools).

:::{.alert .alert-danger}
**Important**

You must disable shallow fetch by setting `fetch-depth: 0` in your `checkout` step;
without it, GitHub Actions might perform a shallow clone, which will cause GitVersion to display an error message.
:::

More information can be found at [gittools/actions](https://github.com/GitTools/actions/blob/main/docs/examples/github/gitversion/index.md).

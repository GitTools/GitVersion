---
Order: 20
Title: Azure DevOps
Description: |
    Details on the Azure DevOps Build Pipeline support in GitVersion
RedirectFrom: docs/build-server-support/build-server/azure-devops
---

## Installation and usage

For Azure DevOps Services or Azure DevOps Server you can install the [GitTools Bundle][gittools-bundle].

:::{.alert .alert-danger}
**Important**

You must disable shallow fetch, either in the pipeline settings UI or by setting `fetchDepth: 0` in your `checkout` step;
without it, Azure DevOps Pipelines will perform a shallow clone, which will cause GitVersion to display an error message.
See [the Azure DevOps documentation][the-azure-devops-documentation] for more information.
:::

More information can be found at [gittools/actions][gittools-actions].

[gittools-bundle]: https://marketplace.visualstudio.com/items?itemName=gittools.gittools

[the-azure-devops-documentation]: https://learn.microsoft.com/en-us/azure/devops/pipelines/yaml-schema/steps-checkout?view=azure-pipelines#shallow-fetch

[gittools-actions]: https://github.com/GitTools/actions/blob/main/docs/examples/azure/gitversion/index.md

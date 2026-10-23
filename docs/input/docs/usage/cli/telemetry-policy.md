---
Order: 71
Title: Telemetry policy and collected data
Description: GitVersion CLI telemetry purpose, exact data fields, opt-outs, first-run notice and retention
---

This policy describes version 2 of GitVersion's optional CLI usage telemetry. Maintainers use sampled
invocation and option-usage aggregates to help prioritize GitVersion development. Collection is disabled
until maintainers configure and enable a campaign. Only the established POSIX CLI supports collection;
the legacy argument parser, experimental `new-cli/` and native MSBuild tasks do not collect.

## Your controls and the first-run notice

Disable telemetry with `--telemetry-opt-out`, `DO_NOT_TRACK=1`, or `GITVERSION_TELEMETRY_OPTOUT=1`.
Both environment variables also accept `true`, ignoring case. Opt-outs take effect before the notice,
notice-state access, sampling or networking. Help, version, CLI-schema and configuration operations do
not collect events or create notice state. Disabled, unconfigured, expired and zero-sampling campaigns
also perform no notice-state I/O or transport.

On the first eligible direct version-calculation run with a positive direct sampling probability,
GitVersion writes a brief notice to **stderr** linking
to this policy at `https://gitversion.net/docs/usage/cli/telemetry-policy`. For direct runs the notice appears
before sampling, including when that invocation is not selected. For recognized CI agents, unknown non-default
build agents, and CLI calls marked as MSBuild or GitTools/actions invocations, sampling happens first: unselected runs neither
write a notice nor access its marker. A selected automated run still discloses before sending an event.
JSON and single-variable stdout remain unchanged. If the notice cannot be written, no event is sent.
Selected automated runs can write to stderr; pipelines that treat stderr as failure should use an opt-out above.

After successful disclosure, GitVersion tries to create an empty `GitVersion/telemetry-policy-v2` marker
under the platform's .NET `LocalApplicationData` directory. It records only that this policy version has
been shown; it contains no identifier or event data and is never uploaded. Later invocations sharing that
directory skip the notice. Removing the marker causes the next eligible direct run or selected automated run
to show it again. A materially changed policy uses a new marker version. Read-only/unavailable storage can cause repeat notices, and
concurrent first runs can each show the notice; neither condition interrupts version calculation.

## Exactly what is sent

An invocation can send at most one explicit `gitversion_cli_invocation` event. The complete event envelope is:

| Field | Value and purpose |
| --- | --- |
| `api_key` | Public PostHog project write key, routing the event to the maintainers' project |
| `event` | The fixed name `gitversion_cli_invocation` |
| `distinct_id` | A fresh random UUID for this event; never reused to identify a person, machine or repository |
| `properties` | Only the fields listed below |

| Property | Allowed data |
| --- | --- |
| `schema_version` | `2` |
| `os` | `windows`, `linux`, `macos`, or `other` |
| `os_distribution` | Allowlisted Linux distro ID, `macos`, `windows`, `other`, or `unknown`; see below |
| `os_version` | Numeric OS release, or `unknown`; Windows uses major.minor.build |
| `container` | `docker`, `other`, `none`, or `unknown`; best-effort runtime detection |
| `gitversion_version` | Installed GitVersion CLI assembly release, excluding branch/SHA build metadata; `unknown` for unsupported versions |
| `architecture` | `x64`, `arm64`, `x86`, `arm`, or `other` |
| `ci` | Existing build-agent classification: one of the fixed provider labels below; no environment-variable values |
| `invocation` | `direct`, `msbuild`, or `gittools-actions`; fixed child-process wrapper markers, independent of CI/container |
| `options` | Canonical names indicating option presence, and bounded output modes listed below |
| `sampling_probability` | Probability of selecting this invocation; at most `0.01` direct or `0.0001` automated |
| `$process_person_profile` | Always `false`, requesting personless processing |
| `$geoip_disable` | Always `true`, requesting no GeoIP enrichment |
| `$ip` | Always the sentinel `0.0.0.0`; see provider processing below |

### How runtime and caller fields are derived

These dimensions are independent: an MSBuild invocation can also run on GitHub Actions inside Docker.
They describe the observed invocation; they do not identify a person, repository, or unique installation.

| Field | Detection and fallback |
| --- | --- |
| `os`, `architecture` | .NET's current OS family and **process** architecture, mapped to the fixed labels above; architecture is not necessarily the host hardware architecture when emulated |
| `ci` | GitVersion's existing build-agent resolver; its local/default agent maps to `none`, and an unmapped non-default agent maps to `other` |
| `invocation` | Trimmed, case-insensitive `GITVERSION_INTERNAL_CALLER`: `GitVersion.MsBuild` or `gitversion-msbuild` maps to `msbuild`; `gittools-actions` maps to `gittools-actions`; all other or absent values map to `direct` |
| `container` | Exact `GITVERSION_CONTAINER_KIND=docker`, set by the official image, or `/.dockerenv` on Linux maps to `docker`; otherwise `DOTNET_RUNNING_IN_CONTAINER=1` or case-insensitive `true` maps to `other`; no recognized hint maps to `none`, and detection errors map to `unknown` |
| Linux `os_distribution`, `os_version` | Only `ID` and `VERSION_ID` from `/etc/os-release`; `/usr/lib/os-release` is used only when the first file is absent, never merged or used after a read failure. No shell expansion is performed |
| macOS `os_distribution`, `os_version` | Fixed `macos` and numeric `ProductVersion` from `/System/Library/CoreServices/SystemVersion.plist`; external XML resources are not loaded; Darwin kernel/build labels are excluded |
| Windows `os_distribution`, `os_version` | Fixed `windows` and .NET's OS version as major.minor.build; revision and marketing labels such as Windows 11 or Server edition are excluded |
| `gitversion_version` | The CLI assembly's informational version, with everything from `+` onward removed; only the release/prerelease syntax described below is accepted, otherwise `unknown` |

Runtime files are read only after eligibility, sampling and disclosure permit an event. Files exceeding
8,192 characters are rejected using a bounded read with one extra character to detect overflow;
oversized or unreadable data is not transmitted. OS release strings allow
at most four numeric components, each up to eight digits, with a total limit of 32 characters.
Pretty names, codenames, arbitrary environment values, file contents and error messages are never copied
to event properties.

The existing GitVersion build-agent resolver supplies `ci`. Its fixed labels are `appveyor`, `azure`,
`bitbucket`, `buildkite`, `codebuild`, `continua`, `drone`, `envrun`, `github`, `gitlab`, `jenkins`, `myget`,
`space`, `teamcity` and `travis`. Local/default builds use `none`; unmapped non-default agent types use
`other` and the automated sampling rate. Telemetry does not call agent branch/tag or output-writing methods.

Linux distro IDs are limited to `debian`, `ubuntu`, `alpine`, `fedora`, `rhel`, `centos`, `rocky`,
`almalinux`, `amzn`, `opensuse-leap`, `opensuse-tumbleweed`, `sles`, `arch`, `gentoo`, `nixos`, `photon`,
`mariner` and `azurelinux`. Only numeric `VERSION_ID` values are accepted; custom distro IDs use `other`
and do not include a version. macOS uses `ProductVersion`; Windows uses its numeric OS build rather than
a marketing name. Missing, malformed, non-numeric or oversized release data uses `unknown`.
Inside a container these fields describe its userland, not the host OS. Docker uses the official image's
fixed marker or the conventional `/.dockerenv` hint; the generic .NET container hint uses `other`.
Detection is best effort and does not inspect cgroup IDs, hostnames, image tags or container IDs.

The installed tool version accepts a numeric major.minor.patch release and optional public
`alpha`, `beta`, `rc`, `preview`, `dev` or `ci` prerelease channel with numeric components.
Custom branch-based prerelease labels use `unknown`. This is the version of GitVersion itself,
not the version calculated for the user's repository.

The MSBuild package marks only its CLI child process. In-process MSBuild tasks do not send events.
GitTools/actions recognition requires the wrapper to set `GITVERSION_INTERNAL_CALLER=gittools-actions`
for the CLI child; older wrappers without the marker remain `direct`. GitHub runner detection alone
does not identify GitTools/actions usage. Arbitrary marker values are never transmitted.

For example, a marked MSBuild CLI child in an Alpine Docker container on a GitHub runner can report
`ci=github`, `invocation=msbuild`, `container=docker`, `os=linux`, `os_distribution=alpine` and
`os_version=3.22`. A direct CLI call on that same runner uses `invocation=direct`.
Here `direct` means no recognized wrapper marker; it does not prove an interactive human call.
Likewise, `container=none` means no recognized container hint, not proof that containerization is absent.
Markers and hints can be omitted, inherited or spoofed; these fields are best-effort usage categories,
not authenticated provenance or reliable measurements of every wrapper/container installation.

Schema-1 events did not include `container`, `os_distribution`, `os_version` or `gitversion_version`.
The dashboard labels those absent values as unknown rather than inferring or backfilling them.

### How CLI options are represented

The `options` list can contain only these names:

```text
--no-cache, --no-fetch, --no-normalize, --allow-shallow, --diagnose,
--config, --override-config, --show-variable, --format,
--url, --branch, --commit, --dynamic-repo-location, --log-file, --output-file,
--update-assembly-info, --update-project-files, --ensure-assembly-info,
--update-wix-version-file,
--output=json, --output=buildserver, --output=file, --output=dotenv
```

Option values are discarded except for those four output-mode labels. For example, `--branch private-name`
contributes only `--branch`; `--format` contributes no format text. Default output modes can also be listed.
The client sends no event timestamp; PostHog assigns a receipt time.

## What is excluded

The payload excludes paths, repository names/URLs, credentials, branch names, commit IDs, configuration
contents or override values, format strings, environment contents, calculated versions, user names and
stable machine/user/repository identifiers. It does not send the raw command line.

An event is attempted after successful argument parsing and before version calculation. It does not
include the final exit code, success/failure outcome, calculation duration, error text or stack traces.
A later calculation failure can therefore have an invocation event; an argument-parsing failure does not.
Event delivery must not be interpreted as evidence that the CLI calculation succeeded.

There are no user profiles, stable cross-event identities, session replay, autocapture, automatic exception
capture, dependency capture, browser cookies, or disk event queues. The first-run marker is separate from
the event payload. Invocation counts must not be presented as unique-user counts.

## Sampling, collection window and reliability

Direct invocations are independently sampled at up to 1%; recognized automation and CLI calls marked as
MSBuild or GitTools/actions invocations are sampled at up to 0.01%. Maintainers can reduce either probability to zero or disable
the campaign. Transport/ingestion loss, opt-outs, quota drops and forged events affect aggregate estimates.
There is no global client-side volume guarantee or authentication of genuine clients using a public write key.

Only `sampling_probability` is transmitted. The dashboard derives an inverse-probability weight as
`1 / sampling_probability` when the probability is positive; it is not a separate event property.
For example, probabilities of `0.01` and `0.0001` produce calculated weights of `100` and `10000`.
Controlled test events must be excluded from adoption estimates. Historical synthetic events may still
contain the former `sampling_weight` property; the dashboard ignores it and derives the value from probability.

Cake automatically embeds the UTC build date only for official tagged releases and prereleases. Collection is allowed
only on dates `start <= today < start + three calendar months` for that assembly. All packages from one Cake
build share that date. Each official release or prerelease build, including patches and later tag rebuilds, gets its
own window. Cake gives internal prerelease, development, local, fork and pull-request builds no key or
collection date, so they collect nothing. Ordinary direct `dotnet` builds also default to disabled with no
date. A controlled local build can explicitly supply both `GitVersionTelemetryProjectKey` and a valid
`GitVersionTelemetryCampaignStart`; these become public key and date assembly metadata and still require
all other campaign checks to pass. Official release builds require no manually chosen start date.

Sending is best effort: one awaited HTTPS request, a 300 ms transport deadline for direct runs or 1 second
for selected automated runs, no retry and no response-body read. The deadline applies to the HTTP request,
not the total reporting work before version calculation. It does not bound earlier disclosure, sampling or
runtime-context evaluation. Caller cancellation can end the request sooner. Offline or rejected events are lost.
Telemetry errors do not alter calculation output or exit codes.

## PostHog processing, access and retention

The selected destination is **PostHog Cloud EU**, `eu.i.posthog.com`. The provider and its network
infrastructure receive the connection's source IP even though the event contains a zero IP sentinel.
The payload is minimized; this does not make the network connection anonymous. Provider transport/security
processing is governed by [PostHog's privacy documentation][posthog-privacy].

Before enabling collection, maintainers must verify a dedicated no-card **Free** project, restrict project
access to authorized maintainers, enable **Discard client IP data**, disable the GeoIP transformation, and
ensure project transformations do not add person processing or additional identifying data. IP discard
alone does not prevent transformations from using the IP before it is discarded. Collection remains disabled
while no project write key is configured. Recheck provider settings before activation.

Official tagged releases and prereleases require the repository's GitHub Actions variable
`GITVERSION_TELEMETRY_PROJECT_KEY` when built. Cake forwards the public write key from the build environment
and embeds it as `TelemetryProjectKey` assembly metadata, together with the release build date.
There is no separate enablement flag. Missing, invalid or duplicate key metadata disables collection.
Other Cake builds omit both key and date, even when the environment contains the key.
No runtime environment variable enables collection or replaces the embedded key. Changing the GitHub
variable affects future builds only and cannot remotely stop or start already distributed releases.

PostHog Free allows one million analytics events per month and permanently drops excess events rather
than billing them. The project must remain on Free with no payment method or automatic paid overages.
See [PostHog pricing][posthog-pricing] and [IP storage controls][posthog-ip-storage].

The allowance is shared across the organization's projects and resets with its billing cycle. On
October 22, 2026, the EU project's billing page showed the Free plan with an invitation to add a card,
a Product Analytics free allowance and matching Free-plan ingestion cap of one million events, and
26 events used in the October 21–November 20 cycle. The Free UI labels these event counts "Free tier limit"
and "Billing limit"; this is not verification of a separately configured monetary cap. Paid-plan billing
limits are dollar amounts and require separate verification if the organization changes plans.
Maintainers must recheck these settings before activation. PostHog documents
that events beyond the billing limit are permanently dropped; legitimate events are also lost if abuse
exhausts the allowance. See [billing limits][posthog-billing-limits] and
[organization-wide usage][posthog-billing-questions].

## Public key and data reliability

The CLI contains a public project write key that identifies the PostHog destination. It is not proof that
an event came from GitVersion. Keeping that key in a build secret would not keep it private after it is
embedded in a distributed binary or sent in a request. GitVersion ships no personal/read/admin key.

Someone can use the public key to submit fabricated events, distort usage estimates or consume the
ingestion allowance. Client-side opt-outs, sampling, the disabled default and the three-month collection
window do not restrict someone sending requests directly to PostHog. GitVersion sends directly to PostHog
and operates no ingestion gateway or client-authentication service. Maintainers inspect ingestion quality,
but plausible forged events cannot reliably be distinguished from real invocations. Dashboard results,
including sampling-weighted estimates, are indicative usage signals rather than authenticated counts.

## Stored-event retention

The three-month collection window stops **new events**. It does not automatically delete stored events.
PostHog Free advertises one-year analytics retention. Any shorter deletion period requires a separate,
verified provider-side deletion policy; GitVersion ships no read/admin or deletion credentials.

Implementation, activation and statistical details are in [Telemetry][telemetry].

[telemetry]: /docs/usage/cli/telemetry

[posthog-privacy]: https://posthog.com/docs/privacy

[posthog-pricing]: https://posthog.com/pricing

[posthog-billing-limits]: https://posthog.com/docs/billing/limits-alerts

[posthog-billing-questions]: https://posthog.com/docs/billing/common-questions

[posthog-ip-storage]: https://posthog.com/docs/privacy/data-storage

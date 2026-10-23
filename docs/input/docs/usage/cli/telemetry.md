---
Order: 70
Title: Telemetry
Description: Optional sampled CLI usage collection, controls and privacy
---

Collection is **disabled by default at build time**. Official release and prerelease builds require a public
EU project write key in the GitHub Actions variable `GITVERSION_TELEMETRY_PROJECT_KEY`.
Keep this repository variable unset until the activation prerequisites below are met.
The implementation supports the established `src/` CLI with the v7 POSIX argument parser only.
The legacy parser, experimental `new-cli/` executable and in-process `GitVersion.MsBuild` tasks do not collect telemetry.

The public [Telemetry policy and collected data][telemetry-policy] documents the exact event schema,
first-run disclosure, local notice marker, opt-outs and provider retention.

## Opt out

Use `--telemetry-opt-out`. Alternatively set
`DO_NOT_TRACK=1` or `GITVERSION_TELEMETRY_OPTOUT=1`. Both variables also accept `true`, ignoring case.
These controls are checked before sampling, disclosure or networking. They do not create local files.
Help, version, CLI schema, configuration get/validate/migrate and show-config operations do not collect events.

## What an enabled campaign collects

One explicitly constructed `gitversion_cli_invocation` event can be sent per invocation. It contains:

* Schema version `2`, operating system family, process architecture, bounded distro/release and container context.
* Installed GitVersion tool version, excluding branch/SHA build metadata and custom prerelease labels.
* A bounded label from the existing build-agent resolver and invocation label (`direct`, `msbuild`, `gittools-actions`).
* Canonical option names indicating presence. Free-form values are discarded. Output modes are bounded enum values.
* Sampling probability. The dashboard calculates its inverse; no sampling weight is transmitted.
* A fresh random event-scoped `distinct_id`, `$process_person_profile=false`, `$geoip_disable=true`, and `$ip=0.0.0.0`.

Paths, repository URLs, credentials, branch names, commit IDs, configuration/format strings,
environment values, calculated versions, user names and machine identifiers are excluded.
There is no stable identity, user profile, session replay, autocapture, exception capture or dependency capture.
No PostHog SDK is used. PostHog assigns receipt time; the client sends no timestamp.

On the first eligible direct run with a positive direct sampling probability, a disclosure linking to
the policy goes to stderr before sampling.
CI, MSBuild and marked GitTools/actions invocations sample first: unselected runs write no notice and never access its marker.
Selected automated runs disclose before sending, so pipelines treating stderr as failure should opt out.
An empty `GitVersion/telemetry-policy-v2` file under .NET's `LocalApplicationData` remembers successful disclosure.
Opted-out, disabled, unconfigured and excluded commands never access this state. State failures can repeat
the notice; concurrent first runs can each disclose. Stdout and the calculation's exit code are preserved.

Runtime release files are read only for selected events after disclosure. Inside Docker, distro/release
describes the image userland. macOS uses its product release and Windows its numeric OS build.
See the policy for exact allowlists and unknown values. The MSBuild package marks its CLI child;
GitTools/actions identification requires a compatible wrapper to set the documented child-process marker.
The policy's [runtime and caller detection table][telemetry-detection] lists every source and fallback.
`direct` means no recognized wrapper marker, and `container=none` means no recognized container hint;
neither proves how the process was launched. The markers can be absent or spoofed.
The event records an invocation before calculation, without its final exit code, success/failure,
duration or error details. Parsing failures produce no event. Older schema-1 events lack the new runtime
and tool-version fields; the dashboard displays them as unknown.
There is no event queue, cookie or retry. A sampled invocation awaits a single HTTP request before
version calculation starts. The request deadline is 300 ms for direct runs and 1 second for selected
automated runs, allowing time for a cold connection to the EU endpoint from hosted CI agents.
The deadline applies to the HTTP request, not the total reporting work before calculation. It does not
bound earlier disclosure, sampling or runtime-context evaluation. Caller cancellation can end the request sooner.
Offline transport, rejected responses and disclosure failures are ignored. Unsent events are lost.
Read-only homes and concurrent marker creation are harmless; the marker contains no identifier or event data.

## Sampling and interpretation

Direct invocations use independent Bernoulli sampling at probability `0.01` (1%). Recognized CI or
`GITVERSION_INTERNAL_CALLER=GitVersion.MsBuild` invocations use `0.0001` (0.01%). The marker also accepts
`gitversion-msbuild` (case-insensitive, surrounding whitespace ignored). The MSBuild package sets it on its
CLI child. `GITVERSION_INTERNAL_CALLER=gittools-actions` uses the same automated rate when supplied by a
compatible Actions wrapper. Native MSBuild tasks run in process without this reporter and produce no events.
The existing `ICurrentBuildAgent` service supplies the CI classification; telemetry does not maintain a
second environment-variable detector. Agent resolution is deferred until after opt-out/campaign checks.
Supported labels are `appveyor`, `azure`, `bitbucket`, `buildkite`, `codebuild`, `continua`, `drone`, `envrun`,
`github`, `gitlab`, `jenkins`, `myget`, `space`, `teamcity` and `travis`. Local/default builds use `none`;
unmapped non-default agent types use `other` and the lower automated sampling rate. Automation that the
existing resolver treats as local can use the MSBuild wrapper marker or opt out. No agent environment values,
branch/tag information, output variables or arbitrary type names become event properties.

For aggregate invocation estimates, calculate `1/sampling_probability` in the dashboard for each event
and sum the calculated weights; segment by probability when calculating uncertainty. Only the probability
is transmitted. Missing or non-positive probabilities must produce no calculated weight, rather than a
division error or an invented estimate. The runtime dashboard uses `calculated_sampling_weight`, derived
from probability even for older synthetic events that still contain the former event property.
For example, this PostHog SQL derives the weight alongside observed counts without reading a weight property:

```sql
SELECT
    properties.sampling_probability AS sampling_probability,
    if(sampling_probability > 0, 1.0 / sampling_probability, null) AS calculated_sampling_weight,
    count() AS sampled_invocations
FROM events
WHERE event = 'gitversion_cli_invocation'
GROUP BY sampling_probability
ORDER BY sampling_probability
```

Independent sampling has variance: an estimate based on sampled count `n`
and fixed probability `p` has estimated variance `n*(1-p)/p^2` before transport or ingestion loss.
These are invocation estimates, **not unique users**. Offline loss, CI detection gaps, opt-outs, quota drops
and forged events cause bias; do not claim these estimates represent all GitVersion usage.
The current dashboard contains controlled test events only and must not be used for adoption estimates.

There is no global client-side volume guarantee. A per-process guard prevents duplicate sends only within
that reporter. Rate caps cannot prevent aggregate traffic from reaching the Free quota. Maintainers can lower
either probability to zero or disable the campaign in source; existing releases retain their fixed settings
until the campaign cutoff. There is no remote configuration service.

## Campaign window and activation

Publish the [Telemetry policy and collected data][telemetry-policy] on gitversion.net and verify the notice's
URL is accessible before shipping an enabled campaign.

Maintainers review activation before setting the repository's GitHub Actions variable
`GITVERSION_TELEMETRY_PROJECT_KEY` under **Settings > Secrets and variables > Actions > Variables**.
Its value is the public EU PostHog project write key, not a personal/read/admin key. The packaging workflow
passes it to Cake as a build environment variable. There is no separate boolean enablement variable.
The key is configured outside source control and embedded in eligible distributed binaries.

Cake's `build/build/BuildLifetime.cs` requires `IsTaggedRelease` or `IsTaggedPreRelease` before forwarding
the key as `GitVersionTelemetryProjectKey`. The CLI embeds it as `TelemetryProjectKey` assembly metadata.
Missing, invalid or duplicate key metadata disables collection. Cake automatically captures the UTC build
date (`yyyy-MM-dd`) for these official tagged releases and prereleases.
Internal prerelease, untagged, local, fork and pull-request builds receive neither a key nor a collection date,
even when the environment contains the key.
Cake forwards the date as the `GitVersionTelemetryCampaignStart` MSBuild property;
the CLI project embeds `AssemblyMetadata("TelemetryCampaignStart", value)`, which the reporter reads.
Direct `dotnet` builds default to disabled and omit the date. A controlled local build must explicitly
supply both `GitVersionTelemetryProjectKey` and a valid `GitVersionTelemetryCampaignStart` to activate.
Personal/read/admin keys must never be shipped.
Missing or invalid metadata fails closed. Region is limited to PostHog's EU/US HTTPS ingestion
hosts; arbitrary endpoints and redirects are disallowed. Direct and automated sampling rates may only be
reduced from the caps above. No runtime environment variable enables collection or supplies a destination.
Changing the GitHub variable affects newly built artifacts only; it does not remotely enable or disable
already distributed releases. Removing the key prevents enablement in subsequent builds.

The campaign is active on UTC dates `start <= today < start.AddMonths(3)`, where `start` is the
release build date embedded in that assembly. Cake captures it once per build and reuses it for all packages
and runtimes produced by that build. Each official release or prerelease build, including a patch or later
tag rebuild, gets its own three-month window. Rebuilding on another UTC date changes the metadata and binary;
the compiler remains deterministic for identical inputs. The explicit metadata override for controlled local
builds described above is separate from this release workflow.
The end date is derived, not separately configured: for example, a November 1 start ends collection at
00:00 UTC on February 1. No manual start-date choice is needed.

Before activation, the project owner must verify **PostHog Cloud Free**, no payment method, no paid plan
or automatic overages. Do not provision services or add payment details as part of building GitVersion.
The Free analytics allowance is 1 million events per month. PostHog documents that excess Free-plan events
are permanently dropped, while pay-as-you-go charges beyond the allowance. Keep the project on Free.
Usage and the free allowance are shared across an organization's projects, and reset at the end of its
billing cycle rather than necessarily on the first day of a calendar month. On October 22, 2026, the EU
project's billing page showed the Free plan with an invitation to add a card, a Product Analytics free
allowance and matching Free-plan ingestion cap of 1 million events, and 26 events used in the
October 21–November 20 cycle. The Free UI labels these event counts "Free tier limit" and "Billing limit";
this verification does not establish a separately configured monetary cap. Paid-plan billing limits are
dollar amounts and would need separate verification if the organization changed plans.
This is a dated verification, not a guarantee of future provider settings; recheck before activation.
See [PostHog billing limits][posthog-billing-limits] and [organization-wide usage][posthog-billing-questions].

### Public key and abuse limitations

The public project write key is embedded in the CLI and included in ingestion requests. It identifies the
destination project; it does not authenticate a genuine GitVersion invocation. Putting the key in a
GitHub Actions secret or 1Password and injecting it during the build would still expose it in the
distributed binary and requests. Personal/read/admin keys must remain private and are never embedded.

Anyone with the public key can forge events, including otherwise valid-looking properties and sampling
probabilities, or exhaust the shared ingestion allowance. GitVersion's opt-outs, sampling rates, disabled
default and three-month campaign cutoff govern this client only; direct requests to PostHog can bypass
them. The ingestion cap limits accepted volume, not abuse: once it is exhausted, legitimate events are
dropped too. A source change does not remotely stop already distributed binaries or direct submissions.

Events go directly to PostHog; GitVersion operates no ingestion gateway or client-authentication service.
Use this dedicated project/schema, inspect unexpected volume and implausible cohorts, and treat dashboard
results and inverse-probability estimates as indicative, unauthenticated usage signals. Schema filters and
anomaly checks can exclude obvious noise but cannot prove that plausible events came from GitVersion.

## Provider processing and retention

EU capture sends HTTPS traffic to `eu.i.posthog.com`. The payload suppresses person processing and GeoIP
enrichment and supplies a zero IP sentinel. The provider and its network infrastructure still receive the
connection's source IP; transport/security logs and provider processing are governed by PostHog's policies.
Omitting identifying payload fields does not make network traffic anonymous. The project owner must verify
GeoIP is disabled in the project ingestion settings too and check transformations do not re-enable enrichment,
person processing or additional capture. Cloud settings must be verified separately from the repository.
Also enable **Settings > Project > IP data capture configuration > Discard client IP data**.
PostHog documents that transformations can still use IPs before this discard step, so that setting alone
does not disable GeoIP enrichment. Disable the GeoIP transformation for this dedicated project as well.

The three-month window stops **new collection**; it does not delete stored events. PostHog's Free plan
advertises one-year analytics retention. If deletion after three months is required, the owner must arrange
and verify provider-side deletion separately; this implementation sends no deletion/admin credentials.

## References

* [PostHog pricing and Free quota][posthog-pricing]
* [Capture API and personless events][posthog-capture]
* [PostHog privacy guidance][posthog-privacy]
* [PostHog SDK processing-control semantics][posthog-controls]
* [Project IP discard settings][posthog-ip-storage]
* [GeoIP enrichment controls][posthog-geoip]

[posthog-pricing]: https://posthog.com/pricing
[posthog-billing-limits]: https://posthog.com/docs/billing/limits-alerts
[posthog-billing-questions]: https://posthog.com/docs/billing/common-questions

[posthog-capture]: https://posthog.com/docs/api/capture

[posthog-privacy]: https://posthog.com/docs/privacy

[posthog-controls]: https://github.com/PostHog/posthog-go/blob/main/capture.go

[posthog-ip-storage]: https://posthog.com/docs/privacy/data-storage

[posthog-geoip]: https://posthog.com/docs/cdp/geoip-enrichment

[telemetry-policy]: /docs/usage/cli/telemetry-policy
[telemetry-detection]: /docs/usage/cli/telemetry-policy#how-runtime-and-caller-fields-are-derived

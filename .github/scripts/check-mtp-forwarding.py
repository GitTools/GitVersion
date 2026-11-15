"""Compare native MTP runners and verify real GitHub workflow annotations."""

import json
import os
from pathlib import Path
import subprocess
import sys
import urllib.request
import xml.etree.ElementTree as ET
from xml.sax.saxutils import quoteattr


REPO = Path(__file__).resolve().parents[2]
SOURCE = REPO / "tests/mtp-forwarding/ForwardingTests.cs"
SOURCE_PATH = SOURCE.relative_to(REPO).as_posix()
FAILURE_LINE = next(i for i, line in enumerate(SOURCE.read_text().splitlines(), 1)
                    if "Is.EqualTo(5)" in line)
MARKERS = [f"MTP_FORWARDING_{runner}_{tfm}" for runner in ("direct", "dotnet-test")
           for tfm in ("net10.0", "net11.0")]


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(command, directory, environment, log):
    result = subprocess.run(command, cwd=directory, env=environment,
                            capture_output=True, text=True, timeout=180)
    text = result.stdout + result.stderr
    log.write_text(text, encoding="utf-8")
    # Preserve workflow commands at the beginning of their original lines.
    print(text, end="", flush=True)
    return result.returncode, text


def probe():
    root = Path(os.environ["RUNNER_TEMP"]) / "mtp-forwarding"
    root.mkdir(parents=True, exist_ok=False)
    environment = os.environ.copy()
    version = subprocess.check_output(["dotnet", "--version"], cwd=REPO, text=True).strip()
    expected_version = json.loads((REPO / "global.json").read_text())["sdk"]["version"]
    require(version == expected_version, f"Expected SDK {expected_version}, got {version}")
    (root / "global.json").write_text((REPO / "global.json").read_text())
    packages = ET.parse(REPO / "src/Directory.Packages.props")
    versions = {item.get("Include"): item.get("Version")
                for item in packages.iter("PackageVersion")}
    names = ["Microsoft.NET.Test.Sdk", "NUnit", "NUnit3TestAdapter",
             "Microsoft.Testing.Extensions.GitHubActionsReport", "JunitXml.TestLogger", "coverlet.MTP"]
    references = "".join(f'<PackageReference Include="{name}" Version="{versions[name]}" />'
                         for name in names)
    library = root / "Probe"
    tests = root / "Probe.Tests"
    library.mkdir()
    tests.mkdir()
    (library / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<TargetFrameworks>net10.0;net11.0</TargetFrameworks></PropertyGroup></Project>')
    (library / "ProbeMath.cs").write_text('namespace Probe; public static class ProbeMath '
        '{ public static int Add(int left, int right) => left + right; }')
    project = tests / "Probe.Tests.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<TargetFrameworks>net10.0;net11.0</TargetFrameworks><OutputType>Exe</OutputType>'
        '<IsTestProject>true</IsTestProject><EnableNUnitRunner>true</EnableNUnitRunner>'
        '<EnableMicrosoftTestingPlatformRunner>true</EnableMicrosoftTestingPlatformRunner>'
        '<ContinuousIntegrationBuild>false</ContinuousIntegrationBuild><Optimize>false</Optimize>'
        '</PropertyGroup><ItemGroup>' + references +
        '<ProjectReference Include="../Probe/Probe.csproj" />'
        '<Compile Include=' + quoteattr(str(SOURCE)) + ' />'
        '</ItemGroup></Project>')
    code, _ = run(["dotnet", "build", str(project), "-c", "Release"], root,
                  environment, root / "build.log")
    require(code == 0, "Probe build failed")
    results = []
    for tfm in ("net10.0", "net11.0"):
        for runner in ("direct", "dotnet-test"):
            for passing in (False, True):
                output = root / f"{runner}-{tfm}-{'pass' if passing else 'fail'}"
                output.mkdir()
                marker = f"MTP_FORWARDING_{runner}_{tfm}"
                environment["MTP_PROBE_MARKER"] = marker
                summary = output / "summary.md"
                environment["GITHUB_STEP_SUMMARY"] = str(summary)
                args = ["--report-spekt-junit", "--report-spekt-junit-filename", str(output / "results.xml"),
                        "--results-directory", str(output), "--report-gh", "--report-gh-annotations", "on",
                        "--report-gh-groups", "off", "--report-gh-step-summary", "on",
                        "--report-gh-step-summary-sections", "test-results", "--report-gh-failure-details", "on",
                        "--report-gh-slow-test-notices", "off"]
                if tfm == "net10.0":
                    args += ["--coverlet", "--coverlet-output-format", "cobertura",
                             "--coverlet-exclude", "[Probe.Tests]*", "--coverlet-exclude", "[Spekt.TestLogger]*"]
                if passing:
                    args += ["--filter", "FullyQualifiedName~PassesAddition"]
                command = (["dotnet", str(tests / "bin/Release" / tfm / "Probe.Tests.dll")]
                           if runner == "direct" else ["dotnet", "test", "--project", str(project),
                           "--framework", tfm, "--configuration", "Release", "--no-build"])
                command += args
                (output / "command.json").write_text(json.dumps(command, indent=2))
                code, text = run(command, root, environment, output / "test.log")
                require(code == (0 if passing else 2), f"Unexpected test exit: {code}")
                require(summary.is_file() and summary.stat().st_size > 0, "Missing GitHub step summary")
                if os.environ.get("GITHUB_STEP_SUMMARY"):
                    with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as combined:
                        combined.write(f"\n### {output.name}\n" + summary.read_text(encoding="utf-8"))
                suites = list(ET.parse(output / "results.xml").iter("testsuite"))
                require(len(suites) == 1, "Expected one JUnit test suite")
                suite = suites[0]
                require(suite.get("tests") == ("1" if passing else "2") and
                        suite.get("failures") == ("0" if passing else "1") and
                        suite.get("errors") == "0" and suite.get("skipped") == "0", "Unexpected JUnit outcome")
                if not passing:
                    annotations = [line for line in text.splitlines() if line.startswith("::error ")]
                    require(len(annotations) == 1 and marker in annotations[0] and
                            f"file={SOURCE_PATH},line={FAILURE_LINE}," in annotations[0], "Missing source annotation")
                if tfm == "net10.0":
                    coverage = list(output.glob("*cobertura*.xml"))
                    require(len(coverage) == 1, "Missing or ambiguous Cobertura report")
                    classes = list(ET.parse(coverage[0]).iter("class"))
                    require(any(c.get("name") == "Probe.ProbeMath" and c.get("line-rate") == "1"
                                for c in classes), "Missing production coverage")
                results.append({"runner": runner, "framework": tfm, "passing": passing, "exit": code})
    (root / "results.json").write_text(json.dumps(results, indent=2))
    print("All eight expected failure/pass runs and report checks passed.")


def api(path):
    request = urllib.request.Request(os.environ["GITHUB_API_URL"] + path,
        headers={"Authorization": "Bearer " + os.environ["GH_TOKEN"],
                 "Accept": "application/vnd.github+json"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def annotations(url):
    page = 1
    found = []
    while True:
        batch = api(url + f"?per_page=100&page={page}")
        found.extend(batch)
        if len(batch) < 100:
            return found
        page += 1


def verify_hosted():
    base = "/repos/" + os.environ["GITHUB_REPOSITORY"]
    attempt = os.environ["GITHUB_RUN_ATTEMPT"]
    route = base + "/actions/runs/" + os.environ["GITHUB_RUN_ID"] + "/attempts/" + attempt + "/jobs"
    jobs = []
    page = 1
    while True:
        batch = api(route + f"?per_page=100&page={page}")
        jobs.extend(batch["jobs"])
        if len(jobs) >= batch["total_count"]:
            break
        page += 1
    probes = [job for job in jobs if job["name"].startswith("Forwarding (")]
    require(len(probes) == 3, "Expected all three hosted operating systems")
    for job in probes:
        require(job["conclusion"] == "success", "Probe job failed: " + job["html_url"])
        check_id = job["check_run_url"].rsplit("/", 1)[1]
        stored = annotations(base + "/check-runs/" + check_id + "/annotations")
        for marker in MARKERS:
            require(any(a["path"] == SOURCE_PATH and a["start_line"] == FAILURE_LINE and
                        a["annotation_level"] == "failure" and marker in a["message"] for a in stored),
                    "GitHub did not store " + marker + " for " + job["html_url"])
        print("Verified all four hosted source annotations: " + job["html_url"])


if __name__ == "__main__":
    if sys.argv[1:] == ["--verify-hosted"]:
        verify_hosted()
    elif not sys.argv[1:]:
        probe()
    else:
        raise SystemExit("Expected no arguments or --verify-hosted")

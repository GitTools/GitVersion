#!/usr/bin/env python3
"""Compare an opt-in native CLI with the matching managed executable before packaging.

Uses only Python's standard library and Git. All repositories, output files and
HTTP requests are isolated locally. Exit zero requires every parity assertion.
"""
import argparse
from functools import partial
import hashlib
import http.server
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import threading


HISTORY_FILE = "history.txt"
GITHUB_WORKFLOW = "workflow: GitHubFlow/v1\n"
MAIN_REF = "refs/heads/main"


def parse_arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--managed", type=Path, required=True)
    parser.add_argument("--native", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--framework", required=True)
    parser.add_argument("--rid", required=True)
    parser.add_argument("--runtime-version", required=True)
    parser.add_argument("--repository", type=Path, help="Also calculate on a local clone of this existing history")
    return parser.parse_args()


def isolated_environment():
    prefixes = ("GITVERSION_", "GITHUB_", "APPVEYOR", "GITLAB_", "TEAMCITY_", "JENKINS_", "BITBUCKET_", "BUILDKITE_", "DRONE_", "TRAVIS_", "CODEBUILD_")
    excluded = {
        "CI", "TF_BUILD", "BUILDKITE", "DRONE", "TRAVIS", "BuildRunner", "ContinuaCI.Version", "ENVRUN_DATABASE", "JB_SPACE_PROJECT_KEY", "JB_SPACE_GIT_BRANCH", "Git_Branch", "GIT_BRANCH", "GIT_LOCAL_BRANCH", "BRANCH_NAME", "BUILD_SOURCEBRANCH", "BUILD_BUILDNUMBER", "CI_COMMIT_TAG", "CI_COMMIT_REF_NAME", "CI_COMMIT_REFSPEC", "CI_MERGE_REQUEST_REF_PATH", "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR", "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES", "GIT_CONFIG", "GIT_CONFIG_COUNT",
    }
    env = {key: value for key, value in os.environ.items() if not key.startswith(prefixes) and key not in excluded}
    env.update(GITVERSION_GIT_BACKEND="managed", GITVERSION_ARGUMENT_PARSER_VERSION="v7", GITVERSION_CONFIGURATION_VERSION="v7",
               GIT_CONFIG_GLOBAL=os.devnull, GIT_CONFIG_NOSYSTEM="1")
    return env


def create_http_handler(requests):
    class Handler(http.server.BaseHTTPRequestHandler):
        def handle_request(self):
            requests.append({"method": self.command, "path": self.path, "body": json.loads(self.rfile.read(int(self.headers["Content-Length"])))})
            self.send_response(204)
            self.end_headers()

        do_PUT = handle_request
        do_POST = handle_request

        def log_message(self, *_):
            # Silence local stub access logs; they are not CLI diagnostics.
            pass

    return Handler


def run_git(env, repo, *arguments):
    return subprocess.run(["git", "-C", str(repo), *arguments], env=env, text=True, capture_output=True, check=True, timeout=60).stdout.strip()


def create_repository(work, git, name, tagged):
    repo = work / name
    repo.mkdir()
    git(repo, "init", "--initial-branch=main")
    git(repo, "config", "user.name", "NativeAOT parity")
    git(repo, "config", "user.email", "nativeaot-parity@example.invalid")
    git(repo, "config", "commit.gpgsign", "false")
    for i in range(2):
        (repo / HISTORY_FILE).write_text(f"commit {i}\n", encoding="utf-8")
        git(repo, "add", HISTORY_FILE)
        git(repo, "commit", "-m", f"initial {i}")
        if tagged and i == 0:
            git(repo, "tag", "1.2.3")
    return repo


def process_command(command, windows=None):
    windows = os.name == "nt" if windows is None else windows
    if not windows or Path(command[0]).suffix.lower() != ".cmd":
        return command
    # list2cmdline quotes whitespace for the CRT, but cmd also needs pipes quoted.
    encoded = []
    for argument in command:
        quoted = subprocess.list2cmdline([str(argument)])
        if not quoted.startswith('"') and any(symbol in str(argument) for symbol in "&|<>^"):
            quoted = '"' + quoted + '"'
        encoded.append(quoted)
    return " ".join(encoded)


def run_flavor(command, arguments, work, env, requests, prepare, capture):
    if prepare:
        prepare()
    start = len(requests)
    run = subprocess.run(process_command(command + [str(a) for a in arguments]), cwd=work, env=env, capture_output=True, timeout=60)
    # Compatibility-mode warnings include wall-clock timestamps. Keep
    # their severity/message/path and all other diagnostics unchanged.
    stderr = re.sub(r"(?m)\b(INFO|WARN|EROR|DBUG) \[\d{2}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}:\d{2}\]", r"\1 [timestamp]", run.stderr.decode("utf-8"))
    result = {"exit": run.returncode, "stdout": run.stdout.decode("utf-8"), "stderr": stderr}
    if capture:
        result["side_effects"] = capture()
    if len(requests) != start:
        result["http"] = requests[start:]
    return result


def assert_matching_outputs(actual, expected_exit, assert_result):
    if actual["managed"] != actual["native"]:
        raise AssertionError("managed/native output, diagnostics or side effects differ")
    if actual["native"]["exit"] != expected_exit:
        raise AssertionError(f"expected exit {expected_exit}, received {actual['native']['exit']}")
    if assert_result:
        assert_result(actual["native"])


def make_comparator(commands, work, env, requests, results):
    def compare(name, arguments, *, expected_exit=0, extra_env=None, prepare=None, capture=None, assert_result=None):
        actual = {}
        try:
            for flavor, command in commands.items():
                actual[flavor] = run_flavor(command, arguments, work, env | (extra_env or {}), requests, prepare, capture)
            assert_matching_outputs(actual, expected_exit, assert_result)
            results.append({"case": name, "passed": True})
        except (AssertionError, subprocess.SubprocessError, ValueError, OSError, KeyError, TypeError) as error:
            results.append({"case": name, "passed": False, "error": str(error), "results": actual})
        return actual

    return compare


def create_fixtures(work, repository):
    tagged = repository("tagged", True)
    untagged = repository("untagged", False)
    config = work / "GitVersion.yml"
    config.write_text(GITHUB_WORKFLOW, encoding="utf-8")
    custom = work / "custom-é.yml"
    custom.write_text("workflow: GitHubFlow/v1\ncalculation:\n  branches:\n    main:\n      increment: Minor\noutput:\n  assembly-informational-format: '{FullSemVer}+{Sha}'\n", encoding="utf-8")
    invalid = work / "invalid.yml"
    invalid.write_text("calculation:\n  not-a-setting: true\n", encoding="utf-8")
    malformed = work / "malformed.yml"
    malformed.write_text("calculation: [\n", encoding="utf-8")
    legacy = work / "legacy.yml"
    legacy.write_text("mode: ContinuousDelivery\nnext-version: 2.0.0\n", encoding="utf-8")

    return tagged, untagged, config, custom, invalid, malformed, legacy


def compare_startup_and_validation(compare, config, custom, invalid, malformed, legacy):
    for option in ["--help", "--version", "--cli-schema"]:
        compare("startup-" + option[2:], [option])
    for path, valid in [(config, True), (custom, True), (invalid, False), (malformed, False)]:
        for format_args in [[], ["--format", "json"]]:
            compare("validate-" + path.stem + ("-json" if format_args else "-text"), ["config", "validate", "--config", path, *format_args], expected_exit=0 if valid else 1)
    compare("migrate-v6", ["config", "migrate", "--config", legacy])


def compare_configuration(compare, tagged, untagged, configuration_files, common):
    config, custom, invalid, malformed, legacy = configuration_files
    compare_startup_and_validation(compare, config, custom, invalid, malformed, legacy)
    for repo in [untagged, tagged]:
        compare(repo.name + "-default-json", [repo, *common])
        compare(repo.name + "-effective-defaults", [repo, "--show-config"])
        compare(repo.name + "-custom-json", [repo, *common, "--config", custom])
        compare(repo.name + "-effective-custom", [repo, "--show-config", "--config", custom])
    for filename in ["GitVersion.yml", "GitVersion.yaml", ".GitVersion.yml", ".GitVersion.yaml"]:
        candidate = untagged / filename
        candidate.write_text(GITHUB_WORKFLOW, encoding="utf-8")
        compare("discover-" + filename, [untagged, *common])
        compare("validate-discovered-" + filename, [untagged, "config", "validate", "--format", "json"])
        candidate.unlink()
    for workflow in ["GitHubFlow/v1", "GitFlow/v1"]:
        for mode in ["ContinuousDelivery", "ContinuousDeployment", "ManualDeployment"]:
            config.write_text(f"workflow: {workflow}\ncalculation:\n  mode: {mode}\n  branches:\n    main:\n      mode: {mode}\n      label: preview\n", encoding="utf-8")
            compare("deployment-" + workflow + "-" + mode, [tagged, *common, "--config", config])
    config.write_text("workflow: GitHubFlow/v1\ncalculation:\n  strategies: [ConfiguredNextVersion, Mainline]\n", encoding="utf-8")
    compare("mainline-strategies", [tagged, *common, "--config", config])
    config.write_text(GITHUB_WORKFLOW, encoding="utf-8")
    compare("override-next-version", [tagged, *common, "--config", config, "--override-config", "calculation.next-version=3.2.1"])
    for property_path in ["calculation.mode", "calculation.increment", "output.assembly-informational-format", "output.update-build-number", "calculation.next-version"]:
        compare("query-" + property_path, [tagged, "config", "get", property_path, "--config", custom])
    compare("query-rejects-dynamic-map", [tagged, "config", "get", "calculation.branches.main.increment", "--config", custom], expected_exit=1)


def compare_history(compare, git, tagged, work, common, config, custom):
    git(tagged, "checkout", "-b", "feature/native-parity")
    (tagged / "feature.txt").write_text("feature\n", encoding="utf-8")
    git(tagged, "add", "feature.txt")
    git(tagged, "commit", "-m", "feat: parity feature")
    compare("feature-branch", [tagged, *common])
    git(tagged, "checkout", "main")
    git(tagged, "merge", "--no-ff", "feature/native-parity", "-m", "Merge branch 'feature/native-parity'")
    compare("merged-main", [tagged, *common])
    config.write_text("workflow: GitHubFlow/v1\ncalculation:\n  strategies: [ConfiguredNextVersion, Mainline]\n", encoding="utf-8")
    compare("mainline-merges", [tagged, *common, "--config", config])
    git(tagged, "tag", "-a", "1.2.4", "-m", "annotated parity tag")
    compare("annotated-tag", [tagged, *common])
    git(tagged, "checkout", "-b", "develop")
    git(tagged, "commit", "--allow-empty", "-m", "develop")
    compare("develop-branch", [tagged, *common])
    git(tagged, "checkout", "-b", "release/1.3.0")
    git(tagged, "commit", "--allow-empty", "-m", "release")
    compare("release-branch", [tagged, *common])
    git(tagged, "checkout", "-b", "hotfix/1.2.5", "1.2.4")
    git(tagged, "commit", "--allow-empty", "-m", "fix: native parity hotfix")
    hotfix_config = work / "hotfix.yml"
    hotfix_config.write_text("workflow: GitFlow/v1\n", encoding="utf-8")
    compare("hotfix-branch", [tagged, *common, "--config", hotfix_config])
    git(tagged, "checkout", "--detach", "1.2.3")

    def assert_tagged(result):
        if json.loads(result["stdout"])["FullSemVer"] != "1.2.3":
            raise AssertionError("tagged calculation did not produce independently expected 1.2.3")

    compare("tagged-detached", [tagged, *common], assert_result=assert_tagged)
    git(tagged, "checkout", "main")

    def assert_dirty(result):
        if int(json.loads(result["stdout"])["UncommittedChanges"]) != 1:
            raise AssertionError("one modified tracked file did not produce UncommittedChanges=1")

    (tagged / HISTORY_FILE).write_text("modified tracked file\n", encoding="utf-8")
    compare("dirty-tracked-file", [tagged, *common], assert_result=assert_dirty)
    git(tagged, "checkout", "--", HISTORY_FILE)
    git(tagged, "gc", "--prune=now")
    compare("packed-history", [tagged, *common])
    compare("custom-nested-format", [tagged, *common, "--format", "{Major}.{Minor}.{Patch}|{FullSemVer.Length}|{CommitDate:yyyy-MM-dd}|{env:NATIVE_PARITY ?? \"fallback\"}"], extra_env={"NATIVE_PARITY": "unicode-é"})
    compare("custom-information-format", [tagged, *common, "--config", custom])
    compare("dotenv", [tagged, *common, "--output", "dotenv"])
    compare("selected-variable", [tagged, *common, "--show-variable", "FullSemVer"])


def compare_cache(compare, tagged):
    cache = tagged / ".git" / "gitversion_cache"

    def reset_cache():
        shutil.rmtree(cache, ignore_errors=True)

    def capture_cache():
        files = {str(path.relative_to(cache)): path.read_bytes().hex() for path in cache.rglob("*") if path.is_file()}
        if not files:
            raise AssertionError("calculation did not write a cache entry")
        return files

    # Compare independent writers first, then restore the same managed-written
    # cache before each reader. A native write cannot mask a native read defect.
    cache_writes = compare("cache-write-and-cross-read", [tagged, "--no-fetch"], prepare=reset_cache, capture=capture_cache)
    cache_seed = cache_writes.get("managed", {}).get("side_effects", {})

    def restore_cache():
        if not cache_seed:
            raise AssertionError("managed calculation did not provide a cache seed")
        reset_cache()
        for filename, content in cache_seed.items():
            path = cache / filename
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(bytes.fromhex(content))

    compare("cache-hit", [tagged, "--no-fetch"], prepare=restore_cache, capture=capture_cache)


def compare_modes_and_cultures(compare, tagged, legacy, common):
    compare("v6-configuration", [tagged, *common, "--config", legacy], extra_env={"GITVERSION_CONFIGURATION_VERSION": "v6"})
    compare("v6-argument-parser", [tagged, "/nocache", "/nofetch", "/output", "json"], extra_env={"GITVERSION_ARGUMENT_PARSER_VERSION": "v6"})
    for culture in ["de_DE.UTF-8", "tr_TR.UTF-8"]:
        culture_env = {"LANG": culture, "LC_ALL": culture, "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT": "0"}
        compare("culture-json-" + culture, [tagged, *common], extra_env=culture_env)
        compare("culture-custom-format-" + culture, [tagged, *common, "--format", "{Major:0000}|{CommitDate:MMMM}|{FullSemVer:u}"], extra_env=culture_env)


def compare_repository_access(compare, git, work, tagged, common, env):
    remote = work / "remote.git"
    subprocess.run(["git", "clone", "--bare", str(tagged), str(remote)], env=env, capture_output=True, check=True, timeout=60)
    git(tagged, "remote", "add", "origin", str(remote))
    git(tagged, "fetch", "origin")
    shallow = work / "shallow"
    subprocess.run(["git", "clone", "--depth", "1", "--branch", "main", remote.as_uri(), str(shallow)], env=env, capture_output=True, check=True, timeout=60)
    if git(shallow, "rev-parse", "--is-shallow-repository") != "true":
        raise AssertionError("local shallow-clone fixture did not preserve a shallow boundary")
    # A tagged shallow tip is sufficient for this existing managed policy.
    compare("shallow-tagged-history", [shallow, *common])
    git(shallow, "fetch", "--unshallow", "origin")
    if git(shallow, "rev-parse", "--is-shallow-repository") != "false":
        raise AssertionError("local fetch did not remove the shallow boundary")
    compare("unshallowed-history", [shallow, *common])
    linked = work / "linked-worktree"
    git(tagged, "worktree", "add", "--detach", str(linked), "main")
    compare("linked-worktree", [linked, *common])
    compare("bare-history-rejected", [remote, *common], expected_exit=1)
    dynamic = work / "dynamic"
    compare("git-cli-local-clone-and-fetch", ["--url", remote, "--branch", "main", "--dynamic-repo-location", dynamic, "--no-cache"], prepare=lambda: shutil.rmtree(dynamic, ignore_errors=True))

    # The executable itself has an absolute path. An empty child-only PATH
    # isolates the missing-Git failure without changing the host installation.
    no_git_path = work / "empty-path"
    no_git_path.mkdir()

    # Both existing CLI flavors route this failure to a nonzero exit with
    # no console diagnostic at the default output settings.
    compare("missing-git-executable", ["--url", remote, "--branch", "main", "--dynamic-repo-location", dynamic, "--no-cache"], expected_exit=1, extra_env={"PATH": str(no_git_path)}, prepare=lambda: shutil.rmtree(dynamic, ignore_errors=True))


def compare_file_outputs(compare, work, tagged, common):
    log = work / "calculation.log"

    def capture_log():
        content = log.read_text(encoding="utf-8")
        content = re.sub(r"(?m)\b(INFO|WARN|EROR|DBUG) \[\d{2}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}:\d{2}\]", r"\1 [timestamp]", content)
        return re.sub(r"\(Took: [0-9,.]+ms\)", "(Took: [duration]ms)", content)

    def assert_log(result):
        content = result["side_effects"]
        if "Applicable build agent found: 'LocalBuild'." not in content or "Begin:" not in content or "End:" not in content:
            raise AssertionError("file logging did not retain the agent and timed calculation messages")

    compare("file-log-calculation", [tagged, *common, "--log-file", log], prepare=lambda: log.unlink(missing_ok=True), capture=capture_log, assert_result=assert_log)

    output = work / "versions.json"
    compare("json-file", [tagged, *common, "--output", "file", "--output-file", output], prepare=lambda output=output: output.unlink(missing_ok=True), capture=lambda output=output: output.read_bytes().decode("utf-8"))
    for extension in ["cs", "fs", "vb"]:
        output = work / ("AssemblyInfo." + extension)
        compare("assembly-template-" + extension, [tagged, *common, "--update-assembly-info", output, "--ensure-assembly-info"], prepare=lambda output=output: output.unlink(missing_ok=True), capture=lambda output=output: output.read_bytes().decode("utf-8"))

    project_output = work / "Fixture.csproj"
    compare("project-file-update", [tagged, *common, "--update-project-files", project_output],
            prepare=lambda: project_output.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>', encoding="utf-8"),
            capture=lambda: project_output.read_bytes().decode("utf-8"))
    wix_output = tagged / "GitVersion_WixVersion.wxi"
    compare("wix-version-file", [tagged, *common, "--update-wix-version-file"],
            prepare=lambda: wix_output.unlink(missing_ok=True), capture=lambda: wix_output.read_bytes().decode("utf-8"))
    wix_output.unlink(missing_ok=True)


def compare_github(compare, work, tagged, common):
    github_env = work / "github-env"
    github_output = work / "github-output"

    def reset_github():
        github_env.write_text("", encoding="utf-8")
        github_output.write_text("", encoding="utf-8")

    def assert_github(result):
        if "GitVersion_FullSemVer=" not in result["side_effects"]["env"]:
            raise AssertionError("GitHub output did not preserve the GitVersion_ variable contract")

    compare("github-buildserver", [tagged, *common, "--output", "buildserver", "--verbosity", "Quiet"], extra_env={"GITHUB_ACTIONS": "true", "GITHUB_REF": MAIN_REF, "GITHUB_ENV": str(github_env), "GITHUB_OUTPUT": str(github_output)}, prepare=reset_github, capture=lambda: {"env": github_env.read_bytes().decode("utf-8"), "output": github_output.read_bytes().decode("utf-8")}, assert_result=assert_github)


def assert_appveyor(result):
    calls = result.get("http", [])
    updates = [call for call in calls if call["method"] == "PUT" and call["path"] == "/api/build"]
    variables = [call for call in calls if call["method"] == "POST" and call["path"] == "/api/build/variables"]
    if len(updates) != 1 or not updates[0]["body"]["version"].endswith(".build.42"):
        raise AssertionError("AppVeyor build-number request/suffix did not reach the local stub")
    if not variables or any(not call["body"]["name"].startswith("GitVersion_") for call in variables):
        raise AssertionError("AppVeyor variable requests did not preserve the GitVersion_ prefix")
    full_versions = [call["body"]["value"] for call in variables if call["body"]["name"] == "GitVersion_FullSemVer"]
    if len(full_versions) != 1:
        raise AssertionError("AppVeyor did not receive exactly one FullSemVer variable")
    full_version = full_versions[0]
    if updates[0]["body"]["version"] != full_version + ".build.42":
        raise AssertionError("AppVeyor variable/build-number values disagree")
def compare_appveyor(compare, handler, tagged, common):
    with http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler) as server:
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            compare("appveyor-buildserver-http", [tagged, *common, "--output", "buildserver", "--verbosity", "Quiet"], extra_env={"APPVEYOR": "true", "APPVEYOR_REPO_BRANCH": "main", "APPVEYOR_BUILD_NUMBER": "42", "APPVEYOR_API_URL": f"http://127.0.0.1:{server.server_port}/"}, assert_result=assert_appveyor)
        finally:
            server.shutdown()
            thread.join()


def assert_agent_selected(result, agent):
    if f"Set Output Variables for '{agent}'." not in result["stdout"]:
        raise AssertionError(f"Adapter {agent} was not selected or emitted no integration header")


def compare_agents(compare, work, tagged, common):
    envrun = work / "envrun.db"
    envrun.touch()
    agents = {
        "AzurePipelines": {"TF_BUILD": "true", "BUILD_SOURCEBRANCH": MAIN_REF, "BUILD_BUILDNUMBER": "$(GITVERSION_FullSemVer)"},
        "TeamCity": {"TEAMCITY_VERSION": "2026.1", "Git_Branch": "main"},
        "MyGet": {"BuildRunner": "MyGet"},
        "ContinuaCi": {"ContinuaCI.Version": "1"},
        "BuildKite": {"BUILDKITE": "true", "BUILDKITE_BRANCH": "main", "BUILDKITE_PULL_REQUEST": "false"},
        "SpaceAutomation": {"JB_SPACE_PROJECT_KEY": "fixture", "JB_SPACE_GIT_BRANCH": "main"},
        "EnvRun": {"ENVRUN_DATABASE": str(envrun)},
        "TravisCi": {"TRAVIS": "true", "CI": "true"},
        "CodeBuild": {"CODEBUILD_WEBHOOK_HEAD_REF": MAIN_REF},
        "BitBucketPipelines": {"BITBUCKET_WORKSPACE": "fixture", "BITBUCKET_BRANCH": MAIN_REF},
        "Drone": {"DRONE": "true", "DRONE_BRANCH": "main"},
        "Jenkins": {"JENKINS_URL": "http://127.0.0.1/", "GIT_LOCAL_BRANCH": "main"},
        "GitLabCi": {"GITLAB_CI": "true", "CI_COMMIT_REF_NAME": "main"},
    }

    def reset_agent_files():
        for filename in ["gitversion.properties", "gitversion.ps1"]:
            (work / filename).unlink(missing_ok=True)

    def capture_agent_files():
        return {filename: (work / filename).read_bytes().decode("utf-8") for filename in ["gitversion.properties", "gitversion.ps1"] if (work / filename).exists()}

    for agent, selection in agents.items():
        compare("buildserver-" + agent, [tagged, *common, "--output", "buildserver", "--verbosity", "Quiet"], extra_env=selection, prepare=reset_agent_files, capture=capture_agent_files, assert_result=partial(assert_agent_selected, agent=agent))


def compare_backends(commands, work, env, tagged, common, results):
    # Native defaults select the managed backend, while the ordinary CLI's
    # release default remains libgit2. Compare actual calculations explicitly.
    for label, selector in [("unset", None), ("blank", " "), ("case-insensitive", " Managed ")]:
        native_env = dict(env)
        if selector is None:
            native_env.pop("GITVERSION_GIT_BACKEND", None)
        else:
            native_env["GITVERSION_GIT_BACKEND"] = selector
        name = "native-backend-" + label
        try:
            baseline = subprocess.run(commands["managed"] + [str(tagged), *common], cwd=work, env=env, capture_output=True, timeout=60)
            calculated = subprocess.run(commands["native"] + [str(tagged), *common], cwd=work, env=native_env, capture_output=True, timeout=60)
            results.append({"case": name, "passed": baseline.returncode == 0 and (calculated.returncode, calculated.stdout, calculated.stderr) == (baseline.returncode, baseline.stdout, baseline.stderr)})
        except (subprocess.SubprocessError, OSError) as error:
            results.append({"case": name, "passed": False, "error": str(error)})
    # Capability rejection intentionally differs from the dual-backend CLI.
    for selector in ["libgit2", "unknown"]:
        name = "unavailable-backend-rejected-" + selector
        try:
            rejected = subprocess.run(commands["native"] + ["--version"], env=env | {"GITVERSION_GIT_BACKEND": selector}, capture_output=True, text=True, timeout=30)
            results.append({"case": name, "passed": rejected.returncode == 1 and "only the managed Git backend" in rejected.stderr})
        except (subprocess.SubprocessError, OSError) as error:
            results.append({"case": name, "passed": False, "error": str(error)})


def compare_existing_history(args, work, env, git, compare, common):
    if args.repository:
        repo = work / "existing-history"
        source = args.repository.resolve()
        subprocess.run(["git", "clone", "--shared", "--no-checkout", str(source), str(repo)], env=env, capture_output=True, check=True, timeout=60)
        # A CI checkout may have only remote branches and a detached HEAD.
        # Normal cloning transfers local heads, losing those calculation inputs.
        references = git(source, "for-each-ref", "--format=%(objectname) %(refname)", "refs/heads", "refs/remotes")
        for reference in references.splitlines():
            commit, name = reference.split(" ", 1)
            if not name.endswith("/HEAD"):
                git(repo, "update-ref", name, commit)
        git(repo, "checkout", "--detach", "HEAD")
        compare("existing-history-json", [repo, *common])
        compare("existing-history-config", [repo, "--show-config"])


def create_report(args, managed, native, env, results):
    root = Path(__file__).resolve().parent.parent
    source = subprocess.run(["git", "rev-parse", "HEAD"], cwd=root, env=env, capture_output=True, text=True, check=True, timeout=60).stdout.strip()
    source_files = subprocess.run(["git", "ls-files", "--cached", "--others", "--exclude-standard", "src"], cwd=root, env=env, capture_output=True, text=True, check=True, timeout=60).stdout.splitlines()
    source_digest = hashlib.sha256()
    for filename in sorted(source_files):
        path = root / filename
        if path.is_file():
            source_digest.update(filename.encode() + b"\0" + path.read_bytes())
    report = {"source_sha": source, "source_tree_sha256": source_digest.hexdigest(), "framework": args.framework, "rid": args.rid, "runtime_version": args.runtime_version, "managed_sha256": hashlib.sha256(managed.read_bytes()).hexdigest(), "native_sha256": hashlib.sha256(native.read_bytes()).hexdigest(), "normalization": "Wall-clock log timestamps are replaced; captured file-log scope durations (Took: ...ms) are also replaced. Exit, stdout, diagnostic messages/paths, other generated-file content and captured HTTP requests must otherwise match exactly. Same fixtures/configuration/path used for both processes. Backend rejection is a separate native-only capability check.", "limitations": ["Culture cases request de-DE/tr-TR via child process locale variables with invariant globalization disabled; they do not validate installed OS locale data or every supported culture.", "Missing Git uses an empty child-only PATH; credential-helper authentication failure is not covered by this harness.", "Raw concurrent Git stdout/stderr capture belongs to GitCliExecutorTests; this harness exercises CLI clone/fetch and process-start failure."], "passed": sum(result["passed"] for result in results), "failed": sum(not result["passed"] for result in results), "cases": results}
    return report


def main():
    args = parse_arguments()
    managed = args.managed.resolve()
    native = args.native.resolve()
    dotnet = shutil.which(args.dotnet) or args.dotnet
    commands = {"managed": ([dotnet, str(managed)] if managed.suffix == ".dll" else [str(managed)]), "native": [str(native)]}
    env = isolated_environment()
    results = []
    requests = []
    with tempfile.TemporaryDirectory(prefix="gitversion-nativeaot-parity-é-") as temp:
        work = Path(temp)
        git = partial(run_git, env)
        repository = partial(create_repository, work, git)
        compare = make_comparator(commands, work, env, requests, results)
        tagged, untagged, config, custom, invalid, malformed, legacy = create_fixtures(work, repository)
        common = ["--no-cache", "--no-fetch"]
        compare_configuration(compare, tagged, untagged, (config, custom, invalid, malformed, legacy), common)
        compare_history(compare, git, tagged, work, common, config, custom)
        compare_cache(compare, tagged)
        compare_modes_and_cultures(compare, tagged, legacy, common)
        compare_repository_access(compare, git, work, tagged, common, env)
        compare_file_outputs(compare, work, tagged, common)
        compare_github(compare, work, tagged, common)
        compare_appveyor(compare, create_http_handler(requests), tagged, common)
        compare_agents(compare, work, tagged, common)
        compare_backends(commands, work, env, tagged, common, results)
        compare_existing_history(args, work, env, git, compare, common)
        report = create_report(args, managed, native, env, results)
        print(json.dumps(report, indent=2, ensure_ascii=False))
        return 1 if report["failed"] else 0


if __name__ == "__main__":
    raise SystemExit(main())

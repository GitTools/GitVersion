#!/usr/bin/env python3
"""Standard-library regression checks for the NativeAOT validation gates."""
from contextlib import redirect_stdout
from functools import partial
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


VALIDATION = load("validation", "nativeaot-platform-validation.py")
PARITY = load("parity", "nativeaot-parity.py")
TOOLS = load("tools", "nativeaot-tool-validation.py")
CONTAINERS = load("containers", "nativeaot-container-validation.py")
ISOLATION = load("isolation", "nativeaot-hosted-isolation.py")
CAPTURE = load("capture", "nativeaot-capture-validation.py")


class ValidationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.work = Path(self.temporary.name)
        self.args = SimpleNamespace(output=self.work, rid="linux-arm64", framework="net10.0", runtime_version="10.0.13")
        for flavor in ("native", "managed"):
            (self.work / flavor).mkdir()
            (self.work / flavor / "gitversion").write_bytes(flavor.encode())
        self.source = {"sha": "source-revision"}
        names = json.loads(Path(__file__).with_name("nativeaot-cases.json").read_text(encoding="utf-8"))
        self.report = {"source_sha": self.source["sha"], "framework": self.args.framework, "rid": self.args.rid, "runtime_version": self.args.runtime_version,
                       "native_sha256": hashlib.sha256(b"native").hexdigest(), "managed_sha256": hashlib.sha256(b"managed").hexdigest(),
                       "passed": 99, "failed": 0, "cases": [{"case": name, "passed": True} for name in names]}

    def test_rejects_substituted_corpus_case(self):
        for name in ("existing-history-config", "startup-version", "tagged-default-json"):
            self.assertIn(name, {case["case"] for case in self.report["cases"]})
            report = self.report | {"cases": [case | {"case": "unrelated-passing-case"} if case["case"] == name else case for case in self.report["cases"]]}
            with self.subTest(replaced=name), self.assertRaises(RuntimeError):
                VALIDATION.check_report(report, self.args, self.source)

    def test_native_output_rejects_any_dll(self):
        self.args.dotnet = "dotnet"
        for name in ("Unrelated.dll", "Other.DLL"):
            path = self.work / "native" / name
            path.write_bytes(b"managed assembly payload")
            with self.subTest(payload=name), patch.object(VALIDATION, "run"), patch.object(VALIDATION, "check_native_graph"), patch.object(VALIDATION, "source_identity", return_value=self.source), patch.object(VALIDATION.Path, "read_text", return_value="{}"), patch.object(VALIDATION.shutil, "copyfile"), self.assertRaises(RuntimeError):
                VALIDATION.publish_pair(self.args, {}, self.work, self.source)
            path.unlink()

    def test_report_binds_installed_command_hashes(self):
        installed = self.work / "installed-tool"
        installed.write_bytes(b"installed executable")
        report = self.report | {"native_sha256": hashlib.sha256(installed.read_bytes()).hexdigest()}
        with self.assertRaises(RuntimeError):
            VALIDATION.check_report(report, self.args, self.source)
        VALIDATION.check_report(report, self.args, self.source, {"native": installed, "managed": self.work / "managed/gitversion"})
        installed.write_bytes(b"changed after installation")
        with self.assertRaises(RuntimeError):
            VALIDATION.check_report(report, self.args, self.source, {"native": installed, "managed": self.work / "managed/gitversion"})

    def test_rejects_mismatched_identity_and_binaries(self):
        VALIDATION.check_report(self.report, self.args, self.source)
        for key in ("source_sha", "framework", "rid", "runtime_version", "native_sha256", "managed_sha256"):
            with self.subTest(field=key):
                report = self.report | {key: "mismatch"}
                with self.assertRaises(RuntimeError):
                    VALIDATION.check_report(report, self.args, self.source)

    def test_requires_complete_unique_successful_cases(self):
        variants = [self.report | {"cases": self.report["cases"][:-1]},
                    self.report | {"cases": self.report["cases"][:-1] + [self.report["cases"][0]]},
                    self.report | {"cases": self.report["cases"][:1] + [{"case": "failed-case", "passed": False}] + self.report["cases"][2:]},
                    self.report | {"cases": [{"case": "missing-history", "passed": True}] + self.report["cases"][1:]},
                    self.report | {"passed": 98}, self.report | {"failed": 1}]
        for index, report in enumerate(variants):
            with self.subTest(variant=index), self.assertRaises(RuntimeError):
                VALIDATION.check_report(report, self.args, self.source)

    def test_native_closure_excludes_backend_and_pins_compiler(self):
        assets = {"libraries": {"Microsoft.DotNet.ILCompiler/10.0.13": {}, "runtime.linux-arm64.Microsoft.DotNet.ILCompiler/10.0.13": {}},
                  "targets": {}, "project": {"restore": {"frameworks": {}}}}
        VALIDATION.check_native_graph(assets, "10.0.13", "linux-arm64")
        for section in (assets["libraries"], assets["targets"], assets["project"]["restore"]["frameworks"]):
            section["LibGit2Sharp/0.32"] = {}
            with self.assertRaises(RuntimeError):
                VALIDATION.check_native_graph(assets, "10.0.13", "linux-arm64")
            del section["LibGit2Sharp/0.32"]
        with self.assertRaises(RuntimeError):
            VALIDATION.check_native_graph(assets, "10.0.14", "linux-arm64")

    def test_target_host_matches_os_architecture_and_libc(self):
        with patch.object(VALIDATION.platform, "system", return_value="Linux"), patch.object(VALIDATION.platform, "machine", return_value="aarch64"):
            with patch.object(VALIDATION.subprocess, "run", return_value=SimpleNamespace(stdout=b"glibc", stderr=b"")):
                VALIDATION.require_target_host("linux-arm64")
                for rid in ("win-arm64", "linux-x64", "linux-musl-arm64"):
                    with self.subTest(rid=rid), self.assertRaises(RuntimeError):
                        VALIDATION.require_target_host(rid)
            with patch.object(VALIDATION.subprocess, "run", return_value=SimpleNamespace(stdout=b"", stderr=b"musl libc")):
                VALIDATION.require_target_host("linux-musl-arm64")
                with self.assertRaises(RuntimeError):
                    VALIDATION.require_target_host("linux-arm64")

    def test_stderr_does_not_corrupt_json(self):
        output = self.work / "parity.json"
        VALIDATION.run([sys.executable, "-c", 'import sys; print(\'{"passed":true}\'); print("diagnostic",file=sys.stderr)'], dict(os.environ), output)
        self.assertEqual({"passed": True}, json.loads(output.read_text()))
        self.assertEqual("diagnostic", output.with_name("parity-stderr.log").read_text().strip())

    def test_invalid_report_writes_failure_evidence(self):
        for index, (invalid, kind) in enumerate([({}, "KeyError"), ([], "TypeError")]):
            output = self.work / f"invalid-{index}"
            arguments = ["validate", "--framework", "net10.0", "--rid", "linux-arm64", "--runtime-version", "10.0.13", "--output", str(output)]

            def run(_command, _env, log, invalid=invalid):
                if log.name == "parity.json":
                    log.write_text(json.dumps(invalid))

            with patch.object(sys, "argv", arguments), patch.object(VALIDATION, "require_target_host"), patch.object(VALIDATION, "source_identity", return_value=self.source), patch.object(VALIDATION, "publish_pair"), patch.object(VALIDATION, "inspect_dependencies"), patch.object(VALIDATION, "run", side_effect=run), redirect_stdout(io.StringIO()):
                self.assertEqual(1, VALIDATION.main())
            result = json.loads((output / "evidence/platform.json").read_text())
            self.assertIs(False, result["passed"])
            self.assertTrue(result["error"].startswith(kind + ":"))

    def test_existing_history_preserves_remote_only_branches(self):
        env = PARITY.isolated_environment()
        git = partial(PARITY.run_git, env)
        source = PARITY.create_repository(self.work, git, "source", True)
        main = git(source, "rev-parse", "HEAD")
        git(source, "checkout", "-b", "feature/validation")
        git(source, "commit", "--allow-empty", "-m", "feature history")
        feature = git(source, "rev-parse", "HEAD")
        git(source, "update-ref", "refs/remotes/origin/main", main)
        git(source, "update-ref", "refs/remotes/origin/feature/validation", feature)
        git(source, "checkout", "--detach", "HEAD")
        git(source, "branch", "-D", "main", "feature/validation")
        cases = []

        def compare(name, arguments):
            cases.append(name)
            self.assertEqual(main, git(arguments[0], "rev-parse", "refs/remotes/origin/main"))
            self.assertEqual(feature, git(arguments[0], "rev-parse", "refs/remotes/origin/feature/validation"))

        PARITY.compare_existing_history(SimpleNamespace(repository=source), self.work, env, git, compare, [])
        self.assertEqual(["existing-history-json", "existing-history-config"], cases)
        self.assertEqual("", git(source, "for-each-ref", "--format=%(refname)", "refs/heads"))


class DistributionValidationTests(unittest.TestCase):
    def test_windows_batch_parity_quotes_pipe_format_as_one_argument(self):
        arguments = ["C:/native-tools/dotnet-gitversion.cmd", "--format", "{Major:0000}|{CommitDate:MMMM}|{FullSemVer:u}"]
        command = PARITY.process_command(arguments, windows=True)
        self.assertIn('"{Major:0000}|{CommitDate:MMMM}|{FullSemVer:u}"', command)
        self.assertEqual(arguments, PARITY.process_command(arguments, windows=False))
        executable = ["C:/native-tools/gitversion.exe", *arguments[1:]]
        self.assertEqual(executable, PARITY.process_command(executable, windows=True))

    def test_configuration_report_checks_file_identity_and_exact_contract(self):
        config = self.work / "GitVersion.yml"
        config.write_text("workflow: GitHubFlow/v1\n")
        nested = self.work / "nested"
        nested.mkdir()
        report = {"schemaVersion": 1, "valid": True, "configurationVersion": "v7", "file": str(nested / ".." / config.name), "diagnostics": []}
        TOOLS.SMOKE.check_configuration_report(report, config)
        other = self.work / "other.yml"
        other.write_text(config.read_text())
        for invalid in (report | {"file": str(other)}, report | {"valid": False}, report | {"unexpected": True}):
            with self.assertRaises(AssertionError):
                TOOLS.SMOKE.check_configuration_report(invalid, config)

    def test_nonroot_tools_execute_as_their_installer_without_relaxing_permissions(self):
        self.assertEqual("1001:1001", CONTAINERS.execution_user("65532:65532", "sdk0-path", False, 1001, 1001))
        self.assertEqual("65532:65532", CONTAINERS.execution_user("65532:65532", "standalone", False, 1001, 1001))
        self.assertEqual("0:0", CONTAINERS.execution_user("0:0", "sdk0-path", False, 1001, 1001))
        self.assertEqual("ContainerUser", CONTAINERS.execution_user("ContainerUser", "sdk0-path", True, 0, 0))
        with self.assertRaises(RuntimeError):
            CONTAINERS.execution_user("65532:65532", "sdk0-path", False, 0, 0)

    def test_windows_batch_dispatcher_binds_native_payload_and_rejects_other_commands(self):
        payload = self.work / ".store/gitversion.tool/fixture/gitversion.exe"
        payload.parent.mkdir(parents=True)
        payload.write_bytes(b"native-payload")
        shim = self.work / "dotnet-gitversion.cmd"
        shim.write_text('@echo off\n"%~dp0.store/gitversion.tool/fixture/gitversion.exe" %*\n')
        self.assertEqual(payload.resolve(), TOOLS.SMOKE.native_payload(shim))
        for content in ('@echo off\n"%~dp0../gitversion.exe" %*\n', '@echo off\ndotnet gitversion.dll %*\n',
                        '@echo off\n"%~dp0.store/gitversion.tool/fixture/gitversion.exe" %*\nextra-command\n'):
            shim.write_text(content)
            with self.assertRaises(RuntimeError):
                TOOLS.SMOKE.native_payload(shim)

    def test_linux_tool_install_uses_native_sdk_without_unsupported_arch_option(self):
        for rid in ("linux-x64", "linux-arm64", "linux-musl-x64", "linux-musl-arm64"):
            args = SimpleNamespace(dotnet="dotnet", rid=rid)
            self.assertNotIn("--arch", TOOLS.installation_command(args, "0.0.0-test", self.work / "NuGet.Config"))
        for rid, architecture in (("win-x64", "x64"), ("win-arm64", "arm64"), ("osx-arm64", "arm64")):
            command = TOOLS.installation_command(SimpleNamespace(dotnet="dotnet", rid=rid), "0.0.0-test", self.work / "NuGet.Config")
            self.assertEqual(architecture, command[command.index("--arch") + 1])

    def test_capture_gate_requires_all_native_checks_and_credential_request(self):
        report = {"Passed": True, "Checks": sorted(CAPTURE.EXPECTED_CHECKS), "Error": None}
        CAPTURE.check_report(report, 1)
        for invalid in (report | {"Passed": False}, report | {"Checks": report["Checks"][:-1]},
                        report | {"Checks": [report["Checks"][0]] * len(report["Checks"])}, report | {"Error": "failure"}):
            with self.assertRaises(RuntimeError):
                CAPTURE.check_report(invalid, 1)
        with self.assertRaises(RuntimeError):
            CAPTURE.check_report(report, 0)

    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.work = Path(self.temporary.name)

    def packages(self, mapping="linux-arm64", runner="executable", revision="revision", extra=None):
        version, rid = "0.0.0-test", "linux-arm64"
        for identity in ("GitVersion.Tool", "GitVersion.Tool.linux-arm64"):
            with zipfile.ZipFile(self.work / f"{identity}.{version}.nupkg", "w") as archive:
                archive.writestr(identity + ".nuspec", f'<package><metadata><id>{identity}</id><version>{version}</version><repository commit="{revision}"/></metadata></package>')
                if identity == "GitVersion.Tool":
                    settings = f'<DotNetCliTool><Commands><Command Name="dotnet-gitversion"/></Commands><RuntimeIdentifierPackages><RuntimeIdentifierPackage RuntimeIdentifier="{mapping}" Id="GitVersion.Tool.{mapping}"/></RuntimeIdentifierPackages></DotNetCliTool>'
                    archive.writestr("tools/any/any/DotnetToolSettings.xml", settings)
                else:
                    settings = f'<DotNetCliTool><Commands><Command Name="dotnet-gitversion" Runner="{runner}" EntryPoint="gitversion"/></Commands></DotNetCliTool>'
                    archive.writestr(f"tools/any/{rid}/DotnetToolSettings.xml", settings)
                    archive.writestr(f"tools/any/{rid}/gitversion", b"native payload")
                    if extra:
                        archive.writestr(extra, b"forbidden payload")
        return TOOLS.package_contract(self.work, version, rid, "revision")

    def test_tool_package_requires_matching_rid_source_and_native_runner(self):
        metadata = self.packages()
        self.assertIn({"payload_sha256": hashlib.sha256(b"native payload").hexdigest()}, metadata)
        for invalid in ({"mapping": "linux-x64"}, {"runner": "dotnet"}, {"revision": "different-source"},
                        {"extra": "tools/any/linux-arm64/Other.DLL"}, {"extra": "tools/any/linux-arm64/libgit2.so"}):
            with self.subTest(invalid=invalid), self.assertRaises(RuntimeError):
                self.packages(**invalid)

    def test_native_packages_cannot_resolve_from_public_feed(self):
        config = self.work / "NuGet.Config"
        TOOLS.feed_config(self.work, config, "win-arm64")
        root = ET.parse(config).getroot()
        mappings = {source.get("key"): {package.get("pattern") for package in source} for source in root.findall("packageSourceMapping/packageSource")}
        self.assertEqual({"local": {"GitVersion.Tool", "GitVersion.Tool.win-arm64"}, "microsoft-host": {"Microsoft.NETCore.App.Host.*"}}, mappings)
        self.assertEqual(str(self.work), root.find("packageSources/add[@key='local']").get("value"))

    def test_container_report_requires_exact_payload_user_and_complete_checks(self):
        report = {"passed": True, "dotnet_installed": False, "native_sha256": "expected", "machine": "aarch64", "system": "Linux", "uid": 65532, "checks": sorted(CONTAINERS.SMOKE_CASES)}
        CONTAINERS.check_report(report, "expected", "linux-arm64", "65532:65532")
        for invalid in ({"native_sha256": "other"}, {"dotnet_installed": True}, {"machine": "x86_64"}, {"system": "Windows"}, {"uid": 0},
                        {"checks": report["checks"][:-1]}, {"checks": [report["checks"][0]] * 6}):
            with self.subTest(invalid=invalid), self.assertRaises(RuntimeError):
                CONTAINERS.check_report(report | invalid, "expected", "linux-arm64", "65532:65532")
        windows = report | {"machine": "AMD64", "system": "Windows", "uid": None, "user": "user manager\\ContainerUser"}
        CONTAINERS.check_report(windows, "expected", "win-x64", "ContainerUser")
        with self.assertRaises(RuntimeError):
            CONTAINERS.check_report(windows, "expected", "win-x64", "ContainerAdministrator")

    def test_container_gate_requires_matching_inputs_and_both_sdk_installers(self):
        platform_report = {"passed": True, "source": {"sha": "revision"}, "framework": "net10.0", "rid": "linux-arm64", "runtime_version": "10.0.13"}
        modes = [{"mode": mode, "execution": {"passed": True, "checks": sorted(CONTAINERS.SMOKE_CASES)}} for mode in ("path", "global", "manifest")]
        tools = platform_report | {"installers": [{"sdk": sdk, "parity_cases": 99, "installations": modes} for sdk in ("10.0.402", "11.0.100-test")]}
        CONTAINERS.validate_gates(platform_report, tools, "11.0.100-test")
        for invalid in ({"source": {"sha": "other"}}, {"rid": "linux-x64"}, {"framework": "net11.0"}, {"installers": tools["installers"][:1]},
                        {"installers": [tools["installers"][0]] * 2}, {"installers": [tools["installers"][0] | {"installations": modes[:-1]}, tools["installers"][1]]}):
            with self.subTest(invalid=invalid), self.assertRaises(RuntimeError):
                CONTAINERS.validate_gates(platform_report, tools | invalid, "11.0.100-test")

    def test_runtime_images_follow_framework_specific_os_policy(self):
        net10 = set(CONTAINERS.runtime_images("net10.0", "linux-arm64"))
        net11 = set(CONTAINERS.runtime_images("net11.0", "linux-arm64"))
        self.assertEqual({"registry.fedoraproject.org/fedora:43"}, net10 - net11)
        self.assertEqual({"registry.fedoraproject.org/fedora:45"}, net11 - net10)
        self.assertEqual(15, len(net10))
        self.assertEqual(net10, set(CONTAINERS.runtime_images("net10.0", "linux-x64")))
        self.assertEqual({"alpine:3.21", "alpine:3.22", "alpine:3.23", "alpine:3.24"}, set(CONTAINERS.runtime_images("net10.0", "linux-musl-arm64")))
        self.assertEqual({"alpine:3.23", "alpine:3.24"}, set(CONTAINERS.runtime_images("net11.0", "linux-musl-x64")))
        self.assertEqual(["mcr.microsoft.com/windows/servercore:ltsc2022", "mcr.microsoft.com/windows/servercore:ltsc2025"], CONTAINERS.runtime_images("net10.0", "win-x64"))

    def test_container_mount_preserves_installed_tool_absolute_paths(self):
        directory = self.work / "tool-validation"
        command = directory / "sdk-10/tools/dotnet-gitversion"
        arguments = CONTAINERS.container_command(self.work, "runtime-check", "65532:65532", command, False)
        self.assertIn(f"{directory}:{directory}:ro", arguments)
        self.assertEqual(["--native", str(command)], arguments[-2:])

    def test_windows_output_requires_container_system_drive(self):
        from pathlib import PureWindowsPath
        CONTAINERS.require_windows_output(PureWindowsPath("C:/nativeaot-validation/net10"))
        with self.assertRaises(RuntimeError):
            CONTAINERS.require_windows_output(PureWindowsPath("D:/a/_temp/nativeaot"))

    def test_runtime_detection_distinguishes_ipc_state_from_installed_sdk(self):
        home = self.work / "home"
        ipc = home / ".dotnet/shm"
        ipc.mkdir(parents=True)
        self.assertIs(False, TOOLS.SMOKE.contains_runtime(home / ".dotnet"))
        (home / ".dotnet/sdk").mkdir()
        self.assertIs(True, TOOLS.SMOKE.contains_runtime(home / ".dotnet"))

    def test_hosted_isolation_requires_explicit_disposable_macos_context(self):
        enabled = {"GITHUB_ACTIONS": "true", "NATIVEAOT_EPHEMERAL_RUNNER": "github-hosted"}
        ISOLATION.require_disposable_runner("Darwin", enabled)
        for system, environment in [("Windows", enabled), ("Darwin", {}), ("Darwin", enabled | {"NATIVEAOT_EPHEMERAL_RUNNER": "self-hosted"}), ("Darwin", enabled | {"GITHUB_ACTIONS": "false"})]:
            with self.subTest(system=system, environment=environment), self.assertRaises(RuntimeError):
                ISOLATION.require_disposable_runner(system, environment)

    def test_hosted_isolation_restores_sdk_after_execution_and_move_failures(self):
        paths = [self.work / name for name in ("sdk", "shared")]
        for path in paths:
            path.mkdir()
            (path / "payload").write_text(path.name)

        def success():
            self.assertTrue(all(not path.exists() for path in paths))
            return "native checks passed"

        self.assertEqual("native checks passed", ISOLATION.with_quarantined(paths, success))
        self.assertEqual(["sdk", "shared"], [(path / "payload").read_text() for path in paths])

        def execute():
            self.assertTrue(all(not path.exists() for path in paths))
            raise RuntimeError("native execution failed")

        with self.assertRaisesRegex(RuntimeError, "native execution failed"):
            ISOLATION.with_quarantined(paths, execute)
        self.assertEqual(["sdk", "shared"], [(path / "payload").read_text() for path in paths])
        calls = []

        def move(source, destination):
            calls.append(source)
            if len(calls) == 2:
                raise PermissionError("second move failed")
            source.rename(destination)

        with self.assertRaisesRegex(PermissionError, "second move failed"):
            ISOLATION.with_quarantined(paths, lambda: self.fail("execution ran after failed isolation"), move)
        self.assertEqual(["sdk", "shared"], [(path / "payload").read_text() for path in paths])


if __name__ == "__main__":
    unittest.main(verbosity=2)

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


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


VALIDATION = load("validation", "nativeaot-platform-validation.py")
PARITY = load("parity", "nativeaot-parity.py")


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
        self.report = {"source_sha": self.source["sha"], "framework": self.args.framework, "rid": self.args.rid, "runtime_version": self.args.runtime_version,
                       "native_sha256": hashlib.sha256(b"native").hexdigest(), "managed_sha256": hashlib.sha256(b"managed").hexdigest(),
                       "passed": 99, "failed": 0, "cases": [{"case": "existing-history-json" if i == 0 else f"case-{i}", "passed": True} for i in range(99)]}

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
            self.assertIs(result["passed"], False)
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


if __name__ == "__main__":
    unittest.main(verbosity=2)

#!/usr/bin/env python3
"""Independent native execution checks in a Linux container without .NET."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from functools import partial


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--native", type=Path, required=True)
    args = parser.parse_args()
    native = args.native.resolve()
    if shutil.which("dotnet") or any(os.path.exists(path) for path in ["/usr/share/dotnet", "/usr/local/share/dotnet", "/root/.dotnet"]):
        raise RuntimeError("This check requires an image without an installed .NET runtime/SDK")
    spec = importlib.util.spec_from_file_location("parity", Path(__file__).with_name("nativeaot-parity.py"))
    parity = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(parity)
    env = parity.isolated_environment()
    env.pop("GITVERSION_GIT_BACKEND", None)
    checks = []
    with tempfile.TemporaryDirectory() as temp:
        work = Path(temp)
        git = partial(parity.run_git, env)
        repo = parity.create_repository(work, git, "fixture", True)
        git(repo, "checkout", "--detach", "1.2.3")
        for option in ["--version", "--help", "--cli-schema"]:
            run = subprocess.run([str(native), option], env=env, capture_output=True, timeout=60)
            require(run.returncode == 0 and run.stdout.strip(), f"startup failed: {option}")
            checks.append("startup-" + option[2:])
        log = work / "calculation.log"
        run = subprocess.run([str(native), str(repo), "--no-cache", "--no-fetch", "--log-file", str(log)], env=env, capture_output=True, timeout=60)
        diagnostic = run.stderr.decode() + (log.read_text(encoding="utf-8") if log.exists() else "")
        require(run.returncode == 0, diagnostic)
        require(json.loads(run.stdout)["FullSemVer"] == "1.2.3", "tagged history calculation changed")
        checks.append("tagged-native-default")
        config = work / "GitVersion.yml"
        config.write_text(parity.GITHUB_WORKFLOW, encoding="utf-8")
        run = subprocess.run([str(native), "config", "validate", "--config", str(config), "--format", "json"], env=env, capture_output=True, timeout=60)
        require(run.returncode == 0, "configuration validation failed")
        validation = json.loads(run.stdout)
        require(validation == {"schemaVersion": 1, "valid": True, "configurationVersion": "v7", "file": str(config), "diagnostics": []}, "configuration validation contract changed")
        checks.append("configuration-validation")
        run = subprocess.run([str(native), "--version"], env=env | {"GITVERSION_GIT_BACKEND": "libgit2"}, capture_output=True, timeout=60)
        require(run.returncode == 1 and b"only the managed Git backend" in run.stderr, "unsupported backend was not rejected")
        checks.append("unavailable-backend-rejected")
    print(json.dumps({"passed": True, "dotnet_installed": False, "uid": os.geteuid(), "native_sha256": hashlib.sha256(native.read_bytes()).hexdigest(), "checks": checks}))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Independent native execution checks, also reusable for installed tools."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import tempfile
from functools import partial


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def contains_runtime(path):
    return any(os.path.exists(str(Path(path) / item)) for item in ("dotnet", "dotnet.exe", "sdk", "shared"))


def operating_system():
    release = Path("/etc/os-release")
    if release.is_file():
        pairs = (line.split("=", 1) for line in release.read_text(encoding="utf-8").splitlines() if "=" in line)
        fields = dict(pairs)
        return {key: fields.get(key, "").strip("\"'") for key in ("ID", "VERSION_ID")}
    return {"release": platform.release(), "version": platform.version()}


def runtime_roots():
    candidates = ["/usr/share/dotnet", "/usr/local/share/dotnet", str(Path.home() / ".dotnet")]
    candidates.extend(value for key, value in os.environ.items() if key.upper().startswith("DOTNET_ROOT"))
    for variable in ("ProgramFiles", "ProgramFiles(x86)"):
        if os.environ.get(variable):
            candidates.append(str(Path(os.environ[variable]) / "dotnet"))
    executable = shutil.which("dotnet")
    if executable:
        candidates.append(str(Path(executable).resolve().parent))
    return sorted({path for path in candidates if contains_runtime(path)})


def native_payload(command):
    if command.suffix.lower() != ".cmd":
        return command.resolve()
    # The SDK's executable runner uses a relative batch dispatcher on Windows.
    match = re.fullmatch(r'@echo off\n"%~dp0([^"\n]+)" %\*\n', command.read_text(encoding="utf-8"))
    if match is None:
        raise RuntimeError("Unexpected native tool batch dispatcher")
    payload = (command.parent / match[1]).resolve()
    if not payload.is_relative_to((command.parent / ".store").resolve()) or payload.name != "gitversion.exe":
        raise RuntimeError("Native tool batch dispatcher escapes its installed payload")
    return payload


def check_configuration_report(report, config):
    filename = report.get("file")
    expected = {"schemaVersion": 1, "valid": True, "configurationVersion": "v7", "file": filename, "diagnostics": []}
    require(report == expected and isinstance(filename, str), f"Configuration validation contract changed: {report!r}")
    # Windows may return a canonical path for a temporary directory's 8.3 alias.
    require(Path(filename).samefile(config), f"Configuration validation selected a different file: {report!r}; expected {config}")


def check_execution(native, command=None, cwd=None, env=None):
    command = command or [str(native.absolute())]
    native = native_payload(native)
    spec = importlib.util.spec_from_file_location("parity", Path(__file__).with_name("nativeaot-parity.py"))
    parity = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(parity)
    env = dict(env) if env is not None else parity.isolated_environment()
    env.pop("GITVERSION_GIT_BACKEND", None)
    checks = []
    with tempfile.TemporaryDirectory() as temp:
        work = Path(temp)
        git = partial(parity.run_git, env)
        repo = parity.create_repository(work, git, "fixture", True)
        git(repo, "checkout", "--detach", "1.2.3")
        for option in ["--version", "--help", "--cli-schema"]:
            run = subprocess.run(command + [option], cwd=cwd, env=env, capture_output=True, timeout=60)
            require(run.returncode == 0 and run.stdout.strip(),
                    f"startup failed: {option}; exit={run.returncode}; stdout={run.stdout!r}; stderr={run.stderr!r}")
            checks.append("startup-" + option[2:])
        log = work / "calculation.log"
        run = subprocess.run(command + [str(repo), "--no-cache", "--no-fetch", "--log-file", str(log)], cwd=cwd, env=env, capture_output=True, timeout=60)
        diagnostic = run.stderr.decode() + (log.read_text(encoding="utf-8") if log.exists() else "")
        require(run.returncode == 0, diagnostic)
        require(json.loads(run.stdout)["FullSemVer"] == "1.2.3", "tagged history calculation changed")
        checks.append("tagged-native-default")
        config = work / "GitVersion.yml"
        config.write_text(parity.GITHUB_WORKFLOW, encoding="utf-8")
        run = subprocess.run(command + ["config", "validate", "--config", str(config), "--format", "json"], cwd=cwd, env=env, capture_output=True, timeout=60)
        require(run.returncode == 0, "configuration validation failed")
        validation = json.loads(run.stdout)
        check_configuration_report(validation, config)
        checks.append("configuration-validation")
        run = subprocess.run(command + ["--version"], cwd=cwd, env=env | {"GITVERSION_GIT_BACKEND": "libgit2"}, capture_output=True, timeout=60)
        require(run.returncode == 1 and b"only the managed Git backend" in run.stderr, "unsupported backend was not rejected")
        checks.append("unavailable-backend-rejected")
    user = subprocess.check_output(["whoami"], text=True, timeout=30).strip() if os.name == "nt" else None
    return {"passed": True, "uid": os.geteuid() if hasattr(os, "geteuid") else None,
            "user": user, "system": platform.system(), "machine": platform.machine(), "os": operating_system(),
            "native_sha256": hashlib.sha256(native.read_bytes()).hexdigest(), "checks": checks}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--native", type=Path, required=True)
    args = parser.parse_args()
    roots = runtime_roots()
    if shutil.which("dotnet") or roots:
        raise RuntimeError(f"This check requires an environment without an installed .NET runtime/SDK: {roots}")
    report = check_execution(args.native)
    report["dotnet_installed"] = False
    print(json.dumps(report))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Exercise native commands with shared .NET unavailable on a disposable macOS runner.

This is runtime-resolution isolation, not a clean macOS installation claim.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import uuid


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


SMOKE = load("smoke", "nativeaot-smoke.py")
PARITY = load("parity", "nativeaot-parity.py")
CONTAINERS = load("containers", "nativeaot-container-validation.py")


def require_disposable_runner(system, environment):
    if system != "Darwin" or environment.get("GITHUB_ACTIONS") != "true" or environment.get("NATIVEAOT_EPHEMERAL_RUNNER") != "github-hosted":
        raise RuntimeError("SDK isolation is restricted to the explicitly selected disposable macOS runner")


def move_path(source, destination):
    try:
        source.rename(destination)
    except PermissionError:
        subprocess.run(["sudo", "-n", "/bin/mv", str(source), str(destination)], check=True, timeout=60)


def with_quarantined(paths, execute, move=move_path):
    moved = []
    try:
        for source in paths:
            destination = source.with_name(source.name + ".nativeaot-unavailable-" + uuid.uuid4().hex)
            move(source, destination)
            moved.append((source, destination))
        return execute()
    finally:
        errors = []
        for source, destination in reversed(moved):
            try:
                move(destination, source)
            except (OSError, subprocess.SubprocessError) as error:
                errors.append(f"{source}: {error}")
        if errors:
            raise RuntimeError("Failed to restore hosted SDK contents: " + "; ".join(errors))


def execute_commands(output, tools):
    if shutil.which("dotnet") or SMOKE.runtime_roots():
        raise RuntimeError("A shared .NET installation is still available")
    env = {key: value for key, value in PARITY.isolated_environment().items()
           if not key.upper().startswith(("DOTNET_", "COREHOST_", "COMPLUS_", "DYLD_"))}
    native_hash = json.loads((output / "evidence/parity.json").read_text(encoding="utf-8"))["native_sha256"]
    reports = []
    for name, source, _destination, expected in CONTAINERS.payloads(output, tools, False, native_hash):
        report = SMOKE.check_execution(source, env=env)
        if report["native_sha256"] != expected or len(report["checks"]) != 6 or set(report["checks"]) != CONTAINERS.SMOKE_CASES:
            raise RuntimeError("Runtime isolation selected an unexpected or incompletely tested payload")
        reports.append({"mode": name, "execution": report})
    return reports


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform-output", type=Path, required=True)
    args = parser.parse_args()
    output = args.platform_output.resolve()
    evidence = output / "evidence"
    try:
        require_disposable_runner(platform.system(), os.environ)
        validated = json.loads((evidence / "platform.json").read_text(encoding="utf-8"))
        tools = json.loads((evidence / "tool-validation.json").read_text(encoding="utf-8"))
        sdk11 = json.loads((Path(__file__).resolve().parent.parent / "global.json").read_text(encoding="utf-8"))["sdk"]["version"]
        CONTAINERS.validate_gates(validated, tools, sdk11)
        env = PARITY.isolated_environment()
        if validated["rid"] != "osx-arm64" or CONTAINERS.TOOLS.VALIDATION.source_identity(env) != validated["source"]:
            raise RuntimeError("Isolation requires matching successful publication/tool gates")
        roots = sorted({Path(root).resolve() for root in SMOKE.runtime_roots()})
        parts = [root / name for root in roots for name in ("dotnet", "dotnet.exe", "sdk", "shared", "host", "packs", "templates", "sdk-manifests", "metadata")
                 if (root / name).exists()]
        if not parts:
            raise RuntimeError("Expected SDK/runtime contents were not found for isolation")
        # Keep the root itself: .NET 11's native mutex may create IPC state there.
        reports = with_quarantined(parts, lambda: execute_commands(output, tools))
        if CONTAINERS.TOOLS.VALIDATION.source_identity(env) != validated["source"]:
            raise RuntimeError("Source inputs changed during hosted runtime isolation")
        result = {"passed": True, "source": validated["source"], "scope": "known SDK/shared runtime contents unavailable; hosted OS unchanged",
                  "quarantined": [str(path) for path in parts], "restored": True, "executions": reports}
    except (OSError, ValueError, RuntimeError, AssertionError, KeyError, subprocess.SubprocessError) as error:
        result = {"passed": False, "error": f"{type(error).__name__}: {error}"}
    (evidence / "hosted-isolation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())

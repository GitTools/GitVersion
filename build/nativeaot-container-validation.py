#!/usr/bin/env python3
"""Execute validated native payloads in disposable containers without .NET."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys


ROOT = Path(__file__).resolve().parent.parent
SMOKE_CASES = {"startup-version", "startup-help", "startup-cli-schema", "tagged-native-default", "configuration-validation", "unavailable-backend-rejected"}
SPEC = importlib.util.spec_from_file_location("tools", Path(__file__).with_name("nativeaot-tool-validation.py"))
TOOLS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(TOOLS)


def runtime_images(framework, rid):
    if rid == "win-x64":
        return ["mcr.microsoft.com/windows/servercore:ltsc2022", "mcr.microsoft.com/windows/servercore:ltsc2025"]
    policy = json.loads(Path(__file__).with_name("nativeaot-runtime-images.json").read_text(encoding="utf-8"))
    libc = "musl" if "musl" in rid else "glibc"
    return (policy["glibc"] if libc == "glibc" else []) + policy[framework][libc]


def check_report(report, expected_hash, rid, user):
    architecture = {"aarch64": "arm64", "arm64": "arm64", "amd64": "x64", "x86_64": "x64"}.get(report["machine"].lower())
    expected_architecture = "arm64" if rid.endswith("arm64") else "x64"
    if report.get("passed") is not True or report.get("dotnet_installed") is not False or report["native_sha256"] != expected_hash:
        raise RuntimeError("Container execution failed or selected a different payload/runtime")
    if architecture != expected_architecture or report["system"] != ("Windows" if rid.startswith("win-") else "Linux"):
        raise RuntimeError("Container execution used the wrong OS/CPU")
    if len(report["checks"]) != len(SMOKE_CASES) or set(report["checks"]) != SMOKE_CASES:
        raise RuntimeError("Container did not complete all independent smoke checks")
    if rid.startswith("win-"):
        if str(report["user"]).rsplit("\\", 1)[-1].lower() != user.lower():
            raise RuntimeError("Container did not execute as the selected Windows user")
    elif report["uid"] != int(user.split(":")[0]):
        raise RuntimeError("Container did not execute as the selected UID")


def check_installer(installer):
    modes = installer["installations"]
    if installer["parity_cases"] != 99 or len(modes) != 3 or {mode["mode"] for mode in modes} != {"path", "global", "manifest"}:
        raise RuntimeError("Tool installation validation is incomplete")
    for mode in modes:
        if mode["execution"]["passed"] is not True or len(mode["execution"]["checks"]) != 6 or set(mode["execution"]["checks"]) != SMOKE_CASES:
            raise RuntimeError("Tool installation smoke checks are incomplete")


def validate_gates(validated, tools, sdk11):
    if not validated["passed"] or not tools["passed"] or any(tools[key] != validated[key] for key in ("source", "framework", "rid", "runtime_version")):
        raise RuntimeError("Container checks require matching successful publication/tool gates")
    installers = tools["installers"]
    if len(installers) != 2 or {item["sdk"] for item in installers} != {"10.0.402", sdk11}:
        raise RuntimeError("Container checks require both SDK installers")
    for installer in installers:
        check_installer(installer)


def payloads(output, tools, windows, native_hash):
    native_name = "gitversion.exe" if windows else "gitversion"
    reports = [("standalone", output / "native" / native_name, Path("C:/native" if windows else "/native") / native_name, native_hash)]
    for index, installer in enumerate(tools["installers"]):
        for mode in ("path", "global"):
            relative = installer[mode + "_command"]
            digest = next(item["execution"]["native_sha256"] for item in installer["installations"] if item["mode"] == mode)
            source = output / "tool-validation" / relative
            if not source.resolve().is_relative_to((output / "tool-validation").resolve()):
                raise RuntimeError("Installed tool path escapes its isolated directory")
            reports.append((f"sdk{index}-{mode}", source, source, digest))
    return reports


def run(command, log, timeout=1800):
    with log.open("wb") as stdout, log.with_name(log.stem + "-stderr.log").open("wb") as stderr:
        result = subprocess.run(command, cwd=ROOT, stdout=stdout, stderr=stderr, timeout=timeout)
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}); see {log}")


def require_windows_output(output):
    if output.drive.upper() != "C:":
        raise RuntimeError("Windows native tool validation must use C: to preserve installed paths in Server Core")


def container_command(output, image, user, executable, windows):
    command = ["docker", "run", "--rm", "--network", "none", "--user", str(user), "-e", "PYTHONUTF8=1", "-e", "PYTHONDONTWRITEBYTECODE=1"]
    if windows:
        require_windows_output(output)
        git_root = Path(shutil.which("git")).resolve().parent.parent
        python_root = Path(sys.executable).parent
        command += ["--isolation=process", "-v", f"{python_root}:C:/python:ro", "-v", f"{git_root}:C:/git:ro",
                    "-v", f"{output / 'native'}:C:/native:ro", "-v", f"{output / 'tool-validation'}:{output / 'tool-validation'}:ro", "-v", f"{ROOT / 'build'}:C:/validation:ro",
                    "-e", "PATH=C:\\python;C:\\git\\cmd;C:\\Windows\\System32;C:\\Windows", image,
                    "C:/python/python.exe", "C:/validation/nativeaot-smoke.py"]
    else:
        command += ["--read-only", "--tmpfs", "/tmp:rw,exec,size=512m", "-v", f"{output / 'native'}:/native:ro",
                    "-v", f"{output / 'tool-validation'}:{output / 'tool-validation'}:ro", "-v", f"{ROOT / 'build'}:/validation:ro", image,
                    "python3", "/validation/nativeaot-smoke.py"]
    return command + ["--native", str(executable)]


def validate_image(output, evidence, image, index, rid, tools, native_hash):
    windows = rid.startswith("win-")
    tag = image if windows else "nativeaot-runtime-check"
    if windows:
        run(["docker", "pull", image], evidence / f"runtime-build-{index}.log")
    else:
        run(["docker", "build", "-f", str(ROOT / "build/nativeaot-runtime.Dockerfile"), "--build-arg", f"BASE={image}", "-t", tag, str(ROOT)], evidence / f"runtime-build-{index}.log")
    inspect = json.loads(subprocess.check_output(["docker", "image", "inspect", tag], timeout=60))[0]
    identity = {"base": image, "image_id": inspect["Id"], "repo_digests": inspect["RepoDigests"], "architecture": inspect["Architecture"], "os": inspect["Os"], "os_version": inspect.get("OsVersion")}
    (evidence / f"runtime-image-{index}.json").write_text(json.dumps(identity, indent=2), encoding="utf-8")
    reports = []
    for user in (["ContainerAdministrator", "ContainerUser"] if windows else ["0:0", "65532:65532"]):
        for name, source, executable, expected in payloads(output, tools, windows, native_hash):
            if hashlib.sha256(TOOLS.SMOKE.native_payload(source).read_bytes()).hexdigest() != expected:
                raise RuntimeError("Container input differs from the validated executable")
            log = evidence / f"no-dotnet-image-{index}-{user.replace(':', '-')}-{name}.json"
            run(container_command(output, tag, user, executable, windows), log, timeout=180)
            report = json.loads(log.read_text(encoding="utf-8"))
            check_report(report, expected, rid, user)
            reports.append({"user": user, "payload": name, "checks": len(report["checks"])})
    return identity | {"passed": True, "executions": reports}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform-output", type=Path, required=True)
    args = parser.parse_args()
    output = args.platform_output.resolve()
    evidence = output / "evidence"
    results = []
    try:
        platform_report = json.loads((evidence / "platform.json").read_text(encoding="utf-8"))
        tools = json.loads((evidence / "tool-validation.json").read_text(encoding="utf-8"))
        sdk11 = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))["sdk"]["version"]
        validate_gates(platform_report, tools, sdk11)
        env = TOOLS.PARITY.isolated_environment()
        if TOOLS.VALIDATION.source_identity(env) != platform_report["source"]:
            raise RuntimeError("Container inputs differ from the validated source")
        rid = platform_report["rid"]
        if not (rid.startswith("linux-") or rid == "win-x64") or (platform.system() == "Windows") != rid.startswith("win-"):
            raise RuntimeError("Container validation requires the corresponding Linux or Windows x64 host")
        native_hash = json.loads((evidence / "parity.json").read_text(encoding="utf-8"))["native_sha256"]
        for index, image in enumerate(runtime_images(platform_report["framework"], rid)):
            try:
                results.append(validate_image(output, evidence, image, index, rid, tools, native_hash))
            except (OSError, ValueError, RuntimeError, KeyError, subprocess.SubprocessError) as error:
                results.append({"base": image, "passed": False, "error": f"{type(error).__name__}: {error}"})
        if TOOLS.VALIDATION.source_identity(env) != platform_report["source"]:
            raise RuntimeError("Source inputs changed during container validation")
        result = {"passed": bool(results) and all(item["passed"] for item in results), "source": platform_report["source"], "images": results}
    except (OSError, ValueError, RuntimeError, KeyError, subprocess.SubprocessError) as error:
        result = {"passed": False, "error": f"{type(error).__name__}: {error}", "images": results}
    (evidence / "container-validation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())

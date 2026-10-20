#!/usr/bin/env python3
"""Build and execute the NativeAOT parity gate on its actual target host.

This validation entry point creates evidence, never release packages.
"""
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
WINDOWS_EXECUTABLE = "gitversion.exe"
EXPECTED_CASES = frozenset(json.loads(Path(__file__).with_name("nativeaot-cases.json").read_text(encoding="utf-8")))


def run(command, env, log):
    diagnostic = log.with_name(log.stem + "-stderr.log")
    with log.open("wb") as output, diagnostic.open("wb") as errors:
        result = subprocess.run(command, cwd=ROOT, env=env, stdout=output, stderr=errors, timeout=1800)
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}); see {log}")


def source_identity(env):
    head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, env=env, timeout=60).decode().strip()
    paths = subprocess.check_output(["git", "ls-files", "--cached", "--others", "--exclude-standard", "--", "src", "build", "global.json", ".editorconfig", ".globalconfig", "NuGet.Config", "nuget.config", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", ".config", "LICENSE", ".github/workflows/nativeaot-validation.yml"], cwd=ROOT, env=env, timeout=60).decode().splitlines()
    digest = hashlib.sha256()
    for filename in sorted(set(paths)):
        path = ROOT / filename
        digest.update(filename.encode() + b"\0")
        digest.update(path.read_bytes() if path.is_file() else b"<deleted>")
    return {"sha": head, "inputs_sha256": digest.hexdigest()}


def require_target_host(rid):
    operating_system = {"win": "Windows", "linux": "Linux", "osx": "Darwin"}[rid.split("-")[0]]
    architecture = "arm64" if rid.endswith("arm64") else "x64"
    machine = platform.machine().lower()
    actual = {"aarch64": "arm64", "arm64": "arm64", "amd64": "x64", "x86_64": "x64"}.get(machine)
    if platform.system() != operating_system or actual != architecture:
        raise RuntimeError(f"{rid} requires its target host; found {platform.system()}/{machine}")
    if operating_system == "Linux":
        libc = subprocess.run(["ldd", "--version"], capture_output=True, timeout=60)
        is_musl = b"musl" in (libc.stdout + libc.stderr).lower()
        if is_musl != ("musl" in rid):
            raise RuntimeError(f"{rid} does not match the host libc")


def check_native_graph(assets, runtime_version, rid):
    for section in (assets["libraries"], assets["targets"], assets["project"]["restore"]["frameworks"]):
        if "libgit2" in json.dumps(section).lower():
            raise RuntimeError("LibGit2Sharp/libgit2 remains in the resolved native graph")
    for package in (f"Microsoft.DotNet.ILCompiler/{runtime_version}", f"runtime.{rid}.Microsoft.DotNet.ILCompiler/{runtime_version}"):
        if package not in assets["libraries"]:
            raise RuntimeError(f"Missing requested native compiler: {package}")


def publish_pair(args, env, evidence, initial):
    common = [args.dotnet, "publish", str(ROOT / "src/GitVersion.App/GitVersion.App.csproj"), "-c", "Release", "-f", args.framework,
              "-r", args.rid, "--self-contained", "true", f"-p:TargetFrameworks={args.framework}", f"-p:RuntimeFrameworkVersion={args.runtime_version}",
              "-p:RollForward=Disable", "-p:PackAsTool=false"]
    native = args.output / "native"
    run(common + ["-o", str(native), "-p:GitVersionManagedOnly=true", "-p:PublishAot=true", "-p:TrimmerSingleWarn=false", "-p:ILLinkTreatWarningsAsErrors=true", "-p:IlcTreatWarningsAsErrors=true"], env, evidence / "native-publish.log")
    assets_path = ROOT / "src/GitVersion.App/obj/project.assets.json"
    assets = json.loads(assets_path.read_text(encoding="utf-8"))
    check_native_graph(assets, args.runtime_version, args.rid)
    shutil.copyfile(assets_path, evidence / "native-project.assets.json")
    if any("libgit2" in path.name.lower() or path.suffix.lower() == ".dll" for path in native.rglob("*")):
        raise RuntimeError("Native output includes a forbidden managed/backend payload")
    if source_identity(env) != initial:
        raise RuntimeError("Source inputs changed during native publication")
    run(common + ["-o", str(args.output / "managed"), "-p:GitVersionManagedOnly=false", "-p:PublishAot=false", "-p:PublishTrimmed=false", "-p:PublishSingleFile=false"], env, evidence / "managed-publish.log")


def check_report(report, args, initial, executables=None):
    cases = report["cases"]
    if report["failed"] or report["passed"] != 99 or len(cases) != 99 or len({case["case"] for case in cases}) != 99:
        raise RuntimeError("Parity did not complete the full unique 99-case corpus")
    if not all(case["passed"] for case in cases) or {case["case"] for case in cases} != EXPECTED_CASES:
        raise RuntimeError("Parity contains an unsuccessful or missing required case")
    expected = {"source_sha": initial["sha"], "framework": args.framework, "rid": args.rid, "runtime_version": args.runtime_version}
    for key, value in expected.items():
        if report[key] != value:
            raise RuntimeError(f"Parity input mismatch: {key}")
    executable = WINDOWS_EXECUTABLE if args.rid.startswith("win-") else "gitversion"
    for flavor in ("native", "managed"):
        path = executables[flavor] if executables is not None else args.output / flavor / executable
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if report[flavor + "_sha256"] != digest:
            raise RuntimeError(f"Parity executable mismatch: {flavor}")


def inspect_dependencies(args, env, evidence):
    native = args.output / "native"
    if args.rid.startswith("win-"):
        tools = []
        for variable in ("ProgramFiles", "ProgramFiles(x86)"):
            base = Path(os.environ.get(variable, "C:/Program Files")) / "Microsoft Visual Studio"
            tools.extend(base.glob("*/*/VC/Tools/MSVC/*/bin/Host*/*/dumpbin.exe"))
        architecture = "arm64" if args.rid.endswith("arm64") else "x64"
        tools = [tool for tool in tools if tool.parent.name.lower() == architecture]
        if not tools:
            raise RuntimeError("Cannot inspect native Windows imports: dumpbin is missing")
        command = [str(max(tools)), "/dependents", str(native / WINDOWS_EXECUTABLE)]
    else:
        command = ["otool", "-L", str(native / "gitversion")] if args.rid.startswith("osx-") else ["ldd", str(native / "gitversion")]
    log = evidence / "native-dependencies.log"
    run(command, env, log)
    imports = log.read_text(encoding="utf-8", errors="replace").lower()
    if any(library in imports for library in ("libgit2", "coreclr", "hostfxr", "hostpolicy")):
        raise RuntimeError("Native imports include a forbidden backend or managed runtime")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--framework", choices=["net10.0", "net11.0"], required=True)
    parser.add_argument("--rid", choices=["win-x64", "win-arm64", "linux-x64", "linux-arm64", "linux-musl-x64", "linux-musl-arm64", "osx-arm64"], required=True)
    parser.add_argument("--runtime-version", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    args = parser.parse_args()
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=False)
    evidence = args.output / "evidence"
    evidence.mkdir()
    spec = importlib.util.spec_from_file_location("parity", ROOT / "build/nativeaot-parity.py")
    parity = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(parity)
    env = parity.isolated_environment() | {"PYTHONUTF8": "1", "PYTHONIOENCODING": "utf-8"}
    try:
        require_target_host(args.rid)
        initial = source_identity(env)
        run([args.dotnet, "--info"], env, evidence / "dotnet-info.log")
        publish_pair(args, env, evidence, initial)
        inspect_dependencies(args, env, evidence)
        executable = WINDOWS_EXECUTABLE if args.rid.startswith("win-") else "gitversion"
        run([sys.executable, str(ROOT / "build/nativeaot-parity.py"), "--managed", str(args.output / "managed" / executable), "--native", str(args.output / "native" / executable),
             "--framework", args.framework, "--rid", args.rid, "--runtime-version", args.runtime_version, "--repository", str(ROOT)], env, evidence / "parity.json")
        report = json.loads((evidence / "parity.json").read_text(encoding="utf-8"))
        check_report(report, args, initial)
        if source_identity(env) != initial:
            raise RuntimeError("Source inputs changed during validation")
        result = {"passed": True, "source": initial, "framework": args.framework, "rid": args.rid, "runtime_version": args.runtime_version,
                  "host": {"system": platform.system(), "machine": platform.machine(), "version": platform.version()}, "parity_cases": report["passed"]}
    except (OSError, ValueError, RuntimeError, subprocess.SubprocessError, KeyError, TypeError) as error:
        result = {"passed": False, "framework": args.framework, "rid": args.rid, "error": f"{type(error).__name__}: {error}"}
    (evidence / "platform.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

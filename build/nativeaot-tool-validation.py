#!/usr/bin/env python3
"""Validate local-only NativeAOT tool packaging and SDK 10/11 dispatchers."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from types import SimpleNamespace
import xml.etree.ElementTree as ET
import zipfile


ROOT = Path(__file__).resolve().parent.parent
PACKAGE_ID = "GitVersion.Tool"
TOOL_COMMAND = "dotnet-gitversion"


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


VALIDATION = load("validation", "nativeaot-platform-validation.py")
SMOKE = load("smoke", "nativeaot-smoke.py")
PARITY = load("parity", "nativeaot-parity.py")


def dispatcher_contract(archive, rid, pointer):
    settings_files = [name for name in archive.namelist() if name.endswith("/DotnetToolSettings.xml")]
    if len(settings_files) != 1:
        raise RuntimeError("Native tool requires one dispatcher settings file")
    settings = ET.fromstring(archive.read(settings_files[0]))
    commands = settings.findall("Commands/Command")
    if len(commands) != 1 or commands[0].get("Name") != TOOL_COMMAND:
        raise RuntimeError("Native tool command mismatch")
    if pointer:
        mappings = settings.findall("RuntimeIdentifierPackages/RuntimeIdentifierPackage")
        if len(mappings) != 1 or mappings[0].attrib != {"RuntimeIdentifier": rid, "Id": f"{PACKAGE_ID}.{rid}"}:
            raise RuntimeError("Native tool pointer selects an unexpected RID/package")
        return None
    executable = "gitversion.exe" if rid.startswith("win-") else "gitversion"
    if commands[0].get("Runner") != "executable" or commands[0].get("EntryPoint") != executable:
        raise RuntimeError("Native tool dispatcher is not an executable runner")
    entry = f"tools/any/{rid}/{executable}"
    if settings_files[0] != f"tools/any/{rid}/DotnetToolSettings.xml" or any(name.lower().endswith(".dll") or "libgit2" in name.lower() for name in archive.namelist()):
        raise RuntimeError("Native tool contains a forbidden payload")
    return hashlib.sha256(archive.read(entry)).hexdigest()


def package_contract(feed, version, rid, revision):
    payload_id = f"{PACKAGE_ID}.{rid}"
    metadata = []
    for identity in (PACKAGE_ID, payload_id):
        package = feed / f"{identity}.{version}.nupkg"
        with zipfile.ZipFile(package) as archive:
            spec = ET.fromstring(archive.read(identity + ".nuspec"))
            values = {element.tag.rsplit("}", 1)[-1]: element for element in spec.iter()}
            if values["id"].text != identity or values["version"].text != version or values["repository"].get("commit") != revision:
                raise RuntimeError("Native tool package identity/source mismatch")
            payload_hash = dispatcher_contract(archive, rid, identity == PACKAGE_ID)
            if payload_hash:
                metadata.append({"payload_sha256": payload_hash})
        metadata.append({"package": package.name, "sha256": hashlib.sha256(package.read_bytes()).hexdigest()})
    return metadata


def feed_config(feed, path, rid):
    configuration = ET.Element("configuration")
    sources = ET.SubElement(configuration, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", key="local", value=str(feed))
    ET.SubElement(sources, "add", key="microsoft-host", value="https://api.nuget.org/v3/index.json")
    mappings = ET.SubElement(configuration, "packageSourceMapping")
    local = ET.SubElement(mappings, "packageSource", key="local")
    for identity in (PACKAGE_ID, f"{PACKAGE_ID}.{rid}"):
        ET.SubElement(local, "package", pattern=identity)
    official = ET.SubElement(mappings, "packageSource", key="microsoft-host")
    ET.SubElement(official, "package", pattern="Microsoft.NETCore.App.Host.*")
    ET.ElementTree(configuration).write(path, encoding="utf-8", xml_declaration=True)


def pack(args, env, evidence, initial):
    version = f"0.0.0-aot.{initial['sha'][:8]}.{args.framework.replace('.', '')}"
    feed = args.output / "feed"
    feed.mkdir()
    common = [args.dotnet, "pack", str(ROOT / "src/GitVersion.App/GitVersion.App.csproj"), "-c", "Release", "-o", str(feed),
              f"-p:TargetFrameworks={args.framework}", f"-p:RuntimeFrameworkVersion={args.runtime_version}", f"-p:RuntimeIdentifiers={args.rid}",
              f"-p:ToolPackageRuntimeIdentifiers={args.rid}", f"-p:PackageVersion={version}", "-p:GitVersionManagedOnly=true", "-p:PublishAot=true",
              "-p:PackAsTool=true", "-p:TrimmerSingleWarn=false", "-p:ILLinkTreatWarningsAsErrors=true", "-p:IlcTreatWarningsAsErrors=true"]
    VALIDATION.run(common + ["-r", args.rid], env, evidence / "tool-pack-rid.log")
    VALIDATION.run(common, env, evidence / "tool-pack-pointer.log")
    return version, feed, package_contract(feed, version, args.rid, initial["sha"])


def installation_command(args, version, config):
    command = [args.dotnet, "tool", "install", PACKAGE_ID, "--version", version, "--configfile", str(config), "--no-http-cache"]
    # Linux --arch takes the SDK's unsupported cross-architecture shim path.
    # The platform gate already requires a native CPU/libc host and SDK.
    if not args.rid.startswith("linux-"):
        command += ["--arch", "arm64" if args.rid.endswith("arm64") else "x64"]
    return command


def validate_installer(args, env, evidence, initial, version, feed, sdk):
    work = args.output / ("sdk-" + sdk)
    work.mkdir()
    (work / "global.json").write_text(json.dumps({"sdk": {"version": sdk, "rollForward": "disable", "allowPrerelease": True}}), encoding="utf-8")
    config = work / "NuGet.Config"
    feed_config(feed, config, args.rid)
    selected = env | {"DOTNET_CLI_HOME": str(work / "home"), "NUGET_PACKAGES": str(work / "cache"), "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH": "false"}
    # Tool commands select the SDK from the isolated working directory, not ROOT.
    def run(command, name, cwd=work):
        log = evidence / ("tool-" + sdk + "-" + name + ".log")
        with log.open("wb") as output, log.with_name(log.stem + "-stderr.log").open("wb") as errors:
            result = subprocess.run(command, cwd=cwd, env=selected, stdout=output, stderr=errors, timeout=1800)
        if result.returncode:
            raise RuntimeError(f"Tool {name} failed; see {log}")
        return log

    log = run([args.dotnet, "--version"], "sdk")
    if log.read_text(encoding="utf-8").strip() != sdk:
        raise RuntimeError("Tool installation did not use the requested SDK")
    local = work / "local"
    run([args.dotnet, "new", "tool-manifest", "--output", str(local)], "manifest")
    install = installation_command(args, version, config)
    run(install + ["--tool-path", str(work / "tools")], "path-install")
    run(install + ["--global"], "global-install")
    run(install, "local-install", local)
    suffix = ".exe" if args.rid.startswith("win-") else ""
    shim_suffix = ".cmd" if args.rid.startswith("win-") else ""
    installed = work / "tools" / (TOOL_COMMAND + shim_suffix)
    global_tool = work / "home/.dotnet/tools" / (TOOL_COMMAND + shim_suffix)
    payload = work / "cache" / f"{PACKAGE_ID}.{args.rid}".lower() / version / f"tools/any/{args.rid}" / ("gitversion" + suffix)
    with zipfile.ZipFile(feed / f"{PACKAGE_ID}.{args.rid}.{version}.nupkg") as archive:
        expected = hashlib.sha256(archive.read(f"tools/any/{args.rid}/gitversion{suffix}")).hexdigest()
    if hashlib.sha256(payload.read_bytes()).hexdigest() != expected:
        raise RuntimeError("Local tool installation selected a different native payload")
    for shim in (installed, global_tool):
        if hashlib.sha256(SMOKE.native_payload(shim).read_bytes()).hexdigest() != expected:
            raise RuntimeError("Installed dispatcher selected a different native payload")
    reports = []
    for mode, native, command, cwd in [("path", installed, None, work), ("global", global_tool, None, work),
                                       ("manifest", payload, [args.dotnet, "tool", "run", TOOL_COMMAND, "--"], local)]:
        report = SMOKE.check_execution(native, command, cwd, selected)
        reports.append({"mode": mode, "sdk": sdk, "execution": report})
    managed = args.platform_output / "managed" / ("gitversion" + suffix)
    parity_log = evidence / ("tool-" + sdk + "-parity.json")
    VALIDATION.run([sys.executable, str(ROOT / "build/nativeaot-parity.py"), "--managed", str(managed), "--native", str(installed),
                    "--framework", args.framework, "--rid", args.rid, "--runtime-version", args.runtime_version, "--repository", str(ROOT)], selected, parity_log)
    parity = json.loads(parity_log.read_text(encoding="utf-8"))
    VALIDATION.check_report(parity, args, initial, {"native": installed, "managed": managed})
    return {"sdk": sdk, "parity_cases": parity["passed"], "installations": reports,
            "path_command": str(installed.relative_to(args.output)), "global_command": str(global_tool.relative_to(args.output))}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform-output", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    args = parser.parse_args()
    args.platform_output = args.platform_output.resolve()
    args.output = args.platform_output / "tool-validation"
    args.output.mkdir(exist_ok=False)
    evidence = args.platform_output / "evidence"
    env = PARITY.isolated_environment() | {"PYTHONUTF8": "1", "PYTHONIOENCODING": "utf-8", "DOTNET_NOLOGO": "1"}
    try:
        args.dotnet = str(Path(shutil.which(args.dotnet) or args.dotnet).resolve())
        platform_report = json.loads((evidence / "platform.json").read_text(encoding="utf-8"))
        if not platform_report["passed"]:
            raise RuntimeError("Tool packaging requires a successful platform gate first")
        args.framework, args.rid, args.runtime_version = (platform_report[key] for key in ("framework", "rid", "runtime_version"))
        initial = VALIDATION.source_identity(env)
        if initial != platform_report["source"]:
            raise RuntimeError("Tool packaging inputs differ from the validated platform")
        platform_args = SimpleNamespace(output=args.platform_output, framework=args.framework, rid=args.rid, runtime_version=args.runtime_version)
        VALIDATION.check_report(json.loads((evidence / "parity.json").read_text(encoding="utf-8")), platform_args, initial)
        version, feed, packages = pack(args, env, evidence, initial)
        sdk11 = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))["sdk"]["version"]
        installers = [validate_installer(args, env, evidence, initial, version, feed, sdk) for sdk in ("10.0.402", sdk11)]
        if VALIDATION.source_identity(env) != initial:
            raise RuntimeError("Source inputs changed during tool validation")
        result = {"passed": True, "source": initial, "framework": args.framework, "rid": args.rid, "runtime_version": args.runtime_version,
                  "package_version": version, "packages": packages, "installers": installers, "public_packages_published": False}
    except (OSError, ValueError, RuntimeError, AssertionError, KeyError, TypeError, subprocess.SubprocessError, zipfile.BadZipFile, ET.ParseError) as error:
        result = {"passed": False, "error": f"{type(error).__name__}: {error}"}
    (evidence / "tool-validation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())

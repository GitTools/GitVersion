#!/usr/bin/env python3
"""Execute real GitCliExecutor stress and credential checks under NativeAOT."""
import argparse
import hashlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import threading


ROOT = Path(__file__).resolve().parent.parent
SPEC = importlib.util.spec_from_file_location("tools", Path(__file__).with_name("nativeaot-tool-validation.py"))
TOOLS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(TOOLS)
EXPECTED_CHECKS = frozenset([
    "CaptureNormalizesCrLfAndUnterminatedLine", "CaptureNormalizesLfAndTerminatedLine", "CaptureNormalizesCarriageReturns",
    "CapturePreservesEmptyOutput", "CaptureSeparatesBothStreamsAndPreservesFailureExitCode", "CaptureDrainsLargeInterleavedStreams",
    "CapturePreservesArgumentsAndDeterministicEnvironment", "CaptureDecodesUtf8WithConsoleCodePage65001",
    "CaptureDecodesUtf8WithConsoleCodePage1252", "CaptureReportsProcessStartupFailure", "CaptureReportsCredentialHelperFailure",
])


class AuthenticationRequired(BaseHTTPRequestHandler):
    def do_GET(self):
        self.server.requests += 1
        self.send_response(401)
        self.send_header("WWW-Authenticate", 'Basic realm="native-validation"')
        self.send_header("Content-Length", "0")
        self.end_headers()

    def log_message(self, *_):
        pass


def check_report(report, requests):
    checks = report["Checks"]
    if not report["Passed"] or report["Error"] is not None or len(checks) != len(EXPECTED_CHECKS) or set(checks) != EXPECTED_CHECKS or requests < 1:
        raise RuntimeError("Native capture did not pass every required check and credential fixture")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform-output", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    args = parser.parse_args()
    args.platform_output = args.platform_output.resolve()
    evidence = args.platform_output / "evidence"
    env = TOOLS.PARITY.isolated_environment() | {"PYTHONUTF8": "1", "PYTHONIOENCODING": "utf-8"}
    try:
        platform = json.loads((evidence / "platform.json").read_text(encoding="utf-8"))
        if not platform["passed"] or TOOLS.VALIDATION.source_identity(env) != platform["source"]:
            raise RuntimeError("Capture requires successful same-source platform parity")
        TOOLS.VALIDATION.require_target_host(platform["rid"])
        output = args.platform_output / "capture"
        TOOLS.VALIDATION.run([args.dotnet, "publish", str(ROOT / "build/nativeaot-capture/GitVersion.NativeCapture.Validation.csproj"),
                              "-c", "Release", "-f", platform["framework"], "-r", platform["rid"], "-o", str(output), "--self-contained", "true",
                              f"-p:TargetFrameworks={platform['framework']}", f"-p:RuntimeFrameworkVersion={platform['runtime_version']}",
                              "-p:GitVersionManagedOnly=true", "-p:PublishAot=true", "-p:TrimmerSingleWarn=false",
                              "-p:ILLinkTreatWarningsAsErrors=true", "-p:IlcTreatWarningsAsErrors=true"], env, evidence / "capture-publish.log")
        executable = output / ("GitVersion.Git.Managed.Tests.exe" if platform["rid"].startswith("win-") else "GitVersion.Git.Managed.Tests")
        with ThreadingHTTPServer(("127.0.0.1", 0), AuthenticationRequired) as server, tempfile.TemporaryDirectory() as directory:
            server.requests = 0
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                TOOLS.VALIDATION.run([str(executable), directory, f"http://127.0.0.1:{server.server_port}/repository.git"],
                                      env, evidence / "capture-checks.json")
                report = json.loads((evidence / "capture-checks.json").read_text(encoding="utf-8"))
                check_report(report, server.requests)
            finally:
                server.shutdown()
                thread.join(timeout=10)
        if TOOLS.VALIDATION.source_identity(env) != platform["source"]:
            raise RuntimeError("Source inputs changed during native capture validation")
        result = {"passed": True, "source": platform["source"], "framework": platform["framework"], "rid": platform["rid"],
                  "runtime_version": platform["runtime_version"], "native_sha256": hashlib.sha256(executable.read_bytes()).hexdigest(),
                  "checks": report["Checks"], "credential_requests": server.requests}
    except (OSError, ValueError, RuntimeError, subprocess.SubprocessError, KeyError, TypeError) as error:
        result = {"passed": False, "error": f"{type(error).__name__}: {error}"}
    (evidence / "capture-validation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

"""Inspect a secret-free Sonar build handoff; this is not a trusted uploader."""

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import xml.etree.ElementTree as ET

SCANNER_VERSION = "11.3.0"
NS = {"s": "http://www.sonarsource.com/msbuild/integration/2015/1"}  # NOSONAR(S5332) XML namespace identifier, not a network endpoint.
DATA_SUFFIXES = {".xml", ".json", ".pb", ".txt", ".ucfgs", ".typedefs", ".udg", ".log", ".lock", ".md"}
MAX_FILE_BYTES = 128 * 1024 * 1024
MAX_TOTAL_BYTES = 1024 * 1024 * 1024
MAX_FILES = 20000


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_xml(path):
    require(path.stat().st_size <= MAX_FILE_BYTES, f"Oversized XML: {path}")
    try:
        data = path.read_bytes().decode("utf-8-sig")
    except UnicodeDecodeError as error:
        raise ValueError(f"Unsupported XML encoding: {path}") from error
    require("<!DOCTYPE" not in data.upper() and "<!ENTITY" not in data.upper(),
            f"XML declarations are not allowed: {path}")
    return ET.fromstring(data)


def data_files(root):
    require(root.is_dir() and not root.is_symlink(), f"Missing or linked data directory: {root}")
    files = []
    total = 0
    for path in sorted(root.rglob("*")):
        require(not path.is_symlink(), f"Linked data: {path}")
        if path.is_dir():
            continue
        require(path.is_file() and path.suffix in DATA_SUFFIXES, f"Unexpected data file: {path}")
        size = path.stat().st_size
        require(size <= MAX_FILE_BYTES, f"Oversized data file: {path}")
        total += size
        files.append(path)
        require(len(files) <= MAX_FILES and total <= MAX_TOTAL_BYTES, "Data bundle exceeds limits")
    require(files, f"Empty data directory: {root}")
    return files


def repository_path(value, producer, repository):
    """Map a producer's absolute source path to the matching checkout."""
    path = PurePosixPath(value)
    base = PurePosixPath(producer)
    require(path.is_absolute() and ".." not in path.parts, f"Invalid source path: {value}")
    require(path.is_relative_to(base), f"Source outside producer checkout: {value}")
    relative = path.relative_to(base)
    current = repository / relative
    require(current.resolve().is_relative_to(repository.resolve()), f"Source escapes checkout: {value}")
    require(current.is_file(), f"Missing source: {relative}")
    return relative.as_posix()


def analysis_scope(payload, producer, repository):
    """Find repository sources in the scanner output for all three solution trees."""
    project_files = sorted((payload / "sonar/out").glob("*/ProjectInfo.xml"))
    require(project_files, "Missing Sonar project metadata")
    projects = set()
    analyzed_sources = set()
    for path in project_files:
        info = read_xml(path)
        if info.findtext("s:IsExcluded", namespaces=NS) == "true":
            continue
        project = repository_path(info.findtext("s:FullPath", namespaces=NS) or "", producer, repository)
        projects.add(project)
        file_list = payload / "sonar/conf" / path.parent.name / "FilesToAnalyze.txt"
        require(file_list.is_file(), f"Missing source list for {project}")
        for value in file_list.read_text(encoding="utf-8-sig").splitlines():
            # Generated and other non-tracked files are recorded separately by the scanner.
            # The proof requires repository source, not generated build outputs on the receiver.
            source = PurePosixPath(value)
            if not source.is_relative_to(PurePosixPath(producer)):
                # SDK file lists can contain NuGet content/PDBs. Do not read them.
                continue
            relative = source.relative_to(PurePosixPath(producer))
            require(".." not in relative.parts, f"Invalid source-list path: {value}")
            if {"obj", "bin"}.intersection(relative.parts):
                continue
            if (repository / relative).is_file():
                analyzed_sources.add(repository_path(value, producer, repository))
    require({PurePosixPath(p).parts[0] for p in projects} >= {"src", "new-cli", "build"},
            "Analysis must include src, new-cli and build projects")
    require(any(p.endswith(".cs") for p in analyzed_sources), "No analyzed C# source files")

    return projects, analyzed_sources


def coverage_scope(payload, producer, repository):
    """Require complete per-project Cobertura reports and map owned source paths."""
    expected = {p.stem for p in (repository / "src").glob("**/*.Tests.csproj")}
    require(expected, "No expected test projects")
    reports = sorted((payload / "coverage").glob("*/net10.0/*cobertura*.xml"))
    actual = [p.parent.parent.name for p in reports]
    require(len(actual) == len(expected) and set(actual) == expected,
            f"Expected one coverage report per test project: {sorted(expected)}; got {actual}")
    owned = set()
    external = set()
    for path in reports:
        coverage = read_xml(path)
        require(coverage.tag == "coverage", f"Not Cobertura: {path}")
        roots = [x.text or "" for x in coverage.findall("./sources/source")]
        mapped = set()
        for item in coverage.findall(".//class"):
            filename = item.get("filename", "")
            candidates = [PurePosixPath(root) / filename for root in roots]
            candidates.append(PurePosixPath(filename))
            matching = [p for p in candidates if p.is_relative_to(PurePosixPath(producer))]
            if matching:
                mapped.add(repository_path(str(matching[0]), producer, repository))
            else:
                external.add(filename)
        require(mapped, f"No repository files in coverage: {path}")
        require(coverage.findall(".//line"), f"No line coverage data: {path}")
        owned.update(mapped)
    return reports, owned, external


def inspect(payload, producer, repository):
    """Check C# scope and source mapping without running the scanner or build."""
    projects, analyzed_sources = analysis_scope(payload, producer, repository)
    reports, owned, external = coverage_scope(payload, producer, repository)
    require(owned <= analyzed_sources, f"Coverage files absent from analysis: {sorted(owned - analyzed_sources)[:5]}")
    return {"projects": sorted(projects), "analyzed_source_files": len(analyzed_sources),
            "coverage_reports": len(reports), "covered_source_files": len(owned),
            "external_coverage_files": len(external)}


def inventory(payload):
    return {p.relative_to(payload).as_posix(): {"bytes": p.stat().st_size,
             "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in data_files(payload)}


def collect(repository, scanner, coverage, bundle, identity):
    require(not bundle.exists(), f"Bundle already exists: {bundle}")
    payload = bundle / "payload"
    # Copy only analysis output and source lists. Never copy scanner configuration or binaries.
    for path in data_files(scanner / "out"):
        target = payload / "sonar/out" / path.relative_to(scanner / "out")
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)
    for path in (scanner / "conf").glob("*/FilesToAnalyze.txt"):
        require(not path.is_symlink(), f"Linked source list: {path}")
        target = payload / "sonar/conf" / path.parent.name / path.name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)
    for path in data_files(coverage):
        if "cobertura" in path.name and path.suffix == ".xml":
            target = payload / "coverage" / path.relative_to(coverage)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, target)
    result = inspect(payload, repository.as_posix(), repository)
    manifest = {"schema": 1, "identity": identity, "scanner": SCANNER_VERSION,
                "producer": repository.as_posix(), "files": inventory(payload), "result": result}
    (bundle / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    return result


def verify(repository, bundle, identity):
    manifest = json.loads((bundle / "manifest.json").read_text())
    require(manifest.get("schema") == 1 and manifest.get("scanner") == SCANNER_VERSION,
            "Unexpected manifest or scanner version")
    require(manifest.get("identity") == identity, "Run, revision or PR identity mismatch")
    require(manifest.get("files") == inventory(bundle / "payload"), "Artifact contents changed")
    result = inspect(bundle / "payload", manifest["producer"], repository)
    require(manifest.get("result") == result, "Analysis/coverage mapping changed")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["collect", "verify"])
    parser.add_argument("--repository", type=Path, default=Path.cwd())
    parser.add_argument("--bundle", type=Path, required=True)
    parser.add_argument("--scanner", type=Path)
    parser.add_argument("--coverage", type=Path)
    args = parser.parse_args()
    identity = {key: os.environ[key] for key in
                ("GITHUB_REPOSITORY", "GITHUB_SHA", "GITHUB_RUN_ID", "GITHUB_RUN_ATTEMPT", "PROOF_HEAD_SHA", "PROOF_PR")}
    repository = args.repository.resolve()
    if args.mode == "collect":
        require(args.scanner is not None and args.coverage is not None, "Collect needs scanner and coverage directories")
        result = collect(repository, args.scanner, args.coverage, args.bundle, identity)
    else:
        result = verify(repository, args.bundle, identity)
    print(json.dumps(result, indent=2))
    if "GITHUB_STEP_SUMMARY" in os.environ:
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as summary:
            summary.write(f"### Sonar handoff proof: {args.mode}\n\n")
            summary.write(f"Revision: `{identity['GITHUB_SHA']}`\n\n")
            summary.write(f"{len(result['projects'])} projects; {result['coverage_reports']} coverage reports; "
                          f"{result['covered_source_files']} mapped source files.\n\n")
            summary.write("Data handoff only: no Sonar analysis uploaded, no credentials used. "
                          "This artifact is not approved for a credential-bearing publisher.\n")


if __name__ == "__main__":
    main()

"""Focused checks for the data-only Sonar proof contract."""

import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import sonar_proof as proof


class SonarProofTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.repo = self.root / "repo"
        self.scanner = self.repo / ".sonarqube"
        self.coverage = self.root / "coverage"
        self.bundle = self.root / "bundle"
        self.identity = {"revision": "a" * 40, "run": "123", "attempt": "1", "pr": "42"}
        self.write(self.repo / "src/Example.Tests/Example.Tests.csproj", "<Project />")
        for i, scope in enumerate(("src", "new-cli", "build")):
            project = self.repo / scope / "Example/Example.csproj"
            source = project.with_name("Example.cs")
            self.write(project, "<Project />")
            self.write(source, "class Example {}")
            self.write(self.scanner / f"out/{i}/ProjectInfo.xml",
                       f'<ProjectInfo xmlns="{proof.NS["s"]}"><FullPath>{project}</FullPath>'
                       f'<ProjectGuid>{proof.uuid.UUID(int=i + 1)}</ProjectGuid>'
                       '<IsExcluded>false</IsExcluded></ProjectInfo>')
            self.write(self.scanner / f"conf/{i}/FilesToAnalyze.txt", str(source) + "\n")
        self.report = self.coverage / "Example.Tests/net10.0/coverage.cobertura.xml"
        self.write(self.report, f'<coverage><sources><source>{self.repo}</source></sources>'
                   '<packages><package><classes><class filename="src/Example/Example.cs">'
                   '<lines><line number="1" hits="1" /></lines></class></classes></package></packages></coverage>')

    @staticmethod
    def write(path, text):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text)

    def collect(self):
        return proof.collect(self.repo, self.scanner, self.coverage, self.bundle, self.identity)

    def test_round_trip_preserves_scope_and_coverage_on_another_checkout(self):
        import shutil
        self.collect()
        receiver = self.root / "receiver"
        shutil.copytree(self.repo, receiver)
        result = proof.verify(receiver, self.bundle, self.identity)
        self.assertEqual(result["coverage_reports"], 1)
        self.assertEqual(result["covered_source_files"], 1)
        self.assertEqual(result["analyzed_source_files"], 3)
        self.assertEqual(result["projects"], ["build/Example/Example.csproj", "new-cli/Example/Example.csproj", "src/Example/Example.csproj"])
        self.assertFalse((self.bundle / "payload/sonar/conf/SonarQubeAnalysisConfig.xml").exists())

    def test_rejects_duplicate_project_ids(self):
        path = self.scanner / "out/1/ProjectInfo.xml"
        self.write(path, path.read_text().replace(str(proof.uuid.UUID(int=2)), str(proof.uuid.UUID(int=1))))
        with self.assertRaisesRegex(ValueError, "Duplicate Sonar project ID"):
            self.collect()

    def test_project_ids_distinguish_same_names_and_survive_checkout_move(self):
        import shutil
        first = self.root / "first.targets"
        second = self.root / "second.targets"
        proof.write_project_ids(self.repo, first)
        receiver = self.root / "receiver"
        shutil.copytree(self.repo, receiver)
        proof.write_project_ids(receiver, second)
        first_ids = [e.text for e in proof.read_xml(first).iter("ProjectGuid")]
        second_ids = [e.text for e in proof.read_xml(second).iter("ProjectGuid")]
        self.assertEqual(len(first_ids), 4)
        self.assertEqual(len(set(first_ids)), 4)
        self.assertEqual(first_ids, second_ids)

    def test_ignores_external_source_list_entries_without_reading_them(self):
        path = self.scanner / "conf/0/FilesToAnalyze.txt"
        self.write(path, path.read_text() + "/outside/nuget/adapter.pdb\n")
        result = self.collect()
        self.assertEqual(result["analyzed_source_files"], 3)
        self.assertEqual(result["covered_source_files"], 1)

    def test_rejects_stale_revision_or_attempt(self):
        self.collect()
        for key, value in (("revision", "b" * 40), ("attempt", "2"), ("pr", "43")):
            with self.subTest(key=key), self.assertRaisesRegex(ValueError, "identity mismatch"):
                proof.verify(self.repo, self.bundle, self.identity | {key: value})

    def test_rejects_changed_payload(self):
        self.collect()
        self.write(self.bundle / "payload/coverage/Example.Tests/net10.0/coverage.cobertura.xml", "changed")
        with self.assertRaisesRegex(ValueError, "contents changed"):
            proof.verify(self.repo, self.bundle, self.identity)

    def test_rejects_missing_reports(self):
        self.report.unlink()
        with self.assertRaisesRegex(ValueError, "Empty data directory"):
            self.collect()

    def test_rejects_duplicate_report_for_one_project(self):
        self.write(self.report.with_name("duplicate.cobertura.xml"), self.report.read_text())
        with self.assertRaisesRegex(ValueError, "one coverage report"):
            self.collect()

    def test_rejects_external_only_coverage(self):
        for filename in ("external/Example.cs", "src/Example/Missing.cs"):
            with self.subTest(filename=filename), tempfile.TemporaryDirectory() as destination:
                self.write(self.report, f'<coverage><sources><source>/outside</source></sources>'
                           f'<class filename="{filename}"><line number="1" hits="1" /></class></coverage>')
                with self.assertRaisesRegex(ValueError, "No repository files"):
                    proof.collect(self.repo, self.scanner, self.coverage, Path(destination)/"bundle", self.identity)

    def test_rejects_missing_owned_source(self):
        self.write(self.report, self.report.read_text().replace("src/Example/Example.cs", "src/Example/Missing.cs"))
        with self.assertRaisesRegex(ValueError, "Missing source"):
            self.collect()

    def test_rejects_coverage_without_line_data(self):
        self.write(self.report, self.report.read_text().replace('<line number="1" hits="1" />', ""))
        with self.assertRaisesRegex(ValueError, "No line coverage data"):
            self.collect()

    def test_rejects_coverage_not_in_analysis(self):
        self.write(self.scanner / "conf/0/FilesToAnalyze.txt", "")
        with self.assertRaisesRegex(ValueError, "absent from analysis"):
            self.collect()

    def test_rejects_lost_solution_scope(self):
        (self.scanner / "out/1/ProjectInfo.xml").unlink()
        with self.assertRaisesRegex(ValueError, "include src, new-cli and build"):
            self.collect()

    def test_rejects_xml_entities_and_malformed_reports(self):
        for text in ('<!DOCTYPE coverage [<!ENTITY e "bad">]><coverage>&e;</coverage>', '<coverage>'):
            with self.subTest(text=text), tempfile.TemporaryDirectory() as destination:
                self.write(self.report, text)
                with self.assertRaises((ValueError, proof.ET.ParseError)):
                    proof.collect(self.repo, self.scanner, self.coverage, Path(destination)/"bundle", self.identity)

    def test_rejects_utf16_entity_declarations(self):
        self.report.write_bytes('<!DOCTYPE coverage [<!ENTITY e "bad">]><coverage>&e;</coverage>'.encode("utf-16"))
        with self.assertRaisesRegex(ValueError, "Unsupported XML encoding"):
            self.collect()

    def test_rejects_linked_data_and_executables(self):
        extra = self.scanner / "out/extra.txt"
        extra.symlink_to(self.report)
        with self.assertRaisesRegex(ValueError, "Linked data"):
            self.collect()
        extra.unlink()
        self.write(self.scanner / "out/evil.dll", "executable")
        with self.assertRaisesRegex(ValueError, "Unexpected data file"):
            self.collect()

    def test_rejects_source_traversal(self):
        self.write(self.scanner / "conf/0/FilesToAnalyze.txt", str(self.repo / "src/../outside.cs"))
        with self.assertRaisesRegex(ValueError, "Invalid source-list path"):
            self.collect()

    def test_rejects_changed_mapping_and_oversized_data(self):
        self.collect()
        manifest = self.bundle / "manifest.json"
        data = json.loads(manifest.read_text())
        data["result"]["coverage_reports"] = 99
        manifest.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, "mapping changed"):
            proof.verify(self.repo, self.bundle, self.identity)
        with patch.object(proof, "MAX_FILE_BYTES", 1), self.assertRaisesRegex(ValueError, "Oversized"):
            proof.inventory(self.bundle / "payload")


if __name__ == "__main__":
    unittest.main()

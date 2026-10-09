import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, writeFileSync, rmSync, renameSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { inspect, acknowledge } from "./check-version-calculation.mjs";
const ids = ["overview","strategies","inheritance","increments","selection","mainline","deployment"];
function fixture(t) {
  const root = mkdtempSync(join(tmpdir(),"algorithm-docs-"));
  t.after(() => rmSync(root,{recursive:true,force:true}));
  function write(path,contents) { mkdirSync(join(root,path,".."),{recursive:true}); writeFileSync(join(root,path),contents); }
  const boundary = "src/GitVersion.Core/VersionCalculation";
  write(boundary + "/Rule.cs","class Rule {}\n");
  write("shared/Common.cs","class Common {}\n");
  write("tests/Evidence.cs","class Evidence {}\n");
  write("docs/page.md",ids.map(id=>"## "+id).join("\n"));
  const manifest = {schemaVersion:1,page:"docs/page.md",coverageRoots:[boundary],sharedSources:["shared"],sections:ids.map(id=>({id,heading:id,sources:[boundary],tests:["tests/Evidence.cs"],review:{sourceDigest:"0".repeat(64),reason:"Initial reviewed fixture"}}))};
  for(const report of inspect(root,manifest)) manifest.sections.find(s=>s.id===report.id).review.sourceDigest=report.digest;
  return {root,manifest,write,boundary};
}
test("unchanged inputs pass",t=>{const f=fixture(t);assert.ok(inspect(f.root,f.manifest).every(r=>!r.stale));});
test("code changes stay stale after an unrelated documentation edit",t=>{
  const f=fixture(t); f.write(f.boundary+"/Rule.cs","class Rule { int Version = 2; }\n"); f.write("docs/unrelated.md","Documentation updated");
  assert.ok(inspect(f.root,f.manifest).every(r=>r.stale));
});
test("new calculation files invalidate review",t=>{const f=fixture(t);f.write(f.boundary+"/NewRule.cs","class NewRule {}");assert.ok(inspect(f.root,f.manifest).every(r=>r.stale));});
test("deleted directory-owned files invalidate review",t=>{const f=fixture(t);f.write(f.boundary+"/Keep.cs","class Keep {}");for(const r of inspect(f.root,f.manifest))f.manifest.sections.find(s=>s.id===r.id).review.sourceDigest=r.digest;rmSync(join(f.root,f.boundary,"Rule.cs"));assert.ok(inspect(f.root,f.manifest).every(r=>r.stale));});
test("renamed files invalidate review even with identical contents",t=>{const f=fixture(t);renameSync(join(f.root,f.boundary,"Rule.cs"),join(f.root,f.boundary,"Renamed.cs"));assert.ok(inspect(f.root,f.manifest).every(r=>r.stale));});
test("CRLF and LF have identical fingerprints",t=>{const f=fixture(t);f.write(f.boundary+"/Rule.cs","class Rule {}\r\n");assert.ok(inspect(f.root,f.manifest).every(r=>!r.stale));});
test("shared inputs invalidate all sections",t=>{const f=fixture(t);f.write("shared/Common.cs","class Common { int X; }");assert.equal(inspect(f.root,f.manifest).filter(r=>r.stale).length,7);});
test("missing sections and empty rationale fail validation",t=>{const f=fixture(t);const missing=structuredClone(f.manifest);missing.sections.pop();assert.throws(()=>inspect(f.root,missing),/seven/);f.manifest.sections[0].review.reason=" ";assert.throws(()=>inspect(f.root,f.manifest),/review record/);});
test("missing explicit input and missing evidence fail validation",t=>{const f=fixture(t);f.manifest.sections[0].sources=["missing.cs"];assert.throws(()=>inspect(f.root,f.manifest));f.manifest.sections[0].sources=[f.boundary];rmSync(join(f.root,"tests/Evidence.cs"));assert.throws(()=>inspect(f.root,f.manifest));});
test("broken page heading or diagram include fails validation",t=>{const f=fixture(t);f.write("docs/page.md","## overview");assert.throws(()=>inspect(f.root,f.manifest),/heading/);f.write("docs/page.md",ids.map(id=>"## "+id).join("\n"));f.write("docs/diagram.mmd","flowchart TD");f.manifest.sections[0].diagram="docs/diagram.mmd";assert.throws(()=>inspect(f.root,f.manifest),/not included/);});
test("uncovered calculation inputs and removed boundary fail",t=>{const f=fixture(t);f.write(f.boundary+"/Other.cs","class Other {}");f.manifest.sections.forEach(s=>s.sources=[f.boundary+"/Rule.cs"]);assert.throws(()=>inspect(f.root,f.manifest),/Unmapped/);f.manifest.coverageRoots=[];assert.throws(()=>inspect(f.root,f.manifest),/boundary/);});
test("acknowledgement is explicit and updates only reviewed sections",t=>{
  const f=fixture(t);f.write(f.boundary+"/Rule.cs","class Changed {}");const reports=inspect(f.root,f.manifest);
  assert.throws(()=>acknowledge(f.manifest,reports,[], "review"),/explicit/);
  assert.throws(()=>acknowledge(f.manifest,reports,["overview"]," "),/nonempty/);
  assert.throws(()=>acknowledge(f.manifest,reports,["unknown"],"review"),/explicit/);
  const updated=acknowledge(f.manifest,reports,["overview"],"Refactor reviewed; documented rules remain accurate.");
  assert.equal(inspect(f.root,updated).filter(r=>r.stale).length,6);
  assert.ok(inspect(f.root,f.manifest).every(r=>r.stale));
});
test("repository path traversal is rejected",t=>{const f=fixture(t);f.manifest.page="../outside.md";assert.throws(()=>inspect(f.root,f.manifest),/Invalid repository path/);});


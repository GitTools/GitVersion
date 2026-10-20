import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, writeFileSync, rmSync, renameSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { inspect } from "./check-version-calculation.mjs";
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
  const manifest = {schemaVersion:1,page:"docs/page.md",coverageRoots:[boundary],sharedSources:["shared"],sections:ids.map(id=>({id,heading:id,sources:[boundary],tests:["tests/Evidence.cs"]}))};
  return {root,manifest,write,boundary};
}
test("valid mappings pass without review metadata",t=>{
  const f=fixture(t);
  assert.deepEqual(inspect(f.root,f.manifest),ids.map(id=>({id,heading:id,files:2})));
});
test("source and shared content edits preserve valid mappings",t=>{
  const f=fixture(t); const before=inspect(f.root,f.manifest);
  f.write(f.boundary+"/Rule.cs","class Rule { int Version = 2; }\r\n");
  f.write("shared/Common.cs","class Common { int X; }");
  assert.deepEqual(inspect(f.root,f.manifest),before);
});
test("new directory-owned calculation files are covered automatically",t=>{const f=fixture(t);f.write(f.boundary+"/NewRule.cs","class NewRule {}");assert.deepEqual(inspect(f.root,f.manifest),ids.map(id=>({id,heading:id,files:3})));});
test("deleted directory-owned files pass when sources remain",t=>{const f=fixture(t);f.write(f.boundary+"/Keep.cs","class Keep {}");rmSync(join(f.root,f.boundary,"Rule.cs"));assert.equal(inspect(f.root,f.manifest).length,7);});
test("renamed directory-owned files preserve valid mappings",t=>{const f=fixture(t);const before=inspect(f.root,f.manifest);renameSync(join(f.root,f.boundary,"Rule.cs"),join(f.root,f.boundary,"Renamed.cs"));assert.deepEqual(inspect(f.root,f.manifest),before);});
test("missing sections fail validation",t=>{const f=fixture(t);f.manifest.sections.pop();assert.throws(()=>inspect(f.root,f.manifest),/seven/);});
test("duplicate sections fail validation",t=>{const f=fixture(t);f.manifest.sections[1].id="overview";assert.throws(()=>inspect(f.root,f.manifest),/seven/);});
test("empty source directories fail validation",t=>{const f=fixture(t);rmSync(join(f.root,f.boundary,"Rule.cs"));assert.throws(()=>inspect(f.root,f.manifest),/Empty source mapping/);});
test("missing shared sources fail validation",t=>{const f=fixture(t);rmSync(join(f.root,"shared/Common.cs"));f.manifest.sharedSources=["shared/Common.cs"];assert.throws(()=>inspect(f.root,f.manifest));});
test("missing explicit input and missing evidence fail validation",t=>{const f=fixture(t);f.manifest.sections[0].sources=["missing.cs"];assert.throws(()=>inspect(f.root,f.manifest));f.manifest.sections[0].sources=[f.boundary];rmSync(join(f.root,"tests/Evidence.cs"));assert.throws(()=>inspect(f.root,f.manifest));});
test("broken page heading or diagram include fails validation",t=>{const f=fixture(t);f.write("docs/page.md","## overview");assert.throws(()=>inspect(f.root,f.manifest),/heading/);f.write("docs/page.md",ids.map(id=>"## "+id).join("\n"));f.write("docs/diagram.mmd","flowchart TD");f.manifest.sections[0].diagram="docs/diagram.mmd";assert.throws(()=>inspect(f.root,f.manifest),/not included/);});
test("uncovered calculation inputs and removed boundary fail",t=>{const f=fixture(t);f.write(f.boundary+"/Other.cs","class Other {}");f.manifest.sections.forEach(s=>s.sources=[f.boundary+"/Rule.cs"]);assert.throws(()=>inspect(f.root,f.manifest),/Unmapped/);f.manifest.coverageRoots=[];assert.throws(()=>inspect(f.root,f.manifest),/boundary/);});
test("repository path traversal is rejected",t=>{const f=fixture(t);f.manifest.page="../outside.md";assert.throws(()=>inspect(f.root,f.manifest),/Invalid repository path/);});

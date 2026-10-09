import { createHash } from "node:crypto";
import { readFileSync, readdirSync, realpathSync, statSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const expectedSections = ["overview", "strategies", "inheritance", "increments", "selection", "mainline", "deployment"];
const defaultRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
export function inspect(root, manifest) {
  root = realpathSync(root);
  function safe(path) {
    if (typeof path !== "string" || !path || path.startsWith("/") || path.includes("\\") || path.split("/").some(part => part === ".." || part === ".")) throw Error("Invalid repository path: " + path);
    const absolute = realpathSync(join(root, path));
    if (absolute !== root && !absolute.startsWith(root + sep)) throw Error("Path escapes repository: " + path);
    return absolute;
  }
  function sources(path) {
    const absolute = safe(path);
    if (statSync(absolute).isDirectory()) return readdirSync(absolute).filter(name => !["bin", "obj"].includes(name)).flatMap(name => sources(path.replace(/\/$/, "") + "/" + name));
    return path.endsWith(".cs") ? [path] : [];
  }
  if (manifest.schemaVersion !== 1 || !Array.isArray(manifest.sections) || !Array.isArray(manifest.sharedSources) || !Array.isArray(manifest.coverageRoots)) throw Error("Invalid documentation map schema.");
  const ids = manifest.sections.map(section => section.id);
  if (ids.length !== expectedSections.length || expectedSections.some(id => ids.filter(value => value === id).length !== 1)) throw Error("Map must contain each of the seven algorithm sections exactly once.");
  // This boundary cannot be silently removed by editing the manifest.
  const boundary = "src/GitVersion.Core/VersionCalculation";
  if (!manifest.coverageRoots.includes(boundary)) throw Error("Missing calculation coverage boundary.");
  const page = readFileSync(safe(manifest.page), "utf8");
  const shared = manifest.sharedSources.flatMap(sources);
  const covered = new Set(shared);
  const reports = manifest.sections.map(section => {
    if (!Array.isArray(section.sources) || section.sources.length === 0 || !Array.isArray(section.tests) || section.tests.length === 0) throw Error("Missing source/test mapping: " + section.id);
    if (!/^#+ /m.test(page) || !page.split("\n").some(line => /^## /.test(line) && line.slice(3).trim() === section.heading)) throw Error("Missing heading: " + section.heading);
    for (const test of section.tests) safe(test);
    if (section.diagram) {
      safe(section.diagram);
      const included = [...page.matchAll(/\^"([^"]+)"/g)].some(([,path]) => resolve(dirname(join(root,manifest.page)),path) === resolve(root,section.diagram));
      if (!included) throw Error("Diagram is not included: " + section.diagram);
    }
    const owned = section.sources.flatMap(sources);
    owned.forEach(path => covered.add(path));
    const inputs = [...new Set([...shared, ...owned])].sort();
    if (!owned.length) throw Error("Empty source mapping: " + section.id);
    const hash = createHash("sha256");
    for (const input of inputs) hash.update(input + "\0" + readFileSync(safe(input),"utf8").replaceAll("\r\n","\n") + "\0");
    const digest = hash.digest("hex");
    const review = section.review;
    if (!review || !/^[a-f0-9]{64}$/.test(review.sourceDigest) || typeof review.reason !== "string" || !review.reason.trim()) throw Error("Missing valid review record: " + section.id);
    return {id:section.id, heading:section.heading, digest, stale:digest !== review.sourceDigest, files:inputs.length};
  });
  const missing = manifest.coverageRoots.flatMap(sources).filter(path => !covered.has(path));
  if (missing.length) throw Error("Unmapped calculation sources: " + missing.join(", "));
  return reports;
}
export function acknowledge(manifest, reports, ids, reason) {
  if (typeof reason !== "string" || !reason.trim()) throw Error("Review requires a nonempty reason.");
  if (!ids.length || new Set(ids).size !== ids.length || ids.some(id => !expectedSections.includes(id))) throw Error("Review requires explicit, distinct section IDs.");
  const result = structuredClone(manifest);
  for (const id of ids) result.sections.find(section => section.id === id).review = {sourceDigest:reports.find(report => report.id === id).digest, reason:reason.trim()};
  return result;
}
if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  try {
    const args = process.argv.slice(2);
    const manifestPath = join(defaultRoot,"docs/version-calculation-map.json");
    const manifest = JSON.parse(readFileSync(manifestPath,"utf8"));
    const reports = inspect(defaultRoot,manifest);
    if (args[0] === "--review") {
      if (args.length !== 4 || args[2] !== "--reason") throw Error("Usage: --review overview,selection --reason 'Explain the reviewed change'");
      const updated = acknowledge(manifest,reports,args[1].split(","),args[3]);
      writeFileSync(manifestPath,JSON.stringify(updated,null,2) + "\n");
      console.log("Recorded documentation review for: " + args[1]);
    } else {
      if (args.length) throw Error("Unknown arguments.");
      const stale = reports.filter(report => report.stale);
      if (stale.length) {
        console.error("Algorithm documentation needs review:\n" + stale.map(report => "- " + report.id + ": " + report.heading).join("\n"));
        console.error("Review the mapped code and examples, update prose/diagrams if needed, then record each reviewed section with --review and --reason.");
        process.exitCode = 1;
      } else console.log("Verified documentation review records for all seven algorithm sections.");
    }
  } catch (error) { console.error(error.message); process.exitCode = 1; }
}


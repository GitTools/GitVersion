import { readFileSync, readdirSync, realpathSync, statSync } from "node:fs";
import { dirname, join, resolve, sep } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const expectedSections = ["overview", "strategies", "inheritance", "increments", "selection", "mainline", "deployment"];
const defaultRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
/**
 * Validate documentation mappings and calculation-source coverage without comparing source contents.
 * @param {string} root Repository root containing the mapped files.
 * @param {object} manifest Documentation map with section, source, test, and diagram paths.
 * @returns {{id: string, heading: string, files: number}[]} Validated section summaries.
 * @throws {Error} If the map is invalid, a path is missing or unsafe, or coverage is incomplete.
 */
export function inspect(root, manifest) {
  root = realpathSync(root);
  function safe(path) {
    if (typeof path !== "string" || !path || path.startsWith("/") || path.includes("\\") || path.split("/").some(part => part === ".." || part === ".")) throw new Error("Invalid repository path: " + path);
    const absolute = realpathSync(join(root, path));
    if (absolute !== root && !absolute.startsWith(root + sep)) throw new Error("Path escapes repository: " + path);
    return absolute;
  }
  function sources(path) {
    const absolute = safe(path);
    if (statSync(absolute).isDirectory()) return readdirSync(absolute).filter(name => !["bin", "obj"].includes(name)).flatMap(name => sources(path.replace(/\/$/, "") + "/" + name));
    return path.endsWith(".cs") ? [path] : [];
  }
  if (manifest.schemaVersion !== 1 || !Array.isArray(manifest.sections) || !Array.isArray(manifest.sharedSources) || !Array.isArray(manifest.coverageRoots)) throw new Error("Invalid documentation map schema.");
  const ids = manifest.sections.map(section => section.id);
  if (ids.length !== expectedSections.length || expectedSections.some(id => ids.filter(value => value === id).length !== 1)) throw new Error("Map must contain each of the seven algorithm sections exactly once.");
  // This boundary cannot be silently removed by editing the manifest.
  const boundary = "src/GitVersion.Core/VersionCalculation";
  if (!manifest.coverageRoots.includes(boundary)) throw new Error("Missing calculation coverage boundary.");
  const page = readFileSync(safe(manifest.page), "utf8");
  const shared = manifest.sharedSources.flatMap(sources);
  const covered = new Set(shared);
  const reports = manifest.sections.map(section => {
    if (!Array.isArray(section.sources) || section.sources.length === 0 || !Array.isArray(section.tests) || section.tests.length === 0) throw new Error("Missing source/test mapping: " + section.id);
    if (!page.split("\n").some(line => line.startsWith("## ") && line.slice(3).trim() === section.heading)) throw new Error("Missing heading: " + section.heading);
    for (const test of section.tests) safe(test);
    if (section.diagram) {
      safe(section.diagram);
      const included = [...page.matchAll(/\^"([^"]+)"/g)].some(([,path]) => resolve(dirname(join(root,manifest.page)),path) === resolve(root,section.diagram));
      if (!included) throw new Error("Diagram is not included: " + section.diagram);
    }
    const owned = section.sources.flatMap(sources);
    owned.forEach(path => covered.add(path));
    if (!owned.length) throw new Error("Empty source mapping: " + section.id);
    return {id:section.id, heading:section.heading, files:new Set([...shared, ...owned]).size};
  });
  const missing = manifest.coverageRoots.flatMap(sources).filter(path => !covered.has(path));
  if (missing.length) throw new Error("Unmapped calculation sources: " + missing.join(", "));
  return reports;
}
if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  try {
    if (process.argv.length > 2) throw new Error("Unknown arguments.");
    const manifestPath = join(defaultRoot,"docs/version-calculation-map.json");
    const manifest = JSON.parse(readFileSync(manifestPath,"utf8"));
    inspect(defaultRoot,manifest);
    console.log("Verified documentation mappings for all seven algorithm sections.");
  } catch (error) { console.error(error.message); process.exitCode = 1; }
}

import { readFileSync, readdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import mermaid from "mermaid";

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const sourceDirectory = join(repositoryRoot, "docs", "diagrams");
const configFile = join(
  repositoryRoot,
  "docs",
  "input",
  "assets",
  "js",
  "mermaid-config.js",
);
const sourceFiles = readdirSync(sourceDirectory)
  .filter((file) => file.endsWith(".mmd"))
  .sort();

if (sourceFiles.length === 0) {
  throw new Error(`No Mermaid sources found in ${sourceDirectory}.`);
}

await import(pathToFileURL(configFile));
mermaid.initialize({ ...globalThis.gitVersionMermaidConfig, startOnLoad: false });

const invalidFiles = [];
for (const sourceFile of sourceFiles) {
  try {
    await mermaid.parse(readFileSync(join(sourceDirectory, sourceFile), "utf8"));
  } catch (error) {
    invalidFiles.push({ sourceFile, error });
  }
}

if (invalidFiles.length > 0) {
  console.error("Invalid Mermaid sources:");
  for (const { sourceFile, error } of invalidFiles) {
    console.error(`- ${join(sourceDirectory, sourceFile)}: ${error.message}`);
  }
  process.exit(1);
}

console.log(`Validated ${sourceFiles.length} Mermaid diagrams.`);

import { spawnSync } from "node:child_process";
import {
  mkdtempSync,
  mkdirSync,
  readFileSync,
  readdirSync,
  rmSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const check = process.argv.includes("--check");
const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const sourceDirectory = join(repositoryRoot, "docs", "diagrams");
const outputDirectory = join(repositoryRoot, "docs", "input", "docs", "img");
const configFile = join(repositoryRoot, "docs", "mermaid-config.json");
const puppeteerConfigFile = join(
  repositoryRoot,
  "docs",
  "puppeteer-ci-config.json",
);
const executable = join(
  repositoryRoot,
  "node_modules",
  ".bin",
  process.platform === "win32" ? "mmdc.cmd" : "mmdc",
);
const temporaryDirectory = check
  ? mkdtempSync(join(tmpdir(), "gitversion-mermaid-"))
  : undefined;

const sourceFiles = readdirSync(sourceDirectory)
  .filter((file) => file.endsWith(".mmd"))
  .sort();

if (sourceFiles.length === 0) {
  throw new Error(`No Mermaid sources found in ${sourceDirectory}.`);
}

if (!check) {
  mkdirSync(outputDirectory, { recursive: true });
}
const staleFiles = [];

try {
  for (const sourceFile of sourceFiles) {
    const name = basename(sourceFile, ".mmd");
    const source = join(sourceDirectory, sourceFile);
    const expectedOutput = join(outputDirectory, `${name}.svg`);
    const renderedOutput = check
      ? join(temporaryDirectory, `${name}.svg`)
      : expectedOutput;
    const result = spawnSync(
      executable,
      [
        "--input",
        source,
        "--output",
        renderedOutput,
        "--configFile",
        configFile,
        "--backgroundColor",
        "transparent",
        "--svgId",
        `mermaid-${name}`,
        ...(process.env.CI
          ? ["--puppeteerConfigFile", puppeteerConfigFile]
          : []),
        "--quiet",
      ],
      { stdio: "inherit" },
    );

    if (result.error) {
      throw new Error(
        `Failed to start Mermaid CLI "${executable}": ${result.error.message}`,
        { cause: result.error },
      );
    }

    if (result.status !== 0) {
      throw new Error(
        `Mermaid rendering failed for ${sourceFile} with exit code ${result.status ?? 1}.`,
      );
    }

    if (check) {
      try {
        const expected = readFileSync(expectedOutput);
        const actual = readFileSync(renderedOutput);
        if (!expected.equals(actual)) {
          staleFiles.push(expectedOutput);
        }
      } catch {
        staleFiles.push(expectedOutput);
      }
    }
  }
} finally {
  if (temporaryDirectory) {
    rmSync(temporaryDirectory, { recursive: true, force: true });
  }
}

if (staleFiles.length > 0) {
  console.error("Mermaid SVGs are missing or stale:");
  for (const file of staleFiles) {
    console.error(`- ${file}`);
  }
  console.error("Run `npm run diagrams:generate` and commit the results.");
  process.exit(1);
}

console.log(
  check
    ? `Verified ${sourceFiles.length} Mermaid diagrams.`
    : `Generated ${sourceFiles.length} Mermaid diagrams.`,
);

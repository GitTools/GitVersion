import { spawnSync } from "node:child_process";
import {
  existsSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  rmSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { dirname, isAbsolute, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const check = process.argv.includes("--check");
const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const dotnetRoot = process.env.DOTNET_ROOT;
const dotnetExecutableName =
  process.platform === "win32" ? "dotnet.exe" : "dotnet";
const dotnetExecutable = dotnetRoot
  ? join(dotnetRoot, dotnetExecutableName)
  : dotnetExecutableName;
const sourceDirectory = join(repositoryRoot, "docs", "diagrams");
const outputDirectory = check
  ? mkdtempSync(join(tmpdir(), "gitversion-mermaid-sources-"))
  : sourceDirectory;
const normalizeLineEndings = (text) => text.replace(/\r\n?/g, "\n");
const getGeneratedFileNames = (directory) =>
  readdirSync(directory)
    .filter(
      (file) => file.startsWith("DocumentationSamplesFor") && file.endsWith(".mmd"),
    )
    .sort();

if (
  dotnetRoot &&
  (!isAbsolute(dotnetRoot) || !existsSync(dotnetExecutable))
) {
  throw new Error(`DOTNET_ROOT does not contain ${dotnetExecutableName}.`);
}

try {
  const result = spawnSync(
    dotnetExecutable, // NOSONAR -- CI validates DOTNET_ROOT; PATH fallback supports contributors.
    [
      "test",
      "--project",
      join(
        repositoryRoot,
        "src",
        "GitVersion.Core.Tests",
        "GitVersion.Core.Tests.csproj",
      ),
      "--filter",
      "FullyQualifiedName~DocumentationSamplesForGitFlow|FullyQualifiedName~DocumentationSamplesForGitHubFlow",
    ],
    {
      cwd: repositoryRoot,
      env: {
        ...process.env,
        MERMAID_OUTPUT_DIRECTORY: outputDirectory,
      },
      stdio: "inherit",
    },
  );

  if (result.error) {
    throw new Error(
      `Failed to start dotnet executable "${dotnetExecutable}": ${result.error.message}`,
      { cause: result.error },
    );
  }

  if (result.status !== 0) {
    throw new Error(
      `Documentation scenario tests failed with exit code ${result.status ?? 1}.`,
    );
  }

  const generatedFiles = getGeneratedFileNames(outputDirectory);

  if (check) {
    const committedFiles = getGeneratedFileNames(sourceDirectory);
    const missingFiles = generatedFiles.filter(
      (file) => !committedFiles.includes(file),
    );
    const obsoleteFiles = committedFiles.filter(
      (file) => !generatedFiles.includes(file),
    );

    if (missingFiles.length > 0 || obsoleteFiles.length > 0) {
      console.error("Test-generated Mermaid source file names are out of date:");
      for (const file of missingFiles) {
        console.error(`- Missing: ${join(sourceDirectory, file)}`);
      }
      for (const file of obsoleteFiles) {
        console.error(`- Obsolete: ${join(sourceDirectory, file)}`);
      }
      console.error("Run `npm run diagrams:sources` and commit the results.");
      throw new Error("Test-generated Mermaid source file names are out of date.");
    }

    const staleFiles = generatedFiles.filter((file) => {
      try {
        const expected = normalizeLineEndings(
          readFileSync(join(sourceDirectory, file), "utf8"),
        );
        const actual = normalizeLineEndings(
          readFileSync(join(outputDirectory, file), "utf8"),
        );
        return expected !== actual;
      } catch {
        return true;
      }
    });

    if (staleFiles.length > 0) {
      console.error("Test-generated Mermaid sources are missing or stale:");
      for (const file of staleFiles) {
        console.error(`- ${join(sourceDirectory, file)}`);
      }
      console.error("Run `npm run diagrams:sources` and commit the results.");
      throw new Error("Test-generated Mermaid sources are stale.");
    }
  }

  console.log(
    check
      ? `Verified ${generatedFiles.length} test-generated Mermaid sources.`
      : `Generated ${generatedFiles.length} Mermaid sources from tests.`,
  );
} finally {
  if (check) {
    rmSync(outputDirectory, { recursive: true, force: true });
  }
}

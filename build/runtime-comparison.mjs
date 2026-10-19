#!/usr/bin/env node
// Linux-only, dependency-free fresh-process measurements. See runtime-comparison.md.
import { spawnSync } from 'node:child_process';
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { cpus, release, arch } from 'node:os';

const [net10, net11, runtime10, runtime11, destination] = process.argv.slice(2);
if (!destination || !runtime10?.startsWith('10.') || !runtime11?.startsWith('11.')) {
  throw new Error('Usage: node build/runtime-comparison.mjs <net10/gitversion.dll> <net11/gitversion.dll> <runtime10> <runtime11> <new-output-directory>');
}
if (process.platform !== 'linux') throw new Error('Requires Linux and GNU /usr/bin/time.');
const repetitions = Number(process.env.COMPARISON_REPETITIONS ?? 15);
const sizes = (process.env.COMPARISON_SIZES ?? '100,5000').split(',').map(Number);
if (!Number.isInteger(repetitions) || repetitions < 2 || repetitions > 100 ||
    sizes.length !== 2 || sizes.some(n => !Number.isInteger(n) || n < 2 || n > 50000)) {
  throw new Error('Use 2–100 repetitions and two fixture sizes of 2–50000 commits.');
}
const output = resolve(destination);
if (existsSync(output)) throw new Error(`Refusing to reuse output directory: ${output}`);
mkdirSync(output, { recursive: true });
// Keep only process-location essentials: CI branch/agent variables and GitVersion
// overrides must not make fixture calculations depend on the hosting job.
const inherited = Object.fromEntries(Object.entries(process.env).filter(([key]) =>
  /^(PATH|HOME|TMPDIR|TMP|TEMP|DOTNET_ROOT(?:_[A-Z0-9]+)?)$/.test(key)));
const env = { ...inherited, LC_ALL: 'C', TZ: 'UTC', DOTNET_ROLL_FORWARD: 'Disable',
  DOTNET_CLI_TELEMETRY_OPTOUT: '1', GIT_CONFIG_NOSYSTEM: '1',
  GIT_CONFIG_GLOBAL: '/dev/null', GIT_TERMINAL_PROMPT: '0' };
function run(command, args, options = {}) {
  const result = spawnSync(command, args, { env, encoding: 'utf8', timeout: 120000,
    maxBuffer: 32 * 1024 * 1024, ...options });
  if (result.error || result.status !== 0) {
    throw new Error(`${command} failed (${result.status}): ${result.error ?? result.stderr}`);
  }
  return result.stdout.trim();
}
function json(path, value) { writeFileSync(path, JSON.stringify(value, null, 2) + '\n'); }
const variants = [
  { name: 'net10-runtime10', dll: resolve(net10), runtime: runtime10 },
  { name: 'net10-runtime11', dll: resolve(net10), runtime: runtime11 },
  { name: 'net11-runtime11', dll: resolve(net11), runtime: runtime11 },
];
const metadata = { source: run('git', ['rev-parse', 'HEAD']), dirty: run('git', ['status', '--porcelain']),
  sdk: run('dotnet', ['--version']), runtimes: run('dotnet', ['--list-runtimes']),
  git: run('git', ['--version']), node: process.version, os: release(), arch: arch(),
  cpu: cpus()[0]?.model, repetitions, sizes, variants };
json(join(output, 'environment.json'), metadata);
for (const variant of variants) {
  const trace = join(output, `${variant.name}.host.txt`);
  run('dotnet', ['exec', '--fx-version', variant.runtime, variant.dll, '--version'], {
    env: { ...env, COREHOST_TRACE: '1', COREHOST_TRACEFILE: trace,
      DOTNET_HOST_TRACE: '1', DOTNET_HOST_TRACEFILE: trace },
  });
  const text = readFileSync(trace, 'utf8');
  const selected = text.split('\n').filter(line => line.includes('CoreCLR path ='));
  if (!selected.some(line => line.includes(`/Microsoft.NETCore.App/${variant.runtime}/libcoreclr.so`))) {
    throw new Error(`Could not verify loaded runtime ${variant.runtime}; inspect ${trace}`);
  }
}

const fixtures = [];
for (const [index, size] of sizes.entries()) {
  const name = index === 0 ? 'small' : 'large';
  const path = join(output, name);
  run('git', ['init', '--initial-branch=main', path]);
  // Fixed identity, timestamps, content and parent chain yield reproducible object IDs.
  let input = '';
  for (let i = 1; i <= size; i++) {
    const content = `fixture ${i}\n`;
    const message = `Commit ${i}\n`;
    input += `commit refs/heads/main\nmark :${i}\ncommitter Fixture <fixture@example.invalid> ${1700000000 + i} +0000\ndata ${Buffer.byteLength(message)}\n${message}`;
    if (i > 1) input += `from :${i - 1}\n`;
    input += `M 100644 inline fixture.txt\ndata ${Buffer.byteLength(content)}\n${content}\n`;
  }
  run('git', ['-C', path, 'fast-import', '--quiet'], { input });
  run('git', ['-C', path, 'reset', '--hard']);
  run('git', ['-C', path, 'tag', '1.0.0', `main~${size - 1}`]);
  run('git', ['-C', path, 'gc', '--aggressive', '--prune=now']);
  fixtures.push({ name, path, size, head: run('git', ['-C', path, 'rev-parse', 'HEAD']) });
}
json(join(output, 'fixtures.json'), fixtures);
const rows = [];
const references = new Map();
function capture(fixture, backend, variant, cache, sample) {
  const id = `${fixture.name}-${backend}-${variant.name}-${cache}-${sample}`;
  const metrics = join(output, `${id}.time`);
  const args = ['exec', '--fx-version', variant.runtime, variant.dll, fixture.path,
    '--output', 'json', '--no-normalize', '--no-fetch', '--verbosity', 'Quiet'];
  if (cache === 'bypass') args.push('--no-cache');
  const stdout = run('/usr/bin/time', ['-f', '%e,%U,%S,%M', '-o', metrics, 'dotnet', ...args], {
    env: { ...env, GITVERSION_GIT_BACKEND: backend },
  });
  const parsed = JSON.parse(stdout);
  const canonical = JSON.stringify(parsed, Object.keys(parsed).sort());
  if (references.has(fixture.name) && references.get(fixture.name) !== canonical) {
    throw new Error(`Version output mismatch: ${id}`);
  }
  references.set(fixture.name, canonical);
  json(join(output, `${id}.json`), parsed);
  const [elapsedSeconds, userSeconds, systemSeconds, peakRssKiB] = readFileSync(metrics, 'utf8').trim().split(',').map(Number);
  if (![elapsedSeconds, userSeconds, systemSeconds, peakRssKiB].every(Number.isFinite) || peakRssKiB <= 0) {
    throw new Error(`Invalid GNU time metrics: ${metrics}`);
  }
  return { fixture: fixture.name, backend, variant: variant.name, cache, sample,
    elapsedSeconds, userSeconds, systemSeconds, cpuSeconds: userSeconds + systemSeconds, peakRssKiB };
}
for (const fixture of fixtures) {
  const cases = ['libgit2', 'managed'].flatMap(backend => variants.flatMap(variant =>
    ['bypass', 'hit'].map(cache => ({ backend, variant, cache }))));
  // Unmeasured warm-up includes cache population. Each sample is still a new process.
  for (const c of cases) capture(fixture, c.backend, c.variant, c.cache, 'warmup');
  for (let sample = 0; sample < repetitions; sample++) {
    // Rotate case order to distribute runner drift across variants.
    for (let j = 0; j < cases.length; j++) {
      const c = cases[(j + sample) % cases.length];
      rows.push(capture(fixture, c.backend, c.variant, c.cache, sample));
      json(join(output, 'samples.json'), rows);
    }
  }
}
const summary = [];
for (const row of rows) {
  const key = [row.fixture, row.backend, row.variant, row.cache].join('/');
  if (summary.some(item => item.case === key)) continue;
  const group = rows.filter(r => [r.fixture, r.backend, r.variant, r.cache].join('/') === key);
  const stats = field => {
    const values = group.map(r => r[field]).sort((a, b) => a - b);
    const middle = Math.floor(values.length / 2);
    return { min: values[0], median: values.length % 2 ? values[middle] : (values[middle - 1] + values[middle]) / 2,
      p95: values[Math.ceil(values.length * .95) - 1], max: values.at(-1) };
  };
  summary.push({ case: key, samples: group.length, elapsedSeconds: stats('elapsedSeconds'),
    userSeconds: stats('userSeconds'), systemSeconds: stats('systemSeconds'), cpuSeconds: stats('cpuSeconds'), peakRssKiB: stats('peakRssKiB') });
}
json(join(output, 'summary.json'), summary);
writeFileSync(join(output, 'summary.md'), [
  '# Runtime comparison',
  '',
  'Informational fresh-process measurements; shared-runner timings are not a performance gate.',
  '',
  '| Fixture / backend / build-runtime / cache | Samples | Median seconds | p95 seconds | Median CPU seconds | Median peak RSS KiB |',
  '|---|---:|---:|---:|---:|---:|',
  ...summary.map(s => `| ${s.case} | ${s.samples} | ${s.elapsedSeconds.median} | ${s.elapsedSeconds.p95} | ${s.cpuSeconds.median.toFixed(3)} | ${s.peakRssKiB.median} |`),
  '',
  'All version JSON outputs matched. See the artifact for raw samples, runtime traces and environment metadata.',
  '',
].join('\n'));
console.log(`Output parity verified across ${rows.length} samples. Results: ${output}`);

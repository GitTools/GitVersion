import {execFileSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {appendFileSync, existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {setTimeout as sleep} from 'node:timers/promises';
import {pathToFileURL} from 'node:url';

const upstream = 'SchemaStore/schemastore';
const bot = 'gittools-bot';
const fork = `${bot}/schemastore`;
const catalogPath = 'src/api/json/catalog.json';
const wrapperPath = 'src/schemas/json/gitversion.json';

export function parseVersion(version) {
    if (!/^(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(version)) throw new Error(`Invalid schema version: ${version}`);
    const parts = version.split('.').map(Number);
    if (!parts.every(Number.isSafeInteger)) throw new Error(`Invalid schema version: ${version}`);
    return parts;
}

function compareVersions(left, right) {
    const a = parseVersion(left);
    const b = parseVersion(right);
    return a[0] - b[0] || a[1] - b[1];
}

export function schemaUrl(version) {
    parseVersion(version);
    return `https://gitversion.net/schemas/${version}/GitVersion.configuration.json`;
}

function objectSpans(text) {
    const stack = [];
    const spans = [];
    let quoted = false;
    let escaped = false;
    for (let index = 0; index < text.length; index++) {
        const character = text[index];
        if (quoted) {
            if (escaped) escaped = false;
            else if (character === '\\') escaped = true;
            else if (character === '"') quoted = false;
        } else if (character === '"') quoted = true;
        else if (character === '{') stack.push(index);
        else if (character === '}') spans.push({start: stack.pop(), end: index + 1});
    }
    return spans;
}

function replaceStringProperty(text, key, value) {
    const escapedKey = key.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`);
    const pattern = new RegExp(String.raw`(?<!\\)("${escapedKey}"\s*:\s*)"(?:\\.|[^"\\])*"`, 'g');
    if ([...text.matchAll(pattern)].length !== 1) throw new Error(`Expected one ${key} property`);
    return text.replace(pattern, (_, prefix) => prefix + JSON.stringify(value));
}

function catalogEntry(catalogText) {
    const entries = JSON.parse(catalogText).schemas.filter(entry => entry.name === 'GitVersion');
    if (entries.length !== 1) throw new Error('Expected one GitVersion catalog entry');
    const entry = entries[0];
    const current = /^https:\/\/gitversion\.net\/schemas\/(\d+\.\d+)\/GitVersion\.configuration\.json$/.exec(entry.url);
    if (!current) throw new Error('Invalid default GitVersion schema URL');
    parseVersion(current[1]);
    if (!entry.versions || Array.isArray(entry.versions) || typeof entry.versions !== 'object') {
        throw new Error('Expected a GitVersion versions object');
    }
    const candidates = objectSpans(catalogText).filter(span => {
        const text = catalogText.slice(span.start, span.end);
        return text.includes('"GitVersion"') && JSON.parse(text).name === 'GitVersion';
    });
    if (candidates.length !== 1) throw new Error('Cannot locate the GitVersion catalog object');
    return {entry, current: current[1], span: candidates[0]};
}

function updateVersions(block, original, schemas) {
    if (schemas.length === 0) return block;
    const versions = {...original};
    for (const schema of schemas) versions[schema.version] = schema.url;
    const ordered = Object.fromEntries(Object.entries(versions).sort(([a], [b]) => compareVersions(a, b)));
    const match = /(?<!\\)"versions"\s*:\s*\{/.exec(block);
    if (!match) throw new Error('Cannot locate GitVersion versions');
    const start = match.index + match[0].lastIndexOf('{');
    const end = objectSpans(block).find(value => value.start === start)?.end;
    if (end === undefined) throw new Error('Cannot locate the end of GitVersion versions');
    const lineStart = block.lastIndexOf('\n', match.index) + 1;
    const indent = /^[ \t]*/.exec(block.slice(lineStart, match.index))[0];
    const newline = block.includes('\r\n') ? '\r\n' : '\n';
    const replacement = JSON.stringify(ordered, null, 2).replaceAll('\n', newline + indent);
    return block.slice(0, start) + replacement + block.slice(end);
}

export function updateSchemaStore(catalogText, wrapperText, schemas) {
    if (schemas.length === 0) return {changed: false, catalogText, wrapperText, schemas: []};
    const {entry, current, span} = catalogEntry(catalogText);
    const sorted = [...schemas].sort((a, b) => compareVersions(a.version, b.version));
    const latest = sorted.at(-1);
    const updates = sorted.filter(schema => entry.versions[schema.version] !== schema.url);
    let block = updateVersions(catalogText.slice(span.start, span.end), entry.versions, updates);
    let newWrapper = wrapperText;
    const checks = new Map(updates.map(schema => [schema.version, schema]));
    if (compareVersions(latest.version, current) >= 0) {
        if (entry.url !== latest.url) block = replaceStringProperty(block, 'url', latest.url);
        const reference = JSON.parse(wrapperText).$ref;
        if (typeof reference !== 'string') throw new Error('Missing SchemaStore wrapper $ref');
        if (reference !== latest.url) newWrapper = replaceStringProperty(wrapperText, '$ref', latest.url);
        if (entry.url !== latest.url || wrapperText !== newWrapper) checks.set(latest.version, latest);
    }
    const newCatalog = catalogText.slice(0, span.start) + block + catalogText.slice(span.end);
    JSON.parse(newCatalog);
    JSON.parse(newWrapper);
    return {changed: newCatalog !== catalogText || newWrapper !== wrapperText,
        catalogText: newCatalog, wrapperText: newWrapper, version: latest.version, schemas: [...checks.values()]};
}

function readPublishedSchema(directory, input, latest, requested) {
    const version = input.edition.id;
    parseVersion(version);
    if (input.release.split('.').slice(0, 2).join('.') !== version) throw new Error(`Release does not match edition ${version}`);
    const path = join(directory, 'schemas', version, 'GitVersion.configuration.json');
    if (!existsSync(path)) {
        if (requested || version === latest) throw new Error(`Missing schema for stable edition ${version}`);
        return undefined;
    }
    const bytes = readFileSync(path);
    JSON.parse(bytes.toString('utf8'));
    return {version, url: schemaUrl(version), digest: createHash('sha256').update(bytes).digest('hex')};
}

export function publishedSchemas(directory, requested = '') {
    if (requested) parseVersion(requested);
    const inputs = JSON.parse(readFileSync(join(directory, 'build-inputs.json'), 'utf8'));
    const stable = inputs.filter(input => input.edition?.status === 'release' && /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(input.release));
    const latest = stable.map(input => input.edition.id).sort(compareVersions).at(-1);
    const schemas = new Map();
    const seen = new Set();
    for (const input of stable) {
        const version = input.edition.id;
        if (requested && version !== requested) continue;
        if (seen.has(version)) throw new Error(`Duplicate stable edition ${version}`);
        seen.add(version);
        const schema = readPublishedSchema(directory, input, latest, requested);
        if (schema) schemas.set(version, schema);
    }
    if (requested && !schemas.has(requested)) throw new Error(`No stable published edition ${requested}`);
    return [...schemas.values()];
}

function command(program, args, options = {}) {
    return execFileSync(program, args, {encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], ...options}).trim();
}

async function download(url) {
    const response = await fetch(url, {signal: AbortSignal.timeout(20000)});
    if (!response.ok) throw new Error(`${url}: HTTP ${response.status}`);
    return Buffer.from(await response.arrayBuffer());
}

function jsonCommand(run, args) {
    return JSON.parse(run('gh', args));
}

async function retry(operation, {attempts, delay, interval, retryable = () => true}) {
    try {
        return await operation();
    } catch (error_) {
        if (attempts <= 1 || !retryable(error_)) throw error_;
        await delay(interval);
        return retry(operation, {attempts: attempts - 1, delay, interval, retryable});
    }
}

export async function waitForSchemas(schemas, {fetchBytes = download, delay = sleep, attempts = 12} = {}) {
    const [schema, ...remaining] = schemas;
    if (!schema) return;
    try {
        await retry(async () => {
            const bytes = await fetchBytes(schema.url);
            if (createHash('sha256').update(bytes).digest('hex') !== schema.digest) throw new Error('Published schema content has not propagated');
            JSON.parse(bytes.toString('utf8'));
        }, {attempts, delay, interval: 10000});
    } catch (error_) {
        throw new Error(`Public schema is not ready: ${schema.url}`, {cause: error_});
    }
    await waitForSchemas(remaining, {fetchBytes, delay, attempts});
}

export async function createPlan(directory, {run = command, fetchBytes = download, version = ''} = {}) {
    const schemas = publishedSchemas(directory, version);
    if (schemas.length === 0) return {changed: false, schemas: []};
    const repository = jsonCommand(run, ['api', `repos/${upstream}`]);
    const branch = repository.default_branch;
    const ref = jsonCommand(run, ['api', `repos/${upstream}/git/ref/heads/${branch}`]);
    const raw = `https://raw.githubusercontent.com/${upstream}/${ref.object.sha}/`;
    const catalog = await fetchBytes(raw + catalogPath);
    const wrapper = await fetchBytes(raw + wrapperPath);
    return {...updateSchemaStore(catalog.toString('utf8'), wrapper.toString('utf8'), schemas), branch, published: schemas};
}

function readUpdate(directory, schemas) {
    return updateSchemaStore(readFileSync(join(directory, catalogPath), 'utf8'), readFileSync(join(directory, wrapperPath), 'utf8'), schemas);
}

async function waitForFork(run, delay) {
    return retry(() => jsonCommand(run, ['api', `repos/${fork}`]), {
        attempts: 5, delay, interval: 1000,
        retryable: error_ => String(error_.stderr).includes('(HTTP 404)')
    });
}

async function ensureFork(run, branch, delay) {
    let repository;
    try {
        repository = jsonCommand(run, ['api', `repos/${fork}`]);
    } catch (error) {
        if (!String(error.stderr).includes('(HTTP 404)')) throw error;
        run('gh', ['api', `repos/${upstream}/forks`, '--method', 'POST', '-F', 'default_branch_only=true']);
        repository = await waitForFork(run, delay);
    }
    if (!repository.fork || repository.parent?.full_name !== upstream || repository.default_branch !== branch) {
        throw new Error(`Unexpected fork or default branch for ${fork}`);
    }
    run('gh', ['repo', 'sync', fork, '--source', upstream, '--branch', branch]);
    const comparison = jsonCommand(run, ['api', `repos/${upstream}/compare/${branch}...${bot}:${branch}`]);
    if (comparison.ahead_by !== 0) throw new Error('The bot fork default branch contains unique commits');
}

function reuseBranch(run, checkout, branch, update, plan, prs) {
    const remote = run('git', ['ls-remote', 'origin', `refs/heads/${branch}`], {cwd: checkout});
    if (!remote) {
        if (prs.length !== 0) throw new Error(`The existing ${branch} PR has no branch`);
        return {exists: false};
    }
    run('git', ['fetch', '--depth=1', 'origin', `refs/heads/${branch}`], {cwd: checkout});
    run('git', ['checkout', '--detach', 'FETCH_HEAD'], {cwd: checkout});
    const existing = readUpdate(checkout, plan.published);
    const defaultUrl = text => catalogEntry(text).entry.url;
    if (existing.changed || defaultUrl(existing.catalogText) !== defaultUrl(update.catalogText) ||
        JSON.parse(existing.wrapperText).$ref !== JSON.parse(update.wrapperText).$ref) {
        throw new Error(`Existing ${branch} differs; refusing to overwrite it`);
    }
    if (prs.length === 1) return {exists: true, url: prs[0].url};
    const difference = jsonCommand(run, ['api', `repos/${upstream}/compare/${plan.branch}...${bot}:${branch}`]);
    if (!difference.files?.length || difference.files.some(file => ![catalogPath, wrapperPath].includes(file.filename))) {
        throw new Error(`Unexpected changes on ${branch}`);
    }
    return {exists: true};
}

function commitUpdate(run, checkout, branch, update) {
    run('git', ['checkout', '-b', branch], {cwd: checkout});
    writeFileSync(join(checkout, catalogPath), update.catalogText);
    writeFileSync(join(checkout, wrapperPath), update.wrapperText);
    run('git', ['config', 'user.name', 'GitTools Bot'], {cwd: checkout});
    run('git', ['config', 'user.email', 'gittoolsbot@outlook.com'], {cwd: checkout});
    run('git', ['add', '--', catalogPath, wrapperPath], {cwd: checkout});
    run('git', ['commit', '-m', `update gitversion schema to ${update.version}`], {cwd: checkout});
    run('git', ['push', '--set-upstream', 'origin', branch], {cwd: checkout});
}

function openPullRequest(run, temporary, branch, update, base) {
    const body = join(temporary, 'body.md');
    const versions = update.schemas.map(schema => `- [${schema.version}](${schema.url})`).join('\n');
    writeFileSync(body, `Update GitVersion configuration schemas after stable documentation publication.\n\n${versions}\n\nOlder versions remain available; backfills do not lower the default version.\n\nCreated by GitTools Bot on behalf of Artur (@arturcic).\n`);
    return run('gh', ['pr', 'create', '--repo', upstream, '--head', `${bot}:${branch}`, '--base', base,
        '--title', `update gitversion schema to ${update.version}`, '--body-file', body]);
}

export async function publishSchemaStore(directory, {run = command, fetchBytes = download, delay = sleep, version = ''} = {}) {
    const plan = await createPlan(directory, {run, fetchBytes, version});
    if (!plan.changed) return {changed: false};
    const user = jsonCommand(run, ['api', 'user']);
    if (user.login !== bot) throw new Error(`SchemaStore publishing requires ${bot}, found ${user.login}`);
    await waitForSchemas(plan.schemas, {fetchBytes, delay});
    await ensureFork(run, plan.branch, delay);
    run('gh', ['auth', 'setup-git']);
    const temporary = mkdtempSync(join(tmpdir(), 'gitversion-schemastore-'));
    try {
        const checkout = join(temporary, 'repository');
        run('gh', ['repo', 'clone', fork, checkout, '--', '--depth=1', '--branch', plan.branch]);
        const update = readUpdate(checkout, plan.published);
        if (!update.changed) return {changed: false};
        const branch = `gitversion-schema-${update.version}`;
        const prs = jsonCommand(run, ['pr', 'list', '--repo', upstream, '--head', `${bot}:${branch}`, '--state', 'all', '--json', 'number,url,state']);
        if (prs.length > 1 || prs.some(pr => pr.state !== 'OPEN')) throw new Error(`A previous ${branch} PR needs maintainer attention`);
        const previous = reuseBranch(run, checkout, branch, update, plan, prs);
        if (previous.url) return {changed: false, url: previous.url};
        if (!previous.exists) commitUpdate(run, checkout, branch, update);
        return {changed: true, url: openPullRequest(run, temporary, branch, update, plan.branch)};
    } finally {
        rmSync(temporary, {recursive: true, force: true});
    }
}

async function main() {
    const [mode, flag, directory] = process.argv.slice(2);
    if (!['plan', 'publish'].includes(mode) || flag !== '--published-dir' || !directory || process.argv.length !== 5) {
        throw new Error('Usage: node schemastore.mjs plan|publish --published-dir DIRECTORY');
    }
    const options = {version: process.env.SCHEMA_VERSION || ''};
    const result = mode === 'plan' ? await createPlan(directory, options) : await publishSchemaStore(directory, options);
    if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, `changed=${result.changed}\n`);
    console.log(result.url || (result.changed ? 'Stable schema updates are ready.' : 'No SchemaStore update needed.'));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try {
        await main();
    } catch (error_) {
        console.error(error_.message);
        process.exitCode = 1;
    }
}

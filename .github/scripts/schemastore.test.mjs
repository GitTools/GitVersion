import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {test} from 'node:test';
import {createPlan, parseVersion, publishedSchemas, publishSchemaStore, schemaUrl, updateSchemaStore, waitForSchemas} from './schemastore.mjs';

const catalogPath = 'src/api/json/catalog.json';
const wrapperPath = 'src/schemas/json/gitversion.json';
const url = version => `https://gitversion.net/schemas/${version}/GitVersion.configuration.json`;
const schemaBytes = version => Buffer.from(JSON.stringify({$id: url(version), type: 'object'}));
const schema = version => ({version, url: url(version), digest: createHash('sha256').update(schemaBytes(version)).digest('hex')});

function fixture(current = '6.8', versions = {[current]: url(current)}) {
    const prefix = '{\n  "schemas": [\n    {"name":"Before","fileMatch":["a","b"]},\n';
    const suffix = ',\n    {"name":"After","description":"braces { } in a string","fileMatch":["c","d"]}\n  ]\n}\n';
    const entry = {name: 'GitVersion', description: 'Configuration file', fileMatch: ['GitVersion.yml', 'GitVersion.yaml'], url: url(current), versions};
    const block = JSON.stringify(entry, null, 2).split('\n').map(line => `    ${line}`).join('\n');
    return {catalog: prefix + block + suffix, wrapper: JSON.stringify({$ref: url(current)}) + '\n', prefix, suffix};
}

function entry(catalog) {
    return JSON.parse(catalog).schemas.find(value => value.name === 'GitVersion');
}

function published(t, rows = [{edition: {id: '7.0', status: 'release'}, release: '7.0.0'}]) {
    const root = mkdtempSync(join(tmpdir(), 'schemastore-test-'));
    t.after(() => rmSync(root, {recursive: true, force: true}));
    writeFileSync(join(root, 'build-inputs.json'), JSON.stringify(rows));
    for (const row of rows) {
        const directory = join(root, 'schemas', row.edition.id);
        mkdirSync(directory, {recursive: true});
        writeFileSync(join(directory, 'GitVersion.configuration.json'), schemaBytes(row.edition.id));
    }
    return root;
}

function files(directory, value) {
    for (const [path, contents] of [[catalogPath, value.catalog], [wrapperPath, value.wrapper]]) {
        mkdirSync(join(directory, path, '..'), {recursive: true});
        writeFileSync(join(directory, path), contents);
    }
}

test('promotes defaults numerically and preserves defaults on older backfills', async t => {
    const cases = [
        {name: 'newer minor', current: '6.9', next: '6.10', expected: '6.10'},
        {name: 'newer major', current: '6.99', next: '7.0', expected: '7.0'},
        {name: 'older minor', current: '6.10', next: '6.9', expected: '6.10'},
        {name: 'older major', current: '7.0', next: '6.99', expected: '7.0'}
    ];
    for (const value of cases) await t.test(value.name, () => {
        const original = fixture(value.current);
        const result = updateSchemaStore(original.catalog, original.wrapper, [schema(value.next)]);
        assert.equal(entry(result.catalogText).url, url(value.expected));
        assert.equal(JSON.parse(result.wrapperText).$ref, url(value.expected));
        assert.equal(entry(result.catalogText).versions[value.next], url(value.next));
        assert.ok(result.catalogText.startsWith(original.prefix));
        assert.ok(result.catalogText.endsWith(original.suffix));
        if (value.expected === value.current) assert.equal(result.wrapperText, original.wrapper);
    });
});

test('equal versions and repeated upgrades preserve every byte', () => {
    const original = fixture('7.0');
    const escaped = original.wrapper.replaceAll('/', '\\/');
    const equal = updateSchemaStore(original.catalog, escaped, [schema('7.0')]);
    assert.equal(equal.changed, false);
    assert.equal(equal.catalogText, original.catalog);
    assert.equal(equal.wrapperText, escaped);
    const prior = fixture();
    const first = updateSchemaStore(prior.catalog, prior.wrapper, [schema('7.0')]);
    const second = updateSchemaStore(first.catalogText, first.wrapperText, [schema('7.0')]);
    assert.equal(second.changed, false);
    assert.equal(second.catalogText, first.catalogText);
    assert.equal(second.wrapperText, first.wrapperText);
});

test('repairs an existing version entry without duplicates or lowering the default', () => {
    const original = fixture('7.0', {'6.8': 'https://wrong.example/schema.json', '7.0': url('7.0')});
    const result = updateSchemaStore(original.catalog, original.wrapper, [schema('6.8')]);
    assert.deepEqual(entry(result.catalogText).versions, {'6.8': url('6.8'), '7.0': url('7.0')});
    assert.equal(entry(result.catalogText).url, url('7.0'));
    assert.equal(result.wrapperText, original.wrapper);
});

test('repairs an equal-version wrapper without rewriting the catalog', () => {
    const original = fixture('7.0');
    const result = updateSchemaStore(original.catalog, fixture().wrapper, [schema('7.0')]);
    assert.equal(result.catalogText, original.catalog);
    assert.equal(JSON.parse(result.wrapperText).$ref, url('7.0'));
    assert.deepEqual(result.schemas, [schema('7.0')]);
});

test('handles an empty versions object and preserves compact unrelated arrays', () => {
    const original = fixture('6.8', {});
    original.catalog = original.catalog.replace('"fileMatch": [\n        "GitVersion.yml",\n        "GitVersion.yaml"\n      ]', '"fileMatch": ["GitVersion.yml", "GitVersion.yaml"]');
    assert.ok(original.catalog.includes('"fileMatch": ["GitVersion.yml", "GitVersion.yaml"]'));
    const result = updateSchemaStore(original.catalog, original.wrapper, [schema('7.0')]);
    assert.deepEqual(entry(result.catalogText).versions, {'7.0': url('7.0')});
    assert.ok(result.catalogText.includes('"fileMatch": ["GitVersion.yml", "GitVersion.yaml"]'));
    assert.ok(result.catalogText.startsWith(original.prefix));
    assert.ok(result.catalogText.endsWith(original.suffix));
});

test('multiple newly published versions promote only the latest', () => {
    const original = fixture('6.8');
    const result = updateSchemaStore(original.catalog, original.wrapper, [schema('7.0'), schema('6.10'), schema('6.9')]);
    assert.equal(entry(result.catalogText).url, url('7.0'));
    assert.equal(JSON.parse(result.wrapperText).$ref, url('7.0'));
    assert.deepEqual(Object.keys(entry(result.catalogText).versions), ['6.8', '6.9', '6.10', '7.0']);
});

test('rejects malformed versions and default URLs before producing edits', () => {
    for (const value of ['../7.0', '7.0.0', '7.0-beta', '07.0', '7.0\ncommand', '99999999999999999999.0']) {
        assert.throws(() => parseVersion(value), /Invalid schema version/);
    }
    assert.equal(schemaUrl('7.0'), 'https://gitversion.net/schemas/7.0/GitVersion.configuration.json');
    const original = fixture();
    assert.throws(() => updateSchemaStore(original.catalog.replace(url('6.8'), 'https://wrong.example'), original.wrapper, [schema('7.0')]), /Invalid default/);
    const duplicate = JSON.parse(original.catalog);
    duplicate.schemas.push({...duplicate.schemas[1]});
    assert.throws(() => updateSchemaStore(JSON.stringify(duplicate), original.wrapper, [schema('7.0')]), /Expected one GitVersion/);
});

test('discovers stable published editions while excluding development and prereleases', t => {
    const root = published(t, [
        {edition: {id: '7.0', status: 'release'}, release: '7.0.0'},
        {edition: {id: '6.8', status: 'release'}, release: '6.8.2'},
        {edition: {id: '8.0', status: 'release'}, release: '8.0.0-beta.1'},
        {edition: {id: '9.0', status: 'development'}, release: '9.0.0'}
    ]);
    assert.deepEqual(publishedSchemas(root), [schema('7.0'), schema('6.8')]);
    assert.deepEqual(publishedSchemas(root, '6.8'), [schema('6.8')]);
    assert.throws(() => publishedSchemas(root, '9.0'), /No stable published edition/);
});

test('fails when stable release metadata disagrees with its edition', t => {
    const root = published(t, [{edition: {id: '7.0', status: 'release'}, release: '6.8.2'}]);
    assert.throws(() => publishedSchemas(root), /Release does not match edition/);
});

test('tolerates an older missing schema but requires the newest stable schema', t => {
    const root = published(t, [
        {edition: {id: '7.0', status: 'release'}, release: '7.0.0'},
        {edition: {id: '6.8', status: 'release'}, release: '6.8.2'}
    ]);
    rmSync(join(root, 'schemas/6.8/GitVersion.configuration.json'));
    assert.deepEqual(publishedSchemas(root), [schema('7.0')]);
    assert.throws(() => publishedSchemas(root, '6.8'), /Missing schema for stable edition 6.8/);
    rmSync(join(root, 'schemas/7.0/GitVersion.configuration.json'));
    assert.throws(() => publishedSchemas(root), /Missing schema for stable edition 7.0/);
});

test('no stable schemas require no network calls or credentials', async t => {
    const root = published(t, [{edition: {id: '8.0', status: 'development'}, release: '8.0.0-beta.1'}]);
    const result = await publishSchemaStore(root, {run: () => assert.fail('No command expected'), fetchBytes: () => assert.fail('No download expected')});
    assert.deepEqual(result, {changed: false});
});

test('waits for matching public schema content and stops after a bounded failure', async () => {
    let calls = 0;
    const delays = [];
    await waitForSchemas([schema('7.0')], {fetchBytes: async () => ++calls === 1 ? Buffer.from('{}') : schemaBytes('7.0'), delay: async ms => delays.push(ms), attempts: 3});
    assert.equal(calls, 2);
    assert.deepEqual(delays, [10000]);
    calls = 0;
    await assert.rejects(waitForSchemas([schema('7.0')], {fetchBytes: async () => {calls++; throw new Error('HTTP 404');}, delay: async () => {}, attempts: 3}), /Public schema is not ready/);
    assert.equal(calls, 3);
});

test('finishes schema retries before checking the next schema', async () => {
    const events = [];
    let firstAttempt = true;
    await waitForSchemas([schema('6.8'), schema('7.0')], {
        fetchBytes: async value => {
            events.push(value);
            if (firstAttempt) {firstAttempt = false; throw new Error('HTTP 404');}
            return schemaBytes(value === url('6.8') ? '6.8' : '7.0');
        },
        delay: async ms => events.push(ms), attempts: 2
    });
    assert.deepEqual(events, [url('6.8'), 10000, url('6.8'), url('7.0')]);
});

function publisherFixture(t, options = {}) {
    const root = published(t);
    const baseline = fixture(options.current || '6.8', options.current === '8.0' ? {'8.0': url('8.0')} : undefined);
    const updated = updateSchemaStore(baseline.catalog, baseline.wrapper, [schema('7.0')]);
    const calls = [];
    let checkout;
    let forkCreated = !options.forkMissing;
    let forkReadAttempts = 0;
    const fetchBytes = async value => {
        calls.push({program: 'download', args: [value]});
        if (value.endsWith(catalogPath)) return Buffer.from(baseline.catalog);
        if (value.endsWith(wrapperPath)) return Buffer.from(baseline.wrapper);
        if (options.notReady) throw new Error('HTTP 404');
        assert.equal(value, url('7.0'));
        return schemaBytes('7.0');
    };
    const run = (program, args, settings = {}) => {
        calls.push({program, args, cwd: settings.cwd});
        if (program === 'gh') {
            if (args[0] === 'api') {
                const path = args[1];
                if (path === 'user') return JSON.stringify({login: options.login || 'gittools-bot'});
                if (path === 'repos/SchemaStore/schemastore') return JSON.stringify({default_branch: 'master'});
                if (path.includes('/git/ref/heads/')) return JSON.stringify({object: {sha: '1234567890abcdef'}});
                if (path === 'repos/gittools-bot/schemastore') {
                    if (options.forkForbidden) throw Object.assign(new Error('Forbidden'), {stderr: 'gh: Forbidden (HTTP 403)'});
                    if (!forkCreated) throw Object.assign(new Error('Not found'), {stderr: 'gh: Not Found (HTTP 404)'});
                    if (options.forkMissing && ++forkReadAttempts <= (options.forkPendingAttempts || 0)) {
                        throw Object.assign(new Error('Fork pending'), {stderr: 'gh: Not Found (HTTP 404)'});
                    }
                    if (options.forkReadFailure) throw Object.assign(new Error('Fork read failed'), {stderr: 'gh: Forbidden (HTTP 403)'});
                    return JSON.stringify({fork: true, parent: {full_name: options.wrongParent || 'SchemaStore/schemastore'}, default_branch: options.forkDefault || 'master'});
                }
                if (path.endsWith('/forks')) {forkCreated = true; return '{}';}
                if (path.includes('...gittools-bot:gitversion-schema-')) return JSON.stringify({files: [{filename: options.unrelated ? 'README.md' : catalogPath}]});
                if (path.includes('/compare/')) return JSON.stringify({ahead_by: options.uniqueFork ? 1 : 0});
            }
            if (args[0] === 'repo' && args[1] === 'clone') {
                checkout = args[3];
                files(checkout, options.cloneUpToDate ? fixture('7.0') : baseline);
                return '';
            }
            if (args[0] === 'pr' && args[1] === 'list') return JSON.stringify(options.existingPr ? [{number: 123, url: 'https://github.com/SchemaStore/schemastore/pull/123', state: options.closedPr ? 'CLOSED' : 'OPEN'}] : []);
            if (args[0] === 'pr' && args[1] === 'create') {
                assert.equal(args[args.indexOf('--head') + 1], 'gittools-bot:gitversion-schema-7.0');
                assert.match(readFileSync(args.at(-1), 'utf8'), /on behalf of Artur/);
                return 'https://github.com/SchemaStore/schemastore/pull/456';
            }
            if (args[0] === 'repo' && args[1] === 'sync' || args[0] === 'auth') return '';
        } else if (program === 'git') {
            if (args[0] === 'ls-remote') return options.existingBranch ? 'abcdef refs/heads/gitversion-schema-7.0' : '';
            if (args[0] === 'checkout' && args[1] === '--detach') {
                const branch = options.staleDefault ? fixture('7.0') : options.branchDiffers ? baseline : {catalog: updated.catalogText, wrapper: updated.wrapperText};
                files(checkout, branch);
            }
            if (args[0] === 'commit' && options.commitFails) throw new Error('Commit rejected');
            if (args[0] === 'push' && options.pushFails) throw new Error('Push rejected');
            if (args[0] === 'push') {
                assert.equal(entry(readFileSync(join(checkout, catalogPath), 'utf8')).versions['7.0'], url('7.0'));
                assert.ok(!args.some(arg => arg.startsWith('--force')));
            }
            return '';
        }
        throw new Error(`Unexpected command: ${program} ${args.join(' ')}`);
    };
    return {root, calls, dependencies: {run, fetchBytes, delay: async ms => calls.push({program: 'delay', args: [ms]})}, checkout: () => checkout};
}

test('creates a missing bot fork and opens one PR with a normal push', async t => {
    const h = publisherFixture(t, {forkMissing: true});
    const result = await publishSchemaStore(h.root, h.dependencies);
    assert.deepEqual(result, {changed: true, url: 'https://github.com/SchemaStore/schemastore/pull/456'});
    assert.equal(h.calls.filter(call => call.args.includes('--method')).length, 1);
    const ready = h.calls.findIndex(call => call.program === 'download' && call.args[0] === url('7.0'));
    const create = h.calls.findIndex(call => call.args.includes('--method'));
    assert.ok(ready >= 0 && ready < create);
    assert.equal(h.calls.filter(call => call.program === 'git' && call.args[0] === 'push').length, 1);
    assert.ok(!h.calls.some(call => call.args.some(arg => arg.startsWith('--force'))));
    assert.equal(existsSync(h.checkout()), false);
});

test('waits for a newly created fork before publishing', async t => {
    const h = publisherFixture(t, {forkMissing: true, forkPendingAttempts: 2});
    const result = await publishSchemaStore(h.root, h.dependencies);
    assert.equal(result.url, 'https://github.com/SchemaStore/schemastore/pull/456');
    assert.deepEqual(h.calls.filter(call => call.program === 'delay').map(call => call.args[0]), [1000, 1000]);
    assert.equal(h.calls.filter(call => call.args[1] === 'repos/gittools-bot/schemastore').length, 4);
});

test('stops fork readiness checks after five attempts without publishing', async t => {
    const h = publisherFixture(t, {forkMissing: true, forkPendingAttempts: 5});
    await assert.rejects(publishSchemaStore(h.root, h.dependencies), /Fork pending/);
    assert.equal(h.calls.filter(call => call.args[1] === 'repos/gittools-bot/schemastore').length, 6);
    assert.deepEqual(h.calls.filter(call => call.program === 'delay').map(call => call.args[0]), [1000, 1000, 1000, 1000]);
    assert.ok(!h.calls.some(call => call.program === 'git' && call.args[0] === 'push'));
});

test('does not retry a forbidden read after creating a fork', async t => {
    const h = publisherFixture(t, {forkMissing: true, forkReadFailure: true});
    await assert.rejects(publishSchemaStore(h.root, h.dependencies), /Fork read failed/);
    assert.ok(!h.calls.some(call => call.program === 'delay'));
    assert.ok(!h.calls.some(call => call.program === 'git' && call.args[0] === 'push'));
});

test('reuses an existing matching PR without committing or pushing', async t => {
    const h = publisherFixture(t, {existingBranch: true, existingPr: true});
    const result = await publishSchemaStore(h.root, h.dependencies);
    assert.deepEqual(result, {changed: false, url: 'https://github.com/SchemaStore/schemastore/pull/123'});
    assert.ok(!h.calls.some(call => call.program === 'git' && ['commit', 'push'].includes(call.args[0])));
    assert.ok(!h.calls.some(call => call.program === 'gh' && call.args[0] === 'pr' && call.args[1] === 'create'));
});

test('recovers a pushed matching branch when PR creation previously failed', async t => {
    const h = publisherFixture(t, {existingBranch: true});
    const result = await publishSchemaStore(h.root, h.dependencies);
    assert.equal(result.url, 'https://github.com/SchemaStore/schemastore/pull/456');
    assert.ok(!h.calls.some(call => call.program === 'git' && ['commit', 'push'].includes(call.args[0])));
});

test('preserves changed branches and closed PR decisions', async t => {
    for (const value of [{name: 'changed branch', branchDiffers: true}, {name: 'closed PR', existingPr: true, closedPr: true}, {name: 'unrelated branch files', unrelated: true}]) {
        await t.test(value.name, async child => {
            const h = publisherFixture(child, {existingBranch: true, ...value});
            await assert.rejects(publishSchemaStore(h.root, h.dependencies), /refusing to overwrite|maintainer attention|Unexpected changes/);
            assert.ok(!h.calls.some(call => call.program === 'git' && call.args[0] === 'push'));
        });
    }
});

test('does not reuse a stale PR that would lower a newer upstream default', async t => {
    const h = publisherFixture(t, {current: '8.0', existingBranch: true, existingPr: true, staleDefault: true});
    await assert.rejects(publishSchemaStore(h.root, h.dependencies), /refusing to overwrite/);
    assert.ok(!h.calls.some(call => call.program === 'git' && call.args[0] === 'push'));
});

test('refuses a wrong identity, an unrelated fork and unique fork commits', async t => {
    for (const value of [{name: 'personal identity', login: 'arturcic'}, {name: 'unrelated fork', wrongParent: 'other/repo'}, {name: 'unexpected fork default', forkDefault: 'other'}, {name: 'unique default commits', uniqueFork: true}]) {
        await t.test(value.name, async child => {
            const h = publisherFixture(child, value);
            await assert.rejects(publishSchemaStore(h.root, h.dependencies), /requires gittools-bot|Unexpected fork|unique commits/);
            assert.ok(!h.calls.some(call => call.program === 'git' && call.args[0] === 'push'));
        });
    }
});

test('an already registered stable schema needs no bot credentials or publication', async t => {
    const h = publisherFixture(t, {current: '7.0'});
    const result = await publishSchemaStore(h.root, h.dependencies);
    assert.deepEqual(result, {changed: false});
    assert.ok(!h.calls.some(call => call.args[0] === 'user' || call.args[1] === 'user' || call.program === 'git'));
    assert.ok(!h.calls.some(call => call.args.includes('--method')));
});

test('public schema failure cannot create a fork or push a branch', async t => {
    const h = publisherFixture(t, {notReady: true, forkMissing: true});
    await assert.rejects(publishSchemaStore(h.root, h.dependencies), /Public schema is not ready/);
    assert.ok(!h.calls.some(call => call.args.includes('--method') || call.program === 'git'));
});

test('commit and push failures propagate without falsely reporting no changes', async t => {
    for (const value of [{name: 'commit rejected', commitFails: true, message: /Commit rejected/}, {name: 'push rejected', pushFails: true, message: /Push rejected/}]) {
        await t.test(value.name, async child => {
            const h = publisherFixture(child, value);
            await assert.rejects(publishSchemaStore(h.root, h.dependencies), value.message);
            assert.ok(!h.calls.some(call => call.program === 'gh' && call.args[0] === 'pr' && call.args[1] === 'create'));
            assert.equal(existsSync(h.checkout()), false);
        });
    }
});


test('a forbidden fork read cannot be treated as a missing fork', async t => {
    const h = publisherFixture(t, {forkForbidden: true});
    await assert.rejects(publishSchemaStore(h.root, h.dependencies), /Forbidden/);
    assert.ok(!h.calls.some(call => call.args.includes('--method') || call.program === 'git'));
});

test('an upstream update arriving before clone avoids duplicate publication', async t => {
    const h = publisherFixture(t, {cloneUpToDate: true});
    const result = await publishSchemaStore(h.root, h.dependencies);
    assert.deepEqual(result, {changed: false});
    assert.ok(!h.calls.some(call => call.program === 'git' && ['commit', 'push'].includes(call.args[0])));
    assert.ok(!h.calls.some(call => call.program === 'gh' && call.args[0] === 'pr' && call.args[1] === 'create'));
});

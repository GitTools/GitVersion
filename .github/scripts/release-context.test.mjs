import assert from 'node:assert/strict';
import { test } from 'node:test';
import { resolveReleaseContext } from './release-context.mjs';

const sha = 'a'.repeat(40);
const env = { GITHUB_REPOSITORY: 'GitTools/GitVersion', GITHUB_REF_NAME: 'main', GITHUB_EVENT_NAME: 'repository_dispatch' };
function fixture(tag = '7.0.0-beta.1') {
  return {
    event: { action: 'ci-release', client_payload: { tag, sha, ref: `refs/tags/${tag}` } },
    responses: [
      { tag_name: tag, draft: false, prerelease: tag.includes('-'), body: 'Reviewed beta notes' },
      { sha },
      { merge_base_commit: { sha } },
    ],
  };
}
function resolve(f, environment = env) {
  return resolveReleaseContext(environment, f.event, async () => f.responses.shift());
}

test('beta dispatch validates release, exact tag SHA and ancestry', async () => {
  const f = fixture();
  const paths = [];
  const result = await resolveReleaseContext(env, f.event, async path => { paths.push(path); return f.responses.shift(); });
  assert.deepEqual(result, { can_publish: true, run_full_validation: true, prerelease: true });
  assert.deepEqual(paths, [
    '/repos/GitTools/GitVersion/releases/tags/7.0.0-beta.1',
    '/repos/GitTools/GitVersion/commits/refs%2Ftags%2F7.0.0-beta.1',
    `/repos/GitTools/GitVersion/compare/${sha}...main`,
  ]);
});
test('stable dispatch preserves stable channel selection', async () => {
  assert.deepEqual(await resolve(fixture('7.0.0')), { can_publish: true, run_full_validation: true, prerelease: false });
});
test('main push keeps internal publishing without release API calls', async () => {
  assert.deepEqual(await resolveReleaseContext({ ...env, GITHUB_EVENT_NAME: 'push' }, {}, () => assert.fail('Unexpected API call')),
    { can_publish: true, run_full_validation: true, prerelease: false });
});
test('pull requests and forks cannot publish', async () => {
  for (const e of [
    { ...env, GITHUB_EVENT_NAME: 'pull_request', GITHUB_REF_NAME: '123/merge' },
    { ...env, GITHUB_EVENT_NAME: 'push', GITHUB_REPOSITORY: 'someone/GitVersion' },
  ]) assert.equal((await resolveReleaseContext(e, {}, () => assert.fail('Unexpected API call'))).can_publish, false);
});
test('invalid dispatch metadata is rejected before API calls', async () => {
  for (const patch of [{ tag: '' }, { tag: '7.0.0-beta.1\nINJECTED=true' }, { tag: '7.0.0\n' }, { sha: 'abc' }, { ref: 'refs/heads/main' }]) {
    const f = fixture();
    Object.assign(f.event.client_payload, patch);
    await assert.rejects(resolveReleaseContext(env, f.event, () => assert.fail('Unexpected API call')), /requires a version tag/);
  }
});
test('foreign repository and unexpected dispatch action are rejected', async () => {
  await assert.rejects(resolve(fixture(), { ...env, GITHUB_REPOSITORY: 'someone/GitVersion' }), /Unexpected release dispatch/);
  const f = fixture();
  f.event.action = 'other';
  await assert.rejects(resolve(f), /Unexpected release dispatch/);
});
for (const [name, change] of [
  ['draft release', f => { f.responses[0].draft = true; }],
  ['wrong release tag', f => { f.responses[0].tag_name = '7.0.0'; }],
  ['wrong prerelease flag', f => { f.responses[0].prerelease = false; }],
  ['wrong tag SHA', f => { f.responses[1].sha = 'b'.repeat(40); }],
  ['commit outside main history', f => { f.responses[2].merge_base_commit.sha = 'b'.repeat(40); }],
]) test(`rejects ${name}`, async () => {
  const f = fixture();
  change(f);
  await assert.rejects(resolve(f), /does not match/);
});
test('beta requires reviewed notes', async () => {
  const f = fixture();
  f.responses[0].body = '  ';
  await assert.rejects(resolve(f), /reviewed release notes/);
});
test('API failure prevents publishing', async () => {
  await assert.rejects(resolveReleaseContext(env, fixture().event, async () => { throw new Error('HTTP 403'); }), /HTTP 403/);
});

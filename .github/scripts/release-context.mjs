import { appendFileSync, readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export async function resolveReleaseContext(env, event, request) {
  const original = env.GITHUB_REPOSITORY === 'GitTools/GitVersion';
  const dispatched = env.GITHUB_EVENT_NAME === 'repository_dispatch';
  const result = {
    can_publish: original && env.GITHUB_REF_NAME === 'main',
    run_full_validation: original && (env.GITHUB_REF_NAME === 'main' || env.GITHUB_EVENT_NAME === 'merge_group' || dispatched),
    prerelease: false,
  };
  if (!dispatched) return result;
  if (!original || event.action !== 'ci-release') throw new Error('Unexpected release dispatch');

  const { tag, sha, ref } = event.client_payload ?? {};
  if (typeof tag !== 'string' || tag !== tag.trim() || !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(tag) ||
      typeof sha !== 'string' || !/^[0-9a-f]{40}$/.test(sha) || ref !== `refs/tags/${tag}`) {
    throw new Error('Release dispatch requires a version tag, its ref, and a full commit SHA');
  }
  const base = `/repos/${env.GITHUB_REPOSITORY}`;
  const release = await request(`${base}/releases/tags/${encodeURIComponent(tag)}`);
  const commit = await request(`${base}/commits/${encodeURIComponent(ref)}`);
  const comparison = await request(`${base}/compare/${sha}...main`);
  const prerelease = tag.includes('-');
  if (release.draft || release.tag_name !== tag || release.prerelease !== prerelease || commit.sha !== sha ||
      comparison.merge_base_commit?.sha !== sha) {
    throw new Error('Release metadata, tag SHA, or main ancestry does not match the dispatch');
  }
  if (prerelease && !release.body?.trim()) throw new Error('Beta releases require reviewed release notes before publishing');
  return { ...result, can_publish: true, prerelease };
}

async function main() {
  const env = process.env;
  const event = JSON.parse(readFileSync(env.GITHUB_EVENT_PATH, 'utf8'));
  const request = async path => {
    const response = await fetch(`${env.GITHUB_API_URL ?? 'https://api.github.com'}${path}`, {
      headers: { Authorization: `Bearer ${env.GITHUB_TOKEN}`, Accept: 'application/vnd.github+json' },
    });
    if (!response.ok) throw new Error(`GitHub release validation failed: HTTP ${response.status}`);
    return response.json();
  };
  const result = await resolveReleaseContext(env, event, request);
  appendFileSync(env.GITHUB_OUTPUT, Object.entries(result).map(([key, value]) => `${key}=${value}\n`).join(''));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
}

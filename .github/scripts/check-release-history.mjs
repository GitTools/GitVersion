import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { copyFileSync, mkdirSync, mkdtempSync, rmSync, symlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const checker = resolve(process.argv[2]);
const cli = resolve(process.argv[3]);
const config = resolve('.gitversion.yml');
const tag = process.argv[4] ?? '7.0.0-beta.1';
const testName = tag.includes('-') ? 'CakeLifetimePinsBetaVersionWhenMainAdvances' : 'CakeLifetimePinsStableVersionWhenMainAdvances';
const root = mkdtempSync(join(tmpdir(), 'gitversion-beta-history-'));
const repo = join(root, 'repo');
const origin = join(root, 'origin.git');
mkdirSync(repo);
const git = (...args) => execFileSync('git', ['-C', repo, ...args], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
try {
  git('init', '-b', 'main');
  git('config', 'user.name', 'Release validation');
  git('config', 'user.email', 'release-validation@example.invalid');
  copyFileSync(config, join(repo, '.gitversion.yml'));
  git('add', '.gitversion.yml');
  git('commit', '-m', 'Beta commit');
  const sha = git('rev-parse', 'HEAD');
  git('tag', tag);
  git('commit', '--allow-empty', '-m', 'Later main commit');
  const main = git('rev-parse', 'HEAD');
  git('clone', '--bare', repo, origin);
  git('remote', 'add', 'origin', origin);
  git('update-ref', 'refs/remotes/origin/main', main);
  git('checkout', '--detach', sha);
  mkdirSync(join(repo, 'tools'));
  symlinkSync(cli, join(repo, 'tools', 'gitversion'), process.platform === 'win32' ? 'junction' : 'dir');
  const output = execFileSync(process.env.DOTNET_HOST_PATH ?? 'dotnet', [checker, '--target=CheckReleaseIdentity', '--verbosity=quiet'], {
    cwd: repo,
    encoding: 'utf8',
    env: {
      ...process.env,
      GITHUB_ACTIONS: 'true', GITHUB_REPOSITORY: 'GitTools/GitVersion',
      GITHUB_EVENT_NAME: 'repository_dispatch', GITHUB_HEAD_REF: '', GITHUB_BASE_REF: '',
      GITHUB_REF: 'refs/heads/main', GITHUB_REF_TYPE: 'branch', GITHUB_SHA: main,
      GITVERSION_RELEASE_TAG: tag, GITVERSION_TELEMETRY_OPTOUT: '1',
      GITHUB_TOKEN: '', GITHUB_ENV: join(root, 'github-env'), GITHUB_OUTPUT: join(root, 'github-output'),
    },
  });
  assert.ok(output.includes(`PASS ${testName}`), 'Release lifetime check did not complete');
  assert.equal(git('rev-parse', 'HEAD'), sha, 'Version calculation must not move HEAD to the later main tip');
  assert.equal(git('rev-parse', 'refs/remotes/origin/main'), main, 'Remote main must remain unchanged');
  console.log(`PASS ${testName}`);
} catch (error) {
  console.error(String(error.stderr ?? error.message).slice(-2500));
  process.exitCode = 1;
} finally {
  rmSync(root, { recursive: true, force: true });
}

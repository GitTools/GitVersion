import {execFileSync} from 'node:child_process';
import {readFile} from 'node:fs/promises';
import {join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {remark} from 'remark';
import remarkFrontmatter from 'remark-frontmatter';
import remarkReferenceLinks from './remark-reference-links.mjs';

const root = fileURLToPath(new URL('../../', import.meta.url));
// Check docs and the two root documents included in this style migration.
const files = [...new Set(execFileSync('git', [
    'ls-files', '-z', '--cached', '--others', '--exclude-standard'
], {cwd: root, encoding: 'utf8'}).split('\0'))]
    .filter(path => /\.(md|markdown|mdown|mkdn|mkd|mdwn)$/i.test(path))
    .filter(path => path.startsWith('docs/') || ['CONTRIBUTING.md', 'BREAKING_CHANGES.md'].includes(path));
const processor = remark().use(remarkFrontmatter).use(remarkReferenceLinks);
let failures = 0;
for (const path of files) {
    const value = await readFile(join(root, path), 'utf8');
    const file = await processor.process({path, value});
    for (const message of file.messages) {
        console.error(`${path}:${message.line}:${message.column}: ${message.reason}`);
        failures++;
    }
}
console.log(`Checked reference links in ${files.length} Markdown files; ${failures} violations.`);
process.exitCode = failures ? 1 : 0;

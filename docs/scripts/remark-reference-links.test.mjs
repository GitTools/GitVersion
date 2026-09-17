import assert from 'node:assert/strict';
import {test} from 'node:test';
import {remark} from 'remark';
import remarkFrontmatter from 'remark-frontmatter';
import remarkReferenceLinks from './remark-reference-links.mjs';

const processor = remark().use(remarkFrontmatter).use(remarkReferenceLinks);

test('rejects inline links and images, including linked badges and nested destinations', async () => {
    const file = await processor.process('[**Guide**](docs/guide.md "Guide")\n\n[![Build](badge.svg)](https://example.com/build)\n\n[API](https://example.com/api(foo))\n');
    assert.equal(file.messages.length, 4);
    assert.ok(file.messages.every(message => message.ruleId === 'reference-links'));
    assert.deepEqual(file.messages.map(message => message.line), [1, 3, 3, 5]);
});

test('accepts references, autolinks, HTML, code examples, and front matter', async () => {
    const file = await processor.process('---\nTitle: "[example](url)"\n---\n\n[Guide][guide], [guide][], [guide], ![Build][badge]\n\n<https://example.com>\n\n<a href="page.html">HTML</a>\n\n`[example](url)`\n\n```md\n[example](url)\n```\n\n[guide]: docs/guide.md "Guide"\n\n[badge]: badge.svg\n');
    assert.equal(file.messages.length, 0);
});

# Maintaining the version-calculation page

The [algorithm page][algorithm] and its flowcharts explain the current implementation.
Changes to mapped calculation code require an explicit review of affected sections.

## Review a calculation change

1. Run `npm run check:algorithm-docs`. It identifies sections whose source
   fingerprints differ from their last review.
2. Inspect the source and test pointers in `docs/version-calculation-map.json`.
   Update the affected explanation and diagrams when behavior changes.
   For a refactor, explain why the existing description remains accurate.
3. Record the reviewed sections and a concrete reason:

   ```bash
   node docs/scripts/check-version-calculation.mjs --review overview,selection --reason "Reviewed the tie-breaking refactor; version and counting-source rules are unchanged."
   ```

4. Review the manifest diff with the prose, diagrams, and regression evidence.
   Run `npm run test:algorithm-docs` and `npm run check:algorithm-docs`.

The record confirms a human review of the current mapped source contents.
It does not prove the explanation is correct. Do not refresh fingerprints
without reviewing the affected rules.

## Maintain the map

Each of the seven sections names its page heading, authored diagram where
applicable, implementation inputs, and executable evidence. Shared inputs
include configuration, tag/history services, semantic-version comparisons,
Git abstractions, and both backends. Changes there conservatively require
review of every section.

The overview watches the whole calculation directory, including new source
files. Directory inputs include C# sources recursively and exclude build
output. Deletions and renames change fingerprints; missing explicit files
fail validation. Line endings are normalized so Windows checkouts agree.

When moving a section, diagram, source, or test, update its mapping too.
When adding algorithm dependencies outside the mapped directories, extend
the inputs. CI cannot infer a new dependency that has not been mapped.

## Executable examples

The page reuses existing test-generated trunk-based histories for tags and
commit-message increments. The docs build runs their assertions, compares
the generated Mermaid sources, and validates syntax. Use the existing
[example contribution workflow][examples] when extending those histories.

Authored decision flowcharts still require review: test-generated histories
show representative outcomes, rather than every history/configuration combination.

[algorithm]: input/docs/learn/version-calculation.md

[examples]: input/docs/learn/branching-strategies/contribute-examples.md


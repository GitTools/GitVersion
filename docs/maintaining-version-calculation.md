# Maintaining the version-calculation page

The [algorithm page][algorithm] and its flowcharts explain the current implementation.
When calculation behavior changes, review the affected explanation and diagrams.

## Review a calculation change

1. Inspect the source and test pointers in `docs/version-calculation-map.json`.
   Update the affected explanation and diagrams when behavior changes.
2. Update the map when its headings, paths, or source coverage change.
3. Run `npm run test:algorithm-docs` and `npm run check:algorithm-docs`.
   The checker validates mappings, headings, diagram includes, and calculation-source
   coverage. It does not compare source contents or record documentation reviews.

## Maintain the map

Each of the seven sections names its page heading, authored diagram where
applicable, implementation inputs, and executable evidence. Shared inputs
include configuration, tag/history services, semantic-version comparisons,
Git abstractions, and both backends. Source content edits do not require map updates.

The overview maps the whole calculation directory, including new source
files. Directory inputs include C# sources recursively and exclude build
output. New files in mapped directories are covered automatically. Missing explicit
files, empty section source mappings, and unmapped calculation files fail validation.

When moving a section, diagram, source, or test, update its mapping too.
When adding algorithm dependencies outside the mapped directories, extend
the inputs. CI cannot infer a new dependency that has not been mapped.

## Executable examples

The page uses `DocumentationSamplesForVersionCalculation` for competing
candidates, inherited increments, equal-version selection, separate sources,
and deployment modes. It also reuses trunk-based histories for tags and
commit-message increments. The docs build runs these assertions, compares
the generated Mermaid sources, and validates syntax. Use the existing
[example contribution workflow][examples] when extending those histories.

Authored decision flowcharts still require review: test-generated histories
show representative outcomes, rather than every history/configuration combination.

[algorithm]: input/docs/learn/version-calculation.md

[examples]: input/docs/learn/branching-strategies/contribute-examples.md

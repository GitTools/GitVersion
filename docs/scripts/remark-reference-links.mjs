// Keep documentation links and images readable without changing autolinks or code.
export default function remarkReferenceLinks() {
    return (tree, file) => {
        function visit(node) {
            if (node.type === 'link' || node.type === 'image') {
                const start = node.position.start.offset;
                const source = String(file).slice(start, node.position.end.offset);
                if (source.startsWith('[') || source.startsWith('![')) {
                    file.message(
                        'Use a reference-style link or image with a definition at the end of the document.',
                        node,
                        'remark-lint:reference-links'
                    );
                }
            }
            for (const child of node.children ?? []) visit(child);
        }
        visit(tree);
    };
}

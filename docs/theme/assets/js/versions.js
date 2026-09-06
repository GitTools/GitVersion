(() => {
    const selector = document.getElementById('docs-version');
    if (!selector) return;
    fetch('/versions.json').then(response => {
        if (!response.ok) throw new Error('Version manifest unavailable');
        return response.json();
    }).then(versions => {
        const current = versions.find(v => v.basePath && (location.pathname === v.basePath || location.pathname.startsWith(v.basePath + '/')))
            || versions.find(v => !v.basePath);
        selector.replaceChildren(...versions.map(v => {
            const option = new Option(v.label, v.id, false, v.id === current.id);
            return option;
        }));
        const normalize = path => path.replace(/\/index\.html$/, '/').replace(/\.html$/, '').replace(/\/$/, '') || '/';
        selector.addEventListener('change', () => {
            const target = versions.find(v => v.id === selector.value);
            const route = normalize(location.pathname.slice(current.basePath.length));
            const exists = target.routes.some(p => normalize(p) === route);
            const fallback = route.startsWith('/api') ? '/api' : '/docs';
            location.assign(target.basePath + (exists ? route : fallback) + (exists ? location.hash : '?version-fallback=1'));
        });
        if (new URLSearchParams(location.search).has('version-fallback')) {
            const message = document.createElement('div');
            message.className = 'alert alert-info';
            message.setAttribute('role', 'status');
            message.textContent = 'That page is unavailable in this version. Choose a page from the navigation.';
            (document.querySelector('.content') || document.querySelector('main') || document.body).prepend(message);
        }
    }).catch(() => { selector.disabled = true; });
})();

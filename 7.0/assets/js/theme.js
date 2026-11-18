(() => {
    const key = 'gitversion-theme';
    const system = window.matchMedia('(prefers-color-scheme: dark)');
    let preference;
    try { preference = localStorage.getItem(key); } catch { /* Storage may be unavailable. */ }
    if (preference !== 'light' && preference !== 'dark') preference = null;

    const apply = theme => {
        document.documentElement.dataset.theme = theme;
        document.querySelectorAll('[data-theme-toggle]').forEach(button => {
            button.setAttribute('aria-checked', String(theme === 'dark'));
            button.setAttribute('title', theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme');
        });
    };
    const current = () => preference || (system.matches ? 'dark' : 'light');
    apply(current());

    document.addEventListener('DOMContentLoaded', () => {
        apply(current());
        document.querySelectorAll('[data-theme-toggle]').forEach(button => {
            button.addEventListener('click', () => {
                preference = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
                try { localStorage.setItem(key, preference); } catch { /* Keep the choice for this page. */ }
                apply(preference);
            });
        });
    });
    system.addEventListener('change', () => { if (!preference) apply(current()); });
    window.addEventListener('storage', event => {
        if (event.key !== key && event.key !== null) return;
        preference = event.newValue === 'light' || event.newValue === 'dark' ? event.newValue : null;
        apply(current());
    });
})();

(() => {
    const content = document.querySelector('section.content');
    const prompt = content?.querySelector('pre code.language-text');
    if (!prompt) return;

    const guideUrl = new URL(window.location.href);
    guideUrl.search = '';
    guideUrl.hash = '';
    const markdownUrl = new URL('./ai-agent-onboarding.md', guideUrl);
    const task = `${prompt.textContent.trim()}\n\nGuide: ${guideUrl.href}\nMarkdown: ${markdownUrl.href}`;
    const toolbar = document.createElement('div');
    toolbar.className = 'gv-agent-tools';
    toolbar.setAttribute('role', 'group');
    toolbar.setAttribute('aria-label', 'Use this guide with an AI agent');

    const status = document.createElement('p');
    status.className = 'gv-agent-status';
    status.setAttribute('role', 'status');
    status.setAttribute('aria-live', 'polite');
    const fallback = document.createElement('textarea');
    fallback.className = 'gv-agent-copy-fallback';
    fallback.setAttribute('aria-label', 'Text to copy manually');
    fallback.readOnly = true;
    fallback.hidden = true;

    async function copy(text, description) {
        fallback.hidden = true;
        try {
            await navigator.clipboard.writeText(text);
            status.textContent = `${description} copied. Paste it into your agent session.`;
        } catch {
            fallback.value = text;
            fallback.hidden = false;
            fallback.focus();
            fallback.select();
            status.textContent = `Clipboard unavailable. ${description} is selected below; copy it with your keyboard.`;
        }
    }

    function button(label, action) {
        const element = document.createElement('button');
        element.type = 'button';
        element.textContent = label;
        element.addEventListener('click', action);
        return element;
    }

    toolbar.appendChild(button('Copy setup prompt', () => copy(task, 'Setup prompt')));
    const agents = document.createElement('details');
    agents.className = 'gv-agent-options';
    const summary = document.createElement('summary');
    summary.textContent = 'Use with an agent';
    agents.appendChild(summary);
    const options = document.createElement('div');
    options.className = 'gv-agent-options-list';
    const instructions = {
        'Codex': 'Work in the Codex session attached to my repository. Read applicable AGENTS.md instructions before starting.',
        'Claude Code': 'Work in my repository in Claude Code. Read applicable CLAUDE.md and repository instructions before starting.',
        'GitHub Copilot': 'Work in my repository workspace using GitHub Copilot agent mode. Read applicable repository instructions before starting.',
        'Antigravity': 'Work in my repository in Antigravity. Read applicable workspace and directory rules before starting.'
    };
    Object.entries(instructions).forEach(([agent, instruction]) => {
        options.appendChild(button(`Copy for ${agent}`, () => {
            agents.open = false;
            summary.focus();
            copy(`${instruction}\n\n${task}`, `${agent} prompt`);
        }));
    });
    agents.appendChild(options);
    agents.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            agents.open = false;
            summary.focus();
        }
    });
    document.addEventListener('click', event => {
        if (!agents.contains(event.target)) agents.open = false;
    });
    toolbar.appendChild(agents);
    const markdown = document.createElement('a');
    markdown.href = markdownUrl.href;
    markdown.textContent = 'View Markdown';
    toolbar.appendChild(markdown);
    toolbar.appendChild(button('Copy Markdown', async () => {
        status.textContent = 'Loading Markdown…';
        try {
            const response = await fetch(markdownUrl);
            if (!response.ok) throw new Error('Markdown unavailable');
            await copy(await response.text(), 'Markdown guide');
        } catch {
            status.textContent = 'Could not load Markdown. Use View Markdown to open the guide directly.';
        }
    }));
    content.prepend(toolbar);
    toolbar.after(status, fallback);
})();

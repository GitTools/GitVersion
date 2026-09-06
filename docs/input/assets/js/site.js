(() => {
    const api = document.querySelector('.gv-api');
    const toc = api?.querySelector('.gv-api-toc');
    if (toc) {
        const headings = [...api.querySelectorAll('.content h1[id], .content h2[id]')];
        if (headings.length >= 3) {
            headings.forEach(heading => {
                const link = document.createElement('a');
                link.href = '#' + encodeURIComponent(heading.id);
                link.textContent = heading.textContent;
                toc.querySelector('nav').appendChild(link);
            });
            toc.hidden = false;
            api.classList.add('gv-api-with-toc');
        }
    }
    document.querySelectorAll('[data-copy]').forEach(button => {
        button.addEventListener('click', async () => {
            const command = document.getElementById(button.dataset.copy);
            const status = document.getElementById('copy-status');
            if (!command) return;
            try {
                await navigator.clipboard.writeText(command.textContent.trim());
                button.innerHTML = '<i class="fa fa-check" aria-hidden="true"></i>';
                button.setAttribute('aria-label', 'Installation command copied');
                if (status) status.textContent = 'Installation command copied.';
            } catch {
                const selection = window.getSelection();
                if (!selection) {
                    if (status) status.textContent = 'Copy unavailable. Select the command and copy it manually.';
                    return;
                }
                const range = document.createRange();
                range.selectNodeContents(command);
                selection.removeAllRanges();
                selection.addRange(range);
                if (status) status.textContent = 'Copy unavailable. The command is selected; copy it with your keyboard.';
            }
        });
    });
})();

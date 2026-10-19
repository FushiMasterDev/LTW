(() => {
    'use strict';
    addEventListener('pagehide', () => {
        const input = document.querySelector('#temporary-password');
        if (input) { input.value = ''; input.removeAttribute('value'); }
    });
    document.querySelector('[data-copy-employee-password]')?.addEventListener('click', async event => {
        const button = event.currentTarget;
        try {
            await navigator.clipboard.writeText(document.querySelector('#temporary-password').value);
            button.textContent = button.dataset.copied;
            document.querySelector('[data-copy-status]').textContent = button.dataset.copied;
        } catch (_) { document.querySelector('[data-copy-status]').textContent = button.dataset.copyFailed; }
    });
})();

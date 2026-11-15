(() => {
    const root = document.documentElement;
    const themeSelect = document.querySelector('[data-theme-select]');
    const variantSelect = document.querySelector('[data-variant-select]');
    const metaTheme = document.querySelector('meta[name="theme-color"]');
    const settingsMenu = document.querySelector('.settings-menu');

    function apply(theme, variant) {
        const safeTheme = theme || 'dark';
        const safeVariant = variant || 'arcade';
        root.dataset.theme = safeTheme;
        root.dataset.variant = safeVariant;
        localStorage.setItem('fgr-theme', safeTheme);
        localStorage.setItem('fgr-variant', safeVariant);
        metaTheme?.setAttribute('content', safeTheme === 'light' ? '#f6f1e7' : '#0b0e14');

        if (themeSelect) themeSelect.value = safeTheme;
        if (variantSelect) variantSelect.value = safeVariant;
    }

    const currentTheme = localStorage.getItem('fgr-theme') || root.dataset.theme || 'dark';
    const currentVariant = localStorage.getItem('fgr-variant') || root.dataset.variant || 'arcade';
    apply(currentTheme, currentVariant);

    themeSelect?.addEventListener('change', () => apply(themeSelect.value, root.dataset.variant || 'arcade'));
    variantSelect?.addEventListener('change', () => apply(root.dataset.theme || 'dark', variantSelect.value));

    document.addEventListener('click', event => {
        if (settingsMenu && !settingsMenu.contains(event.target)) {
            settingsMenu.removeAttribute('open');
        }
    });

    document.querySelectorAll('[data-dismiss-alert]').forEach(button => {
        button.addEventListener('click', () => button.closest('.app-alert')?.remove());
    });

    document.querySelectorAll('[data-open-dialog]').forEach(button => {
        button.addEventListener('click', () => {
            const dialog = document.getElementById(button.dataset.openDialog || '');
            if (dialog instanceof HTMLDialogElement) {
                dialog.showModal();
            }
        });
    });

    document.querySelectorAll('[data-close-dialog]').forEach(button => {
        button.addEventListener('click', () => button.closest('dialog')?.close());
    });

    document.querySelectorAll('[data-scan-form]').forEach(form => {
        form.addEventListener('submit', () => {
            form.classList.add('is-working');
            form.querySelectorAll('button').forEach(button => button.setAttribute('disabled', 'disabled'));
        });
    });
})();

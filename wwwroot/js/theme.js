(() => {
    const key = 'aquaprior-theme';
    const system = window.matchMedia('(prefers-color-scheme: dark)');
    let preference = null;
    try {
        const saved = localStorage.getItem(key);
        if (saved === 'dark' || saved === 'light') preference = saved;
    } catch { /* Private browsers may block storage; switching still works. */ }

    function apply(theme) {
        document.documentElement.dataset.theme = theme;
        const dark = theme === 'dark';
        const button = document.getElementById('theme-toggle');
        if (button) {
            button.hidden = false;
            button.setAttribute('aria-pressed', String(dark));
            button.setAttribute('aria-label', dark ? 'Activar modo día' : 'Activar modo noche');
            button.querySelector('[data-theme-label]').textContent = dark ? 'Modo día' : 'Modo noche';
            button.querySelector('[data-theme-icon]').textContent = dark ? '☀' : '☾';
        }
        const meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.content = dark ? '#10272e' : '#103c46';
    }
    const current = () => preference || (system.matches ? 'dark' : 'light');
    apply(current());
    document.addEventListener('DOMContentLoaded', () => {
        apply(current());
        document.getElementById('theme-toggle')?.addEventListener('click', () => {
            preference = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
            apply(preference);
            try { localStorage.setItem(key, preference); } catch { /* Keep session preference. */ }
        });
    });
    system.addEventListener('change', () => { if (!preference) apply(current()); });
})();

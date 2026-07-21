// Runs in <head> before first paint so the chosen theme never flashes.
// Dark is the default; an explicit OS light preference is honored as a hint.
// Blazor enhanced navigation re-merges server markup and strips data-theme
// from <html>, so site.js calls __applyTheme again after every enhancedload.
(function () {
    function currentTheme() {
        var stored = null;
        try { stored = localStorage.getItem('theme'); } catch (e) { /* storage blocked */ }
        return stored ||
            (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    }

    window.__applyTheme = function () {
        document.documentElement.dataset.theme = currentTheme();
    };

    window.__toggleTheme = function () {
        var next = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
        document.documentElement.dataset.theme = next;
        try { localStorage.setItem('theme', next); } catch (e) { /* storage blocked */ }
    };

    window.__applyTheme();
})();

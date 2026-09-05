// Runs in <head> before first paint so the chosen theme never flashes.
// Dark is the default; an explicit OS light preference is honored as a hint.
// Blazor enhanced navigation re-merges server markup and strips data-theme
// from <html>, so site.js calls __applyTheme again after every enhancedload.
(function () {
    function currentTheme() {
        var stored = null;
        try { stored = localStorage.getItem('theme'); } catch (e) { /* storage blocked */ }
        if (stored !== 'light' && stored !== 'dark') {
            stored = null; // ignore corrupted/unknown values
        }
        return stored ||
            (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    }

    // Under the BJJ landing flavor (data-flavor="bjj" on <html>, rendered by
    // App.razor) the theme toggle's tooltip names the theme a click switches
    // to in gi terms. Only the pointer tooltip changes: the aria-label stays
    // the functional one. Under any other flavor the server-rendered title
    // is left alone.
    function syncToggleTitle() {
        if (document.documentElement.dataset.flavor !== 'bjj') {
            return;
        }
        var toggle = document.querySelector('.theme-toggle');
        if (!toggle) {
            return; // the head-time call runs before the body is parsed
        }
        toggle.title = document.documentElement.dataset.theme === 'light'
            ? 'Switch to the black gi (dark theme)'
            : 'Switch to the white gi (light theme)';
    }

    window.__applyTheme = function () {
        document.documentElement.dataset.theme = currentTheme();
        syncToggleTitle(); // a no-op in the head; effective after every enhancedload
    };

    window.__toggleTheme = function () {
        var next = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
        document.documentElement.dataset.theme = next;
        try { localStorage.setItem('theme', next); } catch (e) { /* storage blocked */ }
        syncToggleTitle(); // the tooltip names the theme the next click switches to
    };

    window.__applyTheme();
    // The call above runs before the toggle exists; set the tooltip once the
    // document is parsed.
    document.addEventListener('DOMContentLoaded', syncToggleTitle);
})();

// Runs in <head> before first paint so the chosen theme never flashes.
// Dark is the default; an explicit OS light preference is honored as a hint.
(function () {
    var stored = null;
    try { stored = localStorage.getItem('theme'); } catch (e) { /* storage blocked */ }
    var theme = stored ||
        (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    document.documentElement.dataset.theme = theme;

    window.__toggleTheme = function () {
        var next = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
        document.documentElement.dataset.theme = next;
        try { localStorage.setItem('theme', next); } catch (e) { /* storage blocked */ }
    };
})();

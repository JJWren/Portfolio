// After Blazor enhanced navigations swap the DOM: restore the theme attribute
// (the merged server markup doesn't carry data-theme) and re-run highlighting.
(function () {
    // Server renders UTC; <time data-local="date|datetime"> elements are
    // rewritten to the visitor's local timezone. A MutationObserver catches
    // nodes re-rendered later by interactive components (comments, admin).
    function localizeTimes() {
        var times = document.querySelectorAll('time[data-local]:not([data-localized])');
        for (var i = 0; i < times.length; i++) {
            var el = times[i];
            var date = new Date(el.getAttribute('datetime'));
            if (isNaN(date)) {
                continue;
            }
            el.textContent = el.dataset.local === 'date'
                ? date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
                : date.toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
            el.setAttribute('data-localized', '');
        }
    }

    function onEnhancedLoad() {
        if (window.__applyTheme) {
            window.__applyTheme();
        }
        if (window.Prism) {
            window.Prism.highlightAll();
        }
        localizeTimes();
    }

    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancedload', onEnhancedLoad);
    }

    new MutationObserver(localizeTimes)
        .observe(document.body, { childList: true, subtree: true });
    localizeTimes();

    window.__toggleNav = function (button) {
        var nav = document.getElementById('site-nav');
        if (nav) {
            var open = nav.classList.toggle('open');
            button.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    };

    window.__scrollProjects = function (direction) {
        var carousel = document.getElementById('projects-carousel');
        if (carousel) {
            carousel.scrollBy({ left: direction * carousel.clientWidth * 0.8, behavior: 'smooth' });
        }
    };
})();

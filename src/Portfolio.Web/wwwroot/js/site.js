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
            var iso = el.getAttribute('datetime');
            if (!iso) {
                continue;
            }
            var date = new Date(iso);
            if (isNaN(date)) {
                continue;
            }
            el.textContent = el.dataset.local === 'date'
                ? date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
                : date.toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
            el.setAttribute('data-localized', '');
        }
    }

    // Prism auto-highlights the initial static document and onEnhancedLoad
    // covers enhanced navigations, but InteractiveServer islands (comments,
    // inboxes, the composer preview) render after both hooks. Highlight
    // whatever the mutation observer surfaces, exactly once per block.
    function highlightNewCode() {
        if (!window.Prism) {
            return;
        }
        var blocks = document.querySelectorAll('pre > code[class*="language-"]:not([data-highlighted])');
        for (var i = 0; i < blocks.length; i++) {
            blocks[i].setAttribute('data-highlighted', '');
            window.Prism.highlightElement(blocks[i]);
        }
    }

    // Coalesce bursts of DOM mutations into one enhancement pass per frame.
    var localizePending = false;
    function scheduleLocalizeTimes() {
        if (localizePending) {
            return;
        }
        localizePending = true;
        requestAnimationFrame(function () {
            localizePending = false;
            localizeTimes();
            highlightNewCode();
        });
    }

    function onEnhancedLoad() {
        if (typeof window.__applyTheme === 'function') {
            window.__applyTheme();
        }
        if (window.Prism) {
            window.Prism.highlightAll();
        }
        // The merged markup arrives with the mobile menu closed; keep the
        // burger's state in sync in case the header survived the merge.
        var nav = document.getElementById('site-nav');
        if (nav) {
            nav.classList.remove('open');
        }
        var burger = document.querySelector('.nav-burger');
        if (burger) {
            burger.setAttribute('aria-expanded', 'false');
        }
        localizeTimes();
    }

    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancedload', onEnhancedLoad);
    }

    new MutationObserver(scheduleLocalizeTimes)
        .observe(document.body, { childList: true, subtree: true });
    localizeTimes();
    highlightNewCode();

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

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
            // Marked before highlighting on purpose: a block Prism throws on
            // would otherwise be retried on every mutation, forever. The
            // per-block catch keeps one bad block from aborting the rest.
            blocks[i].setAttribute('data-highlighted', '');
            try {
                window.Prism.highlightElement(blocks[i]);
            } catch (e) {
                // Leave the block as plain monospace.
            }
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
        // Marker-guarded like the observer path: enhanced navigation swaps in
        // fresh unmarked markup, while surviving already-highlighted blocks
        // keep their attribute and are skipped instead of re-churned.
        highlightNewCode();
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

    function toggleNav(button) {
        var nav = document.getElementById('site-nav');
        if (nav) {
            var open = nav.classList.toggle('open');
            button.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    }

    function scrollProjects(direction) {
        var carousel = document.getElementById('projects-carousel');
        if (carousel) {
            carousel.scrollBy({ left: direction * carousel.clientWidth * 0.8, behavior: 'smooth' });
        }
    }

    // One delegated listener on document, keyed on data-action, instead of
    // per-element inline onclick="" attributes (or window globals rewired
    // per element). A listener on document survives Blazor enhanced-navigation
    // DOM swaps — the burger, theme toggle and carousel buttons are all
    // replaced wholesale on every merge — so nothing needs re-attaching in
    // onEnhancedLoad the way a per-element listener would.
    document.addEventListener('click', function (event) {
        // Only <button>s carry data-action; requiring the tag keeps a stray
        // attribute in rendered content inert (belt and braces: markdown
        // output never carries attributes anyway).
        var target = event.target.closest('button[data-action]');
        if (!target) {
            return;
        }

        switch (target.dataset.action) {
            case 'toggle-nav':
                toggleNav(target);
                break;
            case 'toggle-theme':
                // __toggleTheme stays in theme.js: it must also run from the
                // head before first paint, and __applyTheme's own flow calls
                // it there independently of this delegated handler.
                if (typeof window.__toggleTheme === 'function') {
                    window.__toggleTheme();
                }
                break;
            case 'scroll-projects':
                scrollProjects(parseInt(target.dataset.direction, 10) || 1);
                break;
        }
    });
})();

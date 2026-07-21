// After Blazor enhanced navigations swap the DOM: restore the theme attribute
// (the merged server markup doesn't carry data-theme) and re-run highlighting.
(function () {
    function onEnhancedLoad() {
        if (window.__applyTheme) {
            window.__applyTheme();
        }
        if (window.Prism) {
            window.Prism.highlightAll();
        }
    }

    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancedload', onEnhancedLoad);
    }

    window.__scrollProjects = function (direction) {
        var carousel = document.getElementById('projects-carousel');
        if (carousel) {
            carousel.scrollBy({ left: direction * carousel.clientWidth * 0.8, behavior: 'smooth' });
        }
    };
})();

// Re-run syntax highlighting after Blazor enhanced navigations swap the DOM.
(function () {
    function highlightAll() {
        if (window.Prism) {
            window.Prism.highlightAll();
        }
    }

    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancedload', highlightAll);
    }

    window.__scrollProjects = function (direction) {
        var carousel = document.getElementById('projects-carousel');
        if (carousel) {
            carousel.scrollBy({ left: direction * carousel.clientWidth * 0.8, behavior: 'smooth' });
        }
    };
})();

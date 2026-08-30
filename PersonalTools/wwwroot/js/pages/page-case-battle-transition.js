(() => {
    'use strict';
    const transition = document.querySelector('#caseBattleTransition, #caseDestinationTransition');
    const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const activate = () => { if (!transition || reduced()) return; transition.classList.add('is-active'); window.requestAnimationFrame(() => transition.classList.add('is-closing')); };
    const complete = () => { if (!transition || reduced()) return; window.setTimeout(() => transition.classList.remove('is-active', 'is-closing'), 650); };
    window.caseBattleTransition = {
        begin: activate,
        complete,
        navigate(url) { if (reduced()) { window.location.assign(url); return; } activate(); window.setTimeout(() => window.location.assign(url), 650); }
    };
    document.addEventListener('click', event => {
        const link = event.target.closest('[data-case-battle-transition-link]');
        if (!link || event.defaultPrevented || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || link.target) return;
        const href = link.getAttribute('href'); if (!href) return;
        event.preventDefault(); window.caseBattleTransition.navigate(href);
    });
})();

(() => {
    'use strict';
    const level = document.querySelector('#caseBattleHudLevel');
    if (!level) return;
    const xp = document.querySelector('#caseBattleHudXp');
    const xpTrack = document.querySelector('#caseBattleHudXpTrack');
    const xpFill = document.querySelector('#caseBattleHudXpFill');

    function renderProgress(progress) {
        const current = Number(progress.xpIntoLevel || 0), required = Math.max(1, Number(progress.xpForNextLevel || 100));
        const percent = Math.max(0, Math.min(100, Math.round((current / required) * 100)));
        level.textContent = `Lv ${Number(progress.level || 0)}`;
        xp.textContent = `${current.toLocaleString()} / ${required.toLocaleString()} XP`;
        xpFill.style.width = `${percent}%`;
        xpTrack.setAttribute('aria-valuenow', String(percent));
    }

    fetch('/api/case-opening/progress', { credentials: 'same-origin', cache: 'no-store' }).then(response => response.ok ? response.json() : null).then(progress => { if (progress) renderProgress(progress); }).catch(() => {});
})();

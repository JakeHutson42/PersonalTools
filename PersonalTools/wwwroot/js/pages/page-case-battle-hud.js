(() => {
    'use strict';
    const level = document.querySelector('#caseBattleHudLevel');
    if (!level) return;
    const xp = document.querySelector('#caseBattleHudXp');
    const xpTrack = document.querySelector('#caseBattleHudXpTrack');
    const xpFill = document.querySelector('#caseBattleHudXpFill');
    const nextLevel = document.querySelector('#caseBattleHudNextLevel');
    const avatar = document.querySelector('#caseBattleHudAvatar');
    const profileImages = { 'avatar:operative':'/images/case-tycoon/avatars/operative.svg', 'avatar:vanguard':'/images/case-tycoon/avatars/vanguard.svg', 'avatar:synth':'/images/case-tycoon/avatars/synth.svg' };
    function renderAvatar(value) {
        const imageUrl = profileImages[value];
        avatar.replaceChildren();
        avatar.classList.toggle('has-profile-image', Boolean(imageUrl));
        if (imageUrl) { const image = document.createElement('img'); image.src = imageUrl; image.alt = ''; avatar.append(image); }
        else avatar.textContent = value || '😎';
    }

    function renderProgress(progress) {
        const current = Number(progress.xpIntoLevel || 0), required = Math.max(1, Number(progress.xpForNextLevel || 100));
        const percent = Math.max(0, Math.min(100, Math.round((current / required) * 100)));
        level.textContent = String(Number(progress.level || 0));
        nextLevel.textContent = String(Number(progress.level || 0) + 1);
        xp.textContent = `${current.toLocaleString()} / ${required.toLocaleString()} XP`;
        xpFill.style.width = `${percent}%`;
        xpTrack.setAttribute('aria-valuenow', String(percent));
    }

    fetch('/api/settings', { credentials: 'same-origin', cache: 'no-store' }).then(response => response.ok ? response.json() : []).then(settings => {
        const profileEmoji = settings.find(item => item.definition?.key === 8 || item.definition?.key === 'CaseProfileEmoji' || item.definition?.name === 'Profile emoji');
        if (profileEmoji?.value) renderAvatar(profileEmoji.value);
    }).catch(() => {});

    fetch('/api/case-opening/progress', { credentials: 'same-origin', cache: 'no-store' }).then(response => response.ok ? response.json() : null).then(progress => { if (progress) renderProgress(progress); }).catch(() => {});
})();

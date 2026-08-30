/* global personalToolsToast */
(() => {
    'use strict';
    const root = document.querySelector('[data-case-battle-lobby]'); if (!root) return;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const status = document.querySelector('#caseBattleLobbyStatus'), elapsed = document.querySelector('#caseBattleLobbyElapsed'), scoreboard = document.querySelector('#caseBattleLobbyScoreboard'), track = document.querySelector('#caseBattleLobbyTrack'), ready = document.querySelector('#caseBattleLobbyReady'), actions = document.querySelector('#caseBattleLobbyActions');
    let battleId = root.dataset.caseBattleId, detail = null, timings = { readyPauseMs:900, readyCountdownMs:3000 }, caseImages = new Map(), currentReady = false, countdownShown = false, startScheduled = false, createdAt = null, refreshTimer = 0;
    const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const escape = value => String(value || '').replace(/[&<>"']/g, char => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' })[char]);
    const money = value => new Intl.NumberFormat('en-GB', { style:'currency', currency:'GBP', maximumFractionDigits:2 }).format(Number(value || 0));
    const duration = milliseconds => { const seconds = Math.max(0, Math.floor(milliseconds / 1000)); return String(Math.floor(seconds / 60)).padStart(2, '0') + ':' + String(seconds % 60).padStart(2, '0'); };
    const request = (url, method, body) => fetch(url, { method, credentials:'same-origin', headers:{ 'Content-Type':'application/json', RequestVerificationToken:token }, body:body === undefined ? undefined : JSON.stringify(body) }).then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const wait = ms => new Promise(resolve => window.setTimeout(resolve, ms));
    const playUrl = () => '/CaseOpening/Battles/' + encodeURIComponent(battleId);
    window.setInterval(() => { if (createdAt) elapsed.textContent = duration(Date.now() - createdAt); }, 1000);

    function renderScoreboard() {
        scoreboard.innerHTML = (detail.participants || []).map(player => {
            const isReady = player.isReady, initial = escape((player.displayName || '?').trim().charAt(0).toUpperCase());
            return '<article class="case-battle-player ' + (isReady ? 'is-ready' : 'is-waiting') + '"><span class="case-battle-player-avatar" aria-hidden="true">' + initial + '</span><span class="case-battle-player-identity"><small>Seat ' + player.seat + '</small><strong>' + escape(player.displayName) + '</strong></span><b>' + money(player.totalValue) + '</b><span class="case-battle-ready-state"><i class="fa-solid ' + (isReady ? 'fa-circle-check' : 'fa-circle-notch') + '" aria-hidden="true"></i>' + (isReady ? 'Ready' : 'Waiting') + '</span></article>';
        }).join('');
    }
    function render() {
        if (!detail) return;
        createdAt = new Date(detail.createdUtc).getTime(); elapsed.textContent = duration(Date.now() - createdAt);
        if (detail.status !== 'waiting') { status.textContent = detail.status === 'opening' ? 'Reels are ready' : 'Battle complete'; window.caseBattleTransition?.navigate(playUrl()); return; }
        const players = detail.participants || [], readyCount = players.filter(player => player.isReady).length;
        currentReady = Boolean(players.find(player => String(player.userId) === document.body.dataset.userId)?.isReady);
        status.textContent = readyCount === players.length ? 'Both players are ready' : readyCount + ' of ' + players.length + ' players ready';
        renderScoreboard();
        track.innerHTML = (detail.caseKeys || []).map((key, index) => '<span title="' + escape(key) + '"><b>' + (index + 1) + '</b><img src="' + escape(caseImages.get(String(key).toLowerCase()) || '') + '" alt=""></span>').join('');
        ready.innerHTML = '<span class="case-battle-lobby-ready-icon"><i class="fa-solid ' + (readyCount === players.length ? 'fa-bolt' : 'fa-shield-halved') + '" aria-hidden="true"></i></span><div><strong>' + (readyCount === players.length ? 'Opening sequence armed' : 'Ready when you are') + '</strong><small>' + (readyCount === players.length ? 'The case reels are being prepared.' : 'Both players need to ready up before the battle begins.') + '</small></div>';
        actions.innerHTML = '<button class="btn btn-outline-warning" type="button" data-lobby-action="ready">' + (currentReady ? 'Not ready' : 'Ready up') + '</button>' + (detail.isCreator ? '<button class="btn btn-outline-danger" type="button" data-lobby-action="cancel"><i class="fa-solid fa-xmark me-1" aria-hidden="true"></i>Cancel battle</button>' : '<button class="btn btn-outline-secondary" type="button" data-lobby-action="leave">Leave lobby</button>');
        if (readyCount === players.length) beginCountdown();
    }
    async function beginCountdown() {
        if (countdownShown) return;
        countdownShown = true;
        const seconds = Math.max(1, Math.ceil(timings.readyCountdownMs / 1000));
        ready.innerHTML = '<span class="case-battle-lobby-ready-icon is-armed"><i class="fa-solid fa-bolt" aria-hidden="true"></i></span><div><strong>Opening in <b data-lobby-countdown>' + seconds + '</b></strong><small>Loading the reels and finalising the room.</small></div>';
        if (!reduced()) await wait(timings.readyPauseMs);
        const counter = ready.querySelector('[data-lobby-countdown]');
        if (!reduced() && timings.readyCountdownMs) {
            const started = performance.now();
            await new Promise(resolve => { const frame = now => { const remaining = Math.max(0, timings.readyCountdownMs - (now - started)); if (counter) counter.textContent = String(Math.max(1, Math.ceil(remaining / 1000))); if (remaining) window.requestAnimationFrame(frame); else resolve(); }; window.requestAnimationFrame(frame); });
        }
        if (detail?.isCreator && !startScheduled) { startScheduled = true; request('/api/case-battles/' + encodeURIComponent(battleId) + '/start', 'POST').then(() => window.caseBattleTransition?.navigate(playUrl())).catch(message => { startScheduled = false; countdownShown = false; personalToolsToast?.error(message || 'The battle could not be started.'); load(); }); }
    }
    function load() { return request('/api/case-battles/' + encodeURIComponent(battleId) + '/detail', 'GET').then(value => { detail = value; render(); }).catch(() => { status.textContent = 'This battle is no longer available.'; }); }
    actions.addEventListener('click', event => {
        const button = event.target.closest('[data-lobby-action]'); if (!button) return;
        const action = button.dataset.lobbyAction; button.disabled = true;
        request('/api/case-battles/' + encodeURIComponent(battleId) + '/' + action, action === 'ready' ? 'PUT' : 'POST', action === 'ready' ? !currentReady : undefined)
            .then(() => { if (action === 'cancel' || action === 'leave') { personalToolsToast?.success(action === 'cancel' ? 'Battle cancelled and cases returned.' : 'You left the lobby.'); window.caseBattleTransition?.navigate('/CaseOpening'); return; } return load(); })
            .catch(message => { button.disabled = false; personalToolsToast?.error(message || 'The lobby could not be updated.'); });
    });
    function connectRealtime() { if (!window.signalR) return; const connection = new window.signalR.HubConnectionBuilder().withUrl('/hubs/case-battles').withAutomaticReconnect([0,1000,3000,8000]).build(); connection.on('BattleChanged', () => { window.clearTimeout(refreshTimer); refreshTimer = window.setTimeout(load, 80); }); connection.onreconnected(() => connection.invoke('JoinBattle', battleId).then(load).catch(() => {})); connection.start().then(() => connection.invoke('JoinBattle', battleId)).then(load).catch(load); }
    Promise.all([load(), fetch('/api/case-battles/timings', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : null), fetch('/api/case-opening/cases', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : [])]).then(([, settings, cases]) => { if (settings) timings = { ...timings, ...settings }; (cases || []).forEach(item => caseImages.set(String(item.caseKey || '').toLowerCase(), item.imageUrl || '')); render(); window.caseBattleTransition?.complete(); connectRealtime(); }).catch(() => window.caseBattleTransition?.complete());
})();

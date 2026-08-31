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
    const profileImages = { 'avatar:operative':'/images/case-tycoon/avatars/operative.svg', 'avatar:vanguard':'/images/case-tycoon/avatars/vanguard.svg', 'avatar:synth':'/images/case-tycoon/avatars/synth.svg' };
    const profileAvatar = (player, initial) => profileImages[player.profileAvatar] ? '<img src="' + profileImages[player.profileAvatar] + '" alt="" loading="eager">' : escape(player.profileAvatar || initial);
    const request = (url, method, body) => fetch(url, { method, credentials:'same-origin', headers:{ 'Content-Type':'application/json', RequestVerificationToken:token }, body:body === undefined ? undefined : JSON.stringify(body) }).then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const wait = ms => new Promise(resolve => window.setTimeout(resolve, ms));
    const playUrl = () => '/CaseOpening/Battles/' + encodeURIComponent(battleId);
    window.setInterval(() => { if (createdAt) elapsed.textContent = duration(Date.now() - createdAt); }, 1000);

    function renderScoreboard() {
        const participants = detail.participants || [], invitations = detail.invitations || [];
        const seats = [...participants];
        invitations.filter(invitation => !participants.some(player => String(player.userId) === String(invitation.invitedUserId))).forEach(invitation => seats.push({ displayName:invitation.displayName, invitationStatus:invitation.status, isReady:false }));
        while (seats.length < detail.requiredPlayers) seats.push({ displayName:'Waiting for invitation', invitationStatus:'open', isReady:false });
        scoreboard.dataset.players = String(detail.requiredPlayers || seats.length);
        scoreboard.innerHTML = seats.slice(0, detail.requiredPlayers).map((player, index) => {
            const isReady = player.isReady, initial = escape((player.displayName || '?').trim().charAt(0).toUpperCase());
            const state = player.invitationStatus || (isReady ? 'ready' : 'waiting');
            const stateClass = isReady ? 'ready' : state === 'pending' ? 'pending' : state === 'open' ? 'open' : 'waiting';
            const stateLabel = isReady ? 'Ready' : state === 'pending' ? 'Invite sent' : state === 'open' ? 'Connecting' : 'Not ready';
            const stateIcon = isReady ? 'fa-circle-check' : state === 'pending' ? 'fa-paper-plane' : state === 'open' ? 'fa-satellite-dish' : 'fa-circle-notch';
            return '<article class="case-battle-player is-' + stateClass + '" style="--seat-index:' + index + '"><span class="case-battle-player-connection" aria-hidden="true"><i></i><i></i><i></i></span><span class="case-battle-player-avatar" aria-hidden="true">' + profileAvatar(player, initial) + '</span><span class="case-battle-player-identity"><small>Seat ' + (player.seat || index + 1) + '</small><strong>' + escape(player.displayName) + '</strong></span><b>' + (player.userId ? money(player.totalValue) : '—') + '</b><span class="case-battle-ready-state"><i class="fa-solid ' + stateIcon + '" aria-hidden="true"></i>' + stateLabel + '</span></article>';
        }).join('');
    }
    function render() {
        if (!detail) return;
        createdAt = new Date(detail.createdUtc).getTime(); elapsed.textContent = duration(Date.now() - createdAt);
        if (detail.status !== 'waiting') { status.textContent = detail.status === 'opening' ? 'Reels are ready' : 'Battle complete'; window.caseBattleTransition?.navigate(playUrl()); return; }
        const players = detail.participants || [], readyCount = players.filter(player => player.isReady).length;
        currentReady = Boolean(players.find(player => String(player.userId) === document.body.dataset.userId)?.isReady);
        const roomFull = players.length === detail.requiredPlayers;
        const allReady = roomFull && readyCount === detail.requiredPlayers;
        status.textContent = allReady ? 'All players are ready' : players.length + ' of ' + detail.requiredPlayers + ' joined · ' + readyCount + ' ready';
        renderScoreboard();
        track.innerHTML = (detail.caseKeys || []).map((key, index) => '<span title="' + escape(key) + '"><b>' + (index + 1) + '</b><img src="' + escape(caseImages.get(String(key).toLowerCase()) || '') + '" alt=""></span>').join('');
        ready.innerHTML = '<span class="case-battle-lobby-ready-icon"><i class="fa-solid ' + (allReady ? 'fa-bolt' : 'fa-shield-halved') + '" aria-hidden="true"></i></span><div><strong>' + (allReady ? 'Opening sequence armed' : roomFull ? 'Ready when you are' : 'Waiting for every seat') + '</strong><small>' + (allReady ? 'The case reels are being prepared.' : roomFull ? 'Every player needs to ready up before the battle begins.' : 'Invited players must join before the countdown can start.') + '</small></div>';
        actions.innerHTML = '<button class="btn case-battle-lobby-action is-ready-action" type="button" data-lobby-action="ready"><i class="fa-solid ' + (currentReady ? 'fa-pause' : 'fa-bolt') + '" aria-hidden="true"></i><span><small>' + (currentReady ? 'Status locked' : 'Confirm loadout') + '</small><strong>' + (currentReady ? 'Stand down' : 'Ready up') + '</strong></span></button>' + (detail.isCreator ? '<button class="btn case-battle-lobby-action is-exit-action" type="button" data-lobby-action="cancel"><i class="fa-solid fa-xmark" aria-hidden="true"></i><span><small>Owner control</small><strong>Cancel battle</strong></span></button>' : '<button class="btn case-battle-lobby-action is-exit-action" type="button" data-lobby-action="leave"><i class="fa-solid fa-arrow-right-from-bracket" aria-hidden="true"></i><span><small>Exit room</small><strong>Leave lobby</strong></span></button>');
        if (allReady) beginCountdown();
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

/* global personalToolsToast */
(() => {
    'use strict';
    const status = document.querySelector('#caseBattleStatus'), scoreboard = document.querySelector('#caseBattleScoreboard'), track = document.querySelector('#caseBattleCaseTrack'), arena = document.querySelector('#caseBattleArena'), actions = document.querySelector('#caseBattleActions'), results = document.querySelector('#caseBattleResults'), pot = document.querySelector('#caseBattlePot'), showcase = document.querySelector('#caseBattleShowcase');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    let battleId = document.querySelector('[data-case-battle-id]')?.dataset.caseBattleId || null, replaying = false, refreshTimer = 0, showcaseShown = false;
    const shownRounds = new Set();
    const money = value => new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 2 }).format(Number(value || 0));
    const escape = value => String(value || '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]);
    const request = (url, method, body) => fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type': 'application/json', RequestVerificationToken: token }, body: body === undefined ? undefined : JSON.stringify(body) }).then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const wait = ms => new Promise(resolve => window.setTimeout(resolve, ms));
    const pullsFor = (detail, userId) => (detail.pulls || []).filter(pull => pull.originalOwnerUserId === userId && shownRounds.has(pull.roundNumber));

    function renderScoreboard(detail, useReplayTotals) {
        scoreboard.innerHTML = (detail.participants || []).map(player => {
            const value = useReplayTotals ? pullsFor(detail, player.userId).reduce((sum, pull) => sum + Number(pull.lockedValue || 0), 0) : player.totalValue;
            return '<article class="case-battle-player"><strong>' + escape(player.displayName) + '</strong><span>Seat ' + player.seat + ' · ' + (player.isReady ? 'Ready' : 'Waiting') + '</span><b>' + money(value) + '</b></article>';
        }).join('');
    }
    function renderResultRails(detail) {
        if (!detail.pulls?.length) { results.innerHTML = '<p class="small-muted mb-0">Each player’s pulls will be collected here without covering the reels.</p>'; return; }
        results.innerHTML = (detail.participants || []).map(player => {
            const pulls = pullsFor(detail, player.userId);
            const cards = pulls.map(pull => '<article class="case-battle-result" style="--battle-rarity:' + escape(pull.rarityColor) + '"><img src="' + escape(pull.imageUrl) + '" alt="" loading="lazy"><span title="' + escape(pull.itemName) + '">' + escape(pull.itemName) + '</span><b>' + money(pull.lockedValue) + '</b></article>').join('');
            return '<section class="case-battle-result-rail"><header><strong>' + escape(player.displayName) + '</strong><span>' + pulls.length + ' pull' + (pulls.length === 1 ? '' : 's') + '</span></header><div class="case-battle-result-cards">' + (cards || '<span class="small-muted">No pulls yet</span>') + '</div></section>';
        }).join('');
    }
    function render(detail) {
        battleId = detail.battleId;
        status.textContent = detail.status === 'settled' ? 'Settled · results are permanent.' : detail.joinedPlayers + ' / ' + detail.requiredPlayers + ' players · ' + detail.status;
        renderScoreboard(detail, detail.pulls?.length > 0);
        track.innerHTML = (detail.caseKeys || []).map((key, index) => '<span class="' + (shownRounds.has(index) ? 'is-complete' : '') + '"><b>' + (index + 1) + '</b>' + escape(key) + '</span>').join('');
        pot.textContent = detail.pulls?.length ? 'Locked pot ' + money(detail.lockedPotValue) : 'Locked value pending';
        renderResultRails(detail);
        const readyCount = (detail.participants || []).filter(player => player.isReady).length;
        let html = '';
        if (detail.status === 'waiting') { html += '<button class="btn btn-outline-warning" type="button" data-battle-action="ready">Toggle ready</button>'; if (readyCount === detail.requiredPlayers) html += '<button class="btn btn-warning" type="button" data-battle-action="start">Start battle</button>'; html += detail.isCreator ? '<button class="btn btn-outline-danger" type="button" data-battle-action="cancel">Cancel & return cases</button>' : '<button class="btn btn-outline-secondary" type="button" data-battle-action="leave">Leave battle</button>'; }
        if (detail.status === 'opening' && !detail.pulls?.length) html = '<button class="btn btn-warning" type="button" data-battle-action="start">Resume battle</button>';
        if (detail.status === 'settled') html = '<span class="case-battle-settled"><i class="fa-solid fa-trophy" aria-hidden="true"></i>Settlement complete. Inventory and Battle Overflow deliveries are recorded.</span>';
        actions.innerHTML = html;
        replayAvailable(detail);
    }
    function spinLane(player, winner, allPulls) {
        const lane = document.createElement('article'); lane.className = 'case-battle-spin-lane';
        lane.innerHTML = '<header><strong></strong><span>Opening…</span></header><div class="case-battle-reel-window"><i class="case-battle-reel-marker" aria-hidden="true"></i><div class="case-battle-reel"></div></div>';
        lane.querySelector('header strong').textContent = player.displayName;
        const reel = lane.querySelector('.case-battle-reel'), decoys = allPulls.length ? allPulls : [winner];
        Array.from({ length: 18 }, (_, index) => index === 14 ? winner : decoys[(index * 7 + winner.roundNumber) % decoys.length]).forEach(item => {
            const card = document.createElement('article'); card.className = 'case-battle-reel-item'; card.style.setProperty('--rarity', item.rarityColor || '#e4ae39'); card.innerHTML = '<img alt=""><span></span>'; card.querySelector('img').src = item.imageUrl || ''; card.querySelector('span').textContent = item.itemName || 'Unknown item'; reel.append(card);
        });
        return { lane, reel };
    }
    async function replayRound(detail, round) {
        const pulls = (detail.pulls || []).filter(pull => pull.roundNumber === round); if (!pulls.length) return;
        const byUser = new Map(pulls.map(pull => [pull.originalOwnerUserId, pull]));
        arena.innerHTML = '<header class="case-battle-round-heading"><span>Round ' + (round + 1) + ' of ' + detail.caseKeys.length + '</span><strong>' + escape(detail.caseKeys[round] || 'Case') + '</strong></header><div class="case-battle-spin-grid"></div>';
        const grid = arena.querySelector('.case-battle-spin-grid');
        const lanes = (detail.participants || []).map(player => spinLane(player, byUser.get(player.userId), detail.pulls || [])); lanes.forEach(item => grid.append(item.lane));
        await new Promise(resolve => window.requestAnimationFrame(() => window.requestAnimationFrame(resolve)));
        // Keep the concurrent reels readable: a deliberate spin followed by a clear landed result.
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches, duration = reduced ? 80 : 3800;
        await Promise.all(lanes.map(({ lane, reel }) => new Promise(resolve => {
            const winner = reel.children[14], target = (lane.querySelector('.case-battle-reel-window').clientWidth / 2) - (winner.offsetLeft + winner.offsetWidth / 2), finish = () => { lane.classList.add('is-revealed'); resolve(); };
            if (reduced || !window.anime?.animate) { reel.style.transform = 'translate3d(' + target + 'px,0,0)'; finish(); } else window.anime.animate(reel, { translateX: [0, target], duration, ease: 'out(5)', complete: finish });
        })));
        await wait(reduced ? 0 : 1200); shownRounds.add(round); renderScoreboard(detail, true); renderResultRails(detail); track.querySelectorAll('span')[round]?.classList.add('is-complete');
    }
    async function replayAvailable(detail) { if (replaying || !detail.pulls?.length) return; replaying = true; const rounds = [...new Set(detail.pulls.map(pull => pull.roundNumber))].sort((a, b) => a - b); for (const round of rounds) if (!shownRounds.has(round)) await replayRound(detail, round); replaying = false; revealShowcase(detail); }
    function animateEarnings(element, from, to, duration) {
        if (!element) return;
        const started = performance.now();
        const frame = now => { const progress = Math.min(1, (now - started) / duration); const eased = 1 - Math.pow(1 - progress, 4); element.textContent = money(from + ((to - from) * eased)); if (progress < 1) window.requestAnimationFrame(frame); };
        window.requestAnimationFrame(frame);
    }
    async function revealShowcase(detail) {
        if (showcaseShown || detail.status !== 'settled' || !detail.winningUserId || shownRounds.size < detail.caseKeys.length) return;
        showcaseShown = true;
        const winnerId = String(detail.winningUserId);
        const panels = (detail.participants || []).map(player => '<article class="case-battle-finalist" data-user-id="' + escape(player.userId) + '"><span class="case-battle-smoke" aria-hidden="true"></span><p class="eyebrow mb-1">' + (String(player.userId) === winnerId ? 'Final total' : 'Final total') + '</p><strong>' + escape(player.displayName) + '</strong><b data-earnings>£0.00</b><small>' + (String(player.userId) === winnerId ? 'Awaiting verdict' : 'Awaiting verdict') + '</small></article>').join('');
        showcase.innerHTML = '<div class="case-battle-showcase-title"><p class="eyebrow mb-1">Battle complete</p><h2 class="h3 mb-0">Final verdict</h2><span>Calculating the winner…</span></div><div class="case-battle-finalists">' + panels + '</div>';
        showcase.classList.remove('d-none');
        await wait(window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 1100);
        const winner = showcase.querySelector('[data-user-id="' + CSS.escape(winnerId) + '"]');
        const loser = [...showcase.querySelectorAll('.case-battle-finalist')].find(panel => panel !== winner);
        const valueFor = panel => Number((detail.participants || []).find(player => String(player.userId) === panel?.dataset.userId)?.totalValue || 0);
        const winnerValue = valueFor(winner), loserValue = valueFor(loser), finalWinnerValue = Number(detail.lockedPotValue || winnerValue + loserValue);
        const winnerEarnings = winner?.querySelector('[data-earnings]'), loserEarnings = loser?.querySelector('[data-earnings]');

        showcase.querySelector('.case-battle-showcase-title span').textContent = 'Tallying the battle earnings…';
        await Promise.all([
            new Promise(resolve => { animateEarnings(winnerEarnings, 0, winnerValue, 1500); window.setTimeout(resolve, 1500); }),
            new Promise(resolve => { animateEarnings(loserEarnings, 0, loserValue, 1500); window.setTimeout(resolve, 1500); })
        ]);
        await wait(window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 800);
        winner?.classList.add('is-winner'); loser?.classList.add('is-loser');
        winner?.querySelector('small')?.replaceChildren('Winner · taking the pot'); loser?.querySelector('small')?.replaceChildren('Runner-up · pot transferred');
        showcase.querySelector('.case-battle-showcase-title span').textContent = (winner?.querySelector('strong')?.textContent || 'Winner') + ' takes the pot';
        await Promise.all([
            new Promise(resolve => { animateEarnings(loserEarnings, loserValue, 0, 1450); window.setTimeout(resolve, 1450); }),
            new Promise(resolve => { animateEarnings(winnerEarnings, winnerValue, finalWinnerValue, 1450); window.setTimeout(resolve, 1450); })
        ]);
        winner?.querySelector('small')?.replaceChildren('Winner · settlement locked'); loser?.querySelector('small')?.replaceChildren('Runner-up · settlement locked');
    }
    function load() { if (!battleId) { window.location.replace('/CaseOpening'); return Promise.resolve(); } return request('/api/case-battles/' + encodeURIComponent(battleId) + '/detail', 'GET').then(render).catch(() => { status.textContent = 'Unable to restore this battle.'; }); }
    actions.addEventListener('click', event => { const button = event.target.closest('[data-battle-action]'); if (!button || !battleId) return; const action = button.dataset.battleAction; request('/api/case-battles/' + encodeURIComponent(battleId) + '/' + action, action === 'ready' ? 'PUT' : 'POST', action === 'ready' ? true : undefined).then(load).catch(message => personalToolsToast?.error(message || 'The battle could not be updated.')); });
    function connectRealtime() { if (!battleId || !window.signalR) return; const connection = new window.signalR.HubConnectionBuilder().withUrl('/hubs/case-battles').withAutomaticReconnect([0, 1000, 3000, 8000]).build(); connection.on('BattleChanged', () => { window.clearTimeout(refreshTimer); refreshTimer = window.setTimeout(load, 80); }); connection.onreconnected(() => connection.invoke('JoinBattle', battleId).then(load).catch(() => {})); connection.start().then(() => connection.invoke('JoinBattle', battleId)).then(load).catch(() => { status.dataset.connection = 'offline'; }); }
    load().then(connectRealtime);
})();

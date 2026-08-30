/* global personalToolsToast */
(() => {
    'use strict';
    const status = document.querySelector('#caseBattleStatus'), elapsed = document.querySelector('#caseBattleElapsed'), scoreboard = document.querySelector('#caseBattleScoreboard'), track = document.querySelector('#caseBattleCaseTrack'), arena = document.querySelector('#caseBattleArena'), actions = document.querySelector('#caseBattleActions'), results = document.querySelector('#caseBattleResults'), resultsSection = document.querySelector('#caseBattleResultsSection'), pot = document.querySelector('#caseBattlePot'), showcase = document.querySelector('#caseBattleShowcase'), winnerBackdrop = document.querySelector('#caseBattleWinnerBackdrop');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    let battleId = document.querySelector('[data-case-battle-id]')?.dataset.caseBattleId || null, latestDetail = null, replaying = false, refreshTimer = 0, showcaseShown = false, showcaseDismissEnabled = false, battleCreatedAt = null, currentReady = false, readySequenceShown = false, autoStartScheduled = false, realtimeConnection = null;
    let timings = { readyPauseMs:900, readyCountdownMs:3000, preSpinPauseMs:2500, spinDurationMs:5000, resultsPauseMs:1100, winnerIntroPauseMs:2500, winnerTallyDurationMs:4200, winnerVerdictPauseMs:1800, winnerTransferDurationMs:3500 };
    const caseImages = new Map();
    const shownRounds = new Set();
    const reactionStage = document.querySelector('#caseBattleReactionStage');
    const money = value => new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 2 }).format(Number(value || 0));
    const escape = value => String(value || '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]);
    const request = (url, method, body) => fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type': 'application/json', RequestVerificationToken: token }, body: body === undefined ? undefined : JSON.stringify(body) }).then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const wait = ms => new Promise(resolve => window.setTimeout(resolve, ms));
    const pullsFor = (detail, userId) => (detail.pulls || []).filter(pull => pull.originalOwnerUserId === userId && shownRounds.has(pull.roundNumber));
    const duration = milliseconds => { const total = Math.max(0, Math.floor(milliseconds / 1000)); const hours = Math.floor(total / 3600), minutes = Math.floor((total % 3600) / 60), seconds = total % 60; return (hours ? String(hours).padStart(2, '0') + ':' : '') + String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0'); };
    function showReaction(emoji) { if (!reactionStage || !emoji) return; const reaction = document.createElement('span'); reaction.className = 'case-battle-floating-reaction'; reaction.textContent = emoji; reaction.style.setProperty('--reaction-x', (1.1 + Math.random() * 4.5).toFixed(1) + 'rem'); reaction.style.setProperty('--reaction-drift', (10 + Math.random() * 32).toFixed(1) + 'vw'); reaction.style.setProperty('--reaction-sway', ((Math.random() * 14) - 7).toFixed(1) + 'vw'); reaction.style.setProperty('--reaction-peak', (42 + Math.random() * 18).toFixed(1) + 'vh'); reaction.style.setProperty('--reaction-turn', ((Math.random() * 52) - 26).toFixed(0) + 'deg'); reaction.style.setProperty('--reaction-duration', (2.15 + Math.random() * .7).toFixed(2) + 's'); reactionStage.append(reaction); window.setTimeout(() => reaction.remove(), 3100); }
    window.setInterval(() => { if (elapsed && battleCreatedAt) elapsed.textContent = duration(Date.now() - battleCreatedAt); }, 1000);

    function renderScoreboard(detail, useReplayTotals) {
        scoreboard.innerHTML = (detail.participants || []).map(player => {
            const initial = escape((player.displayName || '?').trim().charAt(0).toUpperCase());
            return '<span class="case-battle-compact-player" data-player-id="' + escape(player.userId) + '"><span aria-hidden="true">' + initial + '</span><strong>' + escape(player.displayName) + '</strong></span>';
        }).join('');
    }
    function renderResultRails(detail) {
        if (!detail.pulls?.length) { results.innerHTML = '<p class="small-muted mb-0">Each player’s pulls will be collected here without covering the reels.</p>'; return; }
        results.innerHTML = (detail.participants || []).map(player => {
            const pulls = pullsFor(detail, player.userId);
            const cards = pulls.map(pull => '<article class="case-battle-result" style="--battle-rarity:' + escape(pull.rarityColor) + '"><img src="' + escape(pull.imageUrl) + '" alt="" loading="lazy"><span class="case-battle-item-name" title="' + escape(pull.itemName) + '">' + escape(pull.itemName) + '</span><small class="case-battle-item-wear">' + escape(pull.wear || 'Wear unavailable') + '</small><b>' + money(pull.lockedValue) + '</b></article>').join('');
            return '<section class="case-battle-result-rail"><header><strong>' + escape(player.displayName) + '</strong><span>' + pulls.length + ' pull' + (pulls.length === 1 ? '' : 's') + '</span></header><div class="case-battle-result-cards">' + (cards || '<span class="small-muted">No pulls yet</span>') + '</div></section>';
        }).join('');
    }
    function slideTrackTo(index) {
        const viewport = track?.parentElement; const items = track?.querySelectorAll('span') || []; const item = items[Math.min(index, Math.max(0, items.length - 1))];
        if (!viewport || !item) return;
        const target = (viewport.clientWidth / 2) - (item.offsetLeft + (item.offsetWidth / 2));
        track.style.transform = 'translate3d(' + target + 'px,0,0)';
    }
    function render(detail) {
        latestDetail = detail;
        battleId = detail.battleId;
        battleCreatedAt = new Date(detail.createdUtc).getTime();
        if (elapsed) elapsed.textContent = duration(Date.now() - battleCreatedAt);
        status.textContent = detail.status === 'settled' ? 'Battle complete' : detail.status === 'opening' ? 'Opening cases' : detail.joinedPlayers + ' / ' + detail.requiredPlayers + ' players in the room';
        renderScoreboard(detail, detail.pulls?.length > 0);
        track.innerHTML = (detail.caseKeys || []).map((key, index) => '<span class="' + (shownRounds.has(index) ? 'is-complete' : '') + '" title="' + escape(key) + '"><b>' + (index + 1) + '</b><img src="' + escape(caseImages.get(String(key).toLowerCase()) || '') + '" alt=""></span>').join('');
        window.requestAnimationFrame(() => slideTrackTo(Math.min(shownRounds.size, Math.max(0, detail.caseKeys.length - 1))));
        pot.textContent = detail.pulls?.length ? 'Pot ' + money(detail.lockedPotValue) : 'Pot pending';
        renderResultRails(detail);
        const readyCount = (detail.participants || []).filter(player => player.isReady).length;
        currentReady = Boolean((detail.participants || []).find(player => String(player.userId) === document.body.dataset.userId)?.isReady);
        let html = '';
        if (detail.status === 'waiting') { html += '<button class="btn btn-outline-warning" type="button" data-battle-action="ready">' + (currentReady ? 'Not ready' : 'Ready up') + '</button>'; if (readyCount === detail.requiredPlayers && !readySequenceShown) html += '<button class="btn btn-warning" type="button" data-battle-action="start">Start battle</button>'; html += detail.isCreator ? '<button class="btn btn-outline-danger" type="button" data-battle-action="cancel"><i class="fa-solid fa-xmark me-1" aria-hidden="true"></i>Cancel invitation</button>' : '<button class="btn btn-outline-secondary" type="button" data-battle-action="leave">Leave battle</button>'; }
        if (detail.status === 'opening' && !detail.pulls?.length) html = '<button class="btn btn-warning" type="button" data-battle-action="start">Resume battle</button>';
        if (detail.status === 'settled') html = '';
        actions.innerHTML = html;
        if (detail.status === 'waiting' && readyCount === detail.requiredPlayers) runReadySequence(detail);
        replayAvailable(detail);
    }
    async function runReadySequence(detail) {
        if (readySequenceShown) return;
        readySequenceShown = true;
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const totalSeconds = Math.max(1, Math.ceil(timings.readyCountdownMs / 1000));
        arena.innerHTML = '<section class="case-battle-countdown" aria-live="assertive"><span class="case-battle-countdown-orbit" aria-hidden="true"></span><p class="eyebrow mb-1">Both players ready</p><strong data-countdown>' + totalSeconds + '</strong><span>Preparing the opening reels…</span></section>';
        window.caseBattleAudio?.start();
        arena.scrollIntoView({ behavior:reduced ? 'auto' : 'smooth', block:'center' });
        if (!reduced) await wait(timings.readyPauseMs);
        const counter = arena.querySelector('[data-countdown]');
        if (!reduced && timings.readyCountdownMs) {
            const started = performance.now();
            await new Promise(resolve => {
                const frame = now => { const remaining = Math.max(0, timings.readyCountdownMs - (now - started)); if (counter) counter.textContent = String(Math.max(1, Math.ceil(remaining / 1000))); if (remaining > 0) window.requestAnimationFrame(frame); else resolve(); };
                window.requestAnimationFrame(frame);
            });
        }
        if (detail.isCreator && !autoStartScheduled) { autoStartScheduled = true; request('/api/case-battles/' + encodeURIComponent(battleId) + '/start', 'POST').then(render).catch(message => { autoStartScheduled = false; readySequenceShown = false; personalToolsToast?.error(message || 'The battle could not be started.'); load(); }); }
    }
    function spinLane(player, winner, allPulls) {
        const lane = document.createElement('article'); lane.className = 'case-battle-spin-lane';
        lane.innerHTML = '<header><strong></strong><span>Opening…</span></header><div class="case-battle-reel-window"><i class="case-battle-reel-marker" aria-hidden="true"></i><div class="case-battle-reel"></div></div>';
        lane.querySelector('header strong').textContent = player.displayName;
        const reel = lane.querySelector('.case-battle-reel'), decoys = allPulls.length ? allPulls : [winner];
        Array.from({ length: 18 }, (_, index) => index === 14 ? winner : decoys[(index * 7 + winner.roundNumber) % decoys.length]).forEach(item => {
            const card = document.createElement('article'); card.className = 'case-battle-reel-item'; card.style.setProperty('--rarity', item.rarityColor || '#e4ae39'); card.innerHTML = '<img alt=""><span><strong></strong><small></small><b></b></span>'; card.querySelector('img').src = item.imageUrl || ''; card.querySelector('strong').textContent = item.itemName || 'Unknown item'; card.querySelector('small').textContent = item.wear || 'Wear unavailable'; card.querySelector('b').textContent = money(item.lockedValue); reel.append(card);
        });
        return { lane, reel };
    }
    function settleReelOnWinner(lane, reel, winner, initialTarget) {
        // Image rendering and responsive card widths can move the apparent centre by a pixel or
        // two after the animation target was measured. Snap from the final painted geometry so
        // the marker and the server-selected result always agree before the next round begins.
        const viewport = lane.querySelector('.case-battle-reel-window');
        const markerCentre = viewport.getBoundingClientRect().left + (viewport.clientWidth / 2);
        const winnerBounds = winner.getBoundingClientRect();
        const correctedTarget = initialTarget + markerCentre - (winnerBounds.left + (winnerBounds.width / 2));
        reel.style.transform = 'translate3d(' + correctedTarget + 'px,0,0)';
        winner.classList.add('is-winning-item');
        lane.classList.add('is-revealed');
    }
    async function replayRound(detail, round) {
        const pulls = (detail.pulls || []).filter(pull => pull.roundNumber === round); if (!pulls.length) return;
        const byUser = new Map(pulls.map(pull => [pull.originalOwnerUserId, pull]));
        arena.innerHTML = '<header class="case-battle-round-heading"><span>Round ' + (round + 1) + ' of ' + detail.caseKeys.length + '</span><strong>' + escape(detail.caseKeys[round] || 'Case') + '</strong></header><div class="case-battle-spin-grid"></div>';
        const grid = arena.querySelector('.case-battle-spin-grid');
        const lanes = (detail.participants || []).map(player => spinLane(player, byUser.get(player.userId), detail.pulls || [])); lanes.forEach(item => grid.append(item.lane));
        await new Promise(resolve => window.requestAnimationFrame(() => window.requestAnimationFrame(resolve)));
        if (round === 0) arena.scrollIntoView({ behavior:window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block:'center' });
        // The track advances only as this round's reels start, so its highlighted case always
        // agrees with the round number and never gives away the following spin.
        slideTrackTo(round);
        // Keep the concurrent reels readable: a deliberate spin followed by a clear landed result.
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const totalSpinDuration = Math.max(3000, Math.min(5000, Number(timings.spinDurationMs || 5000)));
        const landedHoldDuration = 1000;
        const reelMotionDuration = reduced ? 80 : totalSpinDuration - landedHoldDuration;
        await Promise.all(lanes.map(({ lane, reel }) => new Promise(resolve => {
            const winner = reel.children[14], target = (lane.querySelector('.case-battle-reel-window').clientWidth / 2) - (winner.offsetLeft + winner.offsetWidth / 2);
            const finish = () => window.requestAnimationFrame(() => {
                settleReelOnWinner(lane, reel, winner, target);
                // Resolve only after the corrected winning frame has actually painted. The
                // landed and reveal pauses therefore cannot overlap the following case spin.
                window.requestAnimationFrame(resolve);
            });
            if (reduced || !window.anime?.animate) {
                reel.style.transform = 'translate3d(' + target + 'px,0,0)';
                finish();
            } else {
                window.anime.animate(reel, { translateX: [0, target], duration: reelMotionDuration, ease: 'out(4)', onComplete: finish });
            }
        })));
        shownRounds.add(round);
        renderScoreboard(detail, true);
        renderResultRails(detail);
        track.querySelectorAll('span')[round]?.classList.add('is-complete');
        // SpinDurationMs is the complete round duration. Its final second is always a stable,
        // painted winning result; no separate pause can overlap or extend into the next reel.
        await wait(reduced ? 0 : landedHoldDuration);
    }
    async function showPreSpinPause() {
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (reduced || !timings.preSpinPauseMs) return;
        const totalSeconds = Math.max(1, Math.ceil(timings.preSpinPauseMs / 1000));
        arena.innerHTML = '<section class="case-battle-countdown case-battle-attention-countdown" aria-live="assertive"><span class="case-battle-countdown-orbit" aria-hidden="true"></span><p class="eyebrow mb-1">Reels ready</p><strong data-pre-spin-countdown>' + totalSeconds + '</strong><span>First spin starts in a moment…</span></section>';
        arena.scrollIntoView({ behavior:'smooth', block:'center' });
        const counter = arena.querySelector('[data-pre-spin-countdown]'), started = performance.now();
        await new Promise(resolve => {
            const frame = now => { const remaining = Math.max(0, timings.preSpinPauseMs - (now - started)); if (counter) counter.textContent = String(Math.max(1, Math.ceil(remaining / 1000))); if (remaining > 0) window.requestAnimationFrame(frame); else resolve(); };
            window.requestAnimationFrame(frame);
        });
    }
    async function replayAvailable(detail) {
        if (replaying || !detail.pulls?.length) return;
        replaying = true;
        document.querySelector('#caseBattleAdminReplay')?.setAttribute('disabled', '');
        window.caseBattleAudio?.start();
        await showPreSpinPause();
        const rounds = [...new Set(detail.pulls.map(pull => pull.roundNumber))].sort((a, b) => a - b);
        for (const round of rounds) if (!shownRounds.has(round)) await replayRound(detail, round);
        if (detail.status === 'settled') { resultsSection?.scrollIntoView({ behavior:window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block:'start' }); await wait(window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : timings.resultsPauseMs); }
        await revealShowcase(detail);
        replaying = false;
        document.querySelector('#caseBattleAdminReplay')?.removeAttribute('disabled');
    }
    function animateEarnings(element, from, to, duration) {
        if (!element) return;
        const started = performance.now();
        const frame = now => { const progress = Math.min(1, (now - started) / duration); const eased = 1 - Math.pow(1 - progress, 4); element.textContent = money(from + ((to - from) * eased)); if (progress < 1) window.requestAnimationFrame(frame); };
        window.requestAnimationFrame(frame);
    }
    async function revealShowcase(detail) {
        if (showcaseShown || detail.status !== 'settled' || !detail.winningUserId || shownRounds.size < detail.caseKeys.length) return;
        showcaseShown = true;
        showcaseDismissEnabled = false;
        window.caseBattleAudio?.duckForReveal();
        const winnerId = String(detail.winningUserId), highlight = [...(detail.pulls || [])].sort((left, right) => Number(right.lockedValue || 0) - Number(left.lockedValue || 0))[0];
        const panels = (detail.participants || []).map(player => '<article class="case-battle-finalist" data-user-id="' + escape(player.userId) + '"><span class="case-battle-smoke" aria-hidden="true"></span><span class="case-battle-finalist-status"><i class="fa-solid fa-hourglass-half" aria-hidden="true"></i>Awaiting verdict</span><p class="eyebrow mb-1">Final total</p><strong>' + escape(player.displayName) + '</strong><b data-earnings>£0.00</b></article>').join('');
        const sparks = Array.from({ length:28 }, (_, index) => '<i style="--spark:' + index + '"></i>').join('');
        showcase.innerHTML = '<button class="case-battle-winner-dismiss" type="button" data-winner-dismiss aria-label="Close winner reveal" hidden><i class="fa-solid fa-xmark" aria-hidden="true"></i></button><div class="case-battle-victory-effects" aria-hidden="true"><span class="case-battle-victory-flash"></span><span class="case-battle-victory-rings"></span><span class="case-battle-victory-fireworks">' + sparks + '</span></div><div class="case-battle-showcase-title"><p class="eyebrow mb-1">Battle complete</p><h2 class="h3 mb-0">Final verdict</h2><span>Calculating the winner…</span></div><div class="case-battle-victory-item">' + (highlight ? '<img src="' + escape(highlight.imageUrl) + '" alt=""><span><small>Signature pull</small><strong>' + escape(highlight.itemName) + '</strong></span>' : '') + '</div><div class="case-battle-finalists">' + panels + '</div>';
        winnerBackdrop?.classList.remove('d-none');
        showcase.classList.remove('d-none');
        showcase.focus({ preventScroll:true });
        await wait(window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : timings.winnerIntroPauseMs);
        const winner = showcase.querySelector('[data-user-id="' + CSS.escape(winnerId) + '"]');
        const loser = [...showcase.querySelectorAll('.case-battle-finalist')].find(panel => panel !== winner);
        const valueFor = panel => Number((detail.participants || []).find(player => String(player.userId) === panel?.dataset.userId)?.totalValue || 0);
        const winnerValue = valueFor(winner), loserValue = valueFor(loser), finalWinnerValue = Number(detail.lockedPotValue || winnerValue + loserValue);
        const winnerEarnings = winner?.querySelector('[data-earnings]'), loserEarnings = loser?.querySelector('[data-earnings]');

        showcase.querySelector('.case-battle-showcase-title span').textContent = 'Tallying the battle earnings…';
        await Promise.all([
            new Promise(resolve => { animateEarnings(winnerEarnings, 0, winnerValue, timings.winnerTallyDurationMs); window.setTimeout(resolve, timings.winnerTallyDurationMs); }),
            new Promise(resolve => { animateEarnings(loserEarnings, 0, loserValue, timings.winnerTallyDurationMs); window.setTimeout(resolve, timings.winnerTallyDurationMs); })
        ]);
        await wait(window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : timings.winnerVerdictPauseMs);
        winner?.classList.add('is-winner'); loser?.classList.add('is-loser');
        winner?.querySelector('.case-battle-finalist-status')?.replaceChildren('Winner'); loser?.querySelector('.case-battle-finalist-status')?.replaceChildren('Runner-up');
        showcase.querySelector('.case-battle-showcase-title span').textContent = (winner?.querySelector('strong')?.textContent || 'Winner') + ' takes the pot';
        const transferAnimation = Promise.all([
            new Promise(resolve => { animateEarnings(loserEarnings, loserValue, 0, timings.winnerTransferDurationMs); window.setTimeout(resolve, timings.winnerTransferDurationMs); }),
            new Promise(resolve => { animateEarnings(winnerEarnings, winnerValue, finalWinnerValue, timings.winnerTransferDurationMs); window.setTimeout(resolve, timings.winnerTransferDurationMs); })
        ]);
        // Let the winner's final profit counter visibly begin before the celebration enters. This
        // keeps the neutral side-by-side tally from revealing the outcome ahead of the verdict.
        const celebrationDelay = Math.min(280, Math.max(40, Number(timings.winnerTransferDurationMs || 0) * .12));
        await wait(window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : celebrationDelay);
        winner?.classList.add('is-tallying-winner');
        window.caseBattleAudio?.playWinnerReveal();
        if (winner && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            const crownRain = document.createElement('span');
            crownRain.className = 'case-battle-crown-rain';
            crownRain.setAttribute('aria-hidden', 'true');
            crownRain.innerHTML = Array.from({ length:14 }, (_, index) => '<i class="fa-solid fa-crown" style="--crown-left:' + ((index * 37 + 9) % 94) + '%;--crown-delay:' + ((index * 113) % 900) + 'ms;--crown-duration:' + (1750 + ((index * 97) % 850)) + 'ms;--crown-size:' + (0.62 + ((index % 4) * 0.12)).toFixed(2) + 'rem"></i>').join('');
            winner.prepend(crownRain);
        }
        await transferAnimation;
        winner?.classList.remove('is-tallying-winner');
        showcaseDismissEnabled = true;
        const dismissButton = showcase.querySelector('[data-winner-dismiss]');
        if (dismissButton) dismissButton.hidden = false;
    }
    function load() { if (!battleId) { window.location.replace('/CaseOpening'); return Promise.resolve(); } return request('/api/case-battles/' + encodeURIComponent(battleId) + '/detail', 'GET').then(render).catch(() => { status.textContent = 'Unable to restore this battle.'; }); }
    actions.addEventListener('click', event => { const button = event.target.closest('[data-battle-action]'); if (!button || !battleId) return; const action = button.dataset.battleAction; button.disabled = true; request('/api/case-battles/' + encodeURIComponent(battleId) + '/' + action, action === 'ready' ? 'PUT' : 'POST', action === 'ready' ? !currentReady : undefined).then(() => { if (action === 'cancel' || action === 'leave') { personalToolsToast?.success(action === 'cancel' ? 'Invitation cancelled and cases returned.' : 'You left the battle.'); window.location.assign('/CaseOpening'); return; } return load(); }).catch(message => { button.disabled = false; personalToolsToast?.error(message || 'The battle could not be updated.'); }); });
    function dismissWinnerReveal() { if (!showcaseDismissEnabled) return; winnerBackdrop?.classList.add('d-none'); showcase?.classList.add('d-none'); resultsSection?.scrollIntoView({ behavior:window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block:'start' }); }
    showcase?.addEventListener('click', event => { if (event.target.closest('[data-winner-dismiss]')) dismissWinnerReveal(); });
    winnerBackdrop?.addEventListener('click', dismissWinnerReveal);
    document.querySelector('#caseBattleAdminReplay')?.addEventListener('click', () => {
        if (replaying || !latestDetail?.pulls?.length) { personalToolsToast?.info('A completed battle is needed before its animation can be replayed.'); return; }
        winnerBackdrop?.classList.add('d-none');
        showcase?.classList.add('d-none');
        showcase?.replaceChildren();
        showcaseShown = false;
        showcaseDismissEnabled = false;
        shownRounds.clear();
        track.querySelectorAll('span').forEach(item => item.classList.remove('is-complete'));
        slideTrackTo(0);
        renderScoreboard(latestDetail, true);
        renderResultRails(latestDetail);
        arena.replaceChildren();
        replayAvailable(latestDetail);
    });
    let lastReactionSentAt = 0;
    document.querySelector('#caseBattleReactionButton')?.nextElementSibling?.addEventListener('click', event => { const button = event.target.closest('[data-battle-reaction]'); if (!button || !realtimeConnection || realtimeConnection.state !== window.signalR?.HubConnectionState.Connected) return; const now = performance.now(); if (now - lastReactionSentAt < 275) return; lastReactionSentAt = now; realtimeConnection.invoke('SendReaction', battleId, button.dataset.battleReaction).catch(() => personalToolsToast?.error('Reaction could not be sent.')); });
    function connectRealtime() { if (!battleId || !window.signalR) return; realtimeConnection = new window.signalR.HubConnectionBuilder().withUrl('/hubs/case-battles').withAutomaticReconnect([0, 1000, 3000, 8000]).build(); realtimeConnection.on('BattleChanged', () => { window.clearTimeout(refreshTimer); refreshTimer = window.setTimeout(load, 80); }); realtimeConnection.on('CaseBattleReaction', reaction => showReaction(reaction?.emoji)); realtimeConnection.onreconnected(() => realtimeConnection.invoke('JoinBattle', battleId).then(load).catch(() => {})); realtimeConnection.start().then(() => realtimeConnection.invoke('JoinBattle', battleId)).then(load).catch(() => { status.dataset.connection = 'offline'; }); }
    // Keep the Tycoon transition over the destination while the room's three data sources are
    // prepared. The first visible frame therefore has the case art, pacing settings and battle
    // state together instead of popping each in after navigation.
    Promise.all([
        fetch('/api/case-battles/timings', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : null),
        fetch('/api/case-opening/cases', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : [])
    ]).then(([settings, items]) => {
        if (settings) timings = { ...timings, ...settings };
        (items || []).forEach(item => caseImages.set(String(item.caseKey || '').toLowerCase(), item.imageUrl || ''));
        return load();
    }).then(() => { window.caseBattleTransition?.complete(); connectRealtime(); window.setTimeout(() => { if (arena.children.length) arena.scrollIntoView({ behavior:window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block:'center' }); }, 240); }).catch(() => { window.caseBattleTransition?.complete(); load().then(connectRealtime); });
})();

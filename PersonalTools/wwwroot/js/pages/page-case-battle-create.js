/* global personalToolsToast, personalToolsLoader */
(() => {
    'use strict';
    const catalogue = document.querySelector('#caseBattleCreateCases');
    const selectedHost = document.querySelector('#caseBattleSelectedCases');
    const summary = document.querySelector('#caseBattleCreateSummary');
    const create = document.querySelector('#caseBattleCreate');
    const buy = document.querySelector('#caseBattleBuyAll');
    const opponent = document.querySelector('#caseBattleOpponent');
    const opponentCards = document.querySelector('#caseBattleOpponentCards');
    const opponentStatus = document.querySelector('#caseBattleOpponentStatus');
    const opponentHelp = document.querySelector('#caseBattleOpponentHelp');
    const modeInput = document.querySelector('#caseBattleMode');
    const modeButtons = [...document.querySelectorAll('[data-battle-mode]')];
    const contributionCopy = document.querySelector('#caseBattleContributionCopy');
    const search = document.querySelector('#caseBattleCaseSearch');
    const searchEmpty = document.querySelector('#caseBattleSearchEmpty');
    const selectionCount = document.querySelector('#caseBattleSelectionCount');
    const gridView = document.querySelector('#caseBattleGridView');
    const listView = document.querySelector('#caseBattleListView');
    const pendingHost = document.querySelector('#caseBattlePendingCreated');
    const pendingList = document.querySelector('#caseBattlePendingCreatedList');
    const incomingHost = document.querySelector('#caseBattleIncomingInvitations');
    const incomingList = document.querySelector('#caseBattleIncomingInvitationList');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    let cases = [];
    let selected = [];
    let buying = false;
    let query = '';
    let view = localStorage.getItem('case-battle-catalogue-view') === 'list' ? 'list' : 'grid';
    let lastCount = -1;
    let selectedOpponentIds = [];
    let opponentUnlockedCases = new Map();
    let opponents = [];
    let battleBot = null;
    let opponentRequestVersion = 0;
    let maxCases = 20;
    const escape = value => String(value || '').replace(/[&<>"']/g, character => ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;', "'":'&#39;' })[character]);
    const byKey = key => cases.find(item => item.caseKey === key);
    const requirements = () => selected.reduce((result, key) => { result[key] = (result[key] || 0) + 1; return result; }, {});
    const missingKeys = () => Object.entries(requirements()).flatMap(([key, needed]) => Array(Math.max(0, needed - Number(byKey(key)?.ownedQuantity || 0))).fill(key));
    const request = (url, method, body) => fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type':'application/json', 'RequestVerificationToken':token }, body: body ? JSON.stringify(body) : undefined })
        .then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const remaining = value => Math.max(0, Math.ceil((new Date(value).getTime() - Date.now()) / 1000));
    const initials = value => String(value || '?').trim().split(/\s+/).slice(0, 2).map(part => part.charAt(0)).join('').toUpperCase() || '?';
    const mode = () => modeInput?.value || 'duel';
    const playerCount = () => ({ duel:2, 'ffa-3':3, 'ffa-4':4 }[mode()] || 2);
    const requiredOpponentCount = () => playerCount() - 1;
    const botIdsForMode = () => Array.from({ length:requiredOpponentCount() }, (_, index) => 'battle-bot-' + (index + 1));
    const hasRequiredOpponents = () => selectedOpponentIds.length === requiredOpponentCount();
    const sharedUnlockedCases = () => {
        if (!hasRequiredOpponents()) return null;
        if (selectedOpponentIds.length && selectedOpponentIds.every(id => id.startsWith('battle-bot'))) return new Set(cases.filter(item => item.isUnlocked).map(item => String(item.caseKey).toLowerCase()));
        const sets = selectedOpponentIds.map(id => opponentUnlockedCases.get(id));
        if (sets.some(value => !value)) return null;
        return new Set([...sets[0]].filter(key => sets.every(value => value.has(key))));
    };
    const selectedMarkup = (key, index, occurrence) => '<div class="case-battle-selected-item" data-selected-index="' + index + '" data-selection-token="' + escape(key) + ':' + occurrence + '"><span class="case-battle-selected-name"><b>' + (index + 1) + '</b><span title="' + escape(byKey(key)?.name || key) + '">' + escape(byKey(key)?.name || key) + '</span></span><span class="case-battle-selected-actions"><button type="button" data-move="up" data-index="' + index + '" aria-label="Move case up"' + (index === 0 ? ' disabled' : '') + '><i class="fa-solid fa-arrow-up"></i></button><button type="button" data-move="down" data-index="' + index + '" aria-label="Move case down"' + (index === selected.length - 1 ? ' disabled' : '') + '><i class="fa-solid fa-arrow-down"></i></button><button type="button" data-remove="' + index + '" aria-label="Remove case"><i class="fa-solid fa-xmark"></i></button></span></div>';
    function captureSelectionPositions() { return new Map([...selectedHost.querySelectorAll('[data-selection-token]')].map(item => [item.dataset.selectionToken, item.getBoundingClientRect()])); }
    function animateSelectionLayout(before) { if (!before.size || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return; window.requestAnimationFrame(() => selectedHost.querySelectorAll('[data-selection-token]').forEach(item => { const from = before.get(item.dataset.selectionToken); if (!from) return; const to = item.getBoundingClientRect(), delta = from.top - to.top; if (!delta) return; item.animate([{ transform:'translateY(' + delta + 'px)' }, { transform:'translateY(0)' }], { duration:280, easing:'cubic-bezier(.2,.86,.2,1)' }); })); }
    function flyCaseToSelection(source, sourceRect, key, occurrence) { if (!source || !sourceRect || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return; const image = source.querySelector('img'), target = selectedHost.querySelector('[data-selection-token="' + CSS.escape(key + ':' + occurrence) + '"]'); if (!image || !target) return; const from = sourceRect, to = target.getBoundingClientRect(), ghost = image.cloneNode(true); ghost.className = 'case-battle-selection-flight'; Object.assign(ghost.style, { left:from.left + 'px', top:from.top + 'px', width:from.width + 'px', height:from.height + 'px' }); document.body.append(ghost); ghost.animate([{ transform:'translate3d(0,0,0) scale(1)', opacity:1 }, { transform:'translate3d(' + (to.left - from.left) + 'px,' + (to.top - from.top) + 'px,0) scale(.45)', opacity:.25 }], { duration:420, easing:'cubic-bezier(.18,.85,.22,1)' }).finished.finally(() => ghost.remove()); }

    function render() {
        const required = requirements();
        const sharedCases = sharedUnlockedCases();
        const hasOpponent = hasRequiredOpponents() && sharedCases !== null;
        const availableCases = hasOpponent ? cases.filter(item => item.isUnlocked && sharedCases.has(String(item.caseKey).toLowerCase())) : [];
        const visibleCases = availableCases.filter(item => !query || `${item.name || ''} ${item.caseKey || ''}`.toLowerCase().includes(query));
        catalogue.classList.toggle('is-grid', view === 'grid'); catalogue.classList.toggle('is-list', view === 'list');
        gridView?.classList.toggle('active', view === 'grid'); gridView?.setAttribute('aria-pressed', String(view === 'grid'));
        listView?.classList.toggle('active', view === 'list'); listView?.setAttribute('aria-pressed', String(view === 'list'));
        catalogue.innerHTML = visibleCases.map(item => {
            const count = required[item.caseKey] || 0;
            return '<article class="case-battle-catalogue-item' + (count ? ' is-selected' : '') + '"><img src="' + escape(item.imageUrl) + '" alt="" loading="lazy"><span><strong title="' + escape(item.name) + '">' + escape(item.name) + '</strong><small>Owned ' + Number(item.ownedQuantity || 0) + '</small></span><span class="case-battle-catalogue-actions"><button type="button" data-add-case="' + escape(item.caseKey) + '" aria-label="Add ' + escape(item.name) + '"' + (selected.length >= maxCases ? ' disabled' : '') + '><i class="fa-solid fa-plus" aria-hidden="true"></i><span>Add</span></button><b aria-label="' + count + ' selected">' + count + '</b><button type="button" data-remove-case="' + escape(item.caseKey) + '" aria-label="Remove one ' + escape(item.name) + '"' + (!count ? ' disabled' : '') + '><i class="fa-solid fa-minus" aria-hidden="true"></i><span>Remove</span></button></span></article>';
        }).join('');
        if (searchEmpty) { searchEmpty.classList.toggle('d-none', visibleCases.length > 0); searchEmpty.textContent = hasOpponent ? (query ? 'No eligible cases match that search.' : 'The selected players do not have any cases unlocked in common with you.') : 'Choose ' + requiredOpponentCount() + ' opponent' + (requiredOpponentCount() === 1 ? '' : 's') + ' to reveal shared cases.'; }
        const previousPositions = captureSelectionPositions(), occurrences = {};
        selectedHost.innerHTML = selected.length ? selected.map((key, index) => { occurrences[key] = (occurrences[key] || 0) + 1; return selectedMarkup(key, index, occurrences[key]); }).join('') : '<p class="case-battle-selected-empty"><i class="fa-solid fa-layer-group" aria-hidden="true"></i><span>Your ordered case track will appear here.</span></p>';
        animateSelectionLayout(previousPositions);
        if (selectionCount) { selectionCount.querySelector('b').textContent = selected.length; selectionCount.querySelector('small').textContent = '/ ' + maxCases + ' selected'; if (lastCount >= 0 && lastCount !== selected.length) { selectionCount.classList.remove('is-bumping'); void selectionCount.offsetWidth; selectionCount.classList.add('is-bumping'); } lastCount = selected.length; }
        const missing = missingKeys();
        summary.textContent = selected.length ? selected.length + ' round' + (selected.length === 1 ? '' : 's') + ' · ' + (missing.length ? missing.length + ' case' + (missing.length === 1 ? '' : 's') + ' missing' : 'all cases owned') : 'Choose at least one case.';
        create.disabled = !selected.length || missing.length > 0 || !hasRequiredOpponents() || sharedCases === null;
        buy.disabled = buying || missing.length === 0;
        create.classList.toggle('is-ready-attention', selected.length === maxCases && !create.disabled);
        renderOpponents();
    }

    function renderOpponents() {
        if (!opponentCards) return;
        const selectedIds = new Set(selectedOpponentIds);
        const botNames = ['Alpha', 'Bravo', 'Charlie'];
        const botEntries = !battleBot?.enabled ? [] : botIdsForMode().map((userId, index) => ({ userId, displayName:requiredOpponentCount() === 1 ? 'Battle Bot' : 'Battle Bot ' + botNames[index], isBot:true }));
        const entries = [
            ...botEntries,
            ...opponents
        ];
        opponentCards.innerHTML = entries.length ? entries.map((user, index) => {
            const id = String(user.userId), active = selectedIds.has(id);
            const shared = active ? opponentUnlockedCases.get(id)?.size : null;
            const attempts = Number(battleBot?.battlesAttempted || 0);
            const wins = Number(battleBot?.battlesWon || 0);
            const detail = user.isBot
                ? (attempts ? wins + ' wins · ' + attempts + ' battles · auto-ready' : 'Automated opponent · auto-ready')
                : (shared == null ? 'Select to check shared cases' : shared + ' shared case' + (shared === 1 ? '' : 's'));
            const full = !active && selectedOpponentIds.length >= requiredOpponentCount();
            return '<button class="case-battle-opponent-card' + (active ? ' is-selected' : '') + '" type="button" role="option" aria-selected="' + String(active) + '" data-opponent-id="' + escape(user.userId) + '"' + (full ? ' disabled' : '') + '><span class="case-battle-opponent-avatar tone-' + (index % 4) + '">' + (user.isBot ? '<i class="fa-solid fa-robot" aria-hidden="true"></i>' : escape(initials(user.displayName))) + '</span><span class="case-battle-opponent-copy"><small>' + (user.isBot ? 'SYSTEM CHALLENGER' : 'PLAYER CHALLENGER') + '</small><strong>' + escape(user.displayName) + '</strong><span><i class="fa-solid ' + (user.isBot ? 'fa-bolt' : 'fa-layer-group') + '" aria-hidden="true"></i> ' + escape(detail) + '</span></span><span class="case-battle-opponent-select"><i class="fa-solid ' + (active ? 'fa-circle-check' : 'fa-crosshairs') + '" aria-hidden="true"></i><small>' + (active ? 'Selected' : 'Challenge') + '</small></span></button>';
        }).join('') : '<div class="case-battle-opponent-loading"><i class="fa-solid fa-user-slash" aria-hidden="true"></i><span>No challengers are available right now.</span></div>';
        const count = selectedOpponentIds.length, required = requiredOpponentCount();
        if (opponentStatus) opponentStatus.innerHTML = count === required
            ? '<i class="fa-solid fa-circle-check" aria-hidden="true"></i> ' + count + ' of ' + required + ' selected'
            : '<i class="fa-solid fa-user-clock" aria-hidden="true"></i> ' + count + ' of ' + required + ' selected';
        opponentStatus?.classList.toggle('is-complete', count === required);
    }

    catalogue.addEventListener('click', event => {
        const add = event.target.closest('[data-add-case]');
        const remove = event.target.closest('[data-remove-case]');
        const source = add?.closest('.case-battle-catalogue-item'), sourceRect = source?.querySelector('img')?.getBoundingClientRect(); let addedOccurrence = 0;
        if (add && selected.length < maxCases) { selected.push(add.dataset.addCase); addedOccurrence = selected.filter(key => key === add.dataset.addCase).length; }
        if (remove) { const index = selected.lastIndexOf(remove.dataset.removeCase); if (index >= 0) selected.splice(index, 1); }
        if (!add && !remove) return;
        render();
        source?.classList.add('is-adding'); flyCaseToSelection(source, sourceRect, add?.dataset.addCase, addedOccurrence);
    });
    selectedHost.addEventListener('click', async event => {
        const remove = event.target.closest('[data-remove]');
        const move = event.target.closest('[data-move]');
        if (remove) {
            remove.closest('.case-battle-selected-item')?.classList.add('is-removing');
            await new Promise(resolve => window.setTimeout(resolve, window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 170));
            selected.splice(Number(remove.dataset.remove), 1);
        }
        if (move) {
            const index = Number(move.dataset.index);
            const target = move.dataset.move === 'up' ? index - 1 : index + 1;
            if (target >= 0 && target < selected.length) [selected[index], selected[target]] = [selected[target], selected[index]];
        }
        render();
    });
    search?.addEventListener('input', () => { query = search.value.trim().toLowerCase(); render(); });
    function setView(next) { view = next; localStorage.setItem('case-battle-catalogue-view', view); render(); }
    gridView?.addEventListener('click', () => setView('grid'));
    listView?.addEventListener('click', () => setView('list'));
    opponentCards?.addEventListener('click', async event => {
        const card = event.target.closest('[data-opponent-id]');
        if (!card || opponent.disabled) return;
        const id = card.dataset.opponentId;
        if (id.startsWith('battle-bot')) {
            const botIds = botIdsForMode();
            selectedOpponentIds = selectedOpponentIds.some(value => value.startsWith('battle-bot')) ? [] : botIds;
        } else {
            selectedOpponentIds = selectedOpponentIds.filter(value => !value.startsWith('battle-bot'));
            if (selectedOpponentIds.includes(id)) selectedOpponentIds = selectedOpponentIds.filter(value => value !== id);
            else if (selectedOpponentIds.length < requiredOpponentCount()) selectedOpponentIds.push(id);
        }
        opponent.value = selectedOpponentIds[0] || '';
        await refreshOpponentUnlocks();
    });
    function loadPendingCreated() {
        return fetch('/api/case-battles/invitations/created', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : []).then(items => {
            pendingHost?.classList.toggle('d-none', !items.length);
            if (!pendingList) return;
            return Promise.all((items || []).map(item => fetch('/api/case-battles/' + encodeURIComponent(item.battleId) + '/detail', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : null).catch(() => null))).then(details => {
                pendingList.innerHTML = items.map((item, index) => {
                    const invitations = details[index]?.invitations || [];
                    const invitationRows = invitations.map(invitation => {
                        const expired = invitation.status === 'pending' && remaining(invitation.expiresUtc) === 0;
                        const status = expired ? 'expired' : invitation.status;
                        const retry = status === 'declined' || status === 'expired';
                        return '<span class="case-battle-pending-seat"><span><strong>' + escape(invitation.displayName) + '</strong><small>' + escape(status) + '</small></span>' + (retry ? '<button class="btn btn-sm btn-outline-warning" type="button" data-reinvite-user="' + escape(invitation.invitedUserId) + '" data-battle-id="' + escape(item.battleId) + '">Re-invite</button>' : '') + '</span>';
                    }).join('');
                    return '<article data-pending-battle="' + escape(item.battleId) + '"><span><strong>' + escape(item.opponentDisplayName) + '</strong><small>' + item.caseCount + ' round' + (item.caseCount === 1 ? '' : 's') + ' · <b data-pending-expiry="' + escape(item.expiresUtc) + '">' + remaining(item.expiresUtc) + 's left</b></small><span class="case-battle-pending-seats">' + invitationRows + '</span></span><span><a class="btn btn-sm btn-outline-secondary" data-case-battle-transition-link href="/CaseOpening/Battles/Lobby/' + encodeURIComponent(item.battleId) + '">Open</a><button class="btn btn-sm btn-outline-danger" type="button" data-cancel-pending="' + escape(item.battleId) + '">Cancel</button></span></article>';
                }).join('');
            });
        }).catch(() => {});
    }
    function loadIncomingInvitations() {
        return fetch('/api/case-battles/invitations/pending', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : []).then(items => {
            incomingHost?.classList.toggle('d-none', !items.length);
            if (!incomingList) return;
            incomingList.innerHTML = (items || []).map(item => '<article data-incoming-battle="' + escape(item.battleId) + '"><span><strong>' + escape(item.creatorDisplayName) + ' challenged you</strong><small>' + (item.caseKeys || []).length + ' round' + ((item.caseKeys || []).length === 1 ? '' : 's') + ' · <b data-incoming-expiry="' + escape(item.expiresUtc) + '">' + remaining(item.expiresUtc) + 's left</b></small></span><span><button class="btn btn-sm btn-outline-secondary" type="button" data-incoming-decline="' + escape(item.battleId) + '">Decline</button><a class="btn btn-sm btn-warning" data-case-battle-transition-link href="/CaseOpening/Battles/Lobby/' + encodeURIComponent(item.battleId) + '"><i class="fa-solid fa-door-open me-1" aria-hidden="true"></i>Open lobby</a></span></article>').join('');
        }).catch(() => {});
    }
    incomingList?.addEventListener('click', event => {
        const declineButton = event.target.closest('[data-incoming-decline]');
        const battleId = declineButton?.dataset.incomingDecline;
        if (!battleId) return;
        const row = event.target.closest('[data-incoming-battle]');
        row?.querySelectorAll('button').forEach(button => { button.disabled = true; });
        request('/api/case-battles/' + encodeURIComponent(battleId) + '/invite/decline', 'POST').then(() => { row?.remove(); if (!incomingList.children.length) incomingHost?.classList.add('d-none'); personalToolsToast?.info('Battle invitation declined.'); loadPendingCreated(); }).catch(message => { personalToolsToast?.error(message || 'The invitation could not be declined.'); loadIncomingInvitations(); });
    });
    pendingList?.addEventListener('click', event => {
        const cancelButton = event.target.closest('[data-cancel-pending]');
        const reinviteButton = event.target.closest('[data-reinvite-user]');
        if (cancelButton) {
            cancelButton.disabled = true;
            request('/api/case-battles/' + encodeURIComponent(cancelButton.dataset.cancelPending) + '/cancel', 'POST').then(() => { personalToolsToast?.success('Battle cancelled and cases returned.'); loadPendingCreated(); }).catch(message => { cancelButton.disabled = false; personalToolsToast?.error(message || 'The battle could not be cancelled.'); });
        }
        if (reinviteButton) {
            reinviteButton.disabled = true;
            request('/api/case-battles/' + encodeURIComponent(reinviteButton.dataset.battleId) + '/invitations', 'POST', { invitedUserIds:[reinviteButton.dataset.reinviteUser] }).then(() => { personalToolsToast?.success('Invitation sent again.'); loadPendingCreated(); }).catch(message => { reinviteButton.disabled = false; personalToolsToast?.error(message || 'The invitation could not be sent again.'); });
        }
    });
    window.setInterval(() => { document.querySelectorAll('[data-pending-expiry]').forEach(node => { node.textContent = remaining(node.dataset.pendingExpiry) + 's left'; }); document.querySelectorAll('[data-incoming-expiry]').forEach(node => { const seconds=remaining(node.dataset.incomingExpiry); node.textContent=seconds+'s left'; if(!seconds){node.closest('[data-incoming-battle]')?.remove();if(!incomingList?.children.length)incomingHost?.classList.add('d-none');} }); }, 1000);
    buy.addEventListener('click', async () => {
        if (buying || missingKeys().length === 0) return;
        buying = true;
        render();
        try {
            await request('/api/case-battles/buy-all', 'POST', { caseKeys: missingKeys() });
            const response = await fetch('/api/case-opening/cases', { credentials:'same-origin', cache:'no-store' });
            if (!response.ok) throw new Error('The case inventory could not be refreshed.');
            const data = await response.json();
            cases = Array.isArray(data) ? data : [];
            personalToolsToast?.success('Required cases purchased.');
        } catch (message) {
            personalToolsToast?.error(message || 'Unable to buy the required cases.');
        } finally {
            buying = false;
            render();
        }
    });
    async function refreshOpponentUnlocks() {
        const version = ++opponentRequestVersion;
        render();
        const humanIds = selectedOpponentIds.filter(id => !id.startsWith('battle-bot'));
        if (!humanIds.length) { render(); return; }
        try {
            const responses = await Promise.all(humanIds.map(id => opponentUnlockedCases.has(id) ? opponentUnlockedCases.get(id) : fetch('/api/case-battles/invitable-users/' + encodeURIComponent(id) + '/unlocked-cases', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject())));
            if (version !== opponentRequestVersion) return;
            humanIds.forEach((id, index) => opponentUnlockedCases.set(id, responses[index] instanceof Set ? responses[index] : new Set((responses[index] || []).map(key => String(key).toLowerCase()))));
            const shared = sharedUnlockedCases();
            const unavailable = shared ? selected.filter(key => !shared.has(String(key).toLowerCase())) : [];
            if (unavailable.length) { selected = selected.filter(key => shared.has(String(key).toLowerCase())); personalToolsToast?.info('Cases not unlocked by every selected player were removed from the battle.'); }
        } catch {
            if (version === opponentRequestVersion) personalToolsToast?.error('An opponent’s unlocked cases could not be loaded.');
        }
        render();
    }
    opponent.addEventListener('change', refreshOpponentUnlocks);
    modeButtons.forEach(button => button.addEventListener('click', () => {
        if (button.disabled || mode() === button.dataset.battleMode) return;
        modeInput.value = button.dataset.battleMode;
        modeButtons.forEach(option => { const active = option === button; option.classList.toggle('active', active); option.setAttribute('aria-pressed', String(active)); });
        selectedOpponentIds = [];
        opponent.value = '';
        selected = [];
        if (opponentHelp) opponentHelp.textContent = requiredOpponentCount() > 1 ? 'Select ' + requiredOpponentCount() + ' rivals. Only cases unlocked by all ' + playerCount() + ' players will be available.' : 'Your shared unlocked cases are checked as soon as you select a player.';
        if (contributionCopy) contributionCopy.textContent = playerCount() > 2 ? 'All ' + playerCount() + ' players contribute one of every case below. The highest combined value wins the entire pot.' : 'Both players contribute one of every case below. Reorder the rounds before sending your invitation.';
        render();
    }));
    create.addEventListener('click', () => {
        const useBot = selectedOpponentIds.length > 0 && selectedOpponentIds.every(id => id.startsWith('battle-bot'));
        request('/api/case-battles', 'POST', { mode:mode(), useBot, ...(useBot ? {} : { invitedUserIds:selectedOpponentIds }), caseKeys:selected })
            .then(battle => { const target = '/CaseOpening/Battles/Lobby/' + encodeURIComponent(battle.battleId); if (window.caseBattleTransition) window.caseBattleTransition.navigate(target); else window.location.assign(target); })
            .catch(message => personalToolsToast?.error(message || 'Unable to create the battle.'));
    });
    personalToolsLoader?.wrap(Promise.all([
        fetch('/api/case-opening/cases', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject()),
        fetch('/api/case-battles/invitable-users', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject()),
        fetch('/api/case-battles/bot-status', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : { enabled:false }),
        fetch('/api/case-battles/timings', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : null)
    ]), { title:'Preparing your battle', message:'Loading cases and available opponents…' })
        .then(([caseData, users, bot, settings]) => { cases = Array.isArray(caseData) ? caseData : []; opponents = Array.isArray(users) ? users : []; battleBot = bot || null; maxCases = Math.max(1, Number(settings?.maxCasesPerBattle || 20)); const botOption = bot?.enabled ? '<option value="battle-bot">Battle Bot · auto-ready</option>' : ''; opponent.innerHTML = '<option value="">Choose an opponent…</option>' + botOption + opponents.map(user => '<option value="' + escape(user.userId) + '">' + escape(user.displayName) + '</option>').join(''); opponent.disabled = !opponents.length && !bot?.enabled; [{ mode:'ffa-3', enabled:bot?.freeForAll3Enabled, label:'1v1v1', players:3 }, { mode:'ffa-4', enabled:bot?.freeForAll4Enabled, label:'1v1v1v1', players:4 }].forEach(option => { const button = modeButtons.find(candidate => candidate.dataset.battleMode === option.mode); if (button) { button.disabled = !option.enabled; button.title = option.enabled ? option.players + '-player winner-takes-all battle' : option.label + ' is disabled by an administrator'; } }); render(); })
        .catch(() => { summary.textContent = 'Cases or available users could not be loaded.'; opponent.innerHTML = '<option value="">No users available</option>'; });
    loadPendingCreated();
    loadIncomingInvitations();
    window.setInterval(() => { if (document.visibilityState === 'visible') loadIncomingInvitations(); }, 5000);

    // A failed response must never strand an escrowed battle behind the create screen. The room
    // owns the safe persisted-resume action and reuses the existing server roll set if present.
    fetch('/api/case-battles/active', { credentials: 'same-origin', cache: 'no-store' })
        .then(response => response.ok ? response.json() : null)
        .then(battle => {
            // Waiting invitations may coexist (up to five). Only an interrupted opening must
            // take over this page because it may already have persisted server-side rolls.
            if (battle?.battleId && battle.status === 'opening')
                window.location.replace('/CaseOpening/Battles/Lobby/' + encodeURIComponent(battle.battleId));
        }).catch(() => { });
})();

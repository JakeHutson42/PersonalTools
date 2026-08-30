/* global personalToolsToast, personalToolsLoader */
(() => {
    'use strict';
    const catalogue = document.querySelector('#caseBattleCreateCases');
    const selectedHost = document.querySelector('#caseBattleSelectedCases');
    const summary = document.querySelector('#caseBattleCreateSummary');
    const create = document.querySelector('#caseBattleCreate');
    const buy = document.querySelector('#caseBattleBuyAll');
    const opponent = document.querySelector('#caseBattleOpponent');
    const search = document.querySelector('#caseBattleCaseSearch');
    const searchEmpty = document.querySelector('#caseBattleSearchEmpty');
    const selectionCount = document.querySelector('#caseBattleSelectionCount');
    const gridView = document.querySelector('#caseBattleGridView');
    const listView = document.querySelector('#caseBattleListView');
    const pendingHost = document.querySelector('#caseBattlePendingCreated');
    const pendingList = document.querySelector('#caseBattlePendingCreatedList');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    let cases = [];
    let selected = [];
    let buying = false;
    let query = '';
    let view = localStorage.getItem('case-battle-catalogue-view') === 'list' ? 'list' : 'grid';
    let lastCount = -1;
    let opponentUnlockedCases = null;
    let opponentRequestVersion = 0;
    let maxCases = 20;
    const escape = value => String(value || '').replace(/[&<>"']/g, character => ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;', "'":'&#39;' })[character]);
    const byKey = key => cases.find(item => item.caseKey === key);
    const requirements = () => selected.reduce((result, key) => { result[key] = (result[key] || 0) + 1; return result; }, {});
    const missingKeys = () => Object.entries(requirements()).flatMap(([key, needed]) => Array(Math.max(0, needed - Number(byKey(key)?.ownedQuantity || 0))).fill(key));
    const request = (url, method, body) => fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type':'application/json', 'RequestVerificationToken':token }, body: body ? JSON.stringify(body) : undefined })
        .then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const remaining = value => Math.max(0, Math.ceil((new Date(value).getTime() - Date.now()) / 1000));
    const selectedMarkup = (key, index, occurrence) => '<div class="case-battle-selected-item" data-selected-index="' + index + '" data-selection-token="' + escape(key) + ':' + occurrence + '"><span class="case-battle-selected-name"><b>' + (index + 1) + '</b><span title="' + escape(byKey(key)?.name || key) + '">' + escape(byKey(key)?.name || key) + '</span></span><span class="case-battle-selected-actions"><button type="button" data-move="up" data-index="' + index + '" aria-label="Move case up"' + (index === 0 ? ' disabled' : '') + '><i class="fa-solid fa-arrow-up"></i></button><button type="button" data-move="down" data-index="' + index + '" aria-label="Move case down"' + (index === selected.length - 1 ? ' disabled' : '') + '><i class="fa-solid fa-arrow-down"></i></button><button type="button" data-remove="' + index + '" aria-label="Remove case"><i class="fa-solid fa-xmark"></i></button></span></div>';
    function captureSelectionPositions() { return new Map([...selectedHost.querySelectorAll('[data-selection-token]')].map(item => [item.dataset.selectionToken, item.getBoundingClientRect()])); }
    function animateSelectionLayout(before) { if (!before.size || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return; window.requestAnimationFrame(() => selectedHost.querySelectorAll('[data-selection-token]').forEach(item => { const from = before.get(item.dataset.selectionToken); if (!from) return; const to = item.getBoundingClientRect(), delta = from.top - to.top; if (!delta) return; item.animate([{ transform:'translateY(' + delta + 'px)' }, { transform:'translateY(0)' }], { duration:280, easing:'cubic-bezier(.2,.86,.2,1)' }); })); }
    function flyCaseToSelection(source, sourceRect, key, occurrence) { if (!source || !sourceRect || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return; const image = source.querySelector('img'), target = selectedHost.querySelector('[data-selection-token="' + CSS.escape(key + ':' + occurrence) + '"]'); if (!image || !target) return; const from = sourceRect, to = target.getBoundingClientRect(), ghost = image.cloneNode(true); ghost.className = 'case-battle-selection-flight'; Object.assign(ghost.style, { left:from.left + 'px', top:from.top + 'px', width:from.width + 'px', height:from.height + 'px' }); document.body.append(ghost); ghost.animate([{ transform:'translate3d(0,0,0) scale(1)', opacity:1 }, { transform:'translate3d(' + (to.left - from.left) + 'px,' + (to.top - from.top) + 'px,0) scale(.45)', opacity:.25 }], { duration:420, easing:'cubic-bezier(.18,.85,.22,1)' }).finished.finally(() => ghost.remove()); }

    function render() {
        const required = requirements();
        const hasOpponent = Boolean(opponent.value);
        const availableCases = hasOpponent ? cases.filter(item => item.isUnlocked && (opponent.value === 'battle-bot' || opponentUnlockedCases?.has(String(item.caseKey).toLowerCase()))) : [];
        const visibleCases = availableCases.filter(item => !query || `${item.name || ''} ${item.caseKey || ''}`.toLowerCase().includes(query));
        catalogue.classList.toggle('is-grid', view === 'grid'); catalogue.classList.toggle('is-list', view === 'list');
        gridView?.classList.toggle('active', view === 'grid'); gridView?.setAttribute('aria-pressed', String(view === 'grid'));
        listView?.classList.toggle('active', view === 'list'); listView?.setAttribute('aria-pressed', String(view === 'list'));
        catalogue.innerHTML = visibleCases.map(item => {
            const count = required[item.caseKey] || 0;
            return '<article class="case-battle-catalogue-item' + (count ? ' is-selected' : '') + '"><img src="' + escape(item.imageUrl) + '" alt="" loading="lazy"><span><strong title="' + escape(item.name) + '">' + escape(item.name) + '</strong><small>Owned ' + Number(item.ownedQuantity || 0) + '</small></span><span class="case-battle-catalogue-actions"><button type="button" data-add-case="' + escape(item.caseKey) + '" aria-label="Add ' + escape(item.name) + '"' + (selected.length >= maxCases ? ' disabled' : '') + '><i class="fa-solid fa-plus" aria-hidden="true"></i><span>Add</span></button><b aria-label="' + count + ' selected">' + count + '</b><button type="button" data-remove-case="' + escape(item.caseKey) + '" aria-label="Remove one ' + escape(item.name) + '"' + (!count ? ' disabled' : '') + '><i class="fa-solid fa-minus" aria-hidden="true"></i><span>Remove</span></button></span></article>';
        }).join('');
        if (searchEmpty) { searchEmpty.classList.toggle('d-none', visibleCases.length > 0); searchEmpty.textContent = hasOpponent ? (query ? 'No eligible cases match that search.' : 'This opponent does not have any cases unlocked in common with you.') : 'Choose an opponent to see cases both players can use.'; }
        const previousPositions = captureSelectionPositions(), occurrences = {};
        selectedHost.innerHTML = selected.length ? selected.map((key, index) => { occurrences[key] = (occurrences[key] || 0) + 1; return selectedMarkup(key, index, occurrences[key]); }).join('') : '<p class="case-battle-selected-empty"><i class="fa-solid fa-layer-group" aria-hidden="true"></i><span>Your ordered case track will appear here.</span></p>';
        animateSelectionLayout(previousPositions);
        if (selectionCount) { selectionCount.querySelector('b').textContent = selected.length; selectionCount.querySelector('small').textContent = '/ ' + maxCases + ' selected'; if (lastCount >= 0 && lastCount !== selected.length) { selectionCount.classList.remove('is-bumping'); void selectionCount.offsetWidth; selectionCount.classList.add('is-bumping'); } lastCount = selected.length; }
        const missing = missingKeys();
        summary.textContent = selected.length ? selected.length + ' round' + (selected.length === 1 ? '' : 's') + ' · ' + (missing.length ? missing.length + ' case' + (missing.length === 1 ? '' : 's') + ' missing' : 'all cases owned') : 'Choose at least one case.';
        create.disabled = !selected.length || missing.length > 0 || !opponent.value;
        buy.disabled = buying || missing.length === 0;
        create.classList.toggle('is-ready-attention', selected.length === maxCases && !create.disabled);
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
    function loadPendingCreated() {
        return fetch('/api/case-battles/invitations/created', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : []).then(items => {
            pendingHost?.classList.toggle('d-none', !items.length);
            if (!pendingList) return;
            pendingList.innerHTML = (items || []).map(item => '<article data-pending-battle="' + escape(item.battleId) + '"><span><strong>' + escape(item.opponentDisplayName) + '</strong><small>' + item.caseCount + ' round' + (item.caseCount === 1 ? '' : 's') + ' · <b data-pending-expiry="' + escape(item.expiresUtc) + '">' + remaining(item.expiresUtc) + 's left</b></small></span><span><a class="btn btn-sm btn-outline-secondary" data-case-battle-transition-link href="/CaseOpening/Battles/Lobby/' + encodeURIComponent(item.battleId) + '">Open</a><button class="btn btn-sm btn-outline-danger" type="button" data-cancel-pending="' + escape(item.battleId) + '">Cancel</button></span></article>').join('');
        }).catch(() => {});
    }
    pendingList?.addEventListener('click', event => { const button = event.target.closest('[data-cancel-pending]'); if (!button) return; button.disabled = true; request('/api/case-battles/' + encodeURIComponent(button.dataset.cancelPending) + '/cancel', 'POST').then(() => { personalToolsToast?.success('Invitation cancelled and cases returned.'); loadPendingCreated(); }).catch(message => { button.disabled = false; personalToolsToast?.error(message || 'The invitation could not be cancelled.'); }); });
    window.setInterval(() => { document.querySelectorAll('[data-pending-expiry]').forEach(node => { node.textContent = remaining(node.dataset.pendingExpiry) + 's left'; }); }, 1000);
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
    opponent.addEventListener('change', async () => {
        const version = ++opponentRequestVersion;
        opponentUnlockedCases = null;
        render();
        if (!opponent.value || opponent.value === 'battle-bot') { opponentUnlockedCases = new Set(cases.filter(item => item.isUnlocked).map(item => String(item.caseKey).toLowerCase())); render(); return; }
        try {
            const response = await fetch('/api/case-battles/invitable-users/' + encodeURIComponent(opponent.value) + '/unlocked-cases', { credentials:'same-origin', cache:'no-store' });
            if (!response.ok) throw new Error();
            const keys = await response.json();
            if (version !== opponentRequestVersion) return;
            opponentUnlockedCases = new Set((keys || []).map(key => String(key).toLowerCase()));
            const unavailable = selected.filter(key => !opponentUnlockedCases.has(String(key).toLowerCase()));
            if (unavailable.length) { selected = selected.filter(key => opponentUnlockedCases.has(String(key).toLowerCase())); personalToolsToast?.info('Cases the selected opponent has not unlocked were removed from the battle.'); }
        } catch {
            if (version === opponentRequestVersion) { opponentUnlockedCases = new Set(); personalToolsToast?.error('That opponent’s unlocked cases could not be loaded.'); }
        }
        render();
    });
    create.addEventListener('click', () => {
        const useBot = opponent.value === 'battle-bot';
        request('/api/case-battles', 'POST', { mode:'duel', useBot, ...(useBot ? {} : { invitedUserId:opponent.value }), caseKeys:selected })
            .then(battle => { const target = '/CaseOpening/Battles/Lobby/' + encodeURIComponent(battle.battleId); if (window.caseBattleTransition) window.caseBattleTransition.navigate(target); else window.location.assign(target); })
            .catch(message => personalToolsToast?.error(message || 'Unable to create the battle.'));
    });
    personalToolsLoader?.wrap(Promise.all([
        fetch('/api/case-opening/cases', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject()),
        fetch('/api/case-battles/invitable-users', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject()),
        fetch('/api/case-battles/bot-status', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : { enabled:false }),
        fetch('/api/case-battles/timings', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : null)
    ]), { title:'Preparing your battle', message:'Loading cases and available opponents…' })
        .then(([caseData, users, bot, settings]) => { cases = Array.isArray(caseData) ? caseData : []; maxCases = Math.max(1, Number(settings?.maxCasesPerBattle || 20)); const botOption = bot?.enabled ? '<option value="battle-bot">Battle Bot · instant match</option>' : ''; opponent.innerHTML = '<option value="">Choose an opponent…</option>' + botOption + (users || []).map(user => '<option value="' + escape(user.userId) + '">' + escape(user.displayName) + '</option>').join(''); opponent.disabled = !users.length && !bot?.enabled; render(); })
        .catch(() => { summary.textContent = 'Cases or available users could not be loaded.'; opponent.innerHTML = '<option value="">No users available</option>'; });
    loadPendingCreated();

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

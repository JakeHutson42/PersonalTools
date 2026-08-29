/* global personalToolsToast, personalToolsLoader */
(() => {
    'use strict';
    const catalogue = document.querySelector('#caseBattleCreateCases');
    const selectedHost = document.querySelector('#caseBattleSelectedCases');
    const summary = document.querySelector('#caseBattleCreateSummary');
    const create = document.querySelector('#caseBattleCreate');
    const buy = document.querySelector('#caseBattleBuyAll');
    const opponent = document.querySelector('#caseBattleOpponent');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    let cases = [];
    let selected = [];
    let buying = false;
    const escape = value => String(value || '').replace(/[&<>"']/g, character => ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;', "'":'&#39;' })[character]);
    const byKey = key => cases.find(item => item.caseKey === key);
    const requirements = () => selected.reduce((result, key) => { result[key] = (result[key] || 0) + 1; return result; }, {});
    const missingKeys = () => Object.entries(requirements()).flatMap(([key, needed]) => Array(Math.max(0, needed - Number(byKey(key)?.ownedQuantity || 0))).fill(key));
    const request = (url, method, body) => fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type':'application/json', 'RequestVerificationToken':token }, body: body ? JSON.stringify(body) : undefined })
        .then(async response => response.ok ? response.json() : Promise.reject((await response.json()).message));
    const selectedMarkup = (key, index) => '<div class="case-battle-selected-item"><span><b>' + (index + 1) + '</b>' + escape(byKey(key)?.name || key) + '</span><span><button type="button" data-move="up" data-index="' + index + '" aria-label="Move case up"><i class="fa-solid fa-arrow-up"></i></button><button type="button" data-move="down" data-index="' + index + '" aria-label="Move case down"><i class="fa-solid fa-arrow-down"></i></button><button type="button" data-remove="' + index + '" aria-label="Remove case"><i class="fa-solid fa-xmark"></i></button></span></div>';

    function render() {
        const required = requirements();
        catalogue.innerHTML = cases.map(item => {
            const count = required[item.caseKey] || 0;
            return '<button class="case-battle-catalogue-item" type="button" data-case-key="' + escape(item.caseKey) + '"><img src="' + escape(item.imageUrl) + '" alt="" loading="lazy"><span><strong>' + escape(item.name) + '</strong><small>Owned ' + Number(item.ownedQuantity || 0) + (count ? ' · Selected ' + count : '') + '</small></span><i class="fa-solid fa-plus" aria-hidden="true"></i></button>';
        }).join('');
        selectedHost.innerHTML = selected.length ? selected.map(selectedMarkup).join('') : '<p class="small-muted mb-0">Your ordered case track will appear here.</p>';
        const missing = missingKeys();
        summary.textContent = selected.length ? selected.length + ' round' + (selected.length === 1 ? '' : 's') + ' · ' + (missing.length ? missing.length + ' case' + (missing.length === 1 ? '' : 's') + ' missing' : 'all cases owned') : 'Choose at least one case.';
        create.disabled = !selected.length || missing.length > 0 || !opponent.value;
        buy.disabled = buying || missing.length === 0;
    }

    catalogue.addEventListener('click', event => {
        const button = event.target.closest('[data-case-key]');
        if (!button || selected.length >= 20) return;
        selected.push(button.dataset.caseKey);
        render();
    });
    selectedHost.addEventListener('click', event => {
        const remove = event.target.closest('[data-remove]');
        const move = event.target.closest('[data-move]');
        if (remove) selected.splice(Number(remove.dataset.remove), 1);
        if (move) {
            const index = Number(move.dataset.index);
            const target = move.dataset.move === 'up' ? index - 1 : index + 1;
            if (target >= 0 && target < selected.length) [selected[index], selected[target]] = [selected[target], selected[index]];
        }
        render();
    });
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
    opponent.addEventListener('change', render);
    create.addEventListener('click', () => {
        const useBot = opponent.value === 'battle-bot';
        request('/api/case-battles', 'POST', { mode:'duel', useBot, ...(useBot ? {} : { invitedUserId:opponent.value }), caseKeys:selected })
            .then(battle => window.location.assign('/CaseOpening/Battles/' + encodeURIComponent(battle.battleId)))
            .catch(message => personalToolsToast?.error(message || 'Unable to create the battle.'));
    });
    personalToolsLoader?.wrap(Promise.all([
        fetch('/api/case-opening/cases', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject()),
        fetch('/api/case-battles/invitable-users', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : Promise.reject()),
        fetch('/api/case-battles/bot-status', { credentials:'same-origin', cache:'no-store' }).then(response => response.ok ? response.json() : { enabled:false })
    ]), { title:'Preparing your battle', message:'Loading cases and available opponents…' })
        .then(([caseData, users, bot]) => { cases = Array.isArray(caseData) ? caseData : []; const botOption = bot?.enabled ? '<option value="battle-bot">Battle Bot · instant match</option>' : ''; opponent.innerHTML = '<option value="">Choose an opponent…</option>' + botOption + (users || []).map(user => '<option value="' + escape(user.userId) + '">' + escape(user.displayName) + '</option>').join(''); opponent.disabled = !users.length && !bot?.enabled; render(); })
        .catch(() => { summary.textContent = 'Cases or available users could not be loaded.'; opponent.innerHTML = '<option value="">No users available</option>'; });

    // A failed response must never strand an escrowed battle behind the create screen. The room
    // owns the safe persisted-resume action and reuses the existing server roll set if present.
    fetch('/api/case-battles/active', { credentials: 'same-origin', cache: 'no-store' })
        .then(response => response.ok ? response.json() : null)
        .then(battle => {
            if (battle?.battleId && (battle.status === 'waiting' || battle.status === 'opening'))
                window.location.replace('/CaseOpening/Battles/' + encodeURIComponent(battle.battleId));
        }).catch(() => { });
})();

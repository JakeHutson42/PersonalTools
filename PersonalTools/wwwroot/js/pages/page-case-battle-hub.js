/* global jQuery, personalToolsLoader, personalToolsToast */
(function ($) {
    'use strict';
    const token = $('input[name="__RequestVerificationToken"]').first().val() || '';
    const $active = $('#caseBattleHubActive'), $incoming = $('#caseBattleHubIncoming'), $outgoing = $('#caseBattleHubOutgoing'), $history = $('#caseBattleHubHistory');
    const escape = value => $('<span>').text(String(value || '')).html();
    const money = value => new Intl.NumberFormat('en-GB', { style:'currency', currency:'GBP', maximumFractionDigits:2 }).format(Number(value || 0));
    const date = value => new Intl.DateTimeFormat('en-GB', { day:'numeric', month:'short', year:'numeric', hour:'2-digit', minute:'2-digit' }).format(new Date(value));
    const mode = value => ({ duel:'1v1', 'ffa-3':'1v1v1', 'ffa-4':'1v1v1v1', 'teams-2v2':'2v2' }[String(value || '').toLowerCase()] || 'Battle');
    const request = (url, method) => $.ajax({ url, method:method || 'GET', timeout:30000, showLoader:false, showToast:false, headers:{ RequestVerificationToken:token } });
    const roomUrl = battle => battle.status === 'waiting' ? `/CaseOpening/Battles/Lobby/${encodeURIComponent(battle.battleId)}` : `/CaseOpening/Battles/${encodeURIComponent(battle.battleId)}`;

    function renderActive(battle) {
        $active.toggleClass('d-none', !battle);
        if (!battle) return;
        $active.html(`<article><span class="case-battle-hub-active-icon"><i class="fa-solid fa-bolt" aria-hidden="true"></i></span><span><small>Active battle</small><strong>${escape(mode(battle.mode))} · ${Number(battle.caseKeys?.length || 0)} rounds</strong><b>${escape(battle.status === 'waiting' ? `${battle.joinedPlayers} of ${battle.requiredPlayers} players joined` : 'Opening in progress')}</b></span><a class="btn btn-warning" data-case-battle-transition-link href="${roomUrl(battle)}">${battle.status === 'waiting' ? 'Open lobby' : 'Resume battle'}</a></article>`);
    }
    function renderIncoming(items) {
        $('#caseBattleHubIncomingCount').text(items.length);
        $incoming.html(items.length ? items.map(item => `<article class="case-battle-hub-row"><span><strong>${escape(item.creatorDisplayName)}</strong><small>${item.caseKeys.length} round${item.caseKeys.length === 1 ? '' : 's'} · invitation expires ${date(item.expiresUtc)}</small></span><span><button class="btn btn-sm btn-outline-secondary" type="button" data-hub-decline="${escape(item.battleId)}">Decline</button><a class="btn btn-sm btn-warning" data-case-battle-transition-link href="/CaseOpening/Battles/Lobby/${encodeURIComponent(item.battleId)}">Review</a></span></article>`).join('') : '<p class="case-battle-empty">No challenges are waiting for you.</p>');
    }
    function renderOutgoing(items) {
        $('#caseBattleHubOutgoingCount').text(items.length);
        $outgoing.html(items.length ? items.map(item => `<article class="case-battle-hub-row"><span><strong>${escape(item.opponentDisplayName)}</strong><small>${item.caseCount} round${item.caseCount === 1 ? '' : 's'} · expires ${date(item.expiresUtc)}</small></span><span><a class="btn btn-sm btn-outline-warning" data-case-battle-transition-link href="/CaseOpening/Battles/Lobby/${encodeURIComponent(item.battleId)}">Open lobby</a><button class="btn btn-sm btn-outline-danger" type="button" data-hub-cancel="${escape(item.battleId)}">Cancel</button></span></article>`).join('') : '<p class="case-battle-empty">You have no unanswered challenges.</p>');
    }
    function renderHistory(items) {
        $history.html(items.length ? items.slice(0, 8).map(item => `<a class="case-battle-hub-result ${item.won ? 'is-win' : 'is-loss'}" data-case-battle-transition-link href="/CaseOpening/Battles/${encodeURIComponent(item.battleId)}"><span class="case-battle-hub-verdict"><i class="fa-solid ${item.won ? 'fa-crown' : 'fa-shield'}" aria-hidden="true"></i><b>${item.won ? 'Victory' : 'Defeat'}</b></span><span><strong>${escape(mode(item.mode))} battle</strong><small>${date(item.settledUtc)}</small></span><span><small>Your pull value</small><strong>${money(item.personalTotal)}</strong></span><span><small>${item.won ? 'Payout' : 'Result'}</small><strong>${item.won ? money(item.awardedValue) : '—'}</strong></span><i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>`).join('') : '<p class="case-battle-empty">Complete your first battle to start a record here.</p>');
    }
    function load() {
        return Promise.all([
            request('/api/case-battles/active'), request('/api/case-battles/invitations/pending'),
            request('/api/case-battles/invitations/created'), request('/api/case-battles/history')
        ]).then(([active, incoming, outgoing, history]) => { renderActive(active); renderIncoming(incoming || []); renderOutgoing(outgoing || []); renderHistory(history || []); });
    }
    $incoming.on('click', '[data-hub-decline]', function () {
        const $button = $(this).prop('disabled', true);
        request(`/api/case-battles/${encodeURIComponent($button.data('hub-decline'))}/invite/decline`, 'POST').then(() => { personalToolsToast?.info('Battle invitation declined.'); return load(); }).catch(response => { $button.prop('disabled', false); personalToolsToast?.error(response?.responseJSON?.message || 'The invitation could not be declined.'); });
    });
    $outgoing.on('click', '[data-hub-cancel]', function () {
        const $button = $(this).prop('disabled', true);
        request(`/api/case-battles/${encodeURIComponent($button.data('hub-cancel'))}/cancel`, 'POST').then(() => { personalToolsToast?.success('Battle cancelled and cases returned.'); return load(); }).catch(response => { $button.prop('disabled', false); personalToolsToast?.error(response?.responseJSON?.message || 'The battle could not be cancelled.'); });
    });
    personalToolsLoader?.wrap(load(), { title:'Opening battle hub', message:'Checking active rooms, invitations and results…' }).catch(() => {
        $incoming.add($outgoing).add($history).html('<p class="case-battle-empty text-danger">Battle activity could not be loaded. Refresh to try again.</p>');
    });
}(jQuery));

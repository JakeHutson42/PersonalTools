$(function () {
    const $rows = $('#caseBattleAdminRows');
    const token = () => $('input[name="__RequestVerificationToken"]').first().val();
    const date = value => value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
    const escape = value => $('<div>').text(value || '').html();
    function renderBot(status) {
        $('#caseBattlesEnabled').prop('checked', status.caseBattlesEnabled).prop('disabled', false);
        $('#caseBattleBotEnabled').prop('checked', status.enabled).prop('disabled', false);
        $('#caseBattleBotStats strong').each(function (index) { $(this).text([status.battlesAttempted, status.battlesWon, status.skinsDiscarded, new Intl.NumberFormat('en-GB', { style:'currency', currency:'GBP' }).format(Number(status.valueDiscarded || 0))][index]); });
    }
    function load() {
        $rows.html('<tr><td colspan="6" class="text-center small-muted py-5">Loading unresolved battles…</td></tr>');
        $.getJSON('/api/admin/case-battles').done(items => {
            if (!items.length) { $rows.html('<tr><td colspan="6" class="text-center small-muted py-5">No expired or unresolved battles need attention.</td></tr>'); return; }
            $rows.empty();
            items.forEach(item => {
                const canExpire = item.attention === 'expired-waiting';
                $rows.append(`<tr><td><code>${escape(item.battleId)}</code><small class="d-block text-muted">Created ${date(item.createdUtc)}</small></td><td><span class="badge text-bg-${item.status === 'opening' ? 'warning' : 'secondary'}">${escape(item.status)}</span><small class="d-block text-muted">${escape(item.mode)}</small></td><td>${escape(item.creatorDisplayName)}</td><td>${item.joinedPlayers} players · ${item.caseCount} cases<small class="d-block text-muted">${item.reservedCaseCount} escrowed · ${item.stagedRollCount} rolls staged</small></td><td><strong>${escape(item.attention)}</strong><small class="d-block text-muted">Expires ${date(item.expiresUtc)}</small></td><td class="text-end">${canExpire ? `<button class="btn btn-sm btn-outline-warning js-expire-battle" data-id="${item.battleId}">Return cases</button>` : '<span class="small-muted">No automatic action</span>'}</td></tr>`);
            });
        }).fail(() => $rows.html('<tr><td colspan="6" class="text-center text-danger py-5">Could not load reconciliation data.</td></tr>'));
        $.getJSON('/api/admin/case-battles/bot-status').done(renderBot);
    }
    $('#caseBattleAdminRefresh').on('click', load);
    $('#caseBattlesEnabled').on('change', function () { const control = $(this).prop('disabled', true); $.ajax({ url:'/api/admin/case-battles/feature-status', method:'PUT', contentType:'application/json', data:JSON.stringify(control.prop('checked')), headers:{ RequestVerificationToken:token() }, success:renderBot }).fail(() => { window.personalToolsToast?.error('Case Battle visibility could not be saved.'); load(); }); });
    $('#caseBattleBotEnabled').on('change', function () { const control = $(this).prop('disabled', true); $.ajax({ url:'/api/admin/case-battles/bot-status', method:'PUT', contentType:'application/json', data:JSON.stringify(control.prop('checked')), headers:{ RequestVerificationToken:token() }, success:renderBot }).fail(() => { window.personalToolsToast?.error('Battle Bot visibility could not be saved.'); load(); }); });
    $rows.on('click', '.js-expire-battle', function () {
        const button = $(this).prop('disabled', true);
        $.ajax({ url: `/api/admin/case-battles/${encodeURIComponent(button.data('id'))}/expire`, method: 'POST', headers: { RequestVerificationToken: token() } }).always(load);
    });
    load();
});

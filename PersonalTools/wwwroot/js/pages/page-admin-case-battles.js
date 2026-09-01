$(function () {
    const $rows = $('#caseBattleAdminRows');
    const token = () => $('input[name="__RequestVerificationToken"]').first().val();
    const date = value => value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
    const escape = value => $('<div>').text(value || '').html();
    const modeLabel = mode => ({ duel: '1v1', 'ffa-3': '1v1v1', 'ffa-4': '1v1v1v1' }[String(mode || '').toLowerCase()] || mode || 'Unknown mode');
    function renderBot(status) {
        $('#caseBattlesEnabled').prop('checked', status.caseBattlesEnabled).prop('disabled', false);
        $('#caseBattleFfa3Enabled').prop('checked', status.freeForAll3Enabled).prop('disabled', false);
        $('#caseBattleFfa4Enabled').prop('checked', status.freeForAll4Enabled).prop('disabled', false);
        $('#caseBattleBotEnabled').prop('checked', status.enabled).prop('disabled', false);
        $('#caseBattleBotStats strong').each(function (index) { $(this).text([status.battlesAttempted, status.battlesWon, status.skinsDiscarded, new Intl.NumberFormat('en-GB', { style:'currency', currency:'GBP' }).format(Number(status.valueDiscarded || 0))][index]); });
    }
    const timingFields = ['maxCasesPerBattle','readyPauseMs','readyCountdownMs','preSpinPauseMs','spinDurationMs','resultsPauseMs','winnerIntroPauseMs','winnerTallyDurationMs','winnerVerdictPauseMs','winnerTransferDurationMs'];
    function renderTimings(settings) { timingFields.forEach(field => $('#' + field).val(settings[field])); }
    function load() {
        $rows.html('<tr><td colspan="6" class="text-center small-muted py-5">Loading unresolved battles…</td></tr>');
        $.getJSON('/api/admin/case-battles').done(items => {
            if (!items.length) { $rows.html('<tr><td colspan="6" class="text-center small-muted py-5">No pending or unresolved battles need attention.</td></tr>'); return; }
            $rows.empty();
            items.forEach(item => {
                const canCancel = item.status === 'waiting';
                $rows.append(`<tr><td><code>${escape(item.battleId)}</code><small class="d-block text-muted">Created ${date(item.createdUtc)}</small></td><td><span class="badge text-bg-${item.status === 'opening' ? 'warning' : 'secondary'}">${escape(item.status)}</span><small class="d-block text-muted">${escape(modeLabel(item.mode))}</small></td><td>${escape(item.creatorDisplayName)}</td><td>${item.joinedPlayers} players · ${item.caseCount} cases<small class="d-block text-muted">${item.reservedCaseCount} escrowed · ${item.stagedRollCount} rolls staged</small></td><td><strong>${escape(item.attention)}</strong><small class="d-block text-muted">Expires ${date(item.expiresUtc)}</small></td><td class="text-end">${canCancel ? `<button class="btn btn-sm btn-outline-danger js-cancel-battle" data-id="${item.battleId}">Cancel & return cases</button>` : '<span class="small-muted">No automatic action</span>'}</td></tr>`);
            });
        }).fail(() => $rows.html('<tr><td colspan="6" class="text-center text-danger py-5">Could not load reconciliation data.</td></tr>'));
        $.getJSON('/api/admin/case-battles/bot-status').done(renderBot);
        $.getJSON('/api/admin/case-battles/timings').done(renderTimings);
    }
    $('#caseBattleAdminRefresh').on('click', load);
    $('#caseBattlesEnabled').on('change', function () { const control = $(this).prop('disabled', true); $.ajax({ url:'/api/admin/case-battles/feature-status', method:'PUT', contentType:'application/json', data:JSON.stringify(control.prop('checked')), headers:{ RequestVerificationToken:token() }, success:renderBot }).fail(() => { window.personalToolsToast?.error('Case Battle visibility could not be saved.'); load(); }); });
    $('#caseBattleFfa3Enabled').on('change', function () { const control = $(this).prop('disabled', true); $.ajax({ url:'/api/admin/case-battles/ffa-3-status', method:'PUT', contentType:'application/json', data:JSON.stringify(control.prop('checked')), headers:{ RequestVerificationToken:token() }, success:renderBot }).fail(() => { window.personalToolsToast?.error('1v1v1 visibility could not be saved.'); load(); }); });
    $('#caseBattleFfa4Enabled').on('change', function () { const control = $(this).prop('disabled', true); $.ajax({ url:'/api/admin/case-battles/ffa-4-status', method:'PUT', contentType:'application/json', data:JSON.stringify(control.prop('checked')), headers:{ RequestVerificationToken:token() }, success:renderBot }).fail(() => { window.personalToolsToast?.error('1v1v1v1 visibility could not be saved.'); load(); }); });
    $('#caseBattleBotEnabled').on('change', function () { const control = $(this).prop('disabled', true); $.ajax({ url:'/api/admin/case-battles/bot-status', method:'PUT', contentType:'application/json', data:JSON.stringify(control.prop('checked')), headers:{ RequestVerificationToken:token() }, success:renderBot }).fail(() => { window.personalToolsToast?.error('Battle Bot visibility could not be saved.'); load(); }); });
    $('#caseBattleTimingsSave').on('click', function () {
        const button = $(this).prop('disabled', true), payload = Object.fromEntries(timingFields.map(field => [field, Number($('#' + field).val())]));
        $.ajax({ url:'/api/admin/case-battles/timings', method:'PUT', contentType:'application/json', data:JSON.stringify(payload), headers:{ RequestVerificationToken:token() }, success:settings => { renderTimings(settings); window.personalToolsToast?.success('Battle timings saved.'); } })
            .fail(xhr => window.personalToolsToast?.error(xhr.responseJSON?.message || 'Battle timings could not be saved.'))
            .always(() => button.prop('disabled', false));
    });
    $rows.on('click', '.js-expire-battle', function () {
        const button = $(this).prop('disabled', true);
        $.ajax({ url: `/api/admin/case-battles/${encodeURIComponent(button.data('id'))}/expire`, method: 'POST', headers: { RequestVerificationToken: token() } }).always(load);
    });
    $rows.on('click', '.js-cancel-battle', function () {
        const button = $(this).prop('disabled', true);
        $.ajax({ url: `/api/admin/case-battles/${encodeURIComponent(button.data('id'))}/cancel`, method: 'POST', headers: { RequestVerificationToken: token() } })
            .done(() => window.personalToolsToast?.success('Pending battle cancelled and cases returned.'))
            .fail(xhr => window.personalToolsToast?.error(xhr.responseJSON?.message || 'The pending battle could not be cancelled.'))
            .always(load);
    });
    load();
});

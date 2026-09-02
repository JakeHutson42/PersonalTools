$(function () {
    const cookieRows = [
        { name: 'PersonalTools.Auth', purpose: 'Encrypted authentication cookie linked to a revocable server-side session.', duration: 'Browser session normally; up to 14 days with Remember me, or up to 180 days for a Case Tycoon guest.' },
        { name: '.AspNetCore.Antiforgery…', purpose: 'Protects forms and account actions against cross-site request forgery.', duration: 'Browser session.' },
        { name: 'PersonalTools.CookieConsent', purpose: 'Records your storage choice so the banner does not reappear on every page.', duration: '6 months.' },
        { name: 'CaseTycoon.FanNotice', purpose: 'Remembers that you dismissed the one-time fan-project notice.', duration: '1 year.' },
        { name: 'PersonalTools.SteamLinkState', purpose: 'One-time security value used only while you actively link a Steam account.', duration: 'Up to 10 minutes, then deleted.' },
        { name: 'personal_tools_pending_toast', purpose: 'Short fallback used to carry a requested confirmation message across navigation.', duration: 'Up to 30 seconds.' }
    ];

    const body = $('#cookieTableRows');
    cookieRows.forEach(row => $('<tr>').append($('<td>').append($('<code>').text(row.name)), $('<td>').text(row.purpose), $('<td>').text(row.duration)).appendTo(body));
    $('#openCookiePreferences').on('click', () => window.personalToolsConsent?.open());
});

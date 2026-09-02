(() => {
    'use strict';
    const cookieName = 'CaseTycoon.FanNotice';
    const dismissed = document.cookie.split(';').some(value => value.trim().startsWith(`${cookieName}=`));
    const element = document.getElementById('caseFanNotice');
    if (dismissed || !element || !window.bootstrap) return;
    window.caseFanNoticeActive = true;
    const modal = new bootstrap.Modal(element, { backdrop: 'static', keyboard: true, focus: true });
    modal.show();
    document.getElementById('dismissCaseFanNotice')?.addEventListener('click', () => {
        const secure = location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = `${cookieName}=dismissed; Path=/; Max-Age=31536000; SameSite=Lax${secure}`;
        window.caseFanNoticeActive = false;
        modal.hide();
        window.dispatchEvent(new Event('casetycoon:fan-notice-dismissed'));
    });
})();

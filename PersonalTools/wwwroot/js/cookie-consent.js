(() => {
    'use strict';

    const cookieName = 'PersonalTools.CookieConsent';
    const validChoices = new Set(['all', 'necessary', 'denied']);
    const readChoice = () => {
        const prefix = `${cookieName}=`;
        const raw = document.cookie.split(';').map(value => value.trim()).find(value => value.startsWith(prefix));
        const choice = raw ? decodeURIComponent(raw.slice(prefix.length)) : '';
        return validChoices.has(choice) ? choice : null;
    };
    const writeChoice = choice => {
        const secure = location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = `${cookieName}=${encodeURIComponent(choice)}; Path=/; Max-Age=15552000; SameSite=Lax${secure}`;
    };

    let choice = readChoice();
    const nativeGetItem = Storage.prototype.getItem;
    const nativeSetItem = Storage.prototype.setItem;
    const nativeRemoveItem = Storage.prototype.removeItem;
    const nativeClear = Storage.prototype.clear;
    const isLocal = storage => storage === window.localStorage;
    const preferencesAllowed = () => choice === 'all';

    Storage.prototype.getItem = function (key) {
        return isLocal(this) && !preferencesAllowed() ? null : nativeGetItem.call(this, key);
    };
    Storage.prototype.setItem = function (key, value) {
        if (isLocal(this) && !preferencesAllowed()) return;
        nativeSetItem.call(this, key, value);
    };
    Storage.prototype.removeItem = function (key) {
        if (isLocal(this) && !preferencesAllowed()) return;
        nativeRemoveItem.call(this, key);
    };

    window.personalToolsConsent = {
        get choice() { return choice; },
        get preferencesAllowed() { return preferencesAllowed(); },
        open() {
            const banner = document.getElementById('cookieConsent');
            if (banner) banner.hidden = false;
        },
    };

    document.addEventListener('DOMContentLoaded', () => {
        const banner = document.getElementById('cookieConsent');
        if (!banner) return;
        document.querySelectorAll('[data-open-cookie-preferences]').forEach(button => button.addEventListener('click', () => { banner.hidden = false; }));
        if (!choice) banner.hidden = false;
        banner.querySelectorAll('[data-cookie-choice]').forEach(button => button.addEventListener('click', () => {
            const nextChoice = button.dataset.cookieChoice;
            if (!validChoices.has(nextChoice)) return;
            if (nextChoice !== 'all') nativeClear.call(window.localStorage);
            writeChoice(nextChoice);
            choice = nextChoice;
            banner.hidden = true;
            window.dispatchEvent(new CustomEvent('personaltools:consent-changed', { detail: { choice } }));
            if (nextChoice === 'all') window.location.reload();
        }));
    });
})();

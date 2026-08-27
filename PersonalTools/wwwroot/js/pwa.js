(function () {
    'use strict';

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    }

    function isMobileInstallSurface() {
        if (typeof navigator.userAgentData?.mobile === 'boolean') {
            return navigator.userAgentData.mobile;
        }

        return /android|iphone|ipad|ipod|mobile/i.test(navigator.userAgent);
    }

    document.documentElement.classList.toggle('is-standalone-app', isStandalone());
    if (isStandalone() && window.location.pathname === '/' && document.body?.dataset.userId) {
        window.location.replace('/CaseOpening');
        return;
    }
    if (!('serviceWorker' in navigator)) return;

    const prompt = document.getElementById('pwaPrompt');
    const title = document.getElementById('pwaPromptTitle');
    const message = document.getElementById('pwaPromptMessage');
    const action = document.getElementById('pwaPromptAction');
    const dismiss = document.getElementById('pwaPromptDismiss');
    const dismissStorageKey = 'personalTools.pwaInstallDismissedUntil';
    const dismissDurationMs = 14 * 24 * 60 * 60 * 1000;
    let installEvent = null;
    let waitingWorker = null;
    let promptMode = 'install';
    let reloadingForUpdate = false;
    let hideTimer = null;

    function installPromptDismissed() {
        try {
            return Number(localStorage.getItem(dismissStorageKey) || 0) > Date.now();
        } catch {
            return false;
        }
    }

    function rememberInstallDismissal() {
        try {
            localStorage.setItem(dismissStorageKey, String(Date.now() + dismissDurationMs));
        } catch {
            // Installation still works when browser storage is unavailable.
        }
    }

    function hidePrompt() {
        if (!prompt) return;
        window.clearTimeout(hideTimer);
        prompt.classList.remove('is-visible');
        hideTimer = window.setTimeout(() => {
            prompt.hidden = true;
            hideTimer = null;
        }, 220);
    }

    function showPrompt(options) {
        if (!prompt || !title || !message || !action || !dismiss) return;
        promptMode = options.mode;
        window.clearTimeout(hideTimer);
        hideTimer = null;
        title.textContent = options.title;
        message.textContent = options.message;
        action.textContent = options.action;
        dismiss.textContent = options.dismiss || 'Not now';
        action.hidden = options.actionHidden === true;
        prompt.hidden = false;
        window.requestAnimationFrame(() => prompt.classList.add('is-visible'));
    }

    function showInstallPrompt() {
        if (!isMobileInstallSurface() || isStandalone() || installPromptDismissed()) return;
        showPrompt({
            mode: 'install',
            title: 'Install Personal Tools',
            message: 'Launch it from your Home Screen in a dedicated app window.',
            action: 'Install'
        });
    }

    function showIosInstructions() {
        const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
        if (!isIos || isStandalone() || installPromptDismissed() || !document.body.dataset.userId) return;
        showPrompt({
            mode: 'ios',
            title: 'Add Personal Tools to Home Screen',
            message: 'Open the browser Share menu, choose Add to Home Screen, then enable Open as Web App.',
            action: 'Got it'
        });
    }

    function showUpdatePrompt(worker) {
        waitingWorker = worker;
        showPrompt({
            mode: 'update',
            title: 'Personal Tools update ready',
            message: 'Reload once to switch to the latest app assets.',
            action: 'Update now',
            dismiss: 'Later'
        });
    }

    window.addEventListener('beforeinstallprompt', event => {
        event.preventDefault();
        installEvent = event;
        if (document.body.dataset.userId) showInstallPrompt();
    });

    window.addEventListener('appinstalled', () => {
        installEvent = null;
        hidePrompt();
        window.personalToolsToast?.success('Personal Tools was installed successfully.');
    });

    action?.addEventListener('click', async () => {
        if (promptMode === 'update' && waitingWorker) {
            waitingWorker.postMessage({ type: 'SKIP_WAITING' });
            return;
        }

        if (promptMode === 'ios') {
            rememberInstallDismissal();
            hidePrompt();
            return;
        }

        if (!installEvent) return;
        const event = installEvent;
        installEvent = null;
        await event.prompt();
        const choice = await event.userChoice;
        if (choice.outcome !== 'accepted') rememberInstallDismissal();
        hidePrompt();
    });

    dismiss?.addEventListener('click', () => {
        if (promptMode !== 'update') rememberInstallDismissal();
        hidePrompt();
    });

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloadingForUpdate) return;
        reloadingForUpdate = true;
        window.location.reload();
    });

    navigator.serviceWorker.register('/service-worker.js', { scope: '/' })
        .then(registration => {
            if (registration.waiting && navigator.serviceWorker.controller) showUpdatePrompt(registration.waiting);
            registration.addEventListener('updatefound', () => {
                const worker = registration.installing;
                worker?.addEventListener('statechange', () => {
                    if (worker.state === 'installed' && navigator.serviceWorker.controller) showUpdatePrompt(worker);
                });
            });
        })
        .catch(error => console.warn('Personal Tools service worker registration failed.', error));

    if (!installEvent) window.setTimeout(showIosInstructions, 2200);
})();

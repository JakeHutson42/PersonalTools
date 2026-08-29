(function () {
    'use strict';

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    }

    function isIosDevice() {
        // iPadOS identifies itself as macOS, so account for touch-enabled Macs too.
        return /iphone|ipad|ipod/i.test(navigator.userAgent)
            || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    }

    function isMobileInstallSurface() {
        if (isIosDevice()) return true;

        if (typeof navigator.userAgentData?.mobile === 'boolean') {
            return navigator.userAgentData.mobile;
        }

        return /android|iphone|ipad|ipod|mobile/i.test(navigator.userAgent);
    }

    document.documentElement.classList.toggle('is-standalone-app', isStandalone());

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
    let reloadAfterWorkerUpdate = false;
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
        if (!isIosDevice() || isStandalone() || installPromptDismissed() || !document.body.dataset.userId) return;
        showPrompt({
            mode: 'ios',
            title: 'Add Personal Tools to Home Screen',
            message: 'Safari requires you to confirm this yourself. Open the Share Sheet, choose Add to Home Screen, then tap Add.',
            action: 'Open Share Sheet',
            dismiss: 'Not now'
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
            reloadAfterWorkerUpdate = true;
            waitingWorker.postMessage({ type: 'SKIP_WAITING' });
            return;
        }

        if (promptMode === 'ios') {
            if (typeof navigator.share === 'function') {
                try {
                    await navigator.share({
                        title: document.title,
                        text: 'Add Personal Tools to your Home Screen.',
                        url: window.location.href
                    });
                } catch (error) {
                    // Cancelling the Share Sheet is expected, so leave the instructions visible.
                    if (error?.name !== 'AbortError') console.warn('Personal Tools Share Sheet could not be opened.', error);
                }
            } else {
                window.personalToolsToast?.info('Use Safari’s Share button, then choose Add to Home Screen.');
            }
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

    if (!('serviceWorker' in navigator)) {
        window.setTimeout(showIosInstructions, 2200);
        return;
    }

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        // A controller change also happens when the app is first installed. Only reload after
        // the player explicitly accepted an update, never while they are opening cases.
        if (!reloadAfterWorkerUpdate) return;
        reloadAfterWorkerUpdate = false;
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

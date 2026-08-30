(() => {
    'use strict';
    const button = document.querySelector('#caseBattleSoundEnabled');
    if (!button) return;
    const storageKey = 'personalTools.caseBattleAudio';
    let state = { enabled:true, volume:.35 };
    try { state = { ...state, ...JSON.parse(localStorage.getItem(storageKey) || '{}') }; } catch { /* Optional preference. */ }
    const meter = document.querySelector('#caseBattleSoundVolumeMeter'), fill = meter?.querySelector('span'), value = document.querySelector('#caseBattleSoundVolumeValue'), status = document.querySelector('#caseBattleSoundStatus'), icon = document.querySelector('#caseBattleSoundButtonIcon'), text = document.querySelector('#caseBattleSoundButtonText');
    const track = new Audio('/audio/case-battles/where-are-you-liquid-dnb.mp3');
    const winnerReveal = new Audio('/audio/case-battles/winner-reveal.mp3');
    track.loop = true;
    track.preload = 'auto';
    winnerReveal.preload = 'auto';
    let musicLevel = 1;
    const volumeToGain = volume => Math.pow(Math.max(0, Math.min(1, Number(volume) || 0)), 1.6);
    const syncMusicVolume = () => { track.volume = volumeToGain(state.volume) * musicLevel; };
    const syncWinnerVolume = () => { winnerReveal.volume = Math.min(.28, Math.pow(Math.max(0, Math.min(1, Number(state.volume) || 0)), .9) * .55); };
    syncMusicVolume();
    syncWinnerVolume();
    let running = false, playPending = false, fadingFrame = 0, unlockArmed = false;
    const save = () => { try { localStorage.setItem(storageKey, JSON.stringify(state)); } catch { /* Optional preference. */ } };
    function render() {
        const percent = Math.round(state.volume * 100), audible = state.enabled && percent > 0;
        button.setAttribute('aria-pressed', String(state.enabled));
        button.querySelector('i').className = 'fa-solid ' + (audible ? 'fa-volume-high' : 'fa-volume-xmark');
        button.querySelector('span').textContent = audible ? 'On' : 'Off';
        icon.className = 'fa-solid ' + (audible ? 'fa-volume-high' : 'fa-volume-xmark');
        text.textContent = audible ? 'Sound' : 'Muted';
        value.textContent = percent + '%';
        meter?.setAttribute('aria-valuenow', String(percent));
        if (fill) fill.style.width = percent + '%';
        status.textContent = running && audible ? 'Where Are You · playing' : audible && unlockArmed ? 'Ready on your first interaction' : audible ? 'Loading arena track' : 'Audio muted';
    }
    function disarmUnlock() {
        if (!unlockArmed) return;
        unlockArmed = false;
        document.removeEventListener('pointerdown', unlockPlayback, true);
        document.removeEventListener('keydown', unlockPlayback, true);
    }
    function armUnlock() {
        if (unlockArmed || !state.enabled) return;
        unlockArmed = true;
        // Audible autoplay may be rejected by the browser. Capture the first interaction anywhere
        // so playback recovers without requiring a second sound-control click.
        document.addEventListener('pointerdown', unlockPlayback, true);
        document.addEventListener('keydown', unlockPlayback, true);
        render();
    }
    function unlockPlayback() { if (state.enabled && !running && !playPending) start(); }
    function start() {
        if (!state.enabled || running || playPending) return;
        window.cancelAnimationFrame(fadingFrame);
        musicLevel = 1;
        syncMusicVolume();
        playPending = true;
        const attempt = track.play();
        if (!attempt?.then) {
            playPending = false;
            running = !track.paused;
            if (running) disarmUnlock(); else armUnlock();
            render();
            return;
        }
        attempt.then(() => { playPending = false; running = true; disarmUnlock(); render(); })
            .catch(() => { playPending = false; running = false; armUnlock(); render(); });
    }
    function fadeOut() {
        if (!running) return;
        window.cancelAnimationFrame(fadingFrame);
        const started = performance.now(), initialVolume = track.volume;
        const frame = now => {
            const progress = Math.min(1, (now - started) / 1200);
            track.volume = initialVolume * (1 - progress);
            if (progress < 1) { fadingFrame = window.requestAnimationFrame(frame); return; }
            track.pause();
            track.currentTime = 0;
            musicLevel = 1;
            syncMusicVolume();
            running = false;
            render();
        };
        fadingFrame = window.requestAnimationFrame(frame);
    }
    function duckForReveal() {
        if (!running) return;
        window.cancelAnimationFrame(fadingFrame);
        const started = performance.now(), initialLevel = musicLevel, targetLevel = .32;
        const frame = now => {
            const progress = Math.min(1, (now - started) / 850);
            musicLevel = initialLevel + ((targetLevel - initialLevel) * (1 - Math.pow(1 - progress, 3)));
            syncMusicVolume();
            if (progress < 1) fadingFrame = window.requestAnimationFrame(frame);
        };
        fadingFrame = window.requestAnimationFrame(frame);
    }
    function playWinnerReveal() {
        if (!state.enabled || state.volume <= 0) return;
        syncWinnerVolume();
        winnerReveal.currentTime = 0;
        winnerReveal.play()?.catch(() => { /* Playback can still be browser-blocked before interaction. */ });
    }
    function setVolume(event) {
        const rect = meter.getBoundingClientRect();
        state.volume = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
        syncMusicVolume();
        syncWinnerVolume();
        save();
        render();
    }
    button.addEventListener('click', () => { state.enabled = !state.enabled; if (state.enabled) start(); else { disarmUnlock(); fadeOut(); winnerReveal.pause(); winnerReveal.currentTime = 0; } save(); render(); });
    meter?.addEventListener('pointerdown', event => { meter.setPointerCapture?.(event.pointerId); setVolume(event); });
    meter?.addEventListener('pointermove', event => { if (event.buttons) setVolume(event); });
    meter?.addEventListener('keydown', event => {
        if (!['ArrowLeft','ArrowRight','Home','End'].includes(event.key)) return;
        event.preventDefault();
        state.volume = event.key === 'Home' ? 0 : event.key === 'End' ? 1 : Math.max(0, Math.min(1, state.volume + (event.key === 'ArrowRight' ? .05 : -.05)));
        syncMusicVolume();
        syncWinnerVolume();
        save();
        render();
    });
    track.addEventListener('ended', () => { running = false; if (state.enabled) start(); });
    track.addEventListener('error', () => { running = false; status.textContent = 'Arena track could not be loaded'; });
    render();
    start();
    window.caseBattleAudio = { start, fadeOut, duckForReveal, playWinnerReveal };
})();

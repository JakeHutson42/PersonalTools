import { Chessboard, COLOR, INPUT_EVENT_TYPE, FEN } from '/vendor/chess/board/package/src/Chessboard.js';
import { createChessMotion } from './page-chess-motion.js';
import { createChessAnalysis } from './page-chess-analysis.js';
import { approximateElo, beginnerMove } from './page-chess-ai.js';
import { describeChessOutcome, createChessOutcome } from './page-chess-outcome.js';
import { Chess } from '../../vendor/chess/rules/chess.js';

const root = document.querySelector('[data-chess-page]');
if (root) {
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const $ = id => document.getElementById(id);
    const board = new Chessboard($('chessBoard'), {
        position: FEN.start,
        assetsUrl: '/vendor/chess/board/package/assets/',
        style: { animationDuration: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 280, cssClass: 'chess-club' }
    });
    const motion = createChessMotion(board, $('chessBoardStage'), $('chessOpening'), $('chessMotionLayer'), $('chessSkipOpening'));
    const outcomeMotion = createChessOutcome(board, $('chessBoardStage'), $('chessResultModal'));
    const myId = document.body.dataset.userId?.toLowerCase();
    const aiModeKey = 'personaltools.chess.ai-modes.v1';
    let aiModes = (() => { try { return JSON.parse(localStorage.getItem(aiModeKey)) || {}; } catch { return {}; } })();
    const analysis = createChessAnalysis($('chessMoveStrength'), { show: showSuggestedMove, restore: restoreLiveBoard, userId: myId });
    const openedGames = new Set();
    let game = null;
    let games = [];
    let moveListSignature = null;
    let moveQualities = new Map();
    let busy = false;
    let undoing = false;
    let promotionMove = null;
    const promotionModal = window.bootstrap.Modal.getOrCreateInstance($('chessPromotionModal'));
    let connection = null;
    let engine = null;
    let engineReady = false;
    let engineFailed = false;
    let engineRequest = null;
    let aiTurnStartedKey = null;
    let aiRetryKey = null;
    let aiRetryCount = 0;
    let selectedSquare = null;
    let previewState = null;
    let premove = null;
    let annotationStart = null;
    let annotations = { arrows: [], squares: [] };
    let immersive = false;
    const soundKey = 'personaltools.chess.sound-enabled.v1';
    let soundEnabled = (() => { try { return localStorage.getItem(soundKey) !== 'false'; } catch { return true; } })();
    let audioContext = null;

    function ensureAudio() {
        if (!soundEnabled || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return null;
        try { audioContext ||= new AudioContext(); if (audioContext.state === 'suspended') audioContext.resume(); return audioContext; } catch { return null; }
    }
    function playSound(kind) {
        const context = ensureAudio(); if (!context) return;
        const now = context.currentTime, tones = kind === 'capture' ? [[140,.08],[86,.16]] : kind === 'start' ? [[392,.1],[587,.16],[784,.22]] : kind === 'notice' ? [[660,.09],[880,.14]] : [[250,.07],[340,.09]];
        tones.forEach(([frequency, duration], index) => { const oscillator = context.createOscillator(), gain = context.createGain(); oscillator.type = kind === 'capture' ? 'triangle' : 'sine'; oscillator.frequency.setValueAtTime(frequency, now + index * .045); gain.gain.setValueAtTime(.0001, now + index * .045); gain.gain.exponentialRampToValueAtTime(.055, now + index * .045 + .012); gain.gain.exponentialRampToValueAtTime(.0001, now + index * .045 + duration); oscillator.connect(gain).connect(context.destination); oscillator.start(now + index * .045); oscillator.stop(now + index * .045 + duration + .02); });
    }
    function updateSoundButton() { const button = $('chessSound'); if (!button) return; button.textContent = soundEnabled ? 'Sound on' : 'Sound off'; button.setAttribute('aria-pressed', String(soundEnabled)); }
    function updateDifficultyStyle() {
        const select = $('chessDifficulty'), dock = root.querySelector('.chess-action-dock'); if (!select || !dock) return;
        const rating = Number(select.value), name = select.selectedOptions[0]?.textContent?.split(' · ')[0] || 'Computer';
        dock.dataset.difficultyBand = rating < 800 ? 'gentle' : rating < 1400 ? 'steady' : rating < 1900 ? 'bold' : 'legendary';
        $('chessDifficultyLabel').textContent = name;
    }
    function setImmersive(value) {
        immersive = value; root.classList.toggle('chess-immersive', immersive);
        $('chessImmersive').textContent = immersive ? 'Exit immersive' : 'Immersive mode'; $('chessImmersive').setAttribute('aria-pressed', String(immersive));
        $('chessExitImmersive').hidden = !immersive;
        requestAnimationFrame(() => board?.view?.redraw?.());
    }
    async function toggleImmersive() {
        if (immersive) { if (document.fullscreenElement) await document.exitFullscreen?.(); else setImmersive(false); return; }
        setImmersive(true);
        try { await root.requestFullscreen?.(); } catch { /* The focused in-page mode still works if fullscreen is unavailable. */ }
    }

    window.addEventListener('chess-review-ready', event => {
        const detail = event.detail;
        if (!detail || detail.gameId !== game?.gameId) return;
        for (const item of detail.grades || []) moveQualities.set(item.ply, item.quality);
        moveListSignature = null;
        renderMoves(game.moves);
    });

    function message(value) { $('chessMessage').textContent = value || ''; }
    async function api(path, method = 'GET', body) {
        try {
            return await window.jQuery.ajax({
                url: `/api/chess${path}`, method, contentType: 'application/json', dataType: 'json',
                data: body === undefined ? undefined : JSON.stringify(body),
                headers: { RequestVerificationToken: token }, showToast: false,
                showLoader: false
            });
        } catch (xhr) {
            let detail = xhr.responseJSON?.message;
            if (!detail && xhr.responseText) {
                try { detail = JSON.parse(xhr.responseText).message; } catch { /* Keep the generic error below. */ }
            }
            throw new Error(detail || `The chess request could not be completed${xhr.status ? ` (HTTP ${xhr.status})` : ''}.`);
        }
    }
    function canMove() {
        if (!game || game.status !== 'active' || busy || previewState || $('chessBoardStage').classList.contains('chess-intro-active')) return false;
        const whiteTurn = game.fen.split(' ')[1] === 'w';
        return whiteTurn ? game.whiteUserId.toLowerCase() === myId :
            game.mode === 'pvp' && game.blackUserId?.toLowerCase() === myId;
    }
    function playerColor(item = game) {
        if (!item) return null;
        return item.whiteUserId?.toLowerCase() === myId ? 'w' : item.blackUserId?.toLowerCase() === myId ? 'b' : null;
    }
    function canPremove() {
        if (!game || game.mode !== 'pvp' || game.status !== 'active' || busy || previewState || canMove()) return false;
        return playerColor() && game.fen.split(' ')[1] !== playerColor();
    }
    function isOwnPiece(square) {
        try { return new Chess(game.fen).get(square)?.color === playerColor(); } catch { return false; }
    }
    function renderPremoveNotice() {
        const notice = $('chessPremoveNotice');
        if (!notice) return;
        notice.hidden = !premove;
        if (premove) notice.querySelector('span').textContent = `Queued: ${premove.from} to ${premove.to}. It will play if it is still legal.`;
    }
    function clearPremove() { premove = null; renderPremoveNotice(); }
    function clearHighlights() {
        selectedSquare = null;
        $('chessBoard').querySelector('.chess-move-markers')?.remove();
    }
    function endPreview() {
        previewState = null;
        $('chessBoardStage').classList.remove('chess-preview-active');
        if ($('chessPreviewNotice')) $('chessPreviewNotice').hidden = true;
    }
    function showSuggestedMove(suggestion) {
        if (!game || busy) return false;
        clearHighlights();
        previewState = { gameId: game.gameId, revision: game.revision };
        $('chessBoardStage').classList.add('chess-preview-active');
        if ($('chessPreviewNotice')) { $('chessPreviewNotice').textContent = suggestion.notice || 'Suggested move preview · not saved'; $('chessPreviewNotice').hidden = false; }
        $('chessBoard').querySelectorAll('.chess-last-move-from,.chess-last-move-to')
            .forEach(square => square.classList.remove('chess-last-move-from', 'chess-last-move-to'));
        outcomeMotion.hideOverlay();
        board.setPosition(suggestion.fen, true).catch(() => restoreLiveBoard());
        return true;
    }
    function restoreLiveBoard() {
        if (!previewState) return;
        const current = game;
        endPreview();
        board.setPosition(current.fen, true).then(() => {
            if (game?.gameId === current.gameId && game.revision === current.revision) {
                showLastOpponentMove(current);
                outcomeMotion.redraw();
            }
        }).catch(() => {});
    }
    function marker(group, square, className, radius) {
        const point = board.view.squareToPoint(square);
        const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        circle.setAttribute('cx', point.x + board.view.squareWidth / 2);
        circle.setAttribute('cy', point.y + board.view.squareHeight / 2);
        circle.setAttribute('r', board.view.squareWidth * radius);
        circle.setAttribute('class', className);
        group.append(circle);
    }
    function squareAtTarget(target) { return target?.closest?.('.square[data-square]')?.dataset.square || null; }
    function drawAnnotations() {
        $('chessBoard').querySelector('.chess-annotation-layer')?.remove();
        if (!annotations.arrows.length && !annotations.squares.length) return;
        const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        group.setAttribute('class', 'chess-annotation-layer'); group.setAttribute('pointer-events', 'none');
        annotations.squares.forEach(square => {
            const point = board.view.squareToPoint(square), rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
            rect.setAttribute('x', point.x + 3); rect.setAttribute('y', point.y + 3); rect.setAttribute('width', board.view.squareWidth - 6); rect.setAttribute('height', board.view.squareHeight - 6); rect.setAttribute('rx', 4); rect.setAttribute('class', 'chess-annotation-square'); group.append(rect);
        });
        annotations.arrows.forEach(({ from, to }) => {
            const start = board.view.squareToPoint(from), end = board.view.squareToPoint(to), size = board.view.squareWidth;
            const x1 = start.x + size / 2, y1 = start.y + size / 2, x2 = end.x + size / 2, y2 = end.y + size / 2;
            const angle = Math.atan2(y2 - y1, x2 - x1), head = size * .19, line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
            line.setAttribute('x1', x1); line.setAttribute('y1', y1); line.setAttribute('x2', x2 - Math.cos(angle) * head * .55); line.setAttribute('y2', y2 - Math.sin(angle) * head * .55); line.setAttribute('class', 'chess-annotation-arrow'); group.append(line);
            const arrow = document.createElementNS('http://www.w3.org/2000/svg', 'path');
            const leftX = x2 - Math.cos(angle - .55) * head, leftY = y2 - Math.sin(angle - .55) * head, rightX = x2 - Math.cos(angle + .55) * head, rightY = y2 - Math.sin(angle + .55) * head;
            arrow.setAttribute('d', `M ${x2} ${y2} L ${leftX} ${leftY} L ${rightX} ${rightY} Z`); arrow.setAttribute('class', 'chess-annotation-arrow'); group.append(arrow);
        });
        board.view.markersTopLayer.append(group);
    }
    function toggleAnnotation(from, to) {
        if (!from || !to) return;
        if (from === to) { annotations.squares = annotations.squares.includes(from) ? annotations.squares.filter(square => square !== from) : [...annotations.squares, from]; }
        else { const existing = annotations.arrows.findIndex(arrow => arrow.from === from && arrow.to === to); annotations.arrows = existing >= 0 ? annotations.arrows.filter((_, index) => index !== existing) : [...annotations.arrows, { from, to }]; }
        drawAnnotations();
    }
    function drawMoveMarkers() {
        $('chessBoard').querySelector('.chess-move-markers')?.remove();
        if (!selectedSquare) return;
        const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        group.setAttribute('class', 'chess-move-markers');
        group.setAttribute('pointer-events', 'none');
        board.view.markersTopLayer.append(group);
        marker(group, selectedSquare, 'chess-source-ring', .42);
        const targets = new Map();
        for (const move of game?.legalMoves || []) {
            if (move.from === selectedSquare) targets.set(move.to, (targets.get(move.to) || false) || move.capture);
        }
        for (const [square, capture] of targets) marker(group, square, capture ? 'chess-capture-ring' : 'chess-move-dot', capture ? .4 : .115);
    }
    function isCheckmate(item) {
        return item.status === 'finished' && !!item.moves?.at(-1)?.san?.endsWith('#');
    }
    function renderOutcome(item, outcome) {
        const banner = $('chessOutcome');
        if (!banner) return;
        banner.hidden = !outcome;
        if (!outcome) return;
        const mate = outcome.kind === 'checkmate';
        const won = item.result === (item.whiteUserId.toLowerCase() === myId ? 'white' : 'black');
        banner.classList.toggle('chess-outcome-win', mate && won);
        banner.classList.toggle('chess-outcome-draw', !mate);
        banner.querySelector('.chess-outcome-symbol').textContent = mate ? '♚' : '½–½';
        $('chessOutcomeTitle').textContent = outcome.title;
        $('chessOutcomeDetail').textContent = mate ? (won ? 'You won the game.' : 'Your opponent won the game.') :
            outcome.kind === 'stalemate' ? 'Draw — no legal move, and the king is not in check.' : `Draw by ${outcome.reason}.`;
    }
    function showLastOpponentMove(item) {
        $('chessBoard').querySelectorAll('.chess-last-move-from,.chess-last-move-to')
            .forEach(square => square.classList.remove('chess-last-move-from', 'chess-last-move-to'));
        const last = item.moves?.at(-1);
        if (!last) return;
        const opponentMoved = item.mode === 'ai' ? last.ply % 2 === 0 :
            (last.ply % 2 === 1 ? item.whiteUserId.toLowerCase() !== myId : item.blackUserId?.toLowerCase() !== myId);
        if (!opponentMoved) return;
        $('chessBoard').querySelector(`.board .square[data-square="${last.from}"]`)?.classList.add('chess-last-move-from');
        $('chessBoard').querySelector(`.board .square[data-square="${last.to}"]`)?.classList.add('chess-last-move-to');
    }
    function updateUndo() {
        const button = $('chessUndo');
        if (!button) return; // The running Razor page may lag behind this static script until restart.
        button.hidden = game?.mode !== 'ai' || game.version === 0;
        button.disabled = busy || undoing;
        updateAiMode();
    }
    function updateAiMode() {
        const hint = $('chessAskHint'); if (!hint) return;
        const mode = game ? aiModes[game.gameId] || 'challenge' : 'challenge';
        hint.hidden = game?.mode !== 'ai' || mode !== 'coach' || game.status !== 'active';
        hint.disabled = busy || !canMove();
    }
    function highlightMoves(from) {
        clearHighlights();
        selectedSquare = from;
        drawMoveMarkers();
    }
    function boardInput(event) {
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted) {
            const canQueue = canPremove() && isOwnPiece(event.squareFrom);
            if ((!canMove() && !canQueue) || (canMove() && !game.legalMoves?.some(move => move.from === event.squareFrom))) return false;
            highlightMoves(event.squareFrom);
            return true;
        }
        if (event.type === INPUT_EVENT_TYPE.validateMoveInput) {
            if (!canMove()) {
                if (canPremove() && isOwnPiece(event.squareFrom)) {
                    premove = { from: event.squareFrom, to: event.squareTo, gameId: game.gameId };
                    clearHighlights(); renderPremoveNotice();
                }
                return false;
            }
            const legal = game.legalMoves?.filter(move => move.from === event.squareFrom && move.to === event.squareTo) || [];
            if (!legal.length) return false;
            if (legal.some(move => move.promotion)) {
                promotionMove = { gameId: game.gameId, version: game.version, from: event.squareFrom, to: event.squareTo };
                clearHighlights();
                const symbols = event.piece?.startsWith('b') ? { q: '♛', r: '♜', b: '♝', n: '♞' } : { q: '♕', r: '♖', b: '♗', n: '♘' };
                $('chessPromotionModal').querySelectorAll('[data-chess-promotion]').forEach(button => {
                    button.querySelector('span').textContent = symbols[button.dataset.chessPromotion];
                });
                promotionModal.show();
                return false;
            }
            return true;
        }
        if (event.type === INPUT_EVENT_TYPE.moveInputCanceled) clearHighlights();
        if (event.type === INPUT_EVENT_TYPE.moveInputFinished) {
            clearHighlights();
            if (event.legalMove) submitMove(event.squareFrom, event.squareTo, null);
        }
    }
    board.enableMoveInput(boardInput);
    $('chessBoard').addEventListener('contextmenu', event => event.preventDefault());
    $('chessBoard').addEventListener('pointerdown', event => {
        if (event.button !== 2) return;
        annotationStart = squareAtTarget(event.target);
        event.preventDefault(); event.stopPropagation();
    }, true);
    $('chessBoard').addEventListener('pointerup', event => {
        if (event.button !== 2 || !annotationStart) return;
        const start = annotationStart; annotationStart = null;
        toggleAnnotation(start, squareAtTarget(event.target));
        event.preventDefault(); event.stopPropagation();
    }, true);
    $('chessCancelPremove')?.addEventListener('click', clearPremove);
    $('chessImmersive')?.addEventListener('click', toggleImmersive);
    $('chessExitImmersive')?.addEventListener('click', toggleImmersive);
    $('chessSound')?.addEventListener('click', () => { soundEnabled = !soundEnabled; try { localStorage.setItem(soundKey, String(soundEnabled)); } catch { /* Sound still works for this visit. */ } if (soundEnabled) playSound('notice'); updateSoundButton(); });
    $('chessDifficulty')?.addEventListener('change', updateDifficultyStyle);
    document.addEventListener('fullscreenchange', () => { if (!document.fullscreenElement && immersive) setImmersive(false); });
    updateSoundButton();
    updateDifficultyStyle();
    if (window.ResizeObserver) new ResizeObserver(() => requestAnimationFrame(() => {
        if (selectedSquare) drawMoveMarkers();
        drawAnnotations();
        if (!previewState) outcomeMotion.redraw();
    })).observe($('chessBoard'));

    function statusText(item) {
        if (item.status === 'waiting') return 'Waiting for a friend to join';
        if (isCheckmate(item)) return `Checkmate · ${item.result === 'white' ? 'White' : 'Black'} won`;
        if (item.status === 'finished') return item.result === 'draw'
            ? (describeChessOutcome(item)?.kind === 'stalemate' ? 'Stalemate · Draw' : 'Draw')
            : `${item.result === 'white' ? 'White' : 'Black'} won`;
        if (item.mode === 'ai') return `${item.fen.split(' ')[1] === 'w' ? 'Your' : 'Computer’s'} turn`;
        return `${item.fen.split(' ')[1] === 'w' ? 'White' : 'Black'} to move`;
    }
    function renderTurn(item) {
        const badge = $('chessTurnBadge');
        badge.className = 'badge';
        if (item.status === 'waiting') {
            badge.classList.add('text-bg-secondary'); badge.textContent = 'Waiting for friend'; return;
        }
        if (item.status === 'finished') {
            const won = item.result === (item.whiteUserId.toLowerCase() === myId ? 'white' : 'black');
            badge.classList.add(isCheckmate(item) ? (won ? 'text-bg-success' : 'text-bg-danger') : 'text-bg-secondary');
            badge.textContent = isCheckmate(item) ? `Checkmate · You ${won ? 'won' : 'lost'}` :
                item.result === 'draw' ? (describeChessOutcome(item)?.kind === 'stalemate' ? 'Stalemate · Draw' : 'Draw') : (won ? 'You won' : 'You lost');
            return;
        }
        const whiteTurn = item.fen.split(' ')[1] === 'w';
        const mine = whiteTurn ? item.whiteUserId.toLowerCase() === myId :
            item.mode === 'pvp' && item.blackUserId?.toLowerCase() === myId;
        badge.classList.add(mine ? 'text-bg-warning' : 'text-bg-secondary');
        badge.textContent = mine ? `Your turn · ${whiteTurn ? 'White' : 'Black'}` :
            `${item.mode === 'ai' ? 'Computer' : 'Friend'} to move · ${whiteTurn ? 'White' : 'Black'}`;
    }
    function renderList() {
        const list = $('chessGames'); list.replaceChildren();
        if (!games.length) { list.textContent = 'No games yet.'; return; }
        for (const item of games) {
            const button = document.createElement('button'); button.type = 'button';
            button.className = 'chess-game-item';
            const active = game?.gameId === item.gameId;
            button.classList.toggle('active', active);
            button.setAttribute('aria-pressed', String(active));
            const title = document.createElement('strong');
            title.textContent = item.mode === 'ai' ? 'You vs Computer' : 'You vs Friend';
            const meta = document.createElement('span'); meta.className = 'chess-game-meta';
            const status = document.createElement('span'); status.textContent = statusText(active ? game : item);
            const date = document.createElement('time');
            date.dateTime = item.updatedUtc;
            date.textContent = new Date(item.updatedUtc).toLocaleDateString();
            meta.append(status, date); button.append(title, meta);
            button.addEventListener('click', () => openGame(item.gameId)); list.append(button);
        }
    }
    function renderMoves(moves) {
        const signature = `${game?.gameId}:${game?.revision}:${moves?.length || 0}:${[...moveQualities.entries()].map(item => item.join(':')).join('|')}`;
        if (signature === moveListSignature) return;
        moveListSignature = signature;
        const target = $('chessMoves'); target.replaceChildren();
        if (!moves?.length) { target.textContent = 'No moves yet.'; return; }
        for (let index = 0; index < moves.length; index += 2) {
            const row = document.createElement('div'); row.className = 'chess-move-row';
            const number = document.createElement('span'); number.className = 'chess-move-number';
            number.textContent = `${Math.ceil((index + 1) / 2)}.`;
            row.append(number);
            for (const move of [moves[index], moves[index + 1]]) {
                const cell = document.createElement('button'); cell.type = 'button'; cell.className = 'chess-move-cell';
                if (move) {
                    const quality = moveQualities.get(move.ply);
                    if (quality && quality !== 'other') cell.classList.add(`quality-${quality}`);
                    const notation = document.createElement('span');
                    notation.textContent = move.san || `${move.from}-${move.to}`;
                    cell.append(notation);
                    if (/^[wb][pnbrq]$/.test(move.captured || '')) {
                        const names = { p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen' };
                        const icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                        icon.setAttribute('viewBox', '0 0 40 40');
                        icon.setAttribute('class', 'chess-captured-icon');
                        icon.setAttribute('role', 'img');
                        icon.setAttribute('aria-label', `Captured ${move.captured[0] === 'w' ? 'white' : 'black'} ${names[move.captured[1]]}`);
                        const piece = document.createElementNS('http://www.w3.org/2000/svg', 'use');
                        piece.setAttribute('href', `/vendor/chess/board/package/assets/pieces/standard.svg#${move.captured}`);
                        icon.append(piece); cell.append(icon);
                    }
                    cell.classList.toggle('latest', move.ply === moves[moves.length - 1].ply);
                    const qualityLabel = quality && quality !== 'other' ? `, ${quality} move` : '';
                    cell.setAttribute('aria-label', `Review move ${move.ply}: ${move.san || `${move.from}-${move.to}`}${qualityLabel}`);
                    cell.addEventListener('click', () => { if (game?.status === 'finished') analysis.select(move.ply - 1); });
                } else cell.classList.add('empty');
                row.append(cell);
            }
            target.append(row);
        }
        target.scrollTop = target.scrollHeight;
    }
    function playPremoveIfReady() {
        if (!premove) return;
        if (premove.gameId !== game?.gameId) { clearPremove(); return; }
        if (!canMove()) return;
        const legal = game.legalMoves?.filter(move => move.from === premove.from && move.to === premove.to) || [];
        if (!legal.length) { clearPremove(); message('Your queued move is no longer legal after your friend’s move.'); return; }
        const queued = premove, promotion = legal.some(move => move.promotion) ? 'q' : null;
        clearPremove(); message('Playing your queued move…'); submitMove(queued.from, queued.to, promotion, 'premove');
    }
    function render(item) {
        if (game?.gameId === item.gameId && item.revision < game.revision) return;
        const previous = game;
        const boardChanged = previous?.gameId !== item.gameId || previous.revision !== item.revision || previous.fen !== item.fen || previous.status !== item.status;
        if (previous?.gameId !== item.gameId) { moveQualities = new Map(); moveListSignature = null; clearPremove(); annotations = { arrows: [], squares: [] }; }
        const outcome = describeChessOutcome(item);
        game = item;
        $('chessGameTitle').textContent = item.mode === 'ai' ? 'You vs Computer' : 'You vs Friend';
        $('chessGameStatus').textContent = statusText(item);
        renderTurn(item);
        renderOutcome(item, outcome);
        $('chessResign').hidden = item.status !== 'active';
        updateUndo();
        updateAiMode();
        $('chessInvite').hidden = item.status !== 'waiting';
        $('chessJoin').hidden = true;
        if (item.status === 'waiting') $('chessInviteLink').value = `${location.origin}/Chess?join=${encodeURIComponent(item.gameId)}`;
        if (boardChanged) {
            if (previewState) endPreview();
            outcomeMotion.cancel();
            clearHighlights();
            const newGame = previous?.gameId !== item.gameId;
            if (newGame) motion.finishOpening();
            const lastMove = item.moves?.at(-1);
            const capture = !newGame && item.version === previous.version + 1 && lastMove?.captured
                ? motion.captureSnapshot(lastMove) : null;
            const shouldOpen = newGame && item.version === 0 && !openedGames.has(item.gameId);
            if (shouldOpen) openedGames.add(item.gameId);
            const orient = newGame
                ? board.setOrientation(item.blackUserId?.toLowerCase() === myId ? COLOR.black : COLOR.white)
                : Promise.resolve();
            Promise.resolve(orient).then(() => board.setPosition(item.fen, !newGame && previous.fen !== item.fen))
                .then(() => {
                    if (game?.gameId !== item.gameId || game.revision !== item.revision) { capture?.remove(); return; }
                    showLastOpponentMove(item);
                    drawAnnotations();
                    outcomeMotion.play(item, outcome);
                    if (shouldOpen) { motion.playOpening(); playSound('start'); }
                    else {
                        motion.playCapture(capture);
                        if (lastMove) {
                            playSound(lastMove.captured ? 'capture' : 'move');
                            const opponentMoved = item.mode === 'ai' ? lastMove.ply % 2 === 0 : (lastMove.ply % 2 === 1 ? item.whiteUserId.toLowerCase() !== myId : item.blackUserId?.toLowerCase() !== myId);
                            if (opponentMoved && canMove()) window.setTimeout(() => playSound('notice'), 150);
                        }
                    }
                }).catch(() => { capture?.remove(); motion.finishOpening(); });
        }
        renderMoves(item.moves); renderList(); analysis.review(item); playPremoveIfReady();
        if (item.mode === 'ai' && item.status === 'active') {
            // Warm the WASM worker while the player is looking at the board,
            // so the first reply does not wait for engine startup.
            ensureEngine();
            if (item.fen.split(' ')[1] === 'b') startAiTurn();
        }
    }
    async function refreshList() { games = await api(''); renderList(); }
    async function refreshGame() { if (game) render(await api(`/${game.gameId}`)); }
    async function openGame(id) {
        try {
            message(''); render(await api(`/${id}`));
            history.replaceState(null, '', `/Chess?game=${encodeURIComponent(id)}`);
            if (connection?.state === 'Connected') await connection.invoke('JoinGame', id);
        } catch (error) { message(error.message); }
    }
    async function submitMove(from, to, promotion, source = 'player') {
        if (!game || busy) return;
        const id = game.gameId, version = game.version;
        busy = true;
        updateUndo();
        try {
            render(await api(`/${id}/move`, 'POST', { from, to, promotion, version, revision: game.revision }));
            if (source === 'ai') { aiRetryKey = null; aiRetryCount = 0; }
            message('');
            refreshList().catch(() => {});
        } catch (error) {
            await refreshGame().catch(() => board.setPosition(game.fen));
            const stillWaiting = source === 'ai' && game?.gameId === id && game.status === 'active' && game.fen.split(' ')[1] === 'b';
            const retryKey = `${id}:${game?.revision}`;
            if (stillWaiting && (aiRetryKey !== retryKey || aiRetryCount < 1)) {
                aiRetryCount = aiRetryKey === retryKey ? aiRetryCount + 1 : 1;
                aiRetryKey = retryKey;
                aiTurnStartedKey = null;
                engineRequest = null;
                engine?.terminate(); engine = null; engineReady = false;
                message('Retrying the computer move…');
            } else message(source === 'ai' ? `Computer move failed: ${error.message}` : error.message);
        } finally {
            busy = false;
            updateUndo();
            // The response is already on the board; start the reply immediately,
            // without waiting for the secondary games-list refresh.
            if (game?.mode === 'ai' && game.status === 'active' && game.fen.split(' ')[1] === 'b') startAiTurn();
        }
    }
    async function create(mode) {
        try {
            message(''); const item = await api('', 'POST', { mode, difficulty: Number($('chessDifficulty').value) });
            if (mode === 'ai') { aiModes[item.gameId] = $('chessPlayMode').value; localStorage.setItem(aiModeKey, JSON.stringify(aiModes)); }
            await refreshList(); await openGame(item.gameId);
            window.personalToolsToast?.success(mode === 'ai' ? 'Computer game started.' : 'Friend challenge created.');
        } catch (error) { message(error.message); }
    }
    function ensureEngine() {
        if (engine) return true;
        if (engineFailed) return false;
        try {
            engine = new Worker('/vendor/chess/stockfish/package/bin/stockfish-19-lite-single.js');
            engine.onmessage = onEngineMessage;
            engine.onerror = () => {
                message('Computer engine could not start. Refresh to retry.');
                engine?.terminate(); engine = null; engineRequest = null; engineFailed = true;
            };
            engine.postMessage('uci'); engine.postMessage('isready');
            return true;
        } catch (error) {
            message(`Computer engine unavailable: ${error.message}`);
            engineFailed = true;
            return false;
        }
    }
    function startAiTurn() {
        if (!game || busy) return;
        const turnKey = `${game.gameId}:${game.revision}`;
        if (aiTurnStartedKey === turnKey) return;
        aiTurnStartedKey = turnKey;
        const playMode = aiModes[game.gameId] || 'challenge';
        const elo = playMode === 'relaxed' ? 100 : approximateElo(game.difficulty);
        const easyMove = beginnerMove(game.moves || [], elo);
        if (easyMove) { submitMove(easyMove.from, easyMove.to, easyMove.promotion, 'ai'); return; }
        if (!ensureEngine()) { aiTurnStartedKey = null; return; }
        engineRequest = { gameId: game.gameId, version: game.version, revision: game.revision, moves: game.moves || [], elo };
        if (engineReady) searchAiMove();
    }
    function searchAiMove() {
        if (!engine || !engineRequest) return;
        engine.postMessage('setoption name UCI_LimitStrength value true');
        engine.postMessage(`setoption name UCI_Elo value ${Math.max(1320, engineRequest.elo)}`);
        engine.postMessage(`position startpos moves ${engineRequest.moves.map(m => `${m.from}${m.to}${m.promotion || ''}`).join(' ')}`);
        engine.postMessage(`go movetime ${Math.min(500, 180 + engineRequest.elo / 10)}`);
    }
    function onEngineMessage(event) {
        const line = String(event.data || '');
        if (line.includes('readyok')) { engineReady = true; searchAiMove(); return; }
        const match = line.match(/(?:^|\n)bestmove ([a-h][1-8])([a-h][1-8])([qrbn])?/);
        if (!match || !engineRequest) return;
        const request = engineRequest; engineRequest = null;
        if (!undoing && game?.gameId === request.gameId && game.revision === request.revision) submitMove(match[1], match[2], match[3] || null, 'ai');
    }
    async function connect() {
        if (!window.signalR) return;
        connection = new window.signalR.HubConnectionBuilder().withUrl('/hubs/chess').withAutomaticReconnect().build();
        connection.on('ChessChanged', async () => { await refreshGame().catch(() => {}); await refreshList().catch(() => {}); });
        connection.onreconnected(() => { if (game) connection.invoke('JoinGame', game.gameId).then(refreshGame).catch(() => {}); });
        try { await connection.start(); if (game) await connection.invoke('JoinGame', game.gameId); } catch { /* Polling still works. */ }
    }
    $('chessNewAi').addEventListener('click', () => create('ai'));
    $('chessNewFriend').addEventListener('click', () => create('pvp'));
    async function undoTurn() {
        if (!game || game.mode !== 'ai' || !game.version || busy) return;
        const { gameId, version, revision } = game;
        busy = undoing = true;
        updateUndo();
        // A stopped worker can still emit bestmove; the revision check and cleared
        // request ensure that old AI work never replays onto the restored board.
        engineRequest = null;
        aiTurnStartedKey = null;
        analysis.stop();
        outcomeMotion.cancel();
        engine?.terminate(); engine = null; engineReady = false;
        try {
            render(await api(`/${gameId}/undo`, 'POST', { version, revision }));
            message('Turn undone.');
            refreshList().catch(() => {});
        } catch (error) {
            message(error.message);
            await refreshGame().catch(() => {});
        } finally {
            busy = undoing = false;
            updateUndo();
        }
    }
    $('chessUndo')?.addEventListener('click', undoTurn);
    $('chessAskHint')?.addEventListener('click', () => {
        if (!game || game.mode !== 'ai' || (aiModes[game.gameId] || 'challenge') !== 'coach' || !canMove()) return;
        const candidate = game.legalMoves?.find(move => move.capture) || game.legalMoves?.[0];
        message(candidate ? `Hint: pause here and calculate ${candidate.from}–${candidate.to} first. Look for checks, captures, and threats.` : 'Hint: pause and list checks, captures, and threats before moving.');
    });
    $('chessResultUndo')?.addEventListener('click', undoTurn);
    $('chessPromotionModal').addEventListener('hidden.bs.modal', () => { promotionMove = null; });
    $('chessPromotionModal').querySelectorAll('[data-chess-promotion]').forEach(button => button.addEventListener('click', () => {
        const selected = promotionMove;
        promotionMove = null;
        promotionModal.hide();
        if (selected && game?.gameId === selected.gameId && game.version === selected.version)
            submitMove(selected.from, selected.to, button.dataset.chessPromotion);
        else message('The game changed while choosing a promotion. Try again.');
    }));
    $('chessCopyInvite').addEventListener('click', async () => { try { await navigator.clipboard.writeText($('chessInviteLink').value); message('Invitation link copied.'); } catch { $('chessInviteLink').select(); message('Copy the selected link.'); } });
    $('chessJoinButton').addEventListener('click', async () => {
        try { const id = new URLSearchParams(location.search).get('join'); const item = await api(`/${id}/join`, 'POST'); await refreshList(); await openGame(item.gameId); window.personalToolsToast?.success('Challenge joined.'); }
        catch (error) { message(error.message); }
    });
    $('chessResign').addEventListener('click', async () => {
        if (!game || !window.confirm('Resign this game?')) return;
        try { render(await api(`/${game.gameId}/resign`, 'POST')); await refreshList(); window.personalToolsToast?.info('Game resigned.'); } catch (error) { message(error.message); }
    });
    async function init() {
        try {
            await refreshList();
            const params = new URLSearchParams(location.search);
            const joinId = params.get('join');
            if (joinId && /^[0-9a-f-]{36}$/i.test(joinId)) {
                $('chessJoin').hidden = false; $('chessGameTitle').textContent = 'Friend challenge';
                $('chessGameStatus').textContent = 'Join to play Black.';
            } else {
                const selected = params.get('game') || games[0]?.gameId;
                if (selected) await openGame(selected);
            }
            await connect();
            window.setInterval(() => { refreshGame().catch(() => {}); refreshList().catch(() => {}); }, 10000);
        } catch (error) { message(error.message); }
    }
    init();
}

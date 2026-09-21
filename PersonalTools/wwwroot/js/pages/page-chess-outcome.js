import { Chess } from '../../vendor/chess/rules/chess.js';

const svgNs = 'http://www.w3.org/2000/svg';
const files = 'abcdefgh';

function neighbours(square) {
    const file = files.indexOf(square[0]);
    const rank = Number(square[1]);
    const result = [];
    for (let df = -1; df <= 1; df++) for (let dr = -1; dr <= 1; dr++) {
        if (!df && !dr) continue;
        if (file + df >= 0 && file + df < 8 && rank + dr >= 1 && rank + dr <= 8)
            result.push(`${files[file + df]}${rank + dr}`);
    }
    return result;
}

export function describeChessOutcome(game) {
    if (game?.status !== 'finished' || !game.fen) return null;
    let position;
    try { position = new Chess(game.fen); } catch { return null; }
    const side = position.turn();
    const enemy = side === 'w' ? 'b' : 'w';
    const king = position.board().flat().find(piece => piece?.type === 'k' && piece.color === side)?.square || null;
    const checkmate = position.isCheckmate();
    const stalemate = position.isStalemate();
    if (checkmate || stalemate) {
        if (!king) return null;
        // The king's current square must not hide a sliding attack on an escape square.
        const exposed = new Chess(game.fen);
        exposed.remove(king);
        const arrows = [];
        const covered = [];
        const blocked = [];
        for (const square of [king, ...neighbours(king)]) {
            const occupant = position.get(square);
            if (square !== king && occupant?.color === side) { blocked.push(square); continue; }
            const attackers = (square === king ? position : exposed).attackers(square, enemy);
            if (attackers.length) {
                covered.push(square);
                for (const from of attackers.slice(0, 2)) arrows.push({ from, to: square });
            }
        }
        return { kind: checkmate ? 'checkmate' : 'stalemate', king, arrows, covered, blocked,
            title: checkmate ? 'Checkmate' : 'Stalemate',
            explanation: checkmate
                ? 'The king is in check. The arrows show checking pieces and controlled escape squares; the rings mark blocked escapes.'
                : 'The king is not in check, but the side to move has no legal move. The arrows show controlled escape squares; the rings mark squares blocked by its own pieces.' };
    }
    if (game.result !== 'draw') return null; // A resignation needs no king-attack diagram.
    let repetition = false;
    if (game.moves?.length) {
        try {
            const replay = new Chess();
            for (const move of game.moves) replay.move({ from: move.from, to: move.to, promotion: move.promotion || undefined });
            repetition = replay.isThreefoldRepetition();
        } catch { /* The saved FEN still identifies other draw types. */ }
    }
    const reason = repetition ? 'repetition' : position.isDrawByFiftyMoves() ? 'fifty-move rule' :
        position.isInsufficientMaterial() ? 'insufficient material' : 'the draw rules';
    const explanations = {
        repetition: 'The same position occurred three times. The blue arrows trace the recent moves, not an attack on the king.',
        'fifty-move rule': 'Fifty moves by each side passed without a pawn move or capture. The king is not necessarily trapped.',
        'insufficient material': 'Neither side has enough material to force checkmate. The king is not trapped.',
        'the draw rules': 'The game ended in a draw. No king trap is implied.'
    };
    return { kind: 'draw', reason, title: 'Draw', explanation: explanations[reason], king: null,
        arrows: repetition ? game.moves.slice(-4).map(move => ({ from: move.from, to: move.to })) : [], covered: [], blocked: [] };
}

export function createChessOutcome(board, stage, modalElement) {
    const Modal = window.bootstrap?.Modal;
    let modal = null;
    if (modalElement && Modal) {
        const existing = Modal.getInstance?.(modalElement);
        try {
            if (existing?._config) modal = existing;
            else {
                existing?.dispose?.();
                modal = new Modal(modalElement, { backdrop: true, keyboard: true, focus: true });
            }
        } catch (error) {
            console.error('Chess result modal could not be initialised.', error);
        }
    }
    const shown = new Set();
    let current = null;
    let timers = [];
    let ghost = null;
    let hiddenKing = null;

    function schedule(action, delay) { timers.push(window.setTimeout(action, delay)); }
    function clearVisuals() {
        timers.forEach(id => window.clearTimeout(id)); timers = [];
        hiddenKing?.style.removeProperty('visibility'); hiddenKing = null;
        ghost?.remove(); ghost = null;
        stage.querySelector('.chess-result-flourish')?.remove();
        stage.querySelector('.chess-outcome-map')?.remove();
    }
    function cancel() {
        clearVisuals(); current = null; modal?.hide();
    }
    function svgElement(name, attributes = {}) {
        const element = document.createElementNS(svgNs, name);
        for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, value);
        return element;
    }
    function center(square) {
        const point = board.view.squareToPoint(square);
        return { x: point.x + board.view.squareWidth / 2, y: point.y + board.view.squareHeight / 2 };
    }
    function redraw() {
        stage.querySelector('.chess-outcome-map')?.remove();
        if (!current) return;
        const outcome = current.outcome;
        const group = svgElement('g', { class: `chess-outcome-map chess-outcome-map-${outcome.kind}`, 'pointer-events': 'none' });
        board.view.markersTopLayer.append(group);
        const defs = svgElement('defs');
        const markerId = `chess-outcome-tip-${board.id}`;
        const tip = svgElement('marker', { id: markerId, viewBox: '0 0 10 10', refX: '9', refY: '5', markerWidth: '5', markerHeight: '5', orient: 'auto-start-reverse' });
        tip.append(svgElement('path', { d: 'M 0 0 L 10 5 L 0 10 z', class: 'chess-outcome-tip' }));
        defs.append(tip); group.append(defs);
        outcome.arrows.forEach(({ from, to }, index) => {
            const a = center(from), b = center(to);
            const line = svgElement('line', { x1: a.x, y1: a.y, x2: b.x, y2: b.y,
                class: 'chess-outcome-arrow', 'marker-end': `url(#${markerId})` });
            line.style.setProperty('--arrow-delay', `${Math.min(index * 120, 960)}ms`);
            group.append(line);
        });
        for (const square of outcome.covered) {
            const p = center(square);
            group.append(svgElement('circle', { cx: p.x, cy: p.y, r: board.view.squareWidth * .15, class: 'chess-outcome-covered' }));
        }
        for (const square of outcome.blocked) {
            const p = center(square);
            group.append(svgElement('circle', { cx: p.x, cy: p.y, r: board.view.squareWidth * .37, class: 'chess-outcome-blocked' }));
        }
        if (outcome.king) {
            const p = center(outcome.king);
            group.append(svgElement('circle', { cx: p.x, cy: p.y, r: board.view.squareWidth * .47, class: 'chess-outcome-king' }));
        }
    }
    function fallKing() {
        if (!current?.outcome.king || !window.anime?.animate || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        const square = current.outcome.king;
        const piece = board.getPiece(square);
        const squareElement = stage.querySelector(`.board .square[data-square="${square}"]`);
        hiddenKing = stage.querySelector(`.pieces-layer g[data-square="${square}"]`);
        if (!piece || !squareElement || !hiddenKing) return;
        const target = squareElement.getBoundingClientRect(), parent = stage.getBoundingClientRect();
        ghost = document.createElement('div'); ghost.className = 'chess-falling-king';
        ghost.style.left = `${target.left - parent.left}px`; ghost.style.top = `${target.top - parent.top}px`;
        ghost.style.width = `${target.width}px`; ghost.style.height = `${target.height}px`;
        const svg = svgElement('svg', { viewBox: '0 0 40 40', 'aria-hidden': 'true' });
        svg.append(svgElement('use', { href: `/vendor/chess/board/package/assets/pieces/standard.svg#${piece}` }));
        ghost.append(svg); stage.append(ghost);
        hiddenKing.style.visibility = 'hidden';
        window.anime.animate(ghost, { rotate: [0, piece[0] === 'w' ? -65 : 65], translateY: [0, target.height * .3],
            scale: [1, .72], opacity: [1, 0], duration: 520, ease: 'inOut(3)' });
        schedule(() => { hiddenKing?.style.removeProperty('visibility'); hiddenKing = null; ghost?.remove(); ghost = null; }, 560);
    }
    function showPrompt() {
        if (!current || !modal) return;
        const { item, outcome } = current;
        const won = item.result === (item.whiteUserId.toLowerCase() === document.body.dataset.userId?.toLowerCase() ? 'white' : 'black');
        modalElement.classList.toggle('chess-result-win', outcome.kind === 'checkmate' && won);
        modalElement.classList.toggle('chess-result-loss', outcome.kind === 'checkmate' && !won);
        modalElement.classList.toggle('chess-result-draw', outcome.kind !== 'checkmate');
        modalElement.querySelector('#chessResultSymbol').textContent = outcome.kind === 'checkmate' ? '♚' : '½–½';
        modalElement.querySelector('#chessResultTitle').textContent = outcome.title;
        modalElement.querySelector('#chessResultDetail').textContent = outcome.kind === 'checkmate'
            ? (won ? 'You won!' : 'Your opponent won.') : outcome.kind === 'stalemate' ? 'The game is a draw.' : `Draw by ${outcome.reason}.`;
        modalElement.querySelector('#chessResultExplanation').textContent = outcome.explanation;
        modalElement.querySelector('#chessResultUndo').hidden = item.mode !== 'ai' || item.version === 0;
        modal.show();
    }
    function play(item, outcome) {
        clearVisuals();
        if (!outcome) { current = null; return; }
        current = { item, outcome };
        redraw();
        const key = `${item.gameId}:${item.revision}`;
        if (shown.has(key)) return;
        shown.add(key);
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const arrowFinish = 1150 + Math.min(Math.max(outcome.arrows.length - 1, 0) * 120, 960);
        if (!reduced) {
            if (outcome.kind === 'checkmate') schedule(fallKing, arrowFinish + 120);
            else schedule(() => {
                const flourish = document.createElement('div'); flourish.className = 'chess-result-flourish';
                flourish.textContent = '½–½'; stage.append(flourish);
                schedule(() => flourish.remove(), 820);
            }, 460);
        }
        // Let every attack arrow complete, then hold the fallen king briefly before the modal takes focus.
        schedule(showPrompt, reduced ? 120 : outcome.kind === 'checkmate' ? arrowFinish + 760 : 1100);
    }
    function hideOverlay() {
        timers.forEach(id => window.clearTimeout(id)); timers = [];
        hiddenKing?.style.removeProperty('visibility'); hiddenKing = null;
        ghost?.remove(); ghost = null;
        stage.querySelector('.chess-result-flourish')?.remove();
        stage.querySelector('.chess-outcome-map')?.remove();
    }
    return { play, redraw, cancel, hideOverlay };
}

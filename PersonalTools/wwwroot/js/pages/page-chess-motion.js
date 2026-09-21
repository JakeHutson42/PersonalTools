const sprite = '/vendor/chess/board/package/assets/pieces/standard.svg';
const svgNs = 'http://www.w3.org/2000/svg';

function pieceSvg(piece) {
    const svg = document.createElementNS(svgNs, 'svg');
    svg.setAttribute('viewBox', '0 0 40 40');
    svg.setAttribute('aria-hidden', 'true');
    const use = document.createElementNS(svgNs, 'use');
    use.setAttribute('href', `${sprite}#${piece}`);
    svg.append(use);
    return svg;
}

export function createChessMotion(board, stage, opening, layer, skipButton) {
    const piecesLayer = () => stage.querySelector('.pieces-layer');
    const motionAllowed = () => !window.matchMedia('(prefers-reduced-motion: reduce)').matches && !!window.anime?.animate;
    let openingRun = 0;
    let openingTimer = null;
    let openingActive = false;

    function squareRect(square) {
        return stage.querySelector(`.board .square[data-square="${square}"]`)?.getBoundingClientRect();
    }

    function finishOpening() {
        openingRun++;
        if (openingTimer) clearTimeout(openingTimer);
        openingTimer = null;
        opening.hidden = true;
        skipButton.hidden = true;
        layer.replaceChildren();
        stage.classList.remove('chess-intro-active');
        if (piecesLayer()) piecesLayer().style.opacity = '';
        openingActive = false;
    }

    function deployPieces(run) {
        if (run !== openingRun) return;
        opening.hidden = true;
        const stageRect = stage.getBoundingClientRect();
        const flights = [];
        for (const piece of stage.querySelectorAll('.pieces-layer g[data-square][data-piece]')) {
            const target = squareRect(piece.dataset.square);
            if (!target) continue;
            const flight = document.createElement('div');
            flight.className = 'chess-flight';
            flight.style.width = `${target.width}px`;
            flight.style.height = `${target.height}px`;
            flight.style.left = `${(stageRect.width - target.width) / 2}px`;
            flight.style.top = `${(stageRect.height - target.height) / 2}px`;
            flight.append(pieceSvg(piece.dataset.piece));
            layer.append(flight);
            flights.push({ flight, x: target.left - stageRect.left - (stageRect.width - target.width) / 2,
                y: target.top - stageRect.top - (stageRect.height - target.height) / 2 });
        }
        if (!flights.length) { finishOpening(); return; }
        // The center clears outward, so arriving pieces never conceal pieces still taking flight.
        flights.sort((a, b) => Math.hypot(a.x, a.y) - Math.hypot(b.x, b.y));
        flights.forEach(({ flight, x, y }, index) => {
            window.anime.animate(flight, {
                translateX: [0, x], translateY: [0, y], scale: [.45, 1], opacity: [.25, 1],
                duration: 980, delay: index * 28, ease: 'out(3)'
            });
        });
        openingTimer = window.setTimeout(() => { if (run === openingRun) finishOpening(); }, 980 + (flights.length - 1) * 28 + 80);
    }

    function playOpening() {
        finishOpening();
        if (!motionAllowed()) return;
        const layerElement = piecesLayer();
        if (!layerElement) return;
        const run = ++openingRun;
        openingActive = true;
        layerElement.style.opacity = '0';
        stage.classList.add('chess-intro-active');
        opening.hidden = false;
        skipButton.hidden = false;
        const left = opening.querySelector('.chess-door-left');
        const right = opening.querySelector('.chess-door-right');
        const seal = opening.querySelector('.chess-door-seal');
        left.style.transform = right.style.transform = '';
        left.style.opacity = right.style.opacity = '1';
        seal.style.opacity = '1';
        window.anime.animate(left, { rotateY: [0, -104], opacity: [1, .7], duration: 1450, ease: 'inOut(3)' });
        window.anime.animate(right, { rotateY: [0, 104], opacity: [1, .7], duration: 1450, ease: 'inOut(3)' });
        window.anime.animate(seal, { scale: [1, 1.18], opacity: [1, 0], duration: 520, ease: 'in(2)' });
        openingTimer = window.setTimeout(() => deployPieces(run), 1410);
    }

    function captureSnapshot(move) {
        if (!motionAllowed() || !move?.captured || openingActive) return null;
        const target = squareRect(move.to);
        if (!target) return null;
        // En passant removes the pawn beside the destination rather than on it.
        const capturedSquare = board.getPiece(move.to) ? move.to : `${move.to[0]}${move.from[1]}`;
        const piece = board.getPiece(capturedSquare);
        if (!piece) return null;
        const stageRect = stage.getBoundingClientRect();
        const effect = document.createElement('div');
        effect.className = 'chess-capture-effect';
        effect.style.left = `${target.left - stageRect.left}px`;
        effect.style.top = `${target.top - stageRect.top}px`;
        effect.style.width = `${target.width}px`;
        effect.style.height = `${target.height}px`;
        const ghost = document.createElement('div');
        ghost.className = 'chess-capture-piece';
        ghost.append(pieceSvg(piece));
        effect.append(ghost);
        layer.append(effect);
        return effect;
    }

    function playCapture(effect) {
        if (!effect?.isConnected) return;
        if (!motionAllowed()) { effect.remove(); return; }
        const ghost = effect.querySelector('.chess-capture-piece');
        const ring = document.createElement('span');
        ring.className = 'chess-capture-ring';
        effect.append(ring);
        window.anime.animate(ghost, { translateY: [0, -effect.clientHeight * .3], rotate: [0, 16], scale: [1, .72], opacity: [1, 0], duration: 780, ease: 'out(3)' });
        window.anime.animate(ring, { scale: [.35, 1.6], opacity: [.95, 0], duration: 780, ease: 'out(3)' });
        const radius = effect.clientWidth * .65;
        for (let i = 0; i < 8; i++) {
            const spark = document.createElement('span');
            spark.className = 'chess-capture-spark';
            effect.append(spark);
            const angle = i * Math.PI / 4 + Math.PI / 8;
            window.anime.animate(spark, {
                translateX: [0, Math.cos(angle) * radius], translateY: [0, Math.sin(angle) * radius],
                rotate: [0, 120], scale: [1, .2], opacity: [1, 0], duration: 720, ease: 'out(3)'
            });
        }
        window.setTimeout(() => effect.remove(), 860);
    }

    skipButton.addEventListener('click', finishOpening);
    window.addEventListener('resize', () => { if (openingActive) finishOpening(); });
    window.matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change', event => { if (event.matches) finishOpening(); });
    return { playOpening, finishOpening, captureSnapshot, playCapture };
}

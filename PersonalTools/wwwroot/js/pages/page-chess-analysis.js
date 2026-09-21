import { Chess } from '../../vendor/chess/rules/chess.js';

const workerUrl = '/vendor/chess/stockfish/package/bin/stockfish-19-lite-single.js';
const engineVersion = 'stockfish-19-lite/quick-180ms-v2';
const uci = move => `${move?.from || ''}${move?.to || ''}${move?.promotion || ''}`;
const cacheKey = game => `personaltools.chess.review:${game.gameId}:${game.revision}:${engineVersion}`;
const pieceName = piece => ({ p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' })[(piece || '').at(-1)] || 'piece';
const toWhiteScore = (raw, ply) => ply % 2 === 0 ? raw : -raw;
const advantagePercent = score => Math.round(Math.max(8, Math.min(92, 50 + Math.max(-800, Math.min(800, score || 0)) / 20)));

function replay(moves, count) {
    const position = new Chess();
    for (const played of moves.slice(0, count)) position.move({ from: played.from, to: played.to, promotion: played.promotion || undefined });
    return position;
}
function positionAfter(moves, count) { try { return replay(moves, count).fen(); } catch { return null; } }
function moveDescription(moves, ply, alternative) {
    try {
        const position = replay(moves, ply);
        const source = alternative ? { from: alternative.slice(0, 2), to: alternative.slice(2, 4), promotion: alternative[4] || undefined } : moves[ply];
        const move = position.move(source);
        const verb = move.captured ? `takes ${pieceName(move.captured)} on ${move.to}` : `moves to ${move.to}`;
        const suffix = move.isCheckmate?.() ? ', checkmate' : move.isCheck?.() ? ', check' : '';
        return { san: move.san, text: `${pieceName(move.piece)} ${verb}${move.promotion ? ` and becomes a ${pieceName(move.promotion)}` : ''}${suffix}`, piece: move.piece, color: move.color, fen: position.fen() };
    } catch { return null; }
}
function suggestedPosition(moves, ply, best) { return /^[a-h][1-8][a-h][1-8][qrbn]?$/.test(best || '') ? moveDescription(moves, ply, best) : null; }
function concreteExplanation(moves, ply, loss) {
    const reply = moves[ply + 1];
    if (!reply || loss < 90 || !reply.captured) return null;
    const captured = pieceName(reply.captured);
    return captured === 'queen' || captured === 'rook' || (loss >= 220 && captured !== 'pawn') ? `The next reply can win your ${captured}.` : null;
}

export function createChessAnalysis(element, preview = {}) {
    const reviewRoot = document.getElementById('chessReview');
    const controls = { status: document.getElementById('chessReviewStatus'), confidence: document.getElementById('chessReviewConfidence'), graph: document.getElementById('chessEvaluationGraph'), insight: document.getElementById('chessReviewInsight'), label: document.getElementById('chessReviewMoveLabel'), first: document.getElementById('chessReviewFirst'), previous: document.getElementById('chessReviewPrevious'), next: document.getElementById('chessReviewNext'), last: document.getElementById('chessReviewLast'), back: document.getElementById('chessReviewReturn') };
    let worker = null, timeout = null, activeKey = null, reviewGame = null, data = null, selectedPly = 0, pending = null, latest = null, top = [];
    let feedbackKey = null, feedbackWorker = null, feedbackTimeout = null;

    function liveMessage(label, detail, quality = '') {
        if (!element) return;
        element.hidden = false; element.className = `chess-analysis chess-move-feedback ${quality}`; element.replaceChildren();
        const strong = document.createElement('strong'); strong.textContent = label; element.append(strong, document.createTextNode(detail));
    }
    function stop() { if (timeout) clearTimeout(timeout); timeout = null; worker?.terminate(); worker = null; pending = null; }
    function stopFeedback() { if (feedbackTimeout) clearTimeout(feedbackTimeout); feedbackTimeout = null; feedbackWorker?.terminate(); feedbackWorker = null; }
    function showReview(show) { if (reviewRoot) reviewRoot.hidden = !show; }
    function save() { try { localStorage.setItem(cacheKey(reviewGame), JSON.stringify(data)); } catch { /* Current review remains visible. */ } }
    function load(game) { try { return JSON.parse(localStorage.getItem(cacheKey(game)) || 'null'); } catch { return null; } }
    function isMine(move) { const white = move?.ply % 2 === 1; return white ? reviewGame?.whiteUserId?.toLowerCase() === preview.userId : reviewGame?.blackUserId?.toLowerCase() === preview.userId; }
    function actor(move) { const mine = isMine(move); return { mine, name: mine ? 'You' : reviewGame?.mode === 'ai' ? 'The computer' : 'Your opponent', color: move?.ply % 2 === 1 ? 'White' : 'Black' }; }
    function lossFor(ply) { const before = data?.positions?.[ply]?.white, after = data?.positions?.[ply + 1]?.white; return before == null || after == null || !isMine(reviewGame?.moves?.[ply]) ? null : Math.max(0, ply % 2 === 0 ? before - after : after - before); }
    function grade(loss, mine) { if (!mine || loss == null) return 'other'; if (loss <= 15) return 'best'; if (loss <= 60) return 'good'; if (loss <= 180) return 'mistake'; return 'blunder'; }
    function publishGrades() {
        if (!reviewGame?.moves?.length || !data?.positions?.length) return;
        window.dispatchEvent(new CustomEvent('chess-review-ready', { detail: { gameId: reviewGame.gameId, revision: reviewGame.revision, grades: reviewGame.moves.map((move, ply) => ({ ply: move.ply, quality: grade(lossFor(ply), isMine(move)), loss: lossFor(ply), mine: isMine(move) })) } }));
    }
    function markers() { return reviewGame.moves.map((move, ply) => ({ ply, loss: lossFor(ply) })).filter(item => item.loss >= 75).sort((a, b) => b.loss - a.loss).slice(0, 3).map(item => item.ply); }
    function renderGraph() {
        if (!controls.graph || !data?.positions?.length) return;
        const score = data.positions[Math.min(selectedPly + 1, data.positions.length - 1)]?.white || 0, white = advantagePercent(score), marked = new Set(markers());
        controls.graph.replaceChildren();
        const title = document.createElement('p'); title.className = 'chess-advantage-title'; title.textContent = 'Position balance';
        const bar = document.createElement('div'); bar.className = 'chess-advantage-bar'; bar.setAttribute('role', 'img'); bar.setAttribute('aria-label', `White has ${white}% of the position balance. Black has ${100 - white}%.`);
        const whiteFill = document.createElement('span'); whiteFill.className = 'chess-advantage-white'; whiteFill.style.width = `${white}%`; whiteFill.textContent = `White ${white}%`;
        const blackFill = document.createElement('span'); blackFill.className = 'chess-advantage-black'; blackFill.textContent = `Black ${100 - white}%`; bar.append(whiteFill, blackFill);
        const note = document.createElement('p'); note.className = 'chess-advantage-note'; note.textContent = score > 75 ? 'White has the easier position.' : score < -75 ? 'Black has the easier position.' : 'The position is still fairly even.';
        const turns = document.createElement('div'); turns.className = 'chess-turning-points';
        if (marked.size) { const label = document.createElement('span'); label.textContent = 'Key moments'; turns.append(label); [...marked].sort((a, b) => a - b).forEach(ply => { const button = document.createElement('button'); button.type = 'button'; button.className = 'chess-turning-point'; button.classList.toggle('active', ply === selectedPly); button.textContent = `Move ${Math.ceil((ply + 1) / 2)}`; button.addEventListener('click', () => select(ply)); turns.append(button); }); }
        controls.graph.append(title, bar, note, turns);
    }
    function pieceIcon(piece, color) { const icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg'); icon.setAttribute('viewBox', '0 0 40 40'); icon.setAttribute('class', `chess-review-piece ${color === 'b' ? 'black' : 'white'}`); icon.setAttribute('role', 'img'); icon.setAttribute('aria-label', `${color === 'b' ? 'black' : 'white'} ${pieceName(piece)}`); const use = document.createElementNS('http://www.w3.org/2000/svg', 'use'); use.setAttribute('href', `/vendor/chess/board/package/assets/pieces/standard.svg#${color}${piece}`); icon.append(use); return icon; }
    function renderInsight() {
        if (!data || !reviewGame?.moves?.length || !controls.insight) return;
        const move = reviewGame.moves[selectedPly], played = moveDescription(reviewGame.moves, selectedPly), before = data.positions[selectedPly], suggestion = suggestedPosition(reviewGame.moves, selectedPly, before?.best), loss = lossFor(selectedPly), who = actor(move);
        controls.label.textContent = `${Math.ceil((selectedPly + 1) / 2)}${selectedPly % 2 ? '…' : '.'} ${played?.san || move.san || uci(move)}`; controls.first.disabled = controls.previous.disabled = selectedPly === 0; controls.next.disabled = controls.last.disabled = selectedPly >= reviewGame.moves.length - 1;
        controls.insight.replaceChildren();
        const title = document.createElement('strong'); title.textContent = who.mine && markers().includes(selectedPly) ? (loss >= 250 ? 'Major turning point' : 'Instructive moment') : who.mine ? 'Your move' : `${who.name}'s move`;
        const playedLine = document.createElement('p'); playedLine.className = 'chess-review-move-line'; playedLine.append(pieceIcon(played?.piece || 'p', played?.color), document.createTextNode(`${who.name} (${who.color}): ${played?.text || move.san || uci(move)}.`));
        const copy = document.createElement('p');
        if (!who.mine) copy.textContent = `This was ${who.name.toLowerCase()}’s move. It is shown so you can follow the game, but it is not counted as your mistake.`;
        else { const close = before?.close ? ' More than one move was close, so treat this as a helpful idea, not the only good move.' : ''; const swing = loss == null ? 'This quick check is uncertain.' : loss < 35 ? 'The position stayed about the same.' : `This changed the position by about ${(loss / 100).toFixed(1)} pawns.`; const explanation = concreteExplanation(reviewGame.moves, selectedPly, loss || 0); copy.textContent = `${suggestion ? `A strong alternative was ${suggestion.text}. ` : ''}${swing}${explanation ? ` ${explanation}` : ''}${close}`; }
        const actions = document.createElement('div'); actions.className = 'chess-review-continuations'; const actualFen = positionAfter(reviewGame.moves, selectedPly + 1);
        if (actualFen) { const real = document.createElement('button'); real.type = 'button'; real.className = 'btn btn-sm btn-outline-secondary'; real.textContent = 'Show this position'; real.addEventListener('click', () => preview.show?.({ fen: actualFen, notice: 'Game position · not saved' })); actions.append(real); }
        if (suggestion && who.mine) { const best = document.createElement('button'); best.type = 'button'; best.className = 'btn btn-sm btn-outline-secondary'; best.textContent = 'Show the suggestion'; best.addEventListener('click', () => preview.show?.({ fen: suggestion.fen, notice: 'Suggested position · not saved' })); actions.append(best); }
        controls.insight.append(title, playedLine, copy, actions);
    }
    function select(ply) {
        if (!reviewGame?.moves?.[ply]) return;
        selectedPly = ply; renderGraph(); renderInsight();
        const played = moveDescription(reviewGame.moves, ply), fen = positionAfter(reviewGame.moves, ply + 1);
        if (fen) preview.show?.({ fen, notice: `${played?.san || reviewGame.moves[ply].san || 'Reviewed move'} · game position` });
    }
    function saveCoachPositions() {
        try { const current = JSON.parse(localStorage.getItem('personaltools.chess.coach.v1') || '{"tasks":[]}'); const additions = markers().map(ply => { const loss = lossFor(ply), move = reviewGame.moves[ply], before = data.positions[ply]; return { id: `${reviewGame.gameId}:${reviewGame.revision}:${ply}`, fen: positionAfter(reviewGame.moves, ply), best: before?.best, played: move?.san || uci(move), loss, kind: ply < 16 ? 'opening choice' : loss >= 220 ? 'missed tactic' : 'overlooked defence', createdAt: new Date().toISOString(), done: false }; }).filter(item => item.fen && item.best); current.tasks = [...additions, ...(current.tasks || []).filter(item => !additions.some(addition => addition.id === item.id))].slice(0, 30); localStorage.setItem('personaltools.chess.coach.v1', JSON.stringify(current)); } catch { /* Optional coaching history. */ }
    }
    function complete() { saveCoachPositions(); stop(); save(); selectedPly = markers()[0] ?? reviewGame.moves.length - 1; controls.status.textContent = 'Your quick review is ready. Start with the marked key moments.'; controls.confidence.textContent = 'Quick guide'; publishGrades(); renderGraph(); renderInsight(); }
    function queuePosition(index) { if (!worker || !reviewGame || index > reviewGame.moves.length) return complete(); pending = index; latest = null; top = []; const history = reviewGame.moves.slice(0, index).map(uci).join(' '); worker.postMessage(`position startpos${history ? ` moves ${history}` : ''}`); worker.postMessage('go movetime 180'); }
    function start(game) {
        reviewGame = game; data = { engineVersion, positions: [] }; selectedPly = 0; showReview(true); controls.status.textContent = 'Looking over your game…'; controls.confidence.textContent = 'Quick guide';
        try { worker = new Worker(workerUrl); worker.onmessage = event => String(event.data || '').split(/\r?\n/).forEach(line => { const score = line.match(/\bscore (cp|mate) (-?\d+)/), pv = line.match(/\bpv ([a-h][1-8][a-h][1-8][qrbn]?)/), multi = line.match(/\bmultipv (\d+)/), rank = Number(multi?.[1] || 1); if (line.startsWith('info ') && score) { const raw = score[1] === 'mate' ? Math.sign(Number(score[2])) * 100000 : Number(score[2]); latest = raw; if (pv) top[rank] = { score: raw, best: pv[1] }; } if (!line.startsWith('bestmove ')) return; const best = line.match(/^bestmove ([a-h][1-8][a-h][1-8][qrbn]?)/)?.[1] || top[1]?.best || null, raw = latest ?? top[1]?.score ?? 0, second = top[2]?.score; data.positions[pending] = { white: toWhiteScore(raw, pending), best, close: second != null && Math.abs(raw - second) <= 35 }; controls.status.textContent = `Looking at position ${pending + 1} of ${reviewGame.moves.length + 1}…`; queuePosition(pending + 1); }); worker.onerror = () => { stop(); controls.status.textContent = 'This review could not finish in this browser.'; controls.confidence.textContent = 'Unavailable'; }; worker.postMessage('uci'); worker.postMessage('setoption name MultiPV value 2'); queuePosition(0); timeout = setTimeout(() => { if (worker) { stop(); controls.status.textContent = 'The review stopped early. The positions shown are only a quick guide.'; controls.confidence.textContent = 'Partial'; publishGrades(); renderGraph(); renderInsight(); } }, 60000); } catch { controls.status.textContent = 'Review is unavailable in this browser.'; controls.confidence.textContent = 'Unavailable'; }
    }
    function quickMoveFeedback(game) {
        if (!game?.moves?.length || game.status === 'finished' || game.mode !== 'ai') return;
        const ply = game.moves.length % 2 === 0 ? game.moves.length - 2 : -1;
        if (ply < 0 || !isMine(game.moves[ply])) { liveMessage('Move played', 'The computer is considering its reply. Your quick feedback will appear here.'); return; }
        const key = `${game.gameId}:${game.revision}:${ply}`; if (key === feedbackKey) return; feedbackKey = key; stopFeedback(); liveMessage('Move feedback', 'Checking your last move…');
        try { feedbackWorker = new Worker(workerUrl); const scores = []; let stage = 0, raw = 0, best = null; const run = index => { raw = 0; best = null; const history = game.moves.slice(0, index).map(uci).join(' '); feedbackWorker.postMessage(`position startpos${history ? ` moves ${history}` : ''}`); feedbackWorker.postMessage('go movetime 70'); }; feedbackWorker.onmessage = event => String(event.data || '').split(/\r?\n/).forEach(line => { const score = line.match(/\bscore (cp|mate) (-?\d+)/), pv = line.match(/\bpv ([a-h][1-8][a-h][1-8][qrbn]?)/); if (line.startsWith('info ') && score) { raw = score[1] === 'mate' ? Math.sign(Number(score[2])) * 100000 : Number(score[2]); if (pv) best = pv[1]; } if (!line.startsWith('bestmove ')) return; scores[stage] = toWhiteScore(raw, ply + stage); if (stage++ === 0) run(ply + 1); else { const loss = Math.max(0, scores[0] - scores[1]), quality = grade(loss, true), labels = { best: 'Strong move', good: 'Good move', mistake: 'A move to revisit', blunder: 'A costly move' }, suggested = best ? moveDescription(game.moves, ply, best) : null, detail = loss <= 15 ? 'This kept your position steady.' : `This may have changed the position by about ${(loss / 100).toFixed(1)} pawns.${suggested ? ` A strong idea was ${suggested.text}.` : ''}`; liveMessage(labels[quality], detail, quality); window.dispatchEvent(new CustomEvent('chess-review-ready', { detail: { gameId: game.gameId, revision: game.revision, grades: [{ ply: game.moves[ply].ply, quality, loss, mine: true }] } })); stopFeedback(); } }); feedbackWorker.onerror = () => { stopFeedback(); liveMessage('Move feedback', 'Your move will be included in the review when the game ends.'); }; feedbackWorker.postMessage('uci'); run(ply); feedbackTimeout = setTimeout(() => { stopFeedback(); liveMessage('Move feedback', 'Your move will be included in the review when the game ends.'); }, 2500); } catch { liveMessage('Move feedback', 'Your move will be included in the review when the game ends.'); }
    }
    function review(game) {
        reviewGame = game;
        if (!game?.moves?.length || game.status !== 'finished') { activeKey = null; stop(); showReview(false); quickMoveFeedback(game); if (!game?.moves?.length && element) element.hidden = true; return; }
        stopFeedback(); if (element) element.hidden = true; const key = `${game.gameId}:${game.revision}:${engineVersion}`; if (key === activeKey) return; activeKey = key; stop(); data = load(game);
        if (data?.positions?.length === game.moves.length + 1) { showReview(true); selectedPly = Math.max(0, markers()[0] ?? game.moves.length - 1); controls.status.textContent = 'Your saved quick review is ready.'; controls.confidence.textContent = 'Saved guide'; publishGrades(); renderGraph(); renderInsight(); } else start(game);
    }
    controls.first?.addEventListener('click', () => select(0)); controls.previous?.addEventListener('click', () => select(selectedPly - 1)); controls.next?.addEventListener('click', () => select(selectedPly + 1)); controls.last?.addEventListener('click', () => select(reviewGame.moves.length - 1)); controls.back?.addEventListener('click', () => preview.restore?.());
    return { review, stop, select };
}

import { Chessboard, COLOR, INPUT_EVENT_TYPE } from '/vendor/chess/board/package/src/Chessboard.js';
import { Chess } from '../../vendor/chess/rules/chess.js';
import { curatedPuzzles } from './page-chess-puzzles-data.js';

const root = document.querySelector('[data-chess-puzzles-page]');
if (root) {
    const $ = id => document.getElementById(id);
    const storeKey = 'personaltools.chess.tactics.v1';
    const dayKey = () => new Date().toLocaleDateString('en-CA');
    const UCI = /^[a-h][1-8][a-h][1-8][qrbn]?$/;
    const themeNames = { fork: 'Fork', pin: 'Pin', hangingPiece: 'Hanging piece', mate: 'Mate', mateIn1: 'Mate in one', mateIn2: 'Mate in two', defensiveMove: 'Defensive move', sacrifice: 'Sacrifice', discoveredAttack: 'Discovered attack', backRankMate: 'Back-rank mate' };
    const name = type => ({ p: 'pawn', n: 'knight', b: 'bishop', r: 'rook', q: 'queen', k: 'king' })[type] || 'piece';
    let board, puzzles = [], state, current = null, position = null, solutionIndex = 0, hintLevel = 0, startedAt = 0, timer = null, selected = null, hadMiss = false;

    function load() { try { return JSON.parse(localStorage.getItem(storeKey)) || { records: {}, sessions: {} }; } catch { return { records: {}, sessions: {} }; } }
    function save() { localStorage.setItem(storeKey, JSON.stringify(state)); }
    function move(position, uci) { return position.move({ from: uci.slice(0, 2), to: uci.slice(2, 4), promotion: uci[4] || undefined }); }
    function valid(raw) {
        if (!raw?.fen || !raw.moves?.length || raw.moves.some(value => !UCI.test(value))) return null;
        try { const test = new Chess(raw.fen); for (const uci of raw.moves) if (!move(test, uci)) return null; const play = new Chess(raw.fen); move(play, raw.moves[0]); return { ...raw, startFen: play.fen() }; } catch { return null; }
    }
    function due(record) { return record?.due && new Date(record.due).getTime() <= Date.now(); }
    function makeSession() {
        const used = new Set(); const dueItems = puzzles.filter(puzzle => due(state.records[puzzle.id])).sort((a, b) => new Date(state.records[a.id].due) - new Date(state.records[b.id].due));
        const unseen = puzzles.filter(puzzle => !state.records[puzzle.id]); const pick = [];
        for (const [theme, requested] of Object.entries(state.practiceThemes || {})) {
            let remaining = requested;
            for (const puzzle of unseen.filter(item => item.themes.includes(theme))) if (!used.has(puzzle.id) && pick.length < 2 && remaining > 0) { used.add(puzzle.id); pick.push(puzzle.id); remaining -= 1; }
            if (remaining > 0) state.practiceThemes[theme] = remaining; else delete state.practiceThemes[theme];
        }
        // Keep a daily session approachable: at most two overdue retries, then new material.
        for (const puzzle of [...dueItems.slice(0, 2), ...unseen.sort((a, b) => a.rating - b.rating)]) if (!used.has(puzzle.id) && pick.length < 5) { used.add(puzzle.id); pick.push(puzzle.id); }
        if (pick.length < 5) for (const puzzle of puzzles) if (!used.has(puzzle.id) && pick.length < 5) { used.add(puzzle.id); pick.push(puzzle.id); }
        return pick;
    }
    function session() {
        const key = dayKey(); if (!state.sessions[key]?.length) { state.sessions[key] = makeSession(); save(); }
        return state.sessions[key].map(id => puzzles.find(puzzle => puzzle.id === id)).filter(Boolean);
    }
    function puzzleLabel(puzzle) { const theme = puzzle.themes.find(value => themeNames[value]) || puzzle.themes[0]; return themeNames[theme] || theme.replace(/([A-Z])/g, ' $1'); }
    function record(result) {
        const elapsed = Math.max(1, Math.round((Date.now() - startedAt) / 1000)); const existing = state.records[current.id] || {};
        const days = result === 'solved' ? Math.min(14, Math.max(2, (existing.intervalDays || 1) * 2)) : 1;
        state.records[current.id] = { attempts: (existing.attempts || 0) + 1, hints: (existing.hints || 0) + hintLevel, seconds: (existing.seconds || 0) + elapsed, themes: current.themes, missed: !!existing.missed || hadMiss || result !== 'solved', lastResult: result, intervalDays: days, due: new Date(Date.now() + days * 86400000).toISOString() };
        save();
    }
    function setFeedback(text, kind = '') { $('chessPuzzleFeedback').textContent = text; $('chessPuzzleFeedback').className = `chess-puzzle-feedback ${kind}`; }
    function updateTimer() { if (startedAt) $('chessPuzzleMeta').textContent = `${current.rating} rating · ${Math.max(0, Math.round((Date.now() - startedAt) / 1000))}s`; }
    function expected() { return current.moves[solutionIndex]; }
    async function setBoard(animated = true) { await board.setPosition(position.fen(), animated); }
    function clearSelection() { selected = null; }
    function showHint() {
        const expectedMove = expected(); if (!expectedMove) return;
        hintLevel += 1; const piece = position.get(expectedMove.slice(0, 2));
        if (hintLevel === 1) { $('chessPuzzleHint').textContent = `Look for a ${puzzleLabel(current).toLowerCase()}. Start with checks, captures, and threats.`; $('chessPuzzleHintButton').textContent = 'Show which piece'; }
        else if (hintLevel === 2) { $('chessPuzzleHint').textContent = `Try moving the ${name(piece?.type)} first.`; $('chessPuzzleHintButton').textContent = 'Show the squares'; }
        else { $('chessPuzzleHint').textContent = `Try ${expectedMove.slice(0, 2)} to ${expectedMove.slice(2, 4)}.`; $('chessPuzzleHintButton').textContent = 'All clues shown'; $('chessPuzzleHintButton').disabled = true; }
    }
    async function reveal() {
        hintLevel = Math.max(hintLevel, 4); $('chessPuzzleHintButton').disabled = true; $('chessPuzzleSolution').disabled = true; clearInterval(timer);
        while (expected()) { move(position, expected()); solutionIndex += 1; await setBoard(true); }
        record('revealed'); setFeedback('Here is the answer. We will bring this puzzle back tomorrow.', 'missed'); $('chessPuzzleAgain').hidden = false; $('chessPuzzleNext').hidden = false; $('chessPuzzleTurn').textContent = 'Answer shown.';
    }
    async function advanceOpponent() { if (!expected()) return finish(); move(position, expected()); solutionIndex += 1; await setBoard(true); $('chessPuzzleTurn').textContent = 'Your move.'; }
    function finish() { clearInterval(timer); record('solved'); setFeedback(hintLevel ? 'You found it. Your clues are saved so we can help next time.' : 'You found it — well played.', 'solved'); $('chessPuzzleNext').hidden = false; $('chessPuzzleAgain').hidden = false; $('chessPuzzleAgain').textContent = 'Practise tomorrow'; $('chessPuzzleTurn').textContent = 'Puzzle complete.'; }
    async function tryMove(from, to, promotion) {
        const answer = expected(); const played = `${from}${to}${promotion || ''}`;
        if (played !== answer) { hadMiss = true; setFeedback('Not this move yet. Take another look or ask for a clue.', 'missed'); return false; }
        move(position, answer); solutionIndex += 1; await setBoard(true); if (!expected()) finish(); else setTimeout(() => advanceOpponent(), 360); return true;
    }
    function input(event) {
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted) { selected = event.squareFrom; return true; }
        if (event.type === INPUT_EVENT_TYPE.validateMoveInput) return true;
        if (event.type === INPUT_EVENT_TYPE.moveInputFinished) { clearSelection(); if (event.legalMove) tryMove(event.squareFrom, event.squareTo, null); }
    }
    async function open(index) {
        const items = session(); current = items[index]; if (!current) return;
        clearInterval(timer); position = new Chess(current.startFen); solutionIndex = 1; hintLevel = 0; hadMiss = false; startedAt = Date.now();
        $('chessPuzzleCount').textContent = `${index + 1}/${items.length}`; $('chessPuzzleTitle').textContent = 'Find the best move'; $('chessPuzzleTheme').textContent = puzzleLabel(current); $('chessPuzzleHint').textContent = 'Start with checks, captures, and threats.'; $('chessPuzzleHintButton').textContent = 'Give me a clue'; $('chessPuzzleHintButton').disabled = false; $('chessPuzzleSolution').disabled = false; $('chessPuzzleNext').hidden = true; $('chessPuzzleAgain').hidden = true; setFeedback(''); $('chessPuzzleTurn').textContent = 'Your move.';
        board.setOrientation(position.turn() === 'w' ? COLOR.white : COLOR.black); await setBoard(false); updateTimer(); timer = setInterval(updateTimer, 1000);
    }
    function next() { const items = session(); const index = items.findIndex(puzzle => puzzle.id === current.id); if (index < items.length - 1) open(index + 1); else { $('chessPuzzleTitle').textContent = 'Daily session complete'; $('chessPuzzleTurn').textContent = 'Come back tomorrow for new puzzles and anything due for review.'; $('chessPuzzleNext').hidden = true; } }
    function again() { const record = state.records[current.id] || {}; record.due = new Date(Date.now() + 86400000).toISOString(); record.lastResult = 'retry'; state.records[current.id] = record; save(); setFeedback('Added to tomorrow’s review queue.', ''); }
    function init() {
        puzzles = curatedPuzzles.map(valid).filter(Boolean); state = load();
        board = new Chessboard($('chessPuzzleBoard'), { position: puzzles[0]?.startFen, assetsUrl: '/vendor/chess/board/package/assets/', style: { cssClass: 'blue', animationDuration: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 220 } }); board.enableMoveInput(input);
        if (!puzzles.length) { $('chessPuzzleTitle').textContent = 'Puzzle set unavailable'; return; }
        open(0); $('chessPuzzleHintButton').addEventListener('click', showHint); $('chessPuzzleSolution').addEventListener('click', reveal); $('chessPuzzleNext').addEventListener('click', next); $('chessPuzzleAgain').addEventListener('click', again); $('chessPuzzleReset').addEventListener('click', () => { state.sessions[dayKey()] = makeSession(); save(); open(0); });
    }
    init();
}

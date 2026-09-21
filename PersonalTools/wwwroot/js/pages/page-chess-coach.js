import { Chessboard, COLOR, FEN, INPUT_EVENT_TYPE } from '/vendor/chess/board/package/src/Chessboard.js';
import { Chess } from '/vendor/chess/rules/chess.js';

const root = document.querySelector('[data-chess-coach-page]');
if (root) {
    const $ = id => document.getElementById(id);
    const coachKey = 'personaltools.chess.coach.v1', tacticsKey = 'personaltools.chess.tactics.v1', courseKey = 'personaltools.chess.course.v1';
    let state, board, task = null, position = null;
    const read = (key, fallback) => { try { return JSON.parse(localStorage.getItem(key)) || fallback; } catch { return fallback; } };
    const save = () => localStorage.setItem(coachKey, JSON.stringify(state));
    const label = value => ({ 'missed tactic': 'Missed tactic', 'overlooked defence': 'Overlooked defence', 'opening choice': 'Opening choice' })[value] || 'Personal practice';
    function signals() {
        const tactics = read(tacticsKey, { records: {} }), course = read(courseKey, { lessons: {} }); const records = Object.values(tactics.records || {}), lessons = Object.values(course.lessons || {}), tasks = state.tasks || [];
        $('chessCoachDue').textContent = records.filter(item => item.due && new Date(item.due) <= new Date()).length;
        $('chessCoachLessons').textContent = lessons.length;
        const strong = lessons.filter(item => item.confidence >= 3).length; $('chessCoachImproving').textContent = strong ? `${strong} confident topic${strong === 1 ? '' : 's'}` : 'Build a first lesson streak';
        const counts = tasks.reduce((total, item) => ({ ...total, [item.kind]: (total[item.kind] || 0) + 1 }), {}); const common = Object.entries(counts).sort((a, b) => b[1] - a[1])[0]; $('chessCoachError').textContent = common ? label(common[0]) : 'No reviewed games yet';
    }
    function renderQueue() {
        const host = $('chessCoachQueue'); host.replaceChildren(); const open = (state.tasks || []).filter(item => !item.done);
        if (!open.length) { host.textContent = 'Finish a game, then open its review. Your most useful practice positions will appear here.'; return; }
        open.slice(0, 5).forEach(item => { const button = document.createElement('button'); button.type = 'button'; button.className = 'chess-repertoire-item'; button.innerHTML = `<strong>${label(item.kind)}</strong><span>${item.played} changed the evaluation by about ${(item.loss / 100).toFixed(1)} pawns.</span>`; button.addEventListener('click', () => openTask(item)); host.append(button); });
    }
    async function showBoard(animated = true) { await board.setPosition(position.fen(), animated); }
    async function openTask(item) {
        task = item; position = new Chess(item.fen); board.setOrientation(position.turn() === 'b' ? COLOR.black : COLOR.white); await showBoard(false);
        $('chessCoachKind').textContent = label(item.kind); $('chessCoachTitle').textContent = 'What would you play here?'; $('chessCoachPrompt').textContent = item.kind === 'opening choice' ? 'Try a stronger opening move from your own game.' : 'Find the move that avoids the problem from your reviewed game.'; $('chessCoachFeedback').textContent = ''; $('chessCoachIdea').hidden = false;
    }
    async function boardInput(event) {
        if (!task) return false;
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted || event.type === INPUT_EVENT_TYPE.validateMoveInput) return true;
        if (event.type !== INPUT_EVENT_TYPE.moveInputFinished || !event.legalMove) return;
        const played = `${event.squareFrom}${event.squareTo}`;
        if (played !== task.best) { $('chessCoachFeedback').textContent = 'Try again. Ask for a clue if you want to see the move from the quick review.'; await showBoard(true); return; }
        position.move({ from: task.best.slice(0, 2), to: task.best.slice(2, 4), promotion: task.best[4] || undefined }); await showBoard(true); task.done = true; save(); $('chessCoachFeedback').textContent = 'Good correction. This position came from one of your own reviewed games.'; renderQueue(); signals();
    }
    function init() {
        state = read(coachKey, { tasks: [] }); board = new Chessboard($('chessCoachBoard'), { position: FEN.start, assetsUrl: '/vendor/chess/board/package/assets/', style: { cssClass: 'blue', animationDuration: 220 } }); board.enableMoveInput(boardInput); $('chessCoachIdea').addEventListener('click', () => { if (!task) return; $('chessCoachFeedback').textContent = `Clue: try ${task.best.slice(0, 2)} to ${task.best.slice(2, 4)}. Notice what the move protects or attacks.`; }); signals(); renderQueue(); const next = state.tasks?.find(item => !item.done); if (next) openTask(next);
    }
    init();
}

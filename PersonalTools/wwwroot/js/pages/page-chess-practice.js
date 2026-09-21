import { Chessboard, COLOR, FEN, INPUT_EVENT_TYPE } from '/vendor/chess/board/package/src/Chessboard.js';
import { Chess } from '/vendor/chess/rules/chess.js';

const root = document.querySelector('[data-chess-practice-page]');
if (root) {
    const $ = id => document.getElementById(id);
    const storeKey = 'personaltools.chess.repertoire.v1';
    // Labels and UCI lines are a compact CC0-derived subset of lichess-org/chess-openings.
    const openings = [
        { id: 'italian', name: 'Italian Game', colour: 'white', moves: ['e2e4','e7e5','g1f3','b8c6','f1c4'], plan: 'Develop quickly, castle, and aim pressure at f7.' },
        { id: 'london', name: 'London System', colour: 'white', moves: ['d2d4','d7d5','g1f3','g8f6','c1f4'], plan: 'Build the d4–e3 structure, then develop calmly.' },
        { id: 'queens-gambit', name: "Queen’s Gambit", colour: 'white', moves: ['d2d4','d7d5','c2c4'], plan: 'Challenge Black’s centre and develop behind the c-pawn pressure.' },
        { id: 'sicilian', name: 'Sicilian Defense', colour: 'black', moves: ['e2e4','c7c5','g1f3','d7d6','d2d4','c5d4','f3d4','g8f6'], plan: 'Fight for d4, develop with tempo, and counterattack the centre.' },
        { id: 'caro-kann', name: 'Caro-Kann Defense', colour: 'black', moves: ['e2e4','c7c6','d2d4','d7d5'], plan: 'Challenge the centre with a resilient pawn structure.' }
    ];
    const drills = [
        { id: 'opposition', title: 'King-and-pawn opposition', fen: '8/8/8/8/8/4k3/4P3/4K3 w - - 0 1', solution: 'e1d1', prompt: 'Play Kd1. Keep the opposition so the black king cannot take the pawn.', success: 'Opposition held — Black must yield a key square.' },
        { id: 'queen-mate', title: 'Queen mate', fen: '7k/8/5KQ1/8/8/8/8/8 w - - 0 1', solution: 'g6g7', prompt: 'Deliver mate in one. Use the queen while your king protects it.', success: 'Checkmate. The queen covers the escape squares and the king protects g7.' },
        { id: 'rook-activity', title: 'Active rook', fen: '8/6k1/7p/8/8/8/5K2/R7 w - - 0 1', solution: 'a1a7', prompt: 'Activate the rook with Ra7+, cutting the king off from the seventh rank.', success: 'Active rook achieved — checking from the side gains tempo and limits the king.' }
    ];
    let repertoire, openingBoard, drillBoard, opening = null, openingPosition = null, openingIndex = 0, drill = null, drillPosition = null;
    const uci = (from, to, promotion) => `${from}${to}${promotion || ''}`;
    function load() { try { return JSON.parse(localStorage.getItem(storeKey)) || { white: [], black: [] }; } catch { return { white: [], black: [] }; } }
    function save() { localStorage.setItem(storeKey, JSON.stringify(repertoire)); }
    function move(position, value) { return position.move({ from: value.slice(0, 2), to: value.slice(2, 4), promotion: value[4] || undefined }); }
    function selectOptions(id, colour) { const select = $(id); select.replaceChildren(); openings.filter(item => item.colour === colour).forEach(item => { const option = document.createElement('option'); option.value = item.id; option.textContent = item.name; select.append(option); }); }
    function renderRepertoire() {
        const host = $('chessRepertoire'); host.replaceChildren(); const saved = [...repertoire.white, ...repertoire.black];
        if (!saved.length) { host.textContent = 'Choose a plan above, then practise it on the board.'; return; }
        saved.map(id => openings.find(item => item.id === id)).filter(Boolean).forEach(item => {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'chess-repertoire-item'; button.innerHTML = `<strong>${item.name}</strong><span>${item.colour === 'white' ? 'White' : 'Black'} · ${item.plan}</span>`; button.addEventListener('click', () => startOpening(item)); host.append(button);
        });
    }
    function savePlan(colour) {
        const id = $(colour === 'white' ? 'chessWhiteOpening' : 'chessBlackOpening').value; const list = repertoire[colour];
        if (!list.includes(id)) { if (list.length >= 3) list.shift(); list.push(id); save(); renderRepertoire(); }
        startOpening(openings.find(item => item.id === id));
    }
    function openingTurn() { return opening.moves[openingIndex]; }
    async function setOpeningBoard(animated = true) { await openingBoard.setPosition(openingPosition.fen(), animated); }
    async function playOpeningReply() { if (!openingTurn()) return finishOpening(); move(openingPosition, openingTurn()); openingIndex += 1; await setOpeningBoard(true); $('chessPracticePrompt').textContent = 'Your turn. What is your next planned move?'; }
    function finishOpening() { $('chessPracticePrompt').textContent = opening.plan; $('chessPracticeFeedback').textContent = `Nice work. You finished the ${opening.name} plan.`; }
    async function startOpening(item) {
        if (!item) return; opening = item; openingPosition = new Chess(); openingIndex = 0; $('chessPracticeLabel').textContent = item.name; $('chessPracticeTitle').textContent = item.name; $('chessPracticeFeedback').textContent = ''; openingBoard.setOrientation(item.colour === 'black' ? COLOR.black : COLOR.white); await setOpeningBoard(false);
        if (openingPosition.turn() === (item.colour === 'white' ? 'b' : 'w')) await playOpeningReply(); else $('chessPracticePrompt').textContent = 'Your turn. What is your next planned move?';
    }
    async function openingInput(event) {
        if (!opening) return false;
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted) return true;
        if (event.type === INPUT_EVENT_TYPE.validateMoveInput) return true;
        if (event.type !== INPUT_EVENT_TYPE.moveInputFinished || !event.legalMove) return;
        const played = uci(event.squareFrom, event.squareTo); const expected = openingTurn();
        if (played !== expected) { $('chessPracticeFeedback').textContent = `That is not the move in your saved ${opening.name} plan. Try again, or reset the board.`; setOpeningBoard(true); return; }
        move(openingPosition, expected); openingIndex += 1; await setOpeningBoard(true); $('chessPracticeFeedback').textContent = 'Yes — that is the move in your plan.'; setTimeout(playOpeningReply, 280);
    }
    function renderDrills() {
        const host = $('chessDrillList'); host.replaceChildren(); drills.forEach(item => { const button = document.createElement('button'); button.type = 'button'; button.className = 'chess-repertoire-item'; button.innerHTML = `<strong>${item.title}</strong><span>${item.prompt}</span>`; button.addEventListener('click', () => startDrill(item)); host.append(button); });
    }
    async function setDrillBoard(animated = true) { await drillBoard.setPosition(drillPosition.fen(), animated); }
    async function startDrill(item) { drill = item; drillPosition = new Chess(item.fen); $('chessDrillLabel').textContent = 'Endgame drill'; $('chessDrillTitle').textContent = item.title; $('chessDrillPrompt').textContent = item.prompt; $('chessDrillFeedback').textContent = ''; drillBoard.setOrientation(drillPosition.turn() === 'b' ? COLOR.black : COLOR.white); await setDrillBoard(false); }
    async function drillInput(event) {
        if (!drill) return false;
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted || event.type === INPUT_EVENT_TYPE.validateMoveInput) return true;
        if (event.type !== INPUT_EVENT_TYPE.moveInputFinished || !event.legalMove) return;
        const played = uci(event.squareFrom, event.squareTo);
        if (played !== drill.solution) { $('chessDrillFeedback').textContent = 'Not this move. Read the goal again, then try once more.'; setDrillBoard(true); return; }
        move(drillPosition, played); await setDrillBoard(true); $('chessDrillFeedback').textContent = drill.success;
    }
    async function showDeviations() {
        try {
            const games = await fetch('/api/chess').then(response => response.ok ? response.json() : []); let deviations = 0;
            for (const game of games.slice(0, 12)) { const detail = await fetch(`/api/chess/${game.gameId}`).then(response => response.ok ? response.json() : null); const moves = detail?.moves?.map(item => `${item.from}${item.to}${item.promotion || ''}`) || []; for (const id of [...repertoire.white, ...repertoire.black]) { const line = openings.find(item => item.id === id)?.moves || []; if (moves.length >= 2 && line.some((value, index) => moves[index] && moves[index] !== value)) deviations += 1; } }
            $('chessDeviations').textContent = deviations ? `${deviations} recent game line${deviations === 1 ? '' : 's'} left one of your saved plans early. Choose that plan above to practise it.` : 'Your recent games followed your saved plans well.';
        } catch { $('chessDeviations').textContent = 'Your games could not be checked right now.'; }
    }
    function tab(name) { const openingsTab = name === 'openings'; $('chessOpeningsPractice').hidden = !openingsTab; $('chessEndgamesPractice').hidden = openingsTab; document.querySelectorAll('[data-practice-tab]').forEach(button => { const active = button.dataset.practiceTab === name; button.classList.toggle('active', active); button.setAttribute('aria-selected', String(active)); }); }
    function init() {
        repertoire = load(); selectOptions('chessWhiteOpening', 'white'); selectOptions('chessBlackOpening', 'black');
        openingBoard = new Chessboard($('chessPracticeBoard'), { position: FEN.start, assetsUrl: '/vendor/chess/board/package/assets/', style: { cssClass: 'blue', animationDuration: 220 } }); openingBoard.enableMoveInput(openingInput);
        drillBoard = new Chessboard($('chessDrillBoard'), { position: drills[0].fen, assetsUrl: '/vendor/chess/board/package/assets/', style: { cssClass: 'blue', animationDuration: 220 } }); drillBoard.enableMoveInput(drillInput);
        $('chessSaveWhite').addEventListener('click', () => savePlan('white')); $('chessSaveBlack').addEventListener('click', () => savePlan('black')); $('chessPracticeReset').addEventListener('click', () => startOpening(opening)); $('chessDrillReset').addEventListener('click', () => startDrill(drill)); document.querySelectorAll('[data-practice-tab]').forEach(button => button.addEventListener('click', () => tab(button.dataset.practiceTab))); renderRepertoire(); renderDrills(); startDrill(drills[0]); if (repertoire.white[0] || repertoire.black[0]) startOpening(openings.find(item => item.id === (repertoire.white[0] || repertoire.black[0]))); showDeviations();
    }
    init();
}

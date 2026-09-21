import { Chessboard, COLOR, FEN, INPUT_EVENT_TYPE } from '/vendor/chess/board/package/src/Chessboard.js';
import { Chess } from '/vendor/chess/rules/chess.js';

const root = document.querySelector('[data-chess-page]');
if (root && document.getElementById('chessLearn')) {
    const $ = id => document.getElementById(id);
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const levels = ['beginner', 'intermediate', 'advanced'];
    const courseKey = 'personaltools.chess.course.v1';
    const tacticsKey = 'personaltools.chess.tactics.v1';
    const themeLessons = { fork: 'the-fork', pin: 'the-pin', hangingPiece: 'piece-values', mate: 'check-and-mate', mateIn1: 'check-and-mate', mateIn2: 'check-and-mate', backRankMate: 'back-rank-mate', skewer: 'the-skewer', discoveredAttack: 'discovered-attack', deflection: 'deflection', interference: 'interference', defensiveMove: 'thinking-process' };
    const prerequisites = { 'the-pin': ['the-fork'], 'the-skewer': ['the-pin'], 'discovered-attack': ['the-fork'], 'double-attack': ['the-fork'], 'remove-the-defender': ['piece-values'], 'deflection': ['piece-values'], 'zwischenzug': ['thinking-process'], 'back-rank-mate': ['check-and-mate'] };
    let catalog = [];
    let completed = new Set();
    let level = 'beginner';
    let lesson = null;
    let stepIndex = 0;
    let solved = false;
    let board = null;
    let position = null;
    let pendingMove = null;
    let course = { lessons: {} };
    let masteryChecked = false;
    let confidence = 2;

    function loadCourse() { try { return JSON.parse(localStorage.getItem(courseKey)) || { lessons: {} }; } catch { return { lessons: {} }; } }
    function saveCourse() { localStorage.setItem(courseKey, JSON.stringify(course)); }
    function touch(result) {
        const previous = course.lessons[lesson.id] || {};
        course.lessons[lesson.id] = { ...previous, attempts: (previous.attempts || 0) + (result === 'attempt' ? 1 : 0), hints: (previous.hints || 0) + (result === 'hint' ? 1 : 0), lastPractised: new Date().toISOString(), confidence: confidence || previous.confidence || 2 };
        saveCourse();
    }
    function lessonWhy(item) {
        const map = { 'the-fork': 'it attacks two targets at once, so the defender cannot save both.', 'the-pin': 'the pinned piece cannot move without exposing something more valuable.', 'check-and-mate': 'checks narrow the reply, and checkmate leaves the king no legal escape.', 'back-rank-mate': 'the king’s own pawns take away the escape squares.', 'the-skewer': 'the more valuable piece must move, revealing the piece behind it.', 'discovered-attack': 'moving one piece opens a second line of attack.', 'piece-values': 'trades only help when the material you win is worth more than what you give up.' };
        return map[item.id] || `it applies the lesson’s idea: ${String(item.summary || item.title).toLowerCase()}.`;
    }

    function inlineMarkup(text) {
        const fragment = document.createDocumentFragment();
        for (const segment of String(text).split(/(\*\*[^*]+\*\*|\{\{[^}]+\}\})/g)) {
            if (segment.startsWith('**') && segment.endsWith('**')) {
                const strong = document.createElement('strong'); strong.textContent = segment.slice(2, -2); fragment.append(strong);
            } else if (segment.startsWith('{{') && segment.endsWith('}}')) {
                const code = document.createElement('code'); code.textContent = segment.slice(2, -2); fragment.append(code);
            } else fragment.append(document.createTextNode(segment));
        }
        return fragment;
    }
    function renderCopy(value) {
        const host = $('chessStepText'); host.replaceChildren();
        const lines = String(value || '').split('\n');
        let list = null;
        for (const raw of lines) {
            const line = raw.trim();
            if (!line) { list = null; continue; }
            if (line.startsWith('- ')) {
                if (!list) { list = document.createElement('ul'); host.append(list); }
                const item = document.createElement('li'); item.append(inlineMarkup(line.slice(2))); list.append(item); continue;
            }
            list = null;
            const element = document.createElement(line.startsWith('## ') ? 'h5' : line.startsWith('> ') ? 'blockquote' : 'p');
            element.append(inlineMarkup(line.replace(/^(## |> )/, '')));
            host.append(element);
        }
    }
    function clearHighlights() {
        $('chessLessonBoard').querySelectorAll('.chess-lesson-source,.chess-lesson-target')
            .forEach(square => square.classList.remove('chess-lesson-source', 'chess-lesson-target'));
    }
    function markSquare(square, className) {
        $('chessLessonBoard').querySelector(`.board .square[data-square="${square}"]`)?.classList.add(className);
    }
    function onBoardInput(event) {
        const step = lesson?.steps[stepIndex];
        if (!step || step.type !== 'play' || solved) return false;
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted) {
            const moves = position.moves({ square: event.squareFrom, verbose: true });
            if (!moves.length) return false;
            clearHighlights(); markSquare(event.squareFrom, 'chess-lesson-source');
            moves.forEach(move => markSquare(move.to, 'chess-lesson-target'));
            return true;
        }
        if (event.type === INPUT_EVENT_TYPE.validateMoveInput) {
            const options = position.moves({ square: event.squareFrom, verbose: true });
            pendingMove = options.find(move => move.to === event.squareTo && step.targetSan.includes(move.san)) || null;
            if (!pendingMove) { touch('attempt'); $('chessStepFeedback').textContent = 'Not quite. Look again, or tap “Hint” for help.'; }
            return !!pendingMove;
        }
        if (event.type === INPUT_EVENT_TYPE.moveInputCanceled) clearHighlights();
        if (event.type === INPUT_EVENT_TYPE.moveInputFinished) {
            clearHighlights();
            if (!event.legalMove || !pendingMove) return;
            position.move(pendingMove);
            board.setPosition(position.fen(), true);
            pendingMove = null; solved = true;
            touch('attempt');
            $('chessStepFeedback').textContent = `Correct — ${lessonWhy(lesson)}`;
            $('chessLessonNext').disabled = false;
        }
    }
    function lessonsForLevel() { return catalog.filter(item => item.difficulty === level); }
    function renderProgress() {
        const practised = Object.keys(course.lessons).length;
        $('chessLearnProgress').textContent = `${completed.size} of ${catalog.length} completed · ${practised} practised`;
    }
    function renderLevels() {
        const host = $('chessLearnLevels'); host.replaceChildren();
        for (const value of levels) {
            const button = document.createElement('button'); button.type = 'button';
            button.classList.toggle('active', value === level);
            button.setAttribute('aria-pressed', String(value === level));
            button.textContent = `${value[0].toUpperCase()}${value.slice(1)} · ${catalog.filter(item => item.difficulty === value).length}`;
            button.addEventListener('click', () => { level = value; lesson = null; renderLevels(); renderList(); selectLesson(lessonsForLevel()[0]); });
            host.append(button);
        }
    }
    function renderList() {
        const host = $('chessLessonList'); host.replaceChildren();
        for (const item of lessonsForLevel()) {
            const button = document.createElement('button'); button.type = 'button';
            button.classList.toggle('active', item.id === lesson?.id);
            button.setAttribute('aria-current', item.id === lesson?.id ? 'step' : 'false');
            const title = document.createElement('span'); title.textContent = item.title;
            const tag = document.createElement('span'); tag.textContent = completed.has(item.id) ? '✓ Done' : item.category;
            button.append(title, tag);
            button.addEventListener('click', () => selectLesson(item)); host.append(button);
        }
    }
    function missedPuzzleLesson() {
        try {
            const tactics = JSON.parse(localStorage.getItem(tacticsKey) || '{}');
            const record = Object.values(tactics.records || {}).find(item => item.missed && Array.isArray(item.themes) && item.themes.some(theme => themeLessons[theme]));
            const theme = record?.themes?.find(value => themeLessons[value]);
            return theme ? { lesson: catalog.find(item => item.id === themeLessons[theme]), reason: `A recent ${theme.replace(/([A-Z])/g, ' $1')} puzzle was difficult. Learn the pattern, then practise it again.` } : null;
        } catch { return null; }
    }
    function renderContinue() {
        const host = $('chessContinueLearning'); if (!catalog.length) return;
        const due = catalog.find(item => course.lessons[item.id]?.due && new Date(course.lessons[item.id].due) <= new Date());
        const missed = missedPuzzleLesson();
        const target = due || missed?.lesson || catalog.find(item => !completed.has(item.id)) || catalog[0];
        const reason = due ? 'A short revisit will help this idea stay fresh.' : missed?.lesson === target ? missed.reason : completed.has(target.id) ? 'A short revisit keeps this idea fresh.' : `This is a good next step before the lessons that build on it.`;
        const required = (prerequisites[target.id] || []).map(id => catalog.find(item => item.id === id)).find(item => item && !completed.has(item.id));
        $('chessContinueTitle').textContent = target.title; $('chessContinueReason').textContent = reason;
        $('chessPrerequisite').hidden = !required; if (required) $('chessPrerequisite').textContent = `Suggested first: ${required.title} — it makes this lesson easier to understand.`;
        $('chessContinueButton').onclick = () => { const open = required || target; level = open.difficulty; renderLevels(); selectLesson(open); $('chessLearn').scrollIntoView({ behavior: 'smooth', block: 'start' }); };
        host.hidden = false;
    }
    function selectLesson(item) {
        if (!item) return;
        lesson = item; stepIndex = 0; masteryChecked = false; confidence = course.lessons[item.id]?.confidence || 2;
        $('chessLessonPanel').hidden = false;
        $('chessLessonCategory').textContent = `${item.category} · ${item.difficulty}`;
        $('chessLessonTitle').textContent = item.title;
        if (!board) {
            board = new Chessboard($('chessLessonBoard'), {
                position: FEN.start,
                assetsUrl: '/vendor/chess/board/package/assets/',
                style: { cssClass: 'blue', animationDuration: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 220 }
            });
            board.enableMoveInput(onBoardInput);
        }
        renderList(); showStep();
    }
    function startMasteryCheck() {
        masteryChecked = false;
        const host = $('chessLessonMastery'); host.hidden = false;
        $('chessMasteryQuestion').textContent = `What is the main idea of “${lesson.title}”?`;
        const choices = [lesson.summary || lesson.title, 'Ignore the other player’s threats', 'Move without first looking at the board'];
        const options = $('chessMasteryOptions'); options.replaceChildren();
        choices.forEach((choice, index) => {
            const button = document.createElement('button'); button.type = 'button'; button.textContent = choice;
            button.addEventListener('click', () => {
                if (index !== 0) { $('chessMasteryFeedback').textContent = 'Almost. Think about the main idea from this lesson, then try again.'; return; }
                masteryChecked = true; $('chessMasteryFeedback').textContent = `Exactly — ${lessonWhy(lesson)}`; $('chessConfidence').hidden = false; $('chessLessonNext').disabled = false; $('chessLessonNext').textContent = 'Complete lesson';
            }); options.append(button);
        });
        $('chessMasteryFeedback').textContent = ''; $('chessConfidence').hidden = true; $('chessLessonNext').disabled = true;
    }
    function queuePractice() {
        const theme = Object.entries(themeLessons).find(([, id]) => id === lesson.id)?.[0]; if (!theme) return;
        try {
            const tactics = JSON.parse(localStorage.getItem(tacticsKey) || '{}'); tactics.practiceThemes ||= {}; tactics.sessions = {}; tactics.practiceThemes[theme] = (tactics.practiceThemes[theme] || 0) + 2; localStorage.setItem(tacticsKey, JSON.stringify(tactics));
        } catch { /* The lesson remains completed if local practice data is unavailable. */ }
    }
    function showStep() {
        const step = lesson.steps[stepIndex];
        solved = step.type !== 'play'; pendingMove = null; clearHighlights();
        position = new Chess(step.fen, { skipValidation: true });
        board.setOrientation(position.turn() === 'b' ? COLOR.black : COLOR.white);
        board.setPosition(step.fen);
        $('chessLessonStepCount').textContent = `Step ${stepIndex + 1} of ${lesson.steps.length}`;
        $('chessStepTitle').textContent = step.title || (step.type === 'play' ? 'Your turn' : 'Explore the position');
        renderCopy(step.text || step.prompt);
        $('chessStepFeedback').textContent = step.type === 'play' ? 'Your turn: make the move on the board.' : '';
        $('chessLessonHint').hidden = step.type !== 'play';
        $('chessLessonBack').disabled = stepIndex === 0;
        $('chessLessonNext').disabled = !solved;
        $('chessLessonNext').textContent = stepIndex === lesson.steps.length - 1 ? 'Finish lesson' : 'Next step';
        $('chessLessonMastery').hidden = true;
    }
    async function advance() {
        if (!lesson || !solved) return;
        if (stepIndex < lesson.steps.length - 1) { stepIndex++; showStep(); return; }
        if (!masteryChecked) { startMasteryCheck(); return; }
        $('chessLessonNext').disabled = true;
        try {
            await window.jQuery.ajax({ url: `/api/chess/lessons/${encodeURIComponent(lesson.id)}/complete`, method: 'POST',
                headers: { RequestVerificationToken: token }, showLoader: false, showToast: false });
            completed.add(lesson.id);
            const entry = course.lessons[lesson.id] || {}; const days = confidence === 1 ? 1 : confidence === 2 ? 3 : 7;
            course.lessons[lesson.id] = { ...entry, lastPractised: new Date().toISOString(), confidence, due: new Date(Date.now() + days * 86400000).toISOString() }; saveCourse(); queuePractice(); renderProgress(); renderList(); renderContinue();
            const current = catalog.indexOf(lesson);
            const next = catalog[current + 1];
            window.personalToolsToast?.success('Lesson completed.');
            if (next) { level = next.difficulty; renderLevels(); selectLesson(next); }
            else $('chessStepFeedback').textContent = 'Course complete — excellent work!';
        } catch { $('chessStepFeedback').textContent = 'Progress could not be saved. Please try again.'; }
        finally { $('chessLessonNext').disabled = false; }
    }
    $('chessLessonBack').addEventListener('click', () => { if (lesson && stepIndex > 0) { stepIndex--; showStep(); } });
    $('chessLessonNext').addEventListener('click', advance);
    $('chessLessonHint').addEventListener('click', () => {
        const step = lesson?.steps[stepIndex]; if (!step?.hint) return;
        touch('hint');
        $('chessStepFeedback').textContent = step.hint;
        const target = position.moves({ verbose: true }).find(move => step.targetSan.includes(move.san));
        if (target) { clearHighlights(); markSquare(target.from, 'chess-lesson-source'); markSquare(target.to, 'chess-lesson-target'); }
    });
    $('chessConfidence').querySelectorAll('[data-confidence]').forEach(button => button.addEventListener('click', () => {
        confidence = Number(button.dataset.confidence); $('chessConfidence').querySelectorAll('button').forEach(item => item.classList.toggle('active', item === button));
        touch('confidence');
    }));
    async function init() {
        try {
            const response = await fetch('/vendor/chess/tutorials/lessons.json', { cache: 'force-cache' });
            if (!response.ok) throw new Error('Lesson content unavailable.');
            const data = await response.json();
            catalog = data.categories.flatMap(category => category.lessons.map(item => ({ ...item, category: category.id })))
                .sort((a, b) => levels.indexOf(a.difficulty) - levels.indexOf(b.difficulty));
            course = loadCourse();
            let progressLoaded = false;
            try {
                completed = new Set(await window.jQuery.ajax({ url: '/api/chess/lessons/completed', showLoader: false, showToast: false }));
                progressLoaded = true;
            } catch { $('chessLearnProgress').textContent = 'Progress unavailable until the chess database update is applied.'; }
            if (progressLoaded) renderProgress(); else renderProgress();
            renderLevels(); renderContinue(); selectLesson(lessonsForLevel()[0]);
        } catch (error) { $('chessLearnProgress').textContent = error.message || 'Lessons could not load.'; }
    }
    init();
}

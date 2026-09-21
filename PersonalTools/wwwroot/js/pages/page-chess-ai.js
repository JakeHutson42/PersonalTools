import { Chess } from '../../vendor/chess/rules/chess.js';

const legacyLevels = { 1: 300, 2: 600, 3: 1000, 4: 1400 };

export function approximateElo(savedDifficulty) {
    return legacyLevels[savedDifficulty] ||
        (Number.isInteger(savedDifficulty) && savedDifficulty >= 100 && savedDifficulty <= 2400 && savedDifficulty % 100 === 0
            ? savedDifficulty : 500);
}

// Stockfish's UCI_Elo has a floor above beginner strength. Below 1400 we
// occasionally pick a random legal move instead, with the frequency falling
// at each 100-point step. These labels are intentionally approximate ratings.
export function beginnerMove(history, elo, random = Math.random) {
    if (elo >= 1400 || random() < .85 * (elo - 100) / 1200) return null;
    const position = new Chess();
    for (const played of history) {
        if (!position.move({ from: played.from, to: played.to, promotion: played.promotion || undefined })) return null;
    }
    const legal = position.moves({ verbose: true });
    if (!legal.length) return null;
    const choice = legal[Math.floor(random() * legal.length)];
    return { from: choice.from, to: choice.to, promotion: choice.promotion || null };
}

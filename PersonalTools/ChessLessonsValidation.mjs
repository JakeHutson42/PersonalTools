import { readFileSync } from 'node:fs';
import { Chess } from './wwwroot/vendor/chess/rules/chess.js';

const catalog = JSON.parse(readFileSync(new URL('./wwwroot/vendor/chess/tutorials/lessons.json', import.meta.url)));
let lessons = 0;
let exercises = 0;
const errors = [];
for (const category of catalog.categories) {
    for (const lesson of category.lessons) {
        lessons++;
        for (const [index, step] of lesson.steps.entries()) {
            if (step.type !== 'play') continue;
            exercises++;
            try {
                const chess = new Chess(step.fen, { skipValidation: true });
                const legal = chess.moves({ verbose: true });
                if (!step.targetSan?.some(target => legal.some(move => move.san === target)))
                    errors.push(`${lesson.id} step ${index + 1}: no target SAN is legal (${step.targetSan?.join(', ')})`);
            } catch (error) {
                errors.push(`${lesson.id} step ${index + 1}: ${error.message}`);
            }
        }
    }
}
console.log(`${lessons} lessons; ${exercises} exercises; ${errors.length} validation findings`);
errors.forEach(error => console.error(error));
if (errors.length) process.exitCode = 1;

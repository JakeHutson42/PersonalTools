# Chess development plan

## Goal and scope

Add an on-site chess page for signed-in PersonalTools users. The first pass supports human-vs-human games and casual human-vs-AI games, saved and resumable positions, legal moves, game endings, and responsive animated play.

## Decisions

- Use the existing Razor Pages layout, authentication, MySQL, and SignalR infrastructure. Chess is a separate feature from case-battle currency and rewards.
- Use `cm-chessboard` for the board UI and `Gera.Chess` on the server for authoritative rules and FEN/PGN handling.
- Use the lightweight single-threaded Stockfish.js WebAssembly worker in the browser for casual AI. AI games have no rewards or competitive ranking; the server still validates all submitted moves.
- Persist participants, status, FEN, move history, and last activity in MariaDB via the standard Data layer and stored procedures. The move procedure locks the game and rejects stale positions and out-of-turn moves.
- A PvP creator is White. The other signed-in user joins as Black using an unguessable invitation link; this is a link invitation, not a new inbox notification. AI games use the creator as White and browser Stockfish as Black.
- Use casual, approximate Elo-style AI choices from 100 to 2400 in 100-point steps, with no currency, rewards, or competitive result claims. The server still rejects illegal AI moves.
- Real-time events are notifications; every browser can reload authoritative game state after reconnect.
- Keep a move log with from/to/promotion/captured-piece information so a later capture animation can be added without changing storage.
- Ship the required open-source licences and source notices for bundled assets, especially Stockfish.js GPLv3 and any selected chess-piece artwork.
- Reuse the site's bundled Anime.js and the board's existing SVG sprite for a restrained folding-door introduction, piece deployment, and capture effect. No additional animation package or image asset is required. Skip the introduction and suppress nonessential motion when the user prefers reduced motion.
- Use a pinned, locally served MIT-licensed KarpaChess lesson catalog for on-site beginner, intermediate, and advanced learning. Use bundled BSD-licensed chess.js for interactive exercise validation; persist only completion records per user.
- Show the opponent's latest move on the board. In AI games, use a separate low-priority Stockfish worker to review the player's last move after the reply, so review never delays play. Classifications are approximate, not a formal coach.
- Support unlimited AI-game undo back to the start. Each undo removes an unanswered player move or the player's move plus the computer reply. A separate monotonic revision guards against stale requests when the move count decreases. PvP undo is deferred because it needs both players' consent.

## Development steps

1. [x] Confirm repository state and integration points without changing existing work.
2. [x] Add dependency references and locally served board/engine assets with licence notices.
3. [x] Add database migration for chess games and moves, including ownership and concurrency constraints (script written; not applied to a live database).
4. [x] Add server-side chess service and API: create AI/PvP game, link invite/join, list/resume, move, resign, and game status.
5. [x] Add a participant-authorized SignalR chess hub that broadcasts changes only to game members.
6. [x] Add the Razor page, dashboard/sidebar links, board UI, game list, opponent controls, and responsive styles (browser visual QA remains).
7. [x] Add browser Stockfish worker orchestration and difficulty settings for AI games (Node engine smoke test passes; browser QA remains).
8. [ ] Add tests for legal moves, wrong-turn/stale requests, authorization, game endings, and AI response integration. Five chess service tests pass; database and browser integration tests remain.
9. [ ] Build and manually verify new game, two browsers, refresh/reconnect, AI, promotion, castling, en passant, mate, and draw. Build and JavaScript syntax checks pass; live database/browser verification remains.
10. [x] Update this document with results and remaining work.
11. [x] Add on-site beginner, intermediate, and advanced lessons, interactive exercises, hints, and per-user completion progress.
12. [x] Highlight the opponent's last move and review the player's last AI-game move without blocking the AI reply.
13. [x] Add unlimited AI-game undo with move-history replay, an atomic stored procedure, and stale-request protection.

## Progress

- The plan was written before implementation. Existing user changes in `Classes/CaseBattles/CaseBattleFuncs.cs` were not edited.
- Added `Gera.Chess` 1.2.0, `cm-chessboard` 8.14.0, and Stockfish.js 19 lite-single. Only required browser assets and their licence files were extracted into `wwwroot/vendor/chess`.
- Bundled all SQL changes made on 2026-09-20 into the single runnable `DatabaseUpdates/2026-09-20-chess.sql` file: `ChessGames`, `ChessMoves`, and all scoped stored procedures. No other SQL migration was created today. This bundle has **not** been applied to any database.
- Added chess service, data layer, API, hub, page, styles, and page script. The dashboard and sidebar now link to Chess.
- `dotnet build` succeeded with zero warnings/errors. `node --check` passed for the page script. The Stockfish binary returned a legal `bestmove` in a Node smoke test.
- Five chess service tests pass. The full suite has 39 passes and one unrelated failure in `CaseBattleModesTests.ImplementedSoloModes_HaveACompleteSettlementPath` (its case-battle mode expectation is out of date).
- Follow-up polish: the server now returns legal destinations only to the player whose turn it is. Tapping/clicking a piece highlights available squares (captures in a distinct colour), and the client rejects other destinations before sending a move. Promotion uses a Bootstrap piece picker rather than a text prompt. A turn badge now identifies the active player and colour. A fifth chess test covers the turn-scoped legal-move list.
- The board now sizes against both its card width and viewport height, with compact controls at narrow widths and short-landscape sizing. The board library's ResizeObserver redraws the SVG as its container changes.
- Added a wooden folding-door reveal when a new unplayed game opens, followed by pieces deploying from the center to their squares. The sequence is skippable, runs only once per game per page session, and is disabled for reduced motion. Captures now show the removed piece dissolving into a ring and sparks; en passant locates the captured pawn on its actual square.
- After this polish pass, `dotnet build --no-restore` succeeds with zero warnings/errors, both chess JavaScript modules pass `node --check`, `git diff --check` passes, and all five focused chess service tests pass. Visual QA is still pending because the live database-backed page is not available here.
- Live-use follow-up: move POSTs no longer display the global loading overlay, the Stockfish worker starts warming as soon as an AI game opens, and each AI position starts at most one search/submission. The games list and numbered move rows now use theme-aware styling, including a clear active game and latest move. Chess moves have a separate per-user write budget (90/minute) instead of sharing the 12/minute case-battle budget. Public/guest pages no longer poll the registered-user-only case-battle invitation endpoint, and polling stops after an authentication rejection. Build, JavaScript syntax checks, and all five focused chess tests pass; browser verification remains.
- Learning and review pass: added 71 pinned KarpaChess lessons across three levels, including 54 validated interactive move exercises, with hints and saved completion. The separate learning board stays on this site. The game board marks the opponent's last move. AI games have an unlimited Undo turn button and a non-blocking, approximate Stockfish move-strength review. The single 2026-09-20 SQL bundle now also includes `Revision`, `ChessLessonProgress`, and their procedures; it remains unapplied and should be rerun in full if an earlier version was applied.
- Seven focused chess service tests pass, including repeated undo and stale-revision cases. All page JavaScript syntax checks and the 71-lesson/54-exercise catalog validation pass. The full suite currently has 38 passes and four unrelated failures: one pre-existing case-battle mode expectation, and three social database contract tests whose source-file lookup fails when the test output is redirected away from the running app's locked build directory.
- Move-error follow-up: a reported log showed the app running against an older schema without `Revision`. The local `PersonalTools` MariaDB instance now has that column, the undo procedure, and lesson table. An isolated temporary game successfully applied White's first move and the AI reply through the installed move procedure, then was removed. The client now retries one failed AI submission from a fresh engine/state and shows the server's actual error if it still fails; stale-position failures return HTTP 409 rather than 400. Focused chess tests and JavaScript syntax checks pass. The bundled SQL remains the deployment source of truth; other environments still need checking.
- HeidiSQL import follow-up: the single bundle now uses HeidiSQL's documented `!!` query delimiter rather than `//`, and explicitly instructs running the entire file in a new query tab with F9. All ten procedures have distinct parameter names and matching drop/create/terminator counts. The stored SQL bodies were not changed by this delimiter correction.
- Console follow-up: a running site served the new `page-chess.js` while its older compiled Razor page lacked `#chessUndo`, causing a `null.addEventListener` crash and stopping chess initialization. Undo, analysis, and lessons now tolerate that temporary asset/page mismatch. Restarting the app is still required to render the new controls. Visual Studio BrowserLink/live-reload CSP warnings and the install-banner notice are development/browser tooling messages, not chess application failures; the site's CSP was not weakened to accommodate them.
- Board clarity follow-up: replaced full-square green legal-move fills with compact dots for empty destinations and outlined rings for captures, plus a source-piece ring. A checkmate result now has a prominent win/loss banner and a red ring around the mated king. The saved SAN checkmate suffix is covered by a focused test. Browser visual QA remains.
- Difficulty and history follow-up: captures now display the actual captured piece from the bundled SVG sprite beside the move notation. New AI games offer approximate 100–2400 Elo choices in 100-point steps, defaulting to 500. The SQL bundle widens `Difficulty` to `SMALLINT` and accepts the new values while retaining old 1–4 games. Below Stockfish's built-in Elo floor, a graded mixture of engine moves and random legal moves makes the low settings materially easier; the numbers are not calibrated competitive ratings. Higher settings use Stockfish's UCI Elo limiter. Apply/rerun the updated SQL bundle before creating a new-range AI game in any environment.
- Move-review follow-up: the AI-game feedback now names Stockfish's suggested move in SAN and offers a preview button when it differs from the player's move. The preview reconstructs the position before the player move with chess.js, applies the suggestion locally, disables move input, and can restore the authoritative saved position without submitting any move. Live game updates also dismiss previews. This is shallow approximate analysis, not an exhaustive proof of the best move.
- Endgame clarity follow-up: added rules-derived explanations for checkmate, stalemate, repetition, fifty-move draws, and insufficient material. Mate and stalemate show board arrows from pieces controlling the king's square/escape squares and rings for blocked escapes; stalemate explicitly says the king is not in check. Other draw types explain the real reason without inventing a king attack. A reduced-motion-aware result sequence animates a falling king for mate or a draw flourish, then opens an accessible result prompt with AI Undo last turn and Review board actions. Outcome overlays are cleared on game changes and suppressed during suggested-move previews. Browser visual QA remains.

## Still to do before enabling in production

1. Verify the complete `DatabaseUpdates/2026-09-20-chess.sql` bundle in every deployment environment and test with two real user accounts. The local database now has the required schema and its move procedure passed an isolated AI-turn test; staging/production have not been verified here.
2. Browser-test AI and two-player games, promotion, castling, en passant, checkmate, draw, refresh/reconnect, light/dark themes, mobile width, short landscape, keyboard input, intro skip/reduced motion, capture timing, last-move highlighting, tutorial exercise input/progress, strength review, and repeated undo. Resolve any UI or integration findings.
3. Review GPLv3 compliance for the distributed Stockfish.js worker and include corresponding source/distribution information for the exact pinned build. The bundled licence and public source link are present, but that review is not complete.
4. Decide whether the first release needs in-app invitation notifications or whether the shareable link is sufficient.

## Deferred

- Sound is deferred; the capture animation is visual only.
- PvP undo is deferred pending a mutual-consent design.
- Clocks, matchmaking, ratings, leaderboards, spectators, and rewards.

## Risks and checks

- Browser AI can be manipulated; acceptable for casual no-reward play.
- GPLv3 distribution requirements for Stockfish.js must be reviewed before shipping the bundled worker.
- Piece-art licences may differ from board-code licences.
- Database migration must be applied to every deployed environment before enabling the page.

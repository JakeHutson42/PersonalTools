-- Player-facing battle pacing is deliberately stored with the feature settings so operators can
-- tune the presentation without a deployment. Values are milliseconds and are bounded again by
-- the application before they are written.
USE PersonalTools;

ALTER TABLE CaseOpeningBattleBotSettings
    ADD COLUMN IF NOT EXISTS ReadyPauseMs INT NOT NULL DEFAULT 900,
    ADD COLUMN IF NOT EXISTS ReadyCountdownMs INT NOT NULL DEFAULT 3000,
    ADD COLUMN IF NOT EXISTS SpinDurationMs INT NOT NULL DEFAULT 5200,
    ADD COLUMN IF NOT EXISTS RoundRevealPauseMs INT NOT NULL DEFAULT 1650,
    ADD COLUMN IF NOT EXISTS ResultsPauseMs INT NOT NULL DEFAULT 1100,
    ADD COLUMN IF NOT EXISTS WinnerIntroPauseMs INT NOT NULL DEFAULT 1900,
    ADD COLUMN IF NOT EXISTS WinnerTallyDurationMs INT NOT NULL DEFAULT 2600,
    ADD COLUMN IF NOT EXISTS WinnerVerdictPauseMs INT NOT NULL DEFAULT 1450,
    ADD COLUMN IF NOT EXISTS WinnerTransferDurationMs INT NOT NULL DEFAULT 2550;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_timing_settings_get//
CREATE PROCEDURE sp_case_battles_timing_settings_get()
BEGIN
    SELECT ReadyPauseMs,ReadyCountdownMs,SpinDurationMs,RoundRevealPauseMs,ResultsPauseMs,
           WinnerIntroPauseMs,WinnerTallyDurationMs,WinnerVerdictPauseMs,WinnerTransferDurationMs
    FROM CaseOpeningBattleBotSettings WHERE SettingsId=1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_timing_settings_set//
CREATE PROCEDURE sp_case_battles_timing_settings_set(
    IN p_ready_pause_ms INT,IN p_ready_countdown_ms INT,IN p_spin_duration_ms INT,
    IN p_round_reveal_pause_ms INT,IN p_results_pause_ms INT,IN p_winner_intro_pause_ms INT,
    IN p_winner_tally_duration_ms INT,IN p_winner_verdict_pause_ms INT,IN p_winner_transfer_duration_ms INT)
BEGIN
    UPDATE CaseOpeningBattleBotSettings SET
        ReadyPauseMs=p_ready_pause_ms,ReadyCountdownMs=p_ready_countdown_ms,SpinDurationMs=p_spin_duration_ms,
        RoundRevealPauseMs=p_round_reveal_pause_ms,ResultsPauseMs=p_results_pause_ms,WinnerIntroPauseMs=p_winner_intro_pause_ms,
        WinnerTallyDurationMs=p_winner_tally_duration_ms,WinnerVerdictPauseMs=p_winner_verdict_pause_ms,
        WinnerTransferDurationMs=p_winner_transfer_duration_ms,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE SettingsId=1;
END//
DELIMITER ;

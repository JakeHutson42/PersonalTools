-- The selection limit is operationally configurable, but bounded in the application to 1–50.
USE PersonalTools;
ALTER TABLE CaseOpeningBattleBotSettings ADD COLUMN IF NOT EXISTS MaxCasesPerBattle INT NOT NULL DEFAULT 20;
-- Move untouched phase-17 defaults to the more deliberate reveal pace without overwriting an
-- administrator's existing custom pacing.
UPDATE CaseOpeningBattleBotSettings
SET WinnerIntroPauseMs=2500,WinnerTallyDurationMs=4200,WinnerVerdictPauseMs=1800,WinnerTransferDurationMs=3500
WHERE SettingsId=1 AND WinnerIntroPauseMs=1900 AND WinnerTallyDurationMs=2600
  AND WinnerVerdictPauseMs=1450 AND WinnerTransferDurationMs=2550;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_timing_settings_get//
CREATE PROCEDURE sp_case_battles_timing_settings_get()
BEGIN
    SELECT MaxCasesPerBattle,ReadyPauseMs,ReadyCountdownMs,SpinDurationMs,RoundRevealPauseMs,ResultsPauseMs,
           WinnerIntroPauseMs,WinnerTallyDurationMs,WinnerVerdictPauseMs,WinnerTransferDurationMs
    FROM CaseOpeningBattleBotSettings WHERE SettingsId=1;
END//
DROP PROCEDURE IF EXISTS sp_case_battles_timing_settings_set//
CREATE PROCEDURE sp_case_battles_timing_settings_set(
    IN p_max_cases_per_battle INT,IN p_ready_pause_ms INT,IN p_ready_countdown_ms INT,IN p_spin_duration_ms INT,
    IN p_round_reveal_pause_ms INT,IN p_results_pause_ms INT,IN p_winner_intro_pause_ms INT,
    IN p_winner_tally_duration_ms INT,IN p_winner_verdict_pause_ms INT,IN p_winner_transfer_duration_ms INT)
BEGIN
    UPDATE CaseOpeningBattleBotSettings SET MaxCasesPerBattle=p_max_cases_per_battle,
        ReadyPauseMs=p_ready_pause_ms,ReadyCountdownMs=p_ready_countdown_ms,SpinDurationMs=p_spin_duration_ms,
        RoundRevealPauseMs=p_round_reveal_pause_ms,ResultsPauseMs=p_results_pause_ms,WinnerIntroPauseMs=p_winner_intro_pause_ms,
        WinnerTallyDurationMs=p_winner_tally_duration_ms,WinnerVerdictPauseMs=p_winner_verdict_pause_ms,
        WinnerTransferDurationMs=p_winner_transfer_duration_ms,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE SettingsId=1;
END//
DELIMITER ;

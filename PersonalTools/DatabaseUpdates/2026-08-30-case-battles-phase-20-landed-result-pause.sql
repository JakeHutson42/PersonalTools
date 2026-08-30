-- Keeps each landed skin readable before the next case enters the reel.
USE PersonalTools;

ALTER TABLE CaseOpeningBattleBotSettings
    ADD COLUMN IF NOT EXISTS LandedResultPauseMs INT NOT NULL DEFAULT 750;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_timing_settings_get//
CREATE PROCEDURE sp_case_battles_timing_settings_get()
BEGIN
    SELECT MaxCasesPerBattle,ReadyPauseMs,ReadyCountdownMs,SpinDurationMs,LandedResultPauseMs,
           RoundRevealPauseMs,ResultsPauseMs,WinnerIntroPauseMs,WinnerTallyDurationMs,
           WinnerVerdictPauseMs,WinnerTransferDurationMs
    FROM CaseOpeningBattleBotSettings WHERE SettingsId=1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_timing_settings_set//
CREATE PROCEDURE sp_case_battles_timing_settings_set(
    IN p_max_cases_per_battle INT,IN p_ready_pause_ms INT,IN p_ready_countdown_ms INT,
    IN p_spin_duration_ms INT,IN p_landed_result_pause_ms INT,IN p_round_reveal_pause_ms INT,
    IN p_results_pause_ms INT,IN p_winner_intro_pause_ms INT,IN p_winner_tally_duration_ms INT,
    IN p_winner_verdict_pause_ms INT,IN p_winner_transfer_duration_ms INT)
BEGIN
    UPDATE CaseOpeningBattleBotSettings SET
        MaxCasesPerBattle=p_max_cases_per_battle,ReadyPauseMs=p_ready_pause_ms,
        ReadyCountdownMs=p_ready_countdown_ms,SpinDurationMs=p_spin_duration_ms,
        LandedResultPauseMs=p_landed_result_pause_ms,RoundRevealPauseMs=p_round_reveal_pause_ms,
        ResultsPauseMs=p_results_pause_ms,WinnerIntroPauseMs=p_winner_intro_pause_ms,
        WinnerTallyDurationMs=p_winner_tally_duration_ms,WinnerVerdictPauseMs=p_winner_verdict_pause_ms,
        WinnerTransferDurationMs=p_winner_transfer_duration_ms,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE SettingsId=1;
END//
DELIMITER ;

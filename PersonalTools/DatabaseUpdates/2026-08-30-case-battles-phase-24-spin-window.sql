-- SpinDurationMs now represents the complete per-round window. The browser reserves its final
-- 1000ms for the landed result, so constrain existing and future administrator values to 3–5s.
USE PersonalTools;

UPDATE CaseOpeningBattleBotSettings
SET SpinDurationMs=LEAST(5000,GREATEST(3000,SpinDurationMs)),UpdatedUtc=UTC_TIMESTAMP(6)
WHERE SettingsId=1;

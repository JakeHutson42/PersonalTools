-- Phase 4: EV-ranked tiers and configurable profit targets used by the draft balance engine.
-- Upgrade only the original seeded curve; rerunning this migration must not overwrite later
-- administrator adjustments.
UPDATE CaseOpeningTierEconomySettings
SET TargetProfitBasisPoints=Tier*200,UpdatedUtc=UTC_TIMESTAMP(6)
WHERE TargetProfitBasisPoints=CASE Tier
    WHEN 1 THEN 0 WHEN 2 THEN 200 WHEN 3 THEN 300 WHEN 4 THEN 400 WHEN 5 THEN 500
    WHEN 6 THEN 600 WHEN 7 THEN 700 WHEN 8 THEN 800 WHEN 9 THEN 900 WHEN 10 THEN 1000 END;

INSERT IGNORE INTO CaseOpeningTierEconomySettings(Tier,TargetProfitBasisPoints,PriceRoundingPence,UpdatedUtc) VALUES
    (1,200,1,UTC_TIMESTAMP(6)),(2,400,5,UTC_TIMESTAMP(6)),(3,600,5,UTC_TIMESTAMP(6)),
    (4,800,5,UTC_TIMESTAMP(6)),(5,1000,10,UTC_TIMESTAMP(6)),(6,1200,10,UTC_TIMESTAMP(6)),
    (7,1400,25,UTC_TIMESTAMP(6)),(8,1600,25,UTC_TIMESTAMP(6)),(9,1800,50,UTC_TIMESTAMP(6)),
    (10,2000,100,UTC_TIMESTAMP(6));

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_tier_economy_settings_get//
CREATE PROCEDURE sp_case_opening_tier_economy_settings_get()
BEGIN
    SELECT Tier,TargetProfitBasisPoints,PriceRoundingPence
    FROM CaseOpeningTierEconomySettings ORDER BY Tier;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_tier_economy_settings_set//
CREATE PROCEDURE sp_case_opening_tier_economy_settings_set(IN p_tier INT,IN p_target_profit_basis_points INT,IN p_price_rounding_pence INT)
BEGIN
    INSERT INTO CaseOpeningTierEconomySettings(Tier,TargetProfitBasisPoints,PriceRoundingPence,UpdatedUtc)
    VALUES(LEAST(10,GREATEST(1,p_tier)),LEAST(20000,GREATEST(0,p_target_profit_basis_points)),LEAST(10000,GREATEST(1,p_price_rounding_pence)),UTC_TIMESTAMP(6))
    ON DUPLICATE KEY UPDATE TargetProfitBasisPoints=VALUES(TargetProfitBasisPoints),PriceRoundingPence=VALUES(PriceRoundingPence),UpdatedUtc=UTC_TIMESTAMP(6);
END//
DELIMITER ;

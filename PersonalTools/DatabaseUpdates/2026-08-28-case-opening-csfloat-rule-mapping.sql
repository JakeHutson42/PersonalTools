-- Exact CSFloat market-hash mapping prevents a seed import from crossing weapon variants.
ALTER TABLE CaseOpeningSpecialVariantRules ADD COLUMN IF NOT EXISTS MarketHashName VARCHAR(300) NULL AFTER SourceItemId;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_rules_get//
CREATE PROCEDURE sp_case_opening_special_variant_rules_get()
BEGIN
    SELECT r.RuleId,r.SourceItemId,r.MarketHashName,r.Name,r.Tier,r.Description,r.PaintIndex,r.Phase,r.PatternSeed,r.MinimumFloat,r.MaximumFloat,r.RequiresStatTrak,p.PriceSnapshotId,p.Price
    FROM CaseOpeningSpecialVariantRules r LEFT JOIN (SELECT p.RuleId,p.PriceSnapshotId,p.Price FROM CaseOpeningSpecialVariantSnapshotPrices p INNER JOIN CaseOpeningSpecialVariantPriceSnapshots s ON s.PriceSnapshotId=p.PriceSnapshotId WHERE s.IsActive=1) p ON p.RuleId=r.RuleId
    ORDER BY r.IsActive DESC,r.SourceItemId,r.Name;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_rules_active_get//
CREATE PROCEDURE sp_case_opening_special_variant_rules_active_get()
BEGIN
    SELECT r.RuleId,r.SourceItemId,r.MarketHashName,r.Name,r.Tier,r.Description,r.PaintIndex,r.Phase,r.PatternSeed,r.MinimumFloat,r.MaximumFloat,r.RequiresStatTrak,p.PriceSnapshotId,p.Price
    FROM CaseOpeningSpecialVariantRules r LEFT JOIN (SELECT p.RuleId,p.PriceSnapshotId,p.Price FROM CaseOpeningSpecialVariantSnapshotPrices p INNER JOIN CaseOpeningSpecialVariantPriceSnapshots s ON s.PriceSnapshotId=p.PriceSnapshotId WHERE s.IsActive=1) p ON p.RuleId=r.RuleId
    WHERE r.IsActive=1 ORDER BY r.SourceItemId,r.PatternSeed IS NULL,r.PatternSeed,r.RuleId;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_rule_save//
CREATE PROCEDURE sp_case_opening_special_variant_rule_save(IN p_rule_id CHAR(36),IN p_source_item_id VARCHAR(160),IN p_market_hash_name VARCHAR(300),IN p_name VARCHAR(160),IN p_tier VARCHAR(40),IN p_description VARCHAR(500),IN p_paint_index VARCHAR(20),IN p_phase VARCHAR(50),IN p_pattern_seed INT,IN p_minimum_float DECIMAL(9,6),IN p_maximum_float DECIMAL(9,6),IN p_requires_stat_trak TINYINT(1),IN p_is_active TINYINT(1))
BEGIN
INSERT INTO CaseOpeningSpecialVariantRules(RuleId,SourceItemId,MarketHashName,Name,Tier,Description,PaintIndex,Phase,PatternSeed,MinimumFloat,MaximumFloat,RequiresStatTrak,IsActive,CreatedUtc,UpdatedUtc)
VALUES(p_rule_id,p_source_item_id,NULLIF(p_market_hash_name,''),p_name,p_tier,p_description,NULLIF(p_paint_index,''),NULLIF(p_phase,''),p_pattern_seed,p_minimum_float,p_maximum_float,p_requires_stat_trak,IF(p_is_active<>0,1,0),UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE SourceItemId=VALUES(SourceItemId),MarketHashName=VALUES(MarketHashName),Name=VALUES(Name),Tier=VALUES(Tier),Description=VALUES(Description),PaintIndex=VALUES(PaintIndex),Phase=VALUES(Phase),PatternSeed=VALUES(PatternSeed),MinimumFloat=VALUES(MinimumFloat),MaximumFloat=VALUES(MaximumFloat),RequiresStatTrak=VALUES(RequiresStatTrak),IsActive=VALUES(IsActive),UpdatedUtc=UTC_TIMESTAMP(6);
END//
DELIMITER ;

-- Immutable special-variant provenance. Rules identify a rare float/pattern/phase combination;
-- the opening row records the rule and active snapshot price that applied when it was unboxed.
CREATE TABLE IF NOT EXISTS CaseOpeningSpecialVariantRules(
    RuleId CHAR(36) NOT NULL,SourceItemId VARCHAR(160) NOT NULL,Name VARCHAR(160) NOT NULL,Tier VARCHAR(40) NOT NULL,
    Description VARCHAR(500) NOT NULL,PaintIndex VARCHAR(20) NULL,Phase VARCHAR(50) NULL,PatternSeed INT NULL,
    MinimumFloat DECIMAL(9,6) NULL,MaximumFloat DECIMAL(9,6) NULL,RequiresStatTrak TINYINT(1) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,CreatedUtc DATETIME(6) NOT NULL,UpdatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY(RuleId),KEY IX_CaseOpeningSpecialVariantRules_Match(SourceItemId,IsActive,PatternSeed)
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningSpecialVariantPriceSnapshots(
    PriceSnapshotId CHAR(36) NOT NULL,Name VARCHAR(160) NOT NULL,Source VARCHAR(160) NOT NULL,Currency CHAR(3) NOT NULL DEFAULT 'GBP',
    IsActive TINYINT(1) NOT NULL DEFAULT 0,ImportedUtc DATETIME(6) NOT NULL,PRIMARY KEY(PriceSnapshotId)
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningSpecialVariantSnapshotPrices(
    PriceSnapshotId CHAR(36) NOT NULL,RuleId CHAR(36) NOT NULL,Price DECIMAL(14,2) NOT NULL,SourceNote VARCHAR(500) NULL,
    PRIMARY KEY(PriceSnapshotId,RuleId),CONSTRAINT FK_CaseOpeningSpecialVariantSnapshotPrices_Snapshot FOREIGN KEY(PriceSnapshotId) REFERENCES CaseOpeningSpecialVariantPriceSnapshots(PriceSnapshotId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningSpecialVariantSnapshotPrices_Rule FOREIGN KEY(RuleId) REFERENCES CaseOpeningSpecialVariantRules(RuleId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningOpeningSpecialVariants(
    OpeningId CHAR(36) NOT NULL,RuleId CHAR(36) NOT NULL,Name VARCHAR(160) NOT NULL,Tier VARCHAR(40) NOT NULL,Description VARCHAR(500) NOT NULL,
    PriceSnapshotId CHAR(36) NULL,Price DECIMAL(14,2) NULL,DetectedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY(OpeningId),CONSTRAINT FK_CaseOpeningOpeningSpecialVariants_Opening FOREIGN KEY(OpeningId) REFERENCES CaseOpeningHistory(OpeningId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_rules_active_get//
CREATE PROCEDURE sp_case_opening_special_variant_rules_active_get()
BEGIN
    SELECT r.RuleId,r.SourceItemId,r.Name,r.Tier,r.Description,r.PaintIndex,r.Phase,r.PatternSeed,r.MinimumFloat,r.MaximumFloat,r.RequiresStatTrak,
           p.PriceSnapshotId,p.Price
    FROM CaseOpeningSpecialVariantRules r
    LEFT JOIN (
        SELECT p.RuleId,p.PriceSnapshotId,p.Price
        FROM CaseOpeningSpecialVariantSnapshotPrices p
        INNER JOIN CaseOpeningSpecialVariantPriceSnapshots s ON s.PriceSnapshotId=p.PriceSnapshotId
        WHERE s.IsActive=1
    ) p ON p.RuleId=r.RuleId
    WHERE r.IsActive=1
    ORDER BY r.SourceItemId,r.PatternSeed IS NULL,r.PatternSeed,r.RuleId;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_rules_get//
CREATE PROCEDURE sp_case_opening_special_variant_rules_get()
BEGIN
    SELECT r.RuleId,r.SourceItemId,r.Name,r.Tier,r.Description,r.PaintIndex,r.Phase,r.PatternSeed,r.MinimumFloat,r.MaximumFloat,r.RequiresStatTrak,
           p.PriceSnapshotId,p.Price
    FROM CaseOpeningSpecialVariantRules r
    LEFT JOIN (
        SELECT p.RuleId,p.PriceSnapshotId,p.Price FROM CaseOpeningSpecialVariantSnapshotPrices p
        INNER JOIN CaseOpeningSpecialVariantPriceSnapshots s ON s.PriceSnapshotId=p.PriceSnapshotId WHERE s.IsActive=1
    ) p ON p.RuleId=r.RuleId
    ORDER BY r.IsActive DESC,r.SourceItemId,r.Name;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_rule_save//
CREATE PROCEDURE sp_case_opening_special_variant_rule_save(
    IN p_rule_id CHAR(36),IN p_source_item_id VARCHAR(160),IN p_name VARCHAR(160),IN p_tier VARCHAR(40),IN p_description VARCHAR(500),
    IN p_paint_index VARCHAR(20),IN p_phase VARCHAR(50),IN p_pattern_seed INT,IN p_minimum_float DECIMAL(9,6),IN p_maximum_float DECIMAL(9,6),
    IN p_requires_stat_trak TINYINT(1),IN p_is_active TINYINT(1))
BEGIN
    INSERT INTO CaseOpeningSpecialVariantRules(RuleId,SourceItemId,Name,Tier,Description,PaintIndex,Phase,PatternSeed,MinimumFloat,MaximumFloat,RequiresStatTrak,IsActive,CreatedUtc,UpdatedUtc)
    VALUES(p_rule_id,p_source_item_id,p_name,p_tier,p_description,NULLIF(p_paint_index,''),NULLIF(p_phase,''),p_pattern_seed,p_minimum_float,p_maximum_float,p_requires_stat_trak,IF(p_is_active<>0,1,0),UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))
    ON DUPLICATE KEY UPDATE SourceItemId=VALUES(SourceItemId),Name=VALUES(Name),Tier=VALUES(Tier),Description=VALUES(Description),PaintIndex=VALUES(PaintIndex),Phase=VALUES(Phase),PatternSeed=VALUES(PatternSeed),MinimumFloat=VALUES(MinimumFloat),MaximumFloat=VALUES(MaximumFloat),RequiresStatTrak=VALUES(RequiresStatTrak),IsActive=VALUES(IsActive),UpdatedUtc=UTC_TIMESTAMP(6);
END//

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_price_snapshots_get//
CREATE PROCEDURE sp_case_opening_special_variant_price_snapshots_get()
BEGIN SELECT PriceSnapshotId,Name,Source,IsActive,ImportedUtc FROM CaseOpeningSpecialVariantPriceSnapshots ORDER BY ImportedUtc DESC,PriceSnapshotId DESC; END//

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_price_snapshot_create//
CREATE PROCEDURE sp_case_opening_special_variant_price_snapshot_create(IN p_snapshot_id CHAR(36),IN p_name VARCHAR(160),IN p_source VARCHAR(160),IN p_prices JSON)
BEGIN
    INSERT INTO CaseOpeningSpecialVariantPriceSnapshots(PriceSnapshotId,Name,Source,Currency,IsActive,ImportedUtc) VALUES(p_snapshot_id,p_name,p_source,'GBP',0,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningSpecialVariantSnapshotPrices(PriceSnapshotId,RuleId,Price)
    SELECT p_snapshot_id,x.RuleId,x.Price FROM JSON_TABLE(p_prices,'$[*]' COLUMNS(RuleId CHAR(36) PATH '$.ruleId',Price DECIMAL(14,2) PATH '$.price')) x
    INNER JOIN CaseOpeningSpecialVariantRules r ON r.RuleId=x.RuleId;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_price_snapshot_activate//
CREATE PROCEDURE sp_case_opening_special_variant_price_snapshot_activate(IN p_snapshot_id CHAR(36))
BEGIN
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningSpecialVariantPriceSnapshots WHERE PriceSnapshotId=p_snapshot_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The special-variant price snapshot could not be found.'; END IF;
    UPDATE CaseOpeningSpecialVariantPriceSnapshots SET IsActive=0 WHERE IsActive=1;
    UPDATE CaseOpeningSpecialVariantPriceSnapshots SET IsActive=1 WHERE PriceSnapshotId=p_snapshot_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_opening_special_variant_save//
CREATE PROCEDURE sp_case_opening_opening_special_variant_save(
    IN p_opening_id CHAR(36),IN p_rule_id CHAR(36),IN p_name VARCHAR(160),IN p_tier VARCHAR(40),IN p_description VARCHAR(500),
    IN p_price_snapshot_id CHAR(36),IN p_price DECIMAL(14,2))
BEGIN
    INSERT INTO CaseOpeningOpeningSpecialVariants(OpeningId,RuleId,Name,Tier,Description,PriceSnapshotId,Price,DetectedUtc)
    VALUES(p_opening_id,p_rule_id,p_name,p_tier,p_description,p_price_snapshot_id,p_price,UTC_TIMESTAMP(6));
END//

DROP PROCEDURE IF EXISTS sp_case_opening_history_get//
CREATE PROCEDURE sp_case_opening_history_get(IN p_user_id CHAR(36))
BEGIN
    SELECT h.OpeningId,h.UserId,h.CaseKey,h.SourceItemId,h.ItemName,h.MarketHashName,h.ImageUrl,h.Description,h.WeaponName,
        h.PatternName,h.PaintIndex,h.Phase,h.RarityKey,h.RarityName,h.RarityColor,h.Wear,h.IsStatTrak,h.IsRareSpecial,
        h.SupportsStatTrak,h.MinFloat,h.MaxFloat,h.FloatValue,h.PatternSeed,h.EstimatedPrice,h.IsLocked,
        v.RuleId SpecialVariantRuleId,v.Name SpecialVariantName,v.Tier SpecialVariantTier,v.Description SpecialVariantDescription,
        v.PriceSnapshotId SpecialVariantPriceSnapshotId,v.Price SpecialVariantPrice,h.OpenedUtc
    FROM CaseOpeningHistory h
    LEFT JOIN CaseOpeningTradeUpRecipeHoldings ho ON ho.OpeningId=h.OpeningId
    LEFT JOIN CaseOpeningOpeningSpecialVariants v ON v.OpeningId=h.OpeningId
    WHERE h.UserId=p_user_id AND ho.HoldingId IS NULL
    ORDER BY h.OpenedUtc DESC,h.OpeningId DESC;
END//

DELIMITER ;

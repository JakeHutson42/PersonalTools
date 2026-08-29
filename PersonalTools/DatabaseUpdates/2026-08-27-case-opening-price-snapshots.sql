-- Immutable, manually-imported Skinport valuation snapshots for the simulator balance lab.
CREATE TABLE IF NOT EXISTS CaseOpeningPriceSnapshots (
    PriceSnapshotId CHAR(36) NOT NULL,
    Name VARCHAR(160) NOT NULL,
    Source VARCHAR(40) NOT NULL,
    Currency CHAR(3) NOT NULL,
    PriceBasis VARCHAR(80) NOT NULL,
    SourceItemCount INT UNSIGNED NOT NULL,
    MatchedItemCount INT UNSIGNED NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 0,
    ImportedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (PriceSnapshotId),
    KEY IX_CaseOpeningPriceSnapshots_Active (IsActive, ImportedUtc)
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningPriceSnapshotItems (
    PriceSnapshotId CHAR(36) NOT NULL,
    MarketHashName VARCHAR(300) COLLATE utf8mb4_bin NOT NULL,
    Price DECIMAL(14,2) NOT NULL,
    MinimumPrice DECIMAL(14,2) NULL,
    MeanPrice DECIMAL(14,2) NULL,
    MedianPrice DECIMAL(14,2) NULL,
    SuggestedPrice DECIMAL(14,2) NULL,
    Quantity INT UNSIGNED NOT NULL DEFAULT 0,
    SourceUpdatedUtc DATETIME(6) NULL,
    PRIMARY KEY (PriceSnapshotId, MarketHashName),
    CONSTRAINT FK_CaseOpeningPriceSnapshotItems_Snapshot FOREIGN KEY (PriceSnapshotId)
        REFERENCES CaseOpeningPriceSnapshots (PriceSnapshotId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshots_get//
CREATE PROCEDURE sp_case_opening_price_snapshots_get()
BEGIN
    SELECT PriceSnapshotId,Name,Source,Currency,PriceBasis,SourceItemCount,MatchedItemCount,IsActive,ImportedUtc
    FROM CaseOpeningPriceSnapshots ORDER BY ImportedUtc DESC,PriceSnapshotId DESC;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_active_items_get//
CREATE PROCEDURE sp_case_opening_price_snapshot_active_items_get()
BEGIN
    SELECT i.PriceSnapshotId,i.MarketHashName,i.Price,i.MinimumPrice,i.MeanPrice,i.MedianPrice,
           i.SuggestedPrice,i.Quantity,i.SourceUpdatedUtc
    FROM CaseOpeningPriceSnapshotItems i
    INNER JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
    WHERE s.IsActive=1 ORDER BY i.MarketHashName;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_active_price_get//
CREATE PROCEDURE sp_case_opening_price_snapshot_active_price_get(IN p_market_hash_name VARCHAR(300))
BEGIN
    SELECT i.PriceSnapshotId,i.MarketHashName,i.Price,i.MinimumPrice,i.MeanPrice,i.MedianPrice,
           i.SuggestedPrice,i.Quantity,i.SourceUpdatedUtc
    FROM CaseOpeningPriceSnapshotItems i
    INNER JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
    WHERE s.IsActive=1 AND BINARY i.MarketHashName=BINARY p_market_hash_name LIMIT 1;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_create//
CREATE PROCEDURE sp_case_opening_price_snapshot_create(
    IN p_snapshot_id CHAR(36),IN p_name VARCHAR(160),IN p_source VARCHAR(40),IN p_currency CHAR(3),
    IN p_price_basis VARCHAR(80),IN p_source_item_count INT,IN p_matched_item_count INT,IN p_items JSON)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    UPDATE CaseOpeningPriceSnapshots SET IsActive=0 WHERE IsActive=1;
    INSERT INTO CaseOpeningPriceSnapshots(PriceSnapshotId,Name,Source,Currency,PriceBasis,SourceItemCount,MatchedItemCount,IsActive,ImportedUtc)
    VALUES(p_snapshot_id,p_name,p_source,p_currency,p_price_basis,GREATEST(0,p_source_item_count),GREATEST(0,p_matched_item_count),1,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningPriceSnapshotItems(PriceSnapshotId,MarketHashName,Price,MinimumPrice,MeanPrice,MedianPrice,SuggestedPrice,Quantity,SourceUpdatedUtc)
    SELECT p_snapshot_id,j.MarketHashName,j.Price,j.MinimumPrice,j.MeanPrice,j.MedianPrice,j.SuggestedPrice,GREATEST(0,j.Quantity),j.SourceUpdatedUtc
    FROM JSON_TABLE(p_items,'$[*]' COLUMNS(
        MarketHashName VARCHAR(300) PATH '$.marketHashName',Price DECIMAL(14,2) PATH '$.price',
        MinimumPrice DECIMAL(14,2) PATH '$.minimumPrice' NULL ON EMPTY,MeanPrice DECIMAL(14,2) PATH '$.meanPrice' NULL ON EMPTY,
        MedianPrice DECIMAL(14,2) PATH '$.medianPrice' NULL ON EMPTY,SuggestedPrice DECIMAL(14,2) PATH '$.suggestedPrice' NULL ON EMPTY,
        Quantity INT PATH '$.quantity',SourceUpdatedUtc DATETIME(6) PATH '$.sourceUpdatedUtc' NULL ON EMPTY)) j;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_activate//
CREATE PROCEDURE sp_case_opening_price_snapshot_activate(IN p_snapshot_id CHAR(36))
BEGIN
    DECLARE v_exists INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT COUNT(*) INTO v_exists FROM CaseOpeningPriceSnapshots WHERE PriceSnapshotId=p_snapshot_id;
    IF v_exists=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That price snapshot no longer exists.'; END IF;
    START TRANSACTION;
    UPDATE CaseOpeningPriceSnapshots SET IsActive=0 WHERE IsActive=1;
    UPDATE CaseOpeningPriceSnapshots SET IsActive=1 WHERE PriceSnapshotId=p_snapshot_id;
    COMMIT;
END//
DELIMITER ;

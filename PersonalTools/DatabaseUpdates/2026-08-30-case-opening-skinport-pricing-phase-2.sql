-- Phase 2: retain auditable provenance for every snapshot value, including inherited prices.
ALTER TABLE CaseOpeningPriceSnapshotItems
    ADD COLUMN IF NOT EXISTS IsFallback TINYINT(1) NOT NULL DEFAULT 0 AFTER SourceUpdatedUtc,
    ADD COLUMN IF NOT EXISTS PriceSource VARCHAR(40) NOT NULL DEFAULT 'Skinport' AFTER IsFallback,
    ADD COLUMN IF NOT EXISTS PriceMethod VARCHAR(80) NOT NULL DEFAULT '' AFTER PriceSource,
    ADD COLUMN IF NOT EXISTS SourceMarketHashName VARCHAR(300) COLLATE utf8mb4_bin NOT NULL DEFAULT '' AFTER PriceMethod;

UPDATE CaseOpeningPriceSnapshotItems
SET PriceSource='Skinport',
    PriceMethod=CASE
        WHEN MedianPrice IS NOT NULL AND MedianPrice>0 THEN 'legacy-median'
        WHEN SuggestedPrice IS NOT NULL AND SuggestedPrice>0 THEN 'legacy-suggested'
        WHEN MeanPrice IS NOT NULL AND MeanPrice>0 THEN 'legacy-mean'
        WHEN MinimumPrice IS NOT NULL AND MinimumPrice>0 THEN 'legacy-minimum'
        ELSE 'legacy-inferred'
    END,
    IsFallback=IF(MedianPrice IS NULL OR MedianPrice<=0,1,IsFallback),
    SourceMarketHashName=IF(SourceMarketHashName='',MarketHashName,SourceMarketHashName)
WHERE PriceMethod='';

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_active_items_get//
CREATE PROCEDURE sp_case_opening_price_snapshot_active_items_get()
BEGIN
    WITH ranked AS (
        SELECT i.PriceSnapshotId,i.MarketHashName,i.Price,i.MinimumPrice,i.MeanPrice,i.MedianPrice,i.SuggestedPrice,
               i.Quantity,i.SourceUpdatedUtc,i.IsFallback StoredFallback,i.PriceSource,i.PriceMethod,i.SourceMarketHashName,
               s.IsActive,ROW_NUMBER() OVER(PARTITION BY i.MarketHashName ORDER BY s.IsActive DESC,s.ImportedUtc DESC) rn
        FROM CaseOpeningPriceSnapshotItems i
        JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
        CROSS JOIN (SELECT ImportedUtc FROM CaseOpeningPriceSnapshots WHERE IsActive=1 LIMIT 1) active
        WHERE s.ImportedUtc<=active.ImportedUtc
    )
    SELECT PriceSnapshotId,MarketHashName,Price,MinimumPrice,MeanPrice,MedianPrice,SuggestedPrice,Quantity,SourceUpdatedUtc,
           IF(StoredFallback=1 OR IsActive=0,1,0) IsFallback,PriceSource,
           IF(IsActive=0,CONCAT('inherited-',PriceMethod),PriceMethod) PriceMethod,SourceMarketHashName
    FROM ranked WHERE rn=1 ORDER BY MarketHashName;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_active_price_get//
CREATE PROCEDURE sp_case_opening_price_snapshot_active_price_get(IN p_market_hash_name VARCHAR(300))
BEGIN
    WITH ranked AS (
        SELECT i.PriceSnapshotId,i.MarketHashName,i.Price,i.MinimumPrice,i.MeanPrice,i.MedianPrice,i.SuggestedPrice,
               i.Quantity,i.SourceUpdatedUtc,i.IsFallback StoredFallback,i.PriceSource,i.PriceMethod,i.SourceMarketHashName,
               s.IsActive,ROW_NUMBER() OVER(ORDER BY s.IsActive DESC,s.ImportedUtc DESC) rn
        FROM CaseOpeningPriceSnapshotItems i
        JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
        CROSS JOIN (SELECT ImportedUtc FROM CaseOpeningPriceSnapshots WHERE IsActive=1 LIMIT 1) active
        WHERE s.ImportedUtc<=active.ImportedUtc AND BINARY i.MarketHashName=BINARY p_market_hash_name
    )
    SELECT PriceSnapshotId,MarketHashName,Price,MinimumPrice,MeanPrice,MedianPrice,SuggestedPrice,Quantity,SourceUpdatedUtc,
           IF(StoredFallback=1 OR IsActive=0,1,0) IsFallback,PriceSource,
           IF(IsActive=0,CONCAT('inherited-',PriceMethod),PriceMethod) PriceMethod,SourceMarketHashName
    FROM ranked WHERE rn=1;
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
    INSERT INTO CaseOpeningPriceSnapshotItems(
        PriceSnapshotId,MarketHashName,Price,MinimumPrice,MeanPrice,MedianPrice,SuggestedPrice,Quantity,SourceUpdatedUtc,
        IsFallback,PriceSource,PriceMethod,SourceMarketHashName)
    SELECT p_snapshot_id,j.MarketHashName,j.Price,j.MinimumPrice,j.MeanPrice,j.MedianPrice,j.SuggestedPrice,
           GREATEST(0,j.Quantity),j.SourceUpdatedUtc,j.IsFallback,j.PriceSource,j.PriceMethod,j.SourceMarketHashName
    FROM JSON_TABLE(p_items,'$[*]' COLUMNS(
        MarketHashName VARCHAR(300) PATH '$.marketHashName',Price DECIMAL(14,2) PATH '$.price',
        MinimumPrice DECIMAL(14,2) PATH '$.minimumPrice' NULL ON EMPTY,MeanPrice DECIMAL(14,2) PATH '$.meanPrice' NULL ON EMPTY,
        MedianPrice DECIMAL(14,2) PATH '$.medianPrice' NULL ON EMPTY,SuggestedPrice DECIMAL(14,2) PATH '$.suggestedPrice' NULL ON EMPTY,
        Quantity INT PATH '$.quantity',SourceUpdatedUtc DATETIME(6) PATH '$.sourceUpdatedUtc' NULL ON EMPTY,
        IsFallback TINYINT PATH '$.isFallback',PriceSource VARCHAR(40) PATH '$.priceSource',
        PriceMethod VARCHAR(80) PATH '$.priceMethod',SourceMarketHashName VARCHAR(300) PATH '$.sourceMarketHashName')) j;
    COMMIT;
END//
DELIMITER ;

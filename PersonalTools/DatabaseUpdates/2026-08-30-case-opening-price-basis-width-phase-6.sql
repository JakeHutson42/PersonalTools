-- Allow auditable snapshot descriptions to name multiple pricing windows and sources.
ALTER TABLE CaseOpeningPriceSnapshots
    MODIFY COLUMN PriceBasis VARCHAR(200) NOT NULL;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_create//
CREATE PROCEDURE sp_case_opening_price_snapshot_create(
    IN p_snapshot_id CHAR(36),IN p_name VARCHAR(160),IN p_source VARCHAR(40),IN p_currency CHAR(3),
    IN p_price_basis VARCHAR(200),IN p_source_item_count INT,IN p_matched_item_count INT,IN p_items JSON)
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

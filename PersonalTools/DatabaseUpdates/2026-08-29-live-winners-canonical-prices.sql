-- Keep the live 24-hour winners panel on the same active market-price snapshot
-- used by opening results, daily rewards, and inventory.  A recorded special
-- variant price remains authoritative because it represents rare pattern/phase
-- items such as blue gems.
USE PersonalTools;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_live_winners_top_get//
CREATE PROCEDURE sp_live_winners_top_get()
BEGIN
 WITH active_prices AS (
     SELECT MarketHashName,Price
     FROM (
         SELECT i.MarketHashName,i.Price,
                ROW_NUMBER() OVER(PARTITION BY i.MarketHashName ORDER BY s.IsActive DESC,s.ImportedUtc DESC) rn
         FROM CaseOpeningPriceSnapshotItems i
         INNER JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
         CROSS JOIN (SELECT ImportedUtc FROM CaseOpeningPriceSnapshots WHERE IsActive=1 LIMIT 1) active
         WHERE s.ImportedUtc<=active.ImportedUtc
     ) ranked_prices
     WHERE rn=1
 )
 SELECT h.OpeningId,u.DisplayName,h.ItemName,h.ImageUrl,h.RarityColor,
        COALESCE(v.Price,p.Price,h.EstimatedPrice,0) EstimatedPrice,
        h.OpenedUtc,IF(h.CaseKey='trade-up','Trade Up','Case opening') Source
 FROM CaseOpeningHistory h
 INNER JOIN Users u ON u.UserId=h.UserId
 LEFT JOIN CaseOpeningOpeningSpecialVariants v ON v.OpeningId=h.OpeningId
 LEFT JOIN active_prices p ON BINARY p.MarketHashName=BINARY h.MarketHashName
 WHERE h.OpenedUtc>=DATE_SUB(UTC_TIMESTAMP(6),INTERVAL 24 HOUR)
   AND COALESCE(v.Price,p.Price,h.EstimatedPrice) IS NOT NULL
 ORDER BY COALESCE(v.Price,p.Price,h.EstimatedPrice) DESC,h.OpenedUtc DESC
 LIMIT 5;
END//
DELIMITER ;

-- Phase 5C intentionally includes only Contract History Filters. The three ambiguous advanced
-- recipe concepts remain absent so existing recipe/holding behaviour is not redefined.
INSERT INTO CaseOpeningUpgradeDefinitions(UpgradeKey,Name,Description,Category,CostStars,CostGbpPence,RequiredLevel,SortOrder,IsActive)
VALUES('contract-history-filters','Contract History Filters','Browse completed contracts by input rarity, output rarity, case and date.','trade-up-advanced',3600,360,15,800,1)
ON DUPLICATE KEY UPDATE Name=VALUES(Name),Description=VALUES(Description),Category=VALUES(Category),SortOrder=VALUES(SortOrder),IsActive=VALUES(IsActive);

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_trade_up_history_get//
CREATE PROCEDURE sp_case_opening_trade_up_history_get(IN p_user_id CHAR(36))
BEGIN
 SELECT t.TradeUpId,t.InputRarityKey,t.OutputRarityKey,t.OutputCaseKey,t.AverageInputFloat,t.CreatedUtc,
        COALESCE(h.ItemName,'Contract output') OutputName,COALESCE(h.ImageUrl,'') OutputImageUrl,
        COALESCE(h.Wear,'') OutputWear,COALESCE(h.IsStatTrak,0) OutputIsStatTrak,COALESCE(h.EstimatedPrice,0) OutputEstimatedPrice
 FROM CaseOpeningTradeUps t LEFT JOIN CaseOpeningHistory h ON h.OpeningId=t.OutputOpeningId AND h.UserId=t.UserId
 WHERE t.UserId=p_user_id ORDER BY t.CreatedUtc DESC LIMIT 500;
END//
DELIMITER ;

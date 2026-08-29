-- Persistent lifetime simulator statistics. Existing aggregate totals remain untouched;
-- counters that could not be reconstructed from deleted/sold inventory begin at zero.
ALTER TABLE CaseOpeningPlayerStats
    ADD COLUMN IF NOT EXISTS TotalMilSpecPulls BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalRestrictedPulls BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalClassifiedPulls BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalCovertPulls BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalRareSpecialPulls BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalStatTrakPulls BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalCasesPurchased BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalCasePurchaseStarsSpent BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalSaleStarsEarned BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalPullValueStars BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalStarsSpent BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalLevelRewardStars BIGINT UNSIGNED NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS TotalUpgradesPurchased BIGINT UNSIGNED NOT NULL DEFAULT 0;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_player_stats_get//
CREATE PROCEDURE sp_case_opening_player_stats_get(IN p_user_id CHAR(36))
BEGIN
    INSERT IGNORE INTO CaseOpeningPlayerStats(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    SELECT UserId,TotalCasesOpened,TotalSkinsObtained,TotalTradeUpsCompleted,TotalUnlocks,
           TotalLoginDays,CurrentLoginStreak,LongestLoginStreak,CompletedCollections,
           CompletedRaritySets,HighestRewardedLevel,LastLoginUtcDate,TotalMilSpecPulls,
           TotalRestrictedPulls,TotalClassifiedPulls,TotalCovertPulls,TotalRareSpecialPulls,
           TotalStatTrakPulls,TotalCasesPurchased,TotalCasePurchaseStarsSpent,
           TotalSaleStarsEarned,TotalPullValueStars,TotalStarsSpent,TotalLevelRewardStars,
           TotalUpgradesPurchased
    FROM CaseOpeningPlayerStats WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_player_stats_add//
CREATE PROCEDURE sp_case_opening_player_stats_add(
    IN p_user_id CHAR(36),IN p_cases_opened INT,IN p_skins_obtained INT,
    IN p_trade_ups_completed INT,IN p_unlocks_earned INT,IN p_rarity_key VARCHAR(30),
    IN p_is_stat_trak TINYINT,IN p_cases_purchased INT,IN p_case_purchase_stars_spent INT,
    IN p_sale_stars_earned INT,IN p_pull_value_stars INT,IN p_stars_spent INT,
    IN p_level_reward_stars INT,IN p_upgrades_purchased INT)
BEGIN
    INSERT IGNORE INTO CaseOpeningPlayerStats(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    UPDATE CaseOpeningPlayerStats SET
        TotalCasesOpened=TotalCasesOpened+GREATEST(0,p_cases_opened),
        TotalSkinsObtained=TotalSkinsObtained+GREATEST(0,p_skins_obtained),
        TotalTradeUpsCompleted=TotalTradeUpsCompleted+GREATEST(0,p_trade_ups_completed),
        TotalUnlocks=TotalUnlocks+GREATEST(0,p_unlocks_earned),
        TotalMilSpecPulls=TotalMilSpecPulls+IF(p_rarity_key IN ('mil-spec','high-grade'),1,0),
        TotalRestrictedPulls=TotalRestrictedPulls+IF(p_rarity_key IN ('restricted','remarkable'),1,0),
        TotalClassifiedPulls=TotalClassifiedPulls+IF(p_rarity_key IN ('classified','exotic'),1,0),
        TotalCovertPulls=TotalCovertPulls+IF(p_rarity_key='covert',1,0),
        TotalRareSpecialPulls=TotalRareSpecialPulls+IF(p_rarity_key='rare-special',1,0),
        TotalStatTrakPulls=TotalStatTrakPulls+IF(p_is_stat_trak<>0 AND p_cases_opened>0,1,0),
        TotalCasesPurchased=TotalCasesPurchased+GREATEST(0,p_cases_purchased),
        TotalCasePurchaseStarsSpent=TotalCasePurchaseStarsSpent+GREATEST(0,p_case_purchase_stars_spent),
        TotalSaleStarsEarned=TotalSaleStarsEarned+GREATEST(0,p_sale_stars_earned),
        TotalPullValueStars=TotalPullValueStars+GREATEST(0,p_pull_value_stars),
        TotalStarsSpent=TotalStarsSpent+GREATEST(0,p_stars_spent),
        TotalLevelRewardStars=TotalLevelRewardStars+GREATEST(0,p_level_reward_stars),
        TotalUpgradesPurchased=TotalUpgradesPurchased+GREATEST(0,p_upgrades_purchased),
        UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE UserId=p_user_id;
END//
DELIMITER ;

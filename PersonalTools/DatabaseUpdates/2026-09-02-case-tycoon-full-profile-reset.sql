-- Make the administrator's Case Tycoon profile reset cover every user-owned Tycoon feature.
-- UserFriends and the Users identity row are deliberately preserved.
USE PersonalTools;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_reset_dev//
CREATE PROCEDURE sp_case_opening_reset_dev(IN p_user_id CHAR(36))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;

    -- Removing a battle cascades its cases, participants, invitations, rolls, pulls,
    -- reservations and settlements. This also prevents an unresolved escrow from surviving
    -- while the account's owned cases and balances are returned to their initial state.
    DELETE FROM CaseOpeningBattles
    WHERE CreatorUserId=p_user_id
       OR EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants participant WHERE participant.BattleId=CaseOpeningBattles.BattleId AND participant.UserId=p_user_id)
       OR EXISTS(SELECT 1 FROM CaseOpeningBattleInvitations invitation WHERE invitation.BattleId=CaseOpeningBattles.BattleId AND invitation.InvitedUserId=p_user_id);

    DELETE FROM CaseOpeningBattleOverflow WHERE UserId=p_user_id;
    DELETE FROM CaseBattleReactionUnlocks WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningDailyDropUpgrades WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningDailyDrops WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningDevDropRarities WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUserUpgradeUnlocks WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUserPreferences WHERE UserId=p_user_id;
    DELETE FROM AppSettings WHERE UserId=p_user_id AND SettingKey='CaseProfileEmoji';
    DELETE FROM CaseOpeningUserAchievements WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningCompletedRarities WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningCompletedCollections WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningPlayerStats WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningBots WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningBotServers WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningTradeUps WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningAutoBuyRules WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningTradeUpRecipeHoldings WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningTradeUpRecipes WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUserInventoryUpgrades WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningCollection WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningHistory WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningStorageContainers WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningInventoryCapacity WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningOwnedCases WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUnlockedCases WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUserFreeCaseAllowances WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningEconomyLedger WHERE UserId=p_user_id;

    INSERT INTO CaseOpeningUnlockedCases(UserId,CaseKey,UnlockedUtc) VALUES(p_user_id,'kilowatt',UTC_TIMESTAMP());
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) VALUES(p_user_id,'kilowatt',25,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc)
    VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP())
    ON DUPLICATE KEY UPDATE Stars=0,GbpPence=0,Xp=0,SkipAnimationUnlocked=0,MultiOpenLevel=0,OpenSpeedLevel=0,UpdatedUtc=UTC_TIMESTAMP();
    COMMIT;
END//
DELIMITER ;

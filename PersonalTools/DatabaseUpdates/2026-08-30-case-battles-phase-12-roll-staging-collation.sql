-- Make JSON roll verification independent of the server's legacy GUID and CaseKey collations.
USE PersonalTools;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_rolls_stage//
CREATE PROCEDURE sp_case_battles_rolls_stage(IN p_battle_id CHAR(36), IN p_rolls JSON)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_staged INT DEFAULT 0;
    DECLARE v_input INT DEFAULT 0;
    DECLARE v_invalid INT DEFAULT 0;
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE v_snapshot CHAR(36) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT COUNT(*)*(SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id) INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_staged FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    IF v_staged=v_expected AND v_expected>0 THEN
        COMMIT;
    ELSE
        IF v_staged<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A partial battle roll set cannot be replaced.'; END IF;
        SELECT Status,PriceSnapshotId INTO v_status,v_snapshot FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
        IF v_status<>'opening' OR v_snapshot IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle is not ready for server rolls.'; END IF;
        SET v_input=COALESCE(JSON_LENGTH(p_rolls),0);
        IF v_input<>v_expected THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The server roll set is incomplete.'; END IF;

        SELECT COUNT(*) INTO v_invalid
        FROM JSON_TABLE(p_rolls,'$[*]' COLUMNS(OriginalOwnerUserId CHAR(36) PATH '$.originalOwnerUserId',RoundNumber INT PATH '$.roundNumber',CaseKey VARCHAR(80) PATH '$.caseKey',MarketHashName VARCHAR(300) PATH '$.marketHashName')) input
        LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=p_battle_id AND BINARY participant.UserId=BINARY input.OriginalOwnerUserId
        LEFT JOIN CaseOpeningBattleCases battleCase ON battleCase.BattleId=p_battle_id AND battleCase.RoundNumber=input.RoundNumber AND BINARY battleCase.CaseKey=BINARY input.CaseKey
        LEFT JOIN CaseOpeningBattles battle ON battle.BattleId=p_battle_id
        LEFT JOIN CaseOpeningPriceSnapshotItems price ON price.PriceSnapshotId=battle.PriceSnapshotId AND BINARY price.MarketHashName=BINARY input.MarketHashName
        WHERE participant.UserId IS NULL OR battleCase.CaseKey IS NULL OR price.MarketHashName IS NULL OR price.Price<0;
        IF v_invalid<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A staged roll does not match the locked battle snapshot.'; END IF;

        INSERT INTO CaseOpeningBattleRolls(BattleRollId,BattleId,OriginalOwnerUserId,RoundNumber,OpeningId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,LockedValue,CreatedUtc)
        SELECT UUID(),p_battle_id,input.OriginalOwnerUserId,input.RoundNumber,input.OpeningId,input.CaseKey,input.SourceItemId,input.ItemName,input.MarketHashName,input.ImageUrl,input.RarityKey,input.RarityName,input.RarityColor,input.Wear,input.IsStatTrak,input.IsRareSpecial,input.SupportsStatTrak,price.Price,UTC_TIMESTAMP(6)
        FROM JSON_TABLE(p_rolls,'$[*]' COLUMNS(OriginalOwnerUserId CHAR(36) PATH '$.originalOwnerUserId',RoundNumber INT PATH '$.roundNumber',OpeningId CHAR(36) PATH '$.openingId',CaseKey VARCHAR(80) PATH '$.caseKey',SourceItemId VARCHAR(160) PATH '$.sourceItemId',ItemName VARCHAR(255) PATH '$.itemName',MarketHashName VARCHAR(300) PATH '$.marketHashName',ImageUrl VARCHAR(2048) PATH '$.imageUrl',RarityKey VARCHAR(30) PATH '$.rarityKey',RarityName VARCHAR(80) PATH '$.rarityName',RarityColor CHAR(7) PATH '$.rarityColor',Wear VARCHAR(40) PATH '$.wear',IsStatTrak TINYINT PATH '$.isStatTrak',IsRareSpecial TINYINT PATH '$.isRareSpecial',SupportsStatTrak TINYINT PATH '$.supportsStatTrak')) input
        INNER JOIN CaseOpeningBattles battle ON battle.BattleId=p_battle_id
        INNER JOIN CaseOpeningPriceSnapshotItems price ON price.PriceSnapshotId=battle.PriceSnapshotId AND BINARY price.MarketHashName=BINARY input.MarketHashName;
        COMMIT;
    END IF;
END//
DELIMITER ;

-- Apply this after 2026-09-20-chess.sql. It adds safe deletion for games owned by the current user.
DELIMITER !!

DROP PROCEDURE IF EXISTS sp_chess_game_delete!!
CREATE PROCEDURE sp_chess_game_delete(IN p_game_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    -- Do not let one player remove a shared game from the other player's history.
    DELETE FROM ChessGames
    WHERE GameId=p_game_id AND WhiteUserId=p_user_id AND (Mode='ai' OR Status='waiting');
    IF ROW_COUNT()<>1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only your computer games or unjoined invitations can be deleted.';
    END IF;
END!!

DELIMITER ;

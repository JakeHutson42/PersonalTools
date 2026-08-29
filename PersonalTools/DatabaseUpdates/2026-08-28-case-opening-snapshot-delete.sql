-- Administrators may remove discarded snapshots, but never the active snapshot. Rare-variant
-- snapshots that have been applied to an opening remain immutable provenance and cannot be removed.
DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_delete//
CREATE PROCEDURE sp_case_opening_price_snapshot_delete(IN p_snapshot_id CHAR(36))
BEGIN
    DECLARE v_is_active TINYINT(1);

    SELECT IsActive INTO v_is_active
    FROM CaseOpeningPriceSnapshots
    WHERE PriceSnapshotId = p_snapshot_id;

    IF v_is_active IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That price snapshot no longer exists.';
    END IF;
    IF v_is_active <> 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Activate another price snapshot before deleting this one.';
    END IF;

    DELETE FROM CaseOpeningPriceSnapshots WHERE PriceSnapshotId = p_snapshot_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_special_variant_price_snapshot_delete//
CREATE PROCEDURE sp_case_opening_special_variant_price_snapshot_delete(IN p_snapshot_id CHAR(36))
BEGIN
    DECLARE v_is_active TINYINT(1);

    SELECT IsActive INTO v_is_active
    FROM CaseOpeningSpecialVariantPriceSnapshots
    WHERE PriceSnapshotId = p_snapshot_id;

    IF v_is_active IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That rare-variant price snapshot no longer exists.';
    END IF;
    IF v_is_active <> 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Activate another rare-variant snapshot before deleting this one.';
    END IF;
    IF EXISTS (SELECT 1 FROM CaseOpeningOpeningSpecialVariants WHERE PriceSnapshotId = p_snapshot_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This rare-variant snapshot is retained because it is referenced by an opening.';
    END IF;

    DELETE FROM CaseOpeningSpecialVariantPriceSnapshots WHERE PriceSnapshotId = p_snapshot_id;
END//

DELIMITER ;

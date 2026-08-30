-- Replace four containers without dependable current or 90-day sale evidence.
-- These are provisional settings only; the reviewed snapshot publish recalculates their
-- exact tier and prices from EV alongside the rest of the catalogue.
INSERT IGNORE INTO CaseOpeningCaseSettings
    (CaseKey,Tier,UnlockCostStars,UnlockCostGbpPence,PurchaseCostStars,PurchaseCostGbpPence,XpRequirement,UpdatedUtc)
VALUES
    ('feral-predators',2,25,2500,2,200,1,UTC_TIMESTAMP()),
    ('sticker-capsule-2',8,800,80000,25,2500,8,UTC_TIMESTAMP()),
    ('jackass',3,50,5000,3,300,2,UTC_TIMESTAMP()),
    ('paris-2023-mirage-souvenir',6,300,30000,12,1200,5,UTC_TIMESTAMP());

-- Carry unopened inventory and unlocks forward when this catalogue has already been used.
-- Stage the self-table transfer first: MariaDB considers Quantity ambiguous when an
-- INSERT ... SELECT from CaseOpeningOwnedCases also uses ON DUPLICATE KEY UPDATE.
DROP TEMPORARY TABLE IF EXISTS CaseOpeningContainerReplacementInventory;
CREATE TEMPORARY TABLE CaseOpeningContainerReplacementInventory
(
    UserId CHAR(36) NOT NULL,
    CaseKey VARCHAR(80) NOT NULL,
    Quantity INT UNSIGNED NOT NULL,
    PRIMARY KEY(UserId,CaseKey)
) ENGINE=InnoDB;

INSERT INTO CaseOpeningContainerReplacementInventory(UserId,CaseKey,Quantity)
SELECT UserId,
       CASE CaseKey
           WHEN 'chicken' THEN 'feral-predators'
           WHEN 'katowice-2014-challengers' THEN 'sticker-capsule-2'
           WHEN 'katowice-2014-legends' THEN 'jackass'
           WHEN 'cologne-2014-cobblestone-souvenir' THEN 'paris-2023-mirage-souvenir'
       END,
       Quantity
FROM CaseOpeningOwnedCases
WHERE CaseKey IN ('chicken','katowice-2014-challengers','katowice-2014-legends','cologne-2014-cobblestone-souvenir');

UPDATE CaseOpeningOwnedCases destination
INNER JOIN CaseOpeningContainerReplacementInventory source
    ON BINARY source.UserId=BINARY destination.UserId AND source.CaseKey=destination.CaseKey
SET destination.Quantity=destination.Quantity+source.Quantity,
    destination.UpdatedUtc=UTC_TIMESTAMP(6);

INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
SELECT source.UserId,source.CaseKey,source.Quantity,UTC_TIMESTAMP(6)
FROM CaseOpeningContainerReplacementInventory source
LEFT JOIN CaseOpeningOwnedCases destination
    ON BINARY destination.UserId=BINARY source.UserId AND destination.CaseKey=source.CaseKey
WHERE destination.UserId IS NULL;

DELETE FROM CaseOpeningOwnedCases
WHERE CaseKey IN ('chicken','katowice-2014-challengers','katowice-2014-legends','cologne-2014-cobblestone-souvenir');

DROP TEMPORARY TABLE CaseOpeningContainerReplacementInventory;

INSERT IGNORE INTO CaseOpeningUnlockedCases(UserId,CaseKey,UnlockedUtc)
SELECT UserId,
       CASE CaseKey
           WHEN 'chicken' THEN 'feral-predators'
           WHEN 'katowice-2014-challengers' THEN 'sticker-capsule-2'
           WHEN 'katowice-2014-legends' THEN 'jackass'
           WHEN 'cologne-2014-cobblestone-souvenir' THEN 'paris-2023-mirage-souvenir'
       END,
       UnlockedUtc
FROM CaseOpeningUnlockedCases
WHERE CaseKey IN ('chicken','katowice-2014-challengers','katowice-2014-legends','cologne-2014-cobblestone-souvenir');

DELETE FROM CaseOpeningUnlockedCases
WHERE CaseKey IN ('chicken','katowice-2014-challengers','katowice-2014-legends','cologne-2014-cobblestone-souvenir');

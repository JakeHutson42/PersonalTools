-- Case Battle JSON input inherits MariaDB's utf8mb4_general_ci connection collation.
-- Keep every table participating in CaseKey joins on that same collation so normal ownership,
-- Buy all, invitations and Battle Bot parity use the exact same comparisons.
USE PersonalTools;

ALTER TABLE CaseOpeningOwnedCases
    MODIFY CaseKey VARCHAR(80) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;

ALTER TABLE CaseOpeningUnlockedCases
    MODIFY CaseKey VARCHAR(80) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;

ALTER TABLE CaseOpeningBattleCases
    MODIFY CaseKey VARCHAR(80) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;

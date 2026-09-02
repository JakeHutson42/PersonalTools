-- Remove obsolete quality-of-life skill nodes and any purchases recorded for them.
USE PersonalTools;

DELETE FROM CaseOpeningUserUpgradeUnlocks
WHERE UpgradeKey IN (
    'instant-repeat','remember-opening-quantity','streamlined-results','batch-reveal',
    'inventory-filters','auto-buy-reserve','auto-buy-case-groups','auto-sell-price-floor',
    'auto-sell-duplicate-copies','auto-sell-wear-filters','trade-up-reserve',
    'pause-automation-storage-low','contract-history-filters'
);

DELETE FROM CaseOpeningUpgradeDefinitions
WHERE UpgradeKey IN (
    'instant-repeat','remember-opening-quantity','streamlined-results','batch-reveal',
    'inventory-filters','auto-buy-reserve','auto-buy-case-groups','auto-sell-price-floor',
    'auto-sell-duplicate-copies','auto-sell-wear-filters','trade-up-reserve',
    'pause-automation-storage-low','contract-history-filters'
);

-- Clear legacy preference values so the removed automation behaviours cannot continue to run
-- for accounts that purchased them before this migration.
UPDATE CaseOpeningUserPreferences
SET LastOpenQuantity=1,AutoBuyReserveMinor=0,FollowSelectedCase=0,SelectedCaseKey=NULL,
    AutoSellProtectAboveMinor=0,AutoSellDuplicateCopies=0,AutoSellWears='',
    TradeUpReserve=0,PauseAutomationFreeSlots=0,UpdatedUtc=UTC_TIMESTAMP(6);

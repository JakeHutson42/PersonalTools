(function ($) {
    'use strict';

    const $page = $('.case-opening-page');
    const caseSelectionStorageKey = 'personalTools.caseOpeningSelectedCase';
    let caseKey = loadSelectedCaseKey(String($page.data('case-key')));
    const $reel = $('#caseReel');
    const $idle = $('#caseReelIdle');
    const $open = $('#openCaseButton');
    const $result = $('#caseResult');
    const $historyCards = $('#caseHistory');
    const $historyTableBody = $('#caseHistoryTableBody');
    const $empty = $('#caseHistoryEmpty');
    let caseData = null;
    let catalogue = [];
    let profileCollections = [];
    let profileCollectionsLoaded = false;
    let profileCollectionsLoading = false;
    const botCaseStorageKey = 'personalTools.caseOpeningBotCase';
    let botCycleInFlight = false;
    let botCycleFailureCount = 0;
    let botStopNotice = '';
    let botProgress = null;
    let botsRunning = false;
    let botTimer = null;
    let botRefreshTimer = null;
    const historyItems = new Map();
    const historyPageSizeStorageKey = 'personalTools.caseOpeningHistoryPageSize';
    const historyViewStorageKey = 'personalTools.caseOpeningHistoryView';
    const historySortStorageKey = 'personalTools.caseOpeningHistorySort';
    const inventoryKindStorageKey = 'personalTools.caseOpeningInventoryKind';
    const collapsePreferenceStorageKey = 'personalTools.caseOpeningCollapsedSections';
    let allHistoryItems = [];
    let filteredHistoryItems = [];
    let historyDirty = false;
    let historyPage = 1;
    let historyPageSize = loadHistoryPageSize();
    let historyView = loadHistoryView();
    let historySort = loadHistorySort();
    let inventoryKind = loadInventoryKind();
    let historySearchTimer = null;
    let postOpeningRefreshTimer = null;
    let postOpeningRefreshRequests = [];
    let sessionOpenings = [];
    const selectedInventoryIds = new Set();
    const skipAnimationStorageKey = 'personalTools.caseOpeningSkipAnimation';
    let caseProgress = null;
    let dailyDropMayHaveCompleted = false;
    let caseTweakProgress = null;
    let caseTweakCatalogue = [];
    let selectedOpenQuantity = 1;
    let sessionStartedAt = Date.now();
    let statisticsRequestedAfterOpening = null;
    const announcedDryStreaks = new Set();
    let opening = false;
    let tradeUpInFlight = false;
    let inspectX = 0;
    let inspectY = 0;
    let inspectPointer = null;
    const soundStorageKey = 'personalTools.caseOpeningSound';
    const soundState = loadSoundState();
    let audioContext = null;
    let masterGain = null;
    let reelSoundTimers = [];
    let rareRevealTimer = null;
    let rareRevealDismiss = null;
    const warmedCaseImages = new Set();
    let achievementSummary = null;
    let achievementKeysLoaded = false;
    let unlockedAchievementKeys = new Set();
    let currentStatistics = null;
    let inventoryCapacity = null;
    let inventoryUpgrades = null;
    let inventoryUpgradesLoaded = false;
    let autoBuySummary = null;
    let autoBuyRulesLoaded = false;
    let autoBuyPollTimer = null;
    let autoBuyEvaluationRequest = null;
    let tradeUpRecipeSummary = null;
    let tradeUpRecipesLoaded = false;
    let tradeUpRecipePollTimer = null;
    let tradeUpRecipeEvaluationRequest = null;
    let tradeUpRecipeModalMode = 'create';
    let tradeUpRecipeModalRecipeId = null;
    const tradeUpCaseItemsCache = new Map();
    const tradeUpEligibleRarities = new Set(['restricted', 'classified', 'covert']);
    const tradeUpWearNames = ['Factory New', 'Minimal Wear', 'Field-Tested', 'Well-Worn', 'Battle-Scarred'];
    const tradeUpWearAbbreviations = { 'Factory New': 'FN', 'Minimal Wear': 'MW', 'Field-Tested': 'FT', 'Well-Worn': 'WW', 'Battle-Scarred': 'BS' };
    const tradeUpMaximumHoldingCapacity = 20;

    function tradeUpWearSummary(wears) {
        if (!Array.isArray(wears) || wears.length === 0) return 'Any wear';
        return wears.map(wear => tradeUpWearAbbreviations[wear] || wear).join(', ');
    }
    let shopSearch = '';
    let shopTier = '';
    let shopType = '';
    let shopSearchTimer = null;
    let ownedCaseCounterFrame = null;
    let stockStateTimer = null;
    const tradeUpSelectionIds = new Set();
    let tradeUpCompletionAttentionActive = false;
    const destinationStorageKey = 'personalTools.caseOpeningDestination';
    const validDestinations = ['upgrades', 'shop', 'open', 'inventory', 'tradeups'];
    let activeDestination = loadDestinationPreference();
    let loadedCaseKey = '';
    let catalogueLoaded = false;
    let progressLoaded = false;
    let achievementsLoaded = false;
    let inventoryCapacityLoaded = false;
    let botProgressLoaded = false;
    let historyLoaded = false;
    const destinationRefreshedAt = new Map();
    const destinationFreshForMs = 15000;
    const marketValuationSessionKey = 'personalTools.caseOpeningMarketValuation';
    const canUseMarketValuation = $('#caseTweakButton').length > 0;
    let priceSnapshotSummary = null;
    let gameSettingsCache = null;
    let marketValuationEnabled = canUseMarketValuation && loadMarketValuationMode();

    function loadDestinationPreference() {
        // The opening machine is the primary experience. Always begin there instead of restoring
        // a previously browsed shop or inventory panel after a refresh/new visit.
        return 'open';
    }

    function loadHistoryPageSize() {
        try {
            const value = Number(localStorage.getItem(historyPageSizeStorageKey));
            return [10, 25, 50, 100].includes(value) ? value : 25;
        } catch {
            return 25;
        }
    }

    function loadMarketValuationMode() {
        try {
            return sessionStorage.getItem(marketValuationSessionKey) === 'true';
        } catch {
            return false;
        }
    }

    function loadHistorySort() {
        try {
            const value = localStorage.getItem(historySortStorageKey);
            return ['newest', 'oldest', 'value-desc', 'value-asc', 'rarity-desc', 'name'].includes(value) ? value : 'newest';
        } catch {
            return 'newest';
        }
    }

    // The chosen case is a harmless device preference. Keeping it locally avoids another
    // database write every time someone browses the catalogue, while surviving a page refresh.
    function loadSelectedCaseKey(fallbackCaseKey) {
        try {
            return localStorage.getItem(caseSelectionStorageKey) || fallbackCaseKey;
        } catch {
            return fallbackCaseKey;
        }
    }

    function saveSelectedCaseKey(selectedCaseKey) {
        try {
            localStorage.setItem(caseSelectionStorageKey, selectedCaseKey);
        } catch {
            // The currently selected case still works when browser storage is unavailable.
        }
    }

    // The desktop presentation is a device preference. Mobile stays in card view because the
    // full history table is not useful at that width.
    function loadHistoryView() {
        try {
            const value = localStorage.getItem(historyViewStorageKey);
            return ['list', 'cards'].includes(value) ? value : 'list';
        } catch {
            return 'list';
        }
    }

    function renderHistoryView() {
        $('.case-history-section').toggleClass('is-card-view', historyView === 'cards');
        $('[data-history-view]').each(function () {
            const active = String($(this).data('history-view')) === historyView;
            $(this).toggleClass('active', active).attr('aria-pressed', active ? 'true' : 'false');
        });
    }

    function loadCollapsePreferences() {
        try {
            const stored = JSON.parse(localStorage.getItem(collapsePreferenceStorageKey) || '{}');
            return stored && typeof stored === 'object' ? stored : {};
        } catch {
            return {};
        }
    }

    function saveCollapsePreference(section, isOpen) {
        try {
            const preferences = loadCollapsePreferences();
            preferences[section] = isOpen;
            localStorage.setItem(collapsePreferenceStorageKey, JSON.stringify(preferences));
        } catch {
            // These layout choices are convenience preferences and should never block the page.
        }
    }

    function renderCollapseToggle($button, isOpen) {
        const labels = {
            collection: ['Show items', 'Hide items'],
            upgrades: ['Show upgrades', 'Hide upgrades'],
            achievements: ['Show achievements', 'Hide achievements'],
            inventory: ['Show inventory', 'Hide inventory']
        };
        const labelsForSection = labels[$button.data('case-collapse-toggle')] || labels.inventory;
        $button
            .attr('aria-expanded', isOpen ? 'true' : 'false')
            .find('span')
            .text(isOpen ? labelsForSection[1] : labelsForSection[0]);
    }

    function achievementIcon(metricKey) {
        const icons = {
            'cases-opened': 'fa-box-open',
            'skins-obtained': 'fa-gun',
            'trade-ups-completed': 'fa-arrow-up-right-dots',
            unlocks: 'fa-lock-open',
            'login-days': 'fa-calendar-check',
            'login-streak': 'fa-fire-flame-curved',
            'collections-completed': 'fa-trophy',
            'rarity-sets-completed': 'fa-gem'
        };
        return icons[metricKey] || 'fa-medal';
    }

    function renderAchievements(summary) {
        achievementSummary = summary || null;
        const achievements = Array.isArray(achievementSummary?.achievements)
            ? achievementSummary.achievements
            : [];
        const nextUnlockedKeys = new Set(achievements
            .filter(achievement => achievement.isUnlocked)
            .map(achievement => String(achievement.achievementKey || '')));
        const newlyUnlocked = achievementKeysLoaded
            ? achievements.filter(achievement => achievement.isUnlocked
                && !unlockedAchievementKeys.has(String(achievement.achievementKey || '')))
            : [];

        const stats = achievementSummary?.stats || {};
        $('#caseAchievementCollections').text(`${Number(stats.completedCollections || 0)} collections · ${Number(stats.completedRaritySets || 0)} rarity sets`);

        const $grid = $('#caseAchievementGrid').empty();
        achievements.forEach(function (achievement) {
            const target = Math.max(1, Number(achievement.targetValue || 1));
            const current = Math.min(target, Math.max(0, Number(achievement.currentValue || 0)));
            const progress = Math.round((current / target) * 100);
            const unlockedClass = achievement.isUnlocked ? 'is-unlocked' : 'is-locked';
            const isNewUnlock = newlyUnlocked.some(item => item.achievementKey === achievement.achievementKey);
            const $card = $('<article>', { class: `col-12 col-md-6 col-xl-4 case-achievement-card ${unlockedClass}${isNewUnlock ? ' is-new-unlock' : ''}` })
                .append($('<div>', { class: 'case-achievement-icon' })
                    .append($('<i>', { class: `fa-solid ${achievementIcon(achievement.metricKey)}`, 'aria-hidden': 'true' })))
                .append($('<div>', { class: 'case-achievement-copy' })
                    .append($('<div>', { class: 'd-flex align-items-start justify-content-between gap-2' })
                        .append($('<strong>').text(achievement.name || 'Achievement'))
                        .append($('<span>', { class: 'case-achievement-reward' })
                            .append($('<i>', { class: 'fa-solid fa-star', 'aria-hidden': 'true' }))
                            .append(document.createTextNode(` ${formatCurrency(Number(achievement.rewardAmountMinor ?? achievement.rewardStars ?? 0), true)}`))))
                    .append($('<p>').text(achievement.description || ''))
                    .append($('<div>', { class: 'case-achievement-progress' })
                        .attr('role', 'progressbar')
                        .attr('aria-valuemin', '0')
                        .attr('aria-valuemax', target)
                        .attr('aria-valuenow', current)
                        .append($('<span>').css('width', `${progress}%`)))
                    .append($('<small>').text(achievement.isUnlocked
                        ? 'Unlocked'
                        : `${current.toLocaleString()} / ${target.toLocaleString()}`)));
            $grid.append($card);
        });

        newlyUnlocked.forEach(function (achievement) {
            window.personalToolsToast?.success({
                title: 'Achievement unlocked',
                message: `${achievement.name} · +${formatCurrency(Number(achievement.rewardAmountMinor ?? achievement.rewardStars ?? 0), true)}.`
            });
        });
        unlockedAchievementKeys = nextUnlockedKeys;
        achievementKeysLoaded = true;
        renderPlayerProfile();
    }

    function initialiseCollapsibleSections() {
        const preferences = loadCollapsePreferences();

        $('[data-case-collapse-toggle]').each(function () {
            const $button = $(this);
            const section = String($button.data('case-collapse-toggle'));
            const targetSelector = String($button.data('case-collapse-target'));
            const $target = $(targetSelector);
            const target = $target.get(0);

            if (!section || !target || !window.bootstrap?.Collapse) return;

            const defaultOpen = String($target.data('case-collapse-default')) === 'open';
            const isOpen = typeof preferences[section] === 'boolean'
                ? preferences[section]
                : defaultOpen;
            const collapse = bootstrap.Collapse.getOrCreateInstance(target, { toggle: false });

            if (isOpen) collapse.show();
            else collapse.hide();

            renderCollapseToggle($button, isOpen);
            $button.on('click', function () {
                collapse.toggle();
            });
            $target.on('shown.bs.collapse hidden.bs.collapse', function (event) {
                const expanded = event.type === 'shown';
                renderCollapseToggle($button, expanded);
                saveCollapsePreference(section, expanded);
            });
        });
    }

    function loadSkipAnimationPreference() {
        try {
            return localStorage.getItem(skipAnimationStorageKey) === 'true';
        } catch {
            return false;
        }
    }

    function saveSkipAnimationPreference(enabled) {
        try {
            localStorage.setItem(skipAnimationStorageKey, enabled ? 'true' : 'false');
        } catch {
            // This visual preference is optional and should not block opening a case.
        }
    }

    function saleValueFor(item) {
        const rarityValue = Number(caseProgress?.saleValues?.[String(item.rarityKey || '')] || 0);
        const multiplier = Number(caseProgress?.caseSaleMultipliers?.[String(item.caseKey || '')] || 1);
        return rarityValue * multiplier;
    }

    function formatMarketMoney(value, includeSign) {
        const number = Number(value);
        if (!Number.isFinite(number)) return '—';
        const sign = includeSign && number > 0 ? '+' : '';
        return `${sign}${new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' }).format(number)}`;
    }

    function inventoryValueText(item, compact) {
        const amount = saleAmountFor(item);
        return compact ? formatCurrency(amount, true) : `${formatCurrency(amount)} on sale`;
    }

    function saleAmountFor(item) {
        if (isGbpEconomy()) {
            if (item?.estimatedPrice == null || !Number.isFinite(Number(item.estimatedPrice))) return 0;
            return Math.round(Number(item.estimatedPrice) * Number(caseProgress?.skinSaleRateBasisPoints || 9250) / 100);
        }
        return saleValueFor(item);
    }

    function resultMarketText(result) {
        if (!isGbpEconomy() || result?.winner?.estimatedPrice == null || !Number.isFinite(Number(result.winner.estimatedPrice))) return '';
        // A result is presenting the skin's market value.  The reduced amount is
        // only relevant when the player later sells it, and is labelled as such
        // in inventory, so it must not be shown here as the item's price.
        return formatCurrency(Number(result.winner.estimatedPrice), true);
    }

    // Level 5 grants Skip Animation instead of a further multiplier - the reel tops out at 3x
    // (level 4), it doesn't get faster still, it gets bypassed entirely.
    function openSpeedMultiplierText(speedLevel) {
        const level = Number(speedLevel || 0);
        const multiplier = 1 + (Math.min(level, 4) * .5);
        const label = `${multiplier % 1 === 0 ? multiplier.toFixed(0) : multiplier.toFixed(1)}x speed`;
        return level >= 5 ? `${label} + instant reveal` : label;
    }

    // Scales the single "Open case" reel spin and multi-open reveal pacing. Read fresh each time
    // rather than cached, so it always reflects the latest purchased level.
    function openSpeedMultiplier() {
        return 1 + (Math.min(Number(caseProgress?.openSpeedLevel || 0), 4) * .5);
    }

    function isGbpEconomy() {
        return String(caseProgress?.economyMode || 'stars').toLowerCase() === 'gbp';
    }

    function activeBalance() {
        return Number(caseProgress?.activeBalanceMinor ?? (isGbpEconomy() ? caseProgress?.gbpPence : caseProgress?.stars) ?? 0);
    }

    function formatCurrency(amountMinor, compact) {
        const amount = Number(amountMinor || 0);
        if (!isGbpEconomy()) return compact ? `${amount.toLocaleString()}★` : `${amount.toLocaleString()} ${amount === 1 ? 'Star' : 'Stars'}`;
        return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', minimumFractionDigits: amount % 100 === 0 ? 0 : 2, maximumFractionDigits: 2 }).format(amount / 100);
    }

    function formatGbpBalance(amountPence) {
        const amount = Number(amountPence || 0);
        return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', minimumFractionDigits: amount % 100 === 0 ? 0 : 2, maximumFractionDigits: 2 }).format(amount / 100);
    }

    function renderUpdatedBalance(result) {
        if (!result || !caseProgress) return;
        const economyMode = String(result.economyMode || caseProgress.economyMode || 'stars').toLowerCase();
        const updated = { ...caseProgress, economyMode: economyMode };
        if (result.starsBalance != null) updated.stars = Number(result.starsBalance);
        if (result.stars != null) updated.stars = Number(result.stars);
        if (result.gbpPence != null) updated.gbpPence = Number(result.gbpPence);
        const active = Number(result.activeBalanceMinor ?? result.balanceMinor);
        if (Number.isFinite(active)) {
            updated.activeBalanceMinor = active;
            if (economyMode === 'gbp') updated.gbpPence = active;
            else updated.stars = active;
        } else {
            updated.activeBalanceMinor = economyMode === 'gbp'
                ? Number(updated.gbpPence || 0)
                : Number(updated.stars || 0);
        }
        renderProgress(updated);
    }

    function activeCaseCost(item, kind) {
        const activeName = kind === 'unlock' ? 'unlockCost' : 'purchaseCost';
        const gbpName = kind === 'unlock' ? 'unlockCostGbpPence' : 'purchaseCostGbpPence';
        const starName = kind === 'unlock' ? 'unlockCostStars' : 'purchaseCostStars';
        return Number(item?.[activeName] ?? (isGbpEconomy() ? item?.[gbpName] : item?.[starName]) ?? 0);
    }

    function renderProgress(progress) {
        caseProgress = progress || null;
        renderDailyDrop();
        renderDailyDropUpgrades();
        marketValuationEnabled = isGbpEconomy();
        $page.toggleClass('is-market-valuation', marketValuationEnabled);
        $page.toggleClass('is-gbp-economy', isGbpEconomy());
        $('.case-shop-stars, .case-profile-stars').attr('aria-label', isGbpEconomy() ? 'GBP balance' : 'Stars balance');
        $('.case-shop-stars i, .case-profile-stars i').toggleClass('fa-star', !isGbpEconomy()).toggleClass('fa-sterling-sign', isGbpEconomy());
        const balance = activeBalance();
        const level = Number(caseProgress?.level || 0);
        const skipUnlocked = caseProgress?.skipAnimationUnlocked === true;
        const multiLevel = Number(caseProgress?.multiOpenLevel || 0);
        const maximumMultiLevel = Number(caseProgress?.maximumMultiOpenLevel || 4);
        const multiUnlocked = multiLevel > 0;
        const speedLevel = Number(caseProgress?.openSpeedLevel || 0);
        const maximumSpeedLevel = Number(caseProgress?.maximumOpenSpeedLevel || 5);
        const multiCost = Number(caseProgress?.multiOpenCost || 0);
        const speedCost = Number(caseProgress?.openSpeedUpgradeCost || 0);
        const multiXpReq = Number(caseProgress?.multiOpenXpRequirement || 0);
        const speedXpReq = Number(caseProgress?.openSpeedUpgradeXpRequirement || 0);
        const multiLevelLocked = multiXpReq > 0 && level < multiXpReq;
        const speedLevelLocked = speedXpReq > 0 && level < speedXpReq;

        $('#caseStarsBalance').text(Number(caseProgress?.stars || 0).toLocaleString());
        $('#caseGbpBalance').text(formatGbpBalance(caseProgress?.gbpPence));
        const usesGbp = isGbpEconomy();
        $('#caseHudStars').toggleClass('d-none', usesGbp);
        $('#caseHudGbp').toggleClass('d-none', !usesGbp);
        $('#caseHudCurrencies').attr('aria-label', usesGbp ? 'GBP balance' : 'Stars balance');
        $('#caseUpgradeStarsBalance, #caseLegacyUpgradeStarsBalance, #caseShopStarsBalance').text(formatCurrency(balance, true));
        const allowanceEnabled = caseProgress?.freeCaseAllowanceEnabled === true;
        const allowanceRemaining = Number(caseProgress?.freeCaseAllowanceRemaining || 0);
        const allowanceQuantity = Number(caseProgress?.freeCaseAllowanceQuantity || 0);
        const refreshUtc = caseProgress?.freeCaseAllowanceRefreshUtc ? new Date(caseProgress.freeCaseAllowanceRefreshUtc) : null;
        $('#caseFreeAllowanceStatus')
            .toggleClass('d-none', !allowanceEnabled)
            .text(allowanceEnabled
                ? `${allowanceRemaining.toLocaleString()} of ${allowanceQuantity.toLocaleString()} free Tier-1 cases remaining${refreshUtc && !Number.isNaN(refreshUtc.getTime()) ? ` · refreshes ${refreshUtc.toLocaleString()}` : ''}`
                : '');
        renderXpBar();
        $('#caseMultiUpgradeCost').text(formatCurrency(multiCost));
        $('#caseSpeedUpgradeCost').text(formatCurrency(speedCost));
        $('#caseSkipUpgrade').toggleClass('is-unlocked', skipUnlocked);
        $('#caseMultiUpgrade').toggleClass('is-unlocked', multiUnlocked);
        $('#caseSpeedUpgrade').toggleClass('is-unlocked', speedLevel > 0);
        renderXpRequirementBadge($('#caseMultiUpgradeXpBadge'), multiXpReq);
        renderXpRequirementBadge($('#caseSpeedUpgradeXpBadge'), speedXpReq);
        $('#caseSkipAnimation')
            .prop('disabled', !skipUnlocked)
            .prop('checked', skipUnlocked && loadSkipAnimationPreference());
        $('#caseSkipAnimationLabel').text(skipUnlocked ? 'Skip long reel animation' : 'Locked');
        $('#caseMultiUpgradeLabel').text(multiLevel >= maximumMultiLevel
            ? `All ${maximumMultiLevel} extra openings unlocked · up to 5 cases`
            : `${multiLevel} of ${maximumMultiLevel} extra openings unlocked · up to ${1 + multiLevel} cases`);
        $('#caseSpeedUpgradeLabel').text(openSpeedMultiplierText(speedLevel));
        // No longer independently purchasable - always disabled, purely informational. It's
        // granted automatically once the speed upgrade reaches its final level (see below).
        $('#unlockSkipAnimation')
            .prop('disabled', true)
            .text(skipUnlocked ? 'Unlocked' : `Unlocks at speed level ${maximumSpeedLevel}`);
        $('#unlockMultiOpen')
            .prop('disabled', multiLevel >= maximumMultiLevel || balance < multiCost || multiLevelLocked)
            .text(multiLevel >= maximumMultiLevel ? 'Fully unlocked' : multiLevelLocked ? `Reach level ${multiXpReq}` : `Unlock +1 for ${formatCurrency(multiCost, true)}`);
        $('#unlockOpenSpeed')
            .prop('disabled', speedLevel >= maximumSpeedLevel || balance < speedCost || speedLevelLocked)
            .text(speedLevel >= maximumSpeedLevel ? 'Fully upgraded' : speedLevelLocked ? `Reach level ${speedXpReq}` : `Upgrade for ${formatCurrency(speedCost, true)}`);
        $('#caseOpenQuantity').removeClass('d-none');

        if (selectedOpenQuantity > 1 + multiLevel) {
            selectedOpenQuantity = 1;
        }

        renderOpenQuantity();
        renderInventorySelection();
        refreshInventorySaleValues();
        // Stars are shared across the Case Opening page. Keep the bot purchase buttons in sync
        // when inventory is sold or another upgrade changes the balance.
        if (botProgress) renderBotProgress({ ...botProgress, activeBalanceMinor: balance, economyMode: caseProgress?.economyMode });
        if ($('#caseSelectorGrid').children().length) renderCaseSelector(catalogue);
        if ($('#caseShopCaseGrid').children().length) renderShop(catalogue);
    }

    function caseTierFor(item) {
        if (Number(item?.tier || 0) > 0) return Number(item.tier);
        const prices = [...new Set(catalogue
            .map(entry => Number(entry.unlockCostStars || 0))
            .sort((left, right) => left - right))];
        const tier = prices.indexOf(Number(item?.unlockCostStars || 0)) + 1;
        return Math.max(1, tier);
    }

    function filteredShopItems(items) {
        const search = shopSearch.trim().toLocaleLowerCase();
        return (Array.isArray(items) ? items : catalogue).filter(function (item) {
            const matchesSearch = !search
                || String(item.name || '').toLocaleLowerCase().includes(search)
                || String(item.type || '').toLocaleLowerCase().includes(search);
            const matchesTier = !shopTier || caseTierFor(item) === Number(shopTier);
            const type = String(item.type || '').toLocaleLowerCase();
            const matchesType = !shopType
                || (shopType === 'sticker' && type.includes('sticker'))
                || (shopType === 'souvenir' && type.includes('souvenir'))
                || (shopType === 'case' && !type.includes('sticker') && !type.includes('souvenir'));
            return matchesSearch && matchesTier && matchesType;
        });
    }

    function renderShop(items) {
        const stars = activeBalance();
        const shopItems = filteredShopItems(items);
        shopItems.sort((left, right) => Number(left.tier || 0) - Number(right.tier || 0)
            || String(left.name || '').localeCompare(String(right.name || '')));
        const tierValues = [...new Set(catalogue.map(item => caseTierFor(item)))].sort((left, right) => left - right);
        const $tier = $('#caseShopTier');
        const selectedTier = shopTier;
        $tier.empty().append($('<option>', { value: '', text: 'All tiers' }));
        tierValues.forEach(tier => $tier.append($('<option>', { value: tier, text: `Tier ${tier}` })));
        $tier.val(selectedTier);

        $('#caseShopResultCount').text(`${shopItems.length.toLocaleString()} of ${catalogue.length.toLocaleString()} cases`);

        const $grid = $('#caseShopCaseGrid').empty();
        shopItems.forEach(function (item) {
            const unlocked = isCaseUnlocked(item);
            const owned = Number(item.ownedQuantity || 0);
            const $action = unlocked
                ? createCaseShopPurchaseControls(item, stars)
                : $('<button>', { class: 'btn btn-outline-secondary btn-sm js-shop-unlock-case', type: 'button', 'data-case-key': item.caseKey, disabled: stars < activeCaseCost(item, 'unlock'), text: `Unlock · ${formatCurrency(activeCaseCost(item, 'unlock'), true)}` });
            $grid.append($('<div>', { class: 'col-12 col-md-6 col-xl-4' }).append(
                $('<article>', { class: 'case-shop-case', 'data-case-key': item.caseKey }).append(
                    $('<img>', { src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                    $('<div>', { class: 'case-shop-case-copy' }).append(
                        $('<small>', { class: 'small-muted', text: item.type }),
                        $('<strong>', { text: item.name }),
                        $('<span>', { class: 'case-shop-tier', text: `Tier ${caseTierFor(item)}` }),
                        $('<span>', { class: unlocked ? 'case-shop-owned' : 'case-shop-locked', text: unlocked ? `${owned.toLocaleString()} owned` : `Level ${Math.max(1, Number(item.xpRequirement || 0))} · permanent unlock` })
                    ),
                    $action
                )
            ));
        });
        if (shopItems.length === 0) {
            $grid.append($('<div>', { class: 'col-12' }).append(
                $('<div>', { class: 'case-shop-empty', text: 'No cases or capsules match those filters.' })
            ));
        }
        $('#caseShopTypeFilter [data-shop-type]').each(function () {
            const isActive = String($(this).data('shop-type') || '') === shopType;
            $(this).toggleClass('is-active', isActive).attr('aria-pressed', String(isActive));
        });

        const capacity = inventoryCapacity || {};
        const count = Number(capacity.storageContainerCount || 0);
        const maximum = Number(caseProgress?.maximumStorageContainers || 0);
        const cost = Number(caseProgress?.storageContainerBaseCost || 0) + (count * Number(caseProgress?.storageContainerCostIncrement || 0));
        $('#caseShopStorageStatus').text(`${count} / ${maximum} owned`);
        $('#caseShopStorageCapacity').text(
            `${Number(capacity.totalCapacity || capacity.baseCapacity || 1000).toLocaleString()} slots · ${Number(capacity.skinSlots || 0).toLocaleString()} skins · ${Number(capacity.caseSlots || 0).toLocaleString()} cases`
        );
        $('#caseShopStorageCopy').text(`Adds ${Number(caseProgress?.storageContainerSlots || 1000).toLocaleString()} permanent inventory slots. Next container: ${formatCurrency(cost)}.`);
        $('#purchaseStorageContainer').prop('disabled', count >= maximum || stars < cost).text(count >= maximum ? 'Storage limit reached' : `Buy · ${formatCurrency(cost, true)}`);

        const skipUnlocked = caseProgress?.skipAnimationUnlocked === true;
        const multiLevel = Number(caseProgress?.multiOpenLevel || 0);
        const maxMulti = Number(caseProgress?.maximumMultiOpenLevel || 0);
        const speedLevel = Number(caseProgress?.openSpeedLevel || 0);
        const maxSpeed = Number(caseProgress?.maximumOpenSpeedLevel || 0);
        $('#caseShopUpgradeGrid').empty().append(
            shopUpgradeCard('open-speed', 'Faster opening', `Speed up your opening reel and multi-open reveals. Currently ${openSpeedMultiplierText(speedLevel)}.`, speedLevel >= maxSpeed, Number(caseProgress?.openSpeedUpgradeCost || 0)),
            shopUpgradeCard('skip-animation', 'Skip animation', skipUnlocked ? 'Show your secure result immediately with a compact reveal.' : `Granted automatically at Faster opening level ${maxSpeed}.`, skipUnlocked, 0),
            shopUpgradeCard('multi-open', 'Multi case opening', `Unlock another simultaneous opening. Currently ${1 + multiLevel} at a time.`, multiLevel >= maxMulti, Number(caseProgress?.multiOpenCost || 0))
        );
    }

    // The Shop deliberately uses fixed quantity buttons rather than a freeform amount input.
    // It makes repeated purchases faster on touch devices while the API remains the authority.
    function createCaseShopPurchaseControls(item, stars) {
        const caseKeyForPurchase = String(item.caseKey || '');
        const unitPrice = Math.max(0, activeCaseCost(item, 'purchase'));
        const availableSlots = Math.max(0, Number(inventoryCapacity?.availableSlots || 0));
        const affordableQuantity = unitPrice > 0 ? Math.floor(stars / unitPrice) : 500;
        const maximumQuantity = Math.min(500, affordableQuantity, availableSlots);
        const $controls = $('<div>', { class: 'case-shop-purchase', 'data-unit-price': unitPrice });
        [1, 10, 50, 100].forEach(function (quantity) {
            $controls.append($('<button>', {
                class: 'btn btn-outline-warning btn-sm js-shop-buy-case', type: 'button',
                'data-case-key': caseKeyForPurchase, 'data-quantity': quantity,
                disabled: quantity > maximumQuantity,
                text: `Buy ${quantity}`
            }));
        });
        $controls.append($('<button>', {
            class: 'btn btn-warning btn-sm js-shop-buy-case case-shop-buy-max', type: 'button',
            'data-case-key': caseKeyForPurchase, 'data-quantity': maximumQuantity,
            disabled: maximumQuantity < 1,
            text: maximumQuantity > 0 ? `Max · ${maximumQuantity}` : 'Max'
        }));
        $controls.append($('<small>', {
            class: 'case-shop-unit-price',
            text: `${formatCurrency(unitPrice)} each · ${availableSlots.toLocaleString()} inventory slots available`
        }));
        return $controls;
    }

    function shopUpgradeCard(key, title, description, unlocked, cost) {
        const switchId = key === 'skip-animation' && unlocked ? 'caseShopSkipAnimation' : '';
        let $action;
        if (switchId) {
            $action = $('<div>', { class: 'form-check form-switch m-0' }).append(
                $('<input>', { class: 'form-check-input pt-switch js-shop-skip-toggle', id: switchId, type: 'checkbox', role: 'switch', checked: loadSkipAnimationPreference() }),
                $('<label>', { class: 'form-check-label small fw-semibold', for: 'caseShopSkipAnimation', text: 'Use quick open' })
            );
        } else if (key === 'skip-animation') {
            // No longer independently purchasable - earned via the Faster opening upgrade.
            $action = $('<span>', { class: 'badge text-bg-secondary-subtle border', text: 'Locked' });
        } else {
            $action = $('<button>', { class: 'btn btn-outline-warning btn-sm js-shop-unlock-upgrade', type: 'button', 'data-upgrade-key': key, disabled: unlocked || activeBalance() < cost, text: unlocked ? 'Unlocked' : `Unlock · ${formatCurrency(cost, true)}` });
        }
        const $row = $('<article>', {
            class: `case-shop-row${switchId ? ' case-shop-switch-row' : ''}`,
            'data-switch-target': switchId
        }).append(
            $('<div>').append($('<h3>', { class: 'h6 mb-1', text: title }), $('<p>', { class: 'small-muted mb-0', text: description })), $action
        );
        return $('<div>', { class: 'col-12 col-md-6' }).append($row);
    }

    function loadInventoryKind() {
        try {
            return localStorage.getItem(inventoryKindStorageKey) === 'cases' ? 'cases' : 'skins';
        } catch {
            return 'skins';
        }
    }

    function renderXpRequirementBadge($badge, requirement) {
        if (!requirement || requirement <= 0) {
            $badge.addClass('d-none').text('');
            return;
        }
        $badge.removeClass('d-none').text(`Lv ${requirement}`);
    }

    // Mirrors CaseOpeningXpLevels in C#: level N needs an additional 100*N xp beyond level N-1.
    function xpCumulativeForLevel(level) {
        return 100 * level * (level + 1) / 2;
    }

    function xpLevelForTotal(xp) {
        let level = 0;
        while (xp >= xpCumulativeForLevel(level + 1)) level += 1;
        return level;
    }

    function renderXpBar() {
        const xp = Number(caseProgress?.xp || 0);
        const level = xpLevelForTotal(xp);
        const xpIntoLevel = xp - xpCumulativeForLevel(level);
        const xpForNextLevel = Math.max(1, xpCumulativeForLevel(level + 1) - xpCumulativeForLevel(level));
        const percentage = Math.max(0, Math.min(100, Math.round((xpIntoLevel / xpForNextLevel) * 100)));

        $('#caseXpLevelBadge').text(`Lv ${level}`);
        $('#caseXpText').text(`${xpIntoLevel} / ${xpForNextLevel} XP`);
        $('#caseXpFill').css('width', `${percentage}%`);
        $('#caseXpTrack').attr('aria-valuenow', String(percentage));
        renderPlayerProfile();
    }

    function renderDailyDrop() {
        const daily = caseProgress?.dailyDrop || {};
        const required = Math.max(1, Number(daily.requiredXp || 100));
        const percentage = Math.min(100, Math.max(0, Math.round((Number(daily.xp || 0) / required) * 100)));
        const label = percentage >= 100 ? '100% · Ready' : percentage >= 50 ? '50%' : '1%';
        $('#caseDailyDropFill').css('width', `${percentage}%`);
        $('#caseDailyDropText').text(label);
        $('#caseDailyDropTrack').attr('aria-valuenow', String(percentage));
        const ready = daily.isCompleted && !daily.isClaimed && Array.isArray(daily.rewards) && daily.rewards.length === 4;
        $('#caseDailyDrop').toggleClass('is-ready', ready)
            .attr('aria-hidden', 'false')
            .attr('aria-label', ready ? 'Open Daily Drop rewards' : `Daily Drop progress: ${label}`);
        $('#caseDailyDropRecall').toggleClass('d-none', !ready);
        $('.case-game-hud').toggleClass('has-daily-drop-ready', ready);
        if (ready && dailyDropMayHaveCompleted) {
            dailyDropMayHaveCompleted = false;
            window.setTimeout(openDailyDropModal, 250);
        }
    }

    function openDailyDropModal() {
        const rewards = caseProgress?.dailyDrop?.rewards || [];
        if (rewards.length !== 4) return;
        const $grid = $('#caseDailyDropRewards').empty();
        rewards.forEach(function (reward) {
            const $card = $('<label>', { class: 'col-12 col-md-6 col-xl-3 case-daily-reward-option' });
            const $input = $('<input>', { type: 'checkbox', class: 'visually-hidden js-daily-drop-choice', value: reward.rewardKey });
            const $art = $('<span>', { class: 'case-daily-reward-showcase' });
            if (reward.imageUrl) {
                $art.append($('<img>', { class: 'case-daily-reward-image', src: reward.imageUrl, alt: '' }));
            } else {
                $art.append($('<i>', { class: 'fa-solid fa-gift case-daily-reward-icon', 'aria-hidden': 'true' }));
            }
            $card.append($input, $('<span>', { class: 'case-daily-reward-select' }).append($('<i>', { class: 'fa-solid fa-check', 'aria-hidden': 'true' })).append($('<span>', { text: 'Choose' })),
                $art,
                $('<span>', { class: 'case-daily-reward-copy' }).append($('<small>', { text: 'Daily reward' })).append($('<strong>').text(reward.name)).append($('<span>').text(reward.description)),
                $('<span>', { class: 'case-daily-reward-free', text: 'Included' }));
            $grid.append($card);
        });
        $('#caseDailyDropClaim').prop('disabled', true);
        $('#caseDailyDropSelectionStatus').text('0 / 2 rewards selected');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('caseDailyDropModal')).show();
    }

    function renderDailyDropUpgrades() {
        const $tree = $('#caseDailyUpgradeTreeCards').empty();
        (caseProgress?.dailyDrop?.upgrades || []).forEach(function (upgrade) {
            const level = Number(upgrade.level || 0);
            const maximumLevel = Math.max(1, Number(upgrade.maximumLevel || 1));
            const complete = level >= maximumLevel;
            const progressed = level > 0 && !complete;
            const icon = { focus: 'fa-bullseye', cash: 'fa-sack-dollar', 'case-stash': 'fa-box-open', quality: 'fa-gem' }[upgrade.upgradeKey] || 'fa-sparkles';
            const state = complete ? 'is-complete' : progressed ? 'is-progressed' : 'is-dormant';
            const $card = $('<article>', { class: `case-daily-tree-node ${state}`, tabindex: '0' })
                .append($('<span>', { class: 'case-daily-tree-connector', 'aria-hidden': 'true' }))
                .append($('<span>', { class: 'case-daily-tree-emblem', 'aria-hidden': 'true' }).append($('<i>', { class: `fa-solid ${icon}` })))
                .append($('<span>', { class: 'case-daily-tree-level' }).text(`Rank ${level} / ${maximumLevel}`))
                .append($('<strong>').text(upgrade.name))
                .append($('<p>').text(upgrade.description));
            const label = complete ? 'Maxed' : `Upgrade · ${formatCurrency(Number(upgrade.cost || 0), true)}`;
            $card.append($('<span>', { class: 'case-daily-tree-tooltip', role: 'tooltip' }).append($('<b>').text(upgrade.name)).append($('<span>').text(upgrade.description)).append($('<em>').text(complete ? 'Fully attuned' : `${maximumLevel - level} rank${maximumLevel - level === 1 ? '' : 's'} remaining`)));
            $card.append($('<button>', { type: 'button', class: complete ? 'btn btn-outline-secondary btn-sm' : 'btn btn-warning btn-sm js-daily-upgrade', disabled: complete, 'data-upgrade-key': upgrade.upgradeKey }).text(label));
            $tree.append($card);
        });
    }

    function showDailyDropCarousel() {
        const $daily = $('#caseDailyDrop');
        if (!$daily.length || $daily.hasClass('is-ready') || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        $daily.removeClass('is-leaving').addClass('is-showing');
        window.clearTimeout(showDailyDropCarousel.timer);
        showDailyDropCarousel.timer = window.setTimeout(function () {
            $daily.removeClass('is-showing').addClass('is-leaving');
        }, 2300);
    }

    function renderPlayerProfile() {
        const xp = Number(caseProgress?.xp || 0);
        const level = xpLevelForTotal(xp);
        const xpIntoLevel = xp - xpCumulativeForLevel(level);
        const xpForNextLevel = Math.max(1, xpCumulativeForLevel(level + 1) - xpCumulativeForLevel(level));
        const xpPercentage = Math.max(0, Math.min(100, Math.round((xpIntoLevel / xpForNextLevel) * 100)));
        const achievementStats = achievementSummary?.stats || {};
        const achievementsUnlocked = Number(achievementSummary?.unlockedCount || 0);
        const achievementsTotal = Number(achievementSummary?.totalCount || 0);
        const achievementCompletion = achievementsTotal > 0 ? Math.round((achievementsUnlocked / achievementsTotal) * 100) : 0;
        const inventoryUsed = Number(inventoryCapacity?.usedSlots || 0);
        const inventoryTotal = Number(inventoryCapacity?.totalCapacity || 1000);
        const botCount = (botProgress?.servers || []).reduce((count, server) => count + (server.bots || []).length, 0);
        const unlockedCases = new Set((caseProgress?.unlockedCaseKeys || []).map(key => String(key).toLowerCase()));
        catalogue.filter(isCaseUnlocked).forEach(item => unlockedCases.add(String(item.caseKey || '').toLowerCase()));

        $('#caseProfileLevel').text(`Lv ${level}`);
        $('#caseProfileXp').text(`${xpIntoLevel.toLocaleString()} / ${xpForNextLevel.toLocaleString()} XP`);
        $('#caseProfileXpFill').css('width', `${xpPercentage}%`);
        $('#caseProfileXpTrack').attr('aria-valuenow', String(xpPercentage));
        $('#caseProfileStars').text(formatCurrency(activeBalance(), true));
        $('#caseProfileCapacity').text(`${inventoryUsed.toLocaleString()} / ${inventoryTotal.toLocaleString()} inventory slots`);
        const casesOpened = Number(achievementStats.totalCasesOpened || 0);
        const casesPurchased = Number(achievementStats.totalCasesPurchased || 0);
        const skinsObtained = Number(achievementStats.totalSkinsObtained || 0);
        const tradeUps = Number(achievementStats.totalTradeUpsCompleted || 0);
        const statTrakPulls = Number(achievementStats.totalStatTrakPulls || 0);
        const upgradesPurchased = Number(achievementStats.totalUpgradesPurchased || 0);
        $('#caseProfileCasesOpened').text(casesOpened.toLocaleString());
        $('#caseProfileCasesPurchased').text(casesPurchased.toLocaleString());
        $('#caseProfileSkinsObtained').text(skinsObtained.toLocaleString());
        $('#caseProfileTradeUps').text(tradeUps.toLocaleString());
        $('#caseProfileStatTrakPulls').text(statTrakPulls.toLocaleString());
        $('#caseProfileUpgradesPurchased').text(upgradesPurchased.toLocaleString());
        const loginStreak = Number(achievementStats.currentLoginStreak || 0);
        $('#caseProfileLoginStreak').text(`${loginStreak} ${loginStreak === 1 ? 'day' : 'days'}`);
        $('#caseProfileAchievementStars').text(formatCurrency(Number(achievementSummary?.earnedAmountMinor ?? achievementSummary?.earnedStars ?? 0), true));
        $('#caseProfileAchievementCompletion').text(`${achievementCompletion}%`);
        $('#caseProfileCasesUnlocked').text(unlockedCases.size.toLocaleString());
        $('#caseProfileMultiOpen').text((1 + Number(caseProgress?.multiOpenLevel || 0)).toLocaleString());
        $('#caseProfileBots').text(botCount.toLocaleString());

        const rarityPulls = [
            ['MilSpec', Number(achievementStats.totalMilSpecPulls || 0)],
            ['Restricted', Number(achievementStats.totalRestrictedPulls || 0)],
            ['Classified', Number(achievementStats.totalClassifiedPulls || 0)],
            ['Covert', Number(achievementStats.totalCovertPulls || 0)],
            ['RareSpecial', Number(achievementStats.totalRareSpecialPulls || 0)]
        ];
        const trackedPulls = rarityPulls.reduce((total, rarity) => total + rarity[1], 0);
        $('#caseProfileTrackedPulls').text(`${trackedPulls.toLocaleString()} tracked`);
        rarityPulls.forEach(function ([key, count]) {
            const percentage = trackedPulls > 0 ? Math.max(count > 0 ? 1.5 : 0, (count / trackedPulls) * 100) : 0;
            $(`#caseProfile${key}Pulls`).text(count.toLocaleString());
            $(`#caseProfile${key}Fill`).css('width', `${percentage}%`);
        });

        const currentStars = activeBalance();
        const starsSpent = Number(isGbpEconomy() ? achievementStats.totalGbpPenceSpent : achievementStats.totalStarsSpent || 0);
        const caseSpend = Number(isGbpEconomy() ? achievementStats.totalGbpCasePurchasePenceSpent : achievementStats.totalCasePurchaseStarsSpent || 0);
        const saleStars = Number(isGbpEconomy() ? achievementStats.totalGbpSalePenceEarned : achievementStats.totalSaleStarsEarned || 0);
        const pullValue = Number(isGbpEconomy() ? achievementStats.totalGbpPenceEarned : achievementStats.totalPullValueStars || 0);
        const levelRewardStars = Number(isGbpEconomy() ? achievementStats.totalGbpLevelRewardPence : achievementStats.totalLevelRewardStars || 0);
        const achievementRewardStars = Number(isGbpEconomy() ? achievementStats.totalGbpAchievementRewardPence : achievementSummary?.earnedStars || 0);
        const trackedStarsEarned = isGbpEconomy() ? Number(achievementStats.totalGbpPenceEarned || 0) : saleStars + levelRewardStars + achievementRewardStars;
        const netCaseValue = trackedStarsEarned - starsSpent;
        $('#caseProfileEconomyStars').text(formatCurrency(activeBalance(), true));
        $('#caseProfileStarsSpent').text(formatCurrency(starsSpent, true));
        $('#caseProfileStarsEarned').text(formatCurrency(trackedStarsEarned, true));
        $('#caseProfileCaseSpend').text(formatCurrency(caseSpend, true));
        $('#caseProfileSaleStars').text(formatCurrency(saleStars, true));
        $('#caseProfilePullValue').text(formatCurrency(pullValue, true));
        $('#caseProfileLevelRewardStars').text(formatCurrency(levelRewardStars, true));
        $('#caseProfileAchievementRewardStars').text(formatCurrency(achievementRewardStars, true));
        $('#caseProfileNetValue')
            .text(`${netCaseValue > 0 ? '+' : ''}${formatCurrency(netCaseValue, true)}`)
            .toggleClass('is-positive', netCaseValue > 0)
            .toggleClass('is-negative', netCaseValue < 0);

        if (currentStatistics) {
            $('#caseProfileStatisticsCase').text(`Statistics for ${currentStatistics.caseName}.`);
            $('#caseProfileRarePulls').text(Number(currentStatistics.targetPulls || 0).toLocaleString());
            $('#caseProfileDryStreak').text(Number(currentStatistics.currentDryStreak || 0).toLocaleString());
        }
    }

    function playLevelUpAnimation(level) {
        const $toast = $('#caseLevelUpToast');
        $('#caseLevelUpText').text(`Level ${level}`);
        $toast.removeClass('d-none').addClass('is-visible');
        playLevelUp();
        window.clearTimeout(playLevelUpAnimation.timer);
        playLevelUpAnimation.timer = window.setTimeout(function () {
            $toast.removeClass('is-visible');
            window.setTimeout(() => $toast.addClass('d-none'), 300);
        }, 2200);
    }

    const xpBubbleLifeMs = 1300;

    // Finds the element the XP bubble should appear beside - the actual revealed skin wherever it
    // currently is, falling back to the reel window when a result has not mounted yet.
    function resultImageOrigin(result) {
        const $goldImage = $('#caseGoldRevealImage');
        if ($goldImage.length && !$goldImage.closest('#caseGoldReveal').hasClass('d-none')) return $goldImage;

        const $skipCard = $reel.find('.case-skip-result');
        if ($skipCard.length) return $skipCard;

        const $multiCard = $('#caseMultiResults .case-multi-result').last();
        if ($multiCard.length) return $multiCard;

        const $winnerCard = result ? $reel.children().eq(result.winnerIndex) : $();
        if ($winnerCard.length) return $winnerCard;

        return $('#caseReelWindow');
    }

    // XP is presentation-only at this point: the server has already awarded it. Keeping this as a
    // short CSS animation means opening controls never wait for a decorative transition to finish.
    function showXpBubble(amount, $origin) {
        if (!amount) return;
        const $source = $origin && $origin.length ? $origin : $('#caseReelWindow');
        if (!$source.length) return;

        const sourceRect = $source[0].getBoundingClientRect();
        const $bubble = $('<span class="case-xp-bubble">')
            .text(`+${amount}`)
            .toggleClass('is-long', String(amount).length >= 3)
            .css({
                left: sourceRect.left + (sourceRect.width / 2),
                top: sourceRect.top + (sourceRect.height * 0.22)
            });
        $('body').append($bubble);
        window.setTimeout(() => $bubble.remove(), xpBubbleLifeMs);
    }

    const botXpBubbleStaggerMs = 110;
    let nextBotXpBubbleAt = 0;

    function scheduleBotXpBubble(amount, $origin) {
        const now = Date.now();
        nextBotXpBubbleAt = Math.max(now, nextBotXpBubbleAt) + botXpBubbleStaggerMs;
        window.setTimeout(() => showXpBubble(amount, $origin), nextBotXpBubbleAt - now);
    }

    // Multi-opens resolve each result card independently and bot drops are staggered so several
    // results landing together remain readable. XP totals are accumulated from server deltas to
    // prevent older concurrent responses from moving the progress bar backwards.
    function awardXp(results, origin, staggerBubbles) {
        if (!Array.isArray(results) || !results.length || !caseProgress) return;
        const resolveOrigin = typeof origin === 'function' ? origin : () => origin;
        results.forEach(function (result, index) {
            const amount = Number(result.xpAwarded || 0);
            const $origin = resolveOrigin(result, index);
            if (staggerBubbles) scheduleBotXpBubble(amount, $origin);
            else showXpBubble(amount, $origin);
        });

        const xpGained = results.reduce((sum, item) => sum + Number(item.xpAwarded || 0), 0);
        // Only arm the reward picker while this opening can actually complete a new Daily Drop.
        // Once a ready drop has been dismissed, later spins must leave it closed until the user
        // deliberately reopens it from the Daily Drop control.
        dailyDropMayHaveCompleted = caseProgress?.dailyDrop?.isCompleted !== true
            && caseProgress?.dailyDrop?.isClaimed !== true;
        const levelBefore = xpLevelForTotal(Number(caseProgress.xp || 0));
        const totalXp = Number(caseProgress.xp || 0) + xpGained;
        const levelAfter = xpLevelForTotal(totalXp);
        const levelRewardStars = results.reduce((sum, item) => sum + Number(item.levelRewardStars || 0), 0);
        const levelRewardAmount = results.reduce((sum, item) => sum + Number(item.levelRewardAmountMinor ?? item.levelRewardStars ?? 0), 0);

        caseProgress = {
            ...caseProgress,
            xp: totalXp,
            stars: isGbpEconomy() ? Number(caseProgress.stars || 0) : Number(caseProgress.stars || 0) + levelRewardStars,
            gbpPence: isGbpEconomy() ? Number(caseProgress.gbpPence || 0) + levelRewardAmount : Number(caseProgress.gbpPence || 0),
            activeBalanceMinor: activeBalance() + levelRewardAmount
        };
        if (levelRewardStars > 0) renderProgress(caseProgress);
        else renderXpBar();

        if (levelAfter > levelBefore) playLevelUpAnimation(levelAfter);
        if (levelRewardStars > 0) {
            window.personalToolsToast?.success(`Level reward claimed: +${formatCurrency(levelRewardAmount, true)}.`);
        }
    }

    function isCaseUnlocked(item) {
        return item?.isUnlocked === true || (caseProgress?.unlockedCaseKeys || [])
            .some(key => String(key).toLowerCase() === String(item?.caseKey || '').toLowerCase());
    }

    function renderOpenQuantity() {
        const availableQuantity = 1 + Number(caseProgress?.multiOpenLevel || 0);
        const ownedQuantity = Number(caseData?.ownedQuantity || 0);
        $('[data-open-quantity]').each(function () {
            const quantity = Number($(this).data('open-quantity'));
            const active = quantity === selectedOpenQuantity;
            $(this)
                .prop('disabled', opening || quantity > availableQuantity || quantity > ownedQuantity)
                .toggleClass('active', active)
                .attr('aria-pressed', active ? 'true' : 'false');
        });
        if (!opening) renderOpenButton('ready');
    }

    function renderOwnedCaseQuantity(options) {
        const quantity = Number(caseData?.ownedQuantity || 0);
        const from = Number(options?.from);
        const shouldAnimate = options?.animate === true
            && Number.isFinite(from)
            && from !== quantity
            && !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const $counter = $('#caseOwnedQuantity')
            .toggleClass('is-empty', quantity < 1)
            .attr('aria-label', `${quantity.toLocaleString()} ${quantity === 1 ? 'case' : 'cases'} ready`);
        const $value = $('#caseOwnedQuantityValue');

        window.cancelAnimationFrame(ownedCaseCounterFrame);
        $counter.removeClass('is-decrementing is-incrementing');
        if (!shouldAnimate) {
            $value.text(quantity.toLocaleString());
            renderCaseStockState();
            return;
        }

        const difference = quantity - from;
        const duration = Math.min(760, 360 + (Math.abs(difference) * 55));
        const startedAt = performance.now();
        $counter.addClass(difference < 0 ? 'is-decrementing' : 'is-incrementing');

        function tick(timestamp) {
            const progress = Math.min(1, (timestamp - startedAt) / duration);
            const eased = 1 - Math.pow(1 - progress, 3);
            const displayed = Math.round(from + (difference * eased));
            $value.text(displayed.toLocaleString());
            if (progress < 1) {
                ownedCaseCounterFrame = window.requestAnimationFrame(tick);
                return;
            }

            $value.text(quantity.toLocaleString());
            window.setTimeout(() => $counter.removeClass('is-decrementing is-incrementing'), 260);
            renderCaseStockState();
        }

        ownedCaseCounterFrame = window.requestAnimationFrame(tick);
    }

    function renderCaseStockState() {
        const outOfStock = Number(caseData?.ownedQuantity || 0) < 1;
        // Keep the last result visible after the final owned case is opened. The empty-stock hero
        // takes over on initial load, case selection, or as soon as stock is replenished.
        const resultVisible = !$result.hasClass('d-none');
        const showEmptyState = outOfStock && !opening && !resultVisible;
        $('.case-machine').toggleClass('d-none', showEmptyState);
        $('#caseNoStock').toggleClass('d-none', !showEmptyState);
        if (caseData?.imageUrl) $('#caseNoStockImage').attr('src', caseData.imageUrl);
    }

    function renderInventoryCapacity(capacity) {
        inventoryCapacity = capacity || null;
        const skins = Number(inventoryCapacity?.skinSlots || 0);
        const cases = Number(inventoryCapacity?.caseSlots || 0);
        const used = Number(inventoryCapacity?.usedSlots || 0);
        const total = Number(inventoryCapacity?.totalCapacity || 1000);
        const containers = Number(inventoryCapacity?.storageContainerCount || 0);
        const storageNote = containers > 0 ? ` · ${containers} storage ${containers === 1 ? 'container' : 'containers'}` : '';
        $('#caseInventoryCapacity').text(
            `${used.toLocaleString()} / ${total.toLocaleString()} slots used · ${skins.toLocaleString()} skins · ${cases.toLocaleString()} cases${storageNote}`
        );
        $('#caseHudCapacity')
            .toggleClass('is-active', used >= total * .9)
            .attr('title', `${used.toLocaleString()} of ${total.toLocaleString()} inventory slots used`)
            .find('span')
            .text(`${used.toLocaleString()} / ${total.toLocaleString()}`);
        renderPlayerProfile();
        if ($('#caseShopCaseGrid').children().length) renderShop(catalogue);
    }

    // Sound is a device preference rather than user data, so it is kept locally and never delays an opening request.
    function loadSoundState() {
        const fallback = { enabled: true, volume: 0.45, theme: 'classic' };
        try {
            const saved = JSON.parse(localStorage.getItem(soundStorageKey));
            if (!saved || typeof saved !== 'object') return fallback;
            return {
                enabled: saved.enabled !== false,
                volume: Math.max(0, Math.min(1, Number(saved.volume) || 0)),
                theme: ['classic', 'csgo', 'cs2'].includes(saved.theme) ? saved.theme : 'classic'
            };
        } catch {
            return fallback;
        }
    }

    function saveSoundState() {
        try {
            localStorage.setItem(soundStorageKey, JSON.stringify(soundState));
        } catch {
            // A blocked storage API should not prevent the simulator from working for this visit.
        }
    }

    function ensureAudioContext() {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) return null;
        if (!audioContext) {
            audioContext = new AudioContext();
            masterGain = audioContext.createGain();
            masterGain.connect(audioContext.destination);
        }
        if (audioContext.state === 'suspended') audioContext.resume().catch(() => { });
        masterGain.gain.setTargetAtTime(soundState.enabled ? soundState.volume : 0, audioContext.currentTime, 0.015);
        return audioContext;
    }

    // Mobile Safari and installed PWAs only permit Web Audio after a direct user gesture.
    // Starting a one-sample source during the tap reliably unlocks the context without making
    // an audible sound; normal opening sounds can then be scheduled without delaying the roll.
    function unlockAudioContext() {
        if (!soundState.enabled || soundState.volume <= 0) return;
        const context = ensureAudioContext();
        if (!context) return;
        try {
            const source = context.createBufferSource();
            source.buffer = context.createBuffer(1, 1, context.sampleRate);
            source.connect(masterGain);
            source.start(0);
        } catch {
            // Audio support is optional; an unsupported device must never block an opening.
        }
    }

    function tone(frequency, duration, type, level, delay) {
        if (!soundState.enabled || soundState.volume <= 0) return;
        const context = ensureAudioContext();
        if (!context) return;
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        const start = context.currentTime + (delay || 0);
        oscillator.type = type || 'sine';
        oscillator.frequency.setValueAtTime(frequency, start);
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.exponentialRampToValueAtTime(Math.max(0.0001, level || 0.08), start + 0.008);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
        oscillator.connect(gain);
        gain.connect(masterGain);
        oscillator.start(start);
        oscillator.stop(start + duration + 0.02);
    }

    function noise(duration, level) {
        if (!soundState.enabled || soundState.volume <= 0) return;
        const context = ensureAudioContext();
        if (!context) return;
        const sampleCount = Math.max(1, Math.floor(context.sampleRate * duration));
        const buffer = context.createBuffer(1, sampleCount, context.sampleRate);
        const data = buffer.getChannelData(0);
        for (let index = 0; index < sampleCount; index += 1) {
            data[index] = (Math.random() * 2 - 1) * (1 - (index / sampleCount));
        }
        const source = context.createBufferSource();
        const gain = context.createGain();
        gain.gain.value = level;
        source.buffer = buffer;
        source.connect(gain);
        gain.connect(masterGain);
        source.start();
    }

    function clearReelSounds() {
        reelSoundTimers.forEach(timer => window.clearTimeout(timer));
        reelSoundTimers = [];
    }

    function playOpeningStart() {
        ensureAudioContext();
        if (soundState.theme === 'csgo') {
            tone(78, 0.14, 'sawtooth', 0.05);
            tone(116, 0.2, 'square', 0.04, 0.055);
            noise(0.065, 0.018);
            return;
        }
        if (soundState.theme === 'cs2') {
            tone(148, 0.12, 'sine', 0.045);
            tone(296, 0.19, 'triangle', 0.04, 0.045);
            tone(592, 0.11, 'sine', 0.024, 0.11);
            return;
        }
        tone(92, 0.16, 'square', 0.06);
        tone(138, 0.22, 'triangle', 0.045, 0.07);
        noise(0.08, 0.025);
    }

    // The widening interval follows the reel easing, giving the final few item changes more weight.
    function startReelSounds(duration) {
        clearReelSounds();
        let elapsed = 90;
        let interval = 72;
        while (elapsed < duration - 180) {
            const progress = elapsed / duration;
            const pitch = soundState.theme === 'csgo'
                ? Math.max(110, 340 - (progress * 190))
                : soundState.theme === 'cs2'
                    ? Math.max(210, 560 - (progress * 255))
                    : Math.max(155, 430 - (progress * 245));
            const waveform = soundState.theme === 'csgo' ? 'sawtooth' : soundState.theme === 'cs2' ? 'triangle' : 'square';
            reelSoundTimers.push(window.setTimeout(() => tone(pitch, 0.035, waveform, 0.028), elapsed));
            elapsed += interval;
            interval = Math.min(390, interval * 1.052);
        }
    }

    function playReveal(item) {
        clearReelSounds();
        const key = String(item.rarityKey || '').toLowerCase();
        const classicReveal = {
            'restricted': [330, 440],
            'classified': [392, 523, 659],
            'covert': [220, 440, 660],
            'rare-special': [196, 392, 587, 784],
            'remarkable': [349, 466],
            'exotic': [392, 523, 659]
        }[key] || [294, 392];
        const reveal = soundState.theme === 'csgo'
            ? classicReveal.map(frequency => Math.round(frequency * .74))
            : soundState.theme === 'cs2'
                ? classicReveal.map(frequency => Math.round(frequency * 1.18))
                : classicReveal;
        const waveform = soundState.theme === 'csgo' ? 'square' : soundState.theme === 'cs2' ? 'sine' : 'triangle';
        reveal.forEach((frequency, index) => tone(frequency, 0.28 + (index * 0.055), waveform, 0.065, index * 0.045));
        if (key === 'covert' || key === 'rare-special') noise(0.24, key === 'rare-special' ? 0.07 : 0.045);
    }

    // A short rising fanfare - distinct in shape from playReveal's rarity chimes - timed to land
    // just as the level-up toast pops in.
    function playLevelUp() {
        ensureAudioContext();
        [523, 659, 784, 1047].forEach((frequency, index) => tone(frequency, 0.32, 'triangle', 0.07, index * 0.08));
        tone(1568, 0.4, 'sine', 0.05, 0.32);
        noise(0.18, 0.03);
    }

    function renderSoundControls() {
        const percentage = Math.round(soundState.volume * 100);
        const audible = soundState.enabled && percentage > 0;
        $('#caseSoundEnabled')
            .attr('aria-pressed', String(soundState.enabled))
            .attr('aria-label', soundState.enabled ? 'Mute case opening sounds' : 'Enable case opening sounds')
            .find('i').attr('class', soundState.enabled ? 'fa-solid fa-volume-high' : 'fa-solid fa-volume-xmark').end()
            .find('span').text(soundState.enabled ? 'On' : 'Off');
        $('#caseSoundVolumeValue').text(`${percentage}%`);
        $('#caseSoundStatus').text(audible ? `${soundState.theme === 'classic' ? 'Classic' : soundState.theme === 'csgo' ? 'CS:GO-style' : 'CS2-style'} audio enabled` : 'Audio muted');
        $('#caseSoundButtonText').text(audible ? 'Sound' : 'Muted');
        $('#caseSoundButtonIcon')
            .toggleClass('fa-volume-high', audible)
            .toggleClass('fa-volume-xmark', !audible);
        $('.case-sound-theme').each(function () {
            const selected = $(this).data('sound-theme') === soundState.theme;
            $(this).toggleClass('is-selected', selected).attr('aria-checked', String(selected));
        });
        const meter = document.getElementById('caseSoundVolumeMeter');
        if (meter) {
            meter.setAttribute('aria-valuenow', String(percentage));
            meter.setAttribute('aria-valuetext', `${percentage}% volume`);
            const fill = meter.querySelector('span');
            if (!fill) return;
            const targetWidth = `${percentage}%`;
            if (window.anime?.animate && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                window.anime.animate(fill, { width: [fill.style.width || '0%', targetWidth], duration: 260, ease: 'out(3)' });
            } else {
                fill.style.width = targetWidth;
            }
        }
    }

    function request(url, method, options) {
        return $.ajax(Object.assign({
            url: url,
            method: method || 'GET',
            showToast: false,
            headers: { RequestVerificationToken: $('input[name="__RequestVerificationToken"]').first().val() }
        }, options || {}));
    }

    function requestWasAborted(response) {
        return response?.statusText === 'abort';
    }

    // Winner artwork is the only image an opening must have immediately. The roulette's other
    // images stay lazy and low priority, avoiding a burst of unnecessary transfers on mobile.
    function warmCaseImage(url) {
        const source = String(url || '');
        if (!source || warmedCaseImages.has(source)) return;
        warmedCaseImages.add(source);
        const image = new Image();
        image.decoding = 'async';
        image.fetchPriority = 'high';
        image.src = source;
        if (typeof image.decode === 'function') image.decode().catch(() => { });
    }

    function prepareProgressiveCaseImage(image) {
        if (!(image instanceof HTMLImageElement) || image.dataset.caseImagePrepared === 'true') return;
        image.dataset.caseImagePrepared = 'true';
        image.decoding = 'async';
        if (image.complete && image.naturalWidth > 0) image.classList.add('is-image-loaded');
        else image.classList.add('case-image-pending');
    }

    document.addEventListener('load', function (event) {
        const image = event.target;
        if (!(image instanceof HTMLImageElement) || !image.closest('.case-opening-page')) return;
        image.classList.remove('case-image-pending', 'is-image-error');
        image.classList.add('is-image-loaded');
    }, true);
    document.addEventListener('error', function (event) {
        const image = event.target;
        if (!(image instanceof HTMLImageElement) || !image.closest('.case-opening-page')) return;
        image.classList.remove('case-image-pending');
        image.classList.add('is-image-error');
    }, true);
    document.querySelectorAll('.case-opening-page img').forEach(prepareProgressiveCaseImage);
    new MutationObserver(function (mutations) {
        mutations.forEach(mutation => mutation.addedNodes.forEach(node => {
            if (!(node instanceof Element)) return;
            if (node.matches('img')) prepareProgressiveCaseImage(node);
            node.querySelectorAll?.('img').forEach(prepareProgressiveCaseImage);
        }));
    }).observe($page.get(0), { childList: true, subtree: true });

    function loadBotCasePreference() {
        try {
            return localStorage.getItem(botCaseStorageKey) || '';
        } catch {
            return '';
        }
    }

    function saveBotCasePreference(selectedCaseKey) {
        try {
            localStorage.setItem(botCaseStorageKey, selectedCaseKey);
        } catch {
            // The bot assignment is a convenience preference and does not need to block use.
        }
    }

    function availableBotCases() {
        return catalogue.filter(item => isCaseUnlocked(item));
    }

    function renderBotProgress(progress) {
        const hadRenderedRacks = $('#caseBotServers .case-bot-server').length > 0;
        const expandedRackIds = new Set($('#caseBotServers .case-bot-server .collapse.show').map(function () {
            return String(this.id || '');
        }).get());
        botProgress = progress || null;
        const servers = botProgress?.servers || [];
        const bots = servers.flatMap(server => server.bots || []);
        const activeBots = servers.filter(server => server.isEnabled !== false).flatMap(server => server.bots || []);
        const stars = Number(botProgress?.activeBalanceMinor ?? activeBalance());
        const serverCost = Number(botProgress?.activeNextServerCost ?? botProgress?.nextServerCost ?? 0);
        const botCost = Number(botProgress?.activeNextBotCost ?? botProgress?.nextBotCost ?? 0);
        const capacity = Number(botProgress?.serverCapacity || 4);
        $('#caseUpgradeServerOwnership').text(`${servers.length} server${servers.length === 1 ? '' : 's'}`);
        $('#caseUpgradeBotOwnership').text(`${bots.length}/${servers.length * capacity} bots`);
        const $select = $('#caseBotCaseSelect');
        const priorSelection = String($select.val() || loadBotCasePreference() || caseKey);
        const unlockedCases = availableBotCases();

        $select.empty();
        unlockedCases.forEach(item => $select.append($('<option>', { value: item.caseKey, text: item.name })));
        const selectedCaseKey = unlockedCases.some(item => item.caseKey === priorSelection)
            ? priorSelection
            : unlockedCases[0]?.caseKey || '';
        $select.val(selectedCaseKey).prop('disabled', unlockedCases.length === 0);
        saveBotCasePreference(selectedCaseKey);

        $('#buyCaseBotServer')
            .prop('disabled', stars < serverCost)
            .text(`Buy server · ${formatCurrency(serverCost, true)}`);
        $('.js-shop-buy-server').prop('disabled', stars < serverCost).text(`Buy server · ${formatCurrency(serverCost, true)}`);
        $('#buyCaseBot')
            .prop('disabled', servers.length === 0 || bots.length >= servers.length * capacity || stars < botCost)
            .text(`Buy bot · ${formatCurrency(botCost, true)}`);
        $('.js-shop-buy-bot')
            .prop('disabled', servers.length === 0 || bots.length >= servers.length * capacity || stars < botCost)
            .text(`Buy bot · ${formatCurrency(botCost, true)}`);
        $('#startCaseBots').prop('disabled', activeBots.length === 0 || !selectedCaseKey || botsRunning);
        $('#stopCaseBots').prop('disabled', !botsRunning);
        const $status = $('#caseBotStatus')
            .toggleClass('text-bg-success', botsRunning)
            .toggleClass('text-bg-secondary-subtle', !botsRunning)
            .empty();
        $status.append($('<i>', {
            class: `${botsRunning ? 'fa-solid fa-satellite-dish' : 'fa-solid fa-robot'} me-1`,
            'aria-hidden': 'true'
        }), document.createTextNode(botsRunning
            ? `${activeBots.length} bot${activeBots.length === 1 ? '' : 's'} active`
            : bots.length === 0 ? 'No bots installed' : 'Ready'));

        renderPlayerProfile();

        const $servers = $('#caseBotServers').empty();
        servers.forEach((server, index) => {
            const serverBots = server.bots || [];
            const $slots = $('<div>', { class: 'case-bot-rack-grid' });
            for (let slot = 0; slot < capacity; slot += 1) {
                const bot = serverBots[slot];
                if (!bot) {
                    $slots.append($('<div>', { class: 'case-bot-unit is-empty', title: 'Available bot slot' }).append(
                        $('<i>', { class: 'fa-solid fa-plus', 'aria-hidden': 'true' }), $('<span>', { text: 'Empty slot' })));
                    continue;
                }
                const level = Number(bot.speedLevel || 0);
                const pips = $('<span>', { class: 'case-bot-level-pips', 'aria-label': `Level ${level} of 5` });
                for (let pip = 1; pip <= 5; pip += 1) pips.append($('<i>', { class: pip <= level ? 'is-filled' : '' }));
                $slots.append($('<button>', {
                    class: `case-bot-unit is-installed js-upgrade-bot-speed${botsRunning && server.isEnabled !== false ? ' is-working' : ''}${bot.maximumSpeedReached ? ' is-maxed' : ''}`,
                    type: 'button', 'data-bot-id': bot.botId,
                    disabled: bot.maximumSpeedReached || stars < Number(bot.activeNextSpeedUpgradeCost ?? bot.nextSpeedUpgradeCost ?? 0),
                    title: bot.maximumSpeedReached ? `Bot ${slot + 1} is at maximum speed` : `Upgrade Bot ${slot + 1}`
                }).append(
                    $('<span>', { class: 'case-bot-unit-icon' }).append($('<i>', { class: bot.maximumSpeedReached ? 'fa-solid fa-robot' : 'fa-solid fa-microchip', 'aria-hidden': 'true' })),
                    $('<span>', { class: 'case-bot-unit-copy' }).append(
                        $('<strong>', { text: `Bot ${slot + 1}` }),
                        $('<small>', { text: `Level ${level}/5 · ${Number(bot.speedMultiplier || .5).toFixed(1)}×` }), pips),
                    $('<span>', { class: 'case-bot-unit-cost', text: bot.maximumSpeedReached ? 'MAX' : formatCurrency(Number(bot.activeNextSpeedUpgradeCost ?? bot.nextSpeedUpgradeCost ?? 0), true) })
                ));
            }
            const rackId = `caseBotRack-${server.serverId}`;
            const rackExpanded = expandedRackIds.has(rackId) || (!hadRenderedRacks && index === 0);
            $servers.append($('<article>', { class: `case-bot-server${server.isEnabled === false ? ' is-offline' : ''}` }).append(
                $('<div>', { class: 'case-bot-rack-head' }).append(
                    $('<button>', { class: 'case-bot-rack-summary', type: 'button', 'data-bs-toggle': 'collapse', 'data-bs-target': `#${rackId}`, 'aria-expanded': rackExpanded ? 'true' : 'false', 'aria-controls': rackId }).append(
                        $('<span>', { class: 'case-bot-rack-icon' }).append($('<i>', { class: 'fa-solid fa-server', 'aria-hidden': 'true' })),
                        $('<span>').append($('<strong>', { text: `Rack ${index + 1}` }), $('<small>', { text: `${serverBots.length}/${capacity} bots · ${Number(server.speedLevel || 0)}/20 levels · ${server.isEnabled === false ? 'Offline' : 'Online'}` })),
                        $('<i>', { class: 'fa-solid fa-chevron-down case-bot-rack-chevron', 'aria-hidden': 'true' })),
                    $('<label>', { class: 'form-check form-switch case-bot-rack-switch mb-0', title: 'Toggle this rack' }).append(
                        $('<input>', { class: 'form-check-input pt-switch js-toggle-bot-server', type: 'checkbox', role: 'switch', 'data-server-id': server.serverId, checked: server.isEnabled !== false, 'aria-label': `Toggle Rack ${index + 1}` }))
                ),
                $('<div>', { class: `collapse${rackExpanded ? ' show' : ''}`, id: rackId }).append($slots)
            ));
        });
    }

    function loadBotProgress(options) {
        botProgressLoaded = true;
        return request('/api/case-opening/bots', 'GET', Object.assign({ showLoader: false }, options || {}))
            .done(function (progress) {
                renderBotProgress(progress);
            })
            .fail(function (response) {
                botProgressLoaded = false;
                showError(response, 'Bot workshop status could not be loaded.');
            });
    }

    const botFeedMaximumItems = 24;

    function queueBotResult(result) {
        announceNewCollectionDrops([result]);
        addResultsToInventory([result], false);
        const winner = result.winner;
        $('#caseBotFeed').removeClass('d-none').prepend(
            $('<div>', { class: `case-bot-feed-item ${rarityClass(winner)}` }).append(
                $('<img>', { src: winner.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                $('<span>').append(
                    $('<small>', { text: `${result.caseName} bot drop` }),
                    $('<strong>', { text: winner.name })
                )
            )
        ).children().slice(botFeedMaximumItems).remove();

        const catalogueCase = catalogue.find(item => item.caseKey === result.caseKey);
        if (catalogueCase) catalogueCase.ownedQuantity = Math.max(0, Number(catalogueCase.ownedQuantity || 0) - 1);
        if (result.caseKey === caseKey && caseData) {
            const previousQuantity = Number(caseData.ownedQuantity || 0);
            caseData.ownedQuantity = catalogueCase
                ? Number(catalogueCase.ownedQuantity || 0)
                : Math.max(0, previousQuantity - 1);
            renderOwnedCaseQuantity({ from: previousQuantity, animate: true });
            renderOpenQuantity();
        }
        renderOwnedCasesInventory();

        window.clearTimeout(botRefreshTimer);
        const refreshBotBackgroundData = function () {
            // A bot result must never reset or compete with the user's reel. Wait until their
            // opening has completed before refreshing the supporting collection and totals.
            if (opening) {
                botRefreshTimer = window.setTimeout(refreshBotBackgroundData, 650);
                return;
            }

            renderSessionSummary();
            if (activeDestination === 'inventory' && historyDirty) renderHistory(allHistoryItems);
            profileCollectionsLoaded = false;
            loadStatistics(result.caseKey);
            loadInventoryCapacity();
            loadCaseCatalogue();
        };
        botRefreshTimer = window.setTimeout(refreshBotBackgroundData, 800);

    }

    function runBotCycle() {
        if (!botsRunning || document.hidden || botCycleInFlight) return;
        const selectedCaseKey = String($('#caseBotCaseSelect').val() || '');
        if (!selectedCaseKey) return;
        const stock = Number(catalogue.find(item => item.caseKey === selectedCaseKey)?.ownedQuantity || 0);
        if (stock < 1) {
            stopBots(false);
            if (botStopNotice !== `stock:${selectedCaseKey}`) {
                botStopNotice = `stock:${selectedCaseKey}`;
                window.personalToolsToast?.info('Bots paused—the assigned case is out of stock.');
            }
            return;
        }

        botCycleInFlight = true;
        request('/api/case-opening/bots/cycle', 'POST', {
            data: JSON.stringify({ caseKey: selectedCaseKey }),
            contentType: 'application/json; charset=utf-8', showLoader: false
        }).done(function (cycle) {
            botCycleFailureCount = 0;
            (cycle.results || []).forEach(function (result) {
                queueBotResult(result);
                awardXp([result], $('#caseBotFeed'), true);
            });
            if ((cycle.results || []).length) {
                botStopNotice = '';
                evaluateAutoBuy();
                evaluateTradeUpRecipes();
            }
            if (cycle.shouldStop) {
                stopBots(false);
                const noticeKey = `${cycle.stopReason || 'stopped'}:${selectedCaseKey}`;
                if (botStopNotice !== noticeKey) {
                    botStopNotice = noticeKey;
                    window.personalToolsToast?.info(cycle.message || 'Bot operation paused.');
                }
            }
        }).fail(function (response) {
            botCycleFailureCount += 1;
            if (botCycleFailureCount <= 2) {
                window.personalToolsToast?.error(response.responseJSON?.message || 'The bot rack could not complete this cycle.');
            }
            if (botCycleFailureCount >= 2) stopBots(false);
        }).always(() => { botCycleInFlight = false; });
    }

    function stopBots(showToast) {
        botsRunning = false;
        window.clearInterval(botTimer);
        botTimer = null;
        renderBotProgress(botProgress);
        if (showToast) window.personalToolsToast?.info('Bot operation stopped.');
    }

    function startBots(showToast) {
        const bots = (botProgress?.servers || []).filter(server => server.isEnabled !== false).flatMap(server => server.bots || []);
        if (botsRunning || bots.length === 0 || !$('#caseBotCaseSelect').val()) return;
        botCycleFailureCount = 0;
        botStopNotice = '';
        botsRunning = true;
        renderBotProgress(botProgress);
        runBotCycle();
        window.clearInterval(botTimer);
        const fastestInterval = Math.min(...(botProgress?.servers || []).map(server => Number(server.openingIntervalSeconds || 12)), Number(botProgress?.openingIntervalSeconds || 12));
        botTimer = window.setInterval(runBotCycle, fastestInterval * 1000);
        if (showToast) window.personalToolsToast?.success('Bot operation started. Keep this tab visible to continue opening cases.');
    }

    // Bots keep "running" conceptually while the tab is hidden - only the interval that actually
    // ticks openings is paused/resumed, so switching back to this tab picks up right where it
    // left off instead of requiring another manual Start click.
    function pauseBotsForHiddenTab() {
        if (!botTimer) return;
        window.clearInterval(botTimer);
        botTimer = null;
    }

    function resumeBotsIfDue() {
        if (!botsRunning || botTimer || document.hidden) return;
        runBotCycle();
        const fastestInterval = Math.min(...(botProgress?.servers || []).map(server => Number(server.openingIntervalSeconds || 12)), Number(botProgress?.openingIntervalSeconds || 12));
        botTimer = window.setInterval(runBotCycle, fastestInterval * 1000);
        window.personalToolsToast?.info('Bot operation resumed.');
    }

    function rarityClass(item) {
        const key = String(item?.rarityKey || 'mil-spec').toLowerCase();
        return ['mil-spec', 'restricted', 'classified', 'covert', 'rare-special', 'high-grade', 'remarkable', 'exotic'].includes(key)
            ? `case-rarity-${key}`
            : 'case-rarity-mil-spec';
    }

    function isGoldItem(item) {
        return item?.isRareSpecial === true || String(item?.rarityKey || '').toLowerCase() === 'rare-special';
    }

    function rarityRank(item) {
        return {
            'rare-special': 8,
            'covert': 7,
            'exotic': 6,
            'classified': 5,
            'remarkable': 4,
            'restricted': 3,
            'high-grade': 2,
            'mil-spec': 2
        }[String(item?.rarityKey || '').toLowerCase()] || 1;
    }

    function rarityDisplayOrder(rarityKey) {
        return {
            'mil-spec': 1,
            'high-grade': 1,
            restricted: 2,
            remarkable: 2,
            classified: 3,
            exotic: 3,
            covert: 4,
            'rare-special': 5
        }[String(rarityKey || '').toLowerCase()] || 99;
    }

    function sessionDurationText() {
        const elapsedSeconds = Math.max(0, Math.floor((Date.now() - sessionStartedAt) / 1000));
        const hours = Math.floor(elapsedSeconds / 3600);
        const minutes = Math.floor((elapsedSeconds % 3600) / 60);
        const seconds = elapsedSeconds % 60;
        return hours > 0
            ? `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
            : `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }

    function renderSessionDuration() {
        $('#caseSessionDuration').text(sessionDurationText());
    }

    function renderSessionDistribution() {
        const counts = new Map();
        sessionOpenings.forEach(item => {
            const key = String(item.rarityKey || 'mil-spec');
            const existing = counts.get(key) || { key: key, name: item.rarityName || 'Mil-Spec', count: 0, item: item };
            existing.count += 1;
            counts.set(key, existing);
        });

        const groups = Array.from(counts.values()).sort((left, right) => rarityRank(right.item) - rarityRank(left.item));
        const $bar = $('#caseSessionDistributionBar').empty();
        const $legend = $('#caseSessionLegend').empty();
        groups.forEach(group => {
            const percentage = (group.count / sessionOpenings.length) * 100;
            $bar.append($('<span>', {
                class: `case-session-segment ${rarityClass(group.item)}`,
                title: `${group.name}: ${group.count} (${percentage.toFixed(1)}%)`
            }).css('--session-share', group.count));
            $legend.append($('<span>', { class: rarityClass(group.item) }).append(
                $('<i>', { 'aria-hidden': 'true' }),
                $('<span>', { text: group.name }),
                $('<strong>', { text: group.count })
            ));
        });

        const total = sessionOpenings.length;
        $('#caseSessionDistributionSummary').text(total === 0 ? 'Waiting for the first opening' : `${groups.length} rarit${groups.length === 1 ? 'y' : 'ies'} across ${total} result${total === 1 ? '' : 's'}`);
        $bar.attr('aria-label', total === 0
            ? 'No items opened in this session'
            : groups.map(group => `${group.name}: ${group.count}`).join(', '));
    }

    function renderSessionSummary() {
        const total = sessionOpenings.length;
        const rareSpecials = sessionOpenings.filter(item => item.isRareSpecial).length;
        const statTrak = sessionOpenings.filter(item => item.isStatTrak).length;
        $('#caseSessionOpened').text(total);
        $('#caseSessionRares').text(rareSpecials);
        $('#caseSessionStatTrak').text(statTrak);
        $('#resetCaseSession').prop('disabled', total === 0);
        renderSessionDistribution();

        const best = sessionOpenings.reduce((current, item) => {
            return !current || rarityRank(item) > rarityRank(current) ? item : current;
        }, null);
        $('#caseSessionBestEmpty').toggleClass('d-none', Boolean(best));
        $('#caseSessionBestResult').toggleClass('d-none', !best);
        if (best) {
            $('#caseSessionBest')
                .removeClass('case-rarity-mil-spec case-rarity-restricted case-rarity-classified case-rarity-covert case-rarity-rare-special case-rarity-high-grade case-rarity-remarkable case-rarity-exotic')
                .addClass(rarityClass(best));
            $('#caseSessionBestImage').attr({ src: best.imageUrl, alt: best.name });
            $('#caseSessionBestRarity').text([best.rarityName, best.isStatTrak ? 'StatTrak™' : ''].filter(Boolean).join(' · '));
            $('#caseSessionBestName').text(best.name);
            $('#caseSessionBestCase').text(caseNameFor(best.caseKey));
        } else {
            $('#caseSessionBest').removeClass('case-rarity-mil-spec case-rarity-restricted case-rarity-classified case-rarity-covert case-rarity-rare-special case-rarity-high-grade case-rarity-remarkable case-rarity-exotic');
        }

        window.personalToolsMotion?.reveal(
            $('.case-session-stat, #caseSessionBest').get(),
            { fromY: 5, delay: 30, duration: 230 }
        );
        renderRecentDrops();
        renderPlayerProfile();
    }

    function renderRecentDrops() {
        const recentItems = sessionOpenings.slice(-50).reverse();
        const $rail = $('#caseRecentDropsRail').empty();
        $('#caseRecentDropsCount').text(recentItems.length === 0
            ? 'This session'
            : `${sessionOpenings.length.toLocaleString()} this session`);

        if (recentItems.length === 0) {
            $rail.append($('<div>', { class: 'case-recent-drops-empty', id: 'caseRecentDropsEmpty' }).append(
                $('<i>', { class: 'fa-solid fa-box-open', 'aria-hidden': 'true' }),
                $('<span>', { text: 'Your latest pulls will appear here.' })
            ));
            return;
        }

        recentItems.forEach(function (item) {
            $rail.append($('<article>', {
                class: `case-recent-drop ${rarityClass(item)}`,
                title: `${item.name} · ${item.rarityName || 'Item'}`
            }).append(
                $('<img>', { src: item.imageUrl, alt: '', loading: 'lazy', decoding: 'async', referrerpolicy: 'no-referrer' }),
                $('<div>', { class: 'case-recent-drop-copy' }).append(
                    $('<small>', { text: item.rarityName || 'Item' }),
                    $('<strong>', { text: item.name }),
                    $('<span>', { text: [item.wear, caseNameFor(item.caseKey)].filter(Boolean).join(' · ') })
                ),
                statTrakBadge(item)
            ));
        });

        window.personalToolsMotion?.reveal($rail.children().get(), { fromX: 8, delay: 24, duration: 220 });
    }

    function showError(response, fallback) {
        const message = response?.responseJSON?.message || fallback;
        window.personalToolsToast?.error(message);
    }

    // Routine openings stay quiet. A collection-first result is meaningful progression, so it
    // receives one toast and is marked locally before another result in the same batch is checked.
    function announceNewCollectionDrops(results) {
        results.forEach(function (result) {
            if (result.isNewCollectionItem !== true) return;
            profileCollectionsLoaded = false;
            const autoSoldCopy = result.isAutoSold === true
                ? ` It was auto-sold for ${formatCurrency(Number(result.autoSoldAmountMinor ?? result.autoSoldStars ?? 0))}.`
                : '';
            window.personalToolsToast?.success(`New collection item: ${result.winner.name}.${autoSoldCopy}`);
        });
    }

    function itemCard(item, className) {
        const gold = isGoldItem(item);
        return $('<article>', { class: `${className} ${rarityClass(item)}` }).append(
            $('<img>', {
                src: gold ? caseData?.imageUrl : item.imageUrl,
                alt: '',
                loading: 'lazy',
                decoding: 'async',
                fetchpriority: 'low',
                referrerpolicy: 'no-referrer'
            }),
            $('<span>', { text: gold ? '★ Rare Special Item ★' : item.name }),
            statTrakBadge(item),
            specialVariantBadge(item)
        ).toggleClass('case-reel-gold-placeholder', gold);
    }

    // The reference data supplies one asset for the weapon skin. StatTrak is a variant of that
    // same skin rather than a second image, so the game-like marker makes the difference explicit.
    function statTrakBadge(item) {
        if (item?.isStatTrak !== true) return null;

        return $('<span>', { class: 'case-stattrak-badge' }).append(
            $('<i>', { class: 'fa-solid fa-crosshairs', 'aria-hidden': 'true' }),
            document.createTextNode(' StatTrak™')
        );
    }

    // A short gold confirmation belongs to the control that was saved. This gives switches the
    // same feedback as the page's buttons without leaving a permanent success state behind.
    function markSwitchSaved(input) {
        const $input = $(input);
        if (!$input.length) return;
        $input.removeClass('is-save-confirmed');
        window.requestAnimationFrame(function () {
            $input.addClass('is-save-confirmed');
            window.setTimeout(() => $input.removeClass('is-save-confirmed'), 950);
        });
    }

    function renderOpenButton(state) {
        const ownedQuantity = Number(caseData?.ownedQuantity || 0);
        const openingText = ownedQuantity < selectedOpenQuantity
            ? 'No cases owned'
            : selectedOpenQuantity === 1 ? 'Open case' : `Open ${selectedOpenQuantity} cases`;
        const settings = {
            ready: { icon: 'fa-solid fa-box-open me-2', text: openingText },
            requesting: { icon: 'spinner-border spinner-border-sm me-2', text: 'Unlocking…' },
            rolling: { icon: 'fa-solid fa-arrows-left-right me-2', text: selectedOpenQuantity === 1 ? 'Opening…' : `Opening ${selectedOpenQuantity} cases…` }
        }[state] || { icon: 'fa-solid fa-box-open me-2', text: openingText };
        $open.find('.case-open-button-icon').attr('class', `${settings.icon} case-open-button-icon`).removeClass('me-2');
        $open.find('.case-open-button-label').text(settings.text);
        if (state === 'ready') {
            $open.prop('disabled', ownedQuantity < selectedOpenQuantity);
        }
        $('.case-machine').toggleClass('is-requesting', state === 'requesting');
        $page.toggleClass('is-opening', state !== 'ready');
        if (state !== 'ready') $('[data-open-quantity]').prop('disabled', true);
    }

    function oddsMarkup(odds) {
        const $list = $('<div>', { class: 'case-odds-list' });
        odds.forEach(odd => {
            $list.append($('<div>', { class: 'case-odds-row' }).append(
                $('<span>', { class: `case-odds-dot ${rarityClass(odd)}` }),
                $('<span>', { text: odd.rarityName }),
                $('<strong>', { text: `${Number(odd.percentage).toFixed(2)}%` })
            ));
        });
        return $list.prop('outerHTML');
    }

    function configureCase(data) {
        window.clearTimeout(stockStateTimer);
        stockStateTimer = null;
        caseData = data;
        $('.case-machine').removeClass('is-multi-results');
        $('#caseReelWindow').removeClass('has-multi-results has-scrollable-multi');
        $('#caseMultiResults').addClass('d-none').empty().removeAttr('data-open-count');
        $('#caseName').text(data.name);
        $('#caseType').text(data.type);
        renderOwnedCaseQuantity();
        $('#caseImage').attr('src', data.imageUrl);
        const button = document.getElementById('caseOddsButton');
        bootstrap.Popover.getInstance(button)?.dispose();
        bootstrap.Popover.getOrCreateInstance(button, {
            html: true,
            sanitize: true,
            content: oddsMarkup(data.odds),
            customClass: 'case-odds-popover'
        });
        $idle.removeClass('d-none');
        $reel.removeClass('case-skip-reel case-multi-reel').empty().css('transform', 'translateX(0px)');
        $result.addClass('d-none');
        $('#caseSelectorGrid input').prop('checked', false)
            .filter(`[value="${caseKey}"]`).prop('checked', true);
        renderCaseSelector();
        $open.prop('disabled', Number(data.ownedQuantity || 0) < 1);
        renderOpenQuantity();
        renderCaseStockState();
    }

    function profileCollectionCard(collection) {
        const total = Number(collection.totalItemCount || 0);
        const collected = Number(collection.collectedItemCount || 0);
        const percentage = total > 0 ? Math.round((collected / total) * 100) : 0;
        const completed = total > 0 && collected >= total;
        const firstObtained = collection.firstObtainedUtc
            ? new Date(collection.firstObtainedUtc).toLocaleDateString()
            : 'Unknown';
        const $rarities = $('<div>', { class: 'case-profile-collection-rarities' });

        (collection.rarities || []).forEach(function (rarity) {
            $rarities.append($('<span>', { class: `case-collection-rarity-chip ${rarityClass(rarity)}` }).append(
                $('<i>', { 'aria-hidden': 'true' }),
                document.createTextNode(`${rarity.rarityName}: ${Number(rarity.collectedItemCount || 0)}/${Number(rarity.totalItemCount || 0)}`)
            ));
        });

        return $('<article>', { class: `case-profile-collection-card${completed ? ' is-complete' : ''}` }).append(
            $('<div>', { class: 'case-profile-collection-art' }).append(
                $('<img>', { src: collection.imageUrl, alt: '', loading: 'lazy', decoding: 'async', referrerpolicy: 'no-referrer' }),
                completed ? $('<span>', { class: 'case-profile-collection-complete', text: 'Complete' }) : null
            ),
            $('<div>', { class: 'case-profile-collection-body' }).append(
                $('<div>', { class: 'case-profile-collection-heading' }).append(
                    $('<div>').append(
                        $('<small>', { text: `Started ${firstObtained}` }),
                        $('<strong>', { text: collection.caseName })
                    ),
                    $('<span>', { text: `${collected} / ${total}` })
                ),
                $('<div>', {
                    class: 'case-profile-collection-progress',
                    role: 'progressbar',
                    'aria-label': `${collection.caseName} collection progress`,
                    'aria-valuemin': '0',
                    'aria-valuemax': '100',
                    'aria-valuenow': String(percentage)
                }).append($('<span>').css('width', `${percentage}%`)),
                $('<div>', { class: 'case-profile-collection-meta' }).append(
                    $('<span>', { text: `${percentage}% complete` }),
                    $('<span>', { text: `${Math.max(0, total - collected)} remaining` })
                ),
                $rarities
            )
        );
    }

    function specialVariantBadge(item) {
        if (!item?.specialVariantRuleId) return null;
        const label = [item.specialVariantName, item.specialVariantTier].filter(Boolean).join(' · ') || 'Special variant';
        return $('<span>', { class: 'case-special-variant-badge', title: item.specialVariantDescription || label }).append(
            $('<i>', { class: 'fa-solid fa-gem', 'aria-hidden': 'true' }),
            document.createTextNode(` ${label}`)
        );
    }

    function renderProfileCollections() {
        const $grid = $('#caseProfileCollectionsGrid').empty();
        const completed = profileCollections.filter(collection => Number(collection.totalItemCount || 0) > 0
            && Number(collection.collectedItemCount || 0) >= Number(collection.totalItemCount || 0)).length;
        $('#caseProfileCollectionSummary').text(`${profileCollections.length.toLocaleString()} started · ${completed.toLocaleString()} completed`);

        if (profileCollections.length === 0) {
            $grid.append($('<div>', { class: 'case-profile-collections-empty' }).append(
                $('<i>', { class: 'fa-solid fa-layer-group', 'aria-hidden': 'true' }),
                $('<span>', { text: 'Open a case to begin its permanent collection.' })
            ));
            return;
        }

        profileCollections.forEach(collection => $grid.append(profileCollectionCard(collection)));
        window.personalToolsMotion?.reveal($grid.children().get(), { fromY: 7, delay: 28, duration: 260 });
    }

    function loadProfileCollections(options) {
        const settings = options || {};
        if (profileCollectionsLoading) return $.Deferred().resolve().promise();
        if (profileCollectionsLoaded && settings.force !== true) {
            renderProfileCollections();
            return $.Deferred().resolve(profileCollections).promise();
        }

        profileCollectionsLoading = true;
        $('#caseProfileCollectionsGrid').empty().append(
            $('<div>', { class: 'case-profile-collections-loading', role: 'status', 'aria-label': 'Loading collections' })
                .append($('<span>'), $('<span>'), $('<span>')));
        return request('/api/case-opening/collections', 'GET', { showLoader: false })
            .done(function (collections) {
                profileCollections = Array.isArray(collections) ? collections : [];
                profileCollectionsLoaded = true;
                renderProfileCollections();
            })
            .fail(function (response) {
                const message = response.responseJSON?.message || 'Your collection record could not be loaded.';
                $('#caseProfileCollectionsGrid').html('').append($('<div>', { class: 'case-profile-collections-empty is-error' }).append(
                    $('<i>', { class: 'fa-solid fa-triangle-exclamation', 'aria-hidden': 'true' }),
                    $('<span>', { text: message })
                ));
            })
            .always(function () {
                profileCollectionsLoading = false;
            });
    }

    function loadCase(selectedKey, options) {
        const settings = options || {};
        $open.prop('disabled', true);
        loadedCaseKey = selectedKey;
        return request(`/api/case-opening/cases/${encodeURIComponent(selectedKey)}`, 'GET', {
            showLoader: settings.showLoader !== false
        })
            .done(function (data) {
                configureCase(data);
                loadStatistics();
                if (settings.closeSelector) {
                    bootstrap.Modal.getInstance(document.getElementById('caseSelectorModal'))?.hide();
                }
                if (settings.showToast) {
                    window.personalToolsToast?.success(`${data.name} selected.`);
                }
            })
            .fail(function (response) {
                if (loadedCaseKey === selectedKey) loadedCaseKey = '';
                showError(response, 'That case could not be loaded.');
            });
    }

    function animateNumber($element, value, suffix) {
        const target = Number(value) || 0;
        const ending = suffix || '';
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches || target === 0) {
            $element.text(`${target}${ending}`);
            return;
        }

        const started = performance.now();
        const duration = 340;
        function frame(now) {
            const progress = Math.min(1, (now - started) / duration);
            const eased = 1 - Math.pow(1 - progress, 3);
            $element.text(`${Math.round(target * eased)}${ending}`);
            if (progress < 1) requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }

    function renderStatistics(statistics) {
        currentStatistics = statistics;
        $('#caseLuckSubtitle').text(`Statistics for ${statistics.caseName}.`);
        $('#caseLuckTotalLabel').text(statistics.caseName);
        $('#caseLuckTargetLabel').text(`${statistics.targetRarityName} pulls`);
        $('#caseLuckOdds').text(`${Number(statistics.targetOddsPercentage).toFixed(2)}% each opening`);
        $('#caseLuckExpected').text(`Statistical average: roughly 1 in ${statistics.expectedOpeningInterval}`);
        animateNumber($('#caseLuckTotal'), statistics.totalOpenings);
        animateNumber($('#caseLuckTargets'), statistics.targetPulls);
        animateNumber($('#caseLuckDryStreak'), statistics.currentDryStreak);
        $('#caseLuckProbability').text(`${Number(statistics.noTargetStreakProbability).toFixed(2)}%`);
        renderPlayerProfile();
        window.personalToolsMotion?.reveal($('.case-luck-card').get(), { fromY: 7, delay: 35, duration: 280 });

        // Only an opening can trigger the joke. Loading or switching cases must never replay it.
        if (statisticsRequestedAfterOpening === statistics.caseKey) {
            const expected = Math.max(1, Number(statistics.expectedOpeningInterval) || 1);
            const dryStreak = Number(statistics.currentDryStreak) || 0;
            if (dryStreak >= expected && !announcedDryStreaks.has(statistics.caseKey)) {
                window.personalToolsKillfeed?.headshot(
                    'Gaben',
                    document.body.dataset.displayName || 'You',
                    'gaben'
                );
                announcedDryStreaks.add(statistics.caseKey);
            } else if (dryStreak < expected) {
                // A top-tier pull starts a new streak, so this case can earn the joke again later.
                announcedDryStreaks.delete(statistics.caseKey);
            }
            statisticsRequestedAfterOpening = null;
        }
    }

    function loadStatistics(selectedCaseKey) {
        const requestedCaseKey = selectedCaseKey || caseKey;
        if (!requestedCaseKey) return $.Deferred().resolve().promise();
        return request(
            `/api/case-opening/cases/${encodeURIComponent(requestedCaseKey)}/statistics`,
            'GET',
            { showLoader: false })
            .done(function (statistics) {
                if (statistics.caseKey === caseKey) renderStatistics(statistics);
            })
            .fail(function (response) {
                if (requestWasAborted(response)) return;
                if (statisticsRequestedAfterOpening === requestedCaseKey) statisticsRequestedAfterOpening = null;
                window.personalToolsToast?.error(response.responseJSON?.message || 'Case probability statistics could not be loaded.');
            });
    }

    function caseSelectorTile(item) {
        const inputId = `case-option-${item.caseKey}`;
        const multiplier = Number(item.saleMultiplier || 1);
        const status = $('<span>', { class: `case-selector-status${item.caseKey === caseKey ? ' is-selected' : ''}` }).append(
            $('<i>', {
                class: item.caseKey === caseKey ? 'fa-solid fa-circle-check' : 'fa-solid fa-lock-open',
                'aria-hidden': 'true'
            }),
            document.createTextNode(item.caseKey === caseKey ? ' Selected' : ' Ready'));

        return $('<div>', { class: 'case-selector-column' }).append(
            $('<div>', {
                class: 'case-selector-tile',
                role: 'button',
                tabindex: 0,
                'data-case-key': item.caseKey
            }).append(
                $('<img>', { class: 'case-selector-image', src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                $('<div>', { class: 'case-selector-content' }).append(
                    $('<small>', { text: item.type }),
                    $('<strong>', { text: item.name }),
                    $('<span>', { class: 'case-selector-multiplier', text: `${multiplier}× sell rewards` }),
                    $('<span>', { class: 'case-selector-owned', text: `${Number(item.ownedQuantity || 0).toLocaleString()} ready` })
                ),
                $('<input>', {
                    class: 'visually-hidden',
                    type: 'radio',
                    name: 'caseSelection',
                    id: inputId,
                    value: item.caseKey,
                    checked: item.caseKey === caseKey,
                    'aria-label': `Choose ${item.name}`
                }),
                $('<div>', { class: 'case-selector-actions' }).append(status)
            )
        );
    }

    function loadInventoryCapacity(options) {
        inventoryCapacityLoaded = true;
        return request('/api/case-opening/inventory-capacity', 'GET', Object.assign({ showLoader: false }, options || {}))
            .done(function (capacity) {
                renderInventoryCapacity(capacity);
            })
            .fail(function (response) {
                if (requestWasAborted(response)) return;
                inventoryCapacityLoaded = false;
                showError(response, 'Your inventory capacity could not be loaded.');
            });
    }

    function renderCaseSelector(items) {
        // Loading an individual case refreshes its details before the catalogue is requested again.
        // Keep rendering the last known catalogue during that short gap rather than treating an
        // omitted argument as a failed catalogue response.
        const catalogueItems = Array.isArray(items) ? items : catalogue;
        catalogue = catalogueItems;
        const stockedItems = catalogueItems.filter(item => Number(item.ownedQuantity || 0) > 0);
        const searchText = String($('#caseSelectorSearch').val() || '').trim().toLocaleLowerCase();
        const visibleItems = stockedItems.filter(item => !searchText
            || [item.name, item.type, item.caseKey]
                .some(value => String(value || '').toLocaleLowerCase().includes(searchText)));
        const $grid = $('#caseSelectorGrid').empty();
        visibleItems.forEach(item => $grid.append(caseSelectorTile(item)));
        $('#caseSelectorEmpty').toggleClass('d-none', visibleItems.length > 0);
        $('#caseSelectorMatchCount').text(`${visibleItems.length} of ${stockedItems.length} ready`);
        $('#chooseCaseButton')
            .prop('disabled', catalogueItems.length > 0 && stockedItems.length === 0)
            .attr('title', stockedItems.length ? 'Choose a case or capsule from your stock' : 'No cases or capsules are currently in stock');
        renderOwnedCasesInventory();
    }

    function loadCaseCatalogue(options) {
        catalogueLoaded = true;
        return request('/api/case-opening/cases', 'GET', Object.assign({ showLoader: false }, options || {}))
            .done(function (items) {
                renderCaseSelector(items);
                renderShop(items);
                if (botProgress) renderBotProgress(botProgress);
                if (tradeUpRecipesLoaded) renderTradeUpRecipesPanel();
            })
            .fail(function (response) {
                catalogueLoaded = false;
                showError(response, 'The case catalogue could not be loaded.');
            });
    }

    function caseNameFor(key) {
        return catalogue.find(item => item.caseKey === key)?.name || key;
    }

    function caseImageFor(key) {
        return catalogue.find(item => item.caseKey === key)?.imageUrl || '';
    }

    function inventoryOpenedDate(value) {
        const opened = new Date(value);
        if (Number.isNaN(opened.getTime())) return '—';
        const day = opened.getDate();
        const suffix = day % 10 === 1 && day !== 11 ? 'st'
            : day % 10 === 2 && day !== 12 ? 'nd'
                : day % 10 === 3 && day !== 13 ? 'rd' : 'th';
        return `${day}${suffix} ${opened.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })}`;
    }

    function inventoryOpenedDateTime(value) {
        const opened = new Date(value);
        return Number.isNaN(opened.getTime()) ? '' : opened.toLocaleString('en-GB', {
            day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
        });
    }

    function inventoryDetailsText(item) {
        return [
            item.weaponName,
            item.patternName,
            item.phase,
            item.wear,
            item.floatValue == null ? '' : `Float ${Number(item.floatValue).toFixed(6)}`,
            item.patternSeed == null ? '' : `Pattern #${item.patternSeed}`,
            item.isStatTrak ? 'StatTrak™' : ''
        ].filter(Boolean).join(' · ');
    }

    function inventoryImagePreview(item, className) {
        return $('<button>', {
            class: `case-history-image-preview ${className || ''}`,
            type: 'button',
            'data-image-url': item.imageUrl,
            'data-image-name': item.name,
            'aria-label': `Preview ${item.name}`
        }).append($('<img>', { src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }));
    }

    function inventoryCaseLink(item) {
        const caseName = caseNameFor(item.caseKey);
        return $('<button>', {
            class: 'case-history-case-link',
            type: 'button',
            'data-case-key': item.caseKey,
            'aria-label': `View ${caseName} in the shop`
        }).append(
            caseImageFor(item.caseKey) ? $('<img>', { src: caseImageFor(item.caseKey), alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }) : null,
            $('<span>', { text: caseName })
        );
    }

    function openShopCase(caseKeyToShow, focusPurchase) {
        const selectedCase = catalogue.find(item => item.caseKey === caseKeyToShow);
        if (!selectedCase) return;
        shopSearch = selectedCase.name;
        shopTier = '';
        shopType = '';
        $('#caseShopSearch').val(selectedCase.name);
        $('#caseShopTier').val('');
        switchDestination('shop');
        renderShop(catalogue);

        window.setTimeout(function () {
            const card = document.querySelector(`.case-shop-case[data-case-key="${CSS.escape(caseKeyToShow)}"]`);
            card?.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'center' });
            if (focusPurchase) card?.querySelector('.js-shop-buy-case')?.focus({ preventScroll: true });
        }, activeDestination === 'shop' ? 0 : 280);
    }

    function ownedCaseInventoryCard(item) {
        const quantity = Number(item.ownedQuantity || 0);
        const purchaseCost = activeCaseCost(item, 'purchase');
        return $('<div>', { class: 'col-12 col-md-6 col-xl-4' }).append(
            $('<article>', { class: 'case-owned-inventory-card h-100', 'data-case-key': item.caseKey }).append(
                $('<div>', { class: 'case-owned-inventory-image' }).append(
                    $('<img>', { src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                    $('<span>', { class: 'case-owned-inventory-quantity', text: `×${quantity.toLocaleString()}` })
                ),
                $('<div>', { class: 'case-owned-inventory-body' }).append(
                    $('<p>', { class: 'eyebrow mb-1', text: item.type || 'Case' }),
                    $('<h3>', { class: 'h6 mb-1', text: item.name }),
                    $('<p>', {
                        class: 'small-muted mb-0',
                        text: `${formatCurrency(purchaseCost)} each · ${quantity.toLocaleString()} inventory ${quantity === 1 ? 'slot' : 'slots'}`
                    })
                ),
                $('<div>', { class: 'case-owned-inventory-actions' }).append(
                    $('<button>', {
                        class: 'btn btn-warning btn-sm js-owned-case-open',
                        type: 'button',
                        'data-case-key': item.caseKey
                    }).append($('<i>', { class: 'fa-solid fa-box-open me-1', 'aria-hidden': 'true' }), document.createTextNode('Open')),
                    $('<button>', {
                        class: 'btn btn-outline-warning btn-sm js-owned-case-buy',
                        type: 'button',
                        'data-case-key': item.caseKey
                    }).append($('<i>', { class: 'fa-solid fa-cart-plus me-1', 'aria-hidden': 'true' }), document.createTextNode('Buy more')),
                    $('<button>', {
                        class: 'btn btn-outline-danger btn-sm js-owned-case-discard',
                        type: 'button',
                        'data-case-key': item.caseKey,
                        'data-owned-quantity': quantity,
                        'aria-label': `Discard ${item.name}`,
                        title: 'Discard cases'
                    }).append($('<i>', { class: 'fa-solid fa-trash-can me-1', 'aria-hidden': 'true' }), document.createTextNode('Discard'))
                )
            )
        );
    }

    function renderOwnedCasesInventory() {
        const ownedItems = catalogue
            .filter(item => Number(item.ownedQuantity || 0) > 0)
            .sort((left, right) => Number(right.ownedQuantity || 0) - Number(left.ownedQuantity || 0)
                || String(left.name || '').localeCompare(String(right.name || '')));
        const search = String($('#caseOwnedCaseSearch').val() || '').trim().toLowerCase();
        const visibleItems = ownedItems.filter(item => !search || [item.name, item.type, item.caseKey]
            .some(value => String(value || '').toLowerCase().includes(search)));
        const totalQuantity = ownedItems.reduce((total, item) => total + Number(item.ownedQuantity || 0), 0);
        const $grid = $('#caseOwnedCasesGrid').empty();
        visibleItems.forEach(item => $grid.append(ownedCaseInventoryCard(item)));

        $('#caseOwnedCaseSummary').text(
            `${ownedItems.length.toLocaleString()} case ${ownedItems.length === 1 ? 'type' : 'types'} · ${totalQuantity.toLocaleString()} total cases`
        );
        $('#caseOwnedCasesEmpty').toggleClass('d-none', visibleItems.length > 0);
        $('#caseOwnedCasesEmptyTitle').text(ownedItems.length > 0 ? 'No matching cases' : 'No cases owned');
        $('#caseOwnedCasesEmptyCopy').text(ownedItems.length > 0
            ? 'Try a different case or capsule search.'
            : 'Purchase an unlocked case from the Shop and it will appear here.');
        if (inventoryKind === 'cases') {
            $('#caseHistoryCount').text(totalQuantity.toLocaleString());
            window.personalToolsMotion?.reveal($grid.children().get(), { fromY: 6, delay: 22, duration: 230 });
        }
    }

    function setInventoryKind(kind, options) {
        inventoryKind = kind === 'cases' ? 'cases' : 'skins';
        const showingCases = inventoryKind === 'cases';
        $('#caseInventoryKindToggle [data-inventory-kind]').each(function () {
            const active = String($(this).data('inventory-kind')) === inventoryKind;
            $(this).toggleClass('active', active).attr('aria-pressed', active ? 'true' : 'false');
        });
        $('#caseHistoryPanel').toggleClass('d-none', showingCases);
        $('#caseOwnedCasesPanel').toggleClass('d-none', !showingCases);

        try {
            localStorage.setItem(inventoryKindStorageKey, inventoryKind);
        } catch {
            // This is only a display preference and should not block the inventory.
        }

        if (showingCases) {
            renderOwnedCasesInventory();
        } else {
            renderInventorySummary();
            filterHistory({ preservePage: true, skipMotion: options?.skipMotion === true });
        }
    }

    function historyCard(item) {
        const meta = [item.rarityName, item.phase, item.wear].filter(Boolean).join(' · ');
        const condition = item.floatValue == null || item.patternSeed == null
            ? ''
            : `Float ${Number(item.floatValue).toFixed(6)} · Pattern #${item.patternSeed}`;
        const $statTrak = statTrakBadge(item);
        const selected = selectedInventoryIds.has(String(item.openingId));
        return $('<div>', { class: 'col-12 col-lg-6 col-xl-3' }).append(
            $('<article>', {
                class: `case-history-card ${rarityClass(item)}${selected ? ' is-selected' : ''}${item.isLocked ? ' is-locked' : ''}`,
                tabindex: 0,
                role: 'group',
                'aria-label': `${item.name}. Selectable inventory item.`,
                'data-opening-id': item.openingId
            }).append(
                inventoryImagePreview(item, 'case-history-card-image'),
                inventoryLockButton(item),
                $('<div>', { class: 'card-body p-3' }).append(
                    $('<div>', { class: 'case-history-card-rarity-row mb-1' }).append(
                        $('<p>', { class: 'small fw-semibold rarity-label mb-0', text: item.rarityName }),
                        $statTrak,
                        specialVariantBadge(item)
                    ),
                    $('<h3>', { class: 'h6 fw-semibold mb-1', text: item.name }),
                    $('<p>', { class: 'small-muted mb-2', text: meta }),
                    condition ? $('<p>', { class: 'case-history-condition small mb-2', text: condition }) : null,
                    $('<p>', {
                        class: 'small fw-semibold mb-2 text-warning js-case-sale-value',
                        'data-opening-id': item.openingId,
                        text: inventoryValueText(item, false)
                    }),
                    $('<span>', { class: 'badge text-bg-secondary-subtle border mb-2', text: caseNameFor(item.caseKey) }),
                    $('<br>'),
                    $('<time>', { class: 'small-muted', datetime: item.openedUtc, title: inventoryOpenedDateTime(item.openedUtc), text: inventoryOpenedDate(item.openedUtc) }),
                    $('<button>', {
                        class: 'btn btn-outline-primary btn-sm w-100 mt-3 js-inspect-case-item',
                        type: 'button',
                        'data-opening-id': item.openingId,
                        text: 'Inspect item'
                    }).prepend($('<i>', { class: 'fa-solid fa-magnifying-glass me-1', 'aria-hidden': 'true' }))
                )
            )
        );
    }

    function historyTableRow(item) {
        const details = inventoryDetailsText(item);
        const selected = selectedInventoryIds.has(String(item.openingId));
        return $('<tr>', {
            class: `case-history-row ${rarityClass(item)}${selected ? ' is-selected' : ''}${item.isLocked ? ' is-locked' : ''}`,
            tabindex: 0,
            'aria-selected': selected ? 'true' : 'false',
            'data-opening-id': item.openingId
        }).append(
            $('<td>').append(inventoryLockButton(item, true)),
            $('<td>').append(
                $('<span>', { class: 'case-history-item-cell' }).append(
                    inventoryImagePreview(item),
                    $('<span>').append(
                        $('<strong>', { text: item.name }),
                        item.isStatTrak ? statTrakBadge(item) : $('<small>', { text: 'Standard item' }),
                        specialVariantBadge(item)
                    )
                )
            ),
            $('<td>').append(inventoryCaseLink(item)),
            $('<td>').append($('<span>', { class: 'case-history-rarity d-block', text: item.rarityName })),
            $('<td>', { class: 'text-end' }).append($('<span>', {
                class: 'text-warning js-case-sale-value',
                'data-opening-id': item.openingId,
                text: inventoryValueText(item, true)
            })),
            $('<td>').append($('<button>', {
                class: 'case-history-finish',
                type: 'button',
                text: item.wear || '—',
                title: details || 'No additional finish details',
                'data-inventory-details': details || 'No additional finish details'
            })),
            $('<td>').append($('<time>', {
                class: 'small text-nowrap',
                datetime: item.openedUtc,
                title: inventoryOpenedDateTime(item.openedUtc),
                text: inventoryOpenedDate(item.openedUtc)
            })),
            $('<td>', { class: 'text-end' }).append(
                $('<button>', {
                    class: 'btn btn-outline-primary btn-sm js-inspect-case-item',
                    type: 'button',
                    title: `Inspect ${item.name}`,
                    'aria-label': `Inspect ${item.name}`,
                    'data-opening-id': item.openingId
                }).append($('<i>', { class: 'fa-solid fa-magnifying-glass', 'aria-hidden': 'true' }))
            )
        );
    }

    function inventoryLockButton(item, compact) {
        const locked = item.isLocked === true;
        return $('<button>', {
            class: `case-inventory-lock js-case-inventory-lock${locked ? ' is-locked' : ''}${compact ? ' is-compact' : ''}`,
            type: 'button',
            title: locked ? `Unlock ${item.name}` : `Protect ${item.name}`,
            'aria-label': locked ? `Unlock ${item.name}` : `Protect ${item.name}`,
            'aria-pressed': locked ? 'true' : 'false',
            'data-opening-id': item.openingId
        }).append($('<i>', { class: `fa-solid ${locked ? 'fa-lock' : 'fa-lock-open'}`, 'aria-hidden': 'true' }));
    }

    function historyPageItems() {
        const start = (historyPage - 1) * historyPageSize;
        return filteredHistoryItems.slice(start, start + historyPageSize);
    }

    function activeHistoryItems() {
        return allHistoryItems;
    }

    function historyPageCount() {
        return Math.max(1, Math.ceil(filteredHistoryItems.length / historyPageSize));
    }

    function historyPageButton(label, page, disabled, active, ariaLabel) {
        const $item = $('<li>', { class: 'page-item' })
            .toggleClass('disabled', disabled)
            .toggleClass('active', active);
        const $button = $('<button>', {
            class: 'page-link',
            type: 'button',
            text: label,
            'data-page': page,
            'aria-label': ariaLabel || `Page ${page}`
        });
        if (active) $button.attr('aria-current', 'page');
        return $item.append($button);
    }

    function renderHistoryPagination() {
        const pageCount = historyPageCount();
        const $pagination = $('#caseHistoryPagination').empty();
        $pagination.append(historyPageButton('‹', historyPage - 1, historyPage === 1, false, 'Previous page'));

        const startPage = Math.max(1, Math.min(historyPage - 2, pageCount - 4));
        const endPage = Math.min(pageCount, startPage + 4);
        for (let page = startPage; page <= endPage; page += 1) {
            $pagination.append(historyPageButton(String(page), page, false, page === historyPage));
        }

        $pagination.append(historyPageButton('›', historyPage + 1, historyPage === pageCount, false, 'Next page'));
        const first = filteredHistoryItems.length === 0 ? 0 : ((historyPage - 1) * historyPageSize) + 1;
        const last = Math.min(historyPage * historyPageSize, filteredHistoryItems.length);
        $('#caseHistoryPageSummary').text(`Showing ${first}–${last} of ${filteredHistoryItems.length}`);
        $('#caseHistoryPaginationBar').toggleClass('d-none', filteredHistoryItems.length === 0);
    }

    function renderInventorySelection() {
        const sellLimit = Number(inventoryUpgrades?.bulkSellLimit || 100);
        const visibleItems = filteredHistoryItems.filter(item => item.isLocked !== true).slice(0, sellLimit);
        const selectedItems = [...selectedInventoryIds];
        const saleAmount = allHistoryItems
            .filter(item => selectedInventoryIds.has(String(item.openingId)))
            .reduce((total, item) => total + saleAmountFor(item), 0);
        const allVisibleSelected = visibleItems.length > 0
            && visibleItems.every(item => selectedInventoryIds.has(String(item.openingId)));

        $('#caseInventoryActions').toggleClass('d-none', activeHistoryItems().length === 0);
        $('#caseInventoryActions').toggleClass('has-selection', selectedItems.length > 0);
        $('#caseInventorySelectAll').toggleClass('is-active', allVisibleSelected).attr('aria-pressed', allVisibleSelected ? 'true' : 'false');
        $('#caseInventorySelectLabel').text(allVisibleSelected ? 'Clear selected items' : `Select up to ${sellLimit}`);
        $('#caseInventorySelectionText').text(selectedItems.length === 0
            ? '0 selected'
            : `${selectedItems.length} selected · ${formatCurrency(saleAmount)}`);
        $('#sellCaseInventory').prop('disabled', selectedItems.length === 0 || selectedItems.length > sellLimit);

        // Selection should feel immediate. Rebuilding cards after every checkbox change causes
        // the list to jump and interrupts users selecting several items in a row.
        $('.case-history-card, .case-history-row').each(function () {
            const openingId = String($(this).data('opening-id'));
            const selected = selectedInventoryIds.has(openingId);
            $(this).filter('.case-history-card').toggleClass('is-selected', selected);
            $(this).filter('.case-history-row').toggleClass('is-selected', selected).attr('aria-selected', selected ? 'true' : 'false');
        });
    }

    function setInventoryItemSelected(openingId, selected) {
        if (!openingId) return false;
        const item = historyItems.get(openingId);
        if (selected && item?.isLocked === true) {
            window.personalToolsToast?.info('Unlock this protected item before selecting it for sale.');
            return false;
        }
        const sellLimit = Number(inventoryUpgrades?.bulkSellLimit || 100);
        if (selected && !selectedInventoryIds.has(openingId) && selectedInventoryIds.size >= sellLimit) {
            window.personalToolsToast?.info(`Your current bulk-sale limit is ${sellLimit}. Sell this batch or unlock a larger limit.`);
            return false;
        }
        if (selected) selectedInventoryIds.add(openingId);
        else selectedInventoryIds.delete(openingId);
        renderInventorySelection();
        return true;
    }

    function getTradeUpSelection(selectionIds) {
        const ids = selectionIds || selectedInventoryIds;
        const items = allHistoryItems.filter(item => ids.has(String(item.openingId)));
        if (items.length !== 10) {
            return { valid: false, items: items, message: 'Select exactly 10 skins to create a Trade Up Contract.' };
        }

        const rarity = String(items[0].rarityKey || '');
        const outputRarity = { 'mil-spec': 'Restricted', restricted: 'Classified', classified: 'Covert' }[rarity];
        if (!outputRarity) {
            return { valid: false, items: items, message: 'Trade Up Contracts accept Mil-Spec, Restricted or Classified skins.' };
        }

        if (items.some(item => item.isLocked)) {
            return { valid: false, items: items, message: 'Unlock protected skins before adding them to a contract.' };
        }

        if (items.some(item => item.isRareSpecial || item.rarityKey !== rarity || item.floatValue == null)) {
            return { valid: false, items: items, message: 'All 10 contract skins must be standard weapon skins with the same rarity.' };
        }

        const statTrak = items[0].isStatTrak === true;
        if (items.some(item => (item.isStatTrak === true) !== statTrak)) {
            return { valid: false, items: items, message: 'Use either 10 StatTrak™ skins or 10 standard skins.' };
        }

        return {
            valid: true,
            items: items,
            outputRarity: outputRarity,
            message: 'Ready for a Trade Up Contract.'
        };
    }

    // Trade-ups have their own selection state so choosing a contract never interferes with a
    // bulk sale waiting in the Inventory view.
    function renderTradeUpWorkspace() {
        const tradeUp = getTradeUpSelection(tradeUpSelectionIds);
        const selectedItems = tradeUp.items;
        const baseline = selectedItems[0];
        const $slots = $('#caseTradeUpWorkspaceSlots').empty();
        for (let index = 0; index < 10; index += 1) {
            const item = selectedItems[index];
            $slots.append($('<button>', {
                class: `case-trade-up-workspace-slot ${item ? rarityClass(item) : 'is-empty'}`,
                type: 'button',
                disabled: !item,
                'aria-label': item ? `Remove ${item.name} from this contract` : `Empty contract slot ${index + 1}`
            }).data('opening-id', item?.openingId || '').append(item
                ? [$('<img>', { src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }), $('<span>', { text: item.name })]
                : $('<span>', { text: index + 1 })));
        }

        $('#caseTradeUpSelectedCount').text(`${selectedItems.length} / 10 selected`);
        $('#clearTradeUpSelection').prop('disabled', selectedItems.length === 0);
        $('.js-complete-trade-up').prop('disabled', !tradeUp.valid);
        const shouldDrawCompletionAttention = tradeUp.valid && !tradeUpInFlight;
        const $mobileCompletionButton = $('.case-trade-up-mobile-action .js-complete-trade-up');
        if (shouldDrawCompletionAttention && !tradeUpCompletionAttentionActive) {
            $mobileCompletionButton.addClass('is-ready-attention');
            tradeUpCompletionAttentionActive = true;
        } else if (!shouldDrawCompletionAttention && tradeUpCompletionAttentionActive) {
            $mobileCompletionButton.removeClass('is-ready-attention');
            tradeUpCompletionAttentionActive = false;
        }
        $('#caseTradeUpWorkspaceConversion').text(tradeUp.valid
            ? `10 ${selectedItems[0].rarityName} skins → one ${tradeUp.outputRarity} skin`
            : selectedItems.length === 0 ? 'Choose ten eligible skins' : tradeUp.message);

        const $chances = $('#caseTradeUpWorkspaceChances').empty();
        if (tradeUp.valid) {
            const averageFloat = selectedItems.reduce((total, item) => total + Number(item.floatValue || 0), 0) / selectedItems.length;
            const groups = new Map();
            selectedItems.forEach(item => groups.set(item.caseKey, (groups.get(item.caseKey) || 0) + 1));
            $('#caseTradeUpWorkspaceMeta').text(`Average input float: ${averageFloat.toFixed(6)} · Output source chance is based on each collection contribution.`);
            [...groups.entries()].forEach(([sourceCase, count]) => $chances.append($('<span>', { text: `${count * 10}% ${caseNameFor(sourceCase)}` })));
        } else {
            $('#caseTradeUpWorkspaceMeta').text('Mil-Spec, Restricted and Classified standard items can be upgraded. StatTrak™ items must be contracted separately.');
        }

        const candidates = allHistoryItems.filter(item => !item.isLocked && !item.isRareSpecial && ['mil-spec', 'restricted', 'classified'].includes(item.rarityKey) && item.floatValue != null);
        const $candidates = $('#caseTradeUpCandidates').empty();
        candidates.forEach(item => {
            const selected = tradeUpSelectionIds.has(String(item.openingId));
            const compatible = !baseline || selected || (item.rarityKey === baseline.rarityKey && Boolean(item.isStatTrak) === Boolean(baseline.isStatTrak));
            $candidates.append($('<div>', { class: 'col-6 col-md-4 col-xl-3' }).append(
                $('<article>', { class: `case-trade-up-candidate ${rarityClass(item)}${selected ? ' is-selected' : ''}${compatible ? '' : ' is-ineligible'}`, tabindex: compatible ? 0 : -1, role: 'button', 'aria-pressed': selected ? 'true' : 'false' })
                    .data('opening-id', item.openingId)
                    .append($('<img>', { src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }), $('<div>').append($('<small>', { text: item.rarityName }), statTrakBadge(item), $('<strong>', { text: item.name }), $('<span>', { text: `${caseNameFor(item.caseKey)} · ${Number(item.floatValue).toFixed(6)}` })))
            ));
        });
        $('#caseTradeUpCandidateCopy').text(candidates.length ? `${candidates.length} eligible skins in your inventory. Select up to ten matching inputs.` : 'No eligible skins are currently available for a contract.');
    }

    function renderTradeUpResult(result) {
        const item = result.output;
        const rarityKey = String(item.rarityKey || '').toLowerCase();
        const isCovert = rarityKey === 'covert';
        const condition = item.floatValue == null
            ? ''
            : `Float ${Number(item.floatValue).toFixed(6)} · Pattern #${item.patternSeed}`;
        const $statTrak = statTrakBadge(item);
        if ($statTrak) $statTrak.addClass('js-trade-up-result-detail');
        const $effects = $('<div>', { class: 'case-trade-up-reveal-effects', 'aria-hidden': 'true' }).append(
            $('<span>', { class: 'case-trade-up-reveal-flash' }),
            $('<span>', { class: 'case-trade-up-reveal-ring case-trade-up-reveal-ring-one' }),
            $('<span>', { class: 'case-trade-up-reveal-ring case-trade-up-reveal-ring-two' }),
            $('<span>', { class: 'case-trade-up-reveal-slash' }),
            $('<span>', { class: 'case-trade-up-reveal-particles' }).append(
                Array.from({ length: isCovert ? 18 : 8 }, () => $('<i>'))
            )
        );
        const $result = $('#caseTradeUpResult').empty().removeClass('d-none').append(
            $('<div>', { class: `case-trade-up-result-card ${rarityClass(item)}${isCovert ? ' is-covert' : ''}` }).append(
                $effects,
                $('<p>', { class: 'eyebrow mb-1 js-trade-up-result-detail', text: 'Contract complete' }),
                $('<img>', { class: 'js-view-trade-up-inventory', src: item.imageUrl, alt: `View ${item.name} in inventory`, referrerpolicy: 'no-referrer', role: 'button', tabindex: 0, 'data-opening-id': item.openingId }),
                $('<p>', { class: 'case-trade-up-output-rarity mb-1 js-trade-up-result-detail', text: item.rarityName }),
                $statTrak,
                $('<h3>', { class: 'h4 mb-2 js-trade-up-result-detail', text: item.name }),
                $('<p>', { class: 'small-muted mb-2 js-trade-up-result-detail', text: [item.wear, condition].filter(Boolean).join(' · ') }),
                $('<div>', { class: 'case-trade-up-result-chances js-trade-up-result-detail' }).append(
                    result.sourceChances.map(chance => $('<span>', { text: `${Number(chance.percentage).toFixed(0)}% ${chance.caseName}` }))
                )
            )
        );
        const modalElement = document.getElementById('caseTradeUpModal');
        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
        const card = $result.find('.case-trade-up-result-card').get(0);
        if (card && window.anime?.animate && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            $(modalElement).one('shown.bs.modal', function () {
                const modalContent = $(modalElement).find('.modal-content').get(0);
                const image = $(card).find('img').get(0);
                const details = $(card).find('.js-trade-up-result-detail').get();
                const rings = $(card).find('.case-trade-up-reveal-ring').get();
                const flash = $(card).find('.case-trade-up-reveal-flash').get(0);
                const slash = $(card).find('.case-trade-up-reveal-slash').get(0);
                const particles = $(card).find('.case-trade-up-reveal-particles i').get();

                $(modalContent).add(card).add(image).add(details).add(rings).add(flash).add(slash).add(particles).removeAttr('style');
                window.anime.animate(modalContent, {
                    translateY: [24, 0],
                    scale: [.975, 1],
                    opacity: [0, 1],
                    duration: 360,
                    ease: 'out(4)'
                });
                window.anime.animate(card, {
                    scale: isCovert ? [.62, 1.035, 1] : [.76, 1.018, 1],
                    opacity: [0, 1],
                    rotate: isCovert ? [-5, 1.2, 0] : [-2.5, 0],
                    duration: isCovert ? 920 : 680,
                    ease: 'out(6)'
                });
                window.anime.animate(image, {
                    translateY: isCovert ? [-56, 5, 0] : [-30, 0],
                    scale: isCovert ? [.42, 1.16, 1] : [.74, 1.04, 1],
                    rotate: isCovert ? [-9, 2, 0] : [4, 0],
                    opacity: [0, 1],
                    delay: isCovert ? 150 : 100,
                    duration: isCovert ? 880 : 650,
                    ease: 'out(6)'
                });
                window.anime.animate(details, {
                    translateY: [14, 0],
                    opacity: [0, 1],
                    delay: (_, index) => (isCovert ? 340 : 180) + (index * 48),
                    duration: 390,
                    ease: 'out(4)'
                });
                window.anime.animate(rings, {
                    scale: isCovert ? [.18, 1.65] : [.35, 1.25],
                    opacity: [.82, 0],
                    delay: (_, index) => 110 + (index * 145),
                    duration: isCovert ? 1050 : 760,
                    ease: 'out(5)'
                });
                window.anime.animate(slash, {
                    translateX: ['-135%', '135%'],
                    opacity: [0, isCovert ? .9 : .48, 0],
                    delay: 130,
                    duration: isCovert ? 850 : 690,
                    ease: 'inOut(3)'
                });

                if (isCovert) {
                    playReveal(item);
                    window.anime.animate(flash, { opacity: [0, .82, 0], duration: 540, ease: 'out(3)' });
                    window.anime.animate(particles, {
                        translateX: () => `${(Math.random() - .5) * 520}px`,
                        translateY: () => `${-70 - (Math.random() * 260)}px`,
                        scale: [.25, 1.2, 0],
                        rotate: () => `${(Math.random() - .5) * 260}deg`,
                        opacity: [0, 1, 0],
                        delay: (_, index) => 145 + (index * 18),
                        duration: 900,
                        ease: 'out(4)'
                    });
                }
            });
        }
        modal.show();
    }

    function refreshInventorySaleValues() {
        $('.js-case-sale-value').each(function () {
            const item = historyItems.get(String($(this).data('opening-id')));
            if (!item) return;
            $(this).text($(this).closest('tr').length > 0
                ? inventoryValueText(item, true)
                : inventoryValueText(item, false));
        });
    }

    function renderHistoryPage(options) {
        const settings = options || {};
        $historyCards.empty();
        $historyTableBody.empty();
        historyPageItems().forEach(item => {
            $historyCards.append(historyCard(item));
            $historyTableBody.append(historyTableRow(item));
        });

        const hasHistory = activeHistoryItems().length > 0;
        const hasFilteredHistory = filteredHistoryItems.length > 0;
        $('#caseHistoryTableWrap, #caseHistory').toggleClass('d-none', !hasFilteredHistory);
        $empty.toggleClass('d-none', hasHistory);
        $('#caseHistoryFilteredEmpty').toggleClass('d-none', !hasHistory || hasFilteredHistory);
        renderHistoryPagination();
        renderInventorySelection();
        // A sale removes a known set of cards. Replaying the entrance sequence at that point
        // makes the next selection feel delayed, especially when the user sells a full page.
        if (!settings.skipMotion && !window.matchMedia('(max-width: 767.98px), (prefers-reduced-motion: reduce)').matches) {
            window.personalToolsMotion?.reveal(
                $('#caseHistoryTableBody tr:visible, #caseHistory > div:visible').get(),
                { fromY: 6, delay: 18, duration: 220 }
            );
        }
    }

    function filterHistory(options) {
        const search = String($('#caseHistorySearch').val() || '').trim().toLowerCase();
        const rarity = String($('#caseHistoryRarity').val() || '').toLowerCase();
        filteredHistoryItems = activeHistoryItems().filter(item => {
            if (rarity && String(item.rarityKey || '').toLowerCase() !== rarity) return false;
            if (!search) return true;
            return [
                item.name,
                item.marketHashName,
                caseNameFor(item.caseKey),
                item.rarityName,
                item.weaponName,
                item.patternName,
                item.phase,
                item.wear,
                item.floatValue,
                item.patternSeed == null ? '' : `pattern ${item.patternSeed}`,
                item.isStatTrak ? 'stattrak' : '',
                item.isLocked ? 'locked protected' : ''
            ].filter(Boolean).join(' ').toLowerCase().includes(search);
        });
        const compareOpened = (left, right) => new Date(left.openedUtc).getTime() - new Date(right.openedUtc).getTime();
        filteredHistoryItems.sort((left, right) => {
            if (historySort === 'oldest') return compareOpened(left, right);
            if (historySort === 'value-desc') return saleValueFor(right) - saleValueFor(left) || compareOpened(right, left);
            if (historySort === 'value-asc') return saleValueFor(left) - saleValueFor(right) || compareOpened(right, left);
            if (historySort === 'rarity-desc') return rarityRank(right) - rarityRank(left) || compareOpened(right, left);
            if (historySort === 'name') return String(left.name || '').localeCompare(String(right.name || '')) || compareOpened(right, left);
            return compareOpened(right, left);
        });
        $('#caseHistoryClearFiltersWrap').toggleClass('d-none', !search && !rarity && historySort === 'newest');
        // A filter change starts at the first page, but a completed sale should leave the user
        // exactly where they were unless the final item on that page was removed.
        if (!options?.preservePage) {
            historyPage = 1;
        }

        historyPage = Math.min(historyPage, Math.max(1, historyPageCount()));
        renderHistoryPage(options);
    }

    function renderInventorySummary() {
        if (inventoryKind === 'skins') $('#caseHistoryCount').text(allHistoryItems.length);
        $('#caseHistoryEmptyTitle').text('No saved openings yet');
        $('#caseHistoryEmptyCopy').text('Your unsold simulated case items will collect here.');
        refreshHistoryRarityFilter(allHistoryItems);
    }

    function refreshHistoryRarityFilter(items) {
        const selected = String($('#caseHistoryRarity').val() || '');
        const rarities = new Map();
        items.forEach(item => rarities.set(String(item.rarityKey), item.rarityName));
        const $filter = $('#caseHistoryRarity').empty().append($('<option>', { value: '', text: 'All rarities' }));
        Array.from(rarities.entries())
            .sort((left, right) => String(left[1]).localeCompare(String(right[1])))
            .forEach(([key, name]) => $filter.append($('<option>', { value: key, text: name })));
        $filter.val(selected);
    }

    function renderHistory(items, options) {
        allHistoryItems = Array.isArray(items) ? items : [];
        historyDirty = false;
        const availableIds = new Set(allHistoryItems.map(item => String(item.openingId)));
        [...selectedInventoryIds].forEach(openingId => {
            const item = allHistoryItems.find(historyItem => String(historyItem.openingId) === openingId);
            if (!availableIds.has(openingId) || item?.isLocked === true) selectedInventoryIds.delete(openingId);
        });
        historyItems.clear();
        [...allHistoryItems, ...sessionOpenings].forEach(item => {
            historyItems.set(String(item.openingId), item);
        });
        renderInventorySummary();
        filterHistory(options);
    }

    function loadHistory(options) {
        historyLoaded = true;
        return request('/api/case-opening/history', 'GET', options)
            .done(function (items) {
                renderHistory(items);
            })
            .fail(function (response) {
                historyLoaded = false;
                showError(response, 'Your case-opening history could not be loaded.');
            });
    }

    function loadProgress(options) {
        progressLoaded = true;
        return request('/api/case-opening/progress', 'GET', options)
            .done(function (progress) {
                renderProgress(progress);
            })
            .fail(function (response) {
                if (requestWasAborted(response)) return;
                progressLoaded = false;
                showError(response, 'Your currency balance could not be loaded.');
            });
    }

    function loadInventoryUpgrades(options) {
        inventoryUpgradesLoaded = true;
        return request('/api/case-opening/inventory/upgrades', 'GET', options).done(function (result) {
            inventoryUpgrades = result;
            renderInventoryUpgradeStore();
            renderInventorySelection();
        }).fail(function (response) {
            inventoryUpgradesLoaded = false;
            showError(response, 'Inventory upgrades could not be loaded.');
        });
    }

    function renderInventoryUpgradeStore() {
        const $grid = $('#caseInventoryUpgradeGrid').empty();
        const $capacityGrid = $('#caseCapacityUpgradeGrid').empty();
        const stars = Number(inventoryUpgrades?.activeBalanceMinor ?? activeBalance());
        const level = Number(caseProgress?.level || 0);
        const availableUpgrades = inventoryUpgrades?.availableUpgrades || [];
        $('#caseCapacityUpgradeStatus').text(
            `${Number(inventoryUpgrades?.bonusInventorySlots || 0).toLocaleString()} bonus slots unlocked`
        );
        const consolidatedCategories = new Set(['automation', 'bulk-sale', 'auto-sell', 'trade-up-unlock', 'trade-up-slots', 'trade-up-holding']);
        availableUpgrades.forEach(function (upgrade) {
            // Auto-buy and trade-up-recipe tiers each get one consolidated card inside their own
            // panel instead of a generic row per tier - see renderAutoBuyPanel()/renderTradeUpRecipesPanel().
            if (consolidatedCategories.has(String(upgrade.category || '').toLowerCase())) return;
            const unlocked = upgrade.isUnlocked === true;
            const prerequisiteKey = {
                'inventory-slots-500': 'inventory-slots-250',
                'inventory-slots-1000': 'inventory-slots-500'
            }[String(upgrade.upgradeKey || '').toLowerCase()];
            const prerequisiteUnlocked = !prerequisiteKey || availableUpgrades.some(item =>
                String(item.upgradeKey || '').toLowerCase() === prerequisiteKey && item.isUnlocked === true);
            const meetsLevel = level >= Number(upgrade.requiredLevel || 0);
            const actionText = unlocked
                ? 'Unlocked'
                : !prerequisiteUnlocked
                    ? 'Previous tier required'
                    : !meetsLevel
                        ? `Level ${Number(upgrade.requiredLevel || 0)} required`
                        : `Unlock · ${formatCurrency(Number(upgrade.cost || 0), true)}`;
            const $action = $('<button>', {
                class: 'btn btn-outline-warning btn-sm js-unlock-inventory-upgrade', type: 'button',
                'data-upgrade-key': upgrade.upgradeKey,
                disabled: unlocked || !prerequisiteUnlocked || !meetsLevel || stars < Number(upgrade.cost || 0),
                text: actionText
            });
            const $targetGrid = String(upgrade.category || '').toLowerCase() === 'capacity' ? $capacityGrid : $grid;
            $targetGrid.append($('<div>', { class: 'col-12 col-md-6' }).append(
                $('<article>', { class: `case-shop-row h-100${unlocked ? ' is-unlocked' : ''}` }).append(
                    $('<div>').append($('<h3>', { class: 'h6 mb-1', text: upgrade.name }), $('<p>', { class: 'small-muted mb-1', text: upgrade.description }),
                        $('<small>', { class: 'case-upgrade-level', text: `Level ${upgrade.requiredLevel}` })), $action)
            ));
        });

        renderConsolidatedUpgradeTier('bulk-sale', $('#caseBulkSellUpgradeButton'), $('#caseBulkSellUpgradeDescription'), 'Sell more items in one confirmed sale.', 'Bulk-sale limit maxed');
        $('#caseBulkSellStatus').text(`Current limit: ${Number(inventoryUpgrades?.bulkSellLimit || 100).toLocaleString()} items`);

        const autoSell = [
            ['covert', 'Covert', 'Rare', 'autoSellCovertUnlocked', 'autoSellCovertEnabled'],
            ['classified', 'Classified', 'Uncommon', 'autoSellClassifiedUnlocked', 'autoSellClassifiedEnabled'],
            ['restricted', 'Restricted', 'Common', 'autoSellRestrictedUnlocked', 'autoSellRestrictedEnabled'],
            ['mil-spec', 'Mil-Spec', 'Most common', 'autoSellMilSpecUnlocked', 'autoSellMilSpecEnabled']
        ];
        renderConsolidatedUpgradeTier('auto-sell', $('#caseAutoSellUpgradeButton'), $('#caseAutoSellUpgradeDescription'), 'Unlock the next rarity tier for automatic selling.', 'All auto-sell tiers unlocked');
        const autoSellUnlocked = autoSell.some(entry => inventoryUpgrades?.[entry[3]] === true);
        const $progression = $('#caseAutoSellProgression').empty();
        autoSell.forEach(function (entry, index) {
            const unlocked = inventoryUpgrades?.[entry[3]] === true;
            $progression.append($('<li>', { class: unlocked ? 'is-unlocked' : '' }).append(
                $('<strong>', { text: entry[1] }),
                $('<span>', { text: `${entry[2]} drops · ${unlocked ? 'Ready to enable' : `Step ${index + 1}`}` })
            ));
        });
        const $controls = $('#caseAutoSellControls').empty();
        autoSell.forEach(function (entry) {
            const unlocked = inventoryUpgrades?.[entry[3]] === true;
            const inputId = `caseAutoSell-${entry[0]}`;
            $controls.append($('<label>', { class: `case-auto-sell-option${unlocked ? '' : ' is-locked'}`, for: inputId }).append(
                $('<span>').append($('<strong>', { text: entry[1] }), $('<small>', { text: unlocked ? 'Sell immediately after opening' : 'Unlock in the progression list' })),
                $('<input>', { class: 'form-check-input pt-switch js-auto-sell-toggle', type: 'checkbox', role: 'switch', id: inputId,
                    'data-rarity-key': entry[0], checked: unlocked && inventoryUpgrades?.[entry[4]] === true, disabled: !unlocked })
            ));
        });
        $('#casePreserveStatTrak')
            .prop('checked', inventoryUpgrades?.preserveStatTrak !== false)
            .prop('disabled', !autoSellUnlocked);
        $('#casePreserveStatTrakControl')
            .toggleClass('is-locked', !autoSellUnlocked)
            .attr('title', autoSellUnlocked ? 'Protect StatTrak™ items from automatic selling.' : 'Unlock at least one automatic-selling rarity first.');
        $('#casePreserveStatTrakHint').text(autoSellUnlocked ? 'Applies to every enabled rarity' : 'Unlock an auto-sell tier first');

        renderAutoBuyPanel();
        renderTradeUpUpgradePanel();
        if (tradeUpRecipesLoaded) renderTradeUpRecipesPanel();
    }

    function loadAutoBuyRules(options) {
        autoBuyRulesLoaded = true;
        return request('/api/case-opening/auto-buy/rules', 'GET', options).done(function (result) {
            autoBuySummary = result;
            renderAutoBuyPanel();
            manageAutoBuyPolling();
        }).fail(function (response) {
            autoBuyRulesLoaded = false;
            showError(response, 'Auto-buy rules could not be loaded.');
        });
    }

    // Shared by every upgrade progression that gets a single consolidated button instead of one
    // row per tier (auto-buy, trade-up recipe slots, trade-up holding capacity): shows the next
    // locked tier's cost/level, or "Maxed out" once every tier in the category is unlocked.
    // Returns the next locked tier (or null) so callers can key further UI off it.
    function renderConsolidatedUpgradeTier(category, $button, $description, fallbackDescription, maxedOutText) {
        const stars = Number(inventoryUpgrades?.activeBalanceMinor ?? activeBalance());
        const level = Number(caseProgress?.level || 0);
        const tiers = (inventoryUpgrades?.availableUpgrades || [])
            .filter(upgrade => String(upgrade.category || '').toLowerCase() === category)
            .sort((a, b) => Number(a.sortOrder || 0) - Number(b.sortOrder || 0));
        const nextTier = tiers.find(upgrade => upgrade.isUnlocked !== true);

        if (tiers.length === 0) {
            $button.addClass('d-none').removeAttr('data-upgrade-key');
        } else if (!nextTier) {
            $button.removeClass('d-none').prop('disabled', true).removeAttr('data-upgrade-key').text(maxedOutText || 'Maxed out');
            $description.text('All tiers unlocked.');
        } else {
            const meetsLevel = level >= Number(nextTier.requiredLevel || 0);
            const canAfford = stars >= Number(nextTier.cost || 0);
            $button.removeClass('d-none')
                .attr('data-upgrade-key', nextTier.upgradeKey)
                .prop('disabled', !meetsLevel || !canAfford)
                .text(meetsLevel ? `Unlock · ${formatCurrency(Number(nextTier.cost || 0), true)}` : `Level ${Number(nextTier.requiredLevel || 0)} required`);
            $description.text(nextTier.description || fallbackDescription);
        }
        return nextTier || null;
    }

    // Renders both halves of the Auto-buy panel: the consolidated "automation" upgrade button
    // (unlock -> more slots -> more slots), and, once at least the first tier is unlocked, an
    // explicit "add a rule" picker plus one row per configured rule. Rows are driven entirely by
    // configured rules (not by every unlocked case), so slot count and row count always agree.
    function renderAutoBuyPanel() {
        renderConsolidatedUpgradeTier(
            'automation',
            $('#caseAutoBuyUpgradeButton'),
            $('#caseAutoBuyUpgradeDescription'),
            'Automatically repurchase cases when your owned stock runs low.');

        const unlocked = inventoryUpgrades?.autoBuyUnlocked === true;
        $('#caseAutoBuyRuleEditor').toggleClass('d-none', !unlocked);
        if (!unlocked) return;

        const ruleSlots = Number(inventoryUpgrades?.autoBuyRuleSlots || 0);
        const rules = autoBuySummary?.rules || [];
        const usedRuleSlots = Number(autoBuySummary?.usedRuleSlots || 0);
        $('#caseAutoBuyRuleStatus').text(`${usedRuleSlots} / ${ruleSlots} active rules`);

        const configuredKeys = new Set(rules.map(rule => String(rule.caseKey).toLowerCase()));
        const casesWithoutRule = catalogue.filter(item => isCaseUnlocked(item) && !configuredKeys.has(String(item.caseKey).toLowerCase()));

        // The select itself stays enabled even when empty - a disabled <select> falls back to
        // native browser styling here (near-invisible text on a light background) that ignores
        // this page's dark theme. Disabling only the Add button is enough to block the action.
        const $addCase = $('#caseAutoBuyAddCase').empty().prop('disabled', false);
        if (casesWithoutRule.length === 0) {
            // Two different reasons the picker can be empty: either every unlocked case already
            // has a rule (fixed by unlocking more cases), or the rule-slot cap itself is maxed
            // out at the top Auto-buy tier (fixed by nothing - there's no further upgrade).
            const atRuleLimit = ruleSlots > 0 && rules.length >= ruleSlots;
            const message = atRuleLimit && !nextTier
                ? `Auto-buy rule limit reached (${ruleSlots}/${ruleSlots})`
                : 'Unlock more cases to add more rules';
            $addCase.append($('<option>', { value: '', text: message }));
        } else {
            casesWithoutRule.forEach(item => $addCase.append($('<option>', { value: item.caseKey, text: item.name })));
        }
        $('#caseAutoBuyAddRow').removeClass('d-none');
        $('#caseAutoBuyAddButton').prop('disabled', casesWithoutRule.length === 0);

        const $grid = $('#caseAutoBuyRules').empty();
        if (rules.length === 0) {
            $grid.append($('<div>', {
                class: 'case-auto-buy-empty',
                text: casesWithoutRule.length ? 'Add a rule below to start restocking a case automatically.' : 'Unlock a case to configure a restocking rule for it.'
            }));
            return;
        }

        rules.forEach(function (rule) {
            $grid.append($('<article>', { class: 'case-auto-buy-row', 'data-case-key': rule.caseKey }).append(
                $('<img>', { src: rule.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                $('<span>', { class: 'case-auto-buy-row-name', text: rule.caseName }),
                $('<span>', { class: 'case-auto-buy-row-owned', text: `${Number(rule.ownedQuantity || 0).toLocaleString()} owned` }),
                $('<label>', { class: 'case-auto-buy-row-field' }).append(
                    $('<span>', { text: 'Below' }),
                    $('<input>', { class: 'form-control form-control-sm js-auto-buy-threshold', type: 'number', min: 0, step: 1, value: Number(rule.thresholdQuantity ?? 5) })
                ),
                $('<label>', { class: 'case-auto-buy-row-field' }).append(
                    $('<span>', { text: 'Buy' }),
                    $('<input>', { class: 'form-control form-control-sm js-auto-buy-quantity', type: 'number', min: 1, step: 1, value: Number(rule.purchaseQuantity ?? 10) })
                ),
                $('<div>', { class: 'form-check form-switch m-0' }).append(
                    $('<input>', { class: 'form-check-input pt-switch js-auto-buy-toggle', type: 'checkbox', role: 'switch', checked: rule.isEnabled === true }),
                    $('<label>', { class: 'visually-hidden', text: `Enable auto-buy for ${rule.caseName}` })
                ),
                $('<button>', { class: 'btn btn-outline-danger btn-sm js-auto-buy-remove', type: 'button', text: 'Remove' })
            ));
        });
    }

    function addAutoBuyRule() {
        const caseKeyToAdd = String($('#caseAutoBuyAddCase').val() || '');
        if (!caseKeyToAdd) return;
        const payload = {
            thresholdQuantity: Math.max(0, Math.trunc(Number($('#caseAutoBuyAddThreshold').val()) || 0)),
            purchaseQuantity: Math.max(1, Math.trunc(Number($('#caseAutoBuyAddQuantity').val()) || 1)),
            isEnabled: true
        };
        const $button = $('#caseAutoBuyAddButton').prop('disabled', true);
        request(`/api/case-opening/auto-buy/rules/${encodeURIComponent(caseKeyToAdd)}`, 'PUT', {
            data: JSON.stringify(payload),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (result) {
                autoBuySummary = result;
                renderAutoBuyPanel();
                manageAutoBuyPolling();
                window.personalToolsToast?.success('Auto-buy rule added.');
            })
            .fail(function (response) {
                renderAutoBuyPanel();
                showError(response, 'This auto-buy rule could not be added.');
            })
            .always(() => $button.prop('disabled', false));
    }

    function removeAutoBuyRule($row) {
        const caseKeyToRemove = String($row.data('case-key'));
        const $button = $row.find('.js-auto-buy-remove').prop('disabled', true);
        request(`/api/case-opening/auto-buy/rules/${encodeURIComponent(caseKeyToRemove)}`, 'DELETE', { showLoader: false })
            .done(function (result) {
                autoBuySummary = result;
                renderAutoBuyPanel();
                manageAutoBuyPolling();
                window.personalToolsToast?.success('Auto-buy rule removed.');
            })
            .fail(function (response) {
                $button.prop('disabled', false);
                showError(response, 'This auto-buy rule could not be removed.');
            });
    }

    function saveAutoBuyRule($row) {
        const caseKeyToSave = String($row.data('case-key'));
        const payload = {
            thresholdQuantity: Math.max(0, Math.trunc(Number($row.find('.js-auto-buy-threshold').val()) || 0)),
            purchaseQuantity: Math.max(1, Math.trunc(Number($row.find('.js-auto-buy-quantity').val()) || 1)),
            isEnabled: $row.find('.js-auto-buy-toggle').is(':checked')
        };
        request(`/api/case-opening/auto-buy/rules/${encodeURIComponent(caseKeyToSave)}`, 'PUT', {
            data: JSON.stringify(payload),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (result) {
                autoBuySummary = result;
                renderAutoBuyPanel();
                manageAutoBuyPolling();
                window.personalToolsToast?.success('Auto-buy rule saved.');
            })
            .fail(function (response) {
                renderAutoBuyPanel();
                showError(response, 'This auto-buy rule could not be saved.');
            });
    }

    // Same client-driven polling pattern as bots - no server-side background service. Runs only
    // once Auto-buy is actually unlocked, and pauses while the tab isn't visible.
    function manageAutoBuyPolling() {
        const shouldPoll = inventoryUpgrades?.autoBuyUnlocked === true && !document.hidden;
        if (shouldPoll && !autoBuyPollTimer) {
            autoBuyPollTimer = window.setInterval(evaluateAutoBuy, 20000);
        } else if (!shouldPoll && autoBuyPollTimer) {
            window.clearInterval(autoBuyPollTimer);
            autoBuyPollTimer = null;
        }
    }

    // Also called opportunistically right after any case open (player or bot) - the two events
    // that can drop an owned quantity below a rule's threshold between polls.
    function evaluateAutoBuy() {
        if (!inventoryUpgrades?.autoBuyUnlocked || autoBuyEvaluationRequest) return;
        autoBuyEvaluationRequest = request('/api/case-opening/auto-buy/evaluate', 'POST', { showLoader: false })
            .done(function (purchases) {
                if (!Array.isArray(purchases) || purchases.length === 0) return;

                loadInventoryCapacity({ showLoader: false });
                loadProgress({ showLoader: false });
                if (autoBuyRulesLoaded) loadAutoBuyRules({ showLoader: false });
                if ($('#caseShopCaseGrid').children().length) loadCaseCatalogue().done(() => renderShop(catalogue));

                purchases.forEach(function (purchase) {
                    const caseName = catalogue.find(item => item.caseKey === purchase.caseKey)?.name || purchase.caseKey;
                    window.personalToolsToast?.info(`Auto-bought ${purchase.purchasedQuantity} × ${caseName}.`);
                });
            })
            .always(function () {
                autoBuyEvaluationRequest = null;
            });
        // A quiet poll failure just tries again next tick - no .fail handler, no error surfaced.
    }

    function loadTradeUpRecipes(options) {
        tradeUpRecipesLoaded = true;
        return request('/api/case-opening/trade-up-recipes', 'GET', options).done(function (result) {
            tradeUpRecipeSummary = result;
            renderTradeUpRecipesPanel();
            manageTradeUpRecipePolling();
        }).fail(function (response) {
            tradeUpRecipesLoaded = false;
            showError(response, 'Auto trade-up recipes could not be loaded.');
        });
    }

    // Lives in the Upgrades tab: the one-time unlock, then (once unlocked) the recipe-slots
    // ladder. Holding capacity is purchased from inside the recipe modal instead, since it's most
    // relevant right when you're creating or reviewing a recipe.
    function renderTradeUpUpgradePanel() {
        renderConsolidatedUpgradeTier(
            'trade-up-unlock',
            $('#caseTradeUpUnlockButton'),
            $('#caseTradeUpUnlockDescription'),
            'Automatically fire Trade Up Contracts toward a target skin.',
            'Unlocked');

        const unlocked = inventoryUpgrades?.tradeUpRecipesUnlocked === true;
        $('#caseTradeUpUpgradeTiers').toggleClass('d-none', !unlocked);
        if (unlocked) renderTradeUpSlotUpgradeButton();
    }

    // Recipe slots are a repeatable +1 purchase (like the bot speed upgrade), not a discrete-tier
    // upgrade, so this reads its cost straight off inventoryUpgrades rather than availableUpgrades.
    function renderTradeUpSlotUpgradeButton() {
        const slots = Number(inventoryUpgrades?.tradeUpRecipeSlots || 0);
        const maximum = Number(inventoryUpgrades?.maximumTradeUpRecipeSlots || 0);
        const cost = Number(inventoryUpgrades?.tradeUpRecipeSlotUpgradeCost || 0);
        const balance = Number(inventoryUpgrades?.activeBalanceMinor ?? activeBalance());
        const $button = $('#caseTradeUpSlotsUpgradeButton');

        $('#caseTradeUpSlotsDescription').text(`${slots} / ${maximum} recipe slots.`);
        if (slots >= maximum) {
            $button.prop('disabled', true).text('Maximum reached');
        } else {
            $button.prop('disabled', balance < cost).text(`Add slot · ${formatCurrency(cost, true)}`);
        }
    }

    // Caps (recipe slots, holding capacity) come straight off inventoryUpgrades so they update the
    // instant an upgrade is bought - see the "must refresh" lesson from the Auto-buy panel. Used
    // counts and the actual recipe/holding lists still come from the separately-fetched summary.
    function renderTradeUpRecipesPanel() {
        const unlocked = inventoryUpgrades?.tradeUpRecipesUnlocked === true;
        $('#caseTradeUpLockedNotice').toggleClass('d-none', unlocked);
        $('#caseTradeUpUnlockedContent').toggleClass('d-none', !unlocked);
        if (!unlocked) return;

        const summary = tradeUpRecipeSummary;
        const recipes = summary?.recipes || [];
        const holdings = summary?.holdings || [];
        const recipeSlots = Number(inventoryUpgrades?.tradeUpRecipeSlots || 0);
        const usedRecipeSlots = Number(summary?.usedRecipeSlots || 0);
        const usedHoldingCount = Number(summary?.usedHoldingCount || 0);
        const atRecipeCap = usedRecipeSlots >= recipeSlots;

        $('#caseTradeUpRecipeSlotStatus').text(`${usedRecipeSlots} / ${recipeSlots} active recipes`);
        $('#caseTradeUpHoldingStatus').text(`${usedHoldingCount.toLocaleString()} held`);

        const $recipeGrid = $('#caseTradeUpRecipeGrid').empty();
        $recipeGrid.append($('<article>', {
            class: `case-trade-up-recipe-card is-add${atRecipeCap ? ' disabled' : ''}`,
            id: 'caseTradeUpRecipeAddCard',
            role: 'button',
            tabindex: atRecipeCap ? -1 : 0,
            title: atRecipeCap ? 'You have reached your active recipe limit.' : 'Create a recipe'
        }).append(
            $('<i>', { class: 'fa-solid fa-plus', 'aria-hidden': 'true' }),
            $('<span>', { text: 'Create recipe' })
        ));

        recipes.forEach(function (recipe) {
            const eligible = Math.min(10, Number(recipe.eligibleInputCount || 0));
            const held = Number(recipe.heldCount || 0);
            const capacity = Number(recipe.holdingCapacity || 0);
            const $card = $('<article>', {
                class: `case-trade-up-recipe-card ${rarityClass({ rarityKey: recipe.targetRarityKey })}${recipe.isActive ? '' : ' is-inactive'}`,
                'data-recipe-id': recipe.recipeId,
                role: 'button',
                tabindex: 0
            }).append(
                $('<img>', { src: recipe.targetImageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                $('<div>', { class: 'case-trade-up-recipe-card-copy' }).append(
                    $('<strong>', { text: `${recipe.targetItemName}${recipe.targetStatTrak ? ' (ST)' : ''}` }),
                    $('<span>', { text: `${recipe.isActive ? `${eligible}/10 ready` : 'Paused'} · ${held}/${capacity} held` }),
                    $('<span>', { text: tradeUpWearSummary(recipe.targetWears) })
                )
            );
            $recipeGrid.append($card);
        });

        const $holdingGrid = $('#caseTradeUpHoldingGrid').empty();
        if (holdings.length === 0) {
            $holdingGrid.append($('<div>', { class: 'case-trade-up-holding-empty', text: 'Nothing held yet - fired contracts land here until you collect them.' }));
        } else {
            holdings.forEach(function (holding) {
                const isMatch = holding.isMatch === true;
                $holdingGrid.append($('<article>', {
                    class: `case-trade-up-holding-row ${rarityClass(holding)}`,
                    'data-holding-id': holding.holdingId
                }).append(
                    $('<img>', { src: holding.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
                    $('<span>', { class: 'case-trade-up-holding-row-name', text: `${holding.name} (${holding.wear})` }),
                    $('<span>', { class: 'case-trade-up-holding-row-meta', text: `for ${holding.targetItemName}` }),
                    $('<span>', { class: `case-trade-up-holding-row-badge ${isMatch ? 'is-match' : 'is-unmatched'}`, text: isMatch ? 'Matched' : 'No match' }),
                    $('<button>', { class: 'btn btn-warning btn-sm js-trade-up-holding-collect', type: 'button', text: 'Collect' })
                ));
            });
        }
    }

    // Holding capacity is per-recipe, not shared account-wide - the modal always reflects
    // whichever recipe is currently open for management.
    function renderTradeUpModalHoldingCapacity(recipe) {
        const held = Number(recipe.heldCount || 0);
        const capacity = Number(recipe.holdingCapacity || 0);
        const maximum = tradeUpMaximumHoldingCapacity;
        const cost = Number(recipe.holdingUpgradeCost || 0);
        const balance = Number(tradeUpRecipeSummary?.activeBalanceMinor ?? activeBalance());
        const $button = $('#caseTradeUpHoldingUpgradeButton');

        $('#caseTradeUpModalHoldingStatus').text(`${held} / ${capacity} held`);
        if (capacity >= maximum) {
            $button.prop('disabled', true).text('Maximum reached');
        } else {
            $button.prop('disabled', balance < cost).text(`Add capacity · ${formatCurrency(cost, true)}`);
        }
    }

    function openTradeUpCreateModal() {
        const summary = tradeUpRecipeSummary;
        const recipeSlots = Number(inventoryUpgrades?.tradeUpRecipeSlots || 0);
        const usedRecipeSlots = Number(summary?.usedRecipeSlots || 0);
        if (usedRecipeSlots >= recipeSlots) {
            window.personalToolsToast?.error('You have reached your active recipe limit.');
            return;
        }
        tradeUpRecipeModalMode = 'create';
        tradeUpRecipeModalRecipeId = null;
        $('#caseTradeUpRecipeModalTitle').text('Create a recipe');
        $('#caseTradeUpRecipeCreateFields').removeClass('d-none');
        $('#caseTradeUpRecipeManageFields').addClass('d-none');
        $('#caseTradeUpRecipeModalDelete').addClass('d-none');
        $('#caseTradeUpRecipeModalSubmit').removeClass('d-none').text('Create recipe');
        $('#caseTradeUpRecipeModalCost').text(summary ? `Costs ${formatCurrency(Number(summary.recipeCost || 0), true)}` : '');
        $('.js-trade-up-recipe-wear').prop('checked', false);
        renderTradeUpRecipeCasePicker();
        renderTradeUpRecipeSubmitButton();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('caseTradeUpRecipeModal')).show();
    }

    function openTradeUpManageModal(recipeId) {
        const recipe = (tradeUpRecipeSummary?.recipes || []).find(item => String(item.recipeId) === String(recipeId));
        if (!recipe) return;
        tradeUpRecipeModalMode = 'manage';
        tradeUpRecipeModalRecipeId = recipe.recipeId;
        const eligible = Math.min(10, Number(recipe.eligibleInputCount || 0));
        const held = Number(recipe.heldCount || 0);

        $('#caseTradeUpRecipeModalTitle').text('Manage recipe');
        $('#caseTradeUpRecipeCreateFields').addClass('d-none');
        $('#caseTradeUpRecipeManageFields').removeClass('d-none');
        $('#caseTradeUpRecipeManageActive').prop('checked', recipe.isActive === true);
        $('#caseTradeUpRecipeManageMeta').text(`${eligible}/10 matching inputs ready · Wears: ${tradeUpWearSummary(recipe.targetWears)}`);
        renderTradeUpModalHoldingCapacity(recipe);
        $('#caseTradeUpRecipePreview').removeClass('d-none case-rarity-mil-spec case-rarity-restricted case-rarity-classified case-rarity-covert')
            .addClass(rarityClass({ rarityKey: recipe.targetRarityKey }));
        $('#caseTradeUpRecipePreviewImage').attr('src', recipe.targetImageUrl);
        $('#caseTradeUpRecipePreviewName').text(`${recipe.targetItemName}${recipe.targetStatTrak ? ' (StatTrak™)' : ''}`);
        $('#caseTradeUpRecipePreviewRarity').text(recipe.targetRarityName);
        $('#caseTradeUpRecipeModalDelete').removeClass('d-none')
            .prop('disabled', held > 0)
            .attr('title', held > 0 ? 'Collect held skins before deleting this recipe.' : '');
        $('#caseTradeUpRecipeModalSubmit').addClass('d-none');
        $('#caseTradeUpRecipeModalCost').text('');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('caseTradeUpRecipeModal')).show();
    }

    // The case picker only needs to be rebuilt when the unlocked-case set actually changes -
    // repopulating it on every render would reset whatever the player was mid-selecting.
    function renderTradeUpRecipeCasePicker() {
        const $caseSelect = $('#caseTradeUpRecipeCase');
        const unlockedCases = catalogue.filter(item => isCaseUnlocked(item));
        const signature = unlockedCases.map(item => item.caseKey).join('|');
        if ($caseSelect.data('signature') === signature) {
            loadTradeUpRecipeCaseItems(String($caseSelect.val() || ''));
            return;
        }

        $caseSelect.data('signature', signature).empty();
        unlockedCases.forEach(item => $caseSelect.append($('<option>', { value: item.caseKey, text: item.name })));
        if (unlockedCases.length) loadTradeUpRecipeCaseItems(String(unlockedCases[0].caseKey));
    }

    function loadTradeUpRecipeCaseItems(caseKey) {
        if (!caseKey) {
            renderTradeUpRecipeItemPicker([]);
            return;
        }
        if (tradeUpCaseItemsCache.has(caseKey)) {
            renderTradeUpRecipeItemPicker(tradeUpCaseItemsCache.get(caseKey));
            return;
        }
        request(`/api/case-opening/cases/${encodeURIComponent(caseKey)}`, 'GET', { showLoader: false })
            .done(function (data) {
                const items = (data.items || []).filter(item => tradeUpEligibleRarities.has(item.rarityKey) && !item.isRareSpecial);
                tradeUpCaseItemsCache.set(caseKey, items);
                if (String($('#caseTradeUpRecipeCase').val() || '') === caseKey) renderTradeUpRecipeItemPicker(items);
            })
            .fail(response => showError(response, 'That case could not be loaded.'));
    }

    function renderTradeUpRecipeItemPicker(items) {
        const $itemSelect = $('#caseTradeUpRecipeItem').empty();
        if (items.length === 0) {
            $itemSelect.append($('<option>', { value: '', text: 'No eligible skins in this case' })).prop('disabled', true);
        } else {
            $itemSelect.prop('disabled', false);
            items.forEach(item => $itemSelect.append($('<option>', { value: item.sourceItemId, text: `${item.name} (${item.rarityName})` })));
        }
        renderTradeUpRecipeWearOptions();
        syncTradeUpRecipeSelection();
    }

    function renderTradeUpRecipeWearOptions() {
        const $options = $('#caseTradeUpRecipeWearOptions').empty();
        tradeUpWearNames.forEach(function (wear, index) {
            const inputId = `caseTradeUpRecipeWear-${index}`;
            $options.append($('<label>', { class: 'case-trade-up-recipe-wear-option', for: inputId }).append(
                $('<input>', { class: 'form-check-input js-trade-up-recipe-wear', type: 'checkbox', id: inputId, value: wear }),
                $('<span>', { text: wear })
            ));
        });
    }

    // Keeps the StatTrak™ toggle and the live target preview in sync with whatever's currently
    // selected in the case/item pickers.
    function syncTradeUpRecipeSelection() {
        const caseKey = String($('#caseTradeUpRecipeCase').val() || '');
        const sourceItemId = String($('#caseTradeUpRecipeItem').val() || '');
        const items = tradeUpCaseItemsCache.get(caseKey) || [];
        const item = items.find(entry => entry.sourceItemId === sourceItemId);

        const supportsStatTrak = item?.supportsStatTrak === true;
        $('#caseTradeUpRecipeStatTrak')
            .prop('disabled', !supportsStatTrak)
            .prop('checked', supportsStatTrak && $('#caseTradeUpRecipeStatTrak').prop('checked'));

        if (item) {
            $('#caseTradeUpRecipePreview').removeClass('d-none case-rarity-mil-spec case-rarity-restricted case-rarity-classified case-rarity-covert')
                .addClass(rarityClass(item));
            $('#caseTradeUpRecipePreviewImage').attr('src', item.imageUrl);
            $('#caseTradeUpRecipePreviewName').text(item.name);
            $('#caseTradeUpRecipePreviewRarity').text(item.rarityName);
        } else {
            $('#caseTradeUpRecipePreview').addClass('d-none');
        }

        renderTradeUpRecipeSubmitButton();
    }

    function renderTradeUpRecipeSubmitButton() {
        const summary = tradeUpRecipeSummary;
        const hasCase = String($('#caseTradeUpRecipeCase').val() || '') !== '';
        const hasItem = !$('#caseTradeUpRecipeItem').prop('disabled') && String($('#caseTradeUpRecipeItem').val() || '') !== '';
        const stars = activeBalance();
        const cost = Number(summary?.recipeCost || 0);
        const recipeSlots = Number(inventoryUpgrades?.tradeUpRecipeSlots || 0);
        const usedRecipeSlots = Number(summary?.usedRecipeSlots || 0);
        const atCap = !summary || usedRecipeSlots >= recipeSlots;
        $('#caseTradeUpRecipeModalSubmit').prop('disabled', !hasCase || !hasItem || stars < cost || atCap);
    }

    function createTradeUpRecipe() {
        const caseKey = String($('#caseTradeUpRecipeCase').val() || '');
        const sourceItemId = String($('#caseTradeUpRecipeItem').val() || '');
        if (!caseKey || !sourceItemId) return;
        const wears = $('.js-trade-up-recipe-wear:checked').map(function () { return String($(this).val()); }).get();
        const statTrak = $('#caseTradeUpRecipeStatTrak').prop('checked') === true;
        const $button = $('#caseTradeUpRecipeModalSubmit').prop('disabled', true);
        request('/api/case-opening/trade-up-recipes', 'POST', {
            data: JSON.stringify({ caseKey, sourceItemId, wears, statTrak }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (result) {
                tradeUpRecipeSummary = result;
                renderTradeUpRecipesPanel();
                manageTradeUpRecipePolling();
                refreshDestinationData('upgrades');
                window.personalToolsToast?.success('Auto trade-up recipe created.');
                bootstrap.Modal.getOrCreateInstance(document.getElementById('caseTradeUpRecipeModal')).hide();
            })
            .fail(function (response) {
                showError(response, 'This recipe could not be created.');
            })
            .always(() => $button.prop('disabled', false));
    }

    function setTradeUpRecipeActive(recipeId, isActive) {
        return request(`/api/case-opening/trade-up-recipes/${encodeURIComponent(recipeId)}/active`, 'PUT', {
            data: JSON.stringify({ isActive }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (result) {
                tradeUpRecipeSummary = result;
                renderTradeUpRecipesPanel();
                manageTradeUpRecipePolling();
            })
            .fail(function (response) {
                showError(response, 'This recipe could not be updated.');
            });
    }

    function deleteTradeUpRecipe(recipeId) {
        const $button = $('#caseTradeUpRecipeModalDelete').prop('disabled', true);
        request(`/api/case-opening/trade-up-recipes/${encodeURIComponent(recipeId)}`, 'DELETE', { showLoader: false })
            .done(function (result) {
                tradeUpRecipeSummary = result;
                renderTradeUpRecipesPanel();
                manageTradeUpRecipePolling();
                window.personalToolsToast?.success('Recipe deleted.');
                bootstrap.Modal.getOrCreateInstance(document.getElementById('caseTradeUpRecipeModal')).hide();
            })
            .fail(function (response) {
                $button.prop('disabled', false);
                showError(response, 'This recipe could not be deleted.');
            });
    }

    function collectTradeUpHolding($row) {
        const holdingId = String($row.data('holding-id'));
        const $button = $row.find('.js-trade-up-holding-collect').prop('disabled', true);
        request(`/api/case-opening/trade-up-recipes/holdings/${encodeURIComponent(holdingId)}/collect`, 'POST', { showLoader: false })
            .done(function (result) {
                tradeUpRecipeSummary = result;
                renderTradeUpRecipesPanel();
                manageTradeUpRecipePolling();
                loadInventoryCapacity({ showLoader: false });
                if (historyLoaded) loadHistory({ showLoader: false });
                window.personalToolsToast?.success('Skin collected into your inventory.');
            })
            .fail(function (response) {
                $button.prop('disabled', false);
                showError(response, 'This skin could not be collected.');
            });
    }

    // Same client-driven polling pattern as bots and Auto-buy - only runs once the player actually
    // has a recipe, and pauses while the tab isn't visible.
    function manageTradeUpRecipePolling() {
        const shouldPoll = (tradeUpRecipeSummary?.recipes || []).length > 0 && !document.hidden;
        if (shouldPoll && !tradeUpRecipePollTimer) {
            tradeUpRecipePollTimer = window.setInterval(evaluateTradeUpRecipes, 18000);
        } else if (!shouldPoll && tradeUpRecipePollTimer) {
            window.clearInterval(tradeUpRecipePollTimer);
            tradeUpRecipePollTimer = null;
        }
    }

    // Also called opportunistically right after any case open (player or bot) - the event that
    // can bring a recipe's inputs up to ten.
    function evaluateTradeUpRecipes() {
        if (!(tradeUpRecipeSummary?.recipes || []).length || tradeUpRecipeEvaluationRequest) return;

        // Evaluation can take longer than one polling interval when several recipes fire. Keep a
        // single request in flight so a slow response never causes duplicate work from this tab.
        tradeUpRecipeEvaluationRequest = request('/api/case-opening/trade-up-recipes/evaluate', 'POST', { showLoader: false })
            .done(function (results) {
                if (!Array.isArray(results) || results.length === 0) return;

                loadInventoryCapacity({ showLoader: false });
                if (tradeUpRecipesLoaded) loadTradeUpRecipes({ showLoader: false });
                if (historyLoaded) loadHistory({ showLoader: false });

                results.forEach(function (result) {
                    const label = result.isMatch ? 'matched - ready to collect' : 'did not match - waiting to be collected';
                    window.personalToolsToast?.info(`Auto trade-up fired for ${result.output.name}: ${label}.`);
                });
            })
            .always(function () {
                tradeUpRecipeEvaluationRequest = null;
            });
        // A quiet poll failure just tries again next tick - no .fail handler, no error surfaced.
    }

    function loadAchievements(options) {
        achievementsLoaded = true;
        return request('/api/case-opening/achievements', 'GET', options)
            .done(function (summary) {
                renderAchievements(summary);
            })
            .fail(function (response) {
                if (requestWasAborted(response)) return;
                achievementsLoaded = false;
                showError(response, 'Your achievements could not be loaded.');
            });
    }

    function reelTarget(result) {
        const winner = $reel.children().eq(result.winnerIndex).get(0);
        const viewport = document.getElementById('caseReelWindow');
        if (!winner || !viewport) return 0;

        // offsetLeft uses the browser's final responsive layout, avoiding accumulated rounding
        // errors from multiplying a nominal card width across the complete reel.
        const winnerCentre = winner.offsetLeft + (winner.offsetWidth / 2);
        return (viewport.clientWidth / 2) - winnerCentre;
    }

    function animateReel(result) {
        $('.case-machine').removeClass('is-multi-results');
        $('#caseReelWindow').removeClass('has-multi-results has-scrollable-multi');
        $('#caseMultiResults').addClass('d-none').empty().removeAttr('data-open-count');
        $reel.removeClass('case-skip-reel case-multi-reel').empty().css('transform', 'translateX(0px)');
        result.reel.forEach(item => $reel.append(itemCard(item, 'case-reel-item')));
        $idle.addClass('d-none');
        $result.addClass('d-none');
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        renderOpenButton('rolling');

        // Give the browser one complete paint with the reel at its starting position. Starting
        // the transform in the same frame as inserting the cards can skip that visual state on
        // mobile GPUs, which makes the reel appear late or not at all.
        window.requestAnimationFrame(function () {
            window.requestAnimationFrame(function () {
                const target = reelTarget(result);
                if (!window.anime?.animate || reduced) {
                    $reel.css('transform', `translate3d(${target}px,0,0)`);
                    if (isGoldItem(result.winner)) showGoldReveal(result);
                    else finishOpening(result);
                    return;
                }

                const spinDuration = Math.round(5200 / openSpeedMultiplier());
                startReelSounds(spinDuration);
                window.anime.animate($reel.get(0), {
                    translateX: [0, target],
                    duration: spinDuration,
                    ease: 'out(5)',
                    onComplete: function () {
                        if (isGoldItem(result.winner)) showGoldReveal(result);
                        else finishOpening(result);
                    }
                });
            });
        });
    }

    function rareSpecialKind(item) {
        const identity = `${item?.weaponName || ''} ${item?.name || ''}`.toLowerCase();
        return /glove|hand wrap/.test(identity) ? 'glove' : 'knife';
    }

    function showGoldReveal(result, onComplete, awardDuringReveal) {
        const winner = result.winner;
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const complete = typeof onComplete === 'function' ? onComplete : () => finishOpening(result);
        const shouldAward = awardDuringReveal !== false;
        playReveal(winner);
        window.personalToolsKillfeed?.headshot(
            document.body.dataset.displayName || 'You',
            'Gaben',
            'gold'
        );
        runParticles(winner.rarityColor || '#e4ae39', 120);

        const $reveal = $('#caseGoldReveal');
        const reveal = $reveal.get(0);
        const image = document.getElementById('caseGoldRevealImage');
        const baseSilhouette = document.getElementById('caseGoldRevealBaseSilhouette');
        const silhouette = document.getElementById('caseGoldRevealSilhouette');
        const content = $reveal.find('.case-gold-content').get(0);
        const rings = $reveal.find('.case-gold-ring').get();
        const kind = rareSpecialKind(winner);
        const revealIdentity = function () {
            $('#caseGoldRevealEyebrow').text('Rare special item revealed');
            $('#caseGoldRevealName').text(winner.name);
            $('#caseGoldRevealMeta').text([winner.phase, winner.wear, winner.isStatTrak ? 'StatTrak™' : '', resultMarketText(result)].filter(Boolean).join(' · '));
            $(image).attr('alt', winner.name);
            $reveal.addClass('is-item-revealed').attr('aria-busy', 'false');
        };
        window.clearTimeout(rareRevealTimer);
        rareRevealDismiss = null;
        $reveal.add($reveal.find('.case-gold-content, .case-gold-content img, .case-gold-ring, .case-gold-slash, .case-gold-scan')).removeAttr('style');
        $(baseSilhouette).attr('src', kind === 'knife'
            ? 'https://community.akamai.steamstatic.com/economy/image/i0CoZ81Ui0m-9KwlBY1L_18myuGuq1wfhWSaZgMttyVfPaERSR0Wqmu7LAocGJKz2lu_XuWbwcuyMESA4Fdl-4nnpU7iQA3-kKnr8ytd6s2te7cjd6HHXmHBxep157VtTi_rzUR-5WiHnt39c3_EZg4pW5UjQOZbsBCxw8qnab32FBG7RA'
            : '');
        $('#caseGoldRevealSilhouette').attr('src', winner.imageUrl);
        $('#caseGoldRevealImage').attr({ src: winner.imageUrl, alt: '' });
        $('#caseGoldRevealEyebrow').text('Rare special drop');
        $('#caseGoldRevealName, #caseGoldRevealMeta').empty();
        $reveal.removeClass('d-none is-ready is-item-revealed').addClass(`is-${kind}`).attr('aria-busy', 'true');
        $('body').addClass('case-rare-reveal-open');
        reveal.focus({ preventScroll: true });
        if (shouldAward) awardXp([result], $('#caseGoldRevealImage'));

        let closed = false;
        const closeReveal = function () {
            if (closed || !$reveal.hasClass('is-ready')) return;
            closed = true;
            window.clearTimeout(rareRevealTimer);
            rareRevealDismiss = null;
            const finish = function () {
                $reveal.addClass('d-none').removeClass('is-ready is-item-revealed is-knife is-glove').removeAttr('style');
                $('body').removeClass('case-rare-reveal-open');
                complete();
            };
            if (!window.anime?.animate || reduced) {
                finish();
                return;
            }
            window.anime.animate(reveal, {
                opacity: [1, 0],
                scale: [1, 1.018],
                duration: 260,
                ease: 'in(2)',
                onComplete: finish
            });
        };
        rareRevealDismiss = closeReveal;

        if (!window.anime?.animate || reduced) {
            $reveal.css('opacity', 1);
            $(image).css({ opacity: 1, clipPath: 'inset(0 0 0 0)' });
            $(baseSilhouette).css('opacity', 0);
            $(silhouette).css('opacity', .18);
            revealIdentity();
            $reveal.addClass('is-ready');
            rareRevealTimer = window.setTimeout(closeReveal, 1800);
            return;
        }

        window.anime.animate(reveal, { opacity: [0, 1], duration: 180, ease: 'out(3)' });
        window.anime.animate(rings, { scale: [.2, 1.45], opacity: [.9, 0], delay: (_, index) => index * 160, duration: 1150, ease: 'out(5)' });
        window.anime.animate($reveal.find('.case-gold-slash').get(0), { translateX: ['-130%', '130%'], opacity: [0, 1, 0], duration: 920, ease: 'inOut(3)' });
        if (kind === 'knife') {
            window.anime.animate(baseSilhouette, { scale: [.9, 1.03, 1.08], opacity: [0, .84, .84, 0], duration: 1180, ease: 'inOut(3)' });
            window.anime.animate(silhouette, { scale: [.82, .96, 1.08, 1], rotate: [-7, -2, 1, 0], opacity: [0, 0, .88, .18], delay: 500, duration: 1320, ease: 'inOut(4)' });
        } else {
            window.anime.animate(silhouette, { scale: [.35, 1.08, 1], rotate: [-5, 1, 0], opacity: [0, .82, .18], duration: 920, ease: 'out(6)' });
        }
        window.anime.animate(image, { clipPath: ['inset(0 100% 0 0)', 'inset(0 0% 0 0)'], opacity: [0, 1], delay: kind === 'knife' ? 1200 : 560, duration: 760, ease: 'inOut(4)' });
        window.anime.animate($reveal.find('.case-gold-scan').get(0), { translateX: ['-65vw', '65vw'], opacity: [0, 1, 0], delay: 500, duration: 820, ease: 'inOut(3)' });
        window.anime.animate(content, { translateY: [18, 0], opacity: [0, 1], duration: 620, ease: 'out(4)' });

        rareRevealTimer = window.setTimeout(function () {
            revealIdentity();
            $reveal.addClass('is-ready');
            rareRevealTimer = window.setTimeout(closeReveal, 2600);
        }, kind === 'knife' ? 1980 : 1320);
    }

    function showRareSpecialSequence(results, onComplete, index) {
        const position = Number(index || 0);
        if (position >= results.length) {
            onComplete();
            return;
        }
        showGoldReveal(results[position], () => showRareSpecialSequence(results, onComplete, position + 1), false);
    }

    function addResultsToInventory(results, refreshDisplay) {
        results.forEach(result => {
            if (result.isAutoSold === true) return;
            const historyItem = {
                ...result.winner,
                openingId: result.openingId,
                caseKey: result.caseKey,
                openedUtc: new Date().toISOString()
            };
            sessionOpenings.push(historyItem);
            historyItems.set(String(result.openingId), historyItem);
            allHistoryItems.unshift(historyItem);
        });
        historyDirty = true;
        renderRecentDrops();
        if (refreshDisplay !== false) {
            renderSessionSummary();
            if (activeDestination === 'inventory') renderHistory(allHistoryItems);
        }
    }

    function queuePostOpeningRefresh() {
        window.clearTimeout(postOpeningRefreshTimer);

        // The opening controls are the priority. Refresh only once the player pauses; keeping
        // these requests tracked lets the next opening cancel stale DOM-heavy work.
        postOpeningRefreshTimer = window.setTimeout(function () {
            postOpeningRefreshTimer = null;
            renderSessionSummary();
            if (activeDestination === 'inventory' && historyDirty) renderHistory(allHistoryItems);
            const quietOptions = { showLoader: false };
            const requests = [
                loadProgress(quietOptions),
                loadAchievements(quietOptions),
                loadInventoryCapacity(quietOptions),
                loadStatistics(statisticsRequestedAfterOpening)
            ];
            postOpeningRefreshRequests = requests;
            $.when(...requests).always(function () {
                if (postOpeningRefreshRequests === requests) postOpeningRefreshRequests = [];
            });
        }, 1200);
    }

    function cancelPostOpeningRefresh() {
        window.clearTimeout(postOpeningRefreshTimer);
        postOpeningRefreshTimer = null;
        postOpeningRefreshRequests.forEach(request => {
            if (request && typeof request.abort === 'function') request.abort();
        });
        postOpeningRefreshRequests = [];
    }

    function completeOpening(results) {
        announceNewCollectionDrops(results);
        addResultsToInventory(results, false);
        opening = false;
        $('.case-bottom-nav-link').prop('disabled', false);
        renderOpenButton('ready');
        window.requestAnimationFrame(() => $open.prop('disabled', Number(caseData?.ownedQuantity || 0) < selectedOpenQuantity));
        queuePostOpeningRefresh();
        statisticsRequestedAfterOpening = results[0]?.caseKey || caseKey;
        $('#chooseCaseButton, #caseSelectorGrid input').prop('disabled', false);
        evaluateAutoBuy();
        evaluateTradeUpRecipes();

        window.clearTimeout(stockStateTimer);
        const completedCaseKey = caseKey;
        if (Number(caseData?.ownedQuantity || 0) < 1) {
            // The final pull remains visible long enough to register, then the Shop call-to-action
            // takes over the arena. The recent-drop rail keeps that result available afterwards.
            stockStateTimer = window.setTimeout(function () {
                stockStateTimer = null;
                if (caseKey !== completedCaseKey || Number(caseData?.ownedQuantity || 0) > 0) return;
                $result.addClass('d-none');
                renderCaseStockState();
            }, 1800);
        }
    }

    function renderFinishedOpening(result) {
        const winner = result.winner;
        if (!isGoldItem(winner)) playReveal(winner);
        $('#caseResultName').text(winner.name);
        $('#caseResultMeta').text([winner.rarityName, winner.phase, winner.wear, winner.isStatTrak ? 'StatTrak™' : '', resultMarketText(result)].filter(Boolean).join(' · '));
        $result.removeClass('case-rarity-mil-spec case-rarity-restricted case-rarity-classified case-rarity-covert case-rarity-rare-special')
            .addClass(rarityClass(winner))
            .removeClass('d-none');
        if (!isGoldItem(winner)) {
            runParticles(winner.rarityColor, 28);
            awardXp([result], resultImageOrigin(result));
        }
    }

    function finishOpening(result) {
        renderFinishedOpening(result);
        completeOpening([result]);
    }

    function multiResultCard(result, index, total) {
        const winner = result.winner;
        return $('<div>', { class: 'case-multi-result-column' }).append(
            $('<article>', { class: `case-multi-result ${rarityClass(winner)}` }).append(
                $('<span>', { class: 'case-multi-index', text: `${index + 1} / ${total}` }),
                $('<img>', { src: winner.imageUrl, alt: '', decoding: 'async', referrerpolicy: 'no-referrer' }),
                $('<span>', { class: 'case-multi-rarity', text: winner.rarityName }),
                $('<strong>', { text: winner.name }),
                $('<span>', { class: 'case-multi-meta', text: [winner.phase, winner.wear].filter(Boolean).join(' · ') }),
                resultMarketText(result) ? $('<span>', { class: 'case-market-result', text: resultMarketText(result) }) : null,
                statTrakBadge(winner)
            )
        );
    }

    function showMultiResults(results, rareRevealComplete) {
        const rareResults = results.filter(result => isGoldItem(result.winner));
        if (!rareRevealComplete && rareResults.length) {
            showRareSpecialSequence(rareResults, () => showMultiResults(results, true));
            return;
        }
        const $multiResults = $('#caseMultiResults')
            .empty()
            .attr('data-open-count', results.length)
            .removeClass('d-none');

        $('.case-machine').addClass('is-multi-results');
        const mobileMultiScroll = window.matchMedia('(max-width: 575.98px)').matches && results.length >= 3;
        $('#caseReelWindow')
            .addClass('has-multi-results')
            .toggleClass('has-scrollable-multi', mobileMultiScroll);
        $reel.removeClass('case-skip-reel case-multi-reel').empty().css('transform', 'translateX(0px)');
        $idle.addClass('d-none');
        $result.addClass('d-none');
        results.forEach((result, index) => $multiResults.append(multiResultCard(result, index, results.length)));
        $multiResults.attr({ tabindex: results.length >= 3 ? '0' : '-1', 'aria-label': `${results.length} case opening results` }).scrollLeft(0);
        $multiResults.off('.multiHint');
        const enableSwipeDismiss = () => $multiResults.one('scroll.multiHint', () => $('#caseReelWindow').removeClass('has-scrollable-multi'));
        const panMobileResults = () => {
            if (!mobileMultiScroll || reduced) { enableSwipeDismiss(); return; }
            const element = $multiResults.get(0);
            const distance = Math.max(0, element.scrollWidth - element.clientWidth);
            if (distance < 8) { $('#caseReelWindow').removeClass('has-scrollable-multi'); return; }
            // Give the player one fast, uninterrupted overview of the entire pull. Moving to
            // every card separately made the preview feel like a series of stalls, especially
            // with five results. Temporarily disabling snapping keeps this one compositor scroll.
            enableSwipeDismiss();
            $multiResults.addClass('is-auto-panning');
            window.setTimeout(() => {
                element.scrollTo({ left: distance, behavior: 'smooth' });
                window.setTimeout(() => $multiResults.removeClass('is-auto-panning'), 520);
            }, 120);
        };
        results.filter(result => isGoldItem(result.winner)).forEach(result => {
            runParticles(result.winner.rarityColor || '#e4ae39', 64);
        });

        const cards = $multiResults.children().get();
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const completeMultiOpening = function () {
            awardXp(results, (_result, index) => $multiResults.children().eq(index));
            completeOpening(results);
        };

        if (!window.anime?.animate || reduced) {
            completeMultiOpening();
            panMobileResults();
            return;
        }

        const multiplier = openSpeedMultiplier();
        window.requestAnimationFrame(function () {
            window.requestAnimationFrame(function () {
                window.anime.animate(cards, {
                    translateX: [32, 0],
                    scale: [.985, 1],
                    delay: (_, index) => (index * 35) / multiplier,
                    duration: 240 / multiplier,
                    ease: 'out(4)',
                    onComplete: function () { completeMultiOpening(); panMobileResults(); }
                });
            });
        });
    }

    function showSkippedResult(result) {
        const winner = result.winner;
        $('.case-machine').removeClass('is-multi-results');
        $('#caseReelWindow').removeClass('has-multi-results has-scrollable-multi');
        $('#caseMultiResults').addClass('d-none').empty().removeAttr('data-open-count');
        const $skipCard = $('<article>', { class: `case-skip-result ${rarityClass(winner)}` }).append(
            $('<img>', {
                src: winner.imageUrl,
                alt: '',
                referrerpolicy: 'no-referrer'
            }),
            $('<div>', { class: 'case-skip-result-copy' }).append(
                $('<span>', { text: winner.rarityName }),
                $('<strong>', { text: winner.name }),
                resultMarketText(result) ? $('<small>', { class: 'case-market-result', text: resultMarketText(result) }) : null,
                statTrakBadge(winner)
            )
        );

        // Skipping the long reel still needs to show the item that was actually won.
        // Keeping this inside the reel window makes the quick path feel like an intentional reveal.
        $reel
            .addClass('case-skip-reel')
            .removeClass('case-multi-reel')
            .empty()
            .css('transform', 'translateX(0px)')
            .append($skipCard);
        $idle.addClass('d-none');
        $result.addClass('d-none');
        window.setTimeout(function () {
            if (isGoldItem(winner)) {
                showGoldReveal(result);
                return;
            }

            renderFinishedOpening(result);
            const revealTargets = [$skipCard.get(0), $result.get(0)];
            if (!window.anime?.animate || window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                completeOpening([result]);
                return;
            }

            window.anime.animate(revealTargets, {
                opacity: [0, 1],
                translateY: [8, 0],
                duration: 260,
                ease: 'out(4)',
                onComplete: function () {
                    $(revealTargets).css({ opacity: '', transform: '' });
                    completeOpening([result]);
                }
            });
        }, 120);
    }

    function runParticles(colour, count) {
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        const canvas = document.getElementById('caseEffectsCanvas');
        const context = canvas.getContext('2d');
        const bounds = canvas.getBoundingClientRect();
        canvas.width = Math.max(1, Math.floor(bounds.width));
        canvas.height = Math.max(1, Math.floor(bounds.height));
        const particles = Array.from({ length: count }, () => ({
            x: canvas.width / 2,
            y: canvas.height * .52,
            vx: (Math.random() - .5) * 9,
            vy: -Math.random() * 7 - 2,
            life: 50 + Math.random() * 35,
            size: 2 + Math.random() * 4
        }));
        function frame() {
            context.clearRect(0, 0, canvas.width, canvas.height);
            context.fillStyle = colour;
            particles.forEach(particle => {
                particle.x += particle.vx;
                particle.y += particle.vy;
                particle.vy += .14;
                particle.life -= 1;
                context.globalAlpha = Math.max(0, particle.life / 85);
                context.fillRect(particle.x, particle.y, particle.size, particle.size);
            });
            context.globalAlpha = 1;
            if (particles.some(particle => particle.life > 0)) requestAnimationFrame(frame);
            else context.clearRect(0, 0, canvas.width, canvas.height);
        }
        requestAnimationFrame(frame);
    }

    function displayValue(value, fallback) {
        return value === null || value === undefined || value === '' ? (fallback || '—') : String(value);
    }

    function floatText(value) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed.toFixed(6) : 'Not applicable';
    }

    function setInspectAngle(x, y) {
        inspectX = Math.max(-16, Math.min(16, x));
        inspectY = Math.max(-28, Math.min(28, y));
        const stage = document.getElementById('caseInspectStage');
        stage.style.setProperty('--inspect-x', `${inspectX}deg`);
        stage.style.setProperty('--inspect-y', `${inspectY}deg`);
    }

    function resetInspectAngle() {
        setInspectAngle(0, 0);
    }

    function openInspect(item) {
        const $stage = $('#caseInspectStage');
        $stage.removeClass('case-rarity-mil-spec case-rarity-restricted case-rarity-classified case-rarity-covert case-rarity-rare-special case-rarity-high-grade case-rarity-remarkable case-rarity-exotic')
            .addClass(rarityClass(item));
        $('#caseInspectRarity').text([item.rarityName, item.isStatTrak ? 'StatTrak™' : ''].filter(Boolean).join(' · '));
        $('#caseInspectTitle').text(item.name);
        $('#caseInspectImage').attr({ src: item.imageUrl, alt: item.name });
        $('#caseInspectDescription').text(item.description || 'No finish description is available for this item.');
        $('#caseInspectWeapon').text(displayValue(item.weaponName, item.isRareSpecial ? 'Rare special item' : 'Sticker'));
        $('#caseInspectPattern').text(displayValue(item.patternName, item.name));
        $('#caseInspectPhase').text(displayValue(item.phase, 'Not applicable'));
        $('#caseInspectWear').text(displayValue(item.wear, 'Not applicable'));
        $('#caseInspectFloat').text(floatText(item.floatValue));
        $('#caseInspectFloatRange').text(item.minFloat === null || item.minFloat === undefined
            ? 'Not applicable'
            : `${floatText(item.minFloat)} – ${floatText(item.maxFloat)}`);
        $('#caseInspectPaintIndex').text(displayValue(item.paintIndex, 'Not applicable'));
        $('#caseInspectPatternSeed').text(displayValue(item.patternSeed, 'Not applicable'));
        $('#caseInspectStatTrak').text(item.isStatTrak ? 'Yes' : item.supportsStatTrak ? 'No' : 'Not available');
        $('#caseInspectSource').text(caseNameFor(item.caseKey));
        resetInspectAngle();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('caseInspectModal')).show();
    }

    function saveDestinationPreference(destination) {
        try {
            localStorage.setItem(destinationStorageKey, destination);
            window.history.replaceState(null, '', `${window.location.pathname}${window.location.search}#${destination}`);
        } catch {
            // Navigation still works when browser history or storage is unavailable.
        }
    }

    function positionDestinationIndicator(animate) {
        const button = document.querySelector(`.case-bottom-nav-link[data-case-destination="${activeDestination}"]`);
        const indicator = document.getElementById('caseBottomNavIndicator');
        if (!button || !indicator) return;

        const values = {
            translateX: button.offsetLeft,
            width: button.offsetWidth
        };
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (!animate || reducedMotion || !window.anime?.animate) {
            indicator.style.width = `${values.width}px`;
            indicator.style.transform = `translateX(${values.translateX}px)`;
            return;
        }

        window.anime.animate(indicator, {
            translateX: values.translateX,
            width: values.width,
            duration: 320,
            ease: 'out(4)'
        });
    }

    function setDestinationLoading(destination, loading) {
        const selector = `[data-case-destination-loader="${destination}"]`;
        $(selector).remove();
        $(`[data-case-destination-panel="${destination}"]`).attr('aria-busy', loading ? 'true' : 'false');
        if (!loading) return;

        const $loader = $('<div>', {
            class: 'case-destination-loader case-tab-start',
            'data-case-destination-loader': destination,
            'data-case-destination-panel': destination,
            'aria-label': `Loading ${destination}`,
            role: 'status'
        });
        const $case = $('<span>', { class: 'case-destination-loader-case', 'aria-hidden': 'true' })
            .append($('<i>'), $('<b>'));
        $loader.append($case, $('<span>', { class: 'case-destination-loader-copy', text: 'Preparing your next operation…' }))
            .insertBefore($(`[data-case-destination-panel="${destination}"]`).first());
    }

    // Each destination asks for only the data it owns. Once loaded, its mounted DOM keeps the
    // user's filters and selections intact when they move around the simulator.
    function loadDestinationData(destination, options) {
        const settings = options || {};
        const requests = [];
        const quietOptions = { showLoader: false };
        const supportsLiveRefresh = destination === 'shop' || destination === 'upgrades';
        const lastRefresh = Number(destinationRefreshedAt.get(destination) || 0);
        const refreshLiveData = supportsLiveRefresh
            && (settings.force === true || Date.now() - lastRefresh >= destinationFreshForMs);

        if (!catalogueLoaded || (destination === 'shop' && refreshLiveData)) requests.push(loadCaseCatalogue(quietOptions));
        if (!progressLoaded || refreshLiveData) requests.push(loadProgress(quietOptions));

        if (destination === 'open') {
            if (loadedCaseKey !== caseKey) requests.push(loadCase(caseKey, quietOptions));
            if (!achievementsLoaded) requests.push(loadAchievements(quietOptions));
            if (!inventoryCapacityLoaded) requests.push(loadInventoryCapacity(quietOptions));
            if (!botProgressLoaded) requests.push(loadBotProgress(quietOptions));
        }

        if (destination === 'shop') {
            if (!inventoryCapacityLoaded || refreshLiveData) requests.push(loadInventoryCapacity(quietOptions));
        }

        if (destination === 'upgrades') {
            if (!inventoryCapacityLoaded || refreshLiveData) requests.push(loadInventoryCapacity(quietOptions));
            if (!botProgressLoaded || refreshLiveData) requests.push(loadBotProgress(quietOptions));
            if (!inventoryUpgradesLoaded || refreshLiveData) requests.push(loadInventoryUpgrades(quietOptions));
            if (!autoBuyRulesLoaded || refreshLiveData) requests.push(loadAutoBuyRules(quietOptions));
        }

        if (destination === 'inventory') {
            if (!historyLoaded) requests.push(loadHistory(quietOptions));
            else if (historyDirty) {
                const historyRender = $.Deferred();
                requests.push(historyRender.promise());
                window.requestAnimationFrame(function () {
                    renderHistory(allHistoryItems);
                    historyRender.resolve();
                });
            }
            if (!inventoryCapacityLoaded) requests.push(loadInventoryCapacity(quietOptions));
            if (!inventoryUpgradesLoaded) requests.push(loadInventoryUpgrades(quietOptions));
        }

        if (destination === 'tradeups') {
            if (!historyLoaded) requests.push(loadHistory(quietOptions));
            if (!inventoryCapacityLoaded) requests.push(loadInventoryCapacity(quietOptions));
            if (!inventoryUpgradesLoaded) requests.push(loadInventoryUpgrades(quietOptions));
            if (!tradeUpRecipesLoaded) requests.push(loadTradeUpRecipes(quietOptions));
        }

        if (requests.length === 0) return $.Deferred().resolve().promise();

        setDestinationLoading(destination, true);
        return $.when.apply($, requests)
            .done(function () {
                if (supportsLiveRefresh) destinationRefreshedAt.set(destination, Date.now());
            })
            .always(function () {
                setDestinationLoading(destination, false);
            });
    }

    function refreshDestinationData(destination, button) {
        const $button = button ? $(button) : $(`[data-refresh-destination="${destination}"]`);
        $button.prop('disabled', true).addClass('is-refreshing');
        return loadDestinationData(destination, { force: true }).always(function () {
            $button.prop('disabled', false).removeClass('is-refreshing');
        });
    }

    function switchDestination(destination, options) {
        if (!validDestinations.includes(destination)) return;
        const settings = options || {};
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (settings.initial || settings.skipTransition || reducedMotion || destination === activeDestination) {
            applyDestination(destination, settings);
            return;
        }

        const transition = document.getElementById('caseDestinationTransition');
        if (!transition) {
            applyDestination(destination, settings);
            return;
        }

        // Start the destination's scoped AJAX work as soon as the transition begins. The panel
        // remains hidden behind the cover until its void has expanded, so the animation masks the
        // network wait instead of starting it after the visual hand-off.
        $('.case-bottom-nav-link').prop('disabled', true);
        transition.classList.add('is-active');
        const destinationLoad = settings.deferLoad ? $.Deferred().resolve().promise() : loadDestinationData(destination);
        window.requestAnimationFrame(() => transition.classList.add('is-closing'));
        window.setTimeout(function () {
            applyDestination(destination, { ...settings, animate: false, deferLoad: true });
        }, 245);
        destinationLoad.always(function () {
            if (destination === 'tradeups') renderTradeUpWorkspace();
        });
        window.setTimeout(function () {
            transition.classList.remove('is-active', 'is-closing');
            $('.case-bottom-nav-link').prop('disabled', false);
        }, 650);
    }

    function applyDestination(destination, options) {
        if (!validDestinations.includes(destination)) return;
        const settings = options || {};
        activeDestination = destination;

        $('[data-case-destination-panel]').each(function () {
            const visible = String($(this).data('case-destination-panel')) === destination;
            $(this).toggleClass('d-none', !visible).attr('aria-hidden', visible ? 'false' : 'true');
        });
        $('.case-bottom-nav-link').each(function () {
            const active = String($(this).data('case-destination')) === destination;
            $(this).toggleClass('active', active).attr('aria-selected', active ? 'true' : 'false');
        });

        saveDestinationPreference(destination);
        positionDestinationIndicator(settings.animate !== false);
        $('#caseDestinationStatus').text(`${destination.charAt(0).toUpperCase()}${destination.slice(1)} section selected.`);

        const panels = $(`[data-case-destination-panel="${destination}"]`).get();
        const animatedPanels = destination === 'open'
            ? panels.filter(panel => !panel.classList.contains('case-stage')).slice(0, 1)
            : panels;
        if (settings.animate !== false
            && !window.matchMedia('(prefers-reduced-motion: reduce)').matches
            && window.anime?.animate) {
            window.anime.animate(animatedPanels, {
                opacity: [0, 1],
                translateY: [10, 0],
                delay: (_, index) => index * 28,
                duration: 330,
                ease: 'out(4)',
                onComplete: function () {
                    $(animatedPanels).css({ opacity: '', transform: '' });
                }
            });
        }

        if (!settings.initial) {
            const pageTop = $page.offset()?.top || 0;
            window.scrollTo({
                top: Math.max(0, pageTop - 16),
                behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth'
            });
        }

        if (!settings.deferLoad) {
            loadDestinationData(destination).always(function () {
                if (destination === 'tradeups') renderTradeUpWorkspace();
            });
        }
    }

    $('.case-bottom-nav-link').on('click', function () {
        switchDestination(String($(this).data('case-destination')));
    }).on('keydown', function (event) {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault();
        const buttons = $('.case-bottom-nav-link').get();
        const currentIndex = buttons.indexOf(this);
        const nextIndex = event.key === 'Home'
            ? 0
            : event.key === 'End'
                ? buttons.length - 1
                : (currentIndex + (event.key === 'ArrowRight' ? 1 : -1) + buttons.length) % buttons.length;
        buttons[nextIndex].focus();
        buttons[nextIndex].click();
    });

    $('[data-refresh-destination]').on('click', function () {
        refreshDestinationData(String($(this).data('refresh-destination')), this);
    });

    $('[data-case-profile-tab]').on('click', function () {
        const target = String($(this).data('case-profile-tab') || '');
        const tabButton = document.querySelector(`#caseProfileTabs [data-bs-target="${target}"]`);
        if (tabButton) bootstrap.Tab.getOrCreateInstance(tabButton).show();
    });

    $('#casePlayerProfile')
        .on('show.bs.offcanvas', function () {
            renderPlayerProfile();
            if ($('#caseProfileCollectionsPane').hasClass('active')) loadProfileCollections();
            if ($('#caseProfileBattlesPane').hasClass('active')) loadProfileBattleHistory();
            $('#caseBottomNav').addClass('is-obscured');
        })
        .on('hidden.bs.offcanvas', function () {
            if (!$('.modal.show').length) $('#caseBottomNav').removeClass('is-obscured');
        });

    function loadProfileBattleHistory() {
        const $history = $('#caseProfileBattleHistory');
        const formatBattleValue = value => new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 2 }).format(Number(value || 0));
        $history.html('<div class="case-profile-collections-empty"><i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i><span>Loading battle history…</span></div>');
        request('/api/case-battles/history', 'GET', { showLoader: false }).done(function (battles) {
            const items = Array.isArray(battles) ? battles : [];
            if (!items.length) { $history.html('<div class="case-profile-collections-empty"><i class="fa-solid fa-trophy" aria-hidden="true"></i><span>No completed case battles yet.</span></div>'); return; }
            $history.empty();
            items.forEach(function (battle) {
                const value = formatBattleValue(battle.awardedValue || 0);
                $history.append($('<a>', { class: `case-profile-collection-card${battle.won ? ' is-complete' : ''}`, href: '/CaseOpening/Battles/' + encodeURIComponent(battle.battleId) }).append(
                    $('<div>', { class: 'case-profile-collection-body' }).append(
                        $('<div>', { class: 'case-profile-collection-heading' }).append($('<strong>', { text: battle.mode || 'Case battle' }), $('<span>', { text: battle.won ? 'Won' : 'Lost' })),
                        $('<p>', { class: 'small-muted mb-0', text: `Pull total ${formatBattleValue(battle.personalTotal || 0)} · Awarded ${value}` })
                    )
                ));
            });
        }).fail(function () { $history.html('<div class="case-profile-collections-empty is-error"><span>Battle history could not be loaded.</span></div>'); });
    }

    $('#caseProfileBattlesTab').on('shown.bs.tab', loadProfileBattleHistory);

    const profileScrollBody = document.querySelector('#casePlayerProfile .offcanvas-body');
    let profileTouchY = null;
    profileScrollBody?.addEventListener('touchstart', function (event) {
        profileTouchY = event.touches[0]?.clientY ?? null;
    }, { passive: true });
    profileScrollBody?.addEventListener('touchmove', function (event) {
        const currentY = event.touches[0]?.clientY;
        if (profileTouchY === null || currentY === undefined) return;

        const movingDown = currentY > profileTouchY;
        const maximumScroll = Math.max(0, profileScrollBody.scrollHeight - profileScrollBody.clientHeight);
        const atTop = profileScrollBody.scrollTop <= 0;
        const atBottom = profileScrollBody.scrollTop >= maximumScroll - 1;
        if (maximumScroll === 0 || (movingDown && atTop) || (!movingDown && atBottom)) {
            event.preventDefault();
        }
        profileTouchY = currentY;
    }, { passive: false });
    profileScrollBody?.addEventListener('touchend', function () {
        profileTouchY = null;
    }, { passive: true });
    profileScrollBody?.addEventListener('touchcancel', function () {
        profileTouchY = null;
    }, { passive: true });

    $('#caseProfileTabs [data-bs-toggle="pill"]').on('shown.bs.tab', function () {
        document.querySelector('#casePlayerProfile .offcanvas-body')?.scrollTo({ top: 0, behavior: 'auto' });
        if (this.id === 'caseProfileCollectionsTab') loadProfileCollections();
        if (this.id === 'caseProfileStatisticsTab') refreshProfileStatistics();
    });

    function refreshProfileStatistics(button) {
        const $button = button ? $(button) : $('#caseProfileRefreshStatistics');
        $button.prop('disabled', true).addClass('is-refreshing');
        return $.when(
            loadAchievements({ showLoader: false }),
            loadProgress({ showLoader: false }),
            loadStatistics(caseKey)
        ).always(function () {
            $button.prop('disabled', false).removeClass('is-refreshing');
        });
    }

    $('#caseProfileRefreshStatistics').on('click', function () {
        refreshProfileStatistics(this);
    });

    $('#visitCaseShopButton').on('click', function () {
        shopSearch = '';
        shopTier = '';
        shopType = '';
        $('#caseShopSearch').val('');
        $('#caseShopTier').val('');
        switchDestination('shop');
        renderShop(catalogue);
    });

    $(document).on('show.bs.modal', '.modal', function () {
        $('#caseBottomNav').addClass('is-obscured');
    }).on('hidden.bs.modal', '.modal', function () {
        if (!$('.modal.show').length) $('#caseBottomNav').removeClass('is-obscured');
    });
    $(document).on('click.caseOddsPopover', function (event) {
        const oddsButton = document.getElementById('caseOddsButton');
        if (!oddsButton || oddsButton.contains(event.target) || event.target.closest('.popover')) return;
        bootstrap.Popover.getInstance(oddsButton)?.hide();
    });
    $(window).on('resize.caseBottomNav', () => positionDestinationIndicator(false));

    $open.on('click', function () {
        if (opening || !caseData) return;
        cancelPostOpeningRefresh();
        // Restore the normal opening stage before asking the server for the next roll.
        $('.case-machine').removeClass('is-multi-results');
        $('#caseReelWindow').removeClass('has-multi-results has-scrollable-multi');
        $('#caseMultiResults').addClass('d-none').empty().removeAttr('data-open-count');
        playOpeningStart();
        opening = true;
        $('.case-bottom-nav-link').prop('disabled', true);
        $open.prop('disabled', true);
        renderOpenButton('requesting');
        $('#chooseCaseButton, #caseSelectorGrid input').prop('disabled', true);
        request(
            `/api/case-opening/cases/${encodeURIComponent(caseKey)}/open`,
            'POST',
            {
                data: JSON.stringify({ quantity: selectedOpenQuantity }),
                contentType: 'application/json; charset=utf-8',
                showLoader: false
            })
            .done(function (batch) {
                const results = Array.isArray(batch?.results) ? batch.results : [];
                if (results.length !== selectedOpenQuantity) {
                    opening = false;
                    $('.case-bottom-nav-link').prop('disabled', false);
                    $open.prop('disabled', false);
                    renderOpenButton('ready');
                    $('#chooseCaseButton, #caseSelectorGrid input').prop('disabled', false);
                    showError(null, 'The case-opening result was incomplete. Please try again.');
                    return;
                }
                results.forEach(result => warmCaseImage(result?.winner?.imageUrl));
                const remainingQuantity = Number(batch?.remainingCaseQuantity);
                if (Number.isFinite(remainingQuantity) && caseData) {
                    const previousQuantity = Number(caseData.ownedQuantity || 0);
                    caseData.ownedQuantity = remainingQuantity;
                    const catalogueCase = catalogue.find(item => item.caseKey === caseKey);
                    if (catalogueCase) catalogueCase.ownedQuantity = remainingQuantity;
                    renderOwnedCaseQuantity({ from: previousQuantity, animate: true });
                    renderOpenQuantity();
                    renderOwnedCasesInventory();
                }
                if (results.length > 1) {
                    showMultiResults(results);
                } else if (caseProgress?.skipAnimationUnlocked && $('#caseSkipAnimation').prop('checked')) {
                    showSkippedResult(results[0]);
                } else {
                    animateReel(results[0]);
                }
            })
            .fail(response => {
                clearReelSounds();
                opening = false;
                $('.case-bottom-nav-link').prop('disabled', false);
                $open.prop('disabled', false);
                renderOpenButton('ready');
                $('#chooseCaseButton, #caseSelectorGrid input').prop('disabled', false);
                showError(response, 'The case could not be opened. Please try again.');
            });
    });

    $('#caseOpenQuantity').on('click', '[data-open-quantity]', function () {
        if (opening) return;
        const quantity = Number($(this).data('open-quantity')) || 1;
        if (quantity > 1 + Number(caseProgress?.multiOpenLevel || 0)) return;
        selectedOpenQuantity = quantity;
        renderOpenQuantity();
    });

    $('#caseSkipAnimation').on('change', function () {
        if (caseProgress?.skipAnimationUnlocked !== true) {
            this.checked = false;
            return;
        }
        saveSkipAnimationPreference(this.checked);
        markSwitchSaved(this);
        markSwitchSaved($('.js-shop-skip-toggle'));
        window.personalToolsToast?.info(this.checked ? 'Long reel animation will be skipped.' : 'Long reel animation restored.');
    });

    // Excludes .js-unlock-inventory-upgrade buttons (auto-buy, inventory-slots, ...) - those also
    // carry a data-upgrade-key attribute but are handled by their own more specific delegated
    // handler below, which would otherwise double-fire alongside this one on the same click.
    $(document).on('click', '[data-upgrade-key]:not(.js-unlock-inventory-upgrade)', function () {
        const upgradeKey = String($(this).data('upgrade-key'));
        if (!upgradeKey) return;
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/upgrades/${encodeURIComponent(upgradeKey)}/unlock`, 'POST', { showLoader: false })
            .done(function (progress) {
                renderProgress(progress);
                loadAchievements();
                if ($('#caseShopCaseGrid').children().length) renderShop(catalogue);
                if (inventoryUpgradesLoaded) renderAutoBuyPanel();
                refreshDestinationData('upgrades');
                window.personalToolsToast?.success('Case-opening upgrade unlocked.');
            })
            .fail(response => showError(response, 'The upgrade could not be unlocked.'))
            .always(() => $button.prop('disabled', false));
    });

    $(document).on('change', '.js-shop-skip-toggle', function () {
        saveSkipAnimationPreference(this.checked);
        $('#caseSkipAnimation').prop('checked', this.checked);
        markSwitchSaved(this);
        markSwitchSaved($('#caseSkipAnimation'));
        window.personalToolsToast?.info(this.checked ? 'Quick open enabled.' : 'Long reel animation restored.');
    });

    // The switch remains the native form control, but the surrounding upgrade row is also a
    // comfortable touch target. Buttons and links are excluded so their own actions stay intact.
    $(document).on('click', '.case-shop-switch-row', function (event) {
        if ($(event.target).closest('input, label, button, a').length) return;
        const switchId = String($(this).data('switch-target') || '');
        const input = switchId ? document.getElementById(switchId) : null;
        if (!input || input.disabled) return;
        input.checked = !input.checked;
        $(input).trigger('change').trigger('focus');
    });

    $(document).on('click', '.js-shop-buy-case', function () {
        const caseKeyToBuy = String($(this).data('case-key') || '');
        const quantity = Math.max(1, Math.min(500, Math.trunc(Number($(this).data('quantity')) || 1)));
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/cases/${encodeURIComponent(caseKeyToBuy)}/purchase`, 'POST', {
            data: JSON.stringify({ quantity: quantity }), contentType: 'application/json; charset=utf-8', showLoader: false
        }).done(function (result) {
            const item = catalogue.find(entry => entry.caseKey === caseKeyToBuy);
            if (item) item.ownedQuantity = result.ownedQuantity;
            if (caseKey === caseKeyToBuy && caseData) {
                const previousQuantity = Number(caseData.ownedQuantity || 0);
                caseData.ownedQuantity = Number(result.ownedQuantity || 0);
                renderOwnedCaseQuantity({ from: previousQuantity, animate: true });
                renderOpenQuantity();
            }
            renderOwnedCasesInventory();
            renderUpdatedBalance(result);
            loadInventoryCapacity({ showLoader: false });
            const purchasedQuantity = Number(result.purchasedQuantity || quantity);
            const caseLabel = caseNameFor(caseKeyToBuy);
            refreshDestinationData('shop');
            window.personalToolsToast?.success(`${purchasedQuantity.toLocaleString()} × ${caseLabel} added to your stock.`);
        }).fail(response => showError(response, 'The case could not be purchased.')).always(() => $button.prop('disabled', false));
    });

    $('#caseShopSearch').on('input', function () {
        window.clearTimeout(shopSearchTimer);
        shopSearchTimer = window.setTimeout(function () {
            shopSearch = String($('#caseShopSearch').val() || '');
            renderShop(catalogue);
        }, 120);
    });

    $('#caseShopTier').on('change', function () {
        shopTier = String(this.value || '');
        renderShop(catalogue);
    });

    $('#caseShopTypeFilter').on('click', '[data-shop-type]', function () {
        shopType = String($(this).data('shop-type') || '');
        renderShop(catalogue);
    });

    $(document).on('click', '.js-shop-unlock-case', function () {
        const caseKeyToUnlock = String($(this).data('case-key') || '');
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/cases/${encodeURIComponent(caseKeyToUnlock)}/unlock`, 'POST', { showLoader: false })
            .done(function (progress) {
                renderProgress(progress);
                loadAchievements();
                refreshDestinationData('shop');
                window.personalToolsToast?.success('Case permanently unlocked. You can now buy copies.');
            }).fail(response => showError(response, 'The case could not be unlocked.')).always(() => $button.prop('disabled', false));
    });

    $('#purchaseStorageContainer').on('click', function () {
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/storage-containers', 'POST', { showLoader: false })
            .done(function (result) {
                renderUpdatedBalance(result);
                refreshDestinationData('upgrades');
                window.personalToolsToast?.success(`Storage expanded by ${Number(result.addedSlots).toLocaleString()} slots.`);
            }).fail(response => showError(response, 'The storage container could not be purchased.')).always(() => $button.prop('disabled', false));
    });

    $('.js-shop-buy-server').on('click', () => $('#buyCaseBotServer').trigger('click'));
    $('.js-shop-buy-bot').on('click', () => $('#buyCaseBot').trigger('click'));

    $('#caseBotCaseSelect').on('change', function () {
        saveBotCasePreference(String(this.value || ''));
    });

    $('#buyCaseBotServer').on('click', function () {
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/bots/servers', 'POST', { showLoader: false })
            .done(function (progress) {
                renderBotProgress(progress);
                renderUpdatedBalance(progress);
                loadAchievements();
                refreshDestinationData('upgrades');
                window.personalToolsToast?.success('Bot server installed. It has four available slots.');
            })
            .fail(response => showError(response, 'The bot server could not be purchased.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#buyCaseBot').on('click', function () {
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/bots', 'POST', { showLoader: false })
            .done(function (progress) {
                renderBotProgress(progress);
                renderUpdatedBalance(progress);
                loadAchievements();
                refreshDestinationData('upgrades');
                window.personalToolsToast?.success('Opening bot installed and started.');
                startBots(false);
            })
            .fail(response => showError(response, 'The bot could not be purchased.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#startCaseBots').on('click', () => startBots(true));

    $('#stopCaseBots').on('click', () => stopBots(true));

    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            pauseBotsForHiddenTab();
            if (botsRunning) window.personalToolsToast?.info('Bot operation paused because the Case Opening tab is no longer visible.');
        } else {
            resumeBotsIfDue();
        }
        manageAutoBuyPolling();
        manageTradeUpRecipePolling();
    });

    window.addEventListener('pagehide', () => stopBots(false));

    $('#caseSoundEnabled').on('click', function () {
        soundState.enabled = !soundState.enabled;
        if (soundState.enabled) {
            ensureAudioContext();
            tone(392, 0.12, 'sine', 0.045);
        } else if (masterGain && audioContext) {
            masterGain.gain.setTargetAtTime(0, audioContext.currentTime, 0.015);
        }
        saveSoundState();
        renderSoundControls();
    });

    function setSoundVolume(percentage) {
        soundState.volume = Math.max(0, Math.min(1, Number(percentage) / 100));
        if (masterGain && audioContext) {
            masterGain.gain.setTargetAtTime(soundState.enabled ? soundState.volume : 0, audioContext.currentTime, 0.015);
        }
        saveSoundState();
        renderSoundControls();
    }

    $('#caseSoundVolumeMeter').on('pointerdown', function (event) {
        const meter = this;
        const update = function (pointerEvent) {
            const bounds = meter.getBoundingClientRect();
            setSoundVolume(Math.round(((pointerEvent.clientX - bounds.left) / Math.max(1, bounds.width)) * 100));
        };
        update(event.originalEvent);
        meter.setPointerCapture?.(event.originalEvent.pointerId);
        $(meter).on('pointermove.caseSoundVolume', function (moveEvent) { update(moveEvent.originalEvent); })
            .one('pointerup.caseSoundVolume pointercancel.caseSoundVolume', function () { $(meter).off('pointermove.caseSoundVolume'); });
    }).on('keydown', function (event) {
        const current = Math.round(soundState.volume * 100);
        const step = event.shiftKey ? 10 : 5;
        let next = current;
        if (event.key === 'ArrowLeft' || event.key === 'ArrowDown') next -= step;
        else if (event.key === 'ArrowRight' || event.key === 'ArrowUp') next += step;
        else if (event.key === 'Home') next = 0;
        else if (event.key === 'End') next = 100;
        else return;
        event.preventDefault();
        setSoundVolume(next);
    });

    $('#caseSoundThemes').on('click', '.case-sound-theme', function () {
        const theme = String($(this).data('sound-theme') || 'classic');
        if (!['classic', 'csgo', 'cs2'].includes(theme)) return;
        soundState.theme = theme;
        soundState.enabled = true;
        unlockAudioContext();
        saveSoundState();
        renderSoundControls();
        playOpeningStart();
    });

    $('#caseSoundPreview').on('click', function () {
        unlockAudioContext();
        playOpeningStart();
        window.setTimeout(() => playReveal({ rarityKey: 'classified' }), 180);
    });

    // Keep the catalogue manageable as more cases and capsules are added. This only filters
    // the already-loaded list, so searching does not add database or API traffic.
    $('#caseSelectorSearch').on('input', function () {
        renderCaseSelector();
    });

    $('#caseSelectorGrid').on('change', 'input[name="caseSelection"]', function () {
        if (opening) return;
        const selectedKey = String(this.value);
        if (selectedKey === caseKey) {
            bootstrap.Modal.getInstance(document.getElementById('caseSelectorModal'))?.hide();
            return;
        }

        caseKey = selectedKey;
        saveSelectedCaseKey(caseKey);
        loadCase(caseKey, { closeSelector: true, showToast: true });
    });

    $('#caseSelectorGrid').on('click keydown', '.case-selector-tile:not(.is-locked)', function (event) {
        if (event.type === 'keydown' && event.key !== 'Enter' && event.key !== ' ') return;
        if ($(event.target).closest('input,button').length) return;
        event.preventDefault();
        $(this).find('input[name="caseSelection"]').prop('checked', true).trigger('change');
    });

    $('#caseSelectorGrid').on('click', '.js-selector-buy-more', function (event) {
        event.preventDefault();
        event.stopPropagation();
        const selectedKey = String($(this).data('case-key') || '');
        const selectedCase = catalogue.find(item => item.caseKey === selectedKey);
        if (!selectedCase || !isCaseUnlocked(selectedCase)) return;

        const showSelectedCaseInShop = function () { openShopCase(selectedKey, true); };
        const selectorModal = document.getElementById('caseSelectorModal');
        const modal = bootstrap.Modal.getInstance(selectorModal);
        if (!modal) {
            showSelectedCaseInShop();
            return;
        }
        $(selectorModal).one('hidden.bs.modal', showSelectedCaseInShop);
        modal.hide();
    });

    $('.case-history-section').on('click', '.js-inspect-case-item', function () {
        const item = historyItems.get(String($(this).data('opening-id')));
        if (item) openInspect(item);
    });

    $('.case-history-section').on('click', '.js-case-inventory-lock', function () {
        const $button = $(this);
        const openingId = String($button.data('opening-id') || '');
        const item = historyItems.get(openingId);
        if (!item || $button.prop('disabled')) return;
        const requestedState = item.isLocked !== true;
        $('.js-case-inventory-lock').filter(function () { return String($(this).data('opening-id')) === openingId; })
            .prop('disabled', true)
            .addClass('is-saving');

        request(`/api/case-opening/inventory/${encodeURIComponent(openingId)}/lock`, 'PUT', {
            data: JSON.stringify({ isLocked: requestedState }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        }).done(function (result) {
            item.isLocked = result?.isLocked === true;
            if (item.isLocked) {
                selectedInventoryIds.delete(openingId);
                tradeUpSelectionIds.delete(openingId);
            }
            const $controls = $('.js-case-inventory-lock').filter(function () { return String($(this).data('opening-id')) === openingId; });
            $controls
                .prop('disabled', false)
                .removeClass('is-saving')
                .toggleClass('is-locked', item.isLocked)
                .attr('aria-pressed', item.isLocked ? 'true' : 'false')
                .attr('aria-label', item.isLocked ? `Unlock ${item.name}` : `Protect ${item.name}`)
                .attr('title', item.isLocked ? `Unlock ${item.name}` : `Protect ${item.name}`)
                .addClass('is-confirmed')
                .find('i')
                .attr('class', `fa-solid ${item.isLocked ? 'fa-lock' : 'fa-lock-open'}`);
            $(`.case-history-card[data-opening-id="${CSS.escape(openingId)}"], .case-history-row[data-opening-id="${CSS.escape(openingId)}"]`)
                .toggleClass('is-locked', item.isLocked);
            renderInventorySelection();
            window.setTimeout(() => $controls.removeClass('is-confirmed'), 420);
        }).fail(function (response) {
            $('.js-case-inventory-lock').filter(function () { return String($(this).data('opening-id')) === openingId; })
                .prop('disabled', false)
                .removeClass('is-saving');
            window.personalToolsToast?.error(response.responseJSON?.message || 'Item protection could not be updated.');
        });
    }).on('click keydown', '.case-history-card, .case-history-row', function (event) {
        if ($(event.target).closest('button, a, input, label, select, textarea, [role="button"]').length > 0) return;
        if (event.type === 'keydown' && !['Enter', ' '].includes(event.key)) return;
        if (event.type === 'keydown') event.preventDefault();
        const openingId = String($(this).data('opening-id') || '');
        setInventoryItemSelected(openingId, !selectedInventoryIds.has(openingId));
    });

    $('#caseSoundMenuButton, #openCaseButton').on('pointerdown touchstart', unlockAudioContext);

    $('#caseGoldReveal').on('click', function () {
        rareRevealDismiss?.();
    });

    $(document).on('keydown.caseGoldReveal', function (event) {
        if (event.key === 'Escape') rareRevealDismiss?.();
    });

    $('#caseInventorySelectAll').on('click', function () {
        const sellLimit = Number(inventoryUpgrades?.bulkSellLimit || 100);
        const selectableIds = filteredHistoryItems.filter(item => item.isLocked !== true).slice(0, sellLimit).map(item => String(item.openingId));
        const allSelected = selectableIds.length > 0 && selectableIds.every(openingId => selectedInventoryIds.has(openingId));
        if (!allSelected) {
            selectedInventoryIds.clear();
            selectableIds.forEach(openingId => selectedInventoryIds.add(openingId));
        } else {
            selectableIds.forEach(openingId => selectedInventoryIds.delete(openingId));
        }
        renderInventorySelection();
    });

    $('#caseTradeUpCandidates').on('click keydown', '.case-trade-up-candidate', function (event) {
        if (event.type === 'keydown' && !['Enter', ' '].includes(event.key)) return;
        if (event.type === 'keydown') event.preventDefault();
        const $card = $(this);
        if ($card.hasClass('is-ineligible')) return;
        const openingId = String($card.data('opening-id'));
        if (tradeUpSelectionIds.has(openingId)) {
            tradeUpSelectionIds.delete(openingId);
        } else if (tradeUpSelectionIds.size < 10) {
            tradeUpSelectionIds.add(openingId);
        } else {
            window.personalToolsToast?.info('A Trade Up Contract has ten input slots. Remove one to choose another.');
        }
        renderTradeUpWorkspace();
    });

    $('#caseTradeUpWorkspaceSlots').on('click', '.case-trade-up-workspace-slot:not(.is-empty)', function () {
        tradeUpSelectionIds.delete(String($(this).data('opening-id')));
        renderTradeUpWorkspace();
    });

    $('#clearTradeUpSelection').on('click', function () {
        tradeUpSelectionIds.clear();
        renderTradeUpWorkspace();
    });

    $(document).on('click', '.js-complete-trade-up', function () {
        if (tradeUpInFlight) return;
        const tradeUp = getTradeUpSelection(tradeUpSelectionIds);
        if (!tradeUp.valid) {
            window.personalToolsToast?.error(tradeUp.message);
            return;
        }
        const $buttons = $('.js-complete-trade-up').prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Completing contract…');
        tradeUpInFlight = true;
        $('.case-trade-up-mobile-action .js-complete-trade-up').removeClass('is-ready-attention');
        tradeUpCompletionAttentionActive = false;
        request('/api/case-opening/trade-ups', 'POST', {
            data: JSON.stringify({ openingIds: tradeUp.items.map(item => item.openingId) }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        }).done(function (result) {
            const consumed = new Set(tradeUp.items.map(item => String(item.openingId)));
            allHistoryItems = allHistoryItems.filter(item => !consumed.has(String(item.openingId)));
            sessionOpenings = sessionOpenings.filter(item => !consumed.has(String(item.openingId)));
            consumed.forEach(openingId => selectedInventoryIds.delete(openingId));
            allHistoryItems.unshift(result.output);
            tradeUpSelectionIds.clear();
            const output = result.output;
            renderTradeUpWorkspace();
            renderHistory(allHistoryItems);
            renderTradeUpResult(result);
            profileCollectionsLoaded = false;
            loadProgress();
            loadAchievements();
            loadInventoryCapacity();
            window.personalToolsToast?.success(`Contract complete: ${output.name} is now in your inventory.`);
        }).fail(response => showError(response, 'The Trade Up Contract could not be completed.')).always(function () {
            tradeUpInFlight = false;
            $buttons.prop('disabled', !getTradeUpSelection(tradeUpSelectionIds).valid)
                .html('<i class="fa-solid fa-flask-vial me-1"></i>Complete contract');
        });
    });


    $('#caseInventoryUpgradeGrid, #caseCapacityUpgradeGrid, #caseAutoBuyPanel, #caseTradeUpUpgradePanel').on('click', '.js-unlock-inventory-upgrade', function () {
        const upgradeKey = String($(this).data('upgrade-key') || '');
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/inventory/upgrades/${encodeURIComponent(upgradeKey)}/unlock`, 'POST', { showLoader: false })
            .done(function (result) {
                inventoryUpgrades = result;
                renderUpdatedBalance(result);
                renderInventoryUpgradeStore(); renderInventorySelection();
                refreshDestinationData('upgrades');
                if (tradeUpRecipesLoaded) loadTradeUpRecipes({ showLoader: false });
                window.personalToolsToast?.success('Inventory upgrade unlocked.');
            }).fail(response => showError(response, 'The inventory upgrade could not be unlocked.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseAutoBuyAddButton').on('click', addAutoBuyRule);

    $('#caseAutoBuyRules').on('click', '.js-auto-buy-remove', function () {
        removeAutoBuyRule($(this).closest('.case-auto-buy-row'));
    });

    $('#caseTradeUpLockedLink').on('click', function (event) {
        event.preventDefault();
        switchDestination('upgrades');
    });

    $('#caseTradeUpSlotsUpgradeButton').on('click', function () {
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/trade-up-recipes/slots/upgrade', 'POST', { showLoader: false })
            .done(function (result) {
                inventoryUpgrades = result;
                renderUpdatedBalance(result);
                renderInventoryUpgradeStore(); renderInventorySelection();
                refreshDestinationData('upgrades');
                if (tradeUpRecipesLoaded) loadTradeUpRecipes({ showLoader: false });
                window.personalToolsToast?.success('Auto trade-up recipe slot added.');
            })
            .fail(response => showError(response, 'This upgrade could not be purchased.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseTradeUpHoldingUpgradeButton').on('click', function () {
        if (!tradeUpRecipeModalRecipeId) return;
        const recipeId = tradeUpRecipeModalRecipeId;
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/trade-up-recipes/${encodeURIComponent(recipeId)}/holding/upgrade`, 'POST', { showLoader: false })
            .done(function (result) {
                tradeUpRecipeSummary = result;
                renderUpdatedBalance(result);
                renderTradeUpRecipesPanel();
                manageTradeUpRecipePolling();
                refreshDestinationData('upgrades');
                const recipe = (result.recipes || []).find(item => String(item.recipeId) === String(recipeId));
                if (recipe) renderTradeUpModalHoldingCapacity(recipe);
                window.personalToolsToast?.success('Holding capacity added.');
            })
            .fail(response => showError(response, 'This upgrade could not be purchased.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseTradeUpRecipeGrid').on('click', '.case-trade-up-recipe-card', function () {
        if ($(this).is('.is-add')) {
            if (!$(this).is('.disabled')) openTradeUpCreateModal();
            return;
        }
        openTradeUpManageModal($(this).data('recipe-id'));
    }).on('keydown', '.case-trade-up-recipe-card', function (event) {
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        $(this).trigger('click');
    });

    $('#caseTradeUpRecipeCase').on('change', function () {
        const caseKey = String($(this).val() || '');
        $('#caseTradeUpRecipeItem').empty().prop('disabled', true);
        loadTradeUpRecipeCaseItems(caseKey);
        renderTradeUpRecipeSubmitButton();
    });

    $('#caseTradeUpRecipeItem, #caseTradeUpRecipeStatTrak').on('change', function () {
        if (this.id === 'caseTradeUpRecipeItem') syncTradeUpRecipeSelection();
        renderTradeUpRecipeSubmitButton();
    });

    $('#caseTradeUpRecipeModalSubmit').on('click', function () {
        if (tradeUpRecipeModalMode === 'create') createTradeUpRecipe();
    });

    $('#caseTradeUpRecipeModalDelete').on('click', function () {
        if (tradeUpRecipeModalRecipeId) deleteTradeUpRecipe(tradeUpRecipeModalRecipeId);
    });

    $('#caseTradeUpRecipeManageActive').on('change', function () {
        const $checkbox = $(this);
        if (!tradeUpRecipeModalRecipeId) return;
        const isActive = $checkbox.prop('checked');
        setTradeUpRecipeActive(tradeUpRecipeModalRecipeId, isActive).fail(() => $checkbox.prop('checked', !isActive));
    });

    $('#caseTradeUpHoldingGrid').on('click', '.js-trade-up-holding-collect', function () {
        collectTradeUpHolding($(this).closest('.case-trade-up-holding-row'));
    });

    $('#caseAutoSellControls').on('change', '.js-auto-sell-toggle', function () {
        const $input = $(this);
        const rarityKey = String($input.data('rarity-key') || '');
        $input.prop('disabled', true);
        request('/api/case-opening/inventory/auto-sell', 'PUT', {
            data: JSON.stringify({ rarityKey: rarityKey, enabled: $input.prop('checked'), preserveStatTrak: $('#casePreserveStatTrak').prop('checked') }),
            contentType: 'application/json; charset=utf-8', showLoader: false
        }).done(function (result) { inventoryUpgrades = result; renderInventoryUpgradeStore(); markSwitchSaved($(`#caseAutoSell-${rarityKey}`)); window.personalToolsToast?.success('Auto-sell preference saved.'); })
            .fail(function (response) { renderInventoryUpgradeStore(); showError(response, 'The auto-sell preference could not be saved.'); });
    });

    $('#caseAutoBuyRules').on('change', '.js-auto-buy-toggle, .js-auto-buy-threshold, .js-auto-buy-quantity', function () {
        saveAutoBuyRule($(this).closest('.case-auto-buy-row'));
    });

    $('#caseBotServers').on('click', '.js-upgrade-bot-speed', function () {
        const botId = String($(this).data('bot-id') || '');
        const $buttons = $('#caseBotServers .js-upgrade-bot-speed').prop('disabled', true);
        request(`/api/case-opening/bots/${encodeURIComponent(botId)}/speed`, 'POST', { showLoader: false })
            .done(function (result) {
                renderBotProgress(result);
                renderUpdatedBalance(result);
                window.personalToolsToast?.success('Bot speed upgraded by 0.1×.');
            }).fail(function (response) {
                renderBotProgress(botProgress);
                showError(response, 'The bot speed could not be upgraded.');
            }).always(() => $buttons.prop('disabled', false));
    });

    $('#caseBotServers').on('change', '.js-toggle-bot-server', function () {
        const serverId = String($(this).data('server-id') || '');
        const enabled = this.checked;
        const $switch = $(this).prop('disabled', true);
        request(`/api/case-opening/bots/servers/${encodeURIComponent(serverId)}/enabled`, 'PUT', {
            data: JSON.stringify({ isEnabled: enabled }), contentType: 'application/json; charset=utf-8', showLoader: false
        }).done(function (result) {
            renderBotProgress(result);
            markSwitchSaved($switch);
        }).fail(function (response) {
            renderBotProgress(botProgress);
            showError(response, 'The bot rack state could not be saved.');
        });
    });

    $('#casePreserveStatTrak').on('change', function () {
        const $input = $(this).prop('disabled', true);
        const enabledRarity = $('#caseAutoSellControls .js-auto-sell-toggle:checked').first().data('rarity-key') || 'covert';
        request('/api/case-opening/inventory/auto-sell', 'PUT', {
            data: JSON.stringify({ rarityKey: enabledRarity, enabled: $(`#caseAutoSell-${enabledRarity}`).prop('checked'), preserveStatTrak: this.checked }),
            contentType: 'application/json; charset=utf-8', showLoader: false
        }).done(function (result) { inventoryUpgrades = result; renderInventoryUpgradeStore(); markSwitchSaved($('#casePreserveStatTrak')); window.personalToolsToast?.success('StatTrak™ protection saved.'); })
            .fail(function (response) { renderInventoryUpgradeStore(); showError(response, 'StatTrak™ protection could not be saved.'); })
            .always(() => $input.prop('disabled', false));
    });

    $('#caseTradeUpModal').on('click', '.js-view-trade-up-inventory', function (event) {
        event.stopImmediatePropagation();
        const openingId = String($(this).data('opening-id') || '');
        const item = historyItems.get(openingId);
        const tradeUpModalElement = document.getElementById('caseTradeUpModal');
        const tradeUpModal = bootstrap.Modal.getOrCreateInstance(tradeUpModalElement);

        // Wait until Bootstrap has removed the first backdrop before opening the inspector.
        // Opening two modals during the same hide transition leaves the page scroll-locked.
        $(tradeUpModalElement).one('hidden.bs.modal', function () {
            switchDestination('inventory');
            setInventoryKind('skins', { skipMotion: true });
            $('#caseHistorySearch').val('');
            $('#caseHistoryRarity').val('');
            renderInventorySummary();
            filterHistory();

            if (item) {
                window.requestAnimationFrame(function () {
                    openInspect(item);
                });
            }
        });
        tradeUpModal.hide();
    }).on('keydown', '.js-view-trade-up-inventory', function (event) {
        if (event.key !== 'Enter' && event.key !== ' ') return;
        event.preventDefault();
        $(this).trigger('click');
    }).on('click', function () {
        bootstrap.Modal.getInstance(this)?.hide();
    });

    $('#sellCaseInventory').on('click', function () {
        const openingIds = [...selectedInventoryIds];
        if (openingIds.length === 0) return;
        const saleAmount = allHistoryItems.filter(item => selectedInventoryIds.has(String(item.openingId))).reduce((total, item) => total + saleAmountFor(item), 0);
        $('#caseSellConfirmCopy').text(`Sell ${openingIds.length} selected item${openingIds.length === 1 ? '' : 's'} for approximately ${formatCurrency(saleAmount)}?`);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('caseSellConfirmModal')).show();
    });

    $('#confirmCaseInventorySale').on('click', function () {
        const openingIds = [...selectedInventoryIds];
        if (openingIds.length === 0) return;
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/inventory/sell', 'POST', {
            data: JSON.stringify({ openingIds: openingIds }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (result) {
                bootstrap.Modal.getInstance(document.getElementById('caseSellConfirmModal'))?.hide();
                const soldIds = new Set(openingIds);
                allHistoryItems = allHistoryItems.filter(item => !soldIds.has(String(item.openingId)));
                sessionOpenings = sessionOpenings.filter(item => !soldIds.has(String(item.openingId)));
                openingIds.forEach(openingId => selectedInventoryIds.delete(openingId));
                renderUpdatedBalance(result);
                // Keep the inventory response immediate after a sale. The list only loses the
                // selected cards, so it does not need the normal reveal animation again.
                renderHistory(allHistoryItems, { skipMotion: true, preservePage: true });
                loadInventoryCapacity();
                window.personalToolsToast?.success(`${result.soldItemCount} item${result.soldItemCount === 1 ? '' : 's'} sold for ${formatCurrency(Number(result.amountAwardedMinor ?? result.starsAwarded ?? 0))}.`);
            })
            .fail(response => showError(response, 'The selected inventory items could not be sold.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseInspectStage').on('pointerdown', function (event) {
        if ($(event.target).closest('button').length) return;
        inspectPointer = { id: event.originalEvent.pointerId, x: event.clientX, y: event.clientY, startX: inspectX, startY: inspectY };
        this.setPointerCapture?.(inspectPointer.id);
        $(this).addClass('is-dragging');
    }).on('pointermove', function (event) {
        if (!inspectPointer || event.originalEvent.pointerId !== inspectPointer.id) return;
        setInspectAngle(inspectPointer.startY - ((event.clientY - inspectPointer.y) * .12), inspectPointer.startX + ((event.clientX - inspectPointer.x) * .16));
    }).on('pointerup pointercancel lostpointercapture', function () {
        inspectPointer = null;
        $(this).removeClass('is-dragging');
    }).on('keydown', function (event) {
        if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) return;
        event.preventDefault();
        setInspectAngle(
            inspectX + (event.key === 'ArrowUp' ? 2 : event.key === 'ArrowDown' ? -2 : 0),
            inspectY + (event.key === 'ArrowRight' ? 3 : event.key === 'ArrowLeft' ? -3 : 0)
        );
    });

    $('#caseInspectReset').on('click', resetInspectAngle);

    $('#resetCaseSession').on('click', function () {
        sessionOpenings = [];
        sessionStartedAt = Date.now();
        renderSessionDuration();
        renderSessionSummary();
        renderHistory(allHistoryItems);
        window.personalToolsToast?.success('Opening session summary reset. Saved results were not removed.');
    });

    $('#caseInventoryKindToggle').on('click', '[data-inventory-kind]', function () {
        const selectedKind = String($(this).data('inventory-kind'));
        if (selectedKind === inventoryKind) return;
        setInventoryKind(selectedKind);
    }).on('keydown', '[data-inventory-kind]', function (event) {
        if (!['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
        event.preventDefault();
        const selectedKind = inventoryKind === 'skins' ? 'cases' : 'skins';
        $(`#caseInventoryKindToggle [data-inventory-kind="${selectedKind}"]`).trigger('click').trigger('focus');
    });

    $('#caseOwnedCaseSearch').on('input', renderOwnedCasesInventory);

    $('#caseOwnedCasesGrid').on('click', '.js-owned-case-open', function () {
        const selectedKey = String($(this).data('case-key') || '');
        const selectedCase = catalogue.find(item => item.caseKey === selectedKey && Number(item.ownedQuantity || 0) > 0);
        if (!selectedCase) return;

        caseKey = selectedKey;
        saveSelectedCaseKey(caseKey);
        switchDestination('open');
        if (loadedCaseKey !== selectedKey) loadCase(selectedKey, { showLoader: false });
    }).on('click', '.js-owned-case-buy', function () {
        const selectedKey = String($(this).data('case-key') || '');
        openShopCase(selectedKey, true);
    }).on('click', '.js-owned-case-discard', function (event) {
        event.stopPropagation();
        const button = this;
        const caseKeyToDiscard = String($(button).data('case-key') || '');
        const ownedQuantity = Number($(button).data('owned-quantity') || 0);
        const options = [10, 25, 50, 100, 0];
        const menu = document.createElement('div');
        menu.className = 'case-discard-menu';
        const heading = document.createElement('strong');
        heading.textContent = `${ownedQuantity.toLocaleString()} ready to discard`;
        menu.append(heading);
        options.forEach(quantity => {
            const action = document.createElement('button');
            const available = quantity === 0 || ownedQuantity >= quantity;
            action.type = 'button';
            action.className = 'btn btn-outline-danger btn-sm case-discard-option';
            action.disabled = !available;
            action.dataset.caseKey = caseKeyToDiscard;
            action.dataset.quantity = String(quantity);
            action.textContent = quantity === 0 ? 'Discard all' : `Discard ${quantity}`;
            menu.append(action);
        });
        const existing = bootstrap.Popover.getInstance(button);
        if (existing) {
            existing.dispose();
            return;
        }
        // Bootstrap otherwise promotes the trigger's native title into a bright popover header.
        // The discard menu has its own in-theme heading, so suppress that unrelated chrome.
        button.removeAttribute('title');
        bootstrap.Popover.getOrCreateInstance(button, {
            container: 'body',
            title: '',
            content: menu,
            html: true,
            placement: 'auto',
            sanitize: false,
            trigger: 'manual',
            customClass: 'case-discard-popover'
        }).show();
    });

    $(document).on('click', function (event) {
        if ($(event.target).closest('.case-discard-popover, .js-owned-case-discard').length) return;
        $('.js-owned-case-discard').each(function () { bootstrap.Popover.getInstance(this)?.dispose(); });
    }).on('click', '.case-discard-option', function () {
        const $option = $(this).prop('disabled', true);
        const caseKeyToDiscard = String($option.data('case-key') || '');
        const quantity = Number($option.data('quantity'));
        const $trigger = $(`.js-owned-case-discard[data-case-key="${CSS.escape(caseKeyToDiscard)}"]`);
        request(`/api/case-opening/cases/${encodeURIComponent(caseKeyToDiscard)}/discard`, 'POST', {
            data: JSON.stringify({ quantity: quantity }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        }).done(function (result) {
            const remaining = Number(result.ownedQuantity || 0);
            const $card = $trigger.closest('.case-owned-inventory-card');
            const catalogueItem = catalogue.find(item => item.caseKey === caseKeyToDiscard);
            if (catalogueItem) catalogueItem.ownedQuantity = remaining;
            if (remaining === 0) {
                bootstrap.Popover.getInstance($trigger.get(0))?.dispose();
                $card.addClass('is-discarding');
                window.setTimeout(() => renderCaseSelector(catalogue), 430);
            } else {
                $card.addClass('is-discard-hit');
                window.setTimeout(() => $card.removeClass('is-discard-hit'), 360);
                $trigger.data('owned-quantity', remaining).attr('data-owned-quantity', remaining);
                $card.find('.case-owned-inventory-quantity').text(`×${remaining.toLocaleString()}`);
                const popover = bootstrap.Popover.getInstance($trigger.get(0));
                popover?.dispose();
                if (remaining > 1) $trigger.trigger('click');
            }
            loadInventoryCapacity();
            window.personalToolsToast?.success(`${Number(result.discardedQuantity || 0).toLocaleString()} case${Number(result.discardedQuantity || 0) === 1 ? '' : 's'} discarded.`);
        }).fail(response => showError(response, 'Those cases could not be discarded.')).always(() => $option.prop('disabled', false));
    });

    $('#caseHistoryTableBody').on('click', '.case-history-case-link', function () {
        openShopCase(String($(this).data('case-key') || ''), false);
    });

    function showInventoryPopover(trigger, options) {
        if (!window.bootstrap?.Popover) return;
        bootstrap.Popover.getOrCreateInstance(trigger, Object.assign({
            container: 'body',
            placement: 'auto',
            trigger: 'manual'
        }, options)).show();
    }

    $(document).on('mouseenter focusin', '.case-history-finish', function () {
        showInventoryPopover(this, {
            customClass: 'case-inventory-detail-popover',
            content: String($(this).data('inventory-details') || 'No additional finish details')
        });
    }).on('mouseleave focusout', '.case-history-finish', function () {
        bootstrap.Popover.getInstance(this)?.hide();
    }).on('mouseenter focusin', '.case-history-image-preview', function () {
        const image = document.createElement('img');
        image.src = String($(this).data('image-url') || '');
        image.alt = `Zoomed preview of ${String($(this).data('image-name') || 'inventory item')}`;
        image.referrerPolicy = 'no-referrer';
        showInventoryPopover(this, {
            html: true,
            customClass: 'case-inventory-image-popover',
            content: image
        });
    }).on('mouseleave focusout', '.case-history-image-preview', function () {
        bootstrap.Popover.getInstance(this)?.hide();
    });

    $('#caseHistorySearch').on('input', function () {
        window.clearTimeout(historySearchTimer);
        historySearchTimer = window.setTimeout(filterHistory, 140);
    });

    $('#caseHistoryRarity').on('change', filterHistory);

    $('#caseHistorySort').on('change', function () {
        historySort = String(this.value || 'newest');
        try {
            localStorage.setItem(historySortStorageKey, historySort);
        } catch {
            // Sorting still applies for this visit when browser storage is unavailable.
        }
        filterHistory();
    });

    $('#caseHistoryClearFilters').on('click', function () {
        window.clearTimeout(historySearchTimer);
        historySort = 'newest';
        $('#caseHistorySearch').val('');
        $('#caseHistoryRarity').val('');
        $('#caseHistorySort').val(historySort);
        try {
            localStorage.setItem(historySortStorageKey, historySort);
        } catch {
            // Reset still applies for this visit when browser storage is unavailable.
        }
        filterHistory();
        if (window.matchMedia('(min-width: 768px)').matches) $('#caseHistorySearch').trigger('focus');
    });

    $('.case-history-view-toggle').on('click', '[data-history-view]', function () {
        const selectedView = String($(this).data('history-view'));
        if (!['list', 'cards'].includes(selectedView) || selectedView === historyView) return;
        $('#caseHistoryTableWrap, #caseHistory').addClass('case-history-changing');
        window.setTimeout(function () {
            historyView = selectedView;
            try {
                localStorage.setItem(historyViewStorageKey, historyView);
            } catch {
                // The selected view still applies for this visit when browser storage is unavailable.
            }
            renderHistoryView();
            $('#caseHistoryTableWrap, #caseHistory').removeClass('case-history-changing');
        }, 100);
    });

    $('#caseHistoryPageSize').on('change', function () {
        historyPageSize = Number(this.value) || 25;
        try {
            localStorage.setItem(historyPageSizeStorageKey, String(historyPageSize));
        } catch {
            // The selected size still applies for this visit when browser storage is unavailable.
        }
        historyPage = 1;
        renderHistoryPage();
    });

    $('#caseHistoryPagination').on('click', '.page-link', function () {
        const $item = $(this).closest('.page-item');
        if ($item.hasClass('disabled') || $item.hasClass('active')) return;
        historyPage = Math.max(1, Math.min(historyPageCount(), Number($(this).data('page')) || 1));
        $('#caseHistoryTableWrap, #caseHistory').addClass('case-history-changing');
        window.setTimeout(function () {
            renderHistoryPage();
            $('#caseHistoryTableWrap, #caseHistory').removeClass('case-history-changing');
        }, 100);
    });

    // ---------- variable tweak modal (testing tools) ----------

    function selectedCaseTweakUserId() {
        return String($('#caseTweakTargetUser').val() || '').trim();
    }

    function caseTweakDevUrl(path) {
        const targetUserId = selectedCaseTweakUserId();
        return targetUserId ? `${path}?targetUserId=${encodeURIComponent(targetUserId)}` : path;
    }

    function fillTweakProgressForm(progress) {
        const source = progress || caseTweakProgress || caseProgress;
        $('#caseTweakStars').val(Number(source?.stars || 0));
        $('#caseTweakGbp').val((Number(source?.gbpPence || 0) / 100).toFixed(2));
        $('#caseTweakXp').val(Number(source?.xp || 0));
        $('#caseTweakSkipAnimation').prop('checked', source?.skipAnimationUnlocked === true);
        $('#caseTweakMultiOpenLevel').val(Number(source?.multiOpenLevel || 0));
        $('#caseTweakOpenSpeedLevel').val(Number(source?.openSpeedLevel || 0));
    }

    function fillTweakDropRaritiesForm(settings) {
        const selected = new Set((settings?.rarityGroups || []).map(group => String(group).toLowerCase()));
        $('.js-tweak-drop-rarity').each(function () {
            $(this).prop('checked', selected.has(String($(this).val()).toLowerCase()));
        });
    }

    const caseTweakViewStorageKey = 'personalTools.caseOpeningTweakView';

    function loadCaseTweakView() {
        try {
            return localStorage.getItem(caseTweakViewStorageKey) === 'list' ? 'list' : 'cards';
        } catch {
            return 'cards';
        }
    }

    function saveCaseTweakView(view) {
        try {
            localStorage.setItem(caseTweakViewStorageKey, view);
        } catch {
            // The view choice still applies for this visit when browser storage is unavailable.
        }
    }

    function setCaseTweakView(view) {
        const resolved = view === 'list' ? 'list' : 'cards';
        saveCaseTweakView(resolved);
        $('#caseTweakCaseList')
            .toggleClass('case-tweak-case-list-cards', resolved === 'cards')
            .toggleClass('case-tweak-case-list-rows', resolved === 'list');
        $('[data-case-tweak-view]').each(function () {
            const active = $(this).data('case-tweak-view') === resolved;
            $(this).toggleClass('active', active).attr('aria-pressed', active ? 'true' : 'false');
        });
    }

    // The whole card/row is a native <label> wrapping its checkbox (not a for-attribute pointing
    // at a nested id), so clicking anywhere on it toggles the switch exactly once - no separate
    // click handler needed, and no nested <label> (which would be invalid HTML alongside a
    // for-attribute pairing).
    function tweakCaseCard(item, unlocked) {
        return $('<label class="case-tweak-case-card">', { 'data-case-key': item.caseKey }).toggleClass('is-unlocked', unlocked).append(
            $('<img>', { class: 'case-tweak-case-image', src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
            $('<span class="case-tweak-case-shade" aria-hidden="true">'),
            $('<span class="case-tweak-case-name">').text(item.name),
            $('<span class="form-switch case-tweak-case-switch">').append(
                $('<input>', {
                    class: 'form-check-input pt-switch js-tweak-case-toggle',
                    type: 'checkbox',
                    role: 'switch',
                    'data-case-key': item.caseKey,
                    checked: unlocked,
                    'aria-label': `Unlock ${item.name}`
                })
            )
        );
    }

    function tweakCaseRow(item, unlocked) {
        return $('<label class="case-tweak-case-row">', { 'data-case-key': item.caseKey }).append(
            $('<img>', { class: 'case-tweak-case-row-image', src: item.imageUrl, alt: '', loading: 'lazy', referrerpolicy: 'no-referrer' }),
            $('<span class="case-tweak-case-row-name">').text(item.name),
            $('<span class="case-tweak-case-row-status small">').text(unlocked ? 'Unlocked' : 'Locked'),
            $('<span class="form-switch case-tweak-case-row-switch">').append(
                $('<input>', {
                    class: 'form-check-input pt-switch js-tweak-case-toggle',
                    type: 'checkbox',
                    role: 'switch',
                    'data-case-key': item.caseKey,
                    checked: unlocked,
                    'aria-label': `Unlock ${item.name}`
                })
            )
        );
    }

    function renderTweakCaseList() {
        const $list = $('#caseTweakCaseList').empty();
        const unlockedCaseKeys = new Set((caseTweakProgress?.unlockedCaseKeys || []).map(key => String(key).toLowerCase()));
        (caseTweakCatalogue.length ? caseTweakCatalogue : catalogue).forEach(item => {
            const unlocked = caseTweakProgress
                ? unlockedCaseKeys.has(String(item.caseKey).toLowerCase())
                : isCaseUnlocked(item);
            $list.append(tweakCaseCard(item, unlocked), tweakCaseRow(item, unlocked));
        });
        setCaseTweakView(loadCaseTweakView());
    }

    function loadCaseTweakProfile() {
        return request(caseTweakDevUrl('/api/case-opening/dev/profile'), 'GET', { showLoader: false })
            .done(function (profile) {
                caseTweakProgress = profile?.progress || null;
                caseTweakCatalogue = Array.isArray(profile?.cases) ? profile.cases : [];
                fillTweakProgressForm(caseTweakProgress);
                fillTweakDropRaritiesForm(profile?.dropSettings);
                renderTweakCaseList();
                const currentUserId = String($('#caseTweakTargetUser').data('current-user-id') || '');
                $('#caseTweakResetButton').prop('disabled', selectedCaseTweakUserId() !== currentUserId);
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'The selected account could not be loaded.'));
    }

    function loadCaseTweakUsers() {
        const currentUserId = String($('#caseTweakTargetUser').data('current-user-id') || '');
        const $select = $('#caseTweakTargetUser').prop('disabled', true).empty();
        return request('/api/admin/users', 'GET', { showLoader: false })
            .done(function (users) {
                (users || []).filter(user => user.isActive === true).forEach(function (user) {
                    const name = String(user.displayName || user.email || 'Unnamed account');
                    const label = String(user.userId) === currentUserId ? `${name} (you)` : name;
                    $select.append($('<option>', { value: user.userId, text: label }));
                });
                $select.val(currentUserId);
                if (!$select.val() && $select.children().length) $select.prop('selectedIndex', 0);
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'User accounts could not be loaded.'))
            .always(() => $select.prop('disabled', false));
    }

    function fillTweakSettingsForm(settings) {
        gameSettingsCache = settings || {};
        $('#caseTweakEconomyMode').val(String(settings.economyMode || 'stars'));
        $('#caseTweakSaleRate').val((Number(settings.skinSaleRateBasisPoints || 9250) / 100).toFixed(2));
        $('#caseTweakFreeAllowance').prop('checked', settings.freeCaseAllowanceEnabled === true);
        $('#caseTweakFreeAllowanceQuantity').val(Number(settings.freeCaseAllowanceQuantity || 25));
        $('#caseTweakFreeAllowanceHours').val(Number(settings.freeCaseAllowanceHours || 24));
        $('#caseTweakXpPerOpen').val(Number(settings.xpPerCaseOpen || 0));
        $('#caseTweakMultiCost').val(Number(settings.multiOpenCostStars || 0));
        $('#caseTweakMultiXpReq').val(Number(settings.multiOpenXpRequirement || 0));
        $('#caseTweakSpeedBaseCost').val(Number(settings.openSpeedUpgradeBaseCostStars || 0));
        $('#caseTweakSpeedCostIncrement').val(Number(settings.openSpeedUpgradeCostIncrementStars || 0));
        $('#caseTweakSpeedXpReq').val(Number(settings.openSpeedUpgradeXpRequirement || 0));
        $('#caseTweakMaxSpeedLevel').val(Number(settings.maximumOpenSpeedLevel || 4));
        $('#caseTweakMaxMultiLevel').val(Number(settings.maximumMultiOpenLevel || 4));
        $('#caseTweakMaxOpenQuantity').val(Number(settings.maximumOpenQuantity || 5));
        $('#caseTweakBotInterval').val(Number(settings.botOpeningIntervalSeconds || 12));
        $('#caseTweakBotServerBaseCost').val(Number(settings.botServerBaseCostStars || 0));
        $('#caseTweakBotServerCostIncrement').val(Number(settings.botServerCostIncrementStars || 0));
        $('#caseTweakBotBaseCost').val(Number(settings.botBaseCostStars || 0));
        $('#caseTweakBotGrowthRate').val(Number(settings.botCostGrowthRate || 1.55));
        $('#caseTweakStorageBaseCost').val(Number(settings.storageContainerBaseCostStars || 0));
        $('#caseTweakStorageCostIncrement').val(Number(settings.storageContainerCostIncrementStars || 0));
        $('#caseTweakStorageSlots').val(Number(settings.storageContainerSlots || 1000));
        $('#caseTweakMaxStorage').val(Number(settings.maximumStorageContainers || 0));
        const gbp = (value) => (Number(value || 0) / 100).toFixed(2);
        $('#caseTweakMultiCostGbp').val(gbp(settings.multiOpenCostGbpPence));
        $('#caseTweakSpeedBaseCostGbp').val(gbp(settings.openSpeedUpgradeBaseCostGbpPence));
        $('#caseTweakSpeedIncrementGbp').val(gbp(settings.openSpeedUpgradeCostIncrementGbpPence));
        $('#caseTweakTradeRecipeGbp').val(gbp(settings.tradeUpRecipeCostGbpPence));
        $('#caseTweakTradeSlotBaseGbp').val(gbp(settings.tradeUpSlotUpgradeBaseCostGbpPence));
        $('#caseTweakTradeSlotIncrementGbp').val(gbp(settings.tradeUpSlotUpgradeCostIncrementGbpPence));
        $('#caseTweakTradeHoldingBaseGbp').val(gbp(settings.tradeUpHoldingUpgradeBaseCostGbpPence));
        $('#caseTweakTradeHoldingIncrementGbp').val(gbp(settings.tradeUpHoldingUpgradeCostIncrementGbpPence));
        $('#caseTweakTradeSlotBaseStars').val(Number(settings.tradeUpSlotUpgradeBaseCostStars || 0));
        $('#caseTweakTradeSlotIncrementStars').val(Number(settings.tradeUpSlotUpgradeCostIncrementStars || 0));
        $('#caseTweakTradeHoldingBaseStars').val(Number(settings.tradeUpHoldingUpgradeBaseCostStars || 0));
        $('#caseTweakTradeHoldingIncrementStars').val(Number(settings.tradeUpHoldingUpgradeCostIncrementStars || 0));
        $('#caseTweakBotServerBaseGbp').val(gbp(settings.botServerBaseCostGbpPence));
        $('#caseTweakBotServerIncrementGbp').val(gbp(settings.botServerCostIncrementGbpPence));
        $('#caseTweakBotBaseGbp').val(gbp(settings.botBaseCostGbpPence));
        $('#caseTweakBotSpeedBaseGbp').val(gbp(settings.botSpeedUpgradeBaseCostGbpPence));
        $('#caseTweakBotSpeedIncrementGbp').val(gbp(settings.botSpeedUpgradeCostIncrementGbpPence));
        $('#caseTweakStorageBaseGbp').val(gbp(settings.storageContainerBaseCostGbpPence));
        $('#caseTweakStorageIncrementGbp').val(gbp(settings.storageContainerCostIncrementGbpPence));
    }

    function renderTweakCasesTable(caseSettingsList) {
        const settingsByKey = new Map(caseSettingsList.map(item => [String(item.caseKey).toLowerCase(), item]));
        const $body = $('#caseTweakCasesTableBody').empty();
        catalogue.forEach(item => {
            const settings = settingsByKey.get(String(item.caseKey).toLowerCase()) || { tier: 1, unlockCostStars: 0, unlockCostGbpPence: 0, purchaseCostStars: 1, purchaseCostGbpPence: 0, xpRequirement: 0 };
            $body.append(
                $('<tr>').append(
                    $('<td>', { text: item.name }),
                    $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-case-tier', type: 'number', min: 1, max: 10, step: 1, value: Number(settings.tier || 1) })),
                    $('<td>').append($('<input>', {
                        class: 'form-control form-control-sm js-tweak-case-cost',
                        type: 'number',
                        min: 0,
                        step: 1,
                        'data-case-key': item.caseKey,
                        value: Number(settings.unlockCostStars || 0)
                    })),
                    $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-case-unlock-gbp', type: 'number', min: 0, step: .01, value: (Number(settings.unlockCostGbpPence || 0) / 100).toFixed(2) })),
                    $('<td>').append($('<input>', {
                        class: 'form-control form-control-sm js-tweak-case-purchase-cost', type: 'number', min: 0, step: 1,
                        'data-case-key': item.caseKey, value: Number(settings.purchaseCostStars || 0)
                    })),
                    $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-case-purchase-gbp', type: 'number', min: 0, step: .01, value: (Number(settings.purchaseCostGbpPence || 0) / 100).toFixed(2) })),
                    $('<td>').append($('<input>', {
                        class: 'form-control form-control-sm js-tweak-case-xp',
                        type: 'number',
                        min: 0,
                        step: 1,
                        'data-case-key': item.caseKey,
                        value: Number(settings.xpRequirement || 0)
                    }))
                )
            );
        });
    }

    function renderTweakInventoryUpgradesTable(upgrades) {
        const categoryNames = { bulk: 'Bulk selling', 'auto-sell': 'Automatic selling', gallery: 'Gallery' };
        const $body = $('#caseTweakInventoryUpgradesTableBody').empty();
        (upgrades || []).forEach(function (upgrade) {
            $body.append($('<tr>', { 'data-upgrade-key': upgrade.upgradeKey }).append(
                $('<td>').append($('<strong>', { text: upgrade.name }), $('<div>', { class: 'small-muted', text: upgrade.description })),
                $('<td>').append($('<span>', { class: 'badge text-bg-secondary-subtle border', text: categoryNames[upgrade.category] || upgrade.category })),
                $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-upgrade-cost', type: 'number', min: 0, step: 1, value: Number(upgrade.costStars || 0) })),
                $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-upgrade-gbp', type: 'number', min: 0, step: .01, value: (Number(upgrade.costGbpPence || 0) / 100).toFixed(2) })),
                $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-upgrade-level', type: 'number', min: 0, step: 1, value: Number(upgrade.requiredLevel || 0) }))
            ));
        });
    }

    function renderPriceSnapshots(summary) {
        priceSnapshotSummary = summary || { snapshots: [], cases: [] };
        const snapshots = Array.isArray(priceSnapshotSummary.snapshots) ? priceSnapshotSummary.snapshots : [];
        const active = snapshots.find(item => item.isActive === true);
        if (!active && marketValuationEnabled) {
            marketValuationEnabled = false;
            try { sessionStorage.removeItem(marketValuationSessionKey); } catch { /* Optional display preference. */ }
            $page.removeClass('is-market-valuation');
        }
        $('#caseMarketValuationToggle').prop('checked', marketValuationEnabled);
        $('#caseMarketActiveSnapshot').toggleClass('is-empty', !active).empty().append(
            active
                ? $('<div>').append(
                    $('<span>', { class: 'case-market-live-dot', 'aria-hidden': 'true' }),
                    $('<strong>', { text: active.name }),
                    $('<small>', { text: `${active.matchedItemCount.toLocaleString()} matched prices · ${active.priceBasis}` })
                )
                : $('<span>', { text: 'No active snapshot. Create one before enabling market valuation.' })
        );
        $('#caseMarketValuationToggle').prop('disabled', !active);
        $('#casePublishPriceBalance').prop('disabled', priceSnapshotSummary.canPublish !== true);
        const fallbackCount = Number(priceSnapshotSummary.fallbackPriceCount || 0);
        const missingCount = Number(priceSnapshotSummary.missingPriceCount || 0);
        const warningParts = [];
        if (missingCount > 0) warningParts.push(`${missingCount.toLocaleString()} item variant${missingCount === 1 ? '' : 's'} have no snapshot price`);
        if (fallbackCount > 0) warningParts.push(`${fallbackCount.toLocaleString()} price${fallbackCount === 1 ? '' : 's'} use a suggested or safe name fallback`);
        (Array.isArray(priceSnapshotSummary.tierWarnings) ? priceSnapshotSummary.tierWarnings : []).forEach(warning => warningParts.push(warning));
        $('#caseMarketPriceWarning')
            .toggleClass('d-none', warningParts.length === 0)
            .text(warningParts.length ? `${warningParts.join('; ')}. Resolve these before publishing live prices.` : '');

        const $list = $('#caseMarketSnapshotList').empty();
        if (!snapshots.length) {
            $list.append($('<p>', { class: 'small-muted mb-0', text: 'No snapshots have been imported yet.' }));
        } else {
            snapshots.forEach(function (snapshot) {
                const imported = new Date(snapshot.importedUtc);
                const $actions = $('<div>', { class: 'case-market-snapshot-actions' });
                if (snapshot.isActive) $actions.append($('<span>', { class: 'badge text-bg-success', text: 'Active' }));
                else $actions.append(
                    $('<button>', { class: 'btn btn-outline-warning btn-sm js-activate-price-snapshot', type: 'button', 'data-snapshot-id': snapshot.priceSnapshotId, text: 'Activate' }),
                    $('<button>', { class: 'btn btn-outline-danger btn-sm js-delete-price-snapshot', type: 'button', 'data-snapshot-id': snapshot.priceSnapshotId, 'aria-label': `Delete ${snapshot.name}`, title: 'Delete discarded snapshot' }).append($('<i>', { class: 'fa-solid fa-trash-can', 'aria-hidden': 'true' }))
                );
                $list.append($('<article>', { class: `case-market-snapshot${snapshot.isActive ? ' is-active' : ''}` }).append(
                    $('<div>').append(
                        $('<strong>', { text: snapshot.name }),
                        $('<small>', { text: `${snapshot.sourceItemCount.toLocaleString()} source items · ${snapshot.matchedItemCount.toLocaleString()} matched · ${Number.isNaN(imported.getTime()) ? '' : imported.toLocaleString()}` })
                    ),
                    $actions
                ));
            });
        }

        const cases = Array.isArray(priceSnapshotSummary.cases) ? priceSnapshotSummary.cases : [];
        const totalVariants = cases.reduce((total, item) => total + Number(item.totalVariants || 0), 0);
        const pricedVariants = cases.reduce((total, item) => total + Number(item.pricedVariants || 0), 0);
        $('#caseMarketCoverage').text(active ? `${pricedVariants.toLocaleString()} / ${totalVariants.toLocaleString()} item variants priced` : 'No snapshot loaded');
        const $body = $('#caseMarketCaseTableBody').empty();
        cases.forEach(function (item) {
            $body.append($('<tr>').append(
                $('<td>').append($('<strong>', { text: item.caseName })),
                $('<td>', { text: `Tier ${Number(item.tier || 1)}` }),
                $('<td>', { text: item.openingCost == null ? '—' : formatMarketMoney(item.openingCost) }),
                $('<td>', { text: item.expectedValue == null ? 'Incomplete pricing' : formatMarketMoney(item.expectedValue) }),
                $('<td>', { text: item.expectedSaleValuePence == null ? '—' : formatMarketMoney(Number(item.expectedSaleValuePence || 0) / 100) }),
                $('<td>', { text: item.targetReturnPercentage == null ? '—' : `${Number(item.targetReturnPercentage).toFixed(0)}%` }),
                $('<td>', { class: 'case-market-positive', text: `${Number(item.recommendedPurchaseStars || 0).toLocaleString()} ★ / ${formatMarketMoney(Number(item.recommendedPurchaseGbpPence || 0) / 100)}` }),
                $('<td>', { text: formatMarketMoney(Number(item.publishedPurchaseGbpPence || 0) / 100) }),
                $('<td>', { text: `${Number(item.pricedVariants || 0)} / ${Number(item.totalVariants || 0)}` })
            ));
        });
    }

    function loadPriceSnapshots() {
        return request('/api/case-opening/settings/price-snapshots', 'GET', { showLoader: false })
            .done(renderPriceSnapshots)
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Market snapshots could not be loaded.'));
    }

    function renderSpecialVariants(summary) {
        const rules = Array.isArray(summary?.rules) ? summary.rules : [];
        const snapshots = Array.isArray(summary?.snapshots) ? summary.snapshots : [];
        const active = snapshots.find(snapshot => snapshot.isActive === true);
        const pricedRuleCount = active ? rules.filter(rule => String(rule.priceSnapshotId || '') === String(active.priceSnapshotId)).length : 0;
        $('#caseSpecialVariantActiveSnapshot').toggleClass('is-empty', !active).empty().append(
            active
                ? $('<div>').append(
                    $('<span>', { class: 'case-market-live-dot', 'aria-hidden': 'true' }),
                    $('<strong>', { text: active.name }),
                    $('<small>', { text: `${pricedRuleCount.toLocaleString()} / ${rules.length.toLocaleString()} configured rare variants priced · ${active.source}` })
                )
                : $('<span>', { text: 'No active rare-variant snapshot. Add reviewed prices, then publish one.' })
        );
        const $rules = $('#caseSpecialVariantRules').empty();
        if (!rules.length) $rules.append($('<tr>').append($('<td>', { colspan: 3, class: 'small-muted', text: 'No rare-variant rules yet. Add only reviewed community classifications.' })));
        rules.forEach(function (rule) {
            const match = [`ID ${rule.sourceItemId}`, rule.patternSeed == null ? '' : `seed ${rule.patternSeed}`, rule.minimumFloat == null ? '' : `≥ ${Number(rule.minimumFloat).toFixed(6)}`, rule.maximumFloat == null ? '' : `≤ ${Number(rule.maximumFloat).toFixed(6)}`].filter(Boolean).join(' · ');
            $rules.append($('<tr>').append($('<td>').append($('<strong>', { text: rule.name }), $('<small>', { class: 'd-block text-muted', text: `${rule.tier} · ${rule.description}` })), $('<td>', { class: 'small', text: match }), $('<td>').append($('<input>', { class: 'form-control form-control-sm js-variant-price', type: 'number', min: 0, step: '.01', value: rule.price == null ? '' : Number(rule.price).toFixed(2) }).data('rule-id', rule.ruleId))));
        });
        const $snapshots = $('#caseSpecialVariantSnapshots').empty();
        snapshots.forEach(function (snapshot) {
            const $actions = $('<div>', { class: 'case-market-snapshot-actions' });
            if (snapshot.isActive) $actions.append($('<span>', { class: 'badge text-bg-success', text: 'Active' }));
            else $actions.append(
                $('<button>', { class: 'btn btn-outline-warning btn-sm js-activate-variant-snapshot', type: 'button', 'data-snapshot-id': snapshot.priceSnapshotId, text: 'Activate' }),
                $('<button>', { class: 'btn btn-outline-danger btn-sm js-delete-variant-snapshot', type: 'button', 'data-snapshot-id': snapshot.priceSnapshotId, 'aria-label': `Delete ${snapshot.name}`, title: 'Delete discarded rare-variant snapshot' }).append($('<i>', { class: 'fa-solid fa-trash-can', 'aria-hidden': 'true' }))
            );
            $snapshots.append($('<article>', { class: `case-market-snapshot${snapshot.isActive ? ' is-active' : ''}` }).append($('<div>').append($('<strong>', { text: snapshot.name }), $('<small>', { text: `${snapshot.source} · ${new Date(snapshot.importedUtc).toLocaleString()}` })), $actions));
        });
    }

    function loadSpecialVariants() { return request('/api/case-opening/settings/special-variants', 'GET', { showLoader: false }).done(renderSpecialVariants); }

    $('#caseTweakModal').on('show.bs.modal', function () {
        loadCaseTweakUsers().done(loadCaseTweakProfile);
        request('/api/case-opening/settings', 'GET', { showLoader: false })
            .done(fillTweakSettingsForm)
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Game settings could not be loaded.'));
        request('/api/case-opening/settings/xp-by-rarity', 'GET', { showLoader: false })
            .done(function (items) {
                const names = { 'mil-spec': 'Mil-Spec', restricted: 'Restricted', classified: 'Classified', covert: 'Covert', 'rare-special': 'Rare Special (Gold)', 'high-grade': 'High Grade', remarkable: 'Remarkable', exotic: 'Exotic' };
                const $body = $('#caseTweakXpByRarityTableBody').empty();
                (items || []).forEach(function (item) {
                    $body.append($('<tr>').append(
                        $('<td>', { text: names[item.rarityKey] || item.rarityKey }),
                        $('<td>').append($('<input>', { class: 'form-control form-control-sm js-tweak-xp-rarity', type: 'number', min: 0, step: 1, value: Number(item.xpAwarded || 0) }).data('rarity-key', item.rarityKey))
                    ));
                });
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'XP rewards could not be loaded.'));
        request('/api/case-opening/settings/cases', 'GET', { showLoader: false })
            .done(renderTweakCasesTable)
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Case settings could not be loaded.'));
        request('/api/case-opening/settings/inventory-upgrades', 'GET', { showLoader: false })
            .done(renderTweakInventoryUpgradesTable)
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Inventory upgrade settings could not be loaded.'));
        loadPriceSnapshots();
        loadSpecialVariants().fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Rare-variant settings could not be loaded.'));
    });

    $('#caseTweakTargetUser').on('change', function () {
        loadCaseTweakProfile();
    });

    $('#caseSpecialVariantRuleForm').on('submit', function (event) {
        event.preventDefault();
        const numberOrNull = selector => $(selector).val() === '' ? null : Number($(selector).val());
        const statTrak = $('#caseVariantStatTrak').val();
        const payload = { sourceItemId: $('#caseVariantSourceItemId').val().trim(), marketHashName: $('#caseVariantMarketHashName').val().trim(), name: $('#caseVariantName').val().trim(), tier: $('#caseVariantTier').val().trim(), description: $('#caseVariantDescription').val().trim(), patternSeed: numberOrNull('#caseVariantSeed'), minimumFloat: numberOrNull('#caseVariantMinFloat'), maximumFloat: numberOrNull('#caseVariantMaxFloat'), phase: $('#caseVariantPhase').val().trim() || null, requiresStatTrak: statTrak === '' ? null : statTrak === 'true', isActive: true };
        request('/api/case-opening/settings/special-variants', 'POST', { data: JSON.stringify(payload), contentType: 'application/json; charset=utf-8', showLoader: false }).done(function (summary) { renderSpecialVariants(summary); event.target.reset(); window.personalToolsToast?.success('Rare-variant rule saved.'); }).fail(response => showError(response, 'The rare-variant rule could not be saved.'));
    });

    $('#caseCreateVariantSnapshot').on('click', function () {
        const prices = {}; $('.js-variant-price').each(function () { const value = Number($(this).val()); if (Number.isFinite(value) && value >= 0) prices[String($(this).data('rule-id'))] = value; });
        const payload = { name: $('#caseVariantSnapshotName').val().trim(), source: $('#caseVariantSnapshotSource').val().trim(), prices };
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/settings/special-variant-price-snapshots', 'POST', { data: JSON.stringify(payload), contentType: 'application/json; charset=utf-8', showLoader: false }).done(function (summary) { renderSpecialVariants(summary); window.personalToolsToast?.success('Immutable rare-variant snapshot published.'); }).fail(response => showError(response, 'The rare-variant snapshot could not be published.')).always(() => $button.prop('disabled', false));
    });

    $('#caseImportCsFloatEvidence').on('click', function () {
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/settings/special-variants/csfloat-import', 'POST', { showLoader: false }).done(function (items) {
            const evidence = Array.isArray(items) ? items : [];
            $('#caseCsFloatEvidence').text(evidence.length ? `${evidence.length} matching CSFloat listing${evidence.length === 1 ? '' : 's'} found. Review the observed values before entering a GBP snapshot price.` : 'No active CSFloat listings matched the configured rules.');
        }).fail(response => showError(response, 'CSFloat listing evidence could not be loaded.')).always(() => $button.prop('disabled', false));
    });

    $('#caseSpecialVariantSnapshots').on('click', '.js-activate-variant-snapshot', function () {
        const snapshotId = String($(this).data('snapshot-id') || '');
        request(`/api/case-opening/settings/special-variant-price-snapshots/${encodeURIComponent(snapshotId)}/activate`, 'POST', { showLoader: false }).done(function (summary) { renderSpecialVariants(summary); window.personalToolsToast?.success('Rare-variant snapshot activated.'); }).fail(response => showError(response, 'The snapshot could not be activated.'));
    });

    $('#caseSpecialVariantSnapshots').on('click', '.js-delete-variant-snapshot', function () {
        const snapshotId = String($(this).data('snapshot-id') || '');
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/settings/special-variant-price-snapshots/${encodeURIComponent(snapshotId)}`, 'DELETE', { showLoader: false })
            .done(function (summary) { renderSpecialVariants(summary); window.personalToolsToast?.success('Discarded rare-variant snapshot deleted.'); })
            .fail(response => showError(response, 'The rare-variant snapshot could not be deleted.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseMarketValuationToggle').on('change', function () {
        marketValuationEnabled = this.checked;
        try {
            sessionStorage.setItem(marketValuationSessionKey, marketValuationEnabled ? 'true' : 'false');
        } catch {
            // The mode still applies until this page is closed when session storage is unavailable.
        }
        $page.toggleClass('is-market-valuation', marketValuationEnabled);
        refreshInventorySaleValues();
        if (historyLoaded && activeDestination === 'inventory') renderHistoryPage({ skipMotion: true });
        markSwitchSaved(this);
        window.personalToolsToast?.info(marketValuationEnabled ? 'Published GBP valuation enabled.' : 'Published GBP valuation hidden.');
    });

    $('#caseCreatePriceSnapshot').on('click', function () {
        const $button = $(this).prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Importing Skinport…');
        request('/api/case-opening/settings/price-snapshots', 'POST', { showLoader: false })
            .done(function (summary) {
                renderPriceSnapshots(summary);
                window.personalToolsToast?.success('New Skinport GBP snapshot imported and activated.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'The Skinport snapshot could not be imported.'))
            .always(() => $button.prop('disabled', false).html('<i class="fa-solid fa-cloud-arrow-down me-1" aria-hidden="true"></i>New snapshot'));
    });

    $('#casePublishPriceBalance').on('click', function () {
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/settings/price-snapshots/publish-balance', 'POST', { showLoader: false })
            .done(function (summary) {
                renderPriceSnapshots(summary);
                loadCaseCatalogue();
                window.personalToolsToast?.success('Balanced case prices published for Stars and GBP.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'The balance could not be published.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseMarketSnapshotList').on('click', '.js-activate-price-snapshot', function () {
        const snapshotId = String($(this).data('snapshot-id') || '');
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/settings/price-snapshots/${encodeURIComponent(snapshotId)}/activate`, 'POST', { showLoader: false })
            .done(function (summary) {
                renderPriceSnapshots(summary);
                window.personalToolsToast?.success('Price snapshot activated. Future pulls will use its values.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'That snapshot could not be activated.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseMarketSnapshotList').on('click', '.js-delete-price-snapshot', function () {
        const snapshotId = String($(this).data('snapshot-id') || '');
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/settings/price-snapshots/${encodeURIComponent(snapshotId)}`, 'DELETE', { showLoader: false })
            .done(function (summary) { renderPriceSnapshots(summary); window.personalToolsToast?.success('Discarded price snapshot deleted.'); })
            .fail(response => showError(response, 'The price snapshot could not be deleted.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#saveCaseTweakXpByRarity').on('click', function () {
        const $button = $(this).prop('disabled', true);
        const updates = $('#caseTweakXpByRarityTableBody .js-tweak-xp-rarity').map(function () {
            return request(`/api/case-opening/settings/xp-by-rarity/${encodeURIComponent(String($(this).data('rarity-key')))}`, 'PUT', {
                data: JSON.stringify({ xpAwarded: Math.max(0, Math.trunc(Number($(this).val()) || 0)) }),
                contentType: 'application/json; charset=utf-8',
                showLoader: false
            });
        }).get();
        $.when(...updates)
            .done(() => window.personalToolsToast?.success('XP rewards saved.'))
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'One or more XP rewards could not be saved.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#saveCaseTweakInventoryUpgrades').on('click', function () {
        const $button = $(this).prop('disabled', true);
        const updates = $('#caseTweakInventoryUpgradesTableBody tr').map(function () {
            const $row = $(this);
            return request(`/api/case-opening/settings/inventory-upgrades/${encodeURIComponent(String($row.data('upgrade-key')))}`, 'PUT', {
                data: JSON.stringify({
                    costStars: Math.max(0, Math.trunc(Number($row.find('.js-tweak-upgrade-cost').val()) || 0)),
                    costGbpPence: Math.max(0, Math.round((Number($row.find('.js-tweak-upgrade-gbp').val()) || 0) * 100)),
                    requiredLevel: Math.max(0, Math.trunc(Number($row.find('.js-tweak-upgrade-level').val()) || 0))
                }),
                contentType: 'application/json; charset=utf-8',
                showLoader: false
            });
        }).get();
        $.when(...updates)
            .done(function () {
                loadInventoryUpgrades({ showLoader: false });
                window.personalToolsToast?.success('Inventory upgrade settings saved.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'One or more inventory upgrades could not be saved.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseTweakProgressForm').on('submit', function (event) {
        event.preventDefault();
        const payload = {
            stars: Math.max(0, Math.trunc(Number($('#caseTweakStars').val()) || 0)),
            gbpPence: Math.max(0, Math.round((Number($('#caseTweakGbp').val()) || 0) * 100)),
            xp: Math.max(0, Math.trunc(Number($('#caseTweakXp').val()) || 0))
        };
        request(caseTweakDevUrl('/api/case-opening/dev/progress'), 'PUT', {
            data: JSON.stringify(payload),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (progress) {
                caseTweakProgress = progress;
                fillTweakProgressForm(progress);
                if (selectedCaseTweakUserId() === String($('#caseTweakTargetUser').data('current-user-id') || '')) renderProgress(progress);
                window.personalToolsToast?.success('Currency balances and XP updated.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Your progress could not be updated.'));
    });

    $('#caseTweakUpgradesForm').on('submit', function (event) {
        event.preventDefault();
        const payload = {
            skipAnimationUnlocked: $('#caseTweakSkipAnimation').is(':checked'),
            multiOpenLevel: Math.max(0, Math.trunc(Number($('#caseTweakMultiOpenLevel').val()) || 0)),
            openSpeedLevel: Math.max(0, Math.trunc(Number($('#caseTweakOpenSpeedLevel').val()) || 0))
        };
        request(caseTweakDevUrl('/api/case-opening/dev/upgrades'), 'PUT', {
            data: JSON.stringify(payload),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (progress) {
                caseTweakProgress = progress;
                fillTweakProgressForm(progress);
                if (selectedCaseTweakUserId() === String($('#caseTweakTargetUser').data('current-user-id') || '')) renderProgress(progress);
                markSwitchSaved($('#caseTweakSkipAnimation'));
                window.personalToolsToast?.success('Upgrades updated.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Your upgrades could not be updated.'));
    });

    $('#caseTweakDropRaritiesForm').on('submit', function (event) {
        event.preventDefault();
        const $button = $(this).find('button[type="submit"]').prop('disabled', true);
        const rarityGroups = $('.js-tweak-drop-rarity:checked').map(function () { return String($(this).val()); }).get();
        request(caseTweakDevUrl('/api/case-opening/dev/drop-rarities'), 'PUT', {
            data: JSON.stringify({ rarityGroups: rarityGroups }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (settings) {
                fillTweakDropRaritiesForm(settings);
                window.personalToolsToast?.success(rarityGroups.length ? 'Test drop rarities saved for this account.' : 'Normal drop odds restored for this account.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Test drop rarities could not be updated.'))
            .always(() => $button.prop('disabled', false));
    });

    $('[data-case-tweak-view]').on('click', function () {
        setCaseTweakView(String($(this).data('case-tweak-view')));
    });

    $('#caseTweakCaseList').on('change', '.js-tweak-case-toggle', function () {
        const $toggle = $(this);
        const caseKeyToToggle = String($toggle.data('case-key'));
        const unlock = $toggle.is(':checked');
        $toggle.prop('disabled', true);
        request(caseTweakDevUrl(`/api/case-opening/dev/cases/${encodeURIComponent(caseKeyToToggle)}`), 'PUT', {
            data: JSON.stringify({ unlock: unlock }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (progress) {
                caseTweakProgress = progress;
                if (selectedCaseTweakUserId() === String($('#caseTweakTargetUser').data('current-user-id') || '')) renderProgress(progress);
                renderTweakCaseList();
                markSwitchSaved($(`.js-tweak-case-toggle[data-case-key="${CSS.escape(caseKeyToToggle)}"]`));
                window.personalToolsToast?.success(unlock ? 'Case unlocked.' : 'Case locked.');
            })
            .fail(function (response) {
                $toggle.prop('checked', !unlock);
                window.personalToolsToast?.error(response.responseJSON?.message || 'This case could not be updated.');
            })
            .always(() => $toggle.prop('disabled', false));
    });

    $('#caseTweakSettingsForm').on('submit', function (event) {
        event.preventDefault();
        const payload = {
            economyMode: $('#caseTweakEconomyMode').val() === 'gbp' ? 'gbp' : 'stars',
            skinSaleRateBasisPoints: Math.max(0, Math.min(10000, Math.round((Number($('#caseTweakSaleRate').val()) || 0) * 100))),
            freeCaseAllowanceEnabled: $('#caseTweakFreeAllowance').is(':checked'),
            freeCaseAllowanceQuantity: Math.max(1, Math.trunc(Number($('#caseTweakFreeAllowanceQuantity').val()) || 1)),
            freeCaseAllowanceHours: Math.max(1, Math.trunc(Number($('#caseTweakFreeAllowanceHours').val()) || 1)),
            xpPerCaseOpen: Math.max(0, Math.trunc(Number($('#caseTweakXpPerOpen').val()) || 0)),
            // Skip animation is no longer independently purchasable (see the speed-upgrade
            // rebalance) - these columns are inert, kept only so the settings payload shape
            // matches the API/DB signature.
            skipAnimationCostStars: 0,
            skipAnimationXpRequirement: 0,
            multiOpenCostStars: Math.max(0, Math.trunc(Number($('#caseTweakMultiCost').val()) || 0)),
            multiOpenXpRequirement: Math.max(0, Math.trunc(Number($('#caseTweakMultiXpReq').val()) || 0)),
            openSpeedUpgradeBaseCostStars: Math.max(0, Math.trunc(Number($('#caseTweakSpeedBaseCost').val()) || 0)),
            openSpeedUpgradeCostIncrementStars: Math.max(0, Math.trunc(Number($('#caseTweakSpeedCostIncrement').val()) || 0)),
            openSpeedUpgradeXpRequirement: Math.max(0, Math.trunc(Number($('#caseTweakSpeedXpReq').val()) || 0)),
            maximumOpenSpeedLevel: Math.max(1, Math.trunc(Number($('#caseTweakMaxSpeedLevel').val()) || 1)),
            maximumMultiOpenLevel: Math.max(1, Math.trunc(Number($('#caseTweakMaxMultiLevel').val()) || 1)),
            maximumOpenQuantity: Math.max(1, Math.trunc(Number($('#caseTweakMaxOpenQuantity').val()) || 1)),
            botOpeningIntervalSeconds: Math.max(1, Math.trunc(Number($('#caseTweakBotInterval').val()) || 1)),
            botServerBaseCostStars: Math.max(0, Math.trunc(Number($('#caseTweakBotServerBaseCost').val()) || 0)),
            botServerCostIncrementStars: Math.max(0, Math.trunc(Number($('#caseTweakBotServerCostIncrement').val()) || 0)),
            botBaseCostStars: Math.max(0, Math.trunc(Number($('#caseTweakBotBaseCost').val()) || 0)),
            botCostGrowthRate: Math.max(1, Number($('#caseTweakBotGrowthRate').val()) || 1),
            storageContainerBaseCostStars: Math.max(0, Math.trunc(Number($('#caseTweakStorageBaseCost').val()) || 0)),
            storageContainerCostIncrementStars: Math.max(0, Math.trunc(Number($('#caseTweakStorageCostIncrement').val()) || 0)),
            storageContainerSlots: Math.max(1, Math.trunc(Number($('#caseTweakStorageSlots').val()) || 1)),
            maximumStorageContainers: Math.max(0, Math.trunc(Number($('#caseTweakMaxStorage').val()) || 0))
        };
        Object.assign(payload, {
            skipAnimationCostGbpPence: Number(gameSettingsCache?.skipAnimationCostGbpPence || 0),
            multiOpenCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakMultiCostGbp').val()) || 0) * 100)),
            openSpeedUpgradeBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakSpeedBaseCostGbp').val()) || 0) * 100)),
            openSpeedUpgradeCostIncrementGbpPence: Math.max(0, Math.round((Number($('#caseTweakSpeedIncrementGbp').val()) || 0) * 100)),
            botServerBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakBotServerBaseGbp').val()) || 0) * 100)),
            botServerCostIncrementGbpPence: Math.max(0, Math.round((Number($('#caseTweakBotServerIncrementGbp').val()) || 0) * 100)),
            botBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakBotBaseGbp').val()) || 0) * 100)),
            botSpeedUpgradeBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakBotSpeedBaseGbp').val()) || 0) * 100)),
            botSpeedUpgradeCostIncrementGbpPence: Math.max(0, Math.round((Number($('#caseTweakBotSpeedIncrementGbp').val()) || 0) * 100)),
            storageContainerBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakStorageBaseGbp').val()) || 0) * 100)),
            storageContainerCostIncrementGbpPence: Math.max(0, Math.round((Number($('#caseTweakStorageIncrementGbp').val()) || 0) * 100)),
            tradeUpRecipeCostStars: Number(gameSettingsCache?.tradeUpRecipeCostStars || 0),
            tradeUpRecipeCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakTradeRecipeGbp').val()) || 0) * 100)),
            tradeUpSlotUpgradeBaseCostStars: Math.max(0, Math.trunc(Number($('#caseTweakTradeSlotBaseStars').val()) || 0)),
            tradeUpSlotUpgradeCostIncrementStars: Math.max(0, Math.trunc(Number($('#caseTweakTradeSlotIncrementStars').val()) || 0)),
            tradeUpSlotUpgradeBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakTradeSlotBaseGbp').val()) || 0) * 100)),
            tradeUpSlotUpgradeCostIncrementGbpPence: Math.max(0, Math.round((Number($('#caseTweakTradeSlotIncrementGbp').val()) || 0) * 100)),
            tradeUpHoldingUpgradeBaseCostStars: Math.max(0, Math.trunc(Number($('#caseTweakTradeHoldingBaseStars').val()) || 0)),
            tradeUpHoldingUpgradeCostIncrementStars: Math.max(0, Math.trunc(Number($('#caseTweakTradeHoldingIncrementStars').val()) || 0)),
            tradeUpHoldingUpgradeBaseCostGbpPence: Math.max(0, Math.round((Number($('#caseTweakTradeHoldingBaseGbp').val()) || 0) * 100)),
            tradeUpHoldingUpgradeCostIncrementGbpPence: Math.max(0, Math.round((Number($('#caseTweakTradeHoldingIncrementGbp').val()) || 0) * 100))
        });
        request('/api/case-opening/settings', 'PUT', {
            data: JSON.stringify(payload),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })
            .done(function (settings) {
                fillTweakSettingsForm(settings);
                loadProgress();
                loadBotProgress();
                window.personalToolsToast?.success('Game settings saved.');
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Game settings could not be saved.'));
    });

    $('#saveCaseTweakCosts').on('click', function () {
        const $button = $(this).prop('disabled', true);
        const updates = $('#caseTweakCasesTableBody tr').map(function () {
            const $row = $(this);
            const caseKeyToSave = String($row.find('.js-tweak-case-cost').data('case-key'));
            return {
                caseKey: caseKeyToSave,
                tier: Math.max(1, Math.min(10, Math.trunc(Number($row.find('.js-tweak-case-tier').val()) || 1))),
                unlockCostStars: Math.max(0, Math.trunc(Number($row.find('.js-tweak-case-cost').val()) || 0)),
                unlockCostGbpPence: Math.max(0, Math.round((Number($row.find('.js-tweak-case-unlock-gbp').val()) || 0) * 100)),
                purchaseCostStars: Math.max(0, Math.trunc(Number($row.find('.js-tweak-case-purchase-cost').val()) || 0)),
                purchaseCostGbpPence: Math.max(0, Math.round((Number($row.find('.js-tweak-case-purchase-gbp').val()) || 0) * 100)),
                xpRequirement: Math.max(0, Math.trunc(Number($row.find('.js-tweak-case-xp').val()) || 0))
            };
        }).get();

        $.when(...updates.map(update => request(`/api/case-opening/settings/cases/${encodeURIComponent(update.caseKey)}`, 'PUT', {
            data: JSON.stringify(update),
            contentType: 'application/json; charset=utf-8',
            showLoader: false
        })))
            .done(function () {
                loadCaseCatalogue();
                window.personalToolsToast?.success('Case costs saved.');
            })
            .fail(() => window.personalToolsToast?.error('One or more case costs could not be saved.'))
            .always(() => $button.prop('disabled', false));
    });

    $('#caseTweakResetButton').on('click', function () {
        // Showing a second modal before the first one's hide transition (and backdrop cleanup)
        // has actually finished corrupts Bootstrap's modal state - the tweak modal would then
        // refuse to reopen until the page was refreshed. Waiting for hidden.bs.modal before
        // showing the next one avoids that.
        const tweakModalEl = document.getElementById('caseTweakModal');
        $(tweakModalEl).one('hidden.bs.modal', function () {
            bootstrap.Modal.getOrCreateInstance(document.getElementById('caseTweakResetModal')).show();
        });
        bootstrap.Modal.getInstance(tweakModalEl)?.hide();
    });

    $('#caseTweakResetForm').on('submit', function (event) {
        event.preventDefault();
        request('/api/case-opening/dev/reset', 'POST', { showLoader: false })
            .done(function () {
                // Resetting touches selected cases, bot state, history, achievements and every
                // cached counter. Reload once so the screen is rebuilt from the authoritative DB
                // state instead of trying to reconcile a dozen independent client caches.
                window.personalToolsToast?.queue('Your account has been reset to a new player.', 'success');
                window.location.reload();
            })
            .fail(response => window.personalToolsToast?.error(response.responseJSON?.message || 'Your account could not be reset.'));
    });

    $('#caseDailyDrop').on('click keydown', function (event) {
        if (event.type === 'keydown' && event.key !== 'Enter' && event.key !== ' ') return;
        if (event.type === 'keydown') event.preventDefault();
        openDailyDropModal();
    });

    $('#caseDailyDropRecall').on('click', openDailyDropModal);

    $('#caseDailyDropRewards').on('change', '.js-daily-drop-choice', function () {
        const $choices = $('.js-daily-drop-choice:checked');
        if ($choices.length > 2) $(this).prop('checked', false);
        const count = $('.js-daily-drop-choice:checked').length;
        $('#caseDailyDropRewards .case-daily-reward-option').each(function () {
            $(this).toggleClass('is-selected', $(this).find('.js-daily-drop-choice').prop('checked'));
        });
        $('#caseDailyDropClaim').prop('disabled', count !== 2);
        $('#caseDailyDropSelectionStatus').text(count === 2 ? '2 / 2 selected — ready to claim' : `${count} / 2 rewards selected`);
    });

    $('#caseDailyDropClaim').on('click', function () {
        const rewardKeys = $('.js-daily-drop-choice:checked').map(function () { return String($(this).val()); }).get();
        const $button = $(this).prop('disabled', true);
        request('/api/case-opening/daily-drop/claim', 'POST', { data: JSON.stringify({ rewardKeys }), contentType: 'application/json; charset=utf-8' })
            .done(function (progress) {
                renderProgress(progress);
                bootstrap.Modal.getInstance(document.getElementById('caseDailyDropModal'))?.hide();
                window.personalToolsToast?.success('Daily Drop claimed.');
            })
            .fail(response => { window.personalToolsToast?.error(response.responseJSON?.message || 'Daily Drop could not be claimed.'); $button.prop('disabled', false); });
    });

    $('#caseDailyUpgradeTreeCards').on('click', '.js-daily-upgrade', function () {
        const $button = $(this).prop('disabled', true);
        request(`/api/case-opening/daily-drop/upgrades/${encodeURIComponent(String($button.data('upgrade-key')))}/unlock`, 'POST', { showLoader: false })
            .done(function (progress) { renderProgress(progress); window.personalToolsToast?.success('Daily Drop upgrade unlocked.'); })
            .fail(response => { window.personalToolsToast?.error(response.responseJSON?.message || 'This Daily Drop upgrade could not be unlocked.'); $button.prop('disabled', false); });
    });

    function syncPlayerSettingsAppearance() {
        const appearance = window.personalToolsAppearance?.current();
        if (!appearance) return;
        $('.case-theme-choice').each(function () {
            const selected = $(this).data('case-theme') === appearance.theme;
            $(this).toggleClass('is-selected', selected).attr('aria-pressed', String(selected));
        });
        $('.case-mode-choice').each(function () {
            const selected = $(this).data('case-mode') === appearance.mode;
            $(this).toggleClass('is-selected', selected).attr('aria-pressed', String(selected));
        });
    }

    function savePlayerAppearance(key, value) {
        window.personalToolsAppearance?.applySetting(key, value);
        syncPlayerSettingsAppearance();
        request('/api/settings', 'PUT', {
            data: JSON.stringify({ key: key, value: value }),
            contentType: 'application/json; charset=utf-8',
            showLoader: false,
            showToast: false
        }).fail(() => window.personalToolsToast?.error('Your appearance is saved in this browser but could not be synced to your account.'));
    }

    $('#casePlayerSettingsModal').on('show.bs.modal', syncPlayerSettingsAppearance);
    $('.case-theme-choice').on('click', function () { savePlayerAppearance('AppearanceTheme', String($(this).data('case-theme'))); });
    $('.case-mode-choice').on('click', function () { savePlayerAppearance('AppearanceMode', String($(this).data('case-mode'))); });

    function renderCaseBattleAdminStatus(status) {
        $('#caseTweakBattlesEnabled').prop('checked', Boolean(status.caseBattlesEnabled)).prop('disabled', false);
        $('#caseTweakBattleBotEnabled').prop('checked', Boolean(status.enabled)).prop('disabled', false);
        const values = [status.battlesAttempted, status.battlesWon, status.skinsDiscarded, new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' }).format(Number(status.valueDiscarded || 0))];
        $('#caseTweakBattleBotStats strong').each(function (index) { $(this).text(values[index]); });
    }

    function loadCaseBattleAdmin() {
        const $rows = $('#caseTweakBattleRows').html('<tr><td colspan="4" class="text-center small-muted py-4">Loading battle health…</td></tr>');
        $.getJSON('/api/admin/case-battles/bot-status').done(renderCaseBattleAdminStatus).fail(() => window.personalToolsToast?.error('Case Battle controls could not be loaded.'));
        $.getJSON('/api/admin/case-battles').done(items => {
            if (!items.length) { $rows.html('<tr><td colspan="4" class="text-center small-muted py-4">No unresolved battles need attention.</td></tr>'); return; }
            const escape = value => $('<div>').text(value || '').html();
            $rows.empty();
            items.forEach(item => $rows.append(`<tr><td><code>${escape(item.battleId)}</code></td><td>${escape(item.creatorDisplayName)}</td><td>${item.joinedPlayers} players · ${item.caseCount} cases</td><td><span class="badge text-bg-${item.status === 'opening' ? 'warning' : 'secondary'}">${escape(item.attention)}</span></td></tr>`));
        }).fail(() => $rows.html('<tr><td colspan="4" class="text-center text-danger py-4">Could not load battle health.</td></tr>'));
    }

    $('#caseTweakBattlesTab').on('shown.bs.tab', loadCaseBattleAdmin);
    $('#caseTweakBattleRefresh').on('click', loadCaseBattleAdmin);
    $('#caseTweakBattlesEnabled').on('change', function () {
        const control = $(this).prop('disabled', true);
        request('/api/admin/case-battles/feature-status', 'PUT', { data: JSON.stringify(control.prop('checked')), contentType: 'application/json' })
            .done(renderCaseBattleAdminStatus)
            .fail(() => { window.personalToolsToast?.error('Case Battle visibility could not be saved.'); loadCaseBattleAdmin(); });
    });
    $('#caseTweakBattleBotEnabled').on('change', function () {
        const control = $(this).prop('disabled', true);
        request('/api/admin/case-battles/bot-status', 'PUT', { data: JSON.stringify(control.prop('checked')), contentType: 'application/json' })
            .done(renderCaseBattleAdminStatus)
            .fail(() => { window.personalToolsToast?.error('Battle Bot visibility could not be saved.'); loadCaseBattleAdmin(); });
    });

    $('#caseHistoryPageSize').val(String(historyPageSize));
    $('#caseHistorySort').val(historySort);
    initialiseCollapsibleSections();
    renderHistoryView();
    setInventoryKind(inventoryKind, { skipMotion: true });
    renderSessionSummary();
    renderSessionDuration();
    window.setInterval(renderSessionDuration, 1000);
    renderSoundControls();
    $page.toggleClass('is-market-valuation', marketValuationEnabled);
    // Apply the saved destination before the first request completes so another panel never
    // flashes briefly on a slower mobile connection.
    switchDestination(activeDestination, { initial: true, animate: false, deferLoad: true });
    request('/api/case-opening/cases')
        .done(function (items) {
            catalogueLoaded = true;
            renderCaseSelector(items);
            renderShop(items);
            const selected = items.find(item => item.caseKey === caseKey && item.isUnlocked)
                || items.find(item => item.isUnlocked)
                || items[0];
            if (!selected) {
                showError(null, 'The case catalogue could not be loaded.');
                return;
            }
            caseKey = selected.caseKey;
            saveSelectedCaseKey(caseKey);
            switchDestination(activeDestination, { initial: true, animate: false });
        })
        .fail(response => showError(response, 'The case catalogue could not be loaded.'));
})(jQuery);

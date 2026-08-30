# Case-opening economy rollout

This rollout keeps market data and playable prices separate. Creating a price snapshot does not publish the recommended case prices; an administrator must review the quality gates and explicitly publish them.

## Before the maintenance window

- Back up `CaseOpeningGameSettings`, `CaseOpeningCaseSettings`, `CaseOpeningTierEconomySettings`, `CaseOpeningPriceSnapshots`, and `CaseOpeningPriceSnapshotItems`.
- Confirm the existing `2026-08-27-case-opening-dual-economy-foundation.sql` migration has been applied.
- Confirm the protected `CSFloatApiKey` setting is present if backup pricing is required. Skinport remains the primary source.
- Record the active price snapshot ID and the current global return multiplier, sale rate, exchange rate, and tier curve.
- Keep the economy in its current mode until the new snapshot has passed review.

## Apply in order

1. `2026-08-30-case-opening-catalogue-economy-phase-1.sql`
2. `2026-08-30-case-opening-skinport-pricing-phase-2.sql`
3. `2026-08-30-case-opening-csfloat-quality-phase-3.sql`
4. `2026-08-30-case-opening-ev-tiers-phase-4.sql`
5. Deploy the matching application build and restart the service.

The scripts are designed to preserve existing case overrides and administrator-adjusted tier values when rerun. Do not run Phase 4 without the dual-economy foundation because it owns the tier settings table.

## Database verification

Run these read-only checks after the migrations:

```sql
SELECT GlobalReturnMultiplierBasisPoints, CsFloatUsdToGbpBasisPoints
FROM CaseOpeningGameSettings WHERE Id = 1;

SELECT COUNT(*) AS CaseSettingCount FROM CaseOpeningCaseSettings;

SELECT Tier, TargetProfitBasisPoints, PriceRoundingPence
FROM CaseOpeningTierEconomySettings ORDER BY Tier;

SHOW COLUMNS FROM CaseOpeningPriceSnapshotItems
WHERE Field IN ('IsFallback','PriceSource','PriceMethod','SourceMarketHashName');
```

Expected defaults are `10300` for the global return multiplier and `7800` for USD-to-GBP conversion. There must be ten tier rows. The catalogue currently contains 82 containers, so the case settings count must be at least 82.

## Controlled price import

1. Open the administrator Case Opening control centre.
2. Confirm the global return multiplier, Skin sale rate, and CSFloat USD-to-GBP conversion.
3. Create one new market snapshot. Do not retry repeatedly if a provider is unavailable.
4. Review Skinport, CSFloat, inferred, missing, and inherited-price counts.
5. Filter every `Missing prices` and `Needs review` row and resolve it before publication.
6. Confirm all ten tiers contain containers, Kilowatt remains in Tier 1, and recommended prices rise coherently with expected sell value.
7. Save any deliberate tier-curve adjustments, then reload the snapshot report.
8. Publish balanced prices only when the control centre reports `Ready` and the publish action is enabled.

## Smoke checks after publication

- Open Kilowatt through the free/starter flow and confirm it has no purchase or unlock charge.
- Buy and open one low-tier non-starter container in the active economy mode.
- Confirm Stars and GBP display the same intended value convention (`£100 = 100 Stars`).
- Sell a priced skin and verify the award uses its snapshot value and configured sale rate.
- Confirm search, tier filtering, and quality filtering work in the admin balance table.
- Confirm a normal player cannot access the administrator pricing endpoints.
- Watch application logs for provider, stored-procedure, or snapshot-price lookup failures.

## Rollback

- Do not delete the new snapshot first. Activate the previously recorded snapshot so price reads return to known data.
- Restore the backed-up game, tier, and case settings if balanced prices were published.
- Redeploy the previous application build only after its expected database procedures and columns have been confirmed compatible.
- Retain the failed snapshot for diagnosis unless it contains invalid or sensitive data; inactive snapshots do not affect openings.
- Re-run the smoke checks against the restored snapshot and settings.

The prestige multiplier remains deliberately outside base snapshot prices. A future prestige phase should apply it at reward time so rollback never requires rebaking market data or case prices.

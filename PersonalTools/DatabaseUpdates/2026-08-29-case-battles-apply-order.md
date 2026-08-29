# Case Battles database update

Run these files once, in this exact order, against the `PersonalTools` MariaDB database:

1. `2026-08-29-case-battles-foundation.sql`
2. `2026-08-29-case-battles-phase-1-integrity.sql`
3. `2026-08-29-case-battles-phase-5-operations.sql`
4. `2026-08-29-case-battles-phase-6-invitations.sql`
5. `2026-08-29-case-battles-phase-7-battle-bot.sql`
6. `2026-08-29-case-battles-phase-8-feature-visibility.sql`
7. `2026-08-29-case-battles-phase-9-invitation-query-fix.sql`
8. `2026-08-29-case-battles-phase-10-bot-parity.sql`
9. `2026-08-30-case-battles-phase-11-case-key-collation.sql`
10. `2026-08-30-case-battles-phase-12-roll-staging-collation.sql`
11. `2026-08-30-case-battles-phase-13-buy-all-unlock-gate.sql`
12. `2026-08-30-case-battles-phase-14-buy-all-quantity-qualification.sql`

The scripts create and replace procedures deliberately, so do not mix them with older copies of the same Case Battles procedures. The bot is inserted disabled; enable it from the Case Battles admin page only after the scripts finish successfully.

# Changelog

## [0.7.0] - 2026-06-28

### Changed
- Reworked the UI toward a cleaner gaming-magazine/app layout inspired by Amin-style structure, without copying template assets.
- Reduced the oversized hero typography and removed the giant empty status board feeling.
- Replaced the spread-out Light/Dark/Variation buttons with one tidy appearance dropdown.
- Rebuilt the header so navigation no longer looks like loose buttons spread across the page.
- Changed the first screen into a focused lead area with compact stats and top picks.
- Reduced default home volume to 12 free drops and 8 best deals; more items remain behind explicit “more” links.
- Reworked responsive breakpoints for desktop, tablet and mobile.

### Notes
- No database migration is required from v0.6.0 to v0.7.0.
- This release changes Razor/CSS/JS and one query-limit setting only.

## [0.6.0] - 2026-06-28

### Changed
- Reworked the home page into a proper app-style layout instead of a huge wall of cards.
- Default Free drops list now shows 24 items; “Περισσότερα free drops” expands to 96.
- Default Best Deals list now shows 12 items; “Περισσότερα deals” expands to 48.
- Split highlight cards into Best free drop and Best paid deal.
- Replaced the previous giant deal rail with a readable deal list.
- Rewrote the main CSS for a cleaner command-center UI.

### Added
- Confirmation dialog before manual source refresh.
- Loading state while a scan is running.
- Dismissible app alert for refresh results.
- Clear Greek success/warning messages after refresh.

### Fixed
- Reduced excessive vertical scrolling after CheapShark deal imports.
- Prevented paid deals from visually overwhelming the free giveaway board.
- Updated footer/project version to 0.6.0.

## [0.5.0] - 2026-06-28

### Added
- Separate **Best Deals** section for paid discounted PC deals.
- CheapShark paid deal import using `OfferType = Deal`.
- Dashboard deal count.
- Light/Dark theme switcher.
- Theme variations: Arcade, Cyber, Toxic, Sunset.
- Persistent frontend theme settings via `localStorage`.
- `wwwroot/js/theme.js` for theme controls.

### Changed
- CheapShark is now configured with `upperPrice=25` instead of `upperPrice=0`, so it can power both free-to-keep and paid Best Deals.
- Main offer wall now focuses on free drops/giveaways, while paid deals live in their own board.
- Header now includes theme controls and a Best Deals shortcut.
- Footer version updated to v0.5.0.
- Project version updated to 0.5.0.

### Notes
- No database reset required from v0.4.0.
- Run a refresh after updating so new paid deals are imported.
- Ratings remain source-based only; no synthetic review scores are generated.

## [0.4.0] - 2026-06-28

### Added
- Design reset with a more distinctive game-launcher / arcade-radar visual identity.
- New hero copy: official claim links, store/platform filters and public review data without fake scores.
- New console-style hero panel with live board readout.
- New ticket-style offer cards.
- New source dock for store/platform quick filters.
- New original SVG assets: `game-racing-heat.svg`, `game-cyber-rogue.svg`, `game-tactical-grid.svg`.
- Reworked fallback cover artwork.

### Changed
- Replaced the previous generic neon dashboard feel with a sharper, asymmetric, full-width arcade UI.
- Reworked logo, mark and favicon.
- Header now includes the “official free drops · no account access” brand tag.
- Footer version updated to v0.4.0.
- About page updated with the v0.4 design notes.

### Notes
- No database/backend changes.
- No migration required from v0.3.0.
- Decorative game images are original SVG assets, not copyrighted game artwork.

## [0.3.0] - 2026-06-28

### Added
- New FreeGameRadar SVG logo and mark.
- SVG favicon.
- New `wwwroot/assets` structure for brand and visual assets.
- Original gaming-themed SVG hero artwork.
- Custom fallback cover artwork for offers with no source image.
- Best Deal panel now includes the actual offer cover image when available.

### Changed
- Main layout is now full-width from edge to edge instead of centered in a narrow container.
- Header, page and footer use responsive gutters rather than max-width centering.
- Hero section is more lively/gaming themed with neon cover cards, radar ring and stronger visual hierarchy.
- Offer grid now uses `auto-fill` cards to take advantage of large screens.
- Filters are presented as a wider dashboard bar.
- Offer cards now have hover depth, image zoom and stronger badges.
- Footer version updated to v0.3.0.

### Notes
- No database migration is required from v0.2.1 to v0.3.0.
- Decorative game images are original SVG assets, not copyrighted game artwork.

## [0.2.1] - 2026-06-28

### Fixed
- Fixed SQLite migration order when upgrading from v0.1 to v0.2.
- The `best_deal_score` column is now added before the `idx_game_offers_best_deal` index is created.
- Existing v0.1 databases no longer crash at startup with `SQLite Error 1: no such column: best_deal_score`.

## v0.2.0 - Store/platform browsing and deal quality

### Added
- Store filter and quick store chips.
- Platform filter and quick platform chips.
- Sort modes: expiring first, best deal first, newest, store.
- Best Deal card at the top of the dashboard.
- Best Deal score field stored in SQLite.
- Review metadata fields in SQLite.
- Steam rating percent/count display when available from CheapShark.
- Metacritic score display when available from CheapShark.
- CheapShark deal rating display when available.
- Startup migration for v0.1 databases via `ALTER TABLE`.

### Changed
- Main hero copy now explains that free offers are ranked, not blindly trusted.
- Cards now show score, platform and quality/review summary.
- CheapShark adapter parses more metadata.
- GamerPower adapter normalizes platform labels.

### Notes
- GamerPower does not provide normal game review scores in the giveaway payload, so v0.2 explicitly marks those offers as having no public rating from that source.
- Best Deal score is a heuristic, not a critic score.

## v0.1.0 - Initial MVP

### Added
- ASP.NET Core 8 Razor Pages app.
- SQLite persistence with raw SQL repository.
- Source adapter architecture.
- GamerPower source adapter.
- CheapShark source adapter for free deals.
- Manual refresh button.
- Scheduled background refresh.
- Dark responsive UI with cards, stats and filters.
- Dockerfile and docker-compose.yml.
- Basic health endpoint at `/health`.

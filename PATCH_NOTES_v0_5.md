# FreeGameRadar v0.5 patch notes

Patch target: **v0.4.0 → v0.5.0**

## Added

- Separate **Best Deals** section for paid discounted PC deals.
- CheapShark paid deals import with `OfferType = Deal`.
- Dashboard deal count.
- Light/Dark theme support.
- Theme variations: Arcade, Cyber, Toxic, Sunset.
- Frontend theme persistence through `localStorage`.
- New `wwwroot/js/theme.js`.

## Changed

- CheapShark URL now uses `upperPrice=25` instead of `upperPrice=0`.
- Main offer wall focuses on free/giveaway items.
- Paid deals appear in their own board.
- Header includes a Best Deals shortcut and theme controls.
- Project/footer version updated to `0.5.0`.

## Database

No reset or migration required. v0.5 reuses the existing v0.2+ schema.

After patching, run the app and click **Σάρωση τώρα** once so paid deals are imported.

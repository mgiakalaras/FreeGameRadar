# FreeGameRadar v0.6 patch notes

Patch target: **v0.5.0 → v0.6.0**

## Main purpose

Fix the UX regression where the page became an endless card wall after adding Best Deals.

## Changed files

- `src/FreeGameRadar.Web/Pages/Index.cshtml`
- `src/FreeGameRadar.Web/Pages/Index.cshtml.cs`
- `src/FreeGameRadar.Web/Pages/_Layout.cshtml`
- `src/FreeGameRadar.Web/Pages/About.cshtml`
- `src/FreeGameRadar.Web/wwwroot/css/site.css`
- `src/FreeGameRadar.Web/wwwroot/js/theme.js`
- `src/FreeGameRadar.Web/FreeGameRadar.Web.csproj`
- `README.md`
- `CHANGELOG.md`
- `docs/ROADMAP.md`

## Database

No migration. No reset. Existing SQLite database is reused.

## UX changes

- Adds scan confirmation dialog.
- Adds loading state while scanning.
- Adds proper success/warning alerts.
- Limits initial free drops and paid deals so the page behaves like an app, not a dump.

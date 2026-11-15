# FreeGameRadar v0.7.0

Self-hosted tracker για νόμιμα free game giveaways και σοβαρά discounted PC deals.

Η v0.7 είναι UI correction release: πιο κοντά σε gaming magazine/app, με καθαρό πρώτο screen, μικρότερο hero, μαζεμένο navigation και εμφάνιση μέσα σε dropdown.

## Τι κάνει

- Μαζεύει official giveaway/deal links από public πηγές.
- Ξεχωρίζει **Free drops** από **Best Deals**.
- Δείχνει score, store, platform, τύπο offer και review metadata όπου υπάρχουν public δεδομένα.
- Δεν ζητά login.
- Δεν κάνει auto-claim.
- Δεν αποθηκεύει λογαριασμούς store.

## Sources

- GamerPower public giveaways API
- CheapShark public deals API

## v0.7 highlights

- Πιο μοντέρνο gaming-magazine layout.
- Hero χωρίς τεράστια γράμματα που τρώνε όλη την οθόνη.
- Compact radar overview αντί για άκυρο τεράστιο status box.
- Navigation πιο τακτοποιημένο.
- Light/Dark και variations μέσα σε ένα dropdown **Εμφάνιση**.
- Default προβολή: 12 free drops και 8 best deals για να μη γίνεται scroll-μαρτύριο.
- Καλύτερα responsive breakpoints για desktop/tablet/mobile.

## Run locally

```bash
dotnet run --project src/FreeGameRadar.Web
```

Default URL:

```text
http://localhost:5000
```

## Docker

```bash
docker compose up -d --build
```

Default Docker URL:

```text
http://localhost:8088
```

## Data

SQLite database path:

```text
src/FreeGameRadar.Web/App_Data/freegameradar.db
```

No reset is required when upgrading from v0.6 to v0.7.

## Notes

Best Deal score is a heuristic. It ranks offers using public fields such as price, savings, Steam rating, Metacritic score and CheapShark deal rating where available. If a source does not provide review data, FreeGameRadar does not invent it.

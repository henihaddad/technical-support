# Notes

## What I did

**Setup:** Cloned repo, updated packages, ran workspace via Docker, imported endpoints, ran data connector, deployed frontend, configured search indexes.

**STAPI Integration:**
- Ingested 3 entity types from https://stapi.co/: Species (759), Organizations (578), Characters (1000)
- Modeled relationships: Species ↔ Characters, Organizations ↔ Characters
- Built 2 UI views (Species, Organizations) with search + navigation to related characters
- Added Character renderer with species/org links and graph visualization

## How to run

1. Prerequisites: .NET 10, h5-compiler, Curiosity.CLI, Docker
2. `docker run -d -p 8080:8080 -v ~/curiosity/storage:/data/ -e storage=/data/curiosity -e MSK_DISABLE_RESOURCE_MONITORING=true curiosityai/curiosity:64641`
3. Log in at localhost:8080 (admin/admin), import endpoints, create tokens
4. Update tokens in `data-connector/Properties/launchSettings.json` and `custom-front-end/TechnicalSupport.FrontEnd.csproj`
5. `cd data-connector && dotnet run && cd ..`
6. `dotnet build custom-front-end/`
7. Add Full Text Search indexes for Character, Species, Organization (Name field)

## Issues I ran into

- **Summary → CaseSummary**: Package update made `Summary` a reserved field. Renamed across all files.
- **launchSettings.json**: Had old hardcoded tokens that override env vars. Had to update them directly.
- **Relative paths**: Data connector needs to run from its own directory (`cd data-connector`).
- **Docker OOM**: Container crashed importing endpoints. Fixed by increasing Docker memory.

## Trade-offs

- Limited to 1000 characters (out of 7,571) because getting species/org relationships requires a detail API call per character
- Built description strings from boolean flags for readability (e.g. "Humanoid, Telepathic")
- Ingestion is idempotent via `TryAdd()`

## Known gaps

- Not all characters ingested
- No graph tab on Species/Organization views
- No custom endpoint for STAPI data

## With more time

- Ingest all characters with parallel requests
- Add graph visualization to all renderers
- Add semantic search via Sentence Embeddings
- Build a "find similar characters" endpoint

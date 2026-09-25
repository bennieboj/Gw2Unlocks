---
name: download-wiki-pages
description: 'Downloads Guild Wars 2 wiki pages as MediaWiki Special:Export XML fixtures (single or multi-page, current revision only) via curl. Use when asked to "download wiki pages", "export pages", "fetch a wiki fixture", "get Special:Export output", or when a WikiProcessing integration test needs new pages in src/Gw2Unlocks/WikiProcessing/Gw2Unlocks.WikiProcessing.IntegrationTests/xmlfiles/.'
---

# Download Wiki Pages

Fetches one or more pages from <https://wiki.guildwars2.com> as a single `<mediawiki>` export XML
document — the exact format expected by the WikiProcessing integration test fixtures
(`SetFile("...")` → `xmlfiles/<name>.xml` → `ReadXmlPages`).

The curl command below is the direct equivalent of `Gw2WikiSource.DownloadExportBatch`
(`src/Gw2Unlocks/Wiki/Gw2Unlocks.Wiki.Implementation/Gw2WikiSource.cs`): HTTP POST,
`application/x-www-form-urlencoded`, `pages` joined with `%0D%0A`, `curonly=1`, `wpDownload=1`.

## When to Use This Skill

- A WikiProcessing test needs a fixture with real wiki page content.
- Refreshing an existing fixture to the current revision of a page.
- Any request to export/download GW2 wiki pages as XML.

## Prerequisites

- `curl` available on PATH (Git Bash / MSYS2 on Windows provides it).
- Exact page titles, case-sensitive, as shown on the wiki
  (first letter capitalized; apostrophes are part of the title).

## Download Command

Run from the repository root. Replace the page list and output path as needed.

```bash
PAGES=$(printf "%s\r\n%s\r\n%s\r\n%s" \
  "Page Title One" \
  "Page Title Two" \
  "Page Title Three" \
  "Page Title Four")

curl -sS -X POST "https://wiki.guildwars2.com/wiki/Special:Export" \
  --data-urlencode "title=Special:Export" \
  --data-urlencode "pages=$PAGES" \
  --data-urlencode "curonly=1" \
  --data-urlencode "wpDownload=1" \
  -o "src/Gw2Unlocks/WikiProcessing/Gw2Unlocks.WikiProcessing.IntegrationTests/xmlfiles/<fixture-name>.xml"
```

Parameter notes (mirroring the dotnet code):

| Param | Value | Meaning |
|-------|-------|---------|
| `title` | `Special:Export` | required by the export form |
| `pages` | titles joined by `\r\n` | `%0D%0A` after form encoding — multiple pages in one document |
| `curonly` | `1` | current revision only (no history) — fixtures should pin the live format |
| `wpDownload` | `1` | triggers download mode; content is the raw export XML |

For a single page, use `printf "%s" "Page Title"` (no separator).

## Verify the Result

1. The file starts with `<mediawiki xmlns="http://www.mediawiki.org/xml/export-0.11/"`.
2. Each requested page appears as `<page><title>...</title>...` — grep for titles:
   `grep -c "<title>" <fixture-name>.xml`
   A `404`/error HTML page or an empty `<text/>` means the title was misspelled; re-check
   exact capitalization and apostrophes (`'` vs `’`).
3. The wikitext inside `<text ...>` contains the templates under test (e.g. verify the new
   `{{account unlocks table}}` markup is present when that is the point of the fixture).
4. Fixtures are committed under `xmlfiles/`; name them after the scenario
   (e.g. `SerpentWrathWeaponChoiceBox.xml`), not after individual pages.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Output is HTML instead of XML | POST failed — check network / the `title` param is present |
| A page missing from the export | Title typo; export silently skips unknown titles |
| `curl: (3) URL rejected` | Unbalanced quotes around titles containing apostrophes |
| Encoding looks wrong | Keep `\r\n` separator; do not URL-encode titles manually — `--data-urlencode` handles it |

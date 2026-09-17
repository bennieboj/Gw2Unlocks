# AGENTS.md

Entry point for AI coding agents working in this repository. Paths are relative to the repository root.

## Required reading

- `.github/PROJECT_STRUCTURE.md` — architecture, pipeline order, directory layout and conventions. Read it before making changes.

## Skills

Reusable skills live in `.agents/skills/`. Load the relevant `SKILL.md` before doing that kind of work:

- `.agents/skills/debugging-workflow/SKILL.md` — investigating unlock classification regressions (Cache Inspector evidence, test-first validation, fix-approval gates).
- `.agents/skills/make-skill-template/SKILL.md` — creating new skills under `.agents/skills/<skill-name>/`.

## Critical rules

- Run tests from `src/Gw2Unlocks` so its `global.json` selects the runner: `dotnet test --solution Gw2Unlocks.slnx`.
- Do not open, search or parse files under `src/cache-root/` directly. Use the `Gw2Unlocks.CacheInspector.Host` CLI or interface-backed tests (see the debugging-workflow skill).
- Do not regenerate or modify cached data without explicit approval.
- Run pipelines in dependency order: Cache Updater -> Wiki Processing -> Unlock Classifier -> Website Generator.

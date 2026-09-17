---
name: debugging-workflow
description: 'Skill to identify incorrect changes via unit tests and graph analysis. Use when unlock classifications fail or need validation.'
---

# Debugging Workflow

Investigate user-reported unlock classification regressions one at a time using a test-first workflow. The user decides which changes are incorrect; an older result is evidence, not automatically the correct expectation.

## When to Use This Skill

- Unlock classifications change unexpectedly, disappear, or need validation.
- A previous acquisition path or graph link appears to be missing.
- Classifier or wiki-processing regressions need tests and root-cause analysis.

## Prerequisites

All paths below are relative to the repository root. Start command examples from that root unless explicitly stated otherwise.

Read `.github/PROJECT_STRUCTURE.md` first, then verify paths and commands against current source and configuration. Use .NET and existing xUnit conventions, not Node.js tooling.

Tests must execute from `src/Gw2Unlocks` so its `global.json` selects the runner. This PowerShell example starts at the repository root and restores that working directory afterward:

```powershell
Push-Location 'src/Gw2Unlocks'
try {
    dotnet test --solution 'Gw2Unlocks.slnx'
} finally {
    Pop-Location
}
```

For focused runs, use `dotnet test --project` inside the same working-directory block, with a test project path relative to `src/Gw2Unlocks`. Consult runner help before adding filters; do not assume VSTest flags work. Inspect project files: `*.Testing` projects may be helper libraries, not test suites.

## Cache Inspection: Cache Inspector First

- Use `Gw2Unlocks.CacheInspector.Host` for cached API records, wiki pages, graph nodes/links and zones before writing diagnostic tests. It uses `AddJsonCacheApiSource()`, `AddCacheWikiAsSource()` and `AddJsonCacheWikiProcessingSource()` with the read/source interfaces. Do not register live sources or change shared cache implementations for inspection.
- Do not directly open, search, slice or parse cache files with file tools, shell commands or custom scripts. Running the inspector CLI is the supported exception to the former tests-only rule: it reads through the existing application interfaces.
- Agents must use explicit CLI commands, not the interactive menu. Humans can run without arguments for the equivalent Spectre.Console menu.
- For classification results and unsupported datasets (classifier cache and sprite-sheet data), use relevant tests with existing DI/source interfaces. Do not add ad hoc file reads or custom cache parsers to tests.
- Inspector output is diagnostic evidence, not a regression test or proof that classification is correct. Verify unlock identity and graph node type, then add/run focused assertions for the user's expected behavior.
- Do not regenerate or modify shared caches without approval. Cache paths below are dependency references, not permission to inspect file contents directly.

### Commands

PowerShell examples, run from the repository root; replace the sample ID/title with the reported unlock. Build once, then use `--no-build` for repeated queries; rebuild after inspector code changes.

```powershell
$project = 'src/Gw2Unlocks/Gw2Unlocks.CacheInspector.Host/Gw2Unlocks.CacheInspector.Host.csproj'
dotnet build $project --tl:off
dotnet run --no-build --project $project -- --help
dotnet run --no-build --project $project -- lookup Achievements 1 --details
dotnet run --no-build --project $project -- lookup AchievementCategories 1 --details
dotnet run --no-build --project $project -- lookup Achievements Centaur --search
dotnet run --no-build --project $project -- lookup Wiki 'Mini Dolyak' --details
dotnet run --no-build --project $project -- lookup Graph 'Mini Dolyak' --details
dotnet run --no-build --project $project -- links 'Mini Dolyak' --depth 3
dotnet run --no-build --project $project -- links 'WvW Season One Reward Chest (Unlocked)' --incoming --depth 1
```

Datasets: `Achievements`, `AchievementCategories`, `Items`, `Skins`, `Miniatures`, `Novelties`, `Titles`, `Wiki`, `Graph`, `Zones`. API queries accept exact IDs or case-insensitive names; other queries accept names/titles. `--search` matches name substrings; `--details` prints full records (XML for wiki pages).

Example observed output from `lookup AchievementCategories 1`:

```text
1: Slayer
Found 1 match(es).
```

### Interpret the evidence

- Start with a name search when identity is uncertain; use returned IDs and exact graph keys for subsequent queries. Inspect graph `--details` for node type and metadata, not just presence.
- `links` follows outgoing graph edges by default; `--incoming` follows the reverse direction. Output includes edge types/metadata and two-space indentation per level. Incoming output describes reverse traversal, not a reversed stored edge.
- Begin with a small `--depth`; increase it or omit it for all reachable links before claiming a path is absent. `[depth limit]`, `[cycle]` and `[already expanded]` mark traversal boundaries, not missing data.
- Cached graph relationships are not every raw wiki hyperlink. Graph redirects are not persisted; inspect the cached wiki page and relevant parser when identity resolution is suspect.
- Exit code 0 means success/matches; 1 means not found; 2 means an error (CLI validation may use another nonzero code); handled cancellation returns 130. Record the command and relevant output, not just the exit code.
- Missing JSON files inherit the loader's empty-data behavior. A not-found result alone cannot distinguish an absent record from missing/incomplete cache inputs. Check prerequisites through the existing loader/test path; do not refresh data to hide the problem. Wiki scans can take several seconds.
- Restart CLI queries after approved cache regeneration rather than trusting a long-lived interactive session. Existing sources may retain loaded data in memory.

## Approval and Evidence Rules

- Before fix approval, inspect source code, inspect cached data through the Cache Inspector or existing interface-backed tests, run existing tests, and add/run regression tests with isolated fixtures.
- Explain the root cause and proposed fix for EACH issue and wait for explicit approval before changing production code, configuration, or cached/generated data. Production logging changes also require approval.
- Inspect test side effects; isolate fixture writes and do not regenerate shared caches without approval.
- Preserve local edits and failing inputs. Do not reset files, switch branches, delete caches, refresh sources or run pipeline hosts merely to investigate.
- Preserve relevant old/current evidence before approved regeneration. Never manually patch the production graph just to make a test pass.

## Step-by-Step Workflow

### 1. Collect and queue incorrect changes

Ask for a list if none was supplied. Accept a pasted diff, file, or plain-language list. Establish each unlock's name, type/ID when available, actual classification or absence, expected group/category or presence, and relevant current/previous run or artifact. Ask for supporting wiki pages or acquisition routes only as needed.

Ask only for missing information needed to investigate. Keep an ordered checklist: pending, investigating, awaiting approval, fixed, deferred, or blocked. Work on one issue at a time; do not silently skip an approval gate.

### 2. Establish the test baseline

Run all existing unit and integration tests in the solution before fixes. Record command, working directory, passed/failed/skipped counts and pre-existing failures. Separate unrelated failures from the reported issue.

Build errors, runner errors, missing caches, timeouts and zero discovered tests are validation blockers, not passing tests or reproduced bugs. Capture errors and try a focused project run if useful; do not repeat unchanged failing commands. Report blockers and label further read-only investigation or test work as unverified.

### 3. Reproduce the issue (red)

Search existing tests first. Reuse one if it fails for the reported reason; otherwise add a minimal regression test asserting the user's expected behavior. Use Cache Inspector lookups and bounded graph traversal to establish cached identity, node type and relevant links before constructing the reproduction. For unsupported queries, use a focused test with the existing loading path; never parse cache files directly.

Follow existing xUnit `[Fact]`/`[Theory]`, async, DI, fixture and test-output patterns. Prefer deterministic fixtures/fakes for the smallest affected layer. Record dependencies when using cache-backed integration tests; do not call them isolated unit tests.

Assert expected group/category and unlock identity for classification changes, explicit presence for removals, and relevant nodes, directed typed edges and metadata for graph regressions. Never weaken assertions to match incorrect output.

Run the test before a fix and confirm failure for the intended reason. If it passes, refine the reproduction. If blocked, explain what evidence is missing.

### 4. Trace the root cause

Trace backwards: classifier result -> rules/traversal -> processed graph and zone data -> cached wiki/API source. Use `ClassifyUnlocks` through `ClassifierIntegrationTests` for classification evidence. Use the inspector's `lookup Graph --details`, `links`, `lookup Zones`, `lookup Wiki --details` and API lookups (with the required query arguments) for cached-data evidence. Inspect intermediate nodes in both directions where useful, then protect the finding with directed typed-edge and metadata assertions in `GetAcquisitionGraphTests` or another focused interface-backed test. Do not open, search or parse cache files directly.

Compare old/current evidence where available. Trace intermediate nodes, edge direction/type, metadata and identity mapping. Identify the first broken relationship or changed decision rather than merely noting absence.

Investigate these possibilities without assuming any is proven:

- Missing node, edge or intermediate link disconnects the previous acquisition route.
- Renamed pages, redirects, changed IDs, normalization or node types break identity matching.
- Wiki markup changes, parser behavior, exclusions or metadata extraction omit relationships.
- Stale, incomplete or mismatched API/wiki/processed inputs change the graph or results.
- Rule precedence, traversal, filtering, deduplication, currency/vendor/zone logic or output mapping changes results despite an intact graph.
- A legitimate source change invalidates the old classification; discuss it instead of forcing the old result.

Use existing logs, test output or debugger inspection before proposing production instrumentation. If historical data is unavailable, state that historical link loss cannot be confirmed. Add a lower-layer fixture regression when needed to protect the actual cause.


### 5. Explain and request approval

Present the current issue's expected/actual result, reproduction test and outcome, causal evidence, remaining uncertainty, minimal proposed fix, affected files and any required data regeneration. Explain potential effects on other unlocks.

STOP and ask for approval before implementing that fix. Approval is per issue, not blanket approval for the queue. If declined, leave production inputs unchanged and mark the issue deferred. If more investigation is requested, remain within the pre-approval rules.

### 6. Apply the approved fix and verify (green)

Fix the responsible layer rather than hardcoding the reported unlock. Keep changes within the approved scope; seek renewed approval if the scope expands. For approved regeneration, preserve old inputs and run only necessary stages in dependency order: Cache Updater -> Wiki Processing -> Unlock Classifier -> Website Generator. Verify actual host commands in current workflows first; do not deploy or publish as a debugging side effect.

Run the reproducing test, related tests, then the full solution suite after each fix. Compare the affected classifier result with the user's expectation and check for collateral classification changes. If output regeneration was not approved or validation is blocked, report that limitation instead of claiming end-to-end success.

Keep the regression test and remove temporary diagnostics. Never hide unrelated baseline failures or report zero tests as success. Summarize the result, update the queue, and proceed to the next issue after this one is resolved or explicitly deferred.

## Per-Issue Report

- Unlock identity; expected vs actual result.
- Evidence: input/run versions, inspector commands and relevant output, nodes/edges and code decisions.
- Root cause: confirmed findings vs hypotheses.
- Regression test: path/name and pre-fix failure.
- Proposed fix: files, scope and regeneration needs; approval status.
- Verification: targeted/full-suite counts, output comparison and blockers.
- Queue status and remaining issues.

## Troubleshooting

| Symptom | Action |
|---------|--------|
| Test runner fails before discovery | Check working directory, `global.json`, SDK and package configuration. Record the blocker; do not change configuration without approval. |
| All existing tests pass but output is wrong | Add a reproduction for the missing case; passing tests do not prove untested output is correct. |
| Graph link is missing | Trace the source and parser first; rebuilding unchanged inputs is not inherently a fix. |
| Previous graph is unavailable | Request the relevant artifact if needed; distinguish current broken paths from unproven historical link loss. |
| Cache-backed test fails due to absent data | Report the input prerequisite or use isolated fixtures; do not refresh shared data without approval. |

## Repository References

Locations relative to the repository root:

- Classifier implementation: `src/Gw2Unlocks/UnlockClassifier/Gw2Unlocks.UnlockClassifier.Implementation/Classifier.cs`
- Classifier regression examples: `src/Gw2Unlocks/UnlockClassifier/Gw2Unlocks.UnlockClassifier.IntegrationTests/ClassifierIntegrationTests.cs` (cache-backed, supports classification by unlock name).
- Graph regression examples: `src/Gw2Unlocks/WikiProcessing/Gw2Unlocks.WikiProcessing.IntegrationTests/GetAcquisitionGraphTests.cs` (fixture-based node/edge assertions).
- Processed graph: `src/cache-root/wiki-processing/wikigraph.json`
- Raw wiki inputs: `src/cache-root/wiki-cache`
- API inputs: `src/cache-root/api-cache`
- Wiki Processing pipeline: `.github/workflows/2_wiki-processing.yml`
- Classifier pipeline: `.github/workflows/3_unlock-classifier.yml`
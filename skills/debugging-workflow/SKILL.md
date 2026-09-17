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

Read `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/.github/PROJECT_STRUCTURE.md` first, then verify paths and commands against current source and configuration. Use .NET and existing xUnit conventions, not Node.js tooling.

Run tests from `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/Gw2Unlocks` so its `global.json` selects the runner. With the current Microsoft.Testing.Platform configuration, run the full suite with:

```text
dotnet test --solution "C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/Gw2Unlocks/Gw2Unlocks.slnx"
```

Use `dotnet test --project` with the absolute test project path for focused runs. Consult runner help before adding filters; do not assume VSTest flags work. Inspect project files: `*.Testing` projects may be helper libraries, not test suites.

## Cache Inspection: Tests Only

- Never directly open, read, search, slice, deserialize or parse files in the cache to investigate their contents. This applies to every cache (wiki XML, API JSON, processed graphs, zone data and generated cache artifacts), not just `wikigraph.json`.
- Do not use shell commands, PowerShell, Python, Node.js, regex searches, file-reading tools or standalone scripts to inspect cache contents. Small excerpts and read-only commands are not exceptions.
- Start with the existing tests. Reuse a relevant test or add a focused diagnostic/regression test using the repository's existing DI, data-source interfaces, fixtures and test-output conventions. Let the normal application loaders read the cache; inspect the resulting objects, nodes, edges, metadata and classification results through that test's assertions, output or debugger.
- Do not bypass this rule by putting ad hoc file reads or a custom cache parser inside a test. Use the same loading path as the provided tests.
- For classification evidence, use `ClassifierIntegrationTests` and `ClassifyUnlocks`. For graph evidence, follow `GetAcquisitionGraphTests` or resolve `IGw2WikiProcessingSource` through the test service provider and call `GetAcquisitionGraph`. For other cached inputs, use the corresponding existing source interface through a test.
- Verify that the requested unlock name exists and has a classifiable node type through the test before attributing an empty result to traversal logic. Missing data is a prerequisite problem, not proof of a classifier bug.
- If no suitable test/loading path exists, explain the blocker and propose a test harness; do not fall back to direct cache parsing. Cache paths below are dependency references, not permission to inspect files directly.
- Run diagnostic tests and report observed evidence. Keep useful regression assertions and remove temporary diagnostics after investigation. Do not regenerate or modify shared caches without approval.

## Approval and Evidence Rules

- Before fix approval, inspect source code, inspect cached data only through tests as required above, run existing tests, and add/run regression tests with isolated fixtures.
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

Search existing tests first. Reuse one if it fails for the reported reason; otherwise add a minimal regression test asserting the user's expected behavior. Inspect relevant cached input data only through the existing test loading paths or a new focused test using those paths; never parse cache files directly.

Follow existing xUnit `[Fact]`/`[Theory]`, async, DI, fixture and test-output patterns. Prefer deterministic fixtures/fakes for the smallest affected layer. Record dependencies when using cache-backed integration tests; do not call them isolated unit tests.

Assert expected group/category and unlock identity for classification changes, explicit presence for removals, and relevant nodes, directed typed edges and metadata for graph regressions. Never weaken assertions to match incorrect output.

Run the test before a fix and confirm failure for the intended reason. If it passes, refine the reproduction. If blocked, explain what evidence is missing.

### 4. Trace the root cause

Trace backwards: classifier result -> rules/traversal -> processed graph and zone data -> cached wiki/API source. At every cached-data boundary, inspect only objects loaded through tests using the existing application data-source interfaces. Add targeted assertions or test output for the relevant relationships instead of opening, searching or parsing cache files.

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
- Evidence: input/run versions, relevant nodes/edges and code decisions.
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

Verified locations in this checkout; resolve equivalent absolute paths if the checkout moves:

- Classifier implementation: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/Gw2Unlocks/UnlockClassifier/Gw2Unlocks.UnlockClassifier.Implementation/Classifier.cs`
- Classifier regression examples: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/Gw2Unlocks/UnlockClassifier/Gw2Unlocks.UnlockClassifier.IntegrationTests/ClassifierIntegrationTests.cs` (cache-backed, supports classification by unlock name).
- Graph regression examples: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/Gw2Unlocks/WikiProcessing/Gw2Unlocks.WikiProcessing.IntegrationTests/GetAcquisitionGraphTests.cs` (fixture-based node/edge assertions).
- Processed graph: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/cache-root/wiki-processing/wikigraph.json`
- Raw wiki inputs: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/cache-root/wiki-cache`
- API inputs: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/src/cache-root/api-cache`
- Wiki Processing pipeline: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/.github/workflows/2_wiki-processing.yml`
- Classifier pipeline: `C:/Users/Ben/OneDrive/docs/Projects/Gw2Unlocks/.github/workflows/3_unlock-classifier.yml`
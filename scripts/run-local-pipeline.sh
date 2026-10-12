#!/usr/bin/env bash
# Local end-to-end pipeline, mirroring the CI PR flow:
#   1. Cache Updater      (fetch GW2 API + wiki, write api-cache/ + wiki-cache/)
#   2. Wiki Processing    (read wiki-cache/ + api-cache/, write wiki-processing/)
#   3. Unlock Classifier  (classify, write classifier-cache/ + diff report)
#   4. Commit gate: only commit when no unlocks moved or were removed.
#
# The website generator is intentionally not run: its output (wwwroot/) is
# gitignored, and CI's build_site revalidates on push.
#
# Usage: run from the repo root on the branch you want refreshed
# (e.g. `automated`), with a clean tree. Nothing is pushed; pushing and
# opening/updating the pull request stay manual. Needs `gh` only for the
# optional PR hint at the end.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

SOLN="src/Gw2Unlocks"
CACHE_UPDATER="$SOLN/CacheUpdater/Gw2Unlocks.CacheUpdater.Host"
WIKI_PROCESSING="$SOLN/WikiProcessing/Gw2Unlocks.WikiProcessing.Host"
CLASSIFIER="$SOLN/UnlockClassifier/Gw2Unlocks.UnlockClassifier.Host"

DIFF_DIR="${TMPDIR:-/tmp}/gw2unlocks-local-pipeline"
mkdir -p "$DIFF_DIR"
DIFF_REPORT="$DIFF_DIR/classifier-diff.md"
CLASSIFIER_LOG="$DIFF_DIR/classifier.log"
rm -f "$DIFF_REPORT" "$CLASSIFIER_LOG"

BRANCH="$(git branch --show-current)"
if [ -z "$BRANCH" ]; then
  echo "error: could not determine the current branch." >&2
  exit 1
fi

# A dirty tree would silently join the data commit, so require a clean one.
if [ -n "$(git status --porcelain)" ]; then
  echo "error: working tree is not clean; commit or stash first." >&2
  git status --short >&2
  exit 1
fi

echo "=== Local pipeline on branch '$BRANCH' ==="

echo "--- Step 1/3: Cache Updater ---"
dotnet run --no-launch-profile -c Release --project "$CACHE_UPDATER"

echo "--- Step 2/3: Wiki Processing ---"
dotnet run --no-launch-profile -c Release --project "$WIKI_PROCESSING"

echo "--- Step 3/3: Unlock Classifier ---"
# Release is not Development, so the classifier saves without prompting;
# stdin is closed anyway so a prompt can never hang the script.
CLASSIFIER_DIFF_REPORT="$DIFF_REPORT" \
  dotnet run --no-launch-profile -c Release --project "$CLASSIFIER" \
  2>&1 </dev/null | tee "$CLASSIFIER_LOG"

# Same source order as CI: the diff file first, the log as fallback.
summary="$(grep -o 'Diff summary: .*' "$DIFF_REPORT" 2>/dev/null | head -1 || true)"
if [ -z "$summary" ]; then
  summary="$(grep -o 'Diff summary: .*' "$CLASSIFIER_LOG" | head -1)"
fi
echo "Classifier result: $summary"

moved="$(echo "$summary" | sed -n 's/.* \([0-9][0-9]*\) moved.*/\1/p')"
removed="$(echo "$summary" | sed -n 's/.* \([0-9][0-9]*\) removed.*/\1/p')"
if [ -z "$moved" ] || [ -z "$removed" ]; then
  echo "error: could not parse moved/removed counts from: $summary" >&2
  echo "Cache files left uncommitted for inspection." >&2
  exit 1
fi

if [ "$moved" != "0" ] || [ "$removed" != "0" ]; then
  echo "NOT committing: $moved moved, $removed removed (adds never block)." >&2
  echo "Diff:" >&2
  sed -n '/Listing all diffs:/,$p' "$CLASSIFIER_LOG" >&2
  echo "Changed files (uncommitted, left for inspection):" >&2
  git status --short >&2
  exit 1
fi

echo "No moves or removals; committing src/cache-root."
git add src/cache-root
if git diff --cached --quiet; then
  echo "Nothing changed; nothing to commit."
else
  git commit -m "Local pipeline update $(TZ=Europe/Brussels date +%F)"
fi

echo
echo "Done on '$BRANCH'. Push yourself when ready, e.g.:"
echo "  git push origin $BRANCH"
echo "Pushing fires the cloud classifier check; step 4 (website build)"
echo "is validated there. No website output was generated locally."

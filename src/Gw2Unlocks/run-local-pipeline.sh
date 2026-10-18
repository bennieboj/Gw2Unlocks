#!/usr/bin/env bash
# Local end-to-end pipeline, mirroring the CI PR flow:
#   1. Cache Updater      (fetch GW2 API + wiki, write api-cache/ + wiki-cache/ + icon-sprite-sheet/)
#   2. Wiki Processing    (read wiki-cache/ + api-cache/, write wiki-processing/)
#   3. Unlock Classifier  (classify, write classifier-cache/; VS-style "y" prompt gates the save)
#
# The website generator is intentionally not run: its output (wwwroot/) is
# gitignored, and CI's build_site revalidates on push. Publishing stays
# main-only in the cloud deploy job.
#
# Each step is skipped when its outputs are already dirty (uncommitted changes
# under the relevant cache dir). To force a re-run, commit or discard that
# dir first, e.g. `git checkout -- ../cache-root/api-cache`.
#
# Nothing is ever committed or pushed; that stays manual. Run from anywhere on
# the branch you want refreshed (e.g. `automated`).
set -euo pipefail
trap 'rc=$?; if [ "$rc" -ne 0 ]; then
  echo
  echo "ERROR: Script failed with exit code $rc."
  read -r -p "Type confirm to close this window: " answer
  while [ "$answer" != "confirm" ]; do
    read -r -p "Type confirm to close this window: " answer
  done
fi' EXIT

cd "$(dirname "${BASH_SOURCE[0]}")"

CACHE_UPDATER="CacheUpdater/Gw2Unlocks.CacheUpdater.Host"
WIKI_PROCESSING="WikiProcessing/Gw2Unlocks.WikiProcessing.Host"
CLASSIFIER="UnlockClassifier/Gw2Unlocks.UnlockClassifier.Host"

BRANCH="$(git branch --show-current)"
if [ -z "$BRANCH" ]; then
  echo "error: could not determine the current branch." >&2
  exit 1
fi

# Dirty outputs mean the step already ran (or was hand-edited); skip it.
is_dirty() {
  [ -n "$(git status --porcelain -- "$@")" ]
}

echo "=== Local pipeline on branch '$BRANCH' ==="

if is_dirty ../cache-root/api-cache ../cache-root/wiki-cache ../cache-root/icon-sprite-sheet; then
  echo "--- Step 1/3: Cache Updater SKIPPED (outputs already dirty) ---"
  git status --short -- ../cache-root/api-cache ../cache-root/wiki-cache ../cache-root/icon-sprite-sheet
else
  echo "--- Step 1/3: Cache Updater ---"
  dotnet run --no-launch-profile -c Release --project "$CACHE_UPDATER"
fi

if is_dirty ../cache-root/wiki-processing; then
  echo "--- Step 2/3: Wiki Processing SKIPPED (outputs already dirty) ---"
  git status --short -- ../cache-root/wiki-processing
else
  echo "--- Step 2/3: Wiki Processing ---"
  dotnet run --no-launch-profile -c Release --project "$WIKI_PROCESSING"
fi

if is_dirty ../cache-root/classifier-cache; then
  echo "--- Step 3/3: Unlock Classifier SKIPPED (outputs already dirty) ---"
  git status --short -- ../cache-root/classifier-cache
else
  echo "--- Step 3/3: Unlock Classifier ---"
  # Development launch profile, like VS: the classifier prints the diff, then
  # asks "Press y to continue" before saving. stdin stays open so you answer.
  dotnet run --launch-profile Gw2Unlocks.UnlockClassifier.Host -c Release --project "$CLASSIFIER"
fi

echo
echo "Done on '$BRANCH'. Nothing committed or pushed; review, commit, and"
echo "push yourself when ready, e.g.:"
echo "  git status --short -- ../cache-root"
echo "  git add ../cache-root && git commit -m \"Local pipeline update \$(TZ=Europe/Brussels date +%F)\""
echo "  git push origin $BRANCH"
echo "Pushing fires the cloud classifier check; step 4 (website build)"
echo "is validated there. No website output was generated locally."

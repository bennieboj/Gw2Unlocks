# GW2 Unlocks

https://gw2unlocks.com/

## Development Guide
This section describes how to run and develop the **GW2 Unlocks** project locally.

### How it all works

There are 4 steps, each their own dotnet program host that are ran seperately.

Each program interacts with data in `src/cache-root`.
Each steps reads the data from the previous step (if any/needed) and updates the data in their own folder.
The data is stored locally in order to replay the processing in an easy way without overloading the server or slowing down the process.

1. CacheUpdater: stores all relevant data coming from GW2 API (json) and GW2 Wiki (xml) into `src/cache-root/api-cache` and `src/cache-root/wiki-cache`.
2. WikiProcessing: processes the data coming from GW2 Wiki into a graph and zone data in `src/cache-root/wiki-processing`.
3. UnlockClassifier: does the actual classification, produces a `ClassifyConfig`, containing all classified unlocks in `src/cache-root/classifier-cache`.
   - It will display the differences between the existing and the newly generated `ClassifyConfig` in order to see if the algorithm changes don't break anything.
   - Running locally will ask for confirmation
4. WebsiteGenerator: generates the static pages, one per `UnlockGroup` and `UnlockGroup/UnlockCategory` combination.
   - Running locally will run a local static files server `http://localhost:5000` with hot reload.
   - Running in production mode will just generate the static files, this will be used in the build pipeline.

Each program is  tested from a black box perspective, mainly using integration tests, using the data in the `src/cache-root`.


## Tools and libraries
- https://github.com/sliekens/gw2sdk
- https://github.com/cxuesong/WikiClientLibrary
- https://search.gw2dat.com/


## Legal
©2010–2026 ArenaNet, LLC. All rights reserved. Guild Wars, Guild Wars 2, Guild Wars 2: Heart of Thorns, Guild Wars 2: Path of Fire, Guild Wars 2: End of Dragons, Guild Wars 2: Secrets of the Obscure, Guild Wars 2: Janthir Wilds, Guild Wars 2: Visions of Eternity, ArenaNet, NCSOFT, the Interlocking NC Logo, and all associated logos and designs are trademarks or registered trademarks of NCSOFT Corporation. All other trademarks are the property of their respective owners.

---
Made with ![Love](/readme/love.png) and ![Technology](/readme/tech.png) in Tyria.
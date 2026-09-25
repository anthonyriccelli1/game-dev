# Stage A — Reliable interaction + recipes the player can read

Owner: implementation subagent. Reviewer/approver: orchestrator (Claude). Product owner: Anthony.
Scope is deliberately small. Do NOT add new dishes, stations, art packs, networking, or camera work in this stage.

## Why
1. Interaction is unreliable: the E prompt flips between targets/actions. Causes found in review:
   - `FirstPersonPlayer.TryResolveInteractionHit` casts a second "lower assist" ray; direct vs lower hit alternate frame to frame.
   - Prompt text is rebuilt every frame and embeds a live timer (`station.Progress.ToString`), so it visibly churns.
   - Pantry uses a hidden Q-cycled "choice"; Q is also discard. The prompt is decided in `PhysicalRestaurant.InspectPlayerRay`,
     the action in `KitchenModel.Act` — two separate code paths that can disagree.
2. The player has no way to know what a dish needs. Recipes are bitmasks (`Parts` 1/2/4/8, `RecipeOf`).
3. A burger takes too many presses (pantry→prep bench chop→grill→pantry bun→plate rack→assembly...).

## Requirements

### A1. One target, one action (fixes the bug)
- Single center ray (first-person) / existing elevated ray. Remove the lower-assist ray. Use a slightly larger
  interaction collider or a short SphereCast (radius ~0.15) if stations are hard to hit — but ONE query.
- Target stickiness: keep the current target until a different valid target is hit for >= 0.12 s, or nothing is hit for >= 0.2 s.
- Add a pure-model method in `KitchenModel` e.g. `KitchenAction Preview(GameState, actor, stationId, subId)` returning
  {verb, label, kind: Tap|Hold|None, failReason}. `Act`/`Work` MUST execute exactly what `Preview` returned
  (implement Act by calling Preview then applying it). The on-screen prompt comes only from Preview.
- Prompt format: one short line, e.g. `E  Add bun to plate` or `Hold E  Chop greens`. Gamepad shows `A` instead of `E`.
  No timers in text. Progress (cook/chop/wash) is a world-space bar or colored ring on the station.
- Q / B = drop/discard held item only. Remove the pantry choice cycle.

### A2. Pantry shelves instead of a hidden choice
- Keep the single `pantry` catalog item (no layout migration), but build it with one child collider per ingredient
  (patty, bun, greens, midnight sauce ingredients) each carrying a sub-id on `RestaurantTarget`.
  Looking at the bun shelf -> `E  Take bun`. Locked ingredient shows `Needs Midnight recipe`.
- Ingredient stock accounting (Protein/Produce) stays as today.

### A3. Recipes as data
- New `RecipeDefinition` data (plain C#, same pattern as `RestaurantCatalog.Dishes`; link to existing DishDefinition by id):
  id, display name, ordered list of required plate components (e.g. `bun`, `cooked_patty`), list of cookbook steps
  (short strings), and which prepared states come from which station.
- Replace the `Parts` bitmask with `List<string> Components` on plated items. Order-independent match.
  Save migration: on load convert old `Parts` bits (1 patty, 2 bun, 4 greens, 8 midnight sauce) to components. Bump a save version field.
- Initial recipes (keep existing dish ids):
  - burger: bun + cooked_patty. Raw patty goes straight on the grill (NO chopping step for protein).
  - salad: chopped_greens (greens chopped at prep bench, hold E).
  - midnight: bun + cooked_patty + midnight_sauce (sauce prepared at prep bench). Only when RecipeUnlocked.
  - soup / dessert: leave as-is / off the physical menu for now (Stage B).
- Fewer presses: while HOLDING a plate you can (a) E a pantry shelf of a plate-ready ingredient (bun) to add it straight
  onto the plate, (b) E a grill with a cooked patty to slide it onto the plate, (c) E the prep bench to take a finished
  prepped item onto the plate. Holding an item and E on a plate sitting on assembly still works.
  Target: burger = take patty, grill, (wait), take plate, bun shelf, grill, serve = 6 presses, no dead steps.
- Wrong/extra ingredient is refused with a clear reason from Preview ("Burger doesn't use greens").

### A4. Show the player what to make
- Ticket rail (top of each player's viewport; works in split-screen): one card per waiting order, oldest left:
  table/seat number, dish name, component list, patience bar (green->yellow->red). Max ~5 visible, "+N" overflow.
  Use existing UI approach (`PhysicalHud`/`RestaurantUI`); text + colored bullets are fine — art comes later.
- Held-plate checklist near the crosshair: dish being built (best matching waiting order) and each component with
  a check / missing mark. When a plate completes a recipe, show `Ready: Burger -> Table 3`.
- Cookbook: a "Cookbook" tab in the existing management panel listing every recipe (locked ones shown locked with
  how to unlock) with its components and steps; plus a wall board prop in the kitchen showing active-menu recipes.

### A5. Co-op safety (must keep)
- All mutations stay in KitchenModel with actor ids. Two players targeting the same station in the same frame must
  not duplicate items, plates, stock or money. Hold-to-work stays single-owner (`WorkOwner`).

## Verification (report evidence, not just "done")
1. Tests: extend `RestaurantTests`/`Tests` programs for: Preview==Act for every station/hand combination you touch;
   migration of old Parts saves; two actors racing for the last plate / same pantry shelf; recipe matching order-independence.
   Try to run them (install .NET SDK 9 in the device shell if allowed; otherwise say they were not run).
2. Unity compile + Windows build: the Editor-side `PrototypeBuildRequest` builds when a file `prototype-build.request`
   exists in the project root and writes `prototype-build.result`. Save first. If the Editor isn't open, report that.
3. Do not touch the user's real save; if a save format changes, keep backward-compatible load + backup (see SaveBackups/).
4. Commit on branch `stage-a-cooking-clarity` (do not merge). Update AGENTS.md: conductor/approver is now Claude (orchestrator).
5. Report: files changed, what you verified and how, what you could NOT verify, known risks.

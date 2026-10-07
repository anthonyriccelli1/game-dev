# Milestone: Old Market — "Know where to go" and a city worth exploring

**Owner:** Zeus (orchestrator). **Implementers:** Nox and other assigned agents. **Judge of feel:** Anthony.
**Read first:** `AGENTS.md`, `docs/plans/2026-10-02-old-market-demo-roadmap.md` (the chapter plan this milestone serves), `README.md`.

## Why this milestone

A first-time player (Anthony's fiancée) could not find her way. The goal text told her what to do ("buy ingredients from Rose at Milo's cart") but nothing told her *where that was*. The phone map is hard to read. Meanwhile some places look finished (The Alchemist, The Odd Table, Main Street shops, the food trucks) and others still look like a blockout, and The Alchemist sits alone with open sky above it while the rest of the block is a dense city.

**The milestone is done when:** a new player can play from a fresh save to owning The Odd Table and finding the midnight recipe on the North Avenue roof **without anyone telling them where anything is**, and every street they walk in Old Market looks deliberately built.

## Workstreams

Do them roughly in this order. Each numbered stage is a small runnable step: build it, verify it, commit it, then move on. Do not start a later stage on top of an unverified earlier one.

### A. Wayfinding: always know where the current goal is (highest priority)

1. **One place registry.** Every goal-relevant location gets one entry with an id, display name, short "where" phrase, world position and map icon (Little Flame, Rose / Milo's cart, Milo's Market, The Odd Table, Gus's lot, The Alchemist, Hock-9's pawn, the North Avenue fire escape, the harbour, the park, etc.). The current `Reputation.Places` list and hard-coded positions in goal text should read from it. No duplicated coordinates.
2. **Goals know their target.** Each step of `CityGame.Objective` names a target place id, not just text. Goal text uses the registry's display name plus its "where" phrase (for example "Rose at Milo's cart — right beside Little Flame in Truck Park").
3. **On-screen goal marker** (Schedule I style): a small, clean marker on the HUD that points at the current target with the distance in metres. When the target is behind you, the marker sits at the screen edge. It hides when you are at the target or inside a menu. It can be toggled off in settings. **It must work per player in two-player co-op** (each player's own view and marker).
4. **Map shows the way.** On the phone map: your position and facing (an arrow), your partner in co-op, the current goal pin, labelled landmarks from the registry, and a short legend.
5. **In-world signs.** Street name signs at intersections (pack street signs), and a few directional signs where new players get lost (Truck Park → Main Street, Main Street → Truck Park). Use pack signs and 3D pack letters, not floating text labels.

**Accept A when:** with goal text hidden, a tester can reach each of these from spawn using only the marker and the map: Rose's cart, The Odd Table, Milo's Market, Gus's lot, The Alchemist, and the North Avenue fire escape. Add an automated check that every objective step resolves to a valid place id and that the marker points the right way in one- and two-player modes. Screenshots: HUD marker on screen, marker at screen edge, the map open, and two-player split view.

### B. A map that looks like a real city map

Redraw the phone map so it reads at a glance:
- streets as streets, with their names;
- blocks as blocks;
- park, water and plaza areas tinted;
- icons by kind (your places, suppliers, rivals, services, gates);
- district/area names: Market Row, The Home Street, The Flats, The Harbour.

Keep it inside the existing phone panel. The style should match the game's warm, stylized look; no generic debug rectangles.

**Accept B when:** a before/after screenshot pair shows the new map is clearly more readable, and every registry place appears in the right spot.

### C. The Alchemist belongs to the city

Wrap The Alchemist in city architecture the way The Odd Table is wrapped (see `CityMap.RestaurantBuilding`): apartment storeys above the hall's roof, and neighbouring buildings closing the sides, so the street reads as one continuous city block. Constraints:
- The interior is untouched: double-height hall, lab balcony, sign, open kitchen, staff and guest routes, raid arena, anchors.
- The back courtyard stays open to the sky; it is reserved for the future worker rest chambers.
- The 3D sign stays fully visible from the street.

Then check other standalone gameplay buildings for the same problem.

**Accept C when:** day and night screenshots from Main Street show The Alchemist as part of a continuous block, and all Alchemist tests still pass (crew, raid, guests).

### D. Every Old Market street up to the Main Street standard

Main Street already shows the target, in `Assets/Editor/CityMainStreet.cs`:
- **Real storefronts:** ground-floor shops get real furnished rooms behind clear glass (Shops-pack props), a warm interior light, and a shop name in 3D pack letters.
- **Some closed shops:** some units are shuttered and "FOR LEASE".
- **One sidewalk rule:** trees in planted curb pits, lamps, bins and hydrants in the curb strip, and a clear walking lane against the shopfronts. No random pots, no trees mid-sidewalk, no awnings at head height.

Go block by block, in this order: the four Main Street corner buildings, Market Row / North Avenue, South Avenue / The Flats, East and West Streets, the streets around Truck Park, then the harbour. Give each street some character, but keep neon to at most one or two signs per block.

**Accept each block when:** it has before/after screenshots at street level, day and night. A walk-through at sidewalk level finds no blocked sidewalks, no props floating or sunk into the ground, and no glass showing the old painted fake interiors.

### E. Elevation and better Flux hiding spots

The game is too flat. Existing climbable fire escapes are in `Assets/Editor/CityClimb.cs`: a 30 m tower on North Avenue and a 12 m roof on South Avenue, with walkable colliders and an automated climb test. Extend that:
- **More climbs:** a few more climbable routes and connected rooftops (stairs and fire-escape stairs only, no ladders), plus a couple of spots that are "up a little" (balconies, low roofs).
- **Better hiding spots:** improve the 12 Flux hiding spots (`FluxHuntBuilder.cs`, names in `FluxHunt.Hints`). Use tucked-away, rewarding places across street level, mid-height and rooftops. Fix any spot that is trivial or awkward.
- **Keep the numbers:** 12 spots and 2 cases per night, unless Anthony approves a change. The midnight strongbox stays on the tallest roof.

**Accept E when:**
- Every new climb has a route that the automated test walks to the top with the normal movement code (copy the existing climb test in `PhysicalAcceptance.cs`).
- Every Flux spot is reachable on foot; add an automated reachability check.
- Screenshots of each new climb and its view from the top.

### F. Small cleanups (good first tasks)

- Remove the leftover **Tin Diner** rival: its map entry, the "Twin Moons" Cookbook entry that says "won by raiding The Tin Diner", and its diner signage (keep the buildings as ordinary shops).
- The night warning "Don't get caught carrying Zeeb's sauce" should only mention Zeeb once the player knows the midnight recipe.
- The old `PrototypeSmokeTest` still fights the removed alley guard and opens the removed stash: update or retire it.
- Update `claude/world-map.md` (project doc) to match the city as it is.

## Guardrails (do not cross without Anthony's approval)

**Game rules that must not change:**
- **Economy and progression numbers:** prices, wages, Flux amounts, star thresholds, crew cap (stars + 1), raid health/damage/rewards, Flux hunt counts.
- **Cooking mechanics:** flipping, chopping, scrubbing, assembly.
- **Rules Anthony has set:** no perks; recruits are permanent (no firing); the truck sells only burger and salad; Flux stays scarce.
- **Save data:** no format change without a migration that preserves existing saves, and Zeus review. Test with isolated saves, never Anthony's.

**Art direction:**
- Use only the Synty packs already in the project (POLYGON City, Shops, Generic) and Anthony's Tripo character and prop renders.
- No visible primitive or "blob" art: no coloured cubes, spheres or capsules standing in for objects people look at. Invisible colliders are fine.
- **Never recolour character textures in code.**
- If something needs a custom model, stop and write Anthony a one-paragraph render request (what it is, size, style, materials) instead of faking it.

**Don't break what works:**
- Keep gameplay anchors and reserved areas in place, or update everything that depends on them. These include:
  - The Alchemist layout (`AlchemistLayout`) and raid arena;
  - Flux spot and climb anchors;
  - The Odd Table interior;
  - Truck Park and Gus's lot;
  - the `Reserved` rects in `CityMap.cs`.
- Keep performance in check:
  - decorative lights have no shadows;
  - shop interiors use at most one light each;
  - check frame rate at night on Main Street after each block.

**Co-op:**
- Anything on the HUD must work with two local players on split screen.
- Controller input must keep working everywhere.

**Process (see `AGENTS.md`):**
- Small assignments with clear file ownership.
- One stage at a time: build, test, take screenshots, then commit.
- Never push, merge or publish without Anthony's OK.
- **Don't run builds while Anthony is playtesting:** a build overwrites the game he is running, so ask first.
- Report what is verified (automated test, screenshot, runtime check) separately from what still needs Anthony's hands-on playtest. Worker reports alone are not proof; Zeus checks the diff and the evidence.

## How to build and verify

Unity runs on Anthony's PC with the project open; agents talk to it through request files in the project root.

- **Build:**
  - Run `: > prototype-build.result; touch prototype-build.request` and wait about 3 minutes.
  - Expect `PASS: scene generated, validated, and Windows player built.`
  - If the request file is never consumed, Unity is closed: ask Anthony to open it.
- **Physical test suite:**
  - Run `echo RunPhysicalAcceptance > editor-command.request`.
  - It runs the built player, so build first.
  - Expect `PHYSICAL_RUNTIME_PASS <n>` in `Acceptance/physical.log`.
- **Interaction test suite:**
  - Run `echo RunInteractionAcceptance > editor-command.request`.
  - Expect `INTERACTION_RUNTIME_PASS ... 0 failures` in `Acceptance/interaction.log`.
- **Screenshots:**
  - Add shots to `SnapshotTour.Shots` (name, position, yaw, pitch, clock: 60 is day, 195 is night).
  - Put a name filter in `snapshot-only.txt`, then run `echo RunSnapshots > editor-command.request`.
  - Images land in `Snapshots/`.
- **Reusable editor commands** (static methods in `Assets/Editor/EditorTools.cs`):
  - `DumpPrefabSizes` and `RenderPrefabs` read `EditorOutput/render-list.txt`; use them to measure and preview pack props before placing them.
  - `DumpHeights` prints mesh heights.

## Definition of done (Anthony's playtest)

Anthony (and ideally someone who has never played) starts a fresh save and plays from Little Flame to:
1. owning The Odd Table;
2. climbing the North Avenue fire escape for the midnight recipe;
3. finding one Flux case at night.

They do this using only the HUD marker, the map and in-world signs, with nobody pointing the way. Every street they pass looks finished at walking level, by day and by night. Anthony signs off on feel; automated tests alone do not close this milestone.

## Out of scope for this milestone

- The Docks district.
- New rivals or recipes.
- The courtyard rest chambers (separate milestone).
- Economy rebalancing.
- Online play.
- Replacing stand-in Flux and recipe cases before Anthony's renders arrive.

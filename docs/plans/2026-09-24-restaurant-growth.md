# Restaurant ownership and growth — implementation plan

**Goal:** A repeatable 20–30 minute single-player session in which the player earns a restaurant, changes its appearance and operation, recruits staff, and works toward two stars.

**Approved direction:** The user explicitly requested this expansion and staged implementation after a short visual direction/plan. Retain first-person Unity PC gameplay and the existing city/stand/night encounter.

**Visual direction:** A cozy, scruffy sci-fi neighborhood diner: cream tile, teal enamel, coral upholstery, walnut wood, amber pendants, restrained neon. Layered original procedural furniture, worn plaster/wood textures, rounded silhouettes, expressive eyes, walking/seated/working animations. Eight customer silhouettes plus two workers. Record original art and Unity font sources in the asset ledger.

**Architecture:** A pure C# restaurant model owns the catalog, owned layout, dishes, stock, orders, customer traits, ratings/reviews, workers, and progression. A Unity controller handles placement, world interactions, navigation, presentation and menus. Procedural art factories create furniture and characters from stable IDs. Extend the existing save with versioned restaurant state and retain old stand progress.

## Stage 1 — buy and customize
1. Write/verify model tests for the $150 purchase, cash safety, 24 catalog entries, unlocks, overlap/door clearance, rotation, moving, selling and layout persistence.
2. Replace the future-restaurant block with a walk-in shabby diner and starter kitchen/table.
3. Add catalog categories, item descriptions/prices/locks, preview, grid placement, rotation, selecting/moving/selling, finishes and exterior upgrades. Pause service while customizing.
4. Run the Windows player, exercise the purchase and placement flow, capture initial-room and catalog evidence.

## Stage 2 — service, people and progression
1. Add eight customer archetypes, arrival/seating/queue/order/reaction/departure, clear preferences, visible demand and rushes.
2. Add five dishes including midnight burger, selectable menu, stock purchasing, parallel cooking capacity, cleanliness and staffing decisions.
3. Add two distinctive hires (cook and service/cleaning specialist), assignment, cost/effect UI, visible travel/work, reusable worker data.
4. Show persistent star rank, satisfaction, recent explanatory reviews and explicit two-star goals. Two stars unlock desirable equipment, menu and decor.
5. Run a service, test both staff roles and recipe integration, capture busy-service and upgraded-room evidence.

## Stage 3 — acceptance and delivery
1. Run deterministic and runtime tests from a new save, earning the purchase price through the stand rather than injecting cash into the acceptance run.
2. Verify repeated service continues, customer diversity, star progression, furnishings and staff changes, and recipe reward.
3. Test a disk save followed by a separate process loading the saved layout/ownership/menu/staff; isolate acceptance saves from user progress.
4. Inspect rendered evidence, build Windows player, keep reviewable commits per stage and report precisely what was automated versus manually playtested.

## Integration contract
- Catalog IDs: prep_bench, grill, stove, oven, fridge, stool_pair, cafe_table, booth_teal, booth_coral, communal_table, wall_cream, wall_teal, wall_rose, floor_checker, floor_wood, pendant_amber, globe_lamp, neon_moon, fern, art_orbit, rug_sunset, jukebox, awning_coral, sign_neon.
- Restaurant footprint: x=-16.5..-3.5, z=-22..-9; entrance at (-10,0,-9). Placement cells: 12 columns × 10 rows; center of cell (x,z)=(-15.5+x,-20.5+z). Keep entry/central access clear.
- Save retains old GameState fields and adds RestaurantState. No user save is used by automated tests.
- Original street loop remains independent while the restaurant is closed; open restaurant service suppresses duplicate stand orders.

# Physical kitchen playtest

Keyboard/mouse controls: WASD move, mouse look, E take/place/serve, hold E to chop or wash, Q change the pantry choice with empty hands or discard held food, V elevated camera, Tab closed management, B catalog (or edit the furniture you are aiming at), R rotate in placement. Escape pauses.

For two-controller local play, connect both pads. Press Start on the first pad to assign Player 1, then Start on the second pad to join Player 2. Press A on Player 1's pad to start the shift. Left stick moves; right stick looks; A takes/places/serves and can be held to work; X also works at a loaded station; B changes pantry choices or discards; Y changes camera; left shoulder opens closed management; D-pad up edits aimed furniture; RT taps punch (hold and release for a heavy hit), LT blocks, and RB flips a patty while aiming at the grill. Start pauses. Player 1 controls shared menus with the D-pad or left stick, A to select, and B to close. The pause screen offers persistent 220/300/400 degree-per-second look-speed choices; 300 is the default. View/Select opens the phone after the stand is built; the phone has the map. In overhead placement, move the cursor with the D-pad or left stick, press RB to rotate, A to place, and B to return to the catalog. Keyboard and mouse remain available for Player 1.

For kitchen minigames, aim at raw lettuce on the cutting board and press A. Move either stick sideways to line up the knife with each slice, then tap RT or RB six times; B puts the lettuce down without discarding progress. Aim at a dirty plate in the sink and press A, then move either stick across the visible grime until the plate is clean; B puts it down with partial progress saved. At the grill, wait for the underside to turn golden, aim at the grill, and tap RB to flip; press A to pick up the cooked patty when it is ready.

No worker is required to cook. The free starter kit includes pantry, prep bench, grill, clean plate rack, assembly counter and washing sink. Aim at the station for its prompt.

- Burger: take protein, place on prep, hold interact, take prepared patty, place on grill, wait 8 seconds, take cooked patty before 24 seconds. Put a clean plate on assembly, then add the cooked patty and a pantry bun. Take the assembled plate and serve the matching guest.
- Salad: prepare pantry greens and add them to a clean plate on assembly.
- Midnight bun: assemble a burger and add sauce prepared from pantry midnight ingredients. Earn the recipe from the nighttime rival first.
- Clear a dirty plate from a table, place it in the sink, and hold interact to wash. The clean plate returns to the shared rack. Six reusable plates exist; buying racks does not create more plates.

Open service from Tab management. Arrivals stop after two minutes; remaining guests can finish. Interact with the door desk to stop arrivals earlier. After the last guest leaves, the report shows sales, consumed ingredient value, wages, net, satisfaction, star change and staff energy. Lost guests include those who couldn't find a clean table. Losing a shift preserves your restaurant.

Ember can cook physical recipes, Moss can carry completed meals, and either can wash when assigned Wash. They use the same finite ingredients, plates and station slots as players. Resting restores energy; low energy slows work. If a worker waits at an occupied station, clear the blockage or help with the current dish.

Milo sells six produce for $6 or six protein for $10. Aim at his counter and change the purchase choice with Q/B. Purchases during service do not open a modal or freeze the partner. An emergency produce option is available when broke. At night, defeat the rival and claim the stash for the midnight recipe and 3 Flux. Management offers permanent faster prep for 3 Flux or a tired worker energy boost for 1 Flux. Waiting until night enables +30% restaurant sales.

This is local co-op, not online multiplayer. The playable physical menu is burger, salad and midnight bun. Soup/tart data remain future content; the catalog describes the stove as decorative. The two-star oven cooks patties in six seconds. Cosmetic catalog upgrades, workers, pantry and layout persist. Existing pre-version-3 saves receive a backup before migration; transient service orders settle closed on load and held plates recover to stations.

## Verification scope

Runtime acceptance uses an isolated save and scripted game commands. `--controller-test` also exercises startup and shared menu input with two virtual gamepads without touching the real save. It is not a substitute for a two-person fun/feel playtest. Controller button feel, simultaneous human coordination and a complete 20–30 minute human session remain to be tested on the user's hardware.

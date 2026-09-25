# Restaurant City — playable restaurant build

A first-person Windows prototype built with Unity 6000.6.3f1. Run `Builds/Windows/RestaurantCity.exe` with its neighboring `RestaurantCity_Data` folder intact. The Unity project is this directory; open `Assets/Scenes/RestaurantCity.unity` and press Play if you want to work in the Editor.

## Your first restaurant

Choose **New Game** at the title screen for a fresh run. Start with $30, set up the coral food stand for $10, then use the numbered prep, grill, and serve stations to earn $150. The green supplier sells stand ingredients. At night, the rival alley contains a recipe; beat the guard with three spatula hits and interact with the stash. The earned midnight bun can later be added to the restaurant menu.

Buy the shabby building at its front sign for $150. You enter your own restaurant with a prep bench, grill, two-seat table, 20 starter ingredients, and one star. There is no ending at purchase: you can keep running shifts, buying upgrades, and returning to the city.

| Action | Control |
| --- | --- |
| Walk, look, sprint | WASD, mouse, Left Shift |
| Interact with sign, station, furniture, guest, or supplier | Aim and press E |
| Swing spatula in the alley | Left click |
| Open catalog inside restaurant | B |
| Open service, menu, staff, and reviews inside restaurant | Tab |
| Preview and place an item | Choose it in the catalog, move mouse, left click |
| Rotate or cancel placement | R, or right click / Escape |
| Pause or close a management panel | Escape |

The catalog has 24 items across kitchen equipment, seating, finishes, lighting, decor, and exterior work. Placeable furnishings preview from above and show whether a location is valid. With service closed and all guests gone, aim at a furnishing and press E to move or sell it. Walls, floors, and exterior improvements install as whole-building changes. Their price and effect appear in the shop.

In **Service**, open the restaurant to admit guests. Choose a waiting ticket to cook it, close the panel, then press E at a kitchen station to collect a ready dish and E at its matching guest to serve. A cooked dish loses quality if left waiting. Active workers can take over jobs: Ember is a fast cook ($70); Moss serves and cleans quickly ($55). Hire them in **Staff**, then assign Cook, Serve, Clean, or Off. Both earn $1 per order while assigned. Staff move and work on the floor while time is running; management panels pause play.

The supplier across the street stocks restaurant produce and protein in six-portion packs. The menu offers a burger and salad initially; a stove adds soup, the night encounter adds the midnight bun, and two stars unlock an oven and moonberry tart. Customers differ in patience, favorite food, cleanliness concern, and ambience preference. The **Reviews** panel explains ratings from food freshness, waiting, and the room. Reach 20 served meals, 75% satisfaction, and 12 ambience to earn two stars and unlock the oven, jukebox, and neon sign. The rank stays earned even if later reviews dip.

## Saves and testing

The game autosaves after major actions, periodically during play, on pause, and on exit. On Windows the save is `%USERPROFILE%\AppData\LocalLow\AntDev\Restaurant City\restaurant-city-v1.json`; the `.bak` file preserves the previous version. Existing version-one stand saves load into the expanded game. Restaurant layout, money, menu, stock, workers, reviews, recipe, and star rank persist. An unfinished shift closes safely on load.

The Windows build passed the isolated `--restaurant-stage2` walkthrough (153 assertions) and a separate `--restaurant-resume` process that loaded its save. Evidence screenshots are in `Builds/Windows/RestaurantEvidence/`: starting restaurant, catalog, busy service, and upgraded restaurant. The standalone model tests are in `RestaurantTests/`; earlier city checks are in `Tests/`. The test walkthrough uses a separate file and does not change the player's save. It is automated verification, not a human judgement of a 20–30 minute session or mouse feel.

Everything in the restaurant art pass is original procedural geometry and texture work; sources are recorded in `docs/ASSET_SOURCES.md`. This is a local first-person prototype. Vehicles, a dealership, casino, multiplayer, and a larger explorable district are future city expansions.

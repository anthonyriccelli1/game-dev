# Restaurant City (working title)

A stylized first-person restaurant-and-city game for one or two local players, built in **Unity 6000.6.3f1**.
Cook together like PlateUp!, grow a restaurant and a cast of odd residents like Zombie Cafe, and explore a
city with day/night danger like Schedule I, all in **Saffron Bay**, a modern city with a sci-fi streak.

## What's playable now (Old Market district)

- **The Little Flame street stand:** your first kitchen. Guests line up, sit at two sidewalk tables, and
  you cook burgers and salads, carry them out, clear the plates and wash them.
- **Milo's Market:** talk to Milo to shop. Buy ingredients in a cart, carry the grocery bag home, and
  unpack it into your pantry. Each ingredient runs out on its own. Milo fronts broke players free basics once a day.
- **The Odd Table:** your first restaurant ($150 lease). It starts empty and shabby; buy and arrange a
  kitchen and dining room from the catalog, then run shifts.
- **Recipes:** burger and salad are starters. Planet soup is bought in the Cookbook and simmers on the
  stove (stir it or it scorches). The Midnight burger is won from the rival's stash in the alley at night.
- **Reputation ranks:** Street Cook to Mogul, earned by quality play (not money), each unlocking a district.
- **Staff:** recruit residents with Flux, assign jobs, manage their energy.
- **The Bayside:** a bigger second restaurant, listed for later.
- **Local co-op:** a second player joins on a controller with Start.

## Controls (keyboard and mouse)

| Action | Key |
| --- | --- |
| Move, look, sprint | WASD, mouse, Left Shift |
| Interact (take, place, serve, talk) | E (hold E to chop or wash) |
| Discard what you're holding | Q |
| Build / arrange furniture | B |
| Restaurant management | Tab |
| Staff "phone" / city map | P / M |
| Swing spatula (alley rival) | Left click |

## Opening the project

1. Open this folder in Unity Hub with **Unity 6000.6.3f1**.
2. **Art packs are not in this repo.** The Synty POLYGON City, Generic and Starter packs are licensed per
   developer and are gitignored. Import your own copies into `Assets/Synty/`. Without them the city falls
   back to plain blockout shapes, but everything still plays.
3. The scene is generated: `Assets/Scenes/RestaurantCity.unity` is rebuilt by `PrototypeBuilder`
   (Editor menu), which also makes the Windows build in `Builds/Windows/`.

## Tests

Two automated suites run against the Windows build (`EditorTools.RunPhysicalAcceptance` and
`RunInteractionAcceptance`). They play the stand, buy at Milo's, lease the restaurant, cook every dish,
run co-op, staff and a failed shift, and check that save/reload restores the exact restaurant.
Logs are written to `Acceptance/`.

## Saves

`%USERPROFILE%\AppData\LocalLow\AntDev\Restaurant City\restaurant-city-v1.json` (a `.bak` keeps the
previous save). Older saves migrate automatically, with a backup made first.

# Restaurant City (working title)

A stylized first-person restaurant-and-city game for one or two local players, built in **Unity 6000.6.3f1**.
Cook together like PlateUp!, grow a restaurant and a cast of odd residents like Zombie Cafe, and explore a
city with day/night danger like Schedule I, all in **Saffron Bay**, a modern city with a sci-fi streak.

## Implemented in source (Old Market district)

This is a source-level overview, not a verified playthrough of this checkout. See the test scope below.

- **The Little Flame street stand:** your first kitchen. Guests line up, sit at two sidewalk tables, and
  you cook burgers and salads, carry them out, clear the plates and wash them.
- **Milo's Market:** talk to Milo to shop. Buy ingredients in a cart, carry the grocery bag home, and
  unpack it into your pantry. Each ingredient runs out on its own. Milo fronts broke players free basics once a day.
- **The Odd Table:** the only current restaurant site ($350 lease). The lease supplies an empty, shabby
  room, not a free kitchen; keep earning at Little Flame to buy and arrange equipment and seating.
- **Recipes:** Flats Burger and Stoop Salad are starters. The Cookbook sells Planet Soup ($60, 1 star),
  Comet Dog ($70, 1 star), and Moonberry Float ($110, 2 stars). Raids award Cyclops Stack (Greasy Gus)
  and Philosopher's Stack (The Alchemist). Midnight Burger comes from a rooftop strongbox after dark
  once you own a restaurant; call Zeeb for sauce. Twin Moons, Dragon's Hoard and Glowshroom Melt are
  listed but not ready. The truck stocks only patties, buns and greens; expanded recipes are for the restaurant.
- **Kitchen minigames:** first-person lettuce chopping uses six aimed cuts; washing uses mouse/stick
  scrubbing. Flip grill patties for even cooking, stir soup to avoid scorching, and serve floats before they melt.
- **Reputation ranks:** Street Cook to Mogul, earned by quality play (not money), each unlocking a district.
- **Staff:** hire workers for cash or recruit special residents with Flux, assign jobs, manage their energy.
- **Local co-op:** a second player joins on a controller with Start.

## Controls (keyboard and mouse)

| Action | Key |
| --- | --- |
| Move, look, sprint | WASD, mouse, Left Shift |
| Interact (take, place, serve, talk) | E; chop/scrub with mouse in first person, hold E in elevated view |
| Put down chopping board / washing plate | Q (retains partial progress) |
| Discard food | Carry it to the trash can and press E; Q does not discard held food |
| Build / arrange furniture | B |
| Restaurant management | Tab |
| Staff "phone" / city map | P / M |
| Elevated camera / jump | V / Space |
| Attack / block | Left mouse (hold/release for heavy hit) / right mouse |
| Flip patty (aim at grill) | Left click or flick mouse upward |

## Opening the project

1. Open this folder in Unity Hub with **Unity 6000.6.3f1**.
2. **Licensed environment art and animation files are not in this checkout.** Source references Synty
   POLYGON City, Generic, Starter and Shops under `Assets/Synty/`, plus Mixamo clips under
   `Assets/Resources/Mixamo/`; these folders and `Assets/ThirdParty/` are gitignored. Import only copies
   you are entitled to use; this repository does not grant rights to redistribute the raw assets.
   The builder has a blockout branch without POLYGON City, and resident animation has missing-clip
   fallbacks. That does **not** establish full gameplay or visual parity without the packs: the detailed
   city branch also creates raid targets and Flux/recipe hiding spots. Missing-assets runtime behavior
   has not been verified here. See [asset provenance](docs/ASSET_SOURCES.md).
3. Open the saved scene through **Restaurant City > Open Prototype**. **Rebuild Prototype Scene**
   regenerates `Assets/Scenes/RestaurantCity.unity` from `Assets/Editor/PrototypeBuilder.cs` and locally
   available packs. **Build Windows Player** builds `Builds/Windows/RestaurantCity.exe`; scene
   generation and building are separate operations (`GenerateAndBuild` combines them).

## Tests

`EditorTools.RunPhysicalAcceptance` and `EditorTools.RunInteractionAcceptance` launch an existing
Windows build with `--physical-test` and `--interaction-test`, writing logs to `Acceptance/`. Source
contains scripted checks for stand service, groceries, leasing and furnishing, selected cooking paths,
co-op, staff, failed shifts, save/reload, and interaction/minigame behavior; it is not evidence that every
current dish has passed. `--controller-test` uses two virtual gamepads for startup and shared-menu checks.

[Physical verification](docs/PHYSICAL_VERIFICATION.md) and [earlier verification](docs/VERIFICATION.md)
record historical Windows runs, not results reproduced against this checkout. No Unity build, acceptance
suite, missing-assets playthrough or physical-controller session was run for this documentation update;
the Windows executable and `Acceptance/` logs are absent here. Use the [physical playtest guide](docs/PHYSICAL_PLAYTEST.md)
for human testing, which remains distinct from scripted acceptance.

## Saves

`%USERPROFILE%\AppData\LocalLow\AntDev\Restaurant City\restaurant-city-v1.json` (a `.bak` keeps the
previous save). Source accepts save versions 1 through 7 and sanitizes/migrates on load. Additional
migration backups are conditional (pre-v3, pre-v4, and legacy non-Odd-Table sites), not guaranteed for
every older version. Save/reload behavior has not been re-tested in this checkout.

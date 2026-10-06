# Restaurant City: Old Market Demo Plan

**Goal:** A new player can start at a battered Little Flame, learn service through play, build The Odd Table into a two-star restaurant, recruit a crew, defeat Gus and The Alchemist, and finish wanting to enter the Docks. This is the demo's complete first chapter, not the full six-district game.

**Player promise:** Cook food with your hands; make a restaurant visibly yours; meet and recruit strange locals; earn money and reputation; challenge rival food businesses for recipes, people, and territory. The restaurant remains playable after the chapter ends.

## The three progress tracks

| Track | Meaning | Old Market milestone |
| --- | --- | --- |
| Restaurant stars | Service, food, and the quality of *this* restaurant. | Grow The Odd Table from shabby and unrated to two stars. Do not hand out a star for winning a fight. |
| Reputation rank | The player's standing across the city. Earned from good service, discovery, recruits, and rivals. | Reach Line Cook. Current threshold is 400 reputation; tune it by playtesting, not by guessing. |
| Rival victories | Risky challenges that grant distinctive recipes and characters. | Beat Gus, then The Alchemist. Their rewards should change what the player can cook and whom they can recruit. |

**Docks gate:** Both Line Cook reputation **and** a victory over The Alchemist are required. The current implementation grants Line Cook after 400 reputation and a two-star Odd Table, then uses rank alone for district access; it does not yet check the Alchemist victory. Keep Line Cook earnable through restaurant quality and reputation, and add the separate victory check to Docks access. The Docks are currently a planned, unbuilt district. Until they exist, the demo should celebrate completion and tease the route without claiming the player can walk into a playable Docks map.

**Long game:** Five-star restaurant growth belongs across later districts; the Old Market chapter does not require three stars. The working district sequence for the vision is Old Market → Docks → Greenleaf → Neon Row, with the final two districts to be decided. Current `CityDistricts.All` puts Neon Row before Greenleaf; align that data when those districts are built. The tentative Egyptian restaurant is not a required third Old Market boss for this demo.

## Playable story beats

1. **Arrival at Truck Park.** Player starts beside Little Flame, which visibly needs work. Rose and Milo's supply cart are close enough to understand the first errand; the Odd Table and other local businesses are recognizable landmarks, not floating quest labels. A brief controlled first-person camera/sound beat can establish the block, then return control immediately. Skip is available. No separate tutorial town or forced defeat.
2. **First service.** Buy and unpack ingredients, cook one dish, serve an expressive customer, clear and wash. Teach through contextual prompts at the workstations. The first sale pays and gets an in-world reaction. Introduce a second dish before the tutorial disappears, so the truck feels like a small business rather than a single-button lesson.
3. **First star and lease.** A few shifts introduce rushes, stocking, customers, and night. First star marks reliable service. Save for the current $350 Odd Table lease, subject to pacing playtests. The first entry into the shabby, empty restaurant is the chapter's title/reveal moment; the player immediately starts choosing and placing equipment and decor.
4. **Make it yours.** A starter kit, dining layout, wall/floor finishes, signs, and a first hire should produce a visible before/after. The restaurant must support multiple shifts and save/reload. Residents met as customers become recruits, tying discovery to service.
5. **Gus.** His truck and imps are seen before the raid. Once the player has a crew and the restaurant is established, the first raid tests fighting alongside those recruits. Winning gives the Cyclops recipe and makes Gus a recurring local rival rather than ending play.
6. **Two stars and The Alchemist.** Better service, an upgraded room, and stronger staff earn two stars and Line Cook reputation. The Alchemist's restaurant is visible and enterable by day, showing a standard to aspire to. At night, his raid is the chapter climax; require two stars for this Old Market finale even though the current general raid rule allows a rival one star above the player. Victory earns the Philosopher's Stack and makes Frank and Frankie recruitable. The Docks route becomes the next visible goal.

The through-line is an outsider earning a place in a strange, rough neighborhood through food and relationships. Gus competes for the block; The Alchemist is the intimidating established restaurant. A citywide ruler or goon attack is a possible later reveal, not required exposition in the demo.

## Work stages and acceptance

### 1. Opening and Truck Park art

**Work:** Dress the spawn route, food truck exterior and work area, Rose's cart, surrounding storefronts, grime, signage, lighting, and sound to the same authored standard as The Alchemist. Make Little Flame patched and underfunded but readable and inviting to operate. Build a short skippable opening beat using existing scene/camera tools before committing to a cinematic system. Keep the player in control for the first sale.

**Accept when:** A new save shows where to go without a wall of text; the player can buy, unpack, cook, serve, and wash without a developer explaining each step; close-up screenshots of the truck look deliberately built, not like a blockout. Capture daytime, working-interior, and night screenshots. Play it with keyboard/mouse and one controller in co-op.

**Likely code/art:** `Assets/Scripts/CityGame.cs`, `PhysicalStand.cs`, `PhysicalHud.cs`, `CityHud.cs`, `Assets/Editor/CityMap.cs`, `Assets/Editor/PrototypeBuilder.cs`, `Assets/Resources/ArtOverrides/Shell/street_stand.prefab`. Inspect existing behavior before changing any of these; another developer has active work in this checkout.

### 2. Restaurant ownership payoff

**Work:** Tune earnings and starter-kit cost from fresh-save playtests. Make the first Odd Table entry an authored reveal, preserve immediate control, and make the shabby-to-custom transformation unmistakable. Repair any cooking or placement friction that interrupts the first two shifts. Do not replace player freedom with a prescribed furniture layout.

**Accept when:** A new player can afford and open the restaurant at a satisfying point, create a visibly different layout, complete two shifts, quit and reload, and see their exact restaurant restored. The reveal works once and does not replay on load.

**Likely code/art:** `Assets/Scripts/RestaurantController.cs`, `RestaurantUI.cs`, `RestaurantModel.cs`, `CityGame.cs`, `Assets/Editor/PrototypeBuilder.cs`, existing decor and kitchen scripts. Use isolated test saves.

### 3. Character and rival arc

**Work:** Introduce Gus and the Alchemist through visible world activity and short interactions before the raids. Make recruiting understandable and ensure a hired worker visibly helps during service and fights during raids. Keep each rival's recipe/character reward prominent in the UI and usable afterward. Tune difficulty against a realistic demo crew.

**Accept when:** On a new save, the player encounters both rivals before fighting them, recruits and observes workers, beats Gus, earns two stars, beats The Alchemist, uses a rival recipe, and can continue running the Odd Table afterward. The final scene celebrates the win and points toward the Docks.

**Likely code/art:** `Assets/Scripts/Raids.cs`, `RaidFight.cs`, `ResidentCast.cs`, `RestaurantUI.cs`, `CityGame.cs`, `Reputation.cs`, `GameState.cs`, `DistrictLocks.cs`.

### 4. Gate, pacing, and demo release pass

**Work:** Require Line Cook rank and an Alchemist win at the Docks gate while keeping stars, reputation, and victories separate in UI and save data. Do not make the Alchemist win a condition for earning Line Cook itself. Display both unmet Docks conditions. Use fresh-save playtests to tune time-to-first-sale, first-star, lease, first-hire, Gus, two stars, and Alchemist. Polish the continuous critical route instead of every unopened district. Run build and save migration checks.

**Accept when:** Neither 400 reputation alone nor an Alchemist win alone opens the Docks route; both do. A demo-complete state persists across save/reload. No new save is stranded without money or a viable way to recover. The Windows build completes, automated physical/interaction suites pass, and a human playthrough records friction and total time. Do not call a scripted snapshot a human playtest.

**Likely code/tests:** `Assets/Scripts/GameState.cs`, `Reputation.cs`, `DistrictLocks.cs`, `RestaurantUI.cs`, `CityGame.cs`, `PhysicalAcceptance.cs`, `InteractionAcceptance.cs`; `Assets/Editor/EditorTools.cs` provides the build and acceptance commands.

## Scope boundary

Build **Old Market to a coherent demo**, not six partial districts. No full Docks map, vehicle dealership, casino, citywide villain cinematic, third mandatory Old Market boss, or bulk character-generation system in this milestone. Those remain expansion ideas. This plan is the quality bar and sequence for the next implementation work; each stage should end with a runnable build and visual/playtest evidence before the next begins.

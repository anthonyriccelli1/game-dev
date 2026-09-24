# Restaurant City — first playable

A small first-person Unity prototype of the food-stand-to-restaurant concept. Built for Windows using Unity **6000.6.3f1** and Universal Render Pipeline.

## Open and play

1. In Unity Hub, choose **Add / Add project from disk** and select `C:\dev\Ant-Dev\RestaurantCity`.
2. Open the project. Allow script compilation and asset importing to finish.
3. Choose **Restaurant City > Open Prototype**, or open `Assets/Scenes/RestaurantCity.unity`.
4. Press Unity's **Play** triangle, select the **Game** tab, then click **Start your first shift**.

The standalone version, after a successful build, is `Builds/Windows/RestaurantCity.exe`. Keep its neighboring files and `RestaurantCity_Data` folder together.

## Controls

| Action | Control |
| --- | --- |
| Walk | WASD |
| Look | Mouse |
| Sprint | Left Shift |
| Use a station / buy supplies | E while aiming at it |
| Swing spatula | Left mouse button |
| Pause / release mouse | Escape |

## First shift

Start with $30. Walk toward the coral stand and press E to set it up for $10. The green supplier on the left sells 3 ingredients for $6. Return to the numbered stations: **prep**, **grill**, **serve**. Put a prepared burger on the grill, wait at least 4 seconds, and press E again before 10 seconds to plate it. Serve at the right-hand counter or directly to the waiting customer. The bin beside the counter clears a burned dish.

A normal burger pays $12; night service pays $18. Customers leave after 65 seconds. Each day lasts 150 seconds and each night 90 seconds. The pause menu stops cooking, customer patience, and the clock.

At night, enter the marked alley to the right of the stand. The rival signals an attack before striking. Back away to dodge, or swing to interrupt; three hits defeat the rival. Leave the narrow alley to escape. Open the gold stash beyond the rival to learn the midnight burger, worth $18 by day and $27 at night.

Defeat returns you to the street with full health and costs up to $10. Your stand and recipe unlock remain. The supplier provides one recovery ingredient when you cannot afford stock. Money for initial stand setup is reserved so shopping or defeat cannot block progress.

Save $150 and visit the future restaurant sign across the street to finish the prototype goal. The actual restaurant interior is a later milestone.

## Saves

Progress saves after successful actions, every 20 seconds during play, on pause, and on exit. On Windows, the save is normally `%USERPROFILE%\AppData\LocalLow\AntDev\Restaurant City\restaurant-city-v1.json`. A previous save is retained as `.bak`. The clock, cash, stock, sales, stand, and recipe persist; the player returns to the starting point, with orders and in-progress cooking reset. New Game requires a second confirmation click and overwrites the active progress.

## Developer verification

Run the deterministic economic/gameplay checks:

```powershell
dotnet run --project Tests/SimulationTests.csproj
```

Use **Restaurant City > Rebuild Prototype Scene** to regenerate the authored scene, and **Restaurant City > Build Windows Player** to build it. Regeneration replaces only the generated prototype scene and materials; save any scene edits you want to keep first.

For a closed Editor, generation/build can run using `Unity.exe -batchmode -nographics -quit -projectPath C:\dev\Ant-Dev\RestaurantCity -executeMethod PrototypeBuilder.GenerateAndBuild -logFile C:\dev\Ant-Dev\unity-build.log`.

An opt-in `--smoke-test` player argument exercises world-target raycasts, buying, cooking, payment, live rival attacks, retreat, spatula physics/cooldown, night recipe progression, respawn, and Unity JSON serialization. It writes camera renders and `result.txt` into `Builds/Windows/SmokeEvidence` and never accesses the user's save. Camera renders omit the HUD so they can be captured reliably with a hidden test window. This is not a substitute for a human playtest of mouse feel, UI, combat timing, or fun.

## Prototype limits

Primitive placeholder art, one street, one active customer at a time, one base recipe with a night upgrade, and one rival. Stand placement is fixed. No interior building, staff, vehicles, police, audio, or multiplayer yet. HUD is keyboard/mouse oriented and the save format is version 1.

Design and implementation plan are in `docs/plans/`.

# First Playable Implementation Plan

> Use superpowers:executing-plans to implement this plan task-by-task in this session. The workspace is new and empty; no existing checkout needs isolation.

**Goal:** Deliver an openable Unity project and playable first-person food-stand prototype.

**Architecture:** Separate pure simulation from Unity input, world geometry, and UI. Generate a saved scene with an editor command; persist progression as versioned JSON.

**Tech Stack:** Installed Unity 6000.6.3f1, URP template, C#, Windows standalone, dependency-free .NET test harness.

---

### Task 1: Bootstrap and core behavior tests
- Create the Unity URP project and write this plan.
- Create `Tests/SimulationTests.csproj` and `Tests/Program.cs`, linking `Assets/Scripts/GameState.cs`.
- Cover insufficient funds, one-time stand purchase, ingredient consumption, cooking quality, customer timeout, night reward requirements, bounded respawn loss, and save validation.
- Run `dotnet run --project Tests/SimulationTests.csproj`; observe expected missing-feature failures before implementing state behavior.

### Task 2: Simulation
- Implement `Assets/Scripts/GameState.cs` as pure C# with explicit actions and a deterministic Tick.
- Repeat simulation tests until all pass.

### Task 3: First-person scene and service
- Create `Assets/Scripts/FirstPersonPlayer.cs`, `Interactable.cs`, `CityGame.cs`, and `CityHud.cs`.
- Implement pointer capture/pause, collision movement, raycast prompts, stand setup, purchasing, prep/cook/serve, visible customers, order patience, and contextual objectives.
- Create `Assets/Editor/PrototypeBuilder.cs` to generate `Assets/Scenes/RestaurantCity.unity` and materials using primitive meshes and URP.
- Generate scene in Unity batch mode, verify no compilation errors.

### Task 4: Night encounter and persistence
- Add day/night lighting, higher night earnings, guarded recipe, telegraphed enemy attack, player swing, escape, and respawn.
- Add versioned save/load, pause menu, safe restart, and introductory help.
- Add a runtime smoke mode that exercises actual scene interactions and captures screenshots without interfering with user saves.

### Task 5: Deliver and verify
- Run simulation tests, scene validation, Windows build, and runtime smoke check.
- Inspect a rendered frame and player log for runtime errors.
- Write `README.md` with opening instructions, controls, gameplay steps, scope, known limits, and evidence.
- Open the project for the user if available without conflicting with batch processes. Do not claim manual playtesting occurred.

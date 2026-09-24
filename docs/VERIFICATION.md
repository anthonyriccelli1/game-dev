# Verification — September 24, 2026

- Unity 6000.6.3f1 successfully compiled the C# scripts, generated the scene, validated every interaction kind and required reference, and built a Windows x64 development player. Final build exited 0.
- 18/18 standalone C# simulation checks passed. These include service earnings, stock consumption, burn handling, customer timeout, night premium, recipe gating, progression preservation, save validation, economic recovery, setup-fund protection, and rival territory boundaries.
- The final executable's smoke run exited 0 with 45 assertions passed. This tested scene-target raycasts, purchases, prep/grill/serve, actual enemy windup damage, retreat, physics-based spatula hits, attack cooldown, recipe pickup, death/respawn, Unity JSON round trip, and offscreen scene rendering.
- Inspected the final day and night camera renders. Fixed initial world-text scaling/occlusion and first-person spatula proportions before the final build.
- A separate static review found and verified fixes for a pre-stand economic dead end and rival attacks through the warehouse.

Evidence: `docs/screenshots/`, `Builds/Windows/SmokeEvidence/result.txt`, `Builds/Windows/smoke-final.log`, and `C:\dev\Ant-Dev\unity-build-final.log`.

The test launched a hidden executable and submitted explicit camera render requests. These renders omit the IMGUI HUD. The automated check uses the same game actions, physics casts, and guard updates as gameplay, but does not synthesize keyboard/mouse hardware events. Manual input feel, UI readability, enjoyment, and a full quit/relaunch disk-save playtest remain for the first human playtest. The smoke run does not read or write the user's save.

Unity's default package set produced cloud analytics connection warnings in the network-restricted test environment; the gameplay test had no managed exceptions or failed assertions.

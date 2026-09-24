# Restaurant City: first playable

The user approved Unity and first-person movement on September 24, 2026. The source concept is Restaurant_City_Game_Concept_v0.1.md. This is an initial single-player Windows prototype, not the complete game.

## Experience
Begin on a compact stylized street with $30 and a visible empty stand lot. Buy ingredient packs at the supplier, assemble the starter stand for $10, prepare a burger, cook it, and hand it to a waiting customer. Customers have patience; accurate cooking pays better. A short day/night cycle introduces higher prices and a marked alley where a rival guards a recipe. A telegraphed attack, player swing, escape route, and safe respawn test the night loop. Unlocking the recipe increases the value of future meals. Earn $150 toward a visible future restaurant.

## Controls and presentation
WASD movement, mouse look, Shift sprint, E interact, left click swing, Esc pause/release mouse. Start on a clear welcome screen with controls; no automatic mouse capture until the player starts. Warm low-poly city geometry, teal storefronts, coral food stand, readable world signage, and restrained cream/ink HUD. Contextual prompts and a sequential objective explain the loop.

## Architecture
Pure C# GameState owns money, stock, cooking phases, order patience, clock, progress, and night reward. Unity components own rendering, raycast interaction, player collision, NPC motion, and HUD. A scene-building editor command generates a persistent scene and reusable materials. Versioned JSON stores progress between sessions; transient orders and cooking reset on load. Malformed saves recover safely, with an explanatory message.

## Scope and validation
No paid assets, networking, vehicles, police, full interiors, or staff in this milestone. Verify economic invariants, cooking transitions, order failures, night unlock, bounded death penalties, and load validation with standalone C# tests. Import/compile using the installed Unity Editor, generate and validate the scene, build a Windows player, and run an automated gameplay smoke path. Manual mouse feel and player enjoyment remain playtest questions.

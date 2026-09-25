# Physical Kitchen and Local Co-op Implementation Plan

**Goal:** One persistent restaurant shared by a keyboard/mouse player and a controller partner, with physical recipes, reusable plates, visible helpers, two shifts and a meaningful night outing.

**Architecture:** Retain the existing restaurant, catalog and city. Add authoritative serializable kitchen rules for single-owner carried items, station slots, ingredients, processing and serving. Input and rendering issue the same commands for each player and worker. Local split-screen is explicitly not online networking.

**Tech Stack:** Unity 6000.6.3f1, URP, Input System, C#, existing Windows player and isolated acceptance harness.

## Design decisions from the current user direction
- The supplied September 25 prompt is the controlling product brief; preserve first-person, purchased layout and unlocks.
- Keyboard/mouse is P1; a controller joins P2. Two assigned gamepads are also supported. Every actor has one authoritative held item.
- Three physical starter workflows: salad (produce, chop, plate), burger (protein, prepare, grill, plate with bun), midnight bun (burger plus separately prepared night sauce). Other existing menu data remains unlockable but the small slice focuses on these workflows.
- Shared finite clean plates become dirty at tables after eating. Clearing and washing return plates; no free replacement exploit.
- Prep/washing require work at a station. Cookers continue unattended and show ready/burnt states. Correct dish is checked on serving.
- First-person default; optional elevated service camera and overhead building camera never teleport the player.
- Closed planning may pause both players with a clear banner. During service each player has its own prompts/tickets; opening management must not hide or freeze the partner's work.
- Shift report explains revenue, ingredients, wages, reviews, star change and fatigue. Existing night encounter grants recipe and first earned Flux; Flux can research a lasting efficiency upgrade or boost a tired worker. Basic rest/hiring remains cash only.
- Preserve saves, back up prior versions, recover orphaned/transient dishes safely on load, and keep money/layout/staff/unlocks.

## Runnable stages
1. Inspect current code/scene/save and play baseline build. Back up user save. Existing full baseline walkthrough passes 153 checks.
2. Add pure kitchen command rules and contention/recovery tests, then world stations, physical objects, prompts and solo cooking. Keep 24-item catalog and add necessary kitchen stations with free migration kit.
3. Add independent assigned input, split camera, elevated toggle, gamepad focus/navigation and per-player world interaction. Verify same-station contention without duplicate resources.
4. Make workers use the kitchen commands, add fatigue/rest, explicit shift phases and report, recoverable star changes, and earned Flux choice.
5. Update runtime harness for complete physical two-shift loop, failed-shift recovery, city reward and exact-layout restart. Test synthetic simultaneous input plus actual attached hardware where available; distinguish results.
6. Build Windows, inspect rendered screenshots of starting/upgraded room, busy service, two views and report. Review implementation, document controls and known limits, commit and launch for user.

## File ownership for independent work
- Kitchen rules: new `KitchenModel.cs`, relevant `RestaurantModel.cs` and `GameState.cs`, standalone `PhysicalTests/`.
- Local input/cameras: `FirstPersonPlayer.cs`, new `LocalCoop.cs` only.
- Art: new `KitchenArt.cs` only, factories for pantry/sink/rack/pass and identifiable item states.
- Integration, staff, UI, save wiring, tests, build and review: root agent.

The prior Windows build was inspected in a real window and exercised through the existing runtime walkthrough. Direct human-style movement/controller feel remains a separate verification concern. A physical Escape cancelled computer control; subsequent work avoids taking over the user's input.

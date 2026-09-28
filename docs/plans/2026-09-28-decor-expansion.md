# Restaurant Decor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the sparse whole-room finish shop with 22 distinct, progressively unlocked finishes and a saved surface brush.

**Architecture:** RestaurantState stores additive surface overrides over legacy whole-room finishes. A shared finish-definition table supplies catalog metadata and procedural material creation. RestaurantController owns brush UI/input and patches furniture-independent room surfaces; kitchen/service behavior remains intact.

**Tech Stack:** Unity 6 URP, C#, existing runtime procedural art and Windows acceptance runners.

### Task 1 — Model and catalog
Files: Assets/Scripts/RestaurantModel.cs; create Assets/Scripts/FinishModel.cs.
Add compile-safe reflection tests before implementation, run baseline isolated acceptance. Then add twelve wall/ten floor finish definitions, stable IDs/prices/locks, additive persisted surface records, validation, per-patch and fill purchase APIs, no-op handling and bounded coverage ambience. Preserve legacy finish purchases/saves. Model owner defines explicit surface geometry keys shared with the renderer/controller.

### Task 2 — Material and surface rendering
Files: create Assets/Scripts/FinishArt.cs (RestaurantArt partial); minimal RestaurantArt.cs hooks only.
Original procedural paint/paper/brick/panel/tile/terrazzo materials; accurate swatches used by thumbnails. Render collider-free individual floor/wall patches over legacy finishes. Avoid z fighting, hide wear only where covered, honor Room/Site transforms, and destroy/rebind surface objects with rebuild. Art owner reads the shared definition contract.

### Task 3 — Brush integration
Files: RestaurantController.cs, RestaurantUI.cs; create FinishPlacement.cs.
Finish selection starts preview rather than purchase. Provide single-section brush and explicit fill with total price, surface target highlight, cash/lock failure feedback, cancellation and repeated placement. Restore original camera/mask/cursor on exit; reuse roof exclusion. Add wall and floor catalog filters and visible progression locks. Leave general furniture placement unchanged.

### Task 4 — Acceptance
Files: create FinishAcceptance.cs; InteractionAcceptance.cs; docs/ASSET_SOURCES.md.
Exercise targeted patch preserves neighbors, floor/wall target type validation, repeat no charge, insufficient cash, locks, fill atomicity, save JSON/old-save defaults, bounded ambience, open-service rejection, rebuild visuals and cancellation. Run saved-scene Windows build and isolated interaction/physical suites. Inspect initial/mixed/finish catalog screenshots. Record source/licensing and distinguish automated from human/controller testing.

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

## Completed first stage — 2026-09-29

- 22 unique finishes: twelve wall finishes and ten floors, including fourteen immediately accessible choices. Patch prices $1–$6; premium choices use existing stars/reputation. Catalog swatches use the same procedural texture as the installed finish.
- Persistent section brush, floor/wall filters, prices, targeted preview, repeated painting and explicit full-family fill quotation/confirmation. A floor fill covers all 13×13 metres through 12×10 logical tiles; all-wall fill covers 36 sections. Camera/pointer geometry match these cells and wall strips remain clear of the side controls.
- Additive saved SurfaceFinishes over legacy full-room finishes, invalid/duplicate record cleanup, whole-room legacy prices preserved, bounded coverage ambience, cancellation without purchase. Kitchen and ordinary furniture placement remain intact.
- RED baseline: `Acceptance/decor-red.log`, 110 checks with three expected missing-feature failures.
- Final Windows build succeeded via saved-scene `BuildCurrentWindows`. Initial runtime found a stripped Unlit shader in the preview outline; switched to the Lit shader already included by room materials and rebuilt. Split-screen capture rect was corrected for clean full-frame evidence.
- `Acceptance/decor-save-final.log`: **143 checks, zero failures**, including model transactions/locks, exact full-floor bounds, floor mouse targeting, all three wall strip mouse targets, quote-before-purchase, camera restoration, old saves and real isolated disk save/load through sanitation.
- `Acceptance/decor-physical.log`: **PHYSICAL_RUNTIME_PASS 616** on the same production implementation; subsequent rebuilds changed acceptance/capture code only. Covers broader cooking/service/staff/layout/progression and two-player simulation. No normal user save was used or reset.
- Root reviewed delegated source and independently inspected clean `InteractionEvidence/decor-catalog-walls.png`, `decor-fill-quote.png`, and `decor-mixed-room.png`. Independent source review completed; coverage/UI issues were corrected before final verification.
- Controls: close service, B → Finishes → Walls/Floors → Preview brush; click a tile or wall strip. F quotes the whole floor/all walls, followed by a separate confirmation. Esc/B/right click returns to the catalog. Brush interaction was exercised with scripted camera/pointer queries; human controller feel was not manually verified.
- Output: `Builds/Windows/RestaurantCity.exe`. Original texture provenance documented in `docs/ASSET_SOURCES.md`. Placeable windows/shelves and further furnishing collections remain the next decor stage.

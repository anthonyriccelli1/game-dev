# Chopping Feedback Design and Implementation Plan

**Approved:** User approved the prep-table chopping proposal in this conversation.

**Goal:** Make holding E to prepare greens feel like chopping while preserving the existing physical cooking rules, progress bar, timings, saves and co-op controls.

**Architecture:** Procedural station-side knife animation and ingredient presentation consume accepted preparation work. Presentation never advances cooking or consumes ingredients. Every station has independent feedback, visible to either player and when staff work there. Existing chopping audio is synchronized to strokes instead of playing a separate generic cadence.

**Tech stack:** Unity 6000.6.3f1, URP, C#, original procedural models and synthesized audio; no new downloaded assets or skeletal animations.

## Behavior

- Greens placed on the board begin whole. Accepted work produces repeated knife strokes and timed cutting sounds.
- Greens become a readable pile of pieces as preparation progresses. Partial preparation stays visible after stopping.
- Releasing interaction, looking away, pausing, removing the ingredient, completing it or changing station ownership stops chopping; no autonomous cooking progress.
- Finished chopped greens remain available for pickup and assembly. Midnight preparation retains existing rules and receives appropriate tool feedback where useful.
- Tool/food effects are collider-free and are removed/rebound when furnishings rebuild. They work on rotated restaurant benches and the street stand.

## Sequence

1. Inspect prep bench geometry, item rendering, accepted Work calls and sound event consumption.
2. Add focused regression checks proving missing feedback, stop/resume behavior, two stations, staff work, completion and layout rebuild cleanup. Run the isolated acceptance player before implementing.
3. Implement procedural feedback using station-local anchors and authoritative work/progress. Keep presentation separate from shared models and saved data.
4. Independently review spec compliance and code quality. Build through prototype-build.request in the open Editor.
5. Run interaction and physical acceptance using isolated saves. Capture first-person chopping and partial/completed greens via URP offscreen rendering, inspect images, and document verified scope.

## Acceptance

Knife motion and cutting audio match productive work, stop immediately when work stops, and do not continue on stale ownership. Food visibly changes during preparation. Two players cannot double progress one ingredient. Staff and stand use the same presentation. Existing table serving, cooking, progression and persistence checks continue passing. Human controller feel remains a separate playtest.

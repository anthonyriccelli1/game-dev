# Grill, Washing and Bin Feedback Plan

Approved scope: extend the procedural chopping approach to other kitchen interactions. The user explicitly chose visual grill flipping with existing cooking rules preserved.

## Design

- Grill: left click/controller right shoulder while aiming at a patty uses a station-side spatula to lift and flip it. Patty color gradually reflects authoritative cooking progress; steam/sizzle and readiness remain readable. Flips cannot duplicate, move, consume or recook food and have no quality penalty. Existing oven rules remain unchanged.
- Washing: accepted hold-E/A work animates a sponge, water and bubbles while stains visibly fade. Progress, plate conservation and existing washing duration remain authoritative. Stop immediately on release, looking away, pause, removal or completion. Visible to both players and staff.
- Bin: successful food discard animates the lid and adds visible refuse. A bounded, additive WasteCount field on KitchenStation retains contents through saves and furniture moves; missing older-save fields default to zero. E with empty hands empties the bin without fees, damage or service blockage. Failed discards add nothing; plate/grocery safety rules remain intact.
- Every effect is collider-free, follows station-local anchors/rotation, and is destroyed/rebound with furniture. No downloaded assets or skeletal animation are required.

## Ownership and implementation

1. Parent adds compile-safe missing-feature regression checks and builds/runs red. Agents investigate but wait for red evidence before source implementation.
2. Grill implementer owns GrillFeedback.cs and FirstPersonPlayer.cs swing routing only; supplies TryFlipStation and TickGrillFeedback on the RestaurantController partial.
3. Washing/bin implementer owns WashBinFeedback.cs with independent WashFeedback and BinFeedback components and RestaurantController TickWashBinFeedback plus accepted-event handlers. Parent owns model fields/actions and event routing.
4. Parent wires LateUpdate, GameFeel events, physical item presentation and complete focused tests. Existing chopping, table serving and roof fixes remain intact.
5. Independent spec and code review; build Windows player. Run isolated interaction and physical acceptance, inspect grill/washing/bin captures and record what was verified. No real-save reset or new cooking requirements.

## Acceptance

Visual flips preserve patty identity, timer and quality and reject empty/blocked targets. Cooking visuals follow raw/ready/burnt states. Washing starts only with accepted work, keeps partial cleanliness and stops on stale ownership; two stations operate independently and completed plates return normally. Successful discards fill only the aimed bin; emptying and older-save defaults work. Scene/layout rebuilds produce one feedback component per station and no extra interaction colliders. Automated results are distinguished from human/controller feel testing.

## Verification — 2026-09-28

- Missing-feature baseline: `Acceptance/stations-red.log`, 76 checks with four expected failures for the three missing presentation types and WasteCount storage. Existing interaction checks passed.
- Final saved-scene Windows build: `editor-command.result` confirms validation and successful player build. Initial validation found four test harness calls missing the station sub-ID argument; corrected. First runtime caught MaterialPropertyBlock initialization in MonoBehaviour field initializers; moved to Awake in both affected components and rebuilt.
- Final isolated in-player interaction run: `Acceptance/stations-green2.log`, **107 checks, zero failures**. Covers visual flip targeting/obstructions/cooldown, unchanged food identity/quality/cook/burn timing, generic-food hiding, washing release/pause/partial progress/concurrent actors/plate return/rebuild, successful versus failed disposal, capped fill, emptying and additive save compatibility. Existing chopping, roof and seat serving tests remain green.
- Broader isolated physical gameplay run: `Acceptance/stations-physical.log`, **PHYSICAL_RUNTIME_PASS 591**. Existing player progression, cooking, staff, services, layout/save restoration and two-player simulation checks pass. These test flags use isolated state, not the user's normal save.
- Root independently reviewed source after delegated implementation and inspected `InteractionEvidence/grill-flip.png`, `grill-ready.png`, `sink-scrubbing.png`, `bin-full.png`. Original procedural effects fit the equipment and show food/tool/dirt/refuse states. Independent source reviewer found no additional blockers.
- Controls: left click/controller RB while aiming at a grill patty flips its visual; hold E/controller A on a dirty plate in the sink scrubs; E/A at a filled bin with empty hands empties it. Cooking rules are unchanged. These are station-side tool effects; full first-person arms and motion capture are not part of this pass. Human gamepad feel and audio listening were not manually verified.
- Output: `Builds/Windows/RestaurantCity.exe`. No external art/animation purchase required; provenance recorded in `docs/ASSET_SOURCES.md`.

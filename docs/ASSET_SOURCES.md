# Art sources and licensing notes

The restaurant expansion uses original procedural art authored for this project in `Assets/Scripts/RestaurantArt.cs` and animated by `CharacterMotion.cs`.

- Furniture, restaurant architecture, customer bodies, clothing, foliage, accessories, and signs: original generated meshes, including faceted revolution profiles and chamfered polygon boxes. No downloaded model pack or copied game asset is used.
- Wallpaper, worn tile, checker tile, wood grain, and woven fabric: original deterministic textures generated in code. No source photographs or third-party texture library is used.
- Character motion: original transform animation for walking, seated poses, working gestures, and mood expression. No imported motion capture or animation clips are used.
- Visual inspiration: the user's broad direction references stylized low-poly games and unusual restaurant workers. The actual restaurant, visitors, palette, furnishings, and branding here are original and do not incorporate those games' characters, models, textures, logos, or audio.
- Typography: Unity's bundled `LegacyRuntime.ttf`, requested through `Resources.GetBuiltinResource<Font>`. This font is supplied by Unity as a built-in runtime resource; it is not a newly acquired open-source asset or a font for which this project grants a separate license. Its use/distribution remains subject to Unity's applicable software and bundled-component terms. Unity resource API documentation: https://docs.unity3d.com/ScriptReference/Resources.GetBuiltinResource.html . Review the installed Editor's notices and license terms before extracting or redistributing the font separately.
- Materials, mesh rendering, point lights, and shader support: Unity Engine built-in facilities. The existing project `WorldText` shader is reused for depth-tested signs.

No external art assets were purchased or downloaded for this pass. This document records provenance; it does not invent or grant a legal license for Unity-owned components.

## Runtime generation

Call art factories at runtime so generated meshes, materials, and textures are recreated in a built player. They are cached within the current process, but are not serialized as standalone `.asset` resources. Saved restaurant layouts store catalog IDs, transforms, and finish choices rather than transient mesh references.

## Physical kitchen pass

The decor expansion in `FinishArt.cs` creates original deterministic paint, plaster, striped/floral/geometric/scalloped wallpaper, brick, paneling, checker, ceramic, wood, parquet, mosaic, terrazzo, slate and marble textures. Finish swatches and room surfaces use the same generated materials. No photographs, purchased pack, downloaded textures or art copied from the reference games are used. Surface previews and outlines use Unity renderer facilities under its existing engine terms.

The chopping, grill, washing and bin feedback uses original procedural tools, food surfaces, stains, refuse and transform animation (`ChoppingFeedback.cs`, `GrillFeedback.cs`, `WashBinFeedback.cs`). No external animation pack or art asset was acquired for these effects. Grill flips are presentation only; wash progress and bin contents come from the kitchen model.

`Assets/Scripts/KitchenArt.cs` adds original runtime mesh designs for the pantry, plate rack, sink, serving pass, raw/prepared/cooked/burnt ingredients, sesame buns, clean/dirty plates, assembled dishes, ingredient crates, and street herb trough. Faceted revolution profiles and chamfered polygon blocks form the models; split mesh vertices preserve readable flat shading. Cabinet doors, handles, shelf slats, plate rims, sauce bottles, sesame seeds, grill marks, and dish scraps are modeled details. Materials and meshes are cached for reuse. These are original project assets; no external models, images, textures, or sound assets were downloaded or purchased for this pass. Unity shader and engine terms continue to apply as described above.
# Prep-table chopping pass (September 28, 2026)

The chopping knife, intermediate food presentation and procedural station animation are original project-generated art and code. Cutting sounds use the existing synthesized SoundFx.Chop clip. This pass adds no downloaded models, textures, skeletal animations or third-party licenses. Character assets and earlier environment packs retain their own existing licensing requirements.

## POLYGON Shops restaurant architecture (September 29, 2026)

The current restaurant storefront, finish materials, furnishings and the placeable partition wall, open service window, and service counter use the user's imported Synty Studios **POLYGON - Shops Pack** under `Assets/Synty/PolygonShops`. `Assets/Editor/ArtPackDressing.cs` generates scaled runtime override prefabs from those sources. The original pack files and their materials remain in the project; the generated overrides are derivative arrangements of pack assets. The pack's specific license grant is not recorded in this repository, so do not treat the earlier procedural-art/no-external-assets statement as applying to this architecture pass. Confirm the purchased entitlement and distribution terms before shipping the game or sharing source assets.

## Tripo alien pilot (September 30, 2026)

`Assets/Resources/Residents/201_TripoAlien.fbx` and `201_TripoAlien.png` were generated in Tripo Studio from the user's alien character concept. The user supplied the completed Mixamo-preset Humanoid rig export in `ThirdParty-Downloads/alien-rigged.zip` under their Tripo Pro subscription. The FBX has 5,121 triangles and a valid Unity Humanoid avatar; the base-color texture is the only map currently imported for the resident material. Normal, roughness and metallic maps remain in the source ZIP. The character is Zilo, a rare Old Market visitor and five-Flux recruit. Tripo's published paid-plan rights are described at https://www.tripo3d.ai/help/privacy-policy/how-to-use-tripo-models-commercially ; retain the user's purchase records and original concept source when preparing a release.

New custom resident imports use `ResidentCast.CustomResidentHeight` (currently 2.0 m) for consistent in-game scale. This scales the Unity instance; no new Tripo generation or export is required.

## Greasy Gus raid boss (September 30, 2026)

`Assets/Resources/Residents/202_GreasyGus.fbx` and `202_GreasyGus.png` come from the user's Tripo export `ThirdParty-Downloads/demon+chef+3d+model.zip`, generated from their Gus Demon concept. This character replaces the temporary hot-dog model as the boss of Greasy Gus's food-truck raid. The base-color texture is imported for the current resident material; normal, roughness and metallic maps remain in the source ZIP. The boss uses the project's 2.0 m custom-character height. Retain the user's Tripo subscription and source-concept records for release licensing.

`Assets/Resources/Residents/203_GusImp.fbx` and `203_GusImp.png` come from the user's Tripo export `Downloads/stylized+character+3d+model.zip`, generated from their Gus Food Truck Imp concept. The model appears in both of Gus's worker slots, and in the third slot when the player brings three crew. Each slot retains its former combat stats. The base-color map is imported; other maps remain in the ZIP. The similarly named `gus+imp+worker.zip` is actually a byte-for-byte copy of the earlier alien export and is not used.

## Grim, Dracula's new look and Pepper (October 1, 2026)

From the user's Tripo exports (Mixamo-preset Humanoid rigs) in `ThirdParty-Downloads`: `204_TripoReaper` (grim+reaper+3d+model.zip, Grim, a rare night visitor), `205_TripoVampire` (vampire+character+3d+model.zip, the new model for the existing rare resident Dracula; his id `043_Dracula` is unchanged so saves and the People book carry over) and `206_TripoCheerleader` (cheerleader+3d+model.zip, Pepper, a common). Each has the FBX and its base-colour map; the other maps stay in the ZIPs. Custom-character textures import at a 1024 maximum (`ResidentImport`). Retain the user's Tripo subscription and concept records for release licensing.

## Raven, Buck, Blitz and Bonkers (October 1, 2026)

From the user's Tripo exports in `ThirdParty-Downloads`: `207_TripoGothGirl` (gothic+girl, Raven, common, replaces Olivia), `208_TripoConstruction` (construction+worker, Buck, common, replaces Hugo), `209_TripoFootball` (football+player, Blitz, common, replaces Kyle) and `210_TripoClown` (clown+character, Bonkers, uncommon, replaces Biz Dude). FBX plus base-colour map each; `ResidentCast.Replaced` carries old saves over.

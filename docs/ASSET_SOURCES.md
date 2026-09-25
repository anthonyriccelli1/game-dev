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

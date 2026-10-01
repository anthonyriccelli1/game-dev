using UnityEditor;
// Residents (100 Avatars by Polygonal Mind, CC0) import as Humanoid so one set of Mixamo animations drives all of them.
// Materials are assigned at runtime (ResidentModels) from each character's painted texture.
// Mixamo clips (Resources/Mixamo, gitignored) import as Humanoid too; looping clips stay in place.
public class ResidentImport : AssetPostprocessor {
    static bool IsResident(string p) => p.Contains("Resources/Residents/");
    static bool IsClip(string p) => p.Contains("Resources/Mixamo/");
    void OnPreprocessModel() {
        if (!IsResident(assetPath) && !IsClip(assetPath)) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = IsClip(assetPath);
    }
    // Custom (Tripo) characters ship 2048 textures; at our camera distance 1024 looks the same at a quarter of the memory.
    void OnPreprocessTexture() {
        if (!IsResident(assetPath) || !System.IO.Path.GetFileName(assetPath).StartsWith("2")) return;
        ((TextureImporter)assetImporter).maxTextureSize = 1024;
    }
    void OnPreprocessAnimation() {
        if (!IsClip(assetPath)) return;
        var importer = (ModelImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool loop = !(file.StartsWith("Angry") || file.StartsWith("Happy") || file.StartsWith("Cheer") || file.StartsWith("Jump") ||
            file.StartsWith("Punch") || file.StartsWith("ZombiePunch") || file.Contains("HitReact") || file.StartsWith("Headbutt") || file.StartsWith("KnockedOut"));
        var clips = importer.defaultClipAnimations;
        foreach (var c in clips) {
            c.loopTime = loop; c.loopPose = loop;
            // Bake turning and height into the pose; horizontal travel is root motion, which residents ignore (they walk in place).
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootHeightY = true; c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = false;
        }
        importer.clipAnimations = clips;
    }
}

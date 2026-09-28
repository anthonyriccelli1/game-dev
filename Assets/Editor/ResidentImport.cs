using UnityEditor;
// Residents (100 Avatars by Polygonal Mind, CC0) import as Humanoid so one set of Mixamo animations drives all of them.
// Materials are assigned at runtime (ResidentModels) from each character's painted texture.
public class ResidentImport : AssetPostprocessor {
    void OnPreprocessModel() {
        if (!assetPath.Contains("Resources/Residents/")) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = false;
    }
}

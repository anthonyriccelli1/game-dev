using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Allows a requested build to run in an already-open Editor without opening
// another instance of the same project or discarding unsaved scene changes.
[InitializeOnLoad]
public static class PrototypeBuildRequest {
    const string Request = "prototype-build.request";
    const string Result = "prototype-build.result";
    static double nextCheck;
    static PrototypeBuildRequest() { EditorApplication.update += Check; }
    static void Check() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        if (!File.Exists(Request)) return;
        // Pick up script edits made outside the Editor (it may be unfocused with auto-refresh off).
        // If that starts a compile, keep the request; after the domain reload this check runs again.
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request);
        try {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++) {
                if (EditorSceneManager.GetSceneAt(i).isDirty) throw new Exception("Save your current scene before requesting a build; no unsaved work was changed.");
            }
            PrototypeBuilder.GenerateAndBuild();
            File.WriteAllText(Result, "PASS: scene generated, validated, and Windows player built.");
        } catch (Exception e) { File.WriteAllText(Result, "FAIL: " + e); Debug.LogException(e); }
    }
}

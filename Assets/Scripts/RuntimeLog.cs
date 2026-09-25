using System;
using System.IO;
using UnityEngine;

namespace RestaurantCity {
    // Mirrors warnings, errors and exceptions to runtime.log next to the game (build) or project (editor),
    // so problems seen during play can be diagnosed from the project folder.
    public static class RuntimeLog {
        static string path;
        static int lines;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() {
            try {
                path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "runtime.log"));
                File.WriteAllText(path, "Restaurant City runtime log " + DateTime.Now + "\n");
                Application.logMessageReceivedThreaded += Write;
            } catch { path = null; }
        }
        static void Write(string message, string stack, LogType type) {
            if (path == null || type == LogType.Log || lines > 400) return;
            lines++;
            try { lock (typeof(RuntimeLog)) File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {type}: {message}\n{(type == LogType.Exception || type == LogType.Error ? stack : "")}\n"); } catch { }
        }
    }
}

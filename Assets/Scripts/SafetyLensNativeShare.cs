using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Sheet nativo: iOS (UIActivityViewController) e Android (Intent + FileProvider).
    /// O caminho iOS permanece o mesmo; Android é ramo separado (#elif).
    /// </summary>
    public static class SafetyLensNativeShare
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void SafetyLens_ShareFiles(string pathsJoined, string subject);
#endif

        public static bool ShareFiles(string subject, params string[] paths)
        {
            if (paths == null || paths.Length == 0) return false;

            var existing = new List<string>(paths.Length);
            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    existing.Add(path);
            }

            if (existing.Count == 0) return false;

#if UNITY_IOS && !UNITY_EDITOR
            // iOS: inalterado — SafetyLensNativeShare.mm
            SafetyLens_ShareFiles(string.Join("|", existing), subject ?? "SafetyLens");
            return true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return ShareAndroid(subject ?? "SafetyLens", existing);
#else
            Debug.Log($"[SafetyLens] Compartilhar (Editor/outro): {string.Join(", ", existing)}");
            return true;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static bool ShareAndroid(string subject, List<string> paths)
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var intentClass = new AndroidJavaClass("android.content.Intent");
                var action = paths.Count == 1
                    ? intentClass.GetStatic<string>("ACTION_SEND")
                    : intentClass.GetStatic<string>("ACTION_SEND_MULTIPLE");
                using var intent = new AndroidJavaObject("android.content.Intent", action);
                var grantRead = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");

                intent.Call<AndroidJavaObject>("setType", "*/*");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
                intent.Call<AndroidJavaObject>("addFlags", grantRead);

                var authority = activity.Call<string>("getPackageName") + ".safetylens.fileprovider";
                using var fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider");

                if (paths.Count == 1)
                {
                    var file = new AndroidJavaObject("java.io.File", paths[0]);
                    var uri = fileProvider.CallStatic<AndroidJavaObject>(
                        "getUriForFile", activity, authority, file);
                    intent.Call<AndroidJavaObject>(
                        "putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                }
                else
                {
                    var uris = new AndroidJavaObject("java.util.ArrayList");
                    foreach (var path in paths)
                    {
                        var file = new AndroidJavaObject("java.io.File", path);
                        var uri = fileProvider.CallStatic<AndroidJavaObject>(
                            "getUriForFile", activity, authority, file);
                        uris.Call<bool>("add", uri);
                    }

                    intent.Call<AndroidJavaObject>(
                        "putParcelableArrayListExtra",
                        intentClass.GetStatic<string>("EXTRA_STREAM"),
                        uris);
                }

                var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, subject);
                activity.Call("startActivity", chooser);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SafetyLens] Share Android falhou: {ex.Message}");
                return false;
            }
        }
#endif
    }
}

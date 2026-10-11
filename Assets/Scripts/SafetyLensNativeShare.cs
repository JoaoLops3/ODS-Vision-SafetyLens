using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Abre o sheet nativo de compartilhamento no iOS (AirDrop, Mail, Arquivos, etc.).
    /// No Editor apenas registra o caminho — útil para validar o fluxo sem device.
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
            SafetyLens_ShareFiles(string.Join("|", existing), subject ?? "SafetyLens");
            return true;
#else
            Debug.Log($"[SafetyLens] Compartilhar (Editor/outro): {string.Join(", ", existing)}");
            return true;
#endif
        }
    }
}

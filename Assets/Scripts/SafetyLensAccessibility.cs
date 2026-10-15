using System.Runtime.InteropServices;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Preferências de acessibilidade do SO. Reduce motion desliga o pulso do anel AR.
    /// </summary>
    public static class SafetyLensAccessibility
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern bool SafetyLens_IsReduceMotionEnabled();
#endif

        /// <summary>
        /// Editor: defina <c>SAFETYLENS_REDUCE_MOTION</c> nos Scripting Define Symbols para simular.
        /// </summary>
        public static bool PrefersReducedMotion
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                try { return SafetyLens_IsReduceMotionEnabled(); }
                catch (System.EntryPointNotFoundException) { return false; }
#elif UNITY_ANDROID && !UNITY_EDITOR
                return ReadAndroidAnimatorDurationScale() <= 0.01f;
#elif SAFETYLENS_REDUCE_MOTION
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Escala do pulso do anel (1 = parado).</summary>
        public static float ActiveRingPulseScale(float realtimeSinceStartup)
        {
            if (PrefersReducedMotion) return 1f;
            return 1f + SafetyLensTheme.ArPulseAmplitude *
                   (0.5f + 0.5f * Mathf.Sin(
                       realtimeSinceStartup * SafetyLensTheme.ArPulseSpeed * Mathf.PI));
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static float ReadAndroidAnimatorDurationScale()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                using var settings = new AndroidJavaClass("android.provider.Settings$Global");
                return settings.CallStatic<float>(
                    "getFloat", resolver, "animator_duration_scale", 1f);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SafetyLens] Reduce motion Android indisponivel: {ex.Message}");
                return 1f;
            }
        }
#endif
    }
}

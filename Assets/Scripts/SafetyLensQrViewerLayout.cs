using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Layout do viewer QR (models na frente da câmera, fora do Image Target).
    /// Domínio puro: o marcador WL03 não usa esta classe.
    /// </summary>
    public static class SafetyLensQrViewerLayout
    {
        /// <summary>
        /// Posição local do hazard no pivot do viewer.
        /// Espalha HOT → PINCH → EPI na horizontal para toque e leitura claros.
        /// </summary>
        public static Vector3 LocalPositionForHazard(int index, int count)
        {
            if (count <= 0) return Vector3.zero;
            if (count == 1) return new Vector3(0f, SafetyLensTheme.QrViewerLift, 0f);

            var t = Mathf.Clamp01(index / (float)(count - 1));
            var x = Mathf.Lerp(-SafetyLensTheme.QrViewerSpreadX, SafetyLensTheme.QrViewerSpreadX, t);
            return new Vector3(x, SafetyLensTheme.QrViewerLift, 0f);
        }
    }
}

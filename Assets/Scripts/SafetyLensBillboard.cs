using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Rótulo sempre paralelo à tela e logo acima do objeto, independentemente de como o marcador
    /// está posicionado. TextMesh desenha a face legível no seu +Z local; copiar a rotação da câmera
    /// deixa esse +Z olhando para a MESMA direção que a câmera olha (ou seja, para longe dela), então
    /// quem vê o rótulo enxerga as costas do texto — por isso ele aparecia espelhado. Girando 180° em
    /// Y o +Z do rótulo passa a apontar de volta para a câmera, e o texto fica legível.
    /// </summary>
    public sealed class SafetyLensBillboard : MonoBehaviour
    {
        /// <summary>Objeto que o rótulo acompanha; o rótulo fica acima dele no "para cima" da tela.</summary>
        public Renderer anchor;
        public float gap;

        static readonly Quaternion FaceViewerOffset = Quaternion.Euler(0f, 180f, 0f);

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var camTransform = cam.transform;
            transform.rotation = camTransform.rotation * FaceViewerOffset;

            if (anchor == null) return;
            var bounds = anchor.bounds;
            var up = camTransform.up;
            var ext = bounds.extents;
            var extentAlongUp = Mathf.Abs(up.x) * ext.x + Mathf.Abs(up.y) * ext.y + Mathf.Abs(up.z) * ext.z;
            transform.position = bounds.center + up * (extentAlongUp + gap);
        }
    }
}

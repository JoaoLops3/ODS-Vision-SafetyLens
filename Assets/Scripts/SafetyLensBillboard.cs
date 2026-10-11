using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Rótulo sempre paralelo à tela e logo acima do objeto, independentemente de como o marcador
    /// está posicionado. TextMesh é legível quando o forward dele coincide com o da câmera; copiar a
    /// rotação da câmera evita espelhamento e o "texto torto" do LookRotation degenerado.
    /// </summary>
    public sealed class SafetyLensBillboard : MonoBehaviour
    {
        /// <summary>Objeto que o rótulo acompanha; o rótulo fica acima dele no "para cima" da tela.</summary>
        public Renderer anchor;
        public float gap;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var camTransform = cam.transform;
            transform.rotation = camTransform.rotation;

            if (anchor == null) return;
            var bounds = anchor.bounds;
            var up = camTransform.up;
            var ext = bounds.extents;
            var extentAlongUp = Mathf.Abs(up.x) * ext.x + Mathf.Abs(up.y) * ext.y + Mathf.Abs(up.z) * ext.z;
            transform.position = bounds.center + up * (extentAlongUp + gap);
        }
    }
}

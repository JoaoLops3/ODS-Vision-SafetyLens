using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Rótulo acima do objeto no "para cima" da tela. O plano de conteúdo já fica de frente para a
    /// câmera; aqui só garantimos texto legível (TextMesh olha +Z — 180° em Y evita verso espelhado)
    /// e posição ancorada no topo do renderer, sem orbitar o rótulo com cam.up no mundo.
    /// </summary>
    public sealed class SafetyLensBillboard : MonoBehaviour
    {
        /// <summary>Objeto que o rótulo acompanha; o rótulo fica acima dele no eixo local Y do pai.</summary>
        public Renderer anchor;
        public float gap;

        static readonly Quaternion FaceViewerOffset = Quaternion.Euler(0f, 180f, 0f);

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            transform.rotation = cam.transform.rotation * FaceViewerOffset;

            if (anchor == null) return;
            var bounds = anchor.bounds;
            transform.position = bounds.center + cam.transform.up * (bounds.extents.magnitude * 0.55f + gap);
        }
    }
}

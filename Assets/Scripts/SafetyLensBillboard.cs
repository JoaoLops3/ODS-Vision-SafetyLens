using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Rótulo legível na câmera AR. TextMesh quer +Z apontando para longe da câmera;
    /// rotação world (não local) funciona tanto com placa billboard quanto com EPI no marcador.
    /// </summary>
    public sealed class SafetyLensBillboard : MonoBehaviour
    {
        /// <summary>Objeto que o rótulo acompanha.</summary>
        public Renderer anchor;
        public float gap;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var awayFromCam = transform.position - cam.transform.position;
            if (awayFromCam.sqrMagnitude < 1e-8f) return;
            transform.rotation = Quaternion.LookRotation(awayFromCam.normalized, cam.transform.up);

            if (anchor == null) return;
            var bounds = anchor.bounds;
            var rise = Mathf.Max(bounds.extents.y, bounds.extents.magnitude * 0.35f);
            transform.position = bounds.center + cam.transform.up * (rise + gap);
        }
    }
}

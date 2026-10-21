using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>Marca clicável na superfície do compressor. O normal local é o da face, no espaço do giro.</summary>
    public sealed class SafetyLensQrMachinePoint : MonoBehaviour
    {
        public string WhatItIs { get; set; } = string.Empty;
        public string WhatCanHappen { get; set; } = string.Empty;
        public Vector3 LocalOutwardNormal { get; set; } = Vector3.back;

        Material _material;

        public void Bind(Material material) => _material = material;

        public void SetAppearance(Color color, float scale)
        {
            if (_material != null) _material.color = color;
            transform.localScale = Vector3.one * scale;
        }
    }
}

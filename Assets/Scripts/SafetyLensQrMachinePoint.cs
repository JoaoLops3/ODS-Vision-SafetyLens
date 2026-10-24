using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>Marca clicável na superfície do compressor. O normal local é o da face, no espaço do giro.</summary>
    public sealed class SafetyLensQrMachinePoint : MonoBehaviour
    {
        public string Name { get; set; } = string.Empty;
        public string WhatItIs { get; set; } = string.Empty;
        public string WhatCanHappen { get; set; } = string.Empty;
        public Vector3 LocalOutwardNormal { get; set; } = Vector3.back;

        /// <summary>A ficha deste ponto já abriu. O giro não apaga isso.</summary>
        public bool Read { get; private set; }

        Material _material;

        public void Bind(Material material) => _material = material;

        public void MarkRead() => Read = true;

        public void SetAppearance(Color color, float scale)
        {
            if (_material != null) _material.color = color;
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Lido fica verde mesmo de lado: o giro não pode esconder o que a pessoa já abriu.
        /// Sem leitura, amarelo é a face da frente e cinza é o resto.
        /// </summary>
        public static Color AppearanceColor(bool read, bool facing)
        {
            if (read) return SafetyLensTheme.HazardDone;
            return facing ? SafetyLensTheme.Brand : SafetyLensTheme.HazardIdle;
        }

        /// <summary>
        /// O TextMesh antigo transforma o pixel da fonte em unidade local com este fator.
        /// Sem ele a palavra nasce em metros e parece longe do ponto.
        /// </summary>
        public const float TextMeshPixelToLocal = 0.1f;

        /// <summary>
        /// Escala local para uma linha ficar na altura pedida, já descontando a escala do pai.
        /// </summary>
        public static float LabelLocalScale(float glyphPixels, float characterSize, float parentScale, float desiredWorldHeight)
        {
            var localLine = glyphPixels * characterSize * TextMeshPixelToLocal;
            if (localLine < 1e-5f || parentScale < 1e-4f || desiredWorldHeight <= 0f) return 0f;
            return desiredWorldHeight / (localLine * parentScale);
        }

        /// <summary>Quebra no primeiro espaço quando o nome não cabe numa linha curta.</summary>
        public static string FormatLabel(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 12) return name ?? string.Empty;
            var space = name.IndexOf(' ');
            if (space <= 0 || space >= name.Length - 1) return name;
            return name.Substring(0, space) + "\n" + name.Substring(space + 1);
        }
    }
}

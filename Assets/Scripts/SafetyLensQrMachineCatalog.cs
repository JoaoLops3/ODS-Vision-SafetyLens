using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Um ponto provisório na superfície do compressor.
    /// O texto é de mentira nesta fatia: queimadura, pinçamento e EPI entram depois, no mesmo cartão.
    /// </summary>
    public readonly struct SafetyLensQrMachineSpot
    {
        public SafetyLensQrMachineSpot(string whatItIs, string whatCanHappen, Vector3 localPosition, Vector3 localOutwardNormal)
        {
            WhatItIs = whatItIs;
            WhatCanHappen = whatCanHappen;
            LocalPosition = localPosition;
            LocalOutwardNormal = localOutwardNormal;
        }

        public string WhatItIs { get; }
        public string WhatCanHappen { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalOutwardNormal { get; }
    }

    /// <summary>
    /// Quais pontos da máquina aceitam toque.
    /// A face virada para a pessoa é a de maior alinhamento com o olhar, acima de um mínimo.
    /// Pontos na mesma face passam juntos. Lateral e trás ficam fechados até o giro.
    /// </summary>
    public static class SafetyLensQrMachineCatalog
    {
        /// <summary>Abaixo disto a face está de lado ou de costas: o toque não abre.</summary>
        public const float FacingMinDot = 0.35f;

        /// <summary>Folga para dois pontos da mesma face não se eliminarem por erro numérico.</summary>
        public const float FacingTieEpsilon = 0.08f;

        /// <summary>Centro da marca na boca do tubo, à frente de quem olha.</summary>
        public static readonly Vector3 OutletPoint = new Vector3(-0.03f, 0.33f, -0.30f);
        /// <summary>Centro da marca no tampão da extremidade direita do tanque.</summary>
        public static readonly Vector3 SidePoint = new Vector3(0.26f, 0.135f, 0f);
        /// <summary>Centro da marca na grade do ventilador, atrás do cabeçote.</summary>
        public static readonly Vector3 FanPoint = new Vector3(-0.03f, 0.255f, 0.22f);

        public static SafetyLensQrMachineSpot[] CreateSpots()
        {
            return new[]
            {
                new SafetyLensQrMachineSpot(
                    "Saída de ar quente",
                    "Queimar a mão",
                    OutletPoint,
                    Vector3.back),
                new SafetyLensQrMachineSpot(
                    "Tampa lateral",
                    "Uma peça pode soltar",
                    SidePoint,
                    Vector3.right),
                new SafetyLensQrMachineSpot(
                    "Grade do motor",
                    "Pode superaquecer",
                    FanPoint,
                    Vector3.forward)
            };
        }

        /// <summary>Alinhamento do normal com a direção até quem olha. -1 se o vetor for nulo.</summary>
        public static float DotTowardViewer(Vector3 outwardNormal, Vector3 fromPointToViewer)
        {
            if (outwardNormal.sqrMagnitude < 1e-8f || fromPointToViewer.sqrMagnitude < 1e-8f)
                return -1f;
            return Vector3.Dot(outwardNormal.normalized, fromPointToViewer.normalized);
        }

        /// <summary>
        /// True se este ponto está na face que olha para a pessoa.
        /// <paramref name="bestDot"/> é o maior alinhamento entre os pontos da máquina.
        /// </summary>
        public static bool IsOnViewerFace(float pointDot, float bestDot)
        {
            return pointDot >= FacingMinDot && pointDot >= bestDot - FacingTieEpsilon;
        }
    }
}

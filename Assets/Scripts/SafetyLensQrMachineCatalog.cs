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

        /// <summary>Centro da marca no tampão da extremidade direita do tanque.</summary>
        public static readonly Vector3 SidePoint = new Vector3(0.26f, 0.135f, 0f);

        public static SafetyLensQrMachineSpot[] CreateSpots()
        {
            return new[]
            {
                new SafetyLensQrMachineSpot(
                    "Tampa lateral",
                    "Uma peça pode soltar",
                    SidePoint,
                    Vector3.right)
            };
        }

        /// <summary>
        /// Os dois relógios do bloco cinza. O que é o medidor; o que pode acontecer é a eletricidade.
        /// A posição do FBX vem da textura, não deste vetor.
        /// </summary>
        public static SafetyLensQrMachineSpot GaugeSpot()
        {
            return new SafetyLensQrMachineSpot(
                "Medidor de pressão do ar",
                "Perigo de eletricidade",
                new Vector3(-0.03f, 0.30f, -0.12f),
                Vector3.back);
        }

        /// <summary>
        /// A mangueira amarela no alto da bomba. O cartão só diz que é a saída de ar:
        /// não dá para afirmar daqui se esse ar sai quente.
        /// A posição do FBX vem da malha, não deste vetor.
        /// </summary>
        public static SafetyLensQrMachineSpot AirOutletSpot()
        {
            return new SafetyLensQrMachineSpot(
                "Saída de ar",
                string.Empty,
                new Vector3(-0.03f, 0.33f, -0.30f),
                Vector3.left);
        }

        /// <summary>
        /// O disco com as faixas, ao lado da bomba. O que é o motor; o que pode acontecer é a queimadura.
        /// A posição do FBX vem da malha, não deste vetor.
        /// </summary>
        public static SafetyLensQrMachineSpot MotorSpot()
        {
            return new SafetyLensQrMachineSpot(
                "Motor",
                "Chance de queimadura",
                new Vector3(0.22f, 0.26f, 0.08f),
                Vector3.right);
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

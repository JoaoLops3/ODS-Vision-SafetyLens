using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Um ponto na superfície do compressor.
    /// O nome é o título da tabela. As duas frases são as linhas, curtas o bastante para caber no cartão.
    /// </summary>
    public readonly struct SafetyLensQrMachineSpot
    {
        public SafetyLensQrMachineSpot(
            string name,
            string whatItIs,
            string whatCanHappen,
            Vector3 localPosition,
            Vector3 localOutwardNormal)
        {
            Name = name;
            WhatItIs = whatItIs;
            WhatCanHappen = whatCanHappen;
            LocalPosition = localPosition;
            LocalOutwardNormal = localOutwardNormal;
        }

        public string Name { get; }
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
                    "Fecha a ponta do tanque, no lado sem a roda.",
                    "Com pressão, a tampa solta e sai com força.",
                    SidePoint,
                    Vector3.right)
            };
        }

        /// <summary>
        /// Os dois relógios do bloco cinza. O risco é pressão alta passar batida, não eletricidade.
        /// A posição do FBX vem da textura, não deste vetor.
        /// </summary>
        public static SafetyLensQrMachineSpot GaugeSpot()
        {
            return new SafetyLensQrMachineSpot(
                "Medidor de pressão",
                "Mostra quanta pressão tem o ar no tanque.",
                "Se a leitura falha, a pressão alta passa batida.",
                new Vector3(-0.03f, 0.30f, -0.12f),
                Vector3.back);
        }

        /// <summary>
        /// A mangueira amarela no alto da bomba. O risco é o jato e o chicote,
        /// não o calor: daqui não dá para afirmar se esse ar sai quente.
        /// A posição do FBX vem da malha, não deste vetor.
        /// </summary>
        public static SafetyLensQrMachineSpot AirOutletSpot()
        {
            return new SafetyLensQrMachineSpot(
                "Saída de ar",
                "Por aqui o ar comprimido sai da bomba.",
                "O jato no olho fere. A mangueira solta chicoteia.",
                new Vector3(-0.03f, 0.33f, -0.30f),
                Vector3.left);
        }

        /// <summary>
        /// O disco com as faixas, ao lado da bomba. A carcaça do motor esquenta.
        /// A posição do FBX vem da malha, não deste vetor.
        /// </summary>
        public static SafetyLensQrMachineSpot MotorSpot()
        {
            return new SafetyLensQrMachineSpot(
                "Motor",
                "Gira a bomba. A tampa cobre a carcaça.",
                "A carcaça esquenta e queima a mão.",
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

using System;
using UnityEngine;

namespace Facens.SafetyLens
{
    public enum AppPhase
    {
        Identification,
        Scanning,
        /// <summary>QR: scan único, depois o compressor no fundo escuro. O WL03 não entra aqui.</summary>
        QrMachine,
        /// <summary>Ficha do equipamento após o primeiro tracking; o relógio RF04 já corre.</summary>
        EquipmentBrief,
        RiskInspection,
        Quiz,
        /// <summary>RF12: leitura simulada do equipamento (WL05), depois do quiz.</summary>
        Telemetry,
        Finished,
        History
    }

    [Serializable]
    public sealed class SafetyLensScenario
    {
        public string id;
        public string title;
        public string targetName;
        public string intro;
        public SafetyLensHazard[] hazards;
        public SafetyLensQuizQuestion[] quiz;
        /// <summary>Image Targets alternativos (ex.: QR Code) com UVs próprios por hazard.</summary>
        public SafetyLensTargetLayout[] altTargets;
        /// <summary>Ficha do equipamento (nome, função, destaques de risco). Obrigatória no schema.</summary>
        public SafetyLensEquipment equipment;
        /// <summary>RF12: roteiro e limiares da leitura simulada. Ausente ou enabled=false pula a fase.</summary>
        public SafetyLensTelemetryConfig telemetry;

        /// <summary>True se o nome é o alvo principal ou um dos alternativos.</summary>
        public bool IsKnownTarget(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (string.Equals(name, targetName, StringComparison.Ordinal)) return true;
            if (altTargets == null) return false;
            foreach (var layout in altTargets)
                if (layout != null && string.Equals(layout.name, name, StringComparison.Ordinal))
                    return true;
            return false;
        }

        /// <summary>
        /// UV do hazard no alvo rastreado: o layout alternativo quando houver placement;
        /// senão, o u/v do próprio hazard (arte WL03, letras A/B/C).
        /// </summary>
        public Vector2 ResolveUv(SafetyLensHazard hazard, string trackedTargetName)
        {
            if (hazard == null) return Vector2.zero;

            if (altTargets != null
                && !string.IsNullOrEmpty(trackedTargetName)
                && !string.Equals(trackedTargetName, targetName, StringComparison.Ordinal))
            {
                foreach (var layout in altTargets)
                {
                    if (layout?.placements == null) continue;
                    if (!string.Equals(layout.name, trackedTargetName, StringComparison.Ordinal)) continue;
                    foreach (var placement in layout.placements)
                    {
                        if (placement != null && string.Equals(placement.hazardId, hazard.id, StringComparison.Ordinal))
                            return new Vector2(placement.u, placement.v);
                    }
                }
            }

            return new Vector2(hazard.u, hazard.v);
        }
    }

    /// <summary>Contexto do equipamento ancorado no marcador — painel da fase EquipmentBrief.</summary>
    [Serializable]
    public sealed class SafetyLensEquipment
    {
        public string name;
        public string function;
        public string summary;
        public string[] riskHighlights;
    }

    /// <summary>Layout de um Image Target alternativo: onde cada hazard ancora naquele marcador.</summary>
    [Serializable]
    public sealed class SafetyLensTargetLayout
    {
        public string name;
        public SafetyLensPlacement[] placements;
    }

    [Serializable]
    public sealed class SafetyLensPlacement
    {
        public string hazardId;
        public float u;
        public float v;
    }

    [Serializable]
    public sealed class SafetyLensHazard
    {
        public string id;
        public string label;
        /// <summary>alert = zona de risco; epi = equipamento de proteção.</summary>
        public string kind;
        public string instruction;
        /// <summary>Pictograma da placa (alert): hot | pinch | generic. Vazio = inferido pelo id.</summary>
        public string pictogram;
        /// <summary>Modelo 3D (epi): helmet. Vazio = inferido pelo id/label.</summary>
        public string model;
        /// <summary>Âncora UV no marcador: 0 = borda esquerda, 1 = direita.</summary>
        public float u;
        /// <summary>Âncora UV no marcador: 0 = borda inferior, 1 = superior.</summary>
        public float v;
        /// <summary>Ajuste fino em metros no plano do marcador (+X direita, +Y sobe a arte).</summary>
        public float x;
        public float y;
        /// <summary>Elevação em metros fora do plano do marcador (+Y do ARTrackedImage). 0 = token padrão.</summary>
        public float z;

        /// <summary>
        /// Posição local filha do ARTrackedImage (plano XZ, +Y saindo da arte).
        /// u/v mapeiam o ponto fixo da arte; size vem da pose rastreada (ou do tamanho de referência).
        /// </summary>
        public Vector3 LocalPositionOnMarker(Vector2 markerSizeMeters) =>
            LocalPositionOnMarker(markerSizeMeters, u, v);

        /// <summary>Mesma conta com UV externo (layout de Image Target alternativo, ex.: QR).</summary>
        public Vector3 LocalPositionOnMarker(Vector2 markerSizeMeters, float anchorU, float anchorV)
        {
            var width = markerSizeMeters.x > 1e-4f ? markerSizeMeters.x : SafetyLensTheme.ArMarkerWidth;
            var height = markerSizeMeters.y > 1e-4f ? markerSizeMeters.y : SafetyLensTheme.ArMarkerHeight;
            // z no JSON sobrescreve; senão EPI usa lift mínimo e placas o lift padrão.
            float lift;
            if (Mathf.Abs(z) > 1e-6f) lift = z;
            else if (IsEpi) lift = SafetyLensTheme.ArHelmetLift;
            else lift = SafetyLensTheme.ArMarkerLift;
            return new Vector3(
                (anchorU - 0.5f) * width + x,
                lift,
                (anchorV - 0.5f) * height + y);
        }
        public bool IsAlert => string.Equals(kind, "alert", StringComparison.OrdinalIgnoreCase);
        public bool IsEpi => string.Equals(kind, "epi", StringComparison.OrdinalIgnoreCase);

        public string ResolvedPictogram
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(pictogram)) return SafetyLensPictograms.Normalize(pictogram);
                var key = (id ?? string.Empty).ToUpperInvariant();
                if (key.Contains("HOT") || key.Contains("QUENTE")) return SafetyLensPictograms.Hot;
                if (key.Contains("PINCH") || key.Contains("PRENS") || key.Contains("PINC")) return SafetyLensPictograms.Pinch;
                return SafetyLensPictograms.Generic;
            }
        }

        public string ResolvedModel =>
            string.IsNullOrWhiteSpace(model) ? SafetyLensArVisuals.ModelHelmet : model.Trim().ToLowerInvariant();
    }

    [Serializable]
    public sealed class SafetyLensQuizQuestion
    {
        public int id;
        public string question;
        public string[] options;
        public int correctIndex;
    }

    /// <summary>
    /// Registro da sessão para RF06 (exportação local JSON/CSV).
    /// </summary>
    [Serializable]
    public sealed class SafetyLensSessionRecord
    {
        public string participantId;
        public string scenarioId;
        public string scenarioTitle;
        public float elapsedSeconds;
        /// <summary>RF04: do reconhecimento do marcador à confirmação do último risco (inclui a leitura da ficha).</summary>
        public float inspectionSeconds;
        /// <summary>RF04: da primeira pergunta à última resposta do quiz.</summary>
        public float quizSeconds;
        /// <summary>RNF02: FPS médio durante a inspeção em RA.</summary>
        public float fpsAverage;
        /// <summary>RNF02: percentil 1 de FPS (pior 1% dos frames) durante a inspeção.</summary>
        public float fpsLow1Percent;
        /// <summary>RNF02: fração dos frames abaixo de 30 FPS (0 a 1).</summary>
        public float framesBelow30Ratio;
        /// <summary>RNF02: pico de memória alocada (MB) observado na inspeção.</summary>
        public float peakMemoryMb;
        public int errorCount;
        public int hazardsConfirmed;
        public int hazardsTotal;
        public int quizCorrect;
        public int quizTotal;
        public string[] quizAnswers;
        /// <summary>Qual Image Target ancorou a sessão (WL03 ou QR) — variável de controle do experimento.</summary>
        public string markerName;
        public string startedAtIso;
        public string finishedAtIso;
        public string exportedAtIso;
        /// <summary>RF12: status da leitura no momento de concluir (NORMAL/ATENCAO/CRITICO). Vazio se a fase não rodou.</summary>
        public string telemetryStatus;
        /// <summary>RF12: temperatura (°C) no momento de concluir. Zero se a fase não rodou.</summary>
        public float temperature;
        /// <summary>RF12: pressão (bar) no momento de concluir. Zero se a fase não rodou.</summary>
        public float pressure;
        /// <summary>RF12: RPM no momento de concluir. Zero se a fase não rodou.</summary>
        public float rpm;
        /// <summary>Segundos do 1º tracking até Continuar na ficha. 0 se a ficha não concluiu.</summary>
        public float briefSeconds;
        /// <summary>Segundos do 1º tracking até o último hazard confirmado.</summary>
        public float inspectionEndedAtSeconds;
        /// <summary>Segundos do 1º tracking até a última resposta do quiz.</summary>
        public float quizEndedAtSeconds;
        /// <summary>Segundos do 1º tracking até Concluir telemetria. 0 se RF12 não rodou.</summary>
        public float telemetryEndedAtSeconds;
        /// <summary>Segundos do 1º tracking até o export (Finished).</summary>
        public float finishedAtSeconds;
    }

    public sealed class SafetyLensHazardTag : MonoBehaviour
    {
        public string hazardId;
    }
}

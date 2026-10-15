using System;
using UnityEngine;

namespace Facens.SafetyLens
{
    public enum AppPhase
    {
        Identification,
        Scanning,
        RiskInspection,
        Quiz,
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
        public Vector3 LocalPositionOnMarker(Vector2 markerSizeMeters)
        {
            var width = markerSizeMeters.x > 1e-4f ? markerSizeMeters.x : SafetyLensTheme.ArMarkerWidth;
            var height = markerSizeMeters.y > 1e-4f ? markerSizeMeters.y : SafetyLensTheme.ArMarkerHeight;
            // z no JSON sobrescreve; senão EPI usa lift mínimo e placas o lift padrão.
            float lift;
            if (Mathf.Abs(z) > 1e-6f) lift = z;
            else if (IsEpi) lift = SafetyLensTheme.ArHelmetLift;
            else lift = SafetyLensTheme.ArMarkerLift;
            return new Vector3(
                (u - 0.5f) * width + x,
                lift,
                (v - 0.5f) * height + y);
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
        /// <summary>RF04: do reconhecimento do marcador à confirmação do último risco.</summary>
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
        public string startedAtIso;
        public string finishedAtIso;
        public string exportedAtIso;
    }

    public sealed class SafetyLensHazardTag : MonoBehaviour
    {
        public string hazardId;
    }
}

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
        public float x;
        public float y;
        public float z;

        /// <summary>
        /// Posição no plano de leitura (de frente para a câmera). O +Z do conteúdo aponta para a
        /// câmera, então o +X local aparece à esquerda na tela — por isso o X do JSON é negado:
        /// x positivo = direita na tela, x negativo = esquerda. Y = cima/baixo.
        /// </summary>
        public Vector3 LocalPosition => new Vector3(-x, y, 0f);
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

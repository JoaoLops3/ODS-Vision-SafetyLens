using System;
using System.Collections.Generic;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Domínio puro da inspeção + quiz (sem AR / UnityEngine). O controller só orquestra câmera e UI.
    /// </summary>
    public sealed class SafetyLensInspectionProgress
    {
        readonly bool[] _hazardDone;
        readonly List<string> _quizAnswers = new();

        public SafetyLensInspectionProgress(int hazardCount)
        {
            if (hazardCount < 0) throw new ArgumentOutOfRangeException(nameof(hazardCount));
            _hazardDone = new bool[hazardCount];
        }

        public int ActiveHazardIndex { get; private set; }
        public int ActiveQuizIndex { get; private set; }
        public int QuizCorrect { get; private set; }
        public int ErrorCount { get; private set; }
        public bool CurrentHazardArSelected { get; private set; }
        public int HazardConfirmedCount
        {
            get
            {
                var n = 0;
                foreach (var done in _hazardDone) if (done) n++;
                return n;
            }
        }

        public IReadOnlyList<string> QuizAnswers => _quizAnswers;
        public bool IsHazardDone(int index) =>
            index >= 0 && index < _hazardDone.Length && _hazardDone[index];

        /// <summary>Incrementa o núcleo de erros da pesquisa (toque errado ou quiz errado).</summary>
        public void RegisterError() => ErrorCount++;

        /// <summary>Marca o item ativo como selecionado via toque AR. False se já estava selecionado.</summary>
        public bool SelectCurrentHazardFromAr()
        {
            if (CurrentHazardArSelected) return false;
            CurrentHazardArSelected = true;
            return true;
        }

        /// <summary>Zera a seleção AR (toque no item errado ou marcador perdido).</summary>
        public void ClearArSelection() => CurrentHazardArSelected = false;

        /// <summary>Confirma o hazard ativo. Retorna true se a inspeção terminou (ir ao quiz).</summary>
        public bool ConfirmCurrentHazard(int hazardCount)
        {
            if (!CurrentHazardArSelected) return false;
            if (ActiveHazardIndex < 0 || ActiveHazardIndex >= _hazardDone.Length) return false;

            _hazardDone[ActiveHazardIndex] = true;
            CurrentHazardArSelected = false;

            if (ActiveHazardIndex >= hazardCount - 1)
            {
                ActiveQuizIndex = 0;
                return true;
            }

            ActiveHazardIndex++;
            return false;
        }

        /// <summary>Registra resposta. Retorna false se inválida.</summary>
        public bool TryAnswerQuiz(
            SafetyLensQuizQuestion question,
            int optionIndex,
            int quizCount,
            out bool correct,
            out bool isLastQuestion)
        {
            correct = false;
            isLastQuestion = false;
            if (question?.options == null) return false;
            if (optionIndex < 0 || optionIndex >= question.options.Length) return false;

            var selected = question.options[optionIndex];
            correct = optionIndex == question.correctIndex;
            _quizAnswers.Add($"Q{question.id}:{selected}:{(correct ? "OK" : "ERR")}");
            if (correct) QuizCorrect++;
            else ErrorCount++;

            isLastQuestion = ActiveQuizIndex >= quizCount - 1;
            return true;
        }

        /// <summary>Avança o quiz. Retorna true se a sessão deve finalizar.</summary>
        public bool NextQuiz(int quizCount)
        {
            if (ActiveQuizIndex >= quizCount - 1) return true;
            ActiveQuizIndex++;
            return false;
        }

        /// <summary>Monta o registro de export. Tempos e FPS vêm do facade; erros vêm daqui.</summary>
        public SafetyLensSessionRecord BuildRecord(
            string participantId,
            SafetyLensScenario scenario,
            float elapsedSeconds,
            float inspectionSeconds,
            float quizSeconds,
            SafetyLensPerformanceSummary performance,
            string markerName,
            string startedAtIso,
            string finishedAtIso,
            string telemetryStatus = null,
            float temperature = 0f,
            float pressure = 0f,
            float rpm = 0f,
            float briefSeconds = 0f,
            float inspectionEndedAtSeconds = 0f,
            float quizEndedAtSeconds = 0f,
            float telemetryEndedAtSeconds = 0f,
            float finishedAtSeconds = 0f)
        {
            return new SafetyLensSessionRecord
            {
                participantId = participantId,
                scenarioId = scenario?.id,
                scenarioTitle = scenario?.title,
                elapsedSeconds = elapsedSeconds,
                inspectionSeconds = inspectionSeconds,
                quizSeconds = quizSeconds,
                fpsAverage = performance.FpsAverage,
                fpsLow1Percent = performance.FpsLow1Percent,
                framesBelow30Ratio = performance.FramesBelow30Ratio,
                peakMemoryMb = performance.PeakMemoryMb,
                errorCount = ErrorCount,
                hazardsConfirmed = HazardConfirmedCount,
                hazardsTotal = _hazardDone.Length,
                quizCorrect = QuizCorrect,
                quizTotal = scenario?.quiz?.Length ?? 0,
                quizAnswers = _quizAnswers.ToArray(),
                markerName = markerName,
                startedAtIso = startedAtIso,
                finishedAtIso = finishedAtIso,
                telemetryStatus = telemetryStatus ?? string.Empty,
                temperature = temperature,
                pressure = pressure,
                rpm = rpm,
                briefSeconds = briefSeconds,
                inspectionEndedAtSeconds = inspectionEndedAtSeconds,
                quizEndedAtSeconds = quizEndedAtSeconds,
                telemetryEndedAtSeconds = telemetryEndedAtSeconds,
                finishedAtSeconds = finishedAtSeconds
            };
        }
    }
}

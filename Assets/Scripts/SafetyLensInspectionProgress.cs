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

        public void RegisterError() => ErrorCount++;

        public bool SelectCurrentHazardFromAr()
        {
            if (CurrentHazardArSelected) return false;
            CurrentHazardArSelected = true;
            return true;
        }

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

        public SafetyLensSessionRecord BuildRecord(
            string participantId,
            SafetyLensScenario scenario,
            float elapsedSeconds,
            string startedAtIso,
            string finishedAtIso)
        {
            return new SafetyLensSessionRecord
            {
                participantId = participantId,
                scenarioId = scenario?.id,
                scenarioTitle = scenario?.title,
                elapsedSeconds = elapsedSeconds,
                errorCount = ErrorCount,
                hazardsConfirmed = HazardConfirmedCount,
                hazardsTotal = _hazardDone.Length,
                quizCorrect = QuizCorrect,
                quizTotal = scenario?.quiz?.Length ?? 0,
                quizAnswers = _quizAnswers.ToArray(),
                startedAtIso = startedAtIso,
                finishedAtIso = finishedAtIso
            };
        }
    }
}

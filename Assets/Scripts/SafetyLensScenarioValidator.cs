using System;
using System.Collections.Generic;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Validação de schema do cenário (boot + Editor). Sem UnityEngine — testável em EditMode.
    /// </summary>
    public static class SafetyLensScenarioValidator
    {
        public static List<string> Validate(SafetyLensScenario scenario, string expectedTargetName = null)
        {
            var errors = new List<string>();
            if (scenario == null)
            {
                errors.Add("Cenario nulo.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(scenario.id))
                errors.Add("Cenario sem id.");
            if (string.IsNullOrWhiteSpace(scenario.title))
                errors.Add("Cenario sem title.");
            if (string.IsNullOrWhiteSpace(scenario.targetName))
                errors.Add("Cenario sem targetName.");
            else if (!string.IsNullOrEmpty(expectedTargetName)
                     && !string.Equals(scenario.targetName, expectedTargetName, StringComparison.Ordinal))
                errors.Add($"targetName '{scenario.targetName}' deve ser '{expectedTargetName}'.");

            if (scenario.hazards == null || scenario.hazards.Length == 0)
                errors.Add("Cenario sem hazards.");
            else
            {
                for (var i = 0; i < scenario.hazards.Length; i++)
                    ValidateHazard(scenario.hazards[i], i, errors);
            }

            if (scenario.quiz == null || scenario.quiz.Length == 0)
                errors.Add("Cenario sem quiz.");
            else
            {
                for (var i = 0; i < scenario.quiz.Length; i++)
                    ValidateQuiz(scenario.quiz[i], i, errors);
            }

            return errors;
        }

        static void ValidateHazard(SafetyLensHazard h, int index, List<string> errors)
        {
            var label = h == null || string.IsNullOrWhiteSpace(h.id) ? $"hazards[{index}]" : h.id;
            if (h == null)
            {
                errors.Add($"{label}: entrada nula.");
                return;
            }

            if (string.IsNullOrWhiteSpace(h.id))
                errors.Add($"{label}: id vazio.");
            if (string.IsNullOrWhiteSpace(h.label))
                errors.Add($"{label}: label vazio.");
            if (!h.IsAlert && !h.IsEpi)
                errors.Add($"{label}: kind deve ser 'alert' ou 'epi' (atual: '{h.kind}').");
            if (h.u < 0f || h.u > 1f || h.v < 0f || h.v > 1f)
                errors.Add($"{label}: UV fora de [0,1] (u={h.u}, v={h.v}).");
        }

        static void ValidateQuiz(SafetyLensQuizQuestion q, int index, List<string> errors)
        {
            var label = q == null ? $"quiz[{index}]" : $"quiz Q{q.id}";
            if (q == null)
            {
                errors.Add($"{label}: entrada nula.");
                return;
            }

            if (string.IsNullOrWhiteSpace(q.question))
                errors.Add($"{label}: question vazia.");
            if (q.options == null || q.options.Length < 2)
                errors.Add($"{label}: options precisa de pelo menos 2 alternativas.");
            else if (q.correctIndex < 0 || q.correctIndex >= q.options.Length)
                errors.Add($"{label}: correctIndex {q.correctIndex} fora de [0, {q.options.Length - 1}].");
        }
    }
}

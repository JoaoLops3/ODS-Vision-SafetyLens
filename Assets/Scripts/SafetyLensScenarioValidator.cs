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

            if (scenario.altTargets != null && scenario.altTargets.Length > 0)
                ValidateAltTargets(scenario, errors);

            ValidateEquipment(scenario.equipment, errors);
            ValidateTelemetry(scenario.telemetry, errors);

            return errors;
        }

        /// <summary>Ficha obrigatória: nome, função, resumo e pelo menos um destaque de risco.</summary>
        static void ValidateEquipment(SafetyLensEquipment equipment, List<string> errors)
        {
            if (equipment == null)
            {
                errors.Add("equipment ausente (ficha do equipamento).");
                return;
            }

            if (string.IsNullOrWhiteSpace(equipment.name))
                errors.Add("equipment.name vazio.");
            if (string.IsNullOrWhiteSpace(equipment.function))
                errors.Add("equipment.function vazio.");
            if (string.IsNullOrWhiteSpace(equipment.summary))
                errors.Add("equipment.summary vazio.");

            if (equipment.riskHighlights == null || equipment.riskHighlights.Length == 0)
            {
                errors.Add("equipment.riskHighlights vazio.");
                return;
            }

            if (equipment.riskHighlights.Length > 4)
                errors.Add("equipment.riskHighlights: no máximo 4 destaques.");

            for (var i = 0; i < equipment.riskHighlights.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(equipment.riskHighlights[i]))
                    errors.Add($"equipment.riskHighlights[{i}] vazio.");
            }
        }

        /// <summary>
        /// RF12: com enabled=false (ou bloco nulo) não exige o restante.
        /// Com enabled=true: amostra em t=0, tempos crescentes, críticos &gt; atenção.
        /// </summary>
        static void ValidateTelemetry(SafetyLensTelemetryConfig telemetry, List<string> errors)
        {
            if (telemetry == null || !telemetry.enabled) return;

            if (string.IsNullOrWhiteSpace(telemetry.equipmentName))
                errors.Add("telemetry.equipmentName vazio.");

            var thresholds = telemetry.thresholds;
            if (thresholds == null)
            {
                errors.Add("telemetry.thresholds ausente.");
            }
            else
            {
                if (thresholds.temperatureCritical <= thresholds.temperatureWarning)
                    errors.Add("telemetry: temperatureCritical deve ser maior que temperatureWarning.");
                if (thresholds.pressureCritical <= thresholds.pressureWarning)
                    errors.Add("telemetry: pressureCritical deve ser maior que pressureWarning.");
                if (thresholds.rpmCritical <= thresholds.rpmWarning)
                    errors.Add("telemetry: rpmCritical deve ser maior que rpmWarning.");
            }

            var copy = telemetry.statusCopy;
            if (copy == null)
                errors.Add("telemetry.statusCopy ausente.");
            else
            {
                if (string.IsNullOrWhiteSpace(copy.normal))
                    errors.Add("telemetry.statusCopy.normal vazio.");
                if (string.IsNullOrWhiteSpace(copy.warning))
                    errors.Add("telemetry.statusCopy.warning vazio.");
                if (string.IsNullOrWhiteSpace(copy.critical))
                    errors.Add("telemetry.statusCopy.critical vazio.");
            }

            if (telemetry.samples == null || telemetry.samples.Length == 0)
            {
                errors.Add("telemetry.samples vazio.");
                return;
            }

            var hasZero = false;
            float? previous = null;
            for (var i = 0; i < telemetry.samples.Length; i++)
            {
                var sample = telemetry.samples[i];
                var label = $"telemetry.samples[{i}]";
                if (sample == null)
                {
                    errors.Add($"{label}: entrada nula.");
                    continue;
                }

                if (sample.atSeconds < 0f)
                    errors.Add($"{label}: atSeconds negativo ({sample.atSeconds}).");
                if (Math.Abs(sample.atSeconds) < 1e-6f)
                    hasZero = true;
                if (previous.HasValue && sample.atSeconds <= previous.Value)
                    errors.Add($"{label}: atSeconds deve ser estritamente crescente.");
                previous = sample.atSeconds;
            }

            if (!hasZero)
                errors.Add("telemetry.samples precisa de uma amostra com atSeconds == 0.");
        }

        /// <summary>
        /// Layouts de Image Targets alternativos (ex.: QR): nome único e diferente do principal,
        /// e exatamente um placement válido para cada hazard do cenário.
        /// </summary>
        static void ValidateAltTargets(SafetyLensScenario scenario, List<string> errors)
        {
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < scenario.altTargets.Length; i++)
            {
                var layout = scenario.altTargets[i];
                var label = layout == null || string.IsNullOrWhiteSpace(layout.name)
                    ? $"altTargets[{i}]"
                    : $"altTarget '{layout.name}'";

                if (layout == null)
                {
                    errors.Add($"{label}: entrada nula.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(layout.name))
                    errors.Add($"{label}: name vazio.");
                else if (string.Equals(layout.name, scenario.targetName, StringComparison.Ordinal))
                    errors.Add($"{label}: name igual ao targetName principal.");
                else if (!seenNames.Add(layout.name))
                    errors.Add($"{label}: name duplicado.");

                if (layout.placements == null || layout.placements.Length == 0)
                {
                    errors.Add($"{label}: sem placements.");
                    continue;
                }

                var placedHazards = new HashSet<string>(StringComparer.Ordinal);
                foreach (var placement in layout.placements)
                {
                    if (placement == null || string.IsNullOrWhiteSpace(placement.hazardId))
                    {
                        errors.Add($"{label}: placement sem hazardId.");
                        continue;
                    }

                    if (!HazardExists(scenario, placement.hazardId))
                        errors.Add($"{label}: hazardId '{placement.hazardId}' nao existe no cenario.");
                    else if (!placedHazards.Add(placement.hazardId))
                        errors.Add($"{label}: hazardId '{placement.hazardId}' duplicado.");

                    if (placement.u < 0f || placement.u > 1f || placement.v < 0f || placement.v > 1f)
                        errors.Add(
                            $"{label}: UV de '{placement.hazardId}' fora de [0,1] (u={placement.u}, v={placement.v}).");
                }

                if (scenario.hazards != null)
                {
                    foreach (var hazard in scenario.hazards)
                    {
                        if (hazard != null && !string.IsNullOrWhiteSpace(hazard.id) && !placedHazards.Contains(hazard.id))
                            errors.Add($"{label}: falta placement para o hazard '{hazard.id}'.");
                    }
                }
            }
        }

        static bool HazardExists(SafetyLensScenario scenario, string hazardId)
        {
            if (scenario.hazards == null) return false;
            foreach (var hazard in scenario.hazards)
                if (hazard != null && string.Equals(hazard.id, hazardId, StringComparison.Ordinal))
                    return true;
            return false;
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

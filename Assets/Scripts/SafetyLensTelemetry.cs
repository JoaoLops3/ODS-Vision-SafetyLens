using System;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Domínio puro da leitura simulada (WL05): escolhe a amostra pelo tempo da fase e
    /// classifica o status pelos limiares. Sem UI, sem MonoBehaviour — testável em EditMode.
    /// </summary>
    public static class SafetyLensTelemetry
    {
        public static bool IsEnabled(SafetyLensScenario scenario) =>
            scenario?.telemetry != null && scenario.telemetry.enabled;

        /// <summary>
        /// Mesma regra do WL05StatusEvaluator: crítico se qualquer grandeza passa do limiar
        /// crítico; atenção se qualquer uma passa do de atenção; senão normal.
        /// </summary>
        public static SafetyLensTelemetryStatus Evaluate(
            SafetyLensTelemetrySample sample,
            SafetyLensTelemetryThresholds thresholds)
        {
            if (sample == null || thresholds == null)
                return SafetyLensTelemetryStatus.Normal;

            if (sample.temperature >= thresholds.temperatureCritical
                || sample.pressure >= thresholds.pressureCritical
                || sample.rpm >= thresholds.rpmCritical)
                return SafetyLensTelemetryStatus.Critical;

            if (sample.temperature >= thresholds.temperatureWarning
                || sample.pressure >= thresholds.pressureWarning
                || sample.rpm >= thresholds.rpmWarning)
                return SafetyLensTelemetryStatus.Warning;

            return SafetyLensTelemetryStatus.Normal;
        }

        /// <summary>
        /// Última amostra com <c>atSeconds &lt;= t</c>. Antes da primeira (ou t negativo),
        /// devolve a amostra em <c>atSeconds == 0</c> quando existir; senão a de menor tempo.
        /// </summary>
        public static SafetyLensTelemetrySample SampleAt(SafetyLensTelemetrySample[] samples, float t)
        {
            if (samples == null || samples.Length == 0) return null;

            SafetyLensTelemetrySample best = null;
            SafetyLensTelemetrySample earliest = null;
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                if (sample == null) continue;

                if (earliest == null || sample.atSeconds < earliest.atSeconds)
                    earliest = sample;

                if (sample.atSeconds <= t
                    && (best == null || sample.atSeconds > best.atSeconds))
                    best = sample;
            }

            return best ?? earliest;
        }

        public static string Label(SafetyLensTelemetryStatus status) =>
            status switch
            {
                SafetyLensTelemetryStatus.Critical => "CRITICO",
                SafetyLensTelemetryStatus.Warning => "ATENCAO",
                _ => "NORMAL"
            };

        /// <summary>Rótulo da UI (Ideal / Cuidado / Perigoso). O CSV continua com <see cref="Label"/>.</summary>
        public static string NarrativeLabel(SafetyLensTelemetryStatus status) =>
            status switch
            {
                SafetyLensTelemetryStatus.Critical => "Perigoso",
                SafetyLensTelemetryStatus.Warning => "Cuidado",
                _ => "Ideal"
            };

        /// <summary>Frase do JSON para o status atual. Vazio se o bloco statusCopy faltar.</summary>
        public static string NarrativeCopy(SafetyLensTelemetryConfig config, SafetyLensTelemetryStatus status)
        {
            var copy = config?.statusCopy;
            if (copy == null) return string.Empty;
            return status switch
            {
                SafetyLensTelemetryStatus.Critical => copy.critical ?? string.Empty,
                SafetyLensTelemetryStatus.Warning => copy.warning ?? string.Empty,
                _ => copy.normal ?? string.Empty
            };
        }
    }

    public enum SafetyLensTelemetryStatus
    {
        Normal,
        Warning,
        Critical
    }

    [Serializable]
    public sealed class SafetyLensTelemetryConfig
    {
        public bool enabled;
        public string equipmentName;
        public SafetyLensTelemetryThresholds thresholds;
        public SafetyLensTelemetrySample[] samples;
        /// <summary>Frases Ideal / Cuidado / Perigoso na UI. CSV de status permanece NORMAL/ATENCAO/CRITICO.</summary>
        public SafetyLensTelemetryStatusCopy statusCopy;
    }

    [Serializable]
    public sealed class SafetyLensTelemetryStatusCopy
    {
        public string normal;
        public string warning;
        public string critical;
    }

    [Serializable]
    public sealed class SafetyLensTelemetryThresholds
    {
        public float temperatureWarning = 50f;
        public float temperatureCritical = 70f;
        public float pressureWarning = 2.8f;
        public float pressureCritical = 3.2f;
        public float rpmWarning = 1600f;
        public float rpmCritical = 1800f;
    }

    [Serializable]
    public sealed class SafetyLensTelemetrySample
    {
        public float atSeconds;
        public float temperature;
        public float pressure;
        public float rpm;
    }
}

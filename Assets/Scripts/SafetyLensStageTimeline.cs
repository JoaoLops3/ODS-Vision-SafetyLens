using System;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Instantes das etapas da jornada, relativos ao primeiro tracking.
    /// Paralelo ao <see cref="SafetyLensSessionClock"/>: não altera as fórmulas RF04.
    /// </summary>
    public sealed class SafetyLensStageTimeline
    {
        float _markerAt = -1f;
        float _briefAt = -1f;
        float _inspectionEndedAt = -1f;
        float _quizEndedAt = -1f;
        float _telemetryEndedAt = -1f;
        float _finishedAt = -1f;

        /// <summary>Zera todos os Marks (nova sessão / identificação).</summary>
        public void Reset()
        {
            _markerAt = -1f;
            _briefAt = -1f;
            _inspectionEndedAt = -1f;
            _quizEndedAt = -1f;
            _telemetryEndedAt = -1f;
            _finishedAt = -1f;
        }

        /// <summary>Âncora t=0: primeiro tracking. Segunda chamada é no-op.</summary>
        public void MarkMarkerFound(float now)
        {
            if (_markerAt >= 0f) return;
            _markerAt = now;
        }

        /// <summary>Continuar na ficha. No-op se já marcado.</summary>
        public void MarkBriefDone(float now)
        {
            if (_briefAt >= 0f) return;
            _briefAt = now;
        }

        /// <summary>Último hazard confirmado. No-op se já marcado.</summary>
        public void MarkInspectionEnded(float now)
        {
            if (_inspectionEndedAt >= 0f) return;
            _inspectionEndedAt = now;
        }

        /// <summary>Última resposta do quiz. No-op se já marcado.</summary>
        public void MarkQuizEnded(float now)
        {
            if (_quizEndedAt >= 0f) return;
            _quizEndedAt = now;
        }

        /// <summary>
        /// Concluir telemetria. Só chamar em <c>CompleteTelemetry</c>.
        /// Sem RF12 este Mark não ocorre e <see cref="TelemetryEndedAtSeconds"/> fica 0.
        /// </summary>
        public void MarkTelemetryEnded(float now)
        {
            if (_telemetryEndedAt >= 0f) return;
            _telemetryEndedAt = now;
        }

        /// <summary>Entrada em Finished / export. No-op se já marcado.</summary>
        public void MarkFinished(float now)
        {
            if (_finishedAt >= 0f) return;
            _finishedAt = now;
        }

        /// <summary>Segundos do tracking até Continuar. 0 se a ficha não concluiu.</summary>
        public float BriefSeconds => Relative(_briefAt);

        /// <summary>Segundos do tracking até o último hazard.</summary>
        public float InspectionEndedAtSeconds => Relative(_inspectionEndedAt);

        /// <summary>Segundos do tracking até a última resposta do quiz.</summary>
        public float QuizEndedAtSeconds => Relative(_quizEndedAt);

        /// <summary>Segundos do tracking até Concluir leitura. 0 se RF12 não rodou.</summary>
        public float TelemetryEndedAtSeconds => Relative(_telemetryEndedAt);

        /// <summary>Segundos do tracking até o export.</summary>
        public float FinishedAtSeconds => Relative(_finishedAt);

        float Relative(float mark)
        {
            if (_markerAt < 0f || mark < 0f) return 0f;
            return Math.Max(0f, mark - _markerAt);
        }
    }
}

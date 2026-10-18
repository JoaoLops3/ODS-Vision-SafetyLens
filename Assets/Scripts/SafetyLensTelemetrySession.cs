using System;
using UnityEngine;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Estado da fase RF12 (leitura simulada). Não toca nos relógios de inspeção/quiz/elapsed.
    /// </summary>
    public sealed class SafetyLensTelemetrySession
    {
        float _startedAt = -1f;
        SafetyLensTelemetrySample _sample;
        SafetyLensTelemetryStatus _status = SafetyLensTelemetryStatus.Normal;
        DateTime _sampleLocalTime;

        /// <summary>Última amostra exibida. Null se a fase não rodou.</summary>
        public SafetyLensTelemetrySample Sample => _sample;

        /// <summary>Status calculado da amostra corrente.</summary>
        public SafetyLensTelemetryStatus Status => _status;

        /// <summary>Horário local da última atualização de amostra.</summary>
        public DateTime SampleLocalTime => _sampleLocalTime;

        /// <summary>True se alguma amostra foi publicada (export preenche colunas RF12).</summary>
        public bool HasSample => _sample != null;

        /// <summary>Inicia o relógio da fase (independente do clock de pesquisa).</summary>
        public void Begin(float now)
        {
            _startedAt = now;
            _sample = null;
            _status = SafetyLensTelemetryStatus.Normal;
            _sampleLocalTime = default;
        }

        /// <summary>Encerra o relógio da fase após o export. Mantém a amostra para o registro.</summary>
        public void StopPhaseClock()
        {
            _startedAt = -1f;
        }

        /// <summary>Limpa amostra e relógio (nova sessão / identificação).</summary>
        public void Reset()
        {
            _startedAt = -1f;
            _sample = null;
            _status = SafetyLensTelemetryStatus.Normal;
            _sampleLocalTime = default;
        }

        /// <summary>
        /// Segundos desde <see cref="Begin"/>. Zero se a fase não está ativa ou o relógio foi parado.
        /// </summary>
        public float PhaseSeconds(float now, bool phaseIsTelemetry)
        {
            if (!phaseIsTelemetry || _startedAt < 0f) return 0f;
            return Mathf.Max(0f, now - _startedAt);
        }

        /// <summary>
        /// Atualiza amostra/status pelo roteiro. Retorna true se a leitura mudou
        /// (ou se <paramref name="force"/>).
        /// </summary>
        public bool Refresh(SafetyLensTelemetryConfig config, float phaseSeconds, bool force)
        {
            if (config?.samples == null) return false;
            var sample = SafetyLensTelemetry.SampleAt(config.samples, phaseSeconds);
            if (sample == null) return false;

            var status = SafetyLensTelemetry.Evaluate(sample, config.thresholds);
            if (!force
                && _sample != null
                && Mathf.Approximately(_sample.atSeconds, sample.atSeconds)
                && _status == status)
                return false;

            _sample = sample;
            _status = status;
            _sampleLocalTime = DateTime.Now;
            return true;
        }
    }
}

using System;

namespace Facens.SafetyLens
{
    /// <summary>
    /// Relógios da sessão de pesquisa (RF04). Classe pura: o instante atual é injetado
    /// para os testes reproduzirem as mesmas fórmulas do controller legado.
    /// Telemetria (RF12) não chama nenhum Mark deste relógio.
    /// </summary>
    public sealed class SafetyLensSessionClock
    {
        float _startedAt = -1f;
        float _finishedAt = -1f;
        float _inspectionEndedAt = -1f;
        DateTime _startedUtc;
        DateTime _finishedUtc;

        /// <summary>True após o primeiro <see cref="MarkInspectionStarted"/> (primeiro tracking).</summary>
        public bool HasInspectionStarted => _startedAt >= 0f;

        /// <summary>True após a confirmação do último risco.</summary>
        public bool HasInspectionEnded => _inspectionEndedAt >= 0f;

        /// <summary>True após a última resposta do quiz (congela elapsed e quiz).</summary>
        public bool HasQuizFinished => _finishedAt >= 0f;

        /// <summary>UTC do primeiro tracking. Default se a inspeção ainda não começou.</summary>
        public DateTime StartedUtc => _startedUtc;

        /// <summary>UTC da última resposta do quiz (ou do fallback de <see cref="EnsureFinished"/>).</summary>
        public DateTime FinishedUtc => _finishedUtc;

        /// <summary>Zera os três instantes. Não altera ErrorCount (isso vive no progresso).</summary>
        public void Reset()
        {
            _startedAt = -1f;
            _finishedAt = -1f;
            _inspectionEndedAt = -1f;
            _startedUtc = default;
            _finishedUtc = default;
        }

        /// <summary>
        /// RF04: começa no primeiro tracking. Reaquisições não zeraram o tempo no legado
        /// e não zeram aqui — segunda chamada é no-op.
        /// </summary>
        public void MarkInspectionStarted(float now, DateTime utcNow)
        {
            if (_startedAt >= 0f) return;
            _startedAt = now;
            _startedUtc = utcNow;
        }

        /// <summary>Fecha o tempo de inspeção na confirmação do último hazard.</summary>
        public void MarkInspectionEnded(float now)
        {
            if (_inspectionEndedAt >= 0f) return;
            _inspectionEndedAt = now;
        }

        /// <summary>Fecha elapsed e quiz na última resposta — não no tempo de leitura do resultado.</summary>
        public void MarkQuizFinished(float now, DateTime utcNow)
        {
            if (_finishedAt >= 0f) return;
            _finishedAt = now;
            _finishedUtc = utcNow;
        }

        /// <summary>
        /// Fallback do export: se o quiz não marcou o fim (ex.: sessão sem perguntas),
        /// congela agora. Se já marcado, não sobrescreve.
        /// </summary>
        public void EnsureFinished(float now, DateTime utcNow)
        {
            if (_finishedAt >= 0f) return;
            _finishedAt = now;
            _finishedUtc = utcNow;
        }

        /// <summary>Do primeiro tracking ao fim do quiz (ou <paramref name="now"/> se o quiz ainda corre).</summary>
        public float ElapsedSeconds(float now)
        {
            if (_startedAt < 0f) return 0f;
            var end = _finishedAt >= 0f ? _finishedAt : now;
            return Math.Max(0f, end - _startedAt);
        }

        /// <summary>Do primeiro tracking à confirmação do último risco (ou <paramref name="now"/> durante a inspeção).</summary>
        public float InspectionSeconds(float now)
        {
            if (_startedAt < 0f) return 0f;
            var end = _inspectionEndedAt >= 0f ? _inspectionEndedAt : now;
            return Math.Max(0f, end - _startedAt);
        }

        /// <summary>Do fim da inspeção à última resposta (0 se a inspeção não fechou).</summary>
        public float QuizSeconds(float now)
        {
            if (_inspectionEndedAt < 0f) return 0f;
            var end = _finishedAt >= 0f ? _finishedAt : now;
            return Math.Max(0f, end - _inspectionEndedAt);
        }
    }
}

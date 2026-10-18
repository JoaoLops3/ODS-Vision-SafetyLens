using System;

namespace Facens.SafetyLens
{
    /// <summary>Estado do marcador após o gate de graça (P1-6).</summary>
    public enum TargetVisibility { Tracking, Unstable, Lost }

    /// <summary>
    /// Evita flicker de "marcador perdido": Limited/None só vira Lost depois da graça
    /// temporal e de um mínimo de frames. Classe pura, sem UnityEngine.
    /// </summary>
    public sealed class TrackingLossGate
    {
        readonly float _graceSeconds;
        readonly int _minFrames;
        float _degradedSince = -1f;
        int _degradedFrames;

        /// <summary>Cria o gate. Valores negativos viram 0 / 1.</summary>
        public TrackingLossGate(float graceSeconds = 1.5f, int minFrames = 10)
        {
            _graceSeconds = Math.Max(0f, graceSeconds);
            _minFrames = Math.Max(1, minFrames);
        }

        /// <summary>Tracking imediato; senão Unstable até expirar graça + frames.</summary>
        public TargetVisibility Evaluate(bool isTracking, float now)
        {
            if (isTracking) { Reset(); return TargetVisibility.Tracking; }
            if (_degradedSince < 0f) _degradedSince = now;
            _degradedFrames++;
            bool expired = now - _degradedSince >= _graceSeconds
                           && _degradedFrames >= _minFrames;
            return expired ? TargetVisibility.Lost : TargetVisibility.Unstable;
        }

        /// <summary>Zera o prazo. Chamado ao sair da inspeção ou ao recomeçar.</summary>
        public void Reset() { _degradedSince = -1f; _degradedFrames = 0; }
    }
}

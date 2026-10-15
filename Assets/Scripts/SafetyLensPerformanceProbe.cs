using System;

namespace Facens.SafetyLens
{
    /// <summary>Resumo de desempenho de uma sessão (RNF02), pronto para o registro JSON/CSV.</summary>
    public struct SafetyLensPerformanceSummary
    {
        public int FrameCount;
        public float FpsAverage;
        /// <summary>Percentil 1 de FPS (percentil 99 de duração de frame): pior caso sustentado, não um frame isolado.</summary>
        public float FpsLow1Percent;
        /// <summary>Fração dos frames abaixo de 30 FPS (meta do RNF02).</summary>
        public float FramesBelow30Ratio;
        public float PeakMemoryMb;
    }

    /// <summary>
    /// Mede FPS e pico de memória durante a inspeção (RNF02). Classe pura, sem UnityEngine,
    /// testável em EditMode. Histograma de duração de frame com memória fixa: o custo por
    /// frame é O(1) e nada cresce com a duração da sessão — não pesa na thread principal.
    /// </summary>
    public sealed class SafetyLensPerformanceProbe
    {
        /// <summary>Frames acima disso caem todos no último bucket (1 s já é travada completa).</summary>
        const int MaxMs = 1000;
        const float Below30ThresholdSeconds = 1f / 30f;

        readonly int[] _buckets = new int[MaxMs + 1];
        int _frameCount;
        double _totalSeconds;
        int _framesBelow30;
        long _peakMemoryBytes;

        public int FrameCount => _frameCount;

        /// <summary>Registra a duração de um frame em segundos. Deltas inválidos (≤ 0, NaN) são ignorados.</summary>
        public void AddFrame(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || deltaSeconds <= 0f) return;

            _frameCount++;
            _totalSeconds += deltaSeconds;
            if (deltaSeconds > Below30ThresholdSeconds) _framesBelow30++;

            var ms = (int)(deltaSeconds * 1000f);
            _buckets[ms > MaxMs ? MaxMs : ms]++;
        }

        /// <summary>Guarda o pico de memória alocada observado (bytes).</summary>
        public void RecordMemoryBytes(long bytes)
        {
            if (bytes > _peakMemoryBytes) _peakMemoryBytes = bytes;
        }

        public void Reset()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _frameCount = 0;
            _totalSeconds = 0;
            _framesBelow30 = 0;
            _peakMemoryBytes = 0;
        }

        public SafetyLensPerformanceSummary Summarize()
        {
            var summary = new SafetyLensPerformanceSummary { FrameCount = _frameCount };
            if (_frameCount == 0 || _totalSeconds <= 0) return summary;

            summary.FpsAverage = (float)(_frameCount / _totalSeconds);
            summary.FramesBelow30Ratio = _framesBelow30 / (float)_frameCount;
            summary.PeakMemoryMb = _peakMemoryBytes / (1024f * 1024f);

            // Percentil 99 de duração de frame = percentil 1 de FPS.
            var target = (long)Math.Ceiling(_frameCount * 0.99);
            long cumulative = 0;
            var p99Ms = MaxMs;
            for (var ms = 0; ms <= MaxMs; ms++)
            {
                cumulative += _buckets[ms];
                if (cumulative >= target)
                {
                    p99Ms = ms;
                    break;
                }
            }

            // Centro do bucket [ms, ms+1): também evita divisão por zero no bucket 0.
            summary.FpsLow1Percent = 1000f / (p99Ms + 0.5f);
            return summary;
        }
    }
}

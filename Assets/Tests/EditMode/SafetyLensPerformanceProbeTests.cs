using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    /// <summary>RNF02: caracterização da sonda de FPS/memória (classe pura, sem cena AR).</summary>
    public sealed class SafetyLensPerformanceProbeTests
    {
        [Test]
        public void Summarize_WithoutSamples_ReturnsZeros()
        {
            var probe = new SafetyLensPerformanceProbe();
            var summary = probe.Summarize();

            Assert.AreEqual(0, summary.FrameCount);
            Assert.AreEqual(0f, summary.FpsAverage);
            Assert.AreEqual(0f, summary.FpsLow1Percent);
            Assert.AreEqual(0f, summary.FramesBelow30Ratio);
            Assert.AreEqual(0f, summary.PeakMemoryMb);
        }

        [Test]
        public void Summarize_SteadySixtyFps_AveragesSixtyAndNothingBelowThirty()
        {
            var probe = new SafetyLensPerformanceProbe();
            for (var i = 0; i < 600; i++)
                probe.AddFrame(1f / 60f);

            var summary = probe.Summarize();
            Assert.AreEqual(600, summary.FrameCount);
            Assert.AreEqual(60f, summary.FpsAverage, 0.1f);
            // Histograma em buckets de 1 ms: o percentil 1 fica perto de 60, não exato.
            Assert.That(summary.FpsLow1Percent, Is.InRange(55f, 65f));
            Assert.AreEqual(0f, summary.FramesBelow30Ratio);
        }

        [Test]
        public void Summarize_IsolatedSpike_HitsLow1PercentAndBelow30Ratio()
        {
            var probe = new SafetyLensPerformanceProbe();
            // 99 frames bons + 1 travada de 200 ms: o pior 1% deve refletir a travada.
            for (var i = 0; i < 99; i++)
                probe.AddFrame(1f / 60f);
            probe.AddFrame(0.2f);

            var summary = probe.Summarize();
            Assert.AreEqual(100, summary.FrameCount);
            Assert.That(summary.FpsLow1Percent, Is.LessThan(10f));
            Assert.AreEqual(0.01f, summary.FramesBelow30Ratio, 1e-5f);
            Assert.That(summary.FpsAverage, Is.GreaterThan(30f));
        }

        [Test]
        public void AddFrame_IgnoresZeroNegativeAndNaN()
        {
            var probe = new SafetyLensPerformanceProbe();
            probe.AddFrame(0f);
            probe.AddFrame(-0.016f);
            probe.AddFrame(float.NaN);

            Assert.AreEqual(0, probe.FrameCount);
        }

        [Test]
        public void RecordMemoryBytes_KeepsPeak_AndResetClearsEverything()
        {
            var probe = new SafetyLensPerformanceProbe();
            probe.AddFrame(1f / 60f);
            probe.RecordMemoryBytes(512L * 1024 * 1024);
            probe.RecordMemoryBytes(256L * 1024 * 1024);

            var summary = probe.Summarize();
            Assert.AreEqual(512f, summary.PeakMemoryMb, 0.01f);

            probe.Reset();
            var cleared = probe.Summarize();
            Assert.AreEqual(0, cleared.FrameCount);
            Assert.AreEqual(0f, cleared.PeakMemoryMb);
        }
    }
}

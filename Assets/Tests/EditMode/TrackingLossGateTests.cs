using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    public sealed class TrackingLossGateTests
    {
        [Test]
        public void Tracking_ReturnsTracking()
        {
            var gate = new TrackingLossGate(1.5f, 10);
            Assert.AreEqual(TargetVisibility.Tracking, gate.Evaluate(true, 0f));
        }

        [Test]
        public void DegradedUnderGrace_ReturnsUnstable()
        {
            var gate = new TrackingLossGate(1.5f, 10);
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 0f));
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 1.0f));
        }

        [Test]
        public void TimePassedButUnderMinFrames_ReturnsUnstable()
        {
            var gate = new TrackingLossGate(1.5f, 10);
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 0f));
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 2.0f));
        }

        [Test]
        public void TimeAndFramesExpired_ReturnsLost()
        {
            var gate = new TrackingLossGate(1.5f, 10);
            var now = 0f;
            TargetVisibility vis = TargetVisibility.Tracking;
            for (var i = 0; i < 10; i++)
            {
                vis = gate.Evaluate(false, now);
                now += 0.2f;
            }

            Assert.AreEqual(TargetVisibility.Lost, vis);
        }

        [Test]
        public void Lost_StaysLostUntilTracking()
        {
            var gate = new TrackingLossGate(0.1f, 1);
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 0f));
            Assert.AreEqual(TargetVisibility.Lost, gate.Evaluate(false, 0.2f));
            Assert.AreEqual(TargetVisibility.Lost, gate.Evaluate(false, 10f));
            Assert.AreEqual(TargetVisibility.Tracking, gate.Evaluate(true, 11f));
        }

        [Test]
        public void TrackingBeforeDeadline_ResetsToTracking()
        {
            var gate = new TrackingLossGate(1.5f, 10);
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 0f));
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 0.4f));
            Assert.AreEqual(TargetVisibility.Tracking, gate.Evaluate(true, 0.5f));
            Assert.AreEqual(TargetVisibility.Unstable, gate.Evaluate(false, 0.6f));
        }
    }
}

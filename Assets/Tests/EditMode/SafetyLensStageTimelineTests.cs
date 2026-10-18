using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensStageTimelineTests
    {
        [Test]
        public void RelativeSeconds_AreZero_BeforeMarks()
        {
            var t = new SafetyLensStageTimeline();
            Assert.AreEqual(0f, t.BriefSeconds);
            Assert.AreEqual(0f, t.TelemetryEndedAtSeconds);
            Assert.AreEqual(0f, t.FinishedAtSeconds);
        }

        [Test]
        public void Marks_AreRelativeToFirstTracking_AndSecondCallIsNoOp()
        {
            var t = new SafetyLensStageTimeline();
            t.MarkMarkerFound(10f);
            t.MarkMarkerFound(99f);
            t.MarkBriefDone(14.5f);
            t.MarkBriefDone(20f);
            t.MarkInspectionEnded(30f);
            t.MarkQuizEnded(40f);
            t.MarkTelemetryEnded(48f);
            t.MarkFinished(49f);

            Assert.AreEqual(4.5f, t.BriefSeconds, 1e-5f);
            Assert.AreEqual(20f, t.InspectionEndedAtSeconds, 1e-5f);
            Assert.AreEqual(30f, t.QuizEndedAtSeconds, 1e-5f);
            Assert.AreEqual(38f, t.TelemetryEndedAtSeconds, 1e-5f);
            Assert.AreEqual(39f, t.FinishedAtSeconds, 1e-5f);
        }

        [Test]
        public void WithoutTelemetryMark_TelemetryEndedAtSeconds_IsZero()
        {
            var t = new SafetyLensStageTimeline();
            t.MarkMarkerFound(0f);
            t.MarkBriefDone(5f);
            t.MarkInspectionEnded(20f);
            t.MarkQuizEnded(28f);
            t.MarkFinished(28.2f);

            Assert.AreEqual(0f, t.TelemetryEndedAtSeconds);
            Assert.AreEqual(28.2f, t.FinishedAtSeconds, 1e-5f);
        }

        [Test]
        public void Reset_ClearsAllMarks()
        {
            var t = new SafetyLensStageTimeline();
            t.MarkMarkerFound(0f);
            t.MarkBriefDone(3f);
            t.Reset();
            Assert.AreEqual(0f, t.BriefSeconds);
        }
    }
}

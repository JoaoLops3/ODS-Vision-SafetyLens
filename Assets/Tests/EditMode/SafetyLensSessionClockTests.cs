using System;
using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    /// <summary>
    /// Contrato RF04: as fórmulas do relógio não podem mudar sem quebrar o artigo.
    /// </summary>
    public sealed class SafetyLensSessionClockTests
    {
        [Test]
        public void ElapsedAndInspection_AreZero_BeforeStart()
        {
            var clock = new SafetyLensSessionClock();
            Assert.AreEqual(0f, clock.ElapsedSeconds(10f));
            Assert.AreEqual(0f, clock.InspectionSeconds(10f));
            Assert.AreEqual(0f, clock.QuizSeconds(10f));
            Assert.IsFalse(clock.HasInspectionStarted);
        }

        [Test]
        public void MarkInspectionStarted_SecondCall_DoesNotReset()
        {
            var clock = new SafetyLensSessionClock();
            var utc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            clock.MarkInspectionStarted(5f, utc);
            clock.MarkInspectionStarted(9f, utc.AddSeconds(4));

            Assert.AreEqual(5f, clock.ElapsedSeconds(10f), 1e-5f);
            Assert.AreEqual(utc, clock.StartedUtc);
        }

        [Test]
        public void InspectionEnds_ThenQuizRuns_ThenElapsedFreezesOnLastAnswer()
        {
            var clock = new SafetyLensSessionClock();
            var startUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            clock.MarkInspectionStarted(0f, startUtc);
            clock.MarkInspectionEnded(30.25f);
            clock.MarkQuizFinished(45.5f, startUtc.AddSeconds(45.5));

            Assert.AreEqual(45.5f, clock.ElapsedSeconds(999f), 1e-5f);
            Assert.AreEqual(30.25f, clock.InspectionSeconds(999f), 1e-5f);
            Assert.AreEqual(15.25f, clock.QuizSeconds(999f), 1e-5f);
            Assert.AreEqual(clock.ElapsedSeconds(999f), clock.InspectionSeconds(999f) + clock.QuizSeconds(999f), 1e-4f);
        }

        [Test]
        public void DuringInspection_ElapsedAndInspection_UseNow()
        {
            var clock = new SafetyLensSessionClock();
            clock.MarkInspectionStarted(2f, DateTime.UtcNow);

            Assert.AreEqual(8f, clock.ElapsedSeconds(10f), 1e-5f);
            Assert.AreEqual(8f, clock.InspectionSeconds(10f), 1e-5f);
            Assert.AreEqual(0f, clock.QuizSeconds(10f), 1e-5f);
        }

        [Test]
        public void TelemetryDoesNotCallMarks_ElapsedStaysFrozen()
        {
            var clock = new SafetyLensSessionClock();
            var utc = DateTime.UtcNow;
            clock.MarkInspectionStarted(0f, utc);
            clock.MarkInspectionEnded(12f);
            clock.MarkQuizFinished(20f, utc.AddSeconds(20));

            var elapsed = clock.ElapsedSeconds(20f);
            var inspection = clock.InspectionSeconds(20f);
            var quiz = clock.QuizSeconds(20f);

            Assert.AreEqual(elapsed, clock.ElapsedSeconds(80f), 1e-5f);
            Assert.AreEqual(inspection, clock.InspectionSeconds(80f), 1e-5f);
            Assert.AreEqual(quiz, clock.QuizSeconds(80f), 1e-5f);
        }

        [Test]
        public void EnsureFinished_OnlySetsWhenQuizDidNotFinish()
        {
            var clock = new SafetyLensSessionClock();
            clock.MarkInspectionStarted(0f, DateTime.UtcNow);
            clock.EnsureFinished(8f, DateTime.UtcNow);

            Assert.AreEqual(8f, clock.ElapsedSeconds(100f), 1e-5f);
            Assert.IsTrue(clock.HasQuizFinished);
        }

        [Test]
        public void Reset_ClearsAllMarks()
        {
            var clock = new SafetyLensSessionClock();
            clock.MarkInspectionStarted(1f, DateTime.UtcNow);
            clock.MarkInspectionEnded(2f);
            clock.MarkQuizFinished(3f, DateTime.UtcNow);
            clock.Reset();

            Assert.AreEqual(0f, clock.ElapsedSeconds(10f));
            Assert.IsFalse(clock.HasInspectionStarted);
        }
    }
}

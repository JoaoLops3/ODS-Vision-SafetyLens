using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensScenarioAndProgressTests
    {
        static SafetyLensScenario ValidScenario()
        {
            return new SafetyLensScenario
            {
                id = "t",
                title = "Teste",
                targetName = "SafetyLensEquipmentTarget",
                hazards = new[]
                {
                    new SafetyLensHazard { id = "HOT", label = "Hot", kind = "alert", u = 0.2f, v = 0.5f },
                    new SafetyLensHazard { id = "EPI", label = "Cap", kind = "epi", u = 0.8f, v = 0.5f }
                },
                quiz = new[]
                {
                    new SafetyLensQuizQuestion
                    {
                        id = 1,
                        question = "Q?",
                        options = new[] { "a", "b" },
                        correctIndex = 1
                    }
                },
                altTargets = new[]
                {
                    new SafetyLensTargetLayout
                    {
                        name = "SafetyLensQrTarget",
                        placements = new[]
                        {
                            new SafetyLensPlacement { hazardId = "HOT", u = 0.25f, v = 0.5f },
                            new SafetyLensPlacement { hazardId = "EPI", u = 0.75f, v = 0.5f }
                        }
                    }
                }
            };
        }

        [Test]
        public void Validator_AcceptsValidScenario()
        {
            var errors = SafetyLensScenarioValidator.Validate(ValidScenario(), "SafetyLensEquipmentTarget");
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        }

        [Test]
        public void Validator_RejectsBadUvAndCorrectIndex()
        {
            var s = ValidScenario();
            s.hazards[0].u = 1.5f;
            s.quiz[0].correctIndex = 9;
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.GreaterOrEqual(errors.Count, 2);
        }

        [Test]
        public void InspectionProgress_ConfirmRequiresArSelection_ThenAdvances()
        {
            var p = new SafetyLensInspectionProgress(2);
            Assert.IsFalse(p.ConfirmCurrentHazard(2));
            Assert.IsTrue(p.SelectCurrentHazardFromAr());
            Assert.IsFalse(p.ConfirmCurrentHazard(2));
            Assert.AreEqual(1, p.ActiveHazardIndex);
            Assert.IsTrue(p.SelectCurrentHazardFromAr());
            Assert.IsTrue(p.ConfirmCurrentHazard(2));
            Assert.AreEqual(2, p.HazardConfirmedCount);
        }

        [Test]
        public void InspectionProgress_WrongQuizIncrementsError()
        {
            var p = new SafetyLensInspectionProgress(1);
            var q = ValidScenario().quiz[0];
            Assert.IsTrue(p.TryAnswerQuiz(q, 0, 1, out var correct, out var last));
            Assert.IsFalse(correct);
            Assert.IsTrue(last);
            Assert.AreEqual(1, p.ErrorCount);
            Assert.AreEqual(0, p.QuizCorrect);
        }

        [Test]
        public void ResolveUv_UsesAltLayout_FallsBackToHazardUv()
        {
            var s = ValidScenario();
            var hot = s.hazards[0];

            // Layout alternativo (QR) tem placement próprio.
            var qrUv = s.ResolveUv(hot, "SafetyLensQrTarget");
            Assert.AreEqual(0.25f, qrUv.x, 1e-5f);
            Assert.AreEqual(0.5f, qrUv.y, 1e-5f);

            // Alvo principal usa o u/v do próprio hazard.
            var mainUv = s.ResolveUv(hot, "SafetyLensEquipmentTarget");
            Assert.AreEqual(hot.u, mainUv.x, 1e-5f);
            Assert.AreEqual(hot.v, mainUv.y, 1e-5f);

            // Alvo desconhecido (ou nulo) também cai no u/v do hazard.
            var unknownUv = s.ResolveUv(hot, "OutroAlvo");
            Assert.AreEqual(hot.u, unknownUv.x, 1e-5f);
            var nullUv = s.ResolveUv(hot, null);
            Assert.AreEqual(hot.u, nullUv.x, 1e-5f);
        }

        [Test]
        public void IsKnownTarget_AcceptsPrimaryAndAlt_RejectsOthers()
        {
            var s = ValidScenario();
            Assert.IsTrue(s.IsKnownTarget("SafetyLensEquipmentTarget"));
            Assert.IsTrue(s.IsKnownTarget("SafetyLensQrTarget"));
            Assert.IsFalse(s.IsKnownTarget("Qualquer"));
            Assert.IsFalse(s.IsKnownTarget(null));
        }

        [Test]
        public void Validator_RejectsAltTargetMissingPlacementAndUnknownHazard()
        {
            var s = ValidScenario();
            s.altTargets[0].placements = new[]
            {
                new SafetyLensPlacement { hazardId = "HOT", u = 0.25f, v = 0.5f },
                new SafetyLensPlacement { hazardId = "FANTASMA", u = 0.75f, v = 0.5f }
            };
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");

            // hazardId inexistente + falta placement do EPI.
            Assert.IsTrue(errors.Exists(e => e.Contains("FANTASMA")), string.Join("; ", errors));
            Assert.IsTrue(errors.Exists(e => e.Contains("EPI")), string.Join("; ", errors));
        }

        [Test]
        public void Validator_RejectsAltTargetDuplicateNameAndBadUv()
        {
            var s = ValidScenario();
            s.altTargets = new[]
            {
                s.altTargets[0],
                new SafetyLensTargetLayout
                {
                    name = "SafetyLensQrTarget",
                    placements = new[]
                    {
                        new SafetyLensPlacement { hazardId = "HOT", u = 1.5f, v = 0.5f },
                        new SafetyLensPlacement { hazardId = "EPI", u = 0.75f, v = 0.5f }
                    }
                }
            };
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");

            Assert.IsTrue(errors.Exists(e => e.Contains("duplicado")), string.Join("; ", errors));
            Assert.IsTrue(errors.Exists(e => e.Contains("fora de [0,1]")), string.Join("; ", errors));
        }

        [Test]
        public void Validator_RejectsAltTargetNamedLikePrimary()
        {
            var s = ValidScenario();
            s.altTargets[0].name = "SafetyLensEquipmentTarget";
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.IsTrue(errors.Exists(e => e.Contains("igual ao targetName")), string.Join("; ", errors));
        }

        [Test]
        public void LocalPositionOnMarker_WithExternalUv_UsesProvidedAnchor()
        {
            var hazard = ValidScenario().hazards[0];
            var size = new UnityEngine.Vector2(0.12f, 0.12f);
            var pos = hazard.LocalPositionOnMarker(size, 0.2f, 0.5f);

            Assert.AreEqual((0.2f - 0.5f) * 0.12f, pos.x, 1e-5f);
            Assert.AreEqual(0f, pos.z, 1e-5f);
        }

        [Test]
        public void BuildRecord_KeepsInspectionAndQuizSeconds()
        {
            var p = new SafetyLensInspectionProgress(1);
            var perf = new SafetyLensPerformanceSummary
            {
                FrameCount = 1800,
                FpsAverage = 58.5f,
                FpsLow1Percent = 41f,
                FramesBelow30Ratio = 0.01f,
                PeakMemoryMb = 512.5f
            };
            var record = p.BuildRecord(
                "P01",
                ValidScenario(),
                elapsedSeconds: 45.5f,
                inspectionSeconds: 30.25f,
                quizSeconds: 15.25f,
                performance: perf,
                markerName: "SafetyLensQrTarget",
                startedAtIso: "2026-01-01T00:00:00Z",
                finishedAtIso: "2026-01-01T00:00:45Z");

            Assert.AreEqual(45.5f, record.elapsedSeconds, 1e-5f);
            Assert.AreEqual(30.25f, record.inspectionSeconds, 1e-5f);
            Assert.AreEqual(15.25f, record.quizSeconds, 1e-5f);
            Assert.AreEqual(record.elapsedSeconds, record.inspectionSeconds + record.quizSeconds, 1e-4f);
            Assert.AreEqual(58.5f, record.fpsAverage, 1e-5f);
            Assert.AreEqual(41f, record.fpsLow1Percent, 1e-5f);
            Assert.AreEqual(0.01f, record.framesBelow30Ratio, 1e-5f);
            Assert.AreEqual(512.5f, record.peakMemoryMb, 1e-5f);
            Assert.AreEqual("SafetyLensQrTarget", record.markerName);
        }

        [Test]
        public void BuildRecord_QuizWithoutAnswers_QuizSecondsZero()
        {
            var p = new SafetyLensInspectionProgress(1);
            var record = p.BuildRecord(
                "P02",
                ValidScenario(),
                elapsedSeconds: 12f,
                inspectionSeconds: 12f,
                quizSeconds: 0f,
                performance: default,
                markerName: "SafetyLensEquipmentTarget",
                startedAtIso: "2026-01-01T00:00:00Z",
                finishedAtIso: "2026-01-01T00:00:12Z");

            Assert.AreEqual(0f, record.quizSeconds, 1e-5f);
            Assert.AreEqual(0, record.quizCorrect);
            Assert.AreEqual(0f, record.fpsAverage);
            Assert.AreEqual("SafetyLensEquipmentTarget", record.markerName);
        }

        [Test]
        public void ActiveRingPulseScale_ReducedMotionIsOne()
        {
#if SAFETYLENS_REDUCE_MOTION
            Assert.AreEqual(1f, SafetyLensAccessibility.ActiveRingPulseScale(1.23f), 1e-5f);
#else
            Assert.Pass("Sem define SAFETYLENS_REDUCE_MOTION — pulso ativo no Editor.");
#endif
        }
    }
}

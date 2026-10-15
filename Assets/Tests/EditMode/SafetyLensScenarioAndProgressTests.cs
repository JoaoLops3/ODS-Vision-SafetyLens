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

using NUnit.Framework;

namespace Facens.SafetyLens.Tests
{
    public sealed class SafetyLensTelemetryTests
    {
        static SafetyLensTelemetryThresholds DefaultThresholds() => new SafetyLensTelemetryThresholds
        {
            temperatureWarning = 50f,
            temperatureCritical = 70f,
            pressureWarning = 2.8f,
            pressureCritical = 3.2f,
            rpmWarning = 1600f,
            rpmCritical = 1800f
        };

        static SafetyLensTelemetrySample Sample(float t, float temp, float pressure, float rpm) =>
            new SafetyLensTelemetrySample
            {
                atSeconds = t,
                temperature = temp,
                pressure = pressure,
                rpm = rpm
            };

        static SafetyLensTelemetryStatusCopy PlanStatusCopy() =>
            new SafetyLensTelemetryStatusCopy
            {
                normal = "Ideal — operação na faixa segura.",
                warning = "Cuidado — valores elevados; monitore.",
                critical = "Perigoso — risco imediato; interrompa."
            };

        static SafetyLensScenario BaseScenario(SafetyLensTelemetryConfig telemetry = null) =>
            new SafetyLensScenario
            {
                id = "t",
                title = "Teste",
                targetName = "SafetyLensEquipmentTarget",
                hazards = new[]
                {
                    new SafetyLensHazard { id = "HOT", label = "Hot", kind = "alert", u = 0.2f, v = 0.5f }
                },
                quiz = new[]
                {
                    new SafetyLensQuizQuestion
                    {
                        id = 1,
                        question = "Q?",
                        options = new[] { "a", "b" },
                        correctIndex = 0
                    }
                },
                equipment = new SafetyLensEquipment
                {
                    name = "Compressor demo",
                    function = "Gera ar comprimido.",
                    summary = "Identifique os riscos antes de operar.",
                    riskHighlights = new[] { "Superfície quente" }
                },
                telemetry = telemetry
            };

        [Test]
        public void Evaluate_BelowAllThresholds_ReturnsNormal()
        {
            var status = SafetyLensTelemetry.Evaluate(Sample(0, 42.3f, 2.4f, 1450f), DefaultThresholds());
            Assert.AreEqual(SafetyLensTelemetryStatus.Normal, status);
            Assert.AreEqual("NORMAL", SafetyLensTelemetry.Label(status));
        }

        [Test]
        public void Evaluate_OneWarning_NoCritical_ReturnsWarning()
        {
            var status = SafetyLensTelemetry.Evaluate(Sample(0, 58.2f, 2.4f, 1450f), DefaultThresholds());
            Assert.AreEqual(SafetyLensTelemetryStatus.Warning, status);
            Assert.AreEqual("ATENCAO", SafetyLensTelemetry.Label(status));
        }

        [Test]
        public void Evaluate_OneCritical_OthersNormal_ReturnsCritical()
        {
            var status = SafetyLensTelemetry.Evaluate(Sample(0, 42.3f, 2.4f, 1840f), DefaultThresholds());
            Assert.AreEqual(SafetyLensTelemetryStatus.Critical, status);
            Assert.AreEqual("CRITICO", SafetyLensTelemetry.Label(status));
        }

        [Test]
        public void SampleAt_BetweenSamples_KeepsPrevious()
        {
            var samples = new[]
            {
                Sample(0f, 42.3f, 2.4f, 1450f),
                Sample(8f, 58.2f, 2.9f, 1660f),
                Sample(16f, 72.8f, 3.35f, 1840f)
            };

            var mid = SafetyLensTelemetry.SampleAt(samples, 7.9f);
            Assert.AreEqual(0f, mid.atSeconds, 1e-5f);
            Assert.AreEqual(42.3f, mid.temperature, 1e-5f);

            var atEight = SafetyLensTelemetry.SampleAt(samples, 8f);
            Assert.AreEqual(8f, atEight.atSeconds, 1e-5f);
            Assert.AreEqual(58.2f, atEight.temperature, 1e-5f);
        }

        [Test]
        public void SampleAt_NegativeOrBeforeFirst_ReturnsZeroSample()
        {
            var samples = new[]
            {
                Sample(0f, 42.3f, 2.4f, 1450f),
                Sample(8f, 58.2f, 2.9f, 1660f)
            };

            var before = SafetyLensTelemetry.SampleAt(samples, -1f);
            Assert.AreEqual(0f, before.atSeconds, 1e-5f);
            Assert.AreEqual(42.3f, before.temperature, 1e-5f);
        }

        [Test]
        public void IsEnabled_FalseWhenMissingOrDisabled()
        {
            Assert.IsFalse(SafetyLensTelemetry.IsEnabled(null));
            Assert.IsFalse(SafetyLensTelemetry.IsEnabled(BaseScenario(null)));
            Assert.IsFalse(SafetyLensTelemetry.IsEnabled(BaseScenario(new SafetyLensTelemetryConfig { enabled = false })));
            Assert.IsTrue(SafetyLensTelemetry.IsEnabled(BaseScenario(new SafetyLensTelemetryConfig
            {
                enabled = true,
                equipmentName = "Demo",
                thresholds = DefaultThresholds(),
                samples = new[] { Sample(0, 40f, 2f, 1400f) }
            })));
        }

        [Test]
        public void Validator_RejectsMissingZeroSample_AndUnorderedTimes()
        {
            var s = BaseScenario(new SafetyLensTelemetryConfig
            {
                enabled = true,
                equipmentName = "Demo",
                thresholds = DefaultThresholds(),
                samples = new[]
                {
                    Sample(2f, 40f, 2f, 1400f),
                    Sample(1f, 50f, 2.5f, 1500f)
                },
                statusCopy = PlanStatusCopy()
            });
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.IsTrue(errors.Exists(e => e.Contains("atSeconds == 0")), string.Join("; ", errors));
            Assert.IsTrue(errors.Exists(e => e.Contains("estritamente crescente")), string.Join("; ", errors));
        }

        [Test]
        public void Validator_RejectsCriticalNotGreaterThanWarning()
        {
            var thresholds = DefaultThresholds();
            thresholds.temperatureCritical = 40f;
            var s = BaseScenario(new SafetyLensTelemetryConfig
            {
                enabled = true,
                equipmentName = "Demo",
                thresholds = thresholds,
                samples = new[] { Sample(0f, 40f, 2f, 1400f) },
                statusCopy = PlanStatusCopy()
            });
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.IsTrue(errors.Exists(e => e.Contains("temperatureCritical")), string.Join("; ", errors));
        }

        [Test]
        public void Validator_RejectsEnabledTelemetryWithoutStatusCopy()
        {
            var s = BaseScenario(new SafetyLensTelemetryConfig
            {
                enabled = true,
                equipmentName = "Demo",
                thresholds = DefaultThresholds(),
                samples = new[] { Sample(0f, 40f, 2f, 1400f) }
            });
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.IsTrue(errors.Exists(e => e.Contains("statusCopy")), string.Join("; ", errors));
        }

        [Test]
        public void NarrativeLabel_UsesIdealCuidadoPerigoso()
        {
            Assert.AreEqual("Ideal", SafetyLensTelemetry.NarrativeLabel(SafetyLensTelemetryStatus.Normal));
            Assert.AreEqual("Cuidado", SafetyLensTelemetry.NarrativeLabel(SafetyLensTelemetryStatus.Warning));
            Assert.AreEqual("Perigoso", SafetyLensTelemetry.NarrativeLabel(SafetyLensTelemetryStatus.Critical));
        }

        [Test]
        public void Validator_AcceptsDisabledIncompleteBlock()
        {
            var s = BaseScenario(new SafetyLensTelemetryConfig { enabled = false });
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        }

        [Test]
        public void Validator_AcceptsPlanTelemetryBlock()
        {
            var s = BaseScenario(new SafetyLensTelemetryConfig
            {
                enabled = true,
                equipmentName = "Equipamento demo",
                thresholds = DefaultThresholds(),
                samples = new[]
                {
                    Sample(0f, 42.3f, 2.4f, 1450f),
                    Sample(8f, 58.2f, 2.9f, 1660f),
                    Sample(16f, 72.8f, 3.35f, 1840f)
                },
                statusCopy = PlanStatusCopy()
            });
            var errors = SafetyLensScenarioValidator.Validate(s, "SafetyLensEquipmentTarget");
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        }

        [Test]
        public void BuildRecord_WritesTelemetryFields()
        {
            var p = new SafetyLensInspectionProgress(1);
            var record = p.BuildRecord(
                "P01",
                BaseScenario(),
                elapsedSeconds: 12f,
                inspectionSeconds: 8f,
                quizSeconds: 4f,
                performance: default,
                markerName: "SafetyLensEquipmentTarget",
                startedAtIso: "a",
                finishedAtIso: "b",
                telemetryStatus: "CRITICO",
                temperature: 72.8f,
                pressure: 3.35f,
                rpm: 1840f);

            Assert.AreEqual("CRITICO", record.telemetryStatus);
            Assert.AreEqual(72.8f, record.temperature, 1e-4f);
            Assert.AreEqual(3.35f, record.pressure, 1e-4f);
            Assert.AreEqual(1840f, record.rpm, 1e-4f);
            Assert.AreEqual(0f, record.telemetryEndedAtSeconds);
        }

        [Test]
        public void NarrativeCopy_ReadsStatusCopyBlock()
        {
            var config = new SafetyLensTelemetryConfig { statusCopy = PlanStatusCopy() };
            Assert.AreEqual(PlanStatusCopy().normal, SafetyLensTelemetry.NarrativeCopy(config, SafetyLensTelemetryStatus.Normal));
            Assert.AreEqual(PlanStatusCopy().warning, SafetyLensTelemetry.NarrativeCopy(config, SafetyLensTelemetryStatus.Warning));
            Assert.AreEqual(PlanStatusCopy().critical, SafetyLensTelemetry.NarrativeCopy(config, SafetyLensTelemetryStatus.Critical));
            Assert.AreEqual(string.Empty, SafetyLensTelemetry.NarrativeCopy(null, SafetyLensTelemetryStatus.Normal));
        }
    }
}
